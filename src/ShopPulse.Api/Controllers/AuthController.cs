using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShopPulse.Api.Auth;
using ShopPulse.Api.Data;

namespace ShopPulse.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AuthService _auth;
    private readonly AppDbContext _db;

    public AuthController(AuthService auth, AppDbContext db)
    {
        _auth = auth;
        _db = db;
    }

    public record LoginRequest(string Email, string Password);

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return Problem(statusCode: 400, title: "Validation failed",
                detail: "Email and password are required.",
                extensions: new Dictionary<string, object?> { ["code"] = "VALIDATION_FAILED" });

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();
        var (ok, error, data) = await _auth.LoginAsync(req.Email.Trim(), req.Password, ip, ua, ct);

        if (!ok || data is null)
        {
            return Problem(statusCode: 401, title: "Sign-in failed",
                detail: "Email or password is incorrect.",
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = error ?? "AUTH_INVALID_CREDENTIALS",
                    ["correlationId"] = HttpContext.TraceIdentifier
                });
        }

        SetRefreshCookie(data.RefreshTokenRaw);
        return Ok(new
        {
            accessToken = data.AccessToken,
            expiresAtUtc = data.ExpiresAtUtc,
            user = data.User
        });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var raw = Request.Cookies[_auth.CookieOptions.CookieName];
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = Request.Headers.UserAgent.ToString();
        var (ok, error, data) = await _auth.RefreshAsync(raw, ip, ua, ct);

        if (!ok || data is null)
        {
            ClearRefreshCookie();
            return Problem(statusCode: 401, title: "Session ended",
                detail: error switch
                {
                    "AUTH_REFRESH_REUSED" => "Session ended for security reasons. Please sign in again.",
                    "AUTH_REFRESH_EXPIRED" => "Your session has expired. Please sign in again.",
                    _ => "Please sign in again."
                },
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = error ?? "AUTH_REFRESH_INVALID",
                    ["correlationId"] = HttpContext.TraceIdentifier
                });
        }

        SetRefreshCookie(data.RefreshTokenRaw);
        return Ok(new { accessToken = data.AccessToken, expiresAtUtc = data.ExpiresAtUtc });
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        var raw = Request.Cookies[_auth.CookieOptions.CookieName];
        await _auth.LogoutAsync(raw, ct);
        ClearRefreshCookie();
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var sub = User.FindFirst("sub")?.Value;
        if (sub is null || !Guid.TryParse(sub, out var userId))
            return Unauthorized();

        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        if (user is null) return Unauthorized();

        var business = await _db.Businesses.FindAsync(new object[] { user.BusinessId }, ct);
        return Ok(new UserDto
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Role = user.Role,
            BusinessId = user.BusinessId,
            BusinessName = business?.Name ?? "",
            CanAdjustStock = user.CanAdjustStock,
            CanRefundSales = user.CanRefundSales,
            CanManageProducts = user.CanManageProducts,
            CanReceiveStock = user.CanReceiveStock
        });
    }

    private void SetRefreshCookie(string raw)
    {
        var opt = _auth.CookieOptions;
        var isHttps = Request.IsHttps;
        Response.Cookies.Append(opt.CookieName, raw, new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax, // None needed for cross-site HTTPS
            Path = opt.CookiePath,
            MaxAge = TimeSpan.FromDays(opt.IdleDays),
            IsEssential = true
        });
    }

    private void ClearRefreshCookie()
    {
        var opt = _auth.CookieOptions;
        var isHttps = Request.IsHttps;
        Response.Cookies.Delete(opt.CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = isHttps,
            SameSite = isHttps ? SameSiteMode.None : SameSiteMode.Lax,
            Path = opt.CookiePath
        });
    }
}
