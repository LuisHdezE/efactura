using System.Data.Common;
using EFactura.Application.Common.Context;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Write;
using Microsoft.EntityFrameworkCore;

namespace PersistenceIntegrationTests;

internal sealed class TestDatabase : IAsyncDisposable
{
    private readonly DbContextOptions<V1PersistenceDbContext> _options;

    private TestDatabase(DbContextOptions<V1PersistenceDbContext> options) => _options = options;

    public static async Task<TestDatabase?> CreateAsync(V1DatabaseProvider provider)
    {
        var variable = provider == V1DatabaseProvider.PostgreSql
            ? "POSTGRES_TEST_CONNECTION"
            : "MYSQL_TEST_CONNECTION";
        var baseConnectionString = Environment.GetEnvironmentVariable(variable);
        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            if (string.Equals(
                Environment.GetEnvironmentVariable("PERSISTENCE_INTEGRATION_REQUIRED"),
                "true",
                StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Required integration test connection variable {variable} is missing.");
            return null;
        }

        var connectionBuilder = new DbConnectionStringBuilder { ConnectionString = baseConnectionString };
        connectionBuilder["Database"] = $"ef_fiscal_identity_{provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";
        var optionsBuilder = new DbContextOptionsBuilder<V1PersistenceDbContext>();
        V1PersistenceDatabaseConfigurator.Configure(optionsBuilder, provider, connectionBuilder.ConnectionString);

        var database = new TestDatabase(optionsBuilder.Options);
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync();
        return database;
    }

    public V1PersistenceDbContext CreateContext() => new(_options);

    public async ValueTask DisposeAsync()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }
}

internal sealed class FixedCorrelationContextAccessor : ICorrelationContextAccessor
{
    public FixedCorrelationContextAccessor(CorrelationContext current) => Current = current;
    public CorrelationContext Current { get; }
}
