using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ShopPulse.Api.Data;

namespace ShopPulse.Api.Authorization;

public static class ShopPulsePolicies
{
    public const string OwnerOnly = "OwnerOnly";
    public const string Staff = "Staff";
    public const string CompleteSales = "CompleteSales";
    public const string ReceiveStock = "ReceiveStock";
    public const string CanAdjustStock = "CanAdjustStock";
    public const string CanRefundSales = "CanRefundSales";
    public const string CanManageProducts = "CanManageProducts";
    public const string ManageStaff = "ManageStaff";
    public const string ViewFinancialReports = "ViewFinancialReports";
    public const string CloseCashShift = "CloseCashShift";

    public static void AddPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(OwnerOnly, policy => policy.RequireRole("Owner"));
        options.AddPolicy(Staff, policy => policy.RequireRole("Owner", "Operator"));
        options.AddPolicy(CompleteSales, policy => policy.RequireRole("Owner", "Operator"));
        options.AddPolicy(ReceiveStock, policy => policy.RequireAuthenticatedUser().AddRequirements(new CanReceiveStockRequirement()));
        options.AddPolicy(CanAdjustStock, policy =>
            policy.RequireAuthenticatedUser().AddRequirements(new CanAdjustStockRequirement()));
        options.AddPolicy(CanRefundSales, policy =>
            policy.RequireAuthenticatedUser().AddRequirements(new CanRefundSalesRequirement()));
        options.AddPolicy(CanManageProducts, policy =>
            policy.RequireAuthenticatedUser().AddRequirements(new CanManageProductsRequirement()));
        options.AddPolicy(ManageStaff, policy => policy.RequireRole("Owner"));
        options.AddPolicy(ViewFinancialReports, policy => policy.RequireRole("Owner"));
        options.AddPolicy(CloseCashShift, policy => policy.RequireRole("Owner", "Operator"));
    }
}

public sealed class CanAdjustStockRequirement : IAuthorizationRequirement { }

public sealed class CanAdjustStockHandler : AuthorizationHandler<CanAdjustStockRequirement>
{
    private readonly AppDbContext _db;
    private readonly ICurrentBusiness _currentBusiness;

    public CanAdjustStockHandler(AppDbContext db, ICurrentBusiness currentBusiness)
    {
        _db = db;
        _currentBusiness = currentBusiness;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CanAdjustStockRequirement requirement)
    {
        if (context.User.IsInRole("Owner"))
        {
            context.Succeed(requirement);
            return;
        }

        if (!context.User.IsInRole("Operator")
            || _currentBusiness.BusinessId == Guid.Empty
            || _currentBusiness.UserId == Guid.Empty)
            return;

        // Read the permission from the tenant-scoped database on every authorization
        // decision. This avoids stale JWT claims continuing to grant a revoked permission.
        var allowed = await _db.Users.AsNoTracking().AnyAsync(user =>
            user.Id == _currentBusiness.UserId
            && user.BusinessId == _currentBusiness.BusinessId
            && user.IsActive
            && user.CanAdjustStock);

        if (allowed)
            context.Succeed(requirement);
    }
}

public sealed class CanRefundSalesRequirement : IAuthorizationRequirement { }

public sealed class CanRefundSalesHandler : AuthorizationHandler<CanRefundSalesRequirement>
{
    private readonly AppDbContext _db;
    private readonly ICurrentBusiness _currentBusiness;

    public CanRefundSalesHandler(AppDbContext db, ICurrentBusiness currentBusiness)
    {
        _db = db;
        _currentBusiness = currentBusiness;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CanRefundSalesRequirement requirement)
    {
        if (context.User.IsInRole("Owner"))
        {
            context.Succeed(requirement);
            return;
        }

        if (!context.User.IsInRole("Operator")
            || _currentBusiness.BusinessId == Guid.Empty
            || _currentBusiness.UserId == Guid.Empty)
            return;

        var allowed = await _db.Users.AsNoTracking().AnyAsync(user =>
            user.Id == _currentBusiness.UserId
            && user.BusinessId == _currentBusiness.BusinessId
            && user.IsActive
            && user.CanRefundSales);

        if (allowed)
            context.Succeed(requirement);
    }
}

public sealed class CanReceiveStockRequirement : IAuthorizationRequirement { }
public sealed class CanManageProductsRequirement : IAuthorizationRequirement { }

public sealed class CanReceiveStockHandler : AuthorizationHandler<CanReceiveStockRequirement>
{
    private readonly AppDbContext _db;
    private readonly ICurrentBusiness _current;
    public CanReceiveStockHandler(AppDbContext db, ICurrentBusiness current) { _db = db; _current = current; }
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanReceiveStockRequirement requirement)
    {
        if (context.User.IsInRole("Owner")) { context.Succeed(requirement); return; }
        if (!context.User.IsInRole("Operator") || _current.BusinessId == Guid.Empty || _current.UserId == Guid.Empty) return;
        if (await _db.Users.AsNoTracking().AnyAsync(x => x.Id == _current.UserId && x.BusinessId == _current.BusinessId && x.IsActive && x.CanReceiveStock)) context.Succeed(requirement);
    }
}

public sealed class CanManageProductsHandler : AuthorizationHandler<CanManageProductsRequirement>
{
    private readonly AppDbContext _db;
    private readonly ICurrentBusiness _current;
    public CanManageProductsHandler(AppDbContext db, ICurrentBusiness current) { _db = db; _current = current; }
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanManageProductsRequirement requirement)
    {
        if (context.User.IsInRole("Owner")) { context.Succeed(requirement); return; }
        if (!context.User.IsInRole("Operator") || _current.BusinessId == Guid.Empty || _current.UserId == Guid.Empty) return;
        if (await _db.Users.AsNoTracking().AnyAsync(x => x.Id == _current.UserId && x.BusinessId == _current.BusinessId && x.IsActive && x.CanManageProducts)) context.Succeed(requirement);
    }
}
