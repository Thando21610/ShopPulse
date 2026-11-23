using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopPulse.Api.Authorization;
using ShopPulse.Api.Data;

namespace ShopPulse.Api.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize(Policy = ShopPulsePolicies.OwnerOnly)]
public sealed class AuditController : ControllerBase
{
    private readonly AppDbContext _db;
    public AuditController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int take = 100, [FromQuery] string? action = null, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 250);
        var query = _db.AuditEvents.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(action))
        {
            var term = action.Trim().ToLowerInvariant();
            query = query.Where(x => x.Action.ToLower().Contains(term));
        }
        var rows = await query.OrderByDescending(x => x.CreatedAtUtc).Take(take)
            .Select(x => new
            {
                x.Id, x.Action, x.EntityType, x.EntityId, x.DetailsJson, x.CreatedAtUtc,
                ActorName = _db.Users.Where(user => user.Id == x.ActorUserId).Select(user => user.DisplayName).FirstOrDefault()
            }).ToListAsync(ct);
        return Ok(rows);
    }
}
