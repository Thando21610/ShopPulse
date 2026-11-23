using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopPulse.Api.Domain;

// ─── Multi-tenant root ───────────────────────────────────────────────────────
public class Business
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Address { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public bool IsActive { get; set; } = true;

    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
    public ICollection<Product> Products { get; set; } = new List<Product>();
}

// ─── Identity-backed user (extends for claims we need) ───────────────────────
public class AppUser
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Business Business { get; set; } = default!;

    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string Role { get; set; } = "Operator"; // Owner | Operator
    public bool IsActive { get; set; } = true;
    public bool CanAdjustStock { get; set; }
    public bool CanRefundSales { get; set; }
    public bool CanManageProducts { get; set; }
    public bool CanReceiveStock { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public int FailedLoginCount { get; set; }
    public DateTimeOffset? LockoutEndUtc { get; set; }
}

// ─── Products & stock ────────────────────────────────────────────────────────
public class Product
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Sku { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? Category { get; set; }
    [Column(TypeName = "numeric(18,2)")]
    public decimal UnitPrice { get; set; }
    public int MinimumStockLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public bool HasExpiry { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<StockBatch> Batches { get; set; } = new List<StockBatch>();
}

public class StockBatch
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = default!;
    public string? BatchReference { get; set; }
    public string? Supplier { get; set; }
    public int ReceivedQuantity { get; set; }
    public int RemainingQuantity { get; set; }
    [Column(TypeName = "numeric(18,2)")]
    public decimal UnitCost { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    // Optimistic concurrency token (works on SQLite + PostgreSQL)
    public int Version { get; set; } = 1;
}

public class StockMovement
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? StockBatchId { get; set; }
    public string MovementType { get; set; } = default!; // Receipt | Sale | Waste | Adjustment | Refund
    public int QuantityChange { get; set; } // + in, - out
    public Guid? ReferenceId { get; set; } // SaleId, WasteId, etc.
    public string? Reason { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

// ─── Sales ───────────────────────────────────────────────────────────────────
public class Sale
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string ReceiptNumber { get; set; } = default!;
    public Guid OperatorUserId { get; set; }
    [Column(TypeName = "numeric(18,2)")]
    public decimal Subtotal { get; set; }
    [Column(TypeName = "numeric(18,2)")]
    public decimal Discount { get; set; }
    [Column(TypeName = "numeric(18,2)")]
    public decimal GrandTotal { get; set; }
    public string PaymentMethod { get; set; } = "Cash"; // Cash | Card | Other
    [Column(TypeName = "numeric(18,2)")]
    public decimal AmountTendered { get; set; }
    [Column(TypeName = "numeric(18,2)")]
    public decimal ChangeAmount { get; set; }
    public string Status { get; set; } = "Completed"; // Completed | Refunded
    [Column(TypeName = "numeric(18,2)")]
    public decimal RefundAmount { get; set; }
    public DateTimeOffset? RefundedAtUtc { get; set; }
    public Guid? RefundedByUserId { get; set; }
    public string? RefundReason { get; set; }
    public string? RefundPaymentMethod { get; set; }
    public bool RefundRestocked { get; set; }
    public int Version { get; set; } = 1;
    public string? IdempotencyKey { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
}

public class SaleItem
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public Sale Sale { get; set; } = default!;
    public Guid ProductId { get; set; }
    public string ProductNameSnapshot { get; set; } = default!;
    public string SkuSnapshot { get; set; } = default!;
    public int Quantity { get; set; }
    [Column(TypeName = "numeric(18,2)")]
    public decimal UnitPriceSnapshot { get; set; }
    [Column(TypeName = "numeric(18,2)")]
    public decimal LineTotal { get; set; }
    public Guid? StockBatchId { get; set; } // FEFO allocation
}

// ─── Waste / adjustments ─────────────────────────────────────────────────────
public class WasteRecord
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? StockBatchId { get; set; }
    public int Quantity { get; set; }
    public string Reason { get; set; } = default!; // Expired | Damaged | Missing | Spoiled | Other
    public string? Notes { get; set; }
    [Column(TypeName = "numeric(18,2)")]
    public decimal EstimatedCostValue { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

// ─── Cash shifts ─────────────────────────────────────────────────────────────
public class CashShift
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid OperatorUserId { get; set; }
    [Column(TypeName = "numeric(18,2)")]
    public decimal OpeningFloat { get; set; }
    [Column(TypeName = "numeric(18,2)")]
    public decimal? ActualCash { get; set; }
    [Column(TypeName = "numeric(18,2)")]
    public decimal? ExpectedCash { get; set; }
    [Column(TypeName = "numeric(18,2)")]
    public decimal? Variance { get; set; }
    public string? VarianceReason { get; set; }
    public DateTimeOffset OpenedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public string Status { get; set; } = "Open"; // Open | Closed
    public int Version { get; set; } = 1;
}

// ─── Cash drawer movements ───────────────────────────────────────────────────
public class CashMovement
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid CashShiftId { get; set; }
    public Guid OperatorUserId { get; set; }
    public string Direction { get; set; } = "In"; // In | Out
    [Column(TypeName = "numeric(18,2)")]
    public decimal Amount { get; set; }
    public string Reason { get; set; } = default!;
    public Guid ActorUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

// ─── Auth ────────────────────────────────────────────────────────────────────
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid FamilyId { get; set; }
    public string TokenHash { get; set; } = default!; // SHA-256 base64
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? FamilyAbsoluteExpiryUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? RevokedReason { get; set; } // ROTATED | REUSE_DETECTED | LOGOUT | ADMIN
    public Guid? ReplacedByTokenId { get; set; }
    public string? CreatedByIp { get; set; }
    public string? UserAgent { get; set; }
}

public class IdempotencyRecord
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public string Key { get; set; } = default!;
    public string RequestHash { get; set; } = default!;
    public int StatusCode { get; set; }
    public string ResponseJson { get; set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
}

// ─── Audit ───────────────────────────────────────────────────────────────────
public class AuditEvent
{
    public Guid Id { get; set; }
    public Guid BusinessId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = default!;
    public string EntityType { get; set; } = default!;
    public Guid? EntityId { get; set; }
    public string? DetailsJson { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
