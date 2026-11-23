using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShopPulse.Api.Authorization;
using ShopPulse.Api.Data;
using ShopPulse.Api.Domain;
using Xunit;

namespace ShopPulse.Api.Tests;

public sealed class CanAdjustStockAuthorizationTests
{
    [Fact]
    public async Task Owner_CanAdjustStock_EvenWithoutOperatorFlag()
    {
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (provider, connection) = await CreateProviderAsync(businessId, userId, false, businessId);

        await using (connection)
        await using (provider)
        {
            var result = await AuthorizeAsync(provider, userId, "Owner");
            Assert.True(result.Succeeded);
        }
    }

    [Fact]
    public async Task Operator_WithPermission_CanAdjustStock()
    {
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (provider, connection) = await CreateProviderAsync(businessId, userId, true, businessId);

        await using (connection)
        await using (provider)
        {
            var result = await AuthorizeAsync(provider, userId, "Operator");
            Assert.True(result.Succeeded);
        }
    }

    [Fact]
    public async Task Operator_WithoutPermission_CannotAdjustStock()
    {
        var businessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (provider, connection) = await CreateProviderAsync(businessId, userId, false, businessId);

        await using (connection)
        await using (provider)
        {
            var result = await AuthorizeAsync(provider, userId, "Operator");
            Assert.False(result.Succeeded);
        }
    }

    [Fact]
    public async Task Operator_CannotUsePermissionFromAnotherBusiness()
    {
        var currentBusinessId = Guid.NewGuid();
        var otherBusinessId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (provider, connection) = await CreateProviderAsync(
            currentBusinessId, userId, true, otherBusinessId);

        await using (connection)
        await using (provider)
        {
            var result = await AuthorizeAsync(provider, userId, "Operator");
            Assert.False(result.Succeeded);
        }
    }

    private static async Task<AuthorizationResult> AuthorizeAsync(
        ServiceProvider provider, Guid userId, string role)
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        ], authenticationType: "test"));

        using var scope = provider.CreateScope();
        var authorization = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        return await authorization.AuthorizeAsync(
            principal, resource: null, ShopPulsePolicies.CanAdjustStock);
    }

    private static async Task<(ServiceProvider Provider, SqliteConnection Connection)> CreateProviderAsync(
        Guid currentBusinessId, Guid userId, bool canAdjustStock, Guid userBusinessId)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var currentBusiness = new TestCurrentBusiness(currentBusinessId, userId);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var db = new AppDbContext(options, currentBusiness))
        {
            await db.Database.EnsureCreatedAsync();
            db.Businesses.Add(new Business
            {
                Id = currentBusinessId,
                Name = "Current test business"
            });
            if (userBusinessId != currentBusinessId)
            {
                db.Businesses.Add(new Business
                {
                    Id = userBusinessId,
                    Name = "User test business"
                });
            }
            db.Users.Add(new AppUser
            {
                Id = userId,
                BusinessId = userBusinessId,
                Email = "operator@example.test",
                PasswordHash = "not-used-in-this-test",
                DisplayName = "Test Operator",
                Role = "Operator",
                CanAdjustStock = canAdjustStock,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(ShopPulsePolicies.AddPolicies);
        services.AddSingleton<ICurrentBusiness>(currentBusiness);
        services.AddScoped(_ => new AppDbContext(options, currentBusiness));
        services.AddScoped<IAuthorizationHandler, CanAdjustStockHandler>();

        return (services.BuildServiceProvider(), connection);
    }

    private sealed class TestCurrentBusiness(Guid businessId, Guid userId) : ICurrentBusiness
    {
        public Guid BusinessId { get; } = businessId;
        public Guid UserId { get; } = userId;
        public string Role => "Operator";
    }
}
