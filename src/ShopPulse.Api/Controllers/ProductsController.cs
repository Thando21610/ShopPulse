using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopPulse.Api.Data;

namespace ShopPulse.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize(Policy = "Staff")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProductsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? q, CancellationToken ct)
    {
        var query = _db.Products.AsNoTracking().Where(p => p.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(term) || p.Sku.ToLower().Contains(term));
        }
        var items = await query
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id, p.Sku, p.Name, p.UnitPrice, p.MinimumStockLevel, p.HasExpiry,
                Available = p.Batches.Sum(b => b.RemainingQuantity)
            })
            .ToListAsync(ct);
        return Ok(items);
    }
}
