using System.Data.Common;
using EFactura.Application.Catalog;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Security;
using EFactura.Application.Inventory;
using EFactura.Domain.Catalog;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class InventoryHardeningPersistenceTests
{
    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Missing_item_fails_availability_closed(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        await using var context = database.CreateContext();
        var checker = new InventoryAvailabilityChecker(
            new EfCommercialItemRepository(context),
            new EfInventoryRepository(context));

        var result = await checker.CheckAsync(
            "company-1",
            "loc-1",
            new[] { new InventoryAvailabilityRequirement(Guid.NewGuid(), 1m) });

        Assert.False(result.Ready);
        Assert.Single(result.Lines);
        Assert.False(result.Lines.Single().Sufficient);
        Assert.Contains("inventory.item_unavailable", result.Findings);
        Assert.Contains("inventory_availability_check", result.Findings);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Replay_returns_original_adjustment_snapshot_after_later_stock_changes(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var itemId = await SeedItemAsync(database, "REPLAY-STOCK");
        var firstCommand = new CreateStockAdjustmentCommand(
            "company-1", itemId, "loc-1", 10m, "INITIAL_COUNT", null,
            0, "inventory-replay-original", "hash-inventory-replay-original");

        StockAdjustmentResult original;
        await using (var context = database.CreateContext())
        {
            original = await AdjustmentUseCase(context).ExecuteAsync(firstCommand);
        }

        await using (var context = database.CreateContext())
        {
            var later = await AdjustmentUseCase(context).ExecuteAsync(new CreateStockAdjustmentCommand(
                "company-1", itemId, "loc-1", 5m, "COUNT_CORRECTION", "later adjustment",
                original.Version, "inventory-replay-later", "hash-inventory-replay-later"));
            Assert.Equal(15m, later.Quantity);
            Assert.True(later.Version > original.Version);
        }

        await using (var context = database.CreateContext())
        {
            var replay = await AdjustmentUseCase(context).ExecuteAsync(firstCommand);

            Assert.True(replay.Replayed);
            Assert.Equal(original.PositionId, replay.PositionId);
            Assert.Equal(original.MovementId, replay.MovementId);
            Assert.Equal(original.Version, replay.Version);
            Assert.Equal(original.Quantity, replay.Quantity);
            Assert.Equal(10m, replay.Quantity);
        }
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Duplicate_authoritative_position_is_translated_to_portable_concurrency_conflict(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var itemId = await SeedItemAsync(database, "DUPLICATE-POSITION");

        await using (var context = database.CreateContext())
        {
            context.InventoryPositions.Add(Position(Guid.NewGuid(), itemId));
            await new EfUnitOfWork(context).SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            context.InventoryPositions.Add(Position(Guid.NewGuid(), itemId));
            var exception = await Assert.ThrowsAsync<ApplicationProblemException>(
                () => new EfUnitOfWork(context).SaveChangesAsync());

            Assert.Equal(ApplicationProblemKind.Conflict, exception.Kind);
            Assert.Equal("concurrency_conflict", exception.Code);
            Assert.Equal("duplicate_position", exception.ConflictType);
        }

        await using var verification = database.CreateContext();
        Assert.Equal(1, await verification.InventoryPositions.CountAsync());
    }

    private static V1InventoryPositionRecord Position(Guid id, Guid itemId) => new()
    {
        Id = id,
        OrganizationId = "company-1",
        ItemId = itemId,
        LocationId = "loc-1",
        Quantity = 0m,
        Version = 1,
        CreatedAtUtc = DateTimeOffset.UtcNow,
        UpdatedAtUtc = DateTimeOffset.UtcNow
    };

    private static CreateStockAdjustmentUseCase AdjustmentUseCase(V1PersistenceDbContext context) => new(
        new EfInventoryRepository(context),
        new EfCommercialItemRepository(context),
        new EfTransactionManager(context),
        new EfUnitOfWork(context),
        new EfIdempotencyStore(context),
        new EfAuditWriter(context),
        new EfOutboxWriter(context),
        Actor(Permissions.InventoryAdjust),
        new FixedCorrelationContextAccessor(new CorrelationContext("corr-inventory-hardening", "trace-inventory-hardening")));

    private static async Task<Guid> SeedItemAsync(TestDatabase database, string code)
    {
        await using var context = database.CreateContext();
        var item = CommercialItem.Create(
            Guid.NewGuid(), "company-1", code, code, null,
            CommercialItemKind.Product, "UNIT", true, null, null);
        await new EfCommercialItemRepository(context).AddAsync(item);
        await new EfUnitOfWork(context).SaveChangesAsync();
        return item.Id;
    }

    private static FixedActorContextAccessor Actor(params string[] permissions) =>
        new(new ActorContext(
            "actor-1",
            "Inventory Hardening Tester",
            true,
            new HashSet<string>(permissions, StringComparer.Ordinal),
            new HashSet<string>(new[] { "company-1" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "loc-1" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "term-1" }, StringComparer.Ordinal),
            null));

    private sealed class FixedActorContextAccessor : IActorContextAccessor
    {
        public FixedActorContextAccessor(ActorContext current) => Current = current;
        public ActorContext Current { get; }
    }

    private sealed class FixedCorrelationContextAccessor : ICorrelationContextAccessor
    {
        public FixedCorrelationContextAccessor(CorrelationContext current) => Current = current;
        public CorrelationContext Current { get; }
    }

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
                    throw new InvalidOperationException(
                        $"Required integration test connection variable {variable} is missing.");
                }

                return null;
            }

            var connectionBuilder = new DbConnectionStringBuilder { ConnectionString = baseConnectionString };
            connectionBuilder["Database"] =
                $"efactura_inventory_hardening_{provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";
            var optionsBuilder = new DbContextOptionsBuilder<V1PersistenceDbContext>();
            V1PersistenceDatabaseConfigurator.Configure(
                optionsBuilder, provider, connectionBuilder.ConnectionString);

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
