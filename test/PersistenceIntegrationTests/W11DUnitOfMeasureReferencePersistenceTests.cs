using System.Data.Common;
using EFactura.Domain.Catalog;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class W11DUnitOfMeasureReferencePersistenceTests
{
    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Unit_reference_reader_returns_sorted_distinct_active_units_with_actor_scope_isolation(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
        {
            return;
        }

        await using (var context = database.CreateContext())
        {
            var repository = new EfCommercialItemRepository(context);

            await repository.AddAsync(Item("company-a", "A-1", "UNIT"));
            await repository.AddAsync(Item("company-a", "A-2", "KG"));
            await repository.AddAsync(Item("company-a", "A-3", "KG"));
            await repository.AddAsync(Item("company-b", "B-1", "BOX"));

            var inactive = Item("company-a", "A-4", "OLD");
            inactive.Deactivate(inactive.Version);
            await repository.AddAsync(inactive);

            await context.SaveChangesAsync();

            Assert.Equal(
                new[] { "KG", "UNIT" },
                await repository.ListActiveDistinctUnitsAsync(new[] { "company-a" }));

            Assert.Equal(
                new[] { "BOX", "KG", "UNIT" },
                await repository.ListActiveDistinctUnitsAsync(new[] { "company-a", "company-b" }));

            Assert.Empty(await repository.ListActiveDistinctUnitsAsync(Array.Empty<string>()));
        }
    }

    private static CommercialItem Item(string organizationId, string code, string unit) =>
        CommercialItem.Create(
            Guid.NewGuid(),
            organizationId,
            code,
            code,
            null,
            CommercialItemKind.Product,
            unit,
            false,
            null,
            null);

    private sealed class TestDatabase : IAsyncDisposable
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
                {
                    throw new InvalidOperationException($"Required integration test connection variable {variable} is missing.");
                }

                return null;
            }

            var connectionBuilder = new DbConnectionStringBuilder { ConnectionString = baseConnectionString };
            connectionBuilder["Database"] = $"efactura_w11d_{provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";

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
}
