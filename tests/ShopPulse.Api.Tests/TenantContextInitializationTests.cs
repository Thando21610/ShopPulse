using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ShopPulse.Api.Data;
using ShopPulse.Api.Domain;
using Xunit;

namespace ShopPulse.Api.Tests;

public sealed class TenantContextInitializationTests
{
    [Fact]
    public async Task DbContextCreatedBeforeAuthentication_UsesBusinessClaimWhenQueriesRun()
    {
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var httpContext = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var tenant = new CurrentBusiness(accessor);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (connection)
        await using (var db = new AppDbContext(options, tenant))
        {
            await db.Database.EnsureCreatedAsync();

            db.Businesses.Add(new Business { Id = businessId, Name = "Tenant context test shop" });
            db.Users.Add(new AppUser
            {
                Id = ownerId,
                BusinessId = businessId,
                DisplayName = "Test Owner",
                Email = "owner@tenant-context.test",
                PasswordHash = "not-used",
                Role = "Owner",
                IsActive = true
            });
            db.Products.Add(new Product
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                Sku = "BREAD-001",
                Name = "Bread",
                UnitPrice = 18m,
                MinimumStockLevel = 5,
                HasExpiry = true,
                IsActive = true
            });
            await db.SaveChangesAsync();

            // Simulate JwtBearer OnTokenValidated creating the scoped DbContext
            // before the authentication middleware assigns HttpContext.User.
            Assert.Equal(Guid.Empty, tenant.BusinessId);

            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", ownerId.ToString()),
                new Claim("business_id", businessId.ToString()),
                new Claim("role", "Owner")
            ], "test"));

            Assert.Equal(businessId, tenant.BusinessId);
            Assert.Single(await db.Users.AsNoTracking().ToListAsync());
            Assert.Single(await db.Products.AsNoTracking().ToListAsync());
        }
    }
}
