using System.Data.Common;
using EFactura.Application.Common.Persistence;
using EFactura.Domain.Payments;
using EFactura.Domain.Receivables;
using EFactura.Domain.Sales;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class FinancePersistenceTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Settlement = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Payment_method_round_trips_enabled_and_version_evidence(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var methodId = Guid.NewGuid();
        await using (var context = database.CreateContext())
        {
            var repository = new EfPaymentMethodRepository(context);
            var method = PaymentMethod.Create(methodId, "company-1", "Card");
            await repository.AddAsync(method);
            await new EfUnitOfWork(context).SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var repository = new EfPaymentMethodRepository(context);
            var method = await repository.GetAsync("company-1", methodId);
            Assert.NotNull(method);
            Assert.True(method!.Enabled);
            Assert.Equal(1, method.Version);

            method.SetEnabled(false, 1);
            await repository.SaveAsync(method);
            await new EfUnitOfWork(context).SaveChangesAsync();
        }

        await using var verification = database.CreateContext();
        var persisted = await new EfPaymentMethodRepository(verification).GetAsync("company-1", methodId);
        Assert.NotNull(persisted);
        Assert.False(persisted!.Enabled);
        Assert.Equal(2, persisted.Version);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Sale_payment_round_trips_plan_and_method_version_snapshot(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database, includeCustomer: false);
        var paymentId = Guid.NewGuid();
        await using (var context = database.CreateContext())
        {
            var payment = Payment.CreateFromSale(
                paymentId, "company-1", seed.SaleId, 1, seed.PaymentMethodId, 1,
                250m, "UYU", "POS-001", Confirmation, Settlement, DateTimeOffset.UtcNow);
            await new EfPaymentRepository(context).AddAsync(payment);
            await new EfUnitOfWork(context).SaveChangesAsync();
        }

        await using var verification = database.CreateContext();
        var persisted = await new EfPaymentRepository(verification).GetAsync("company-1", paymentId);
        Assert.NotNull(persisted);
        Assert.Equal(seed.SaleId, persisted!.SaleId);
        Assert.Equal(1, persisted.Sequence);
        Assert.Equal(seed.PaymentMethodId, persisted.PaymentMethodId);
        Assert.Equal(1, persisted.PaymentMethodVersion);
        Assert.Equal(250m, persisted.Amount);
        Assert.Equal(Settlement, persisted.SettlementFingerprint);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Sale_receivable_round_trips_original_obligation_without_stored_balance(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database, includeCustomer: true);
        var receivableId = Guid.NewGuid();
        await using (var context = database.CreateContext())
        {
            var receivable = Receivable.CreateFromSale(
                receivableId, "company-1", seed.CustomerPartyId!.Value, seed.SaleId,
                500m, "UYU", new DateOnly(2026, 9, 4), new DateOnly(2026, 10, 4),
                Confirmation, Settlement, DateTimeOffset.UtcNow);
            await new EfReceivableRepository(context).AddAsync(receivable);
            await new EfUnitOfWork(context).SaveChangesAsync();
        }

        await using var verification = database.CreateContext();
        var persisted = await new EfReceivableRepository(verification).GetBySaleAsync("company-1", seed.SaleId);
        Assert.NotNull(persisted);
        Assert.Equal(receivableId, persisted!.Id);
        Assert.Equal(500m, persisted.OriginalAmount);
        Assert.Equal(new DateOnly(2026, 10, 4), persisted.DueDate);
        Assert.Equal(1, persisted.Version);
        Assert.Equal(Settlement, persisted.SettlementFingerprint);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Payment_and_receivable_rows_roll_back_together_after_flush_failure(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database, includeCustomer: true);
        await using (var context = database.CreateContext())
        {
            var transactions = new EfTransactionManager(context);
            var unitOfWork = new EfUnitOfWork(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => transactions.ExecuteAsync(async ct =>
            {
                await new EfPaymentRepository(context).AddAsync(Payment.CreateFromSale(
                    Guid.NewGuid(), "company-1", seed.SaleId, 1, seed.PaymentMethodId, 1,
                    100m, "UYU", null, Confirmation, Settlement, DateTimeOffset.UtcNow), ct);
                await new EfReceivableRepository(context).AddAsync(Receivable.CreateFromSale(
                    Guid.NewGuid(), "company-1", seed.CustomerPartyId!.Value, seed.SaleId,
                    400m, "UYU", new DateOnly(2026, 9, 4), new DateOnly(2026, 10, 4),
                    Confirmation, Settlement, DateTimeOffset.UtcNow), ct);

                await unitOfWork.SaveChangesAsync(ct);
                throw new InvalidOperationException("Injected failure after financial rows were flushed.");
            }));
        }

        await using var verification = database.CreateContext();
        Assert.Equal(0, await verification.Set<V1PaymentRecord>().CountAsync());
        Assert.Equal(0, await verification.Set<V1ReceivableRecord>().CountAsync());
        Assert.Equal(1, await verification.Set<V1PaymentMethodRecord>().CountAsync());
        Assert.Equal(1, await verification.Sales.CountAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Database_rejects_second_base_receivable_for_same_sale(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var seed = await SeedAsync(database, includeCustomer: true);
        await using (var context = database.CreateContext())
        {
            var repository = new EfReceivableRepository(context);
            await repository.AddAsync(Receivable.CreateFromSale(
                Guid.NewGuid(), "company-1", seed.CustomerPartyId!.Value, seed.SaleId,
                500m, "UYU", new DateOnly(2026, 9, 4), new DateOnly(2026, 10, 4),
                Confirmation, Settlement, DateTimeOffset.UtcNow));
            await repository.AddAsync(Receivable.CreateFromSale(
                Guid.NewGuid(), "company-1", seed.CustomerPartyId.Value, seed.SaleId,
                500m, "UYU", new DateOnly(2026, 9, 4), new DateOnly(2026, 10, 4),
                Confirmation, "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc",
                DateTimeOffset.UtcNow));

            await Assert.ThrowsAsync<DbUpdateException>(() => new EfUnitOfWork(context).SaveChangesAsync());
        }

        await using var verification = database.CreateContext();
        Assert.Equal(0, await verification.Set<V1ReceivableRecord>().CountAsync());
    }

    private static async Task<Seed> SeedAsync(TestDatabase database, bool includeCustomer)
    {
        var saleId = Guid.NewGuid();
        var methodId = Guid.NewGuid();
        Guid? customerId = includeCustomer ? Guid.NewGuid() : null;
        var now = DateTimeOffset.UtcNow;

        await using var context = database.CreateContext();
        if (customerId.HasValue)
        {
            context.Parties.Add(new V1PartyRecord
            {
                Id = customerId.Value,
                OrganizationId = "company-1",
                Name = "Customer",
                ResidenceCountry = "UY",
                TaxResidenceCountry = "UY",
                Active = true,
                Version = 1,
                CreatedAtUtc = now.UtcDateTime,
                UpdatedAtUtc = now.UtcDateTime
            });
        }

        context.Sales.Add(new V1SaleRecord
        {
            Id = saleId,
            OrganizationId = "company-1",
            LocationId = "loc-1",
            TerminalId = "term-1",
            CustomerPartyId = customerId,
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

        await new EfPaymentMethodRepository(context).AddAsync(PaymentMethod.Create(methodId, "company-1", "Card"));
        await new EfUnitOfWork(context).SaveChangesAsync();
        return new Seed(saleId, methodId, customerId);
    }

    private sealed record Seed(Guid SaleId, Guid PaymentMethodId, Guid? CustomerPartyId);

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
            connectionBuilder["Database"] = $"ef_fin_{provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";
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
