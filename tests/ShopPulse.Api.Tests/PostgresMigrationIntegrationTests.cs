using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ShopPulse.Api.Auth;
using ShopPulse.Api.Domain;
using Npgsql;
using ShopPulse.Api.Data;
using Xunit;

namespace ShopPulse.Api.Tests;

/// <summary>
/// Runs against the PostgreSQL service container in CI. A private schema keeps
/// this test isolated from any other test data and makes cleanup deterministic.
/// </summary>
public sealed class PostgresMigrationIntegrationTests
{
    [Fact]
    public async Task ProductionModelMatchesMigrations_AndMigrationsApplyToPostgres()
    {
        var connectionString = Environment.GetEnvironmentVariable("SHOPPULSE_TEST_POSTGRES_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString))
            return; // Local SQLite-only runs remain possible; CI always supplies PostgreSQL.

        var schema = $"ci_{Guid.NewGuid():N}";
        var schemaConnection = new NpgsqlConnectionStringBuilder(connectionString)
        {
            SearchPath = schema,
            Pooling = false
        };

        await using (var admin = new NpgsqlConnection(connectionString))
        {
            await admin.OpenAsync();
            await using var create = admin.CreateCommand();
            create.CommandText = $"""CREATE SCHEMA "{schema}";""";
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(schemaConnection.ConnectionString)
                .Options;

            await using var db = new AppDbContext(options, new NullCurrentBusiness());

            Assert.False(
                db.Database.HasPendingModelChanges(),
                "The EF Core model has changes that are not represented by a migration. Add and review a migration before deployment.");

            await db.Database.MigrateAsync();
            Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            Assert.True(await db.Database.CanConnectAsync());

            // Exercise refresh rotation using two independent request scopes/DbContexts.
            var businessId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var familyId = Guid.NewGuid();
            var rawRefreshToken = Guid.NewGuid().ToString("N");
            db.Businesses.Add(new Business { Id = businessId, Name = "Refresh concurrency test" });
            db.Users.Add(new AppUser
            {
                Id = userId,
                BusinessId = businessId,
                Email = $"refresh-{userId:N}@example.test",
                PasswordHash = "not-used",
                DisplayName = "Refresh Test User",
                Role = "Owner",
                IsActive = true
            });
            db.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FamilyId = familyId,
                TokenHash = AuthService.Hash(rawRefreshToken),
                CreatedAtUtc = DateTimeOffset.UtcNow,
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(7),
                FamilyAbsoluteExpiryUtc = DateTimeOffset.UtcNow.AddDays(30)
            });
            await db.SaveChangesAsync();

            var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:SigningKey"] = "CI-only-signing-key-with-at-least-32-bytes",
                    ["Jwt:Issuer"] = "shoppulse-tests",
                    ["Jwt:Audience"] = "shoppulse-tests",
                    ["RefreshToken:AbsoluteDays"] = "30",
                    ["Jwt:AccessTokenMinutes"] = "15"
                }).Build();

            await using var requestDb1 = new AppDbContext(options, new NullCurrentBusiness());
            await using var requestDb2 = new AppDbContext(options, new NullCurrentBusiness());
            var auth1 = new AuthService(requestDb1, configuration);
            var auth2 = new AuthService(requestDb2, configuration);

            var rotations = await Task.WhenAll(
                auth1.RefreshAsync(rawRefreshToken, "127.0.0.1", "ci", CancellationToken.None),
                auth2.RefreshAsync(rawRefreshToken, "127.0.0.1", "ci", CancellationToken.None));

            Assert.Equal(1, rotations.Count(result => result.Ok));

            await using var verificationDb = new AppDbContext(options, new NullCurrentBusiness());
            var familyTokens = await verificationDb.RefreshTokens
                .IgnoreQueryFilters()
                .Where(token => token.FamilyId == familyId)
                .ToListAsync();

            Assert.Equal(2, familyTokens.Count); // Original plus at most one successor.
            Assert.InRange(
                familyTokens.Count(token => token.RevokedAtUtc == null),
                0,
                1); // Reuse detection may revoke the winner's successor; duplicates are never active.
        }
        finally
        {
            await using var admin = new NpgsqlConnection(connectionString);
            await admin.OpenAsync();
            await using var drop = admin.CreateCommand();
            drop.CommandText = $"""DROP SCHEMA IF EXISTS "{schema}" CASCADE;""";
            await drop.ExecuteNonQueryAsync();
        }
    }
}
