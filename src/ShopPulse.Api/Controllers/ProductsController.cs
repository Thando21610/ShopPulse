using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopPulse.Api.Data;
using ShopPulse.Api.Authorization;
using ShopPulse.Api.Domain;
using System.Text.Json;

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
        var shopToday = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(2)).Date);
        var items = await query
            .OrderBy(p => p.Name)
            .Select(p => new
            {
                p.Id, p.Sku, p.Name, p.Category, p.UnitPrice, p.MinimumStockLevel, p.HasExpiry,
                Available = p.Batches.Where(b => b.ExpiryDate == null || b.ExpiryDate >= shopToday).Sum(b => b.RemainingQuantity)
            })
            .ToListAsync(ct);
        return Ok(items);
    }
    public sealed record SaveProductRequest(
        string Sku, string Name, string? Category, decimal UnitPrice,
        int MinimumStockLevel, bool HasExpiry);

    [HttpGet("manage")]
    [Authorize(Policy = ShopPulsePolicies.CanManageProducts)]
    public async Task<IActionResult> Manage(CancellationToken ct)
    {
        var shopToday = DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(2)).Date);
        var items = await _db.Products.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new
            {
                x.Id, x.Sku, x.Name, x.Category, x.UnitPrice,
                x.MinimumStockLevel, x.HasExpiry, x.IsActive,
                Available = x.Batches.Where(b => b.ExpiryDate == null || b.ExpiryDate >= shopToday).Sum(b => b.RemainingQuantity)
            }).ToListAsync(ct);
        return Ok(items);
    }

    [HttpPost]
    [Authorize(Policy = ShopPulsePolicies.CanManageProducts)]
    public async Task<IActionResult> Create([FromBody] SaveProductRequest request, CancellationToken ct)
    {
        var validation = Validate(request);
        if (validation is not null) return BadRequest(new { code = "VALIDATION_FAILED", message = validation });
        var sku = request.Sku.Trim().ToUpperInvariant();
        if (await _db.Products.AnyAsync(x => x.Sku == sku, ct))
            return Conflict(new { code = "SKU_EXISTS", message = "That SKU is already used by another product." });

        var product = new Product
        {
            Id = Guid.NewGuid(), BusinessId = CurrentBusinessId(), Sku = sku,
            Name = request.Name.Trim(), Category = Clean(request.Category),
            UnitPrice = decimal.Round(request.UnitPrice, 2),
            MinimumStockLevel = request.MinimumStockLevel, HasExpiry = request.HasExpiry,
            IsActive = true
        };
        _db.Products.Add(product);
        AddAudit("ProductCreated", product.Id, new { product.Sku, product.Name, product.UnitPrice });
        await _db.SaveChangesAsync(ct);
        return Created($"/api/products/{product.Id}", new { product.Id, product.Sku, product.Name, product.Category, product.UnitPrice, product.MinimumStockLevel, product.HasExpiry, product.IsActive, Available = 0 });
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = ShopPulsePolicies.CanManageProducts)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveProductRequest request, CancellationToken ct)
    {
        var validation = Validate(request);
        if (validation is not null) return BadRequest(new { code = "VALIDATION_FAILED", message = validation });
        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (product is null) return NotFound(new { code = "PRODUCT_NOT_FOUND", message = "Product not found." });
        var sku = request.Sku.Trim().ToUpperInvariant();
        if (await _db.Products.AnyAsync(x => x.Id != id && x.Sku == sku, ct))
            return Conflict(new { code = "SKU_EXISTS", message = "That SKU is already used by another product." });

        var before = new { product.Sku, product.Name, product.UnitPrice, product.MinimumStockLevel, product.IsActive };
        product.Sku = sku;
        product.Name = request.Name.Trim();
        product.Category = Clean(request.Category);
        product.UnitPrice = decimal.Round(request.UnitPrice, 2);
        product.MinimumStockLevel = request.MinimumStockLevel;
        product.HasExpiry = request.HasExpiry;
        AddAudit("ProductUpdated", product.Id, new { Before = before, product.Sku, product.Name, product.UnitPrice, product.MinimumStockLevel, product.HasExpiry });
        await _db.SaveChangesAsync(ct);
        return Ok(new { product.Id, product.Sku, product.Name, product.Category, product.UnitPrice, product.MinimumStockLevel, product.HasExpiry, product.IsActive });
    }

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = ShopPulsePolicies.CanManageProducts)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (product is null) return NotFound(new { code = "PRODUCT_NOT_FOUND", message = "Product not found." });
        if (!product.IsActive) return Ok(new { product.Id, product.IsActive });
        product.IsActive = false;
        AddAudit("ProductDeactivated", product.Id, new { product.Sku, product.Name });
        await _db.SaveChangesAsync(ct);
        return Ok(new { product.Id, product.IsActive });
    }

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = ShopPulsePolicies.CanManageProducts)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (product is null) return NotFound(new { code = "PRODUCT_NOT_FOUND", message = "Product not found." });
        if (product.IsActive) return Ok(new { product.Id, product.IsActive });
        product.IsActive = true;
        AddAudit("ProductActivated", product.Id, new { product.Sku, product.Name });
        await _db.SaveChangesAsync(ct);
        return Ok(new { product.Id, product.IsActive });
    }

    private static string? Validate(SaveProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Sku) || request.Sku.Trim().Length > 64)
            return "Enter a SKU between 1 and 64 characters.";
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200)
            return "Enter a product name between 1 and 200 characters.";
        if (request.Category?.Length > 80) return "Category cannot exceed 80 characters.";
        if (request.UnitPrice < 0 || request.UnitPrice > 10000000m)
            return "Unit price must be between R0.00 and R10,000,000.00.";
        if (request.MinimumStockLevel < 0 || request.MinimumStockLevel > 1000000)
            return "Minimum stock level must be zero or greater.";
        return null;
    }

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private Guid CurrentBusinessId()
        => HttpContext.User.FindFirst("business_id") is { } claim && Guid.TryParse(claim.Value, out var id) ? id : Guid.Empty;

    private void AddAudit(string action, Guid productId, object details)
    {
        _db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(), BusinessId = CurrentBusinessId(),
            ActorUserId = HttpContext.User.FindFirst("sub") is { } claim && Guid.TryParse(claim.Value, out var userId) ? userId : null,
            Action = action, EntityType = "Product", EntityId = productId,
            DetailsJson = JsonSerializer.Serialize(details), CreatedAtUtc = DateTimeOffset.UtcNow
        });
    }

}
