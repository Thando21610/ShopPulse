using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopPulse.Api.Authorization;
using ShopPulse.Api.Data;
using ShopPulse.Api.Domain;

namespace ShopPulse.Api.Controllers;

[ApiController]
[Route("api/business")]
[Authorize(Policy = ShopPulsePolicies.OwnerOnly)]
public sealed class BusinessController : ControllerBase
{
    private readonly AppDbContext _db;
    private Guid BusinessId => Guid.TryParse(User.FindFirst("business_id")?.Value, out var id) ? id : Guid.Empty;
    private Guid ActorId => Guid.TryParse(User.FindFirst("sub")?.Value, out var id) ? id : Guid.Empty;

    public BusinessController(AppDbContext db) => _db = db;

    [HttpGet("current")]
    public async Task<IActionResult> Current(CancellationToken ct)
    {
        var business = await _db.Businesses.FirstOrDefaultAsync(x => x.Id == BusinessId, ct);
        return business is null ? NotFound() : Ok(new { business.Id, business.Name, business.Address });
    }

    [HttpPut("current")]
    public async Task<IActionResult> Update([FromBody] UpdateBusinessRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200)
            return BadRequest(new { code = "VALIDATION_FAILED", message = "Enter a business name up to 200 characters." });
        if (request.Address?.Length > 500)
            return BadRequest(new { code = "VALIDATION_FAILED", message = "Address cannot exceed 500 characters." });

        var business = await _db.Businesses.FirstOrDefaultAsync(x => x.Id == BusinessId, ct);
        if (business is null) return NotFound();
        var before = new { business.Name, business.Address };
        business.Name = request.Name.Trim();
        business.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
        _db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(), BusinessId = BusinessId, ActorUserId = ActorId,
            Action = "BusinessSettingsUpdated", EntityType = "Business", EntityId = business.Id,
            DetailsJson = JsonSerializer.Serialize(new { Before = before, business.Name, business.Address }),
            CreatedAtUtc = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        return Ok(new { business.Id, business.Name, business.Address });
    }

    public sealed record UpdateBusinessRequest(string Name, string? Address);
}
