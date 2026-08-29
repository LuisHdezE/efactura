using System.Data.Common;
using EFactura.Application.Catalog;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Security;
using EFactura.Application.Parties;
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

public sealed class SalesDraftPersistenceTests
{
    [Fact]
    public async Task Release1_UI_projection_accepts_UYI_without_inventing_quotes_for_other_currencies()
    {
        var converter = new Release1UiAmountConverter();
        var effectiveOn = new DateOnly(2026, 8, 29);

        Assert.Equal(5000m, await converter.TryConvertToUiAsync("UYI", 5000m, effectiveOn));
        Assert.Null(await converter.TryConvertToUiAsync("UYU", 5000m, effectiveOn));
        Assert.Null(await converter.TryConvertToUiAsync("USD", 5000m, effectiveOn));
        Assert.Null(await converter.TryConvertToUiAsync("UI", 5000m, effectiveOn));
    }

    [Fact]
    public void Editing_a_validated_sale_invalidates_the_prior_validation_fingerprint()
    {
        var itemId = Guid.NewGuid();
        var sale = Sale.Create(
            Guid.NewGuid(),
            "company-1",
            null,
            null,
            null,
            SaleCommercialIntent.ConsumerFinal,
            "UYU",
            new DateOnly(2026, 8, 29),
            "UY",
            false,
            new[] { ProductLine(itemId, 2m, 100m) });

        sale.MarkValidated("fingerprint-1", DateTimeOffset.UtcNow, 1);
        Assert.Equal(SaleStatus.Validated, sale.Status);
        Assert.Equal("fingerprint-1", sale.ValidationFingerprint);

        sale.ReplaceDraft(
            null,
            SaleCommercialIntent.ConsumerFinal,
            "UYU",
            new DateOnly(2026, 8, 29),
            "UY",
            false,
            new[] { ProductLine(itemId, 3m, 100m) },
            expectedVersion: 2);

        Assert.Equal(SaleStatus.Draft, sale.Status);
        Assert.Null(sale.ValidationFingerprint);
        Assert.Null(sale.ValidatedAtUtc);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Failure_after_sale_flush_rolls_back_sale_lines_and_atomic_companions(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        Guid itemId;
        await using (var setup = database.CreateContext())
        {
            var item = CommercialItem.Create(
                Guid.NewGuid(), "company-1", "PROD-ROLLBACK", "Rollback product", null,
                CommercialItemKind.Product, "UNIT", true, null, null);
            itemId = item.Id;
            await new EfCommercialItemRepository(setup).AddAsync(item);
            await new EfUnitOfWork(setup).SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var transactions = new EfTransactionManager(context);
            var unitOfWork = new EfUnitOfWork(context);
            var sales = new EfSaleRepository(context);
            var audit = new EfAuditWriter(context);
            var outbox = new EfOutboxWriter(context);
            var idempotency = new EfIdempotencyStore(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => transactions.ExecuteAsync(async ct =>
            {
                var reservation = await idempotency.TryReserveAsync(
                    new IdempotencyReservation(
                        "sales.create:company-1",
                        "sale-rollback-key",
                        "sale-rollback-hash",
                        "actor-1",
                        "corr-sale-rollback",
                        DateTimeOffset.UtcNow.AddMinutes(10)), ct);
                Assert.Equal(IdempotencyReservationStatus.Acquired, reservation.Status);
                await unitOfWork.SaveChangesAsync(ct);

                var sale = Sale.Create(
                    Guid.NewGuid(),
                    "company-1",
                    "loc-1",
                    "term-1",
                    null,
                    SaleCommercialIntent.ConsumerFinal,
                    "UYU",
                    new DateOnly(2026, 8, 29),
                    "UY",
                    false,
                    new[] { ProductLine(itemId, 1m, 500m) });

                await sales.AddAsync(sale, ct);
                await audit.AppendAsync(
                    new AuditEvent(
                        Guid.NewGuid(), DateTimeOffset.UtcNow, "SALE_DRAFT_CREATED", "actor-1",
                        "company-1", "loc-1", "term-1", "Sale", sale.Id.ToString(),
                        AuditOutcome.Succeeded, "corr-sale-rollback", null,
                        new Dictionary<string, string?>()), ct);
                await outbox.EnqueueAsync(
                    new TestSaleEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, sale.Id),
                    new OutboxContext("corr-sale-rollback", OrganizationId: "company-1", ActorId: "actor-1"), ct);

                await unitOfWork.SaveChangesAsync(ct);
                throw new InvalidOperationException("Injected failure after sales rows were flushed.");
            }));
        }

        await using var verification = database.CreateContext();
        Assert.Equal(0, await verification.Sales.CountAsync());
        Assert.Equal(0, await verification.SaleLines.CountAsync());
        Assert.Equal(0, await verification.AuditEvents.CountAsync());
        Assert.Equal(0, await verification.OutboxMessages.CountAsync());
        Assert.Equal(0, await verification.IdempotencyRecords.CountAsync());
        Assert.Equal(1, await verification.CommercialItems.CountAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Create_sale_use_case_commits_sale_audit_outbox_and_idempotency_together(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        Guid itemId;
        await using (var setup = database.CreateContext())
        {
            var item = CommercialItem.Create(
                Guid.NewGuid(), "company-1", "PROD-001", "Product 001", null,
                CommercialItemKind.Product, "UNIT", true, null, null);
            itemId = item.Id;
            await new EfCommercialItemRepository(setup).AddAsync(item);
            await new EfUnitOfWork(setup).SaveChangesAsync();
        }

        Guid saleId;
        await using (var context = database.CreateContext())
        {
            var actor = Actor(Permissions.SalesCreate);
            var correlation = new FixedCorrelationContextAccessor(
                new CorrelationContext("corr-sale-create", "trace-sale-create"));
            var useCase = new CreateSaleUseCase(
                new EfSaleRepository(context),
                new SaleDraftBuilder(new EfCommercialItemRepository(context), new EfPartyRepository(context)),
                new EfTransactionManager(context),
                new EfUnitOfWork(context),
                new EfIdempotencyStore(context),
                new EfAuditWriter(context),
                new EfOutboxWriter(context),
                actor,
                correlation);

            var result = await useCase.ExecuteAsync(new CreateSaleCommand(
                "company-1",
                "loc-1",
                "term-1",
                null,
                SaleCommercialIntent.ConsumerFinal,
                "UYU",
                new DateOnly(2026, 8, 29),
                "UY",
                false,
                new[] { new SaleLineInput(itemId, 2m, 125m) },
                "sale-create-key",
                "sale-create-hash"));

            Assert.False(result.Replayed);
            saleId = result.SaleId;
        }

        await using var verification = database.CreateContext();
        var sale = await verification.Sales.Include(x => x.Lines).SingleAsync();
        Assert.Equal(saleId, sale.Id);
        Assert.Single(sale.Lines);
        Assert.Equal(1, await verification.AuditEvents.CountAsync());
        Assert.Equal(1, await verification.OutboxMessages.CountAsync());
        Assert.Equal(1, await verification.IdempotencyRecords.CountAsync());
        Assert.Equal(1, (await verification.IdempotencyRecords.SingleAsync()).State);
    }

    private static SaleLine ProductLine(Guid itemId, decimal quantity, decimal unitPrice) =>
        SaleLine.Create(
            Guid.NewGuid(), itemId, "PROD", "Product", SaleLineKind.Product,
            quantity, unitPrice, null);

    private static FixedActorContextAccessor Actor(params string[] permissions) =>
        new(new ActorContext(
            "actor-1",
            "Sales Integration Tester",
            true,
            new HashSet<string>(permissions, StringComparer.Ordinal),
            new HashSet<string>(new[] { "company-1" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "loc-1" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "term-1" }, StringComparer.Ordinal),
            null));

    private sealed record TestSaleEvent(
        Guid EventId,
        DateTimeOffset OccurredAt,
        Guid SaleId) : IIntegrationEvent;

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
            connectionBuilder["Database"] = $"efactura_sales_{provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";
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
