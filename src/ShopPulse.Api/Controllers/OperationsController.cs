using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopPulse.Api.Authorization;
using ShopPulse.Api.Data;
using ShopPulse.Api.Domain;

namespace ShopPulse.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Policy = ShopPulsePolicies.Staff)]
public sealed class OperationsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentBusiness _current;

    public OperationsController(AppDbContext db, ICurrentBusiness current)
    {
        _db = db;
        _current = current;
    }

    public sealed record SaleLineRequest(Guid ProductId, int Quantity);
    public sealed record CompleteSaleRequest(IReadOnlyList<SaleLineRequest> Items, string PaymentMethod, decimal AmountTendered);
    public sealed record SaleResult(Guid Id, string ReceiptNumber, decimal Subtotal, decimal Discount, decimal GrandTotal, string PaymentMethod, decimal AmountTendered, decimal ChangeAmount, DateTimeOffset CreatedAtUtc, IReadOnlyList<SaleLineResult> Items);
    public sealed record SaleLineResult(string ProductName, string Sku, int Quantity, decimal UnitPrice, decimal LineTotal);
    public sealed record ReceiveStockRequest(Guid ProductId, int Quantity, decimal UnitCost, DateOnly? ExpiryDate, string? Supplier, string? BatchReference);
    public sealed record WasteRequest(Guid ProductId, Guid StockBatchId, int Quantity, string Reason, string? Notes);
    public sealed record OpenCashShiftRequest(decimal OpeningFloat);
    public sealed record CloseCashShiftRequest(decimal ActualCash, string? VarianceReason);

    [HttpPost("sales")]
    [Authorize(Policy = ShopPulsePolicies.CompleteSales)]
    public async Task<IActionResult> CompleteSale([FromBody] CompleteSaleRequest request, CancellationToken ct)
    {
        var key = Request.Headers["Idempotency-Key"].ToString().Trim();
        if (key.Length is < 8 or > 128)
            return Failure(400, "VALIDATION_FAILED", "A valid Idempotency-Key header (8–128 characters) is required.");

        if (request.Items is null || request.Items.Count == 0 || request.Items.Count > 100)
            return Failure(400, "VALIDATION_FAILED", "Add at least one product to the sale.");

        if (request.Items.Any(x => x.ProductId == Guid.Empty || x.Quantity <= 0 || x.Quantity > 10000))
            return Failure(400, "VALIDATION_FAILED", "Every sale line must have a product and a positive quantity.");

        var method = request.PaymentMethod?.Trim();
        if (method is not ("Cash" or "Card"))
            return Failure(400, "VALIDATION_FAILED", "Payment method must be Cash or Card.");

        var normalized = request.Items.GroupBy(x => x.ProductId)
            .Select(g => new SaleLineRequest(g.Key, g.Sum(x => x.Quantity)))
            .OrderBy(x => x.ProductId).ToArray();
        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new { Items = normalized, PaymentMethod = method, request.AmountTendered }))));

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        var previous = await _db.IdempotencyRecords.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Key == key, ct);
        if (previous is not null)
        {
            if (previous.RequestHash != requestHash)
                return Conflict(new { code = "IDEMPOTENCY_KEY_REUSED", message = "This idempotency key was already used for a different sale." });
            var replay = JsonSerializer.Deserialize<SaleResult>(previous.ResponseJson);
            await transaction.CommitAsync(ct);
            return StatusCode(previous.StatusCode, replay);
        }

        var productIds = normalized.Select(x => x.ProductId).ToArray();
        var products = await _db.Products.Where(x => productIds.Contains(x.Id) && x.IsActive).ToDictionaryAsync(x => x.Id, ct);
        if (products.Count != productIds.Length)
            return Failure(400, "VALIDATION_FAILED", "One or more selected products are unavailable.");

        var subtotal = normalized.Sum(line => products[line.ProductId].UnitPrice * line.Quantity);
        const decimal discount = 0m;
        var total = subtotal - discount;
        var tendered = method == "Card" ? total : request.AmountTendered;
        if (tendered < total)
            return Failure(400, "INSUFFICIENT_PAYMENT", "The cash received is less than the sale total.");
        if (tendered < 0 || tendered > 100000000m)
            return Failure(400, "VALIDATION_FAILED", "Enter a valid payment amount.");

        var sale = new Sale
        {
            Id = Guid.NewGuid(),
            BusinessId = _current.BusinessId,
            ReceiptNumber = $"SP-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
            OperatorUserId = _current.UserId,
            Subtotal = subtotal,
            Discount = discount,
            GrandTotal = total,
            PaymentMethod = method!,
            AmountTendered = tendered,
            ChangeAmount = method == "Cash" ? tendered - total : 0m,
            Status = "Completed",
            IdempotencyKey = key,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        var lineResults = new List<SaleLineResult>();
        foreach (var line in normalized)
        {
            var product = products[line.ProductId];
            var batches = await _db.StockBatches
                .Where(x => x.ProductId == product.Id && x.RemainingQuantity > 0)
                .OrderBy(x => x.ExpiryDate ?? DateOnly.MaxValue)
                .ThenBy(x => x.ReceivedAtUtc)
                .ToListAsync(ct);
            var available = batches.Sum(x => x.RemainingQuantity);
            if (available < line.Quantity)
                return Conflict(new { code = "INSUFFICIENT_STOCK", productId = product.Id, productName = product.Name, available, requested = line.Quantity, message = $"Only {available} units of {product.Name} remain." });

            var remaining = line.Quantity;
            foreach (var batch in batches)
            {
                if (remaining == 0) break;
                var allocated = Math.Min(batch.RemainingQuantity, remaining);
                batch.RemainingQuantity -= allocated;
                batch.Version++;
                remaining -= allocated;

                sale.Items.Add(new SaleItem
                {
                    Id = Guid.NewGuid(),
                    ProductId = product.Id,
                    ProductNameSnapshot = product.Name,
                    SkuSnapshot = product.Sku,
                    Quantity = allocated,
                    UnitPriceSnapshot = product.UnitPrice,
                    LineTotal = product.UnitPrice * allocated,
                    StockBatchId = batch.Id
                });
                _db.StockMovements.Add(new StockMovement
                {
                    Id = Guid.NewGuid(), BusinessId = _current.BusinessId, ProductId = product.Id,
                    StockBatchId = batch.Id, MovementType = "Sale", QuantityChange = -allocated,
                    ReferenceId = sale.Id, ActorUserId = _current.UserId
                });
            }
            lineResults.Add(new SaleLineResult(product.Name, product.Sku, line.Quantity, product.UnitPrice, product.UnitPrice * line.Quantity));
        }

        _db.Sales.Add(sale);
        var result = new SaleResult(sale.Id, sale.ReceiptNumber, sale.Subtotal, sale.Discount, sale.GrandTotal,
            sale.PaymentMethod, sale.AmountTendered, sale.ChangeAmount, sale.CreatedAtUtc, lineResults);
        _db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Id = Guid.NewGuid(), BusinessId = _current.BusinessId, Key = key, RequestHash = requestHash,
            StatusCode = 201, ResponseJson = JsonSerializer.Serialize(result),
            CreatedAtUtc = DateTimeOffset.UtcNow, ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(2)
        });
        AddAudit("SaleCompleted", "Sale", sale.Id, new { sale.ReceiptNumber, sale.GrandTotal, sale.PaymentMethod });
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return Created($"/api/sales/{sale.Id}", result);
    }

    [HttpGet("sales")]
    [Authorize(Policy = ShopPulsePolicies.ViewFinancialReports)]
    public async Task<IActionResult> ListSales([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken ct)
    {
        var query = _db.Sales.AsNoTracking().Where(x => x.Status == "Completed");
        if (from.HasValue) query = query.Where(x => x.CreatedAtUtc >= from.Value);
        if (to.HasValue) query = query.Where(x => x.CreatedAtUtc < to.Value);
        var sales = await query.OrderByDescending(x => x.CreatedAtUtc).Take(500)
            .Select(x => new { x.Id, x.ReceiptNumber, x.Subtotal, x.Discount, x.GrandTotal, x.PaymentMethod, x.AmountTendered, x.ChangeAmount, x.CreatedAtUtc })
            .ToListAsync(ct);
        return Ok(sales);
    }

    [HttpPost("stock/receipts")]
    [Authorize(Policy = ShopPulsePolicies.ReceiveStock)]
    public async Task<IActionResult> ReceiveStock([FromBody] ReceiveStockRequest request, CancellationToken ct)
    {
        if (request.ProductId == Guid.Empty || request.Quantity is < 1 or > 100000 || request.UnitCost < 0 || request.UnitCost > 10000000m)
            return Failure(400, "VALIDATION_FAILED", "Choose a product and enter a positive quantity and a non-negative unit cost.");
        if (request.ExpiryDate.HasValue && request.ExpiryDate.Value < DateOnly.FromDateTime(DateTime.UtcNow))
            return Failure(400, "VALIDATION_FAILED", "Expiry date cannot be in the past.");

        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId && x.IsActive, ct);
        if (product is null) return NotFound(new { code = "PRODUCT_NOT_FOUND", message = "The selected product could not be found." });

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var batch = new StockBatch
        {
            Id = Guid.NewGuid(), BusinessId = _current.BusinessId, ProductId = product.Id,
            BatchReference = string.IsNullOrWhiteSpace(request.BatchReference) ? $"RCV-{Guid.NewGuid():N}"[..16].ToUpperInvariant() : request.BatchReference.Trim(),
            Supplier = string.IsNullOrWhiteSpace(request.Supplier) ? null : request.Supplier.Trim(),
            ReceivedQuantity = request.Quantity, RemainingQuantity = request.Quantity,
            UnitCost = decimal.Round(request.UnitCost, 2), ExpiryDate = request.ExpiryDate,
            ReceivedAtUtc = DateTimeOffset.UtcNow
        };
        _db.StockBatches.Add(batch);
        _db.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(), BusinessId = _current.BusinessId, ProductId = product.Id,
            StockBatchId = batch.Id, MovementType = "Receipt", QuantityChange = request.Quantity,
            ActorUserId = _current.UserId, Reason = "Stock received"
        });
        AddAudit("StockReceived", "StockBatch", batch.Id, new { product.Name, request.Quantity, request.UnitCost, request.ExpiryDate });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Created($"/api/stock/batches/{batch.Id}", new { batch.Id, productId = product.Id, productName = product.Name, batch.ReceivedQuantity, batch.RemainingQuantity, batch.UnitCost, batch.ExpiryDate, batch.BatchReference });
    }

    [HttpGet("stock/batches")]
    public async Task<IActionResult> StockBatches([FromQuery] Guid? productId, CancellationToken ct)
    {
        var query = _db.StockBatches.AsNoTracking().Where(x => x.RemainingQuantity > 0);
        if (productId.HasValue) query = query.Where(x => x.ProductId == productId.Value);
        var batches = await query.OrderBy(x => x.Product.Name).ThenBy(x => x.ExpiryDate ?? DateOnly.MaxValue)
            .Select(x => new
            {
                x.Id, x.ProductId, ProductName = x.Product.Name, x.Product.Sku,
                x.BatchReference, x.RemainingQuantity, x.UnitCost, x.ExpiryDate
            }).ToListAsync(ct);
        return Ok(batches);
    }

    [HttpGet("stock/expiry")]
    public async Task<IActionResult> Expiry([FromQuery] int days = 7, CancellationToken ct = default)
    {
        days = Math.Clamp(days, 1, 90);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cutoff = today.AddDays(days);
        var batches = await _db.StockBatches.AsNoTracking()
            .Where(x => x.RemainingQuantity > 0 && x.ExpiryDate != null && x.ExpiryDate <= cutoff)
            .OrderBy(x => x.ExpiryDate).ThenBy(x => x.Product.Name)
            .Select(x => new
            {
                x.Id, x.ProductId, ProductName = x.Product.Name, x.Product.Sku,
                x.BatchReference, x.RemainingQuantity, x.UnitCost, x.ExpiryDate,
                Status = x.ExpiryDate < today ? "Expired" : "ExpiringSoon"
            }).ToListAsync(ct);
        return Ok(batches);
    }

    [HttpPost("stock/waste")]
    [Authorize(Policy = ShopPulsePolicies.CanAdjustStock)]
    public async Task<IActionResult> RecordWaste([FromBody] WasteRequest request, CancellationToken ct)
    {
        var allowedReasons = new[] { "Expired", "Damaged", "Missing", "Spoiled", "Other" };
        if (request.ProductId == Guid.Empty || request.StockBatchId == Guid.Empty || request.Quantity <= 0 || !allowedReasons.Contains(request.Reason))
            return Failure(400, "VALIDATION_FAILED", "Select a batch, enter a positive quantity and choose a valid waste reason.");

        var batch = await _db.StockBatches.Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == request.StockBatchId && x.ProductId == request.ProductId, ct);
        if (batch is null) return NotFound(new { code = "BATCH_NOT_FOUND", message = "The selected stock batch could not be found." });
        if (batch.RemainingQuantity < request.Quantity)
            return Conflict(new { code = "INSUFFICIENT_STOCK", available = batch.RemainingQuantity, requested = request.Quantity, message = $"Only {batch.RemainingQuantity} units remain in this batch." });

        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        batch.RemainingQuantity -= request.Quantity;
        batch.Version++;
        var waste = new WasteRecord
        {
            Id = Guid.NewGuid(), BusinessId = _current.BusinessId, ProductId = request.ProductId,
            StockBatchId = batch.Id, Quantity = request.Quantity, Reason = request.Reason,
            Notes = request.Notes?.Trim(), EstimatedCostValue = batch.UnitCost * request.Quantity,
            ActorUserId = _current.UserId
        };
        _db.WasteRecords.Add(waste);
        _db.StockMovements.Add(new StockMovement
        {
            Id = Guid.NewGuid(), BusinessId = _current.BusinessId, ProductId = request.ProductId,
            StockBatchId = batch.Id, MovementType = "Waste", QuantityChange = -request.Quantity,
            ReferenceId = waste.Id, Reason = request.Reason, ActorUserId = _current.UserId
        });
        AddAudit("WasteRecorded", "WasteRecord", waste.Id, new { request.Reason, request.Quantity, batch.Product.Name });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Ok(new { waste.Id, productName = batch.Product.Name, request.Quantity, request.Reason, waste.EstimatedCostValue, batch.RemainingQuantity });
    }

    [HttpGet("cash-shifts/current")]
    public async Task<IActionResult> CurrentShift(CancellationToken ct)
    {
        var shift = await _db.CashShifts.AsNoTracking()
            .Where(x => x.OperatorUserId == _current.UserId && x.Status == "Open")
            .OrderByDescending(x => x.OpenedAtUtc).FirstOrDefaultAsync(ct);
        return Ok(shift is null ? null : new { shift.Id, shift.OpeningFloat, shift.OpenedAtUtc, shift.Status });
    }

    [HttpPost("cash-shifts/open")]
    public async Task<IActionResult> OpenShift([FromBody] OpenCashShiftRequest request, CancellationToken ct)
    {
        if (request.OpeningFloat < 0 || request.OpeningFloat > 10000000m)
            return Failure(400, "VALIDATION_FAILED", "Opening float must be zero or greater.");
        var existing = await _db.CashShifts.AnyAsync(x => x.OperatorUserId == _current.UserId && x.Status == "Open", ct);
        if (existing) return Conflict(new { code = "SHIFT_ALREADY_OPEN", message = "You already have an open cash shift." });
        var shift = new CashShift
        {
            Id = Guid.NewGuid(), BusinessId = _current.BusinessId, OperatorUserId = _current.UserId,
            OpeningFloat = decimal.Round(request.OpeningFloat, 2), Status = "Open", OpenedAtUtc = DateTimeOffset.UtcNow
        };
        _db.CashShifts.Add(shift);
        AddAudit("CashShiftOpened", "CashShift", shift.Id, new { shift.OpeningFloat });
        await _db.SaveChangesAsync(ct);
        return Created($"/api/cash-shifts/{shift.Id}", new { shift.Id, shift.OpeningFloat, shift.OpenedAtUtc, shift.Status });
    }

    [HttpPost("cash-shifts/close")]
    [Authorize(Policy = ShopPulsePolicies.CloseCashShift)]
    public async Task<IActionResult> CloseShift([FromBody] CloseCashShiftRequest request, CancellationToken ct)
    {
        if (request.ActualCash < 0 || request.ActualCash > 100000000m)
            return Failure(400, "VALIDATION_FAILED", "Enter a valid counted cash amount.");
        var shift = await _db.CashShifts
            .Where(x => x.Status == "Open")
            .OrderByDescending(x => x.OpenedAtUtc).FirstOrDefaultAsync(ct);
        if (shift is null) return NotFound(new { code = "NO_OPEN_SHIFT", message = "There is no open cash shift to close." });

        var cashSales = await _db.Sales.Where(x => x.OperatorUserId == shift.OperatorUserId
                && x.PaymentMethod == "Cash" && x.Status == "Completed" && x.CreatedAtUtc >= shift.OpenedAtUtc)
            .SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0m;
        var expected = shift.OpeningFloat + cashSales;
        shift.ExpectedCash = expected;
        shift.ActualCash = decimal.Round(request.ActualCash, 2);
        shift.Variance = shift.ActualCash - expected;
        shift.VarianceReason = request.VarianceReason?.Trim();
        shift.ClosedAtUtc = DateTimeOffset.UtcNow;
        shift.Status = "Closed";
        AddAudit("CashShiftClosed", "CashShift", shift.Id, new { shift.ExpectedCash, shift.ActualCash, shift.Variance, shift.VarianceReason });
        await _db.SaveChangesAsync(ct);
        return Ok(new { shift.Id, shift.OpeningFloat, shift.ExpectedCash, shift.ActualCash, shift.Variance, shift.VarianceReason, shift.ClosedAtUtc, shift.Status });
    }

    [HttpGet("reports/summary")]
    [Authorize(Policy = ShopPulsePolicies.ViewFinancialReports)]
    public async Task<IActionResult> ReportSummary([FromQuery] DateOnly? date, CancellationToken ct)
    {
        var selected = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = new DateTimeOffset(selected.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
        var end = start.AddDays(1);
        var sales = _db.Sales.Where(x => x.CreatedAtUtc >= start && x.CreatedAtUtc < end && x.Status == "Completed");
        var count = await sales.CountAsync(ct);
        var total = await sales.SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0m;
        var cash = await sales.Where(x => x.PaymentMethod == "Cash").SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0m;
        var card = await sales.Where(x => x.PaymentMethod == "Card").SumAsync(x => (decimal?)x.GrandTotal, ct) ?? 0m;
        var waste = await _db.WasteRecords.Where(x => x.CreatedAtUtc >= start && x.CreatedAtUtc < end)
            .SumAsync(x => (decimal?)x.EstimatedCostValue, ct) ?? 0m;
        return Ok(new { date = selected, salesCount = count, salesTotal = total, cashTotal = cash, cardTotal = card, wasteCost = waste, currency = "ZAR" });
    }

    private void AddAudit(string action, string entityType, Guid entityId, object details)
    {
        _db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(), BusinessId = _current.BusinessId, ActorUserId = _current.UserId,
            Action = action, EntityType = entityType, EntityId = entityId,
            DetailsJson = JsonSerializer.Serialize(details), CreatedAtUtc = DateTimeOffset.UtcNow
        });
    }

    private ObjectResult Failure(int status, string code, string message)
        => Problem(statusCode: status, title: "Request could not be completed", detail: message,
            extensions: new Dictionary<string, object?> { ["code"] = code, ["correlationId"] = HttpContext.TraceIdentifier });
}
