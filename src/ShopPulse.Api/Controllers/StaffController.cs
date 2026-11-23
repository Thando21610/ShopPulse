using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopPulse.Api.Authorization;
using ShopPulse.Api.Data;
using ShopPulse.Api.Domain;

namespace ShopPulse.Api.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize(Policy = ShopPulsePolicies.OwnerOnly)]
public sealed class StaffController : ControllerBase
{
    private readonly AppDbContext _db;
    private Guid BusinessId => Guid.TryParse(User.FindFirst("business_id")?.Value, out var id) ? id : Guid.Empty;
    private Guid ActorId => Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : Guid.Empty;

    public StaffController(AppDbContext db) => _db = db;

    public sealed record CreateStaffRequest(string DisplayName, string Email, string Password, bool CanAdjustStock, bool CanRefundSales);
    public sealed record UpdateStaffRequest(string DisplayName, bool IsActive, bool CanAdjustStock, bool CanRefundSales);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var users = await _db.Users.AsNoTracking().OrderBy(x => x.Role).ThenBy(x => x.DisplayName)
            .Select(x => new { x.Id, x.DisplayName, x.Email, x.Role, x.IsActive, x.CanAdjustStock, x.CreatedAtUtc })
            .ToListAsync(ct);
        return Ok(users);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStaffRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Trim().Length > 120)
            return BadRequest(new { code = "VALIDATION_FAILED", message = "Enter a staff name up to 120 characters." });
        var email = request.Email?.Trim().ToLowerInvariant() ?? "";
        if (!new EmailAddressAttribute().IsValid(email) || email.Length > 256)
            return BadRequest(new { code = "VALIDATION_FAILED", message = "Enter a valid email address." });
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 12 || request.Password.Length > 128)
            return BadRequest(new { code = "VALIDATION_FAILED", message = "Set an initial password between 12 and 128 characters." });
        if (await _db.Users.IgnoreQueryFilters().AnyAsync(x => x.Email == email, ct))
            return Conflict(new { code = "EMAIL_EXISTS", message = "An account with that email address already exists." });

        var user = new AppUser
        {
            Id = Guid.NewGuid(), BusinessId = BusinessId, DisplayName = request.DisplayName.Trim(),
            Email = email, PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "Operator", IsActive = true, CanAdjustStock = request.CanAdjustStock, CanRefundSales = request.CanRefundSales
        };
        _db.Users.Add(user);
        AddAudit("StaffCreated", user.Id, new { user.DisplayName, user.Email, user.Role, user.CanAdjustStock });
        await _db.SaveChangesAsync(ct);
        return Created($"/api/staff/{user.Id}", ToDto(user));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStaffRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Trim().Length > 120)
            return BadRequest(new { code = "VALIDATION_FAILED", message = "Enter a staff name up to 120 characters." });
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (user is null) return NotFound(new { code = "STAFF_NOT_FOUND", message = "Staff member not found." });
        if (user.Role == "Owner")
            return BadRequest(new { code = "OWNER_PROTECTED", message = "Owner accounts cannot be changed from staff management." });

        var before = new { user.DisplayName, user.IsActive, user.CanAdjustStock, user.CanRefundSales };
        var wasActive = user.IsActive;
        user.DisplayName = request.DisplayName.Trim();
        user.IsActive = request.IsActive;
        user.CanAdjustStock = request.CanAdjustStock;
        user.CanRefundSales = request.CanRefundSales;
        if (wasActive && !user.IsActive)
        {
            var activeTokens = await _db.RefreshTokens
                .Where(token => token.UserId == user.Id && token.RevokedAtUtc == null)
                .ToListAsync(ct);
            foreach (var token in activeTokens)
            {
                token.RevokedAtUtc = DateTimeOffset.UtcNow;
                token.RevokedReason = "ADMIN";
            }
        }
        AddAudit("StaffUpdated", user.Id, new { Before = before, user.DisplayName, user.IsActive, user.CanAdjustStock, user.CanRefundSales });
        await _db.SaveChangesAsync(ct);
        return Ok(ToDto(user));
    }

    private static object ToDto(AppUser user) => new
    {
        user.Id, user.DisplayName, user.Email, user.Role, user.IsActive, user.CanAdjustStock, user.CanRefundSales, user.CreatedAtUtc
    };

    private void AddAudit(string action, Guid userId, object details)
    {
        _db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(), BusinessId = BusinessId, ActorUserId = ActorId,
            Action = action, EntityType = "AppUser", EntityId = userId,
            DetailsJson = JsonSerializer.Serialize(details), CreatedAtUtc = DateTimeOffset.UtcNow
        });
    }
}
