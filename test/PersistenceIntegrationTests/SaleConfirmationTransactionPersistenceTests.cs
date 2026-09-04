using System.Data.Common;
using EFactura.Application.Catalog;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Common.Security;
using EFactura.Application.Fiscal;
using EFactura.Application.Inventory;
using EFactura.Application.Sales;
using EFactura.Domain.Catalog;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Parties;
using EFactura.Domain.Payments;
using EFactura.Domain.Sales;
using EFactura.Domain.Taxation;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class SaleConfirmationTransactionPersistenceTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Immediate_payment_confirmation_commits_every_local_effect_and_replays_without_duplicates(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database);
        var command = Command(seed, new[] { new SaleImmediatePaymentIntent(seed.PaymentMethodId, 122m, "UYU", "POS-1") }, null, "confirm-pay-1");

        SaleConfirmationResult first;
        await using (var context = database.CreateContext())
        {
            first = await UseCase(context, seed).ExecuteAsync(command);
            Assert.False(first.Replayed);
            Assert.Equal(1, first.PaymentCount);
            Assert.Null(first.ReceivableId);
        }

        await AssertPersistedAsync(database, seed, expectedPayments: 1, expectedReceivables: 0);

        SaleConfirmationResult replay;
        await using (var context = database.CreateContext())
        {
            replay = await UseCase(context, seed).ExecuteAsync(command);
            Assert.True(replay.Replayed);
            Assert.Equal(first.ConfirmationFingerprint, replay.ConfirmationFingerprint);
            Assert.Equal(first.SettlementFingerprint, replay.SettlementFingerprint);
            Assert.Equal(first.FiscalizationRequestId, replay.FiscalizationRequestId);
        }

        await AssertPersistedAsync(database, seed, expectedPayments: 1, expectedReceivables: 0);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Completed_replay_rechecks_sale_location_scope(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database);
        var command = Command(
            seed,
            new[] { new SaleImmediatePaymentIntent(seed.PaymentMethodId, 122m, "UYU") },
            null,
            "scope-replay");

        await using (var context = database.CreateContext())
            await UseCase(context, seed).ExecuteAsync(command);

        await using (var context = database.CreateContext())
        {
            var error = await Assert.ThrowsAsync<EFactura.Application.Common.Errors.ApplicationProblemException>(() =>
                UseCase(context, seed, actor: ActorOutsideSaleScope()).ExecuteAsync(command));
            Assert.Equal("location_scope_denied", error.Code);
        }

        await AssertPersistedAsync(database, seed, expectedPayments: 1, expectedReceivables: 0);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Credit_confirmation_derives_and_commits_one_receivable_without_payment(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database);
        var command = Command(
            seed,
            Array.Empty<SaleImmediatePaymentIntent>(),
            new SaleCreditTerms(new DateOnly(2026, 10, 4)),
            "confirm-credit-1");

        await using (var context = database.CreateContext())
        {
            var result = await UseCase(context, seed).ExecuteAsync(command);
            Assert.False(result.Replayed);
            Assert.Equal(0, result.PaymentCount);
            Assert.NotNull(result.ReceivableId);
        }

        await AssertPersistedAsync(database, seed, expectedPayments: 0, expectedReceivables: 1);
        await using var verification = database.CreateContext();
        var receivable = await verification.Set<V1ReceivableRecord>().SingleAsync();
        Assert.Equal(122m, receivable.OriginalAmount);
        Assert.Equal(new DateTime(2026, 10, 4), receivable.DueDate);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Stale_sale_version_rolls_back_idempotency_and_creates_no_confirmation_effects(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database);
        var stale = Command(
            seed,
            new[] { new SaleImmediatePaymentIntent(seed.PaymentMethodId, 122m, "UYU") },
            null,
            "stale-confirm") with { ExpectedVersion = 1 };

        await using (var context = database.CreateContext())
        {
            var error = await Assert.ThrowsAsync<EFactura.Application.Common.Errors.ApplicationProblemException>(() =>
                UseCase(context, seed).ExecuteAsync(stale));
            Assert.Equal("concurrency.stale_version", error.Code);
        }

        await AssertNoConfirmationEffectsAsync(database, seed);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Failure_after_final_flush_rolls_back_sale_finance_stock_fiscalization_audit_outbox_and_idempotency(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database);
        await using (var context = database.CreateContext())
        {
            var failingUnitOfWork = new ThrowAfterSaveUnitOfWork(new EfUnitOfWork(context), throwAfterCall: 2);
            var useCase = UseCase(context, seed, failingUnitOfWork);
            var command = Command(
                seed,
                new[] { new SaleImmediatePaymentIntent(seed.PaymentMethodId, 122m, "UYU") },
                null,
                "rollback-confirm");

            await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(command));
        }

        await AssertNoConfirmationEffectsAsync(database, seed);
    }

    private static ConfirmSaleUseCase UseCase(
        V1PersistenceDbContext context,
        Seed seed,
        IUnitOfWork? unitOfWork = null,
        IActorContextAccessor? actor = null) =>
        new(
            new EfSaleRepository(context),
            new FixedEvidenceResolver(seed.ItemId),
            new SaleSettlementPlanner(),
            new EfPaymentMethodRepository(context),
            new EfPaymentRepository(context),
            new EfReceivableRepository(context),
            new SaleStockConsumer(new EfInventoryRepository(context)),
            new EfFiscalizationRequestRepository(context),
            new EfTransactionManager(context),
            unitOfWork ?? new EfUnitOfWork(context),
            new EfIdempotencyStore(context),
            new EfAuditWriter(context),
            new EfOutboxWriter(context),
            actor ?? Actor(),
            new FixedCorrelationContextAccessor(new CorrelationContext("corr-confirm", "trace-confirm")));

    private static ConfirmSaleCommand Command(
        Seed seed,
        IReadOnlyCollection<SaleImmediatePaymentIntent> payments,
        SaleCreditTerms? credit,
        string key) =>
        new(
            "company-1",
            seed.SaleId,
            2,
            payments,
            credit,
            "Operator confirmed the authoritative sale snapshot.",
            "integration-test",
            key,
            $"hash-{key}");

    private static async Task<Seed> SeedAsync(TestDatabase database)
    {
        var saleId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var paymentMethodId = Guid.NewGuid();
        var positionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        await using var context = database.CreateContext();
        var customer = Party.Create(
            customerId,
            "company-1",
            PartyKind.Organization,
            "Customer One",
            "UY",
            "UY",
            new[] { PartyRole.Customer });
        await new EfPartyRepository(context).AddAsync(customer);

        var item = CommercialItem.Create(
            itemId,
            "company-1",
            "CONF-ITEM",
            "Confirmation item",
            null,
            CommercialItemKind.Product,
            "UNIT",
            true,
            null,
            null);
        await new EfCommercialItemRepository(context).AddAsync(item);
        await new EfPaymentMethodRepository(context).AddAsync(
            PaymentMethod.Create(paymentMethodId, "company-1", "Card"));

        var sale = Sale.Create(
            saleId,
            "company-1",
            "loc-1",
            "term-1",
            customerId,
            SaleCommercialIntent.TaxpayerInvoice,
            "UYU",
            new DateOnly(2026, 9, 4),
            "UY",
            false,
            new[]
            {
                SaleLine.Create(
                    Guid.NewGuid(),
                    itemId,
                    "CONF-ITEM",
                    "Confirmation item",
                    SaleLineKind.Product,
                    1m,
                    100m,
                    null)
            });
        sale.MarkValidated("validated-sale-evidence", now, 1);
        await new EfSaleRepository(context).AddAsync(sale);

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
        return new Seed(saleId, itemId, customerId, paymentMethodId, positionId);
    }

    private static async Task AssertPersistedAsync(
        TestDatabase database,
        Seed seed,
        int expectedPayments,
        int expectedReceivables)
    {
        await using var verification = database.CreateContext();
        var sale = await verification.Sales.SingleAsync(x => x.Id == seed.SaleId);
        Assert.Equal((int)SaleStatus.Confirmed, sale.Status);
        Assert.Equal(3, sale.Version);
        Assert.NotNull(sale.ConfirmationFingerprint);
        Assert.NotNull(sale.SettlementFingerprint);
        Assert.NotNull(sale.ConfirmedAtUtc);

        var position = await verification.InventoryPositions.SingleAsync(x => x.Id == seed.PositionId);
        Assert.Equal(9m, position.Quantity);
        Assert.Equal(6, position.Version);
        Assert.Equal(1, await verification.StockMovements.CountAsync(x => x.SourceSaleId == seed.SaleId));
        Assert.Equal(expectedPayments, await verification.Set<V1PaymentRecord>().CountAsync(x => x.SaleId == seed.SaleId));
        Assert.Equal(expectedReceivables, await verification.Set<V1ReceivableRecord>().CountAsync(x => x.SaleId == seed.SaleId));
        Assert.Equal(1, await verification.Set<V1FiscalizationRequestRecord>().CountAsync(x => x.SaleId == seed.SaleId));
        Assert.Equal(1, await verification.AuditEvents.CountAsync(x => x.TargetId == seed.SaleId.ToString()));
        Assert.Equal(2, await verification.OutboxMessages.CountAsync());
        Assert.Equal(1, await verification.IdempotencyRecords.CountAsync());
    }

    private static async Task AssertNoConfirmationEffectsAsync(TestDatabase database, Seed seed)
    {
        await using var verification = database.CreateContext();
        var sale = await verification.Sales.SingleAsync(x => x.Id == seed.SaleId);
        Assert.Equal((int)SaleStatus.Validated, sale.Status);
        Assert.Equal(2, sale.Version);
        Assert.Null(sale.ConfirmationFingerprint);
        Assert.Null(sale.SettlementFingerprint);
        Assert.Null(sale.ConfirmedAtUtc);

        var position = await verification.InventoryPositions.SingleAsync(x => x.Id == seed.PositionId);
        Assert.Equal(10m, position.Quantity);
        Assert.Equal(5, position.Version);
        Assert.Equal(0, await verification.StockMovements.CountAsync(x => x.SourceSaleId == seed.SaleId));
        Assert.Equal(0, await verification.Set<V1PaymentRecord>().CountAsync(x => x.SaleId == seed.SaleId));
        Assert.Equal(0, await verification.Set<V1ReceivableRecord>().CountAsync(x => x.SaleId == seed.SaleId));
        Assert.Equal(0, await verification.Set<V1FiscalizationRequestRecord>().CountAsync(x => x.SaleId == seed.SaleId));
        Assert.Equal(0, await verification.AuditEvents.CountAsync(x => x.TargetId == seed.SaleId.ToString()));
        Assert.Equal(0, await verification.OutboxMessages.CountAsync());
        Assert.Equal(0, await verification.IdempotencyRecords.CountAsync());
    }

    private static FixedActorContextAccessor Actor() =>
        new(new ActorContext(
            "actor-1",
            "Confirmation Integration Tester",
            true,
            new HashSet<string>(new[] { Permissions.SalesConfirm }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "company-1" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "loc-1" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "term-1" }, StringComparer.Ordinal),
            null));

    private static FixedActorContextAccessor ActorOutsideSaleScope() =>
        new(new ActorContext(
            "actor-1",
            "Confirmation Integration Tester",
            true,
            new HashSet<string>(new[] { Permissions.SalesConfirm }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "company-1" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "loc-2" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "term-1" }, StringComparer.Ordinal),
            null));

    private sealed class FixedEvidenceResolver : ISaleConfirmationEvidenceResolver
    {
        private readonly Guid _itemId;

        public FixedEvidenceResolver(Guid itemId) => _itemId = itemId;

        public Task<SaleConfirmationPlan> PrepareAsync(Sale sale, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var evidence = new RegulatoryRuleEvidence(
                "TEST-CONFIRM-RULE",
                "Confirmation transaction integration evidence",
                "https://example.invalid/confirmation",
                "test",
                new DateOnly(2026, 1, 1));
            var selection = new CfeSelectionResult(
                CfeSelectionStatus.Selected,
                CfeFamily.EFactura,
                ReceiverIdentificationRequirement.Required,
                new[]
                {
                    new CfeCandidate(
                        CfeFamily.EFactura,
                        ReceiverIdentificationRequirement.Required,
                        new[] { "test.selected" })
                },
                new[] { "test.selected" },
                Array.Empty<string>(),
                new[] { evidence },
                "25.2");
            var arithmetic = new CfeArithmeticResult(
                "UYU",
                "25.2",
                "TEST-ARITH",
                new[]
                {
                    new CfeArithmeticLineResult(
                        sale.Lines.Single().Id,
                        100m,
                        VatLiabilityKind.VatDue,
                        VatRateKind.Basic,
                        22m,
                        new[] { evidence },
                        "TEST-RATE")
                },
                new CfeArithmeticTotals(100m, 0m, 100m, 0m, 0m, 22m, 22m, 122m),
                new[] { evidence });
            var inventory = new InventoryAvailabilityResult(
                true,
                new[]
                {
                    new InventoryAvailabilityLineResult(_itemId, true, 1m, 10m, 5, true, null)
                },
                Array.Empty<string>());

            return Task.FromResult(new SaleConfirmationPlan(
                sale.Id,
                sale.Version,
                sale.ValidationFingerprint!,
                Confirmation,
                selection,
                arithmetic,
                inventory));
        }
    }

    private sealed class ThrowAfterSaveUnitOfWork : IUnitOfWork
    {
        private readonly IUnitOfWork _inner;
        private readonly int _throwAfterCall;
        private int _calls;

        public ThrowAfterSaveUnitOfWork(IUnitOfWork inner, int throwAfterCall)
        {
            _inner = inner;
            _throwAfterCall = throwAfterCall;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var result = await _inner.SaveChangesAsync(cancellationToken);
            _calls++;
            if (_calls == _throwAfterCall)
                throw new InvalidOperationException("Injected failure after confirmation effects were flushed.");
            return result;
        }
    }

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

    private sealed record Seed(
        Guid SaleId,
        Guid ItemId,
        Guid CustomerId,
        Guid PaymentMethodId,
        Guid PositionId);

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
            connectionBuilder["Database"] = $"ef_confirm_{provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";
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
