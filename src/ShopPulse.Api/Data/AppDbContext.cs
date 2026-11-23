using Microsoft.EntityFrameworkCore;
using ShopPulse.Api.Domain;

namespace ShopPulse.Api.Data;

public class AppDbContext : DbContext
{
    private readonly ICurrentBusiness? _currentBusiness;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentBusiness? currentBusiness = null)
        : base(options)
    {
        _currentBusiness = currentBusiness;
    }

    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<StockBatch> StockBatches => Set<StockBatch>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<WasteRecord> WasteRecords => Set<WasteRecord>();
    public DbSet<CashShift> CashShifts => Set<CashShift>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    // Always apply tenant filters. EF caches the model, so conditionally adding
    // filters based on the first request's scoped tenant would leak data across tenants.
    private Guid CurrentBusinessId => _currentBusiness?.BusinessId ?? Guid.Empty;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>().HasQueryFilter(u => u.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<Product>().HasQueryFilter(p => p.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<StockBatch>().HasQueryFilter(b => b.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<StockMovement>().HasQueryFilter(m => m.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<Sale>().HasQueryFilter(s => s.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<WasteRecord>().HasQueryFilter(w => w.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<CashShift>().HasQueryFilter(c => c.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<CashMovement>().HasQueryFilter(c => c.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<AuditEvent>().HasQueryFilter(a => a.BusinessId == CurrentBusinessId);
        modelBuilder.Entity<IdempotencyRecord>().HasQueryFilter(i => i.BusinessId == CurrentBusinessId);

        modelBuilder.Entity<Business>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(256).IsRequired();
            e.Property(x => x.Role).HasMaxLength(32).IsRequired();
            e.HasOne(x => x.Business).WithMany(b => b.Users).HasForeignKey(x => x.BusinessId);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.BusinessId, x.Sku }).IsUnique();
            e.Property(x => x.Sku).HasMaxLength(64).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.UnitPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<StockBatch>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitCost).HasPrecision(18, 2);
            e.HasOne(x => x.Product).WithMany(p => p.Batches).HasForeignKey(x => x.ProductId);
            e.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<Sale>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.BusinessId, x.IdempotencyKey }).IsUnique()
                .HasFilter("\"IdempotencyKey\" IS NOT NULL");
            e.Property(x => x.Subtotal).HasPrecision(18, 2);
            e.Property(x => x.Discount).HasPrecision(18, 2);
            e.Property(x => x.GrandTotal).HasPrecision(18, 2);
            e.Property(x => x.AmountTendered).HasPrecision(18, 2);
            e.Property(x => x.ChangeAmount).HasPrecision(18, 2);
            e.Property(x => x.ReceiptNumber).HasMaxLength(32);
            e.Property(x => x.RefundAmount).HasPrecision(18, 2);
            e.Property(x => x.RefundReason).HasMaxLength(500);
            e.Property(x => x.Version).IsConcurrencyToken();
        });

        modelBuilder.Entity<SaleItem>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UnitPriceSnapshot).HasPrecision(18, 2);
            e.Property(x => x.LineTotal).HasPrecision(18, 2);
            e.HasOne(x => x.Sale).WithMany(s => s.Items).HasForeignKey(x => x.SaleId);
        });

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => x.FamilyId);
            e.HasIndex(x => new { x.UserId, x.RevokedAtUtc });
            e.Property(x => x.TokenHash).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<IdempotencyRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.BusinessId, x.Key }).IsUnique();
            e.Property(x => x.Key).HasMaxLength(128).IsRequired();
        });

        modelBuilder.Entity<AuditEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.BusinessId, x.CreatedAtUtc });
        });

        modelBuilder.Entity<WasteRecord>(e =>
        {
            e.Property(x => x.EstimatedCostValue).HasPrecision(18, 2);
        });

        modelBuilder.Entity<CashShift>(e =>
        {
            e.Property(x => x.OpeningFloat).HasPrecision(18, 2);
            e.Property(x => x.ActualCash).HasPrecision(18, 2);
            e.Property(x => x.ExpectedCash).HasPrecision(18, 2);
            e.Property(x => x.Variance).HasPrecision(18, 2);
        });

        modelBuilder.Entity<CashMovement>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Direction).HasMaxLength(8).IsRequired();
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            e.HasIndex(x => new { x.BusinessId, x.CashShiftId, x.CreatedAtUtc });
            e.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne<CashShift>().WithMany().HasForeignKey(x => x.CashShiftId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CashMovement>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.Property(x => x.Direction).HasMaxLength(8).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(500).IsRequired();
            e.HasIndex(x => new { x.BusinessId, x.CashShiftId, x.CreatedAtUtc });
        });
    }
}

public interface ICurrentBusiness
{
    Guid BusinessId { get; }
    string Role { get; }
    Guid UserId { get; }
}

public class CurrentBusiness : ICurrentBusiness
{
    public Guid BusinessId { get; }
    public string Role { get; }
    public Guid UserId { get; }

    public CurrentBusiness(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User
            ?? throw new UnauthorizedAccessException("No authenticated user.");

        var rawBid = user.FindFirst("business_id")?.Value
            ?? throw new UnauthorizedAccessException("Missing business_id claim.");
        var rawUid = user.FindFirst("sub")?.Value
            ?? user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? throw new UnauthorizedAccessException("Missing sub claim.");

        BusinessId = Guid.Parse(rawBid);
        UserId = Guid.Parse(rawUid);
        Role = user.FindFirst("role")?.Value ?? "Operator";
    }
}

public class NullCurrentBusiness : ICurrentBusiness
{
    public Guid BusinessId => Guid.Empty;
    public string Role => "";
    public Guid UserId => Guid.Empty;
}
