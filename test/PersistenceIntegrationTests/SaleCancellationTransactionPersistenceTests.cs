using System.Data.Common;
using EFactura.Application.Catalog;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Common.Security;
using EFactura.Application.Sales;
using EFactura.Domain.Catalog;
using EFactura.Domain.Sales;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class SaleCancellationTransactionPersistenceTests
{
    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Validated_cancellation_round_trips_and_replay_creates_no_duplicates(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database, confirmed: false);
        var command = Command(seed.SaleId, expectedVersion: 2, key: "cancel-success");

        SaleCancellationResult first;
        await using (var context = database.CreateContext())
            first = await UseCase(context).ExecuteAsync(command);

        Assert.False(first.Replayed);
        Assert.Equal(SaleStatus.Cancelled, first.Status);
        Assert.Equal(3, first.Version);
        await AssertCancelledEvidenceAsync(database, seed.SaleId, expectedAudit: 1, expectedOutbox: 1, expectedIdempotency: 1);

        SaleCancellationResult replay;
        await using (var context = database.CreateContext())
            replay = await UseCase(context).ExecuteAsync(command);

        Assert.True(replay.Replayed);
        Assert.Equal(3, replay.Version);
        await AssertCancelledEvidenceAsync(database, seed.SaleId, expectedAudit: 1, expectedOutbox: 1, expectedIdempotency: 1);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Stale_cancellation_rolls_back_reservation_and_leaves_sale_unchanged(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database, confirmed: false);

        await using (var context = database.CreateContext())
        {
            var error = await Assert.ThrowsAsync<EFactura.Application.Common.Errors.ApplicationProblemException>(() =>
                UseCase(context).ExecuteAsync(Command(seed.SaleId, expectedVersion: 1, key: "cancel-stale")));
            Assert.Equal("concurrency.stale_version", error.Code);
            Assert.Equal("2", error.CurrentVersion);
        }

        await AssertNoCancellationEvidenceAsync(database, seed.SaleId, SaleStatus.Validated, version: 2);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Confirmed_sale_fails_at_irreversible_boundary_without_cancellation_residue(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database, confirmed: true);

        await using (var context = database.CreateContext())
        {
            var error = await Assert.ThrowsAsync<EFactura.Application.Common.Errors.ApplicationProblemException>(() =>
                UseCase(context).ExecuteAsync(Command(seed.SaleId, expectedVersion: 1, key: "cancel-confirmed")));
            Assert.Equal("sales.cancellation.irreversible_boundary_crossed", error.Code);
            Assert.Equal("irreversible_boundary", error.ConflictType);
            Assert.Equal("3", error.CurrentVersion);
        }

        await AssertNoCancellationEvidenceAsync(database, seed.SaleId, SaleStatus.Confirmed, version: 3);
    }

    private static CancelSaleUseCase UseCase(V1PersistenceDbContext context) => new(
        new EfSaleRepository(context),
        new EfTransactionManager(context),
        new EfUnitOfWork(context),
        new EfIdempotencyStore(context),
        new EfAuditWriter(context),
        new EfOutboxWriter(context),
        Actor(),
        new FixedCorrelationContextAccessor(new CorrelationContext("corr-cancel", "trace-cancel")));

    private static CancelSaleCommand Command(Guid saleId, long expectedVersion, string key) => new(
        "company-1",
        saleId,
        expectedVersion,
        "Customer requested cancellation before confirmation",
        "provider-real",
        key,
        $"hash-{key}");

    private static async Task<Seed> SeedAsync(TestDatabase database, bool confirmed)
    {
        var saleId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using var context = database.CreateContext();
        var item = CommercialItem.Create(
            itemId,
            "company-1",
            "CANCEL-ITEM",
            "Cancellation item",
            null,
            CommercialItemKind.Product,
            "UNIT",
            true,
            null,
            null);
        await new EfCommercialItemRepository(context).AddAsync(item);

        var sale = Sale.Create(
            saleId,
            "company-1",
            "loc-1",
            "term-1",
            null,
            SaleCommercialIntent.ConsumerFinal,
            "UYU",
            new DateOnly(2026, 9, 21),
            null,
            false,
            new[]
            {
                SaleLine.Create(
                    Guid.NewGuid(),
                    itemId,
                    "CANCEL-ITEM",
                    "Cancellation item",
                    SaleLineKind.Product,
                    1m,
                    100m,
                    null)
            });
        sale.MarkValidated("validated-cancellation-evidence", now.AddMinutes(-2), 1);
        if (confirmed)
        {
            sale.MarkConfirmed(
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                now.AddMinutes(-1),
                2);
        }

        await new EfSaleRepository(context).AddAsync(sale);
        await new EfUnitOfWork(context).SaveChangesAsync();
        return new Seed(saleId);
    }

    private static async Task AssertCancelledEvidenceAsync(
        TestDatabase database,
        Guid saleId,
        int expectedAudit,
        int expectedOutbox,
        int expectedIdempotency)
    {
        await using var verification = database.CreateContext();
        var sale = await verification.Sales.SingleAsync(x => x.Id == saleId);
        Assert.Equal((int)SaleStatus.Cancelled, sale.Status);
        Assert.Equal(3, sale.Version);
        Assert.Equal("validated-cancellation-evidence", sale.ValidationFingerprint);
        Assert.NotNull(sale.ValidatedAtUtc);
        Assert.Null(sale.ConfirmationFingerprint);
        Assert.Null(sale.SettlementFingerprint);
        Assert.Null(sale.ConfirmedAtUtc);
        Assert.Equal(expectedAudit, await verification.AuditEvents.CountAsync(x =>
            x.TargetId == saleId.ToString() && x.EventName == "SALE_CANCELLED"));
        Assert.Equal(expectedOutbox, await verification.OutboxMessages.CountAsync());
        Assert.Equal(expectedIdempotency, await verification.IdempotencyRecords.CountAsync());
    }

    private static async Task AssertNoCancellationEvidenceAsync(
        TestDatabase database,
        Guid saleId,
        SaleStatus expectedStatus,
        long version)
    {
        await using var verification = database.CreateContext();
        var sale = await verification.Sales.SingleAsync(x => x.Id == saleId);
        Assert.Equal((int)expectedStatus, sale.Status);
        Assert.Equal(version, sale.Version);
        Assert.Equal(0, await verification.AuditEvents.CountAsync(x =>
            x.TargetId == saleId.ToString() && x.EventName == "SALE_CANCELLED"));
        Assert.Equal(0, await verification.OutboxMessages.CountAsync());
        Assert.Equal(0, await verification.IdempotencyRecords.CountAsync());
    }

    private static FixedActorContextAccessor Actor() => new(new ActorContext(
        "actor-cancel",
        "Cancellation Integration Tester",
        true,
        new HashSet<string>(new[] { Permissions.SalesCancel }, StringComparer.Ordinal),
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

    private sealed record Seed(Guid SaleId);

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
            connectionBuilder["Database"] = $"ef_cancel_{provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";
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
