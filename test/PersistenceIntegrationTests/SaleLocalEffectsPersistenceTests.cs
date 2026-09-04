using System.Data.Common;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Fiscal;
using EFactura.Application.Inventory;
using EFactura.Domain.Catalog;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Sales;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class SaleLocalEffectsPersistenceTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Settlement = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Sale_consumption_round_trips_quantity_version_and_source_evidence(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database);
        await using (var context = database.CreateContext())
        {
            var consumer = new SaleStockConsumer(new EfInventoryRepository(context));
            var result = await consumer.StageAsync(Request(seed, 3m));
            Assert.Single(result.Movements);
            await new EfUnitOfWork(context).SaveChangesAsync();
        }

        await using var verification = database.CreateContext();
        var position = await verification.InventoryPositions.SingleAsync();
        var movement = await verification.StockMovements.SingleAsync();
        Assert.Equal(7m, position.Quantity);
        Assert.Equal(6, position.Version);
        Assert.Equal((int)EFactura.Domain.Inventory.StockMovementKind.SaleConsumption, movement.Kind);
        Assert.Equal(-3m, movement.QuantityDelta);
        Assert.Equal(seed.SaleId, movement.SourceSaleId);
        Assert.Equal(Confirmation, movement.ConfirmationFingerprint);
        Assert.Equal(Settlement, movement.SettlementFingerprint);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Database_rejects_second_sale_consumption_for_same_sale_and_position(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database);
        await using (var context = database.CreateContext())
        {
            await new SaleStockConsumer(new EfInventoryRepository(context)).StageAsync(Request(seed, 2m));
            await new EfUnitOfWork(context).SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var repository = new EfInventoryRepository(context);
            var position = await repository.GetPositionAsync("company-1", seed.ItemId, "loc-1");
            Assert.NotNull(position);
            var movement = position!.ConsumeForSale(
                seed.SaleId, 1m, Confirmation, Settlement, DateTimeOffset.UtcNow, position.Version);
            await repository.AddMovementAsync(movement);
            await Assert.ThrowsAsync<DbUpdateException>(() => new EfUnitOfWork(context).SaveChangesAsync());
        }

        await using var verification = database.CreateContext();
        Assert.Equal(1, await verification.StockMovements.CountAsync());
        var persisted = await verification.InventoryPositions.SingleAsync();
        Assert.Equal(8m, persisted.Quantity);
        Assert.Equal(6, persisted.Version);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Fiscalization_request_round_trips_and_database_allows_only_one_work_item_per_sale(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database);
        var requestId = Guid.NewGuid();
        await using (var context = database.CreateContext())
        {
            var repository = new EfFiscalizationRequestRepository(context);
            await repository.AddAsync(CreateFiscalRequest(requestId, seed.SaleId));
            await new EfUnitOfWork(context).SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var persisted = await new EfFiscalizationRequestRepository(context).GetBySaleAsync("company-1", seed.SaleId);
            Assert.NotNull(persisted);
            Assert.Equal(requestId, persisted!.Id);
            Assert.Equal(CfeFamily.EFactura, persisted.CfeFamily);
            Assert.Equal(FiscalizationRequestStatus.Pending, persisted.Status);
            Assert.Equal(122m, persisted.TotalAmount);
            Assert.Equal(Confirmation, persisted.ConfirmationFingerprint);
            Assert.Equal(Settlement, persisted.SettlementFingerprint);
        }

        await using (var context = database.CreateContext())
        {
            await new EfFiscalizationRequestRepository(context).AddAsync(
                CreateFiscalRequest(Guid.NewGuid(), seed.SaleId));
            await Assert.ThrowsAsync<DbUpdateException>(() => new EfUnitOfWork(context).SaveChangesAsync());
        }

        await using var verification = database.CreateContext();
        Assert.Equal(1, await verification.Set<V1FiscalizationRequestRecord>().CountAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Stock_and_fiscalization_work_item_roll_back_together_after_post_flush_failure(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database);
        await using (var context = database.CreateContext())
        {
            var transactions = new EfTransactionManager(context);
            var unitOfWork = new EfUnitOfWork(context);
            var consumer = new SaleStockConsumer(new EfInventoryRepository(context));
            var fiscalization = new EfFiscalizationRequestRepository(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => transactions.ExecuteAsync(async ct =>
            {
                await consumer.StageAsync(Request(seed, 4m), ct);
                await fiscalization.AddAsync(CreateFiscalRequest(Guid.NewGuid(), seed.SaleId), ct);
                await unitOfWork.SaveChangesAsync(ct);
                throw new InvalidOperationException("Injected failure after local sale effects were flushed.");
            }));
        }

        await using var verification = database.CreateContext();
        var position = await verification.InventoryPositions.SingleAsync();
        Assert.Equal(10m, position.Quantity);
        Assert.Equal(5, position.Version);
        Assert.Equal(0, await verification.StockMovements.CountAsync());
        Assert.Equal(0, await verification.Set<V1FiscalizationRequestRecord>().CountAsync());
    }

    private static SaleStockConsumptionRequest Request(Seed seed, decimal quantity) =>
        new(
            "company-1",
            "loc-1",
            seed.SaleId,
            Confirmation,
            Settlement,
            new[]
            {
                new InventoryAvailabilityLineResult(seed.ItemId, true, quantity, 10m, 5L, true, null)
            });

    private static FiscalizationRequest CreateFiscalRequest(Guid requestId, Guid saleId) =>
        FiscalizationRequest.CreateFromSale(
            requestId,
            "company-1",
            saleId,
            "loc-1",
            "term-1",
            CfeFamily.EFactura,
            ReceiverIdentificationRequirement.Required,
            "25.2",
            Confirmation,
            Settlement,
            "UYU",
            100m,
            22m,
            122m,
            DateTimeOffset.UtcNow);

    private static async Task<Seed> SeedAsync(TestDatabase database)
    {
        var saleId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var positionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using var context = database.CreateContext();
        context.CommercialItems.Add(new V1CommercialItemRecord
        {
            Id = itemId,
            OrganizationId = "company-1",
            Code = $"ITEM-{itemId:N}"[..20],
            Name = "Tracked product",
            Kind = (int)CommercialItemKind.Product,
            Unit = "EA",
            TrackInventory = true,
            Active = true,
            Version = 1,
            CreatedAtUtc = now.UtcDateTime,
            UpdatedAtUtc = now.UtcDateTime
        });
        context.Sales.Add(new V1SaleRecord
        {
            Id = saleId,
            OrganizationId = "company-1",
            LocationId = "loc-1",
            TerminalId = "term-1",
            Intent = (int)SaleCommercialIntent.ConsumerFinal,
            CurrencyCode = "UYU",
            EffectiveOnUtc = new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc),
            DeliveryCountry = "UY",
            GoodsExportConfirmed = false,
            Status = (int)SaleStatus.Validated,
            ValidationFingerprint = Confirmation,
            ValidatedAtUtc = now,
            Version = 2,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        context.InventoryPositions.Add(new V1InventoryPositionRecord
        {
            Id = positionId,
            OrganizationId = "company-1",
            ItemId = itemId,
            LocationId = "loc-1",
            Quantity = 10m,
            Version = 5,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await new EfUnitOfWork(context).SaveChangesAsync();
        return new Seed(saleId, itemId, positionId);
    }

    private sealed record Seed(Guid SaleId, Guid ItemId, Guid PositionId);

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
            connectionBuilder["Database"] = $"ef_sale_effects_{provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";
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
