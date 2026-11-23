using Microsoft.EntityFrameworkCore;
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
