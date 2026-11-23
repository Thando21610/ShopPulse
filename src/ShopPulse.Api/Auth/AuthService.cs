using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShopPulse.Api.Data;
using ShopPulse.Api.Domain;

namespace ShopPulse.Api.Auth;

public class JwtOptions
{
    public string Issuer { get; set; } = "shoppulse-api";
    public string Audience { get; set; } = "shoppulse-app";
    public string SigningKey { get; set; } = default!;
    public int AccessTokenMinutes { get; set; } = 15;
    public int ClockSkewSeconds { get; set; } = 60;
}

public class RefreshTokenOptions
{
    public string CookieName { get; set; } = "sp_rt";
    public string CookiePath { get; set; } = "/api/auth";
    public int IdleDays { get; set; } = 7;
    public int AbsoluteDays { get; set; } = 30;
}

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly JwtOptions _jwt;
    private readonly RefreshTokenOptions _rtOpt;
    private readonly IConfiguration _config;

    public AuthService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
        _jwt = config.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        _jwt.SigningKey = config["Jwt:SigningKey"]
            ?? throw new InvalidOperationException("Jwt:SigningKey must be configured.");
        _rtOpt = config.GetSection("RefreshToken").Get<RefreshTokenOptions>() ?? new RefreshTokenOptions();
    }

    public async Task<(bool Ok, string? Error, LoginResponse? Data)> LoginAsync(
        string email, string password, string? ip, string? ua, CancellationToken ct)
    {
        var user = await _db.Users
            .IgnoreQueryFilters()
            .Include(u => u.Business)
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

        if (user is null || !user.IsActive || !user.Business.IsActive)
            return (false, "AUTH_INVALID_CREDENTIALS", null);

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc > DateTimeOffset.UtcNow)
            return (false, "AUTH_ACCOUNT_LOCKED", null);

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= 5)
                user.LockoutEndUtc = DateTimeOffset.UtcNow.AddMinutes(15);
            await _db.SaveChangesAsync(ct);
            return (false, "AUTH_INVALID_CREDENTIALS", null);
        }

        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;

        var familyId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var (raw, entity) = CreateRefreshToken(user.Id, familyId, now, now.AddDays(_rtOpt.AbsoluteDays));
        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync(ct);

        var access = IssueAccessToken(user);
        return (true, null, new LoginResponse
        {
            AccessToken = access.Token,
            ExpiresAtUtc = access.Expires,
            User = new UserDto
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email,
                Role = user.Role,
                BusinessId = user.BusinessId,
                BusinessName = user.Business.Name,
                CanAdjustStock = user.CanAdjustStock,
                CanRefundSales = user.CanRefundSales,
                CanManageProducts = user.CanManageProducts,
                CanReceiveStock = user.CanReceiveStock
            },
            RefreshTokenRaw = raw
        });
    }

    public async Task<(bool Ok, string? Error, RefreshResponse? Data)> RefreshAsync(
        string? rawToken, string? ip, string? ua, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return (false, "AUTH_REFRESH_MISSING", null);

        var hash = Hash(rawToken);
        var token = await _db.RefreshTokens
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (token is null)
            return (false, "AUTH_REFRESH_INVALID", null);

        if (token.RevokedAtUtc.HasValue)
        {
            // Reuse detection → revoke family
            await RevokeFamilyAsync(token.FamilyId, "REUSE_DETECTED", ct);
            return (false, "AUTH_REFRESH_REUSED", null);
        }

        if (token.ExpiresAtUtc < DateTimeOffset.UtcNow)
            return (false, "AUTH_REFRESH_EXPIRED", null);

        var user = await _db.Users.IgnoreQueryFilters()
            .Include(u => u.Business)
            .FirstOrDefaultAsync(u => u.Id == token.UserId, ct);

        if (user is null || !user.IsActive || !user.Business.IsActive)
            return (false, "AUTH_ACCOUNT_DISABLED", null);

        // Claim the one-time token atomically. A read-then-save update is unsafe:
        // two concurrent requests can both read the active token and both rotate it.
        var now = DateTimeOffset.UtcNow;
        var (newRaw, next) = CreateRefreshToken(
            user.Id, token.FamilyId, now,
            token.FamilyAbsoluteExpiryUtc ?? now.AddDays(_rtOpt.AbsoluteDays));

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var claimed = await _db.RefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.Id == token.Id
                && t.RevokedAtUtc == null
                && t.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(update => update
                .SetProperty(t => t.RevokedAtUtc, now)
                .SetProperty(t => t.RevokedReason, "ROTATED")
                .SetProperty(t => t.ReplacedByTokenId, next.Id), ct);

        if (claimed != 1)
        {
            await transaction.RollbackAsync(ct);

            // A token that lost the atomic claim is a replay. Preserve strict
            // reuse detection by revoking every still-active token in its family.
            await RevokeFamilyAsync(token.FamilyId, "REUSE_DETECTED", ct);
            return (false, "AUTH_REFRESH_REUSED", null);
        }

        _db.RefreshTokens.Add(next);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var access = IssueAccessToken(user);
        return (true, null, new RefreshResponse
        {
            AccessToken = access.Token,
            ExpiresAtUtc = access.Expires,
            RefreshTokenRaw = newRaw
        });
    }

    public async Task LogoutAsync(string? rawToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rawToken)) return;
        var hash = Hash(rawToken);
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null || token.RevokedAtUtc.HasValue) return;
        await RevokeFamilyAsync(token.FamilyId, "LOGOUT", ct);
    }

    private async Task RevokeFamilyAsync(Guid familyId, string reason, CancellationToken ct)
    {
        await _db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAtUtc == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.RevokedAtUtc, DateTimeOffset.UtcNow)
                .SetProperty(t => t.RevokedReason, reason), ct);
    }

    private (string Token, DateTimeOffset Expires) IssueAccessToken(AppUser user)
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role),
            new("role", user.Role),
            new("business_id", user.BusinessId.ToString()),
            new("business_name", user.Business?.Name ?? ""),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }

    private (string Raw, RefreshToken Entity) CreateRefreshToken(
        Guid userId, Guid familyId, DateTimeOffset now, DateTimeOffset absoluteExpiry)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var idleExpiry = now.AddDays(_rtOpt.IdleDays);
        var expires = idleExpiry < absoluteExpiry ? idleExpiry : absoluteExpiry;

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FamilyId = familyId,
            TokenHash = Hash(raw),
            CreatedAtUtc = now,
            ExpiresAtUtc = expires,
            FamilyAbsoluteExpiryUtc = absoluteExpiry
        };
        return (raw, entity);
    }

    public static string Hash(string raw)
        => Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    public RefreshTokenOptions CookieOptions => _rtOpt;
}

public record LoginResponse
{
    public string AccessToken { get; init; } = default!;
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public UserDto User { get; init; } = default!;
    public string RefreshTokenRaw { get; init; } = default!; // set as cookie only
}

public record RefreshResponse
{
    public string AccessToken { get; init; } = default!;
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public string RefreshTokenRaw { get; init; } = default!;
}

public record UserDto
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = default!;
    public string Email { get; init; } = default!;
    public string Role { get; init; } = default!;
    public Guid BusinessId { get; init; }
    public string BusinessName { get; init; } = default!;
    public bool CanAdjustStock { get; init; }
    public bool CanRefundSales { get; init; }
    public bool CanManageProducts { get; init; }
    public bool CanReceiveStock { get; init; }
}
