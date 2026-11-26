using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShopPulse.Api.Auth;
using ShopPulse.Api.Controllers;
using ShopPulse.Api.Data;
using ShopPulse.Api.Domain;
using Xunit;

namespace ShopPulse.Api.Tests;

public sealed class OperatorAccountFlowTests
{
    [Fact]
    public async Task Owner_CreatesOperator_OperatorAppearsInTeam_AndCanSignIn()
    {
        var businessId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var tenant = new TestCurrentBusiness(businessId, ownerId);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (connection)
        await using (var db = new AppDbContext(options, tenant))
        {
            await db.Database.EnsureCreatedAsync();
            db.Businesses.Add(new Business { Id = businessId, Name = "Operator test shop" });
            db.Users.Add(new AppUser
            {
                Id = ownerId,
                BusinessId = businessId,
                DisplayName = "Test Owner",
                Email = "owner@operator-flow.test",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("OwnerPassword!123"),
                Role = "Owner",
                IsActive = true
            });
            await db.SaveChangesAsync();

            var controller = new StaffController(db);
            var http = new DefaultHttpContext();
            http.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", ownerId.ToString()),
                new Claim("business_id", businessId.ToString()),
                new Claim("role", "Owner")
            ], "test"));
            controller.ControllerContext = new ControllerContext { HttpContext = http };

            const string email = "cashier@operator-flow.test";
            const string password = "OperatorPassword!123";
            var created = await controller.Create(
                new StaffController.CreateStaffRequest(
                    "Test Cashier", email, password,
                    CanAdjustStock: false,
                    CanRefundSales: false,
                    CanManageProducts: false,
                    CanReceiveStock: false),
                CancellationToken.None);

            Assert.IsType<CreatedResult>(created);

            var teamResponse = Assert.IsType<OkObjectResult>(
                await controller.List(CancellationToken.None));
            var teamJson = JsonSerializer.Serialize(teamResponse.Value);
            Assert.Contains(email, teamJson, StringComparison.OrdinalIgnoreCase);

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:SigningKey"] = "test-only-signing-key-with-at-least-32-bytes",
                    ["Jwt:Issuer"] = "shoppulse-api",
                    ["Jwt:Audience"] = "shoppulse-app",
                    ["Jwt:AccessTokenMinutes"] = "15",
                    ["RefreshToken:CookieName"] = "sp_rt",
                    ["RefreshToken:CookiePath"] = "/api/auth",
                    ["RefreshToken:IdleDays"] = "7",
                    ["RefreshToken:AbsoluteDays"] = "30"
                })
                .Build();

            var auth = new AuthService(db, configuration);
            var (ok, error, login) = await auth.LoginAsync(
                email, password, "127.0.0.1", "operator-flow-test", CancellationToken.None);

            Assert.True(ok, error);
            Assert.NotNull(login);
            Assert.Equal(email, login!.User.Email);
            Assert.Equal("Operator", login.User.Role);
            Assert.False(login.User.CanAdjustStock);
            Assert.False(login.User.CanRefundSales);
            Assert.False(login.User.CanManageProducts);
            Assert.False(login.User.CanReceiveStock);
            Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
            Assert.False(string.IsNullOrWhiteSpace(login.RefreshTokenRaw));
        }
    }

    private sealed class TestCurrentBusiness(Guid businessId, Guid userId) : ICurrentBusiness
    {
        public Guid BusinessId { get; } = businessId;
        public Guid UserId { get; } = userId;
        public string Role => "Owner";
    }
}