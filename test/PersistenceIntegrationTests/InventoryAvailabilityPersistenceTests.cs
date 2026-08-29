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
using EFactura.Domain.Inventory;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class InventoryAvailabilityPersistenceTests
{
    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task First_adjustment_commits_position_movement_audit_outbox_and_idempotency_together(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var itemId = await SeedItemAsync(database, "STOCK-001", trackInventory: true);

        StockAdjustmentResult result;
        await using (var context = database.CreateContext())
        {
            var useCase = AdjustmentUseCase(context);
            result = await useCase.ExecuteAsync(new CreateStockAdjustmentCommand(
                "company-1", itemId, "loc-1", 15m, "INITIAL_COUNT", "Opening physical count",
                0, "inv-adjust-1", "hash-inv-adjust-1"));
        }

        Assert.False(result.Replayed);
        Assert.Equal(15m, result.Quantity);

        await using var verification = database.CreateContext();
        var position = await verification.InventoryPositions.SingleAsync();
        var movement = await verification.StockMovements.SingleAsync();
        Assert.Equal(result.PositionId, position.Id);
        Assert.Equal(result.MovementId, movement.Id);
        Assert.Equal(15m, position.Quantity);
        Assert.Equal(0m, movement.QuantityBefore);
        Assert.Equal(15m, movement.QuantityDelta);
        Assert.Equal(15m, movement.QuantityAfter);
        Assert.Equal(1, await verification.AuditEvents.CountAsync());
        Assert.Equal("inventory.adjustment.posted", (await verification.AuditEvents.SingleAsync()).EventName);
        Assert.Equal(1, await verification.OutboxMessages.CountAsync());
        Assert.Equal(1, await verification.IdempotencyRecords.CountAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Stale_adjustment_creates_no_second_movement_or_success_evidence(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var itemId = await SeedItemAsync(database, "STOCK-STALE", trackInventory: true);
        long currentVersion;
        await using (var context = database.CreateContext())
        {
            var first = await AdjustmentUseCase(context).ExecuteAsync(new CreateStockAdjustmentCommand(
                "company-1", itemId, "loc-1", 10m, "INITIAL_COUNT", null,
                0, "inv-stale-1", "hash-stale-1"));
            currentVersion = first.Version;
        }

        await using (var context = database.CreateContext())
        {
            var exception = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
                AdjustmentUseCase(context).ExecuteAsync(new CreateStockAdjustmentCommand(
                    "company-1", itemId, "loc-1", -2m, "COUNT_CORRECTION", "stale writer",
                    currentVersion - 1, "inv-stale-2", "hash-stale-2")));
            Assert.Equal("concurrency_conflict", exception.Code);
            Assert.Equal("stale_version", exception.ConflictType);
        }

        await using var verification = database.CreateContext();
        Assert.Equal(10m, (await verification.InventoryPositions.SingleAsync()).Quantity);
        Assert.Equal(1, await verification.StockMovements.CountAsync());
        Assert.Equal(1, await verification.AuditEvents.CountAsync());
        Assert.Equal(1, await verification.OutboxMessages.CountAsync());
        Assert.Equal(1, await verification.IdempotencyRecords.CountAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Failure_after_inventory_flush_rolls_back_position_movement_and_atomic_companions(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var itemId = await SeedItemAsync(database, "STOCK-ROLLBACK", trackInventory: true);

        await using (var context = database.CreateContext())
        {
            var transactions = new EfTransactionManager(context);
            var unitOfWork = new EfUnitOfWork(context);
            var inventory = new EfInventoryRepository(context);
            var audit = new EfAuditWriter(context);
            var outbox = new EfOutboxWriter(context);
            var idempotency = new EfIdempotencyStore(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => transactions.ExecuteAsync(async ct =>
            {
                var reservation = await idempotency.TryReserveAsync(
                    new IdempotencyReservation(
                        $"inventory.adjust:company-1:loc-1:{itemId}",
                        "inv-rollback-key", "inv-rollback-hash", "actor-1", "corr-inv-rollback",
                        DateTimeOffset.UtcNow.AddMinutes(10)), ct);
                Assert.Equal(IdempotencyReservationStatus.Acquired, reservation.Status);
                await unitOfWork.SaveChangesAsync(ct);

                var position = InventoryPosition.Create(Guid.NewGuid(), "company-1", itemId, "loc-1");
                var movement = position.ApplyAdjustment(9m, "PHYSICAL_COUNT", null, DateTimeOffset.UtcNow, 1);
                await inventory.AddPositionAsync(position, ct);
                await inventory.AddMovementAsync(movement, ct);
                await audit.AppendAsync(new AuditEvent(
                    Guid.NewGuid(), DateTimeOffset.UtcNow, "inventory.adjustment.posted", "actor-1",
                    "company-1", "loc-1", null, "InventoryPosition", position.Id.ToString(),
                    AuditOutcome.Succeeded, "corr-inv-rollback", null,
                    new Dictionary<string, string?>()), ct);
                await outbox.EnqueueAsync(
                    new TestInventoryEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, position.Id),
                    new OutboxContext("corr-inv-rollback", OrganizationId: "company-1", ActorId: "actor-1"), ct);
                await unitOfWork.SaveChangesAsync(ct);

                throw new InvalidOperationException("Injected failure after inventory rows were flushed.");
            }));
        }

        await using var verification = database.CreateContext();
        Assert.Equal(0, await verification.InventoryPositions.CountAsync());
        Assert.Equal(0, await verification.StockMovements.CountAsync());
        Assert.Equal(0, await verification.AuditEvents.CountAsync());
        Assert.Equal(0, await verification.OutboxMessages.CountAsync());
        Assert.Equal(0, await verification.IdempotencyRecords.CountAsync());
        Assert.Equal(1, await verification.CommercialItems.CountAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Availability_uses_TrackInventory_and_aggregates_duplicate_item_requirements(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var trackedId = await SeedItemAsync(database, "TRACKED", trackInventory: true);
        var untrackedId = await SeedItemAsync(database, "UNTRACKED", trackInventory: false);

        await using (var context = database.CreateContext())
        {
            var position = InventoryPosition.Create(Guid.NewGuid(), "company-1", trackedId, "loc-1");
            position.ApplyAdjustment(5m, "INITIAL_COUNT", null, DateTimeOffset.UtcNow, 1);
            await new EfInventoryRepository(context).AddPositionAsync(position);
            await new EfUnitOfWork(context).SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var checker = new InventoryAvailabilityChecker(
                new EfCommercialItemRepository(context), new EfInventoryRepository(context));

            var sufficient = await checker.CheckAsync(
                "company-1", "loc-1",
                new[]
                {
                    new InventoryAvailabilityRequirement(trackedId, 2m),
                    new InventoryAvailabilityRequirement(trackedId, 3m),
                    new InventoryAvailabilityRequirement(untrackedId, 999m)
                });
            Assert.True(sufficient.Ready);
            Assert.Equal(2, sufficient.Lines.Count);
            Assert.Equal(5m, sufficient.Lines.Single(x => x.ItemId == trackedId).RequiredQuantity);
            Assert.False(sufficient.Lines.Single(x => x.ItemId == untrackedId).TracksInventory);

            var insufficient = await checker.CheckAsync(
                "company-1", "loc-1",
                new[] { new InventoryAvailabilityRequirement(trackedId, 6m) });
            Assert.False(insufficient.Ready);
            Assert.Contains("inventory.insufficient_stock", insufficient.Findings);
            Assert.Contains("inventory_availability_check", insufficient.Findings);
        }
    }

    private static CreateStockAdjustmentUseCase AdjustmentUseCase(V1PersistenceDbContext context) => new(
        new EfInventoryRepository(context),
        new EfCommercialItemRepository(context),
        new EfTransactionManager(context),
        new EfUnitOfWork(context),
        new EfIdempotencyStore(context),
        new EfAuditWriter(context),
        new EfOutboxWriter(context),
        Actor(Permissions.InventoryAdjust),
        new FixedCorrelationContextAccessor(new CorrelationContext("corr-inventory", "trace-inventory")));

    private static async Task<Guid> SeedItemAsync(TestDatabase database, string code, bool trackInventory)
    {
        await using var context = database.CreateContext();
        var item = CommercialItem.Create(
            Guid.NewGuid(), "company-1", code, code, null,
            CommercialItemKind.Product, "UNIT", trackInventory, null, null);
        await new EfCommercialItemRepository(context).AddAsync(item);
        await new EfUnitOfWork(context).SaveChangesAsync();
        return item.Id;
    }

    private static FixedActorContextAccessor Actor(params string[] permissions) =>
        new(new ActorContext(
            "actor-1",
            "Inventory Integration Tester",
            true,
            new HashSet<string>(permissions, StringComparer.Ordinal),
            new HashSet<string>(new[] { "company-1" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "loc-1" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "term-1" }, StringComparer.Ordinal),
            null));

    private sealed record TestInventoryEvent(
        Guid EventId,
        DateTimeOffset OccurredAt,
        Guid PositionId) : IIntegrationEvent;

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
                    throw new InvalidOperationException($"Required integration test connection variable {variable} is missing.");
                return null;
            }

            var connectionBuilder = new DbConnectionStringBuilder { ConnectionString = baseConnectionString };
            connectionBuilder["Database"] = $"efactura_inventory_{provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";
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
