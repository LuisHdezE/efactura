using System.Data.Common;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Payables;
using EFactura.Domain.Payables;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class W24PayableBalancePersistenceTests
{
    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Party_scoped_projection_round_trips_payable_sources_effects_and_currency_buckets(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var partyId = Guid.NewGuid();
        var uyu = await SeedPayableAsync(
            database, "company-1", partyId, PayableSourceKind.PurchaseReceipt, "receipt-1",
            100m, "UYU", new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 23));
        var usd = await SeedPayableAsync(
            database, "company-1", partyId, PayableSourceKind.ReceivedFiscalDocument, "received-cfe-1",
            50m, "USD", new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 24));
        _ = await SeedPayableAsync(
            database, "company-2", Guid.NewGuid(), PayableSourceKind.PurchaseReceipt, "receipt-other-org",
            900m, "UYU", new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31));

        var asOf = new DateTimeOffset(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);
        await using (var context = database.CreateContext())
        {
            var ledger = new EfPayableBalanceRepository(context);
            var allocation = PayableBalanceEffect.CreateSupplierPaymentAllocation(
                Guid.NewGuid(), "company-1", uyu, 40m, "supplier-payment-1", 0, asOf.AddDays(-4));

            await ledger.AddAsync(PayableBalanceEffect.CreateAdjustment(
                Guid.NewGuid(), "company-1", uyu, 25m, true, "adjustment-plus", 0, asOf.AddDays(-5)));
            await ledger.AddAsync(allocation);
            await ledger.AddAsync(PayableBalanceEffect.CreateSupplierPaymentReversal(
                Guid.NewGuid(), "company-1", uyu, 40m, "supplier-payment-reversal-1", 0, allocation.Id, asOf.AddDays(-3)));
            await ledger.AddAsync(PayableBalanceEffect.CreateAdjustment(
                Guid.NewGuid(), "company-1", uyu, 5m, false, "adjustment-minus", 0, asOf.AddDays(-2)));
            await ledger.AddAsync(PayableBalanceEffect.CreateSupplierPaymentAllocation(
                Guid.NewGuid(), "company-1", usd, 50m, "supplier-payment-usd", 0, asOf.AddDays(-1)));

            await new EfUnitOfWork(context).SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var repository = new EfPayableBalanceRepository(context);
            var model = new PartyPayableAccountReadModel(repository);
            var result = await model.GetAsync("company-1", partyId, asOf);

            Assert.Equal(2, result.Currencies.Count);
            var uyuSummary = Assert.Single(result.Currencies, x => x.CurrencyCode == "UYU");
            Assert.Equal(120m, uyuSummary.Outstanding);
            Assert.Equal(120m, uyuSummary.Overdue);
            Assert.Equal(120m, uyuSummary.Aging.Days31To60);

            var usdSummary = Assert.Single(result.Currencies, x => x.CurrencyCode == "USD");
            Assert.Equal(0m, usdSummary.Outstanding);

            var sources = await repository.ListByPartyAsync("company-1", partyId);
            Assert.Equal(2, sources.Count);
            Assert.All(sources, x => Assert.Equal("company-1", x.OrganizationId));
            Assert.DoesNotContain(sources, x => x.OriginalAmount == 900m);
        }
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Database_rejects_duplicate_payable_for_same_accepted_source(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var partyId = Guid.NewGuid();
        await SeedPartyAsync(database, "company-1", partyId, DateTimeOffset.UtcNow);

        await using var context = database.CreateContext();
        var repository = new EfPayableRepository(context);
        var now = new DateTimeOffset(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

        await repository.AddAsync(Payable.CreateFromPurchaseReceipt(
            Guid.NewGuid(), "company-1", partyId, "receipt-duplicate", 100m, "UYU",
            new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1), now));
        await repository.AddAsync(Payable.CreateFromPurchaseReceipt(
            Guid.NewGuid(), "company-1", partyId, "receipt-duplicate", 125m, "UYU",
            new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1), now));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => new EfUnitOfWork(context).SaveChangesAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Database_rejects_second_reversal_of_same_supplier_payment_allocation(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var partyId = Guid.NewGuid();
        var payableId = await SeedPayableAsync(
            database, "company-1", partyId, PayableSourceKind.PurchaseReceipt, "receipt-reversal",
            100m, "UYU", new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1));
        var now = new DateTimeOffset(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

        Guid allocationId;
        await using (var context = database.CreateContext())
        {
            var ledger = new EfPayableBalanceRepository(context);
            var allocation = PayableBalanceEffect.CreateSupplierPaymentAllocation(
                Guid.NewGuid(), "company-1", payableId, 25m, "supplier-payment-1", 0, now.AddMinutes(-2));
            allocationId = allocation.Id;
            await ledger.AddAsync(allocation);
            await ledger.AddAsync(PayableBalanceEffect.CreateSupplierPaymentReversal(
                Guid.NewGuid(), "company-1", payableId, 25m, "supplier-payment-reversal-a", 0, allocationId, now.AddMinutes(-1)));
            await new EfUnitOfWork(context).SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var ledger = new EfPayableBalanceRepository(context);
            await ledger.AddAsync(PayableBalanceEffect.CreateSupplierPaymentReversal(
                Guid.NewGuid(), "company-1", payableId, 25m, "supplier-payment-reversal-b", 0, allocationId, now));

            await Assert.ThrowsAsync<DbUpdateException>(
                () => new EfUnitOfWork(context).SaveChangesAsync());
        }

        await using var verification = database.CreateContext();
        Assert.Equal(
            1,
            await verification.Set<V1PayableBalanceEffectRecord>()
                .CountAsync(x => x.ReversesEffectId == allocationId));
    }

    private static async Task<Guid> SeedPayableAsync(
        TestDatabase database,
        string organizationId,
        Guid partyId,
        PayableSourceKind sourceKind,
        string sourceId,
        decimal amount,
        string currency,
        DateOnly sourceEffectiveOn,
        DateOnly dueDate)
    {
        var createdAt = new DateTimeOffset(
            sourceEffectiveOn.Year,
            sourceEffectiveOn.Month,
            sourceEffectiveOn.Day,
            12,
            0,
            0,
            TimeSpan.Zero);
        await SeedPartyAsync(database, organizationId, partyId, createdAt);

        await using var context = database.CreateContext();
        var payable = sourceKind switch
        {
            PayableSourceKind.PurchaseReceipt => Payable.CreateFromPurchaseReceipt(
                Guid.NewGuid(), organizationId, partyId, sourceId, amount, currency,
                sourceEffectiveOn, dueDate, createdAt),
            PayableSourceKind.ReceivedFiscalDocument => Payable.CreateFromReceivedFiscalDocument(
                Guid.NewGuid(), organizationId, partyId, sourceId, amount, currency,
                sourceEffectiveOn, dueDate, createdAt),
            _ => throw new InvalidOperationException("Unsupported test payable source kind.")
        };

        await new EfPayableRepository(context).AddAsync(payable);
        await new EfUnitOfWork(context).SaveChangesAsync();
        return payable.Id;
    }

    private static async Task SeedPartyAsync(
        TestDatabase database,
        string organizationId,
        Guid partyId,
        DateTimeOffset createdAt)
    {
        await using var context = database.CreateContext();
        if (await context.Parties.AnyAsync(x => x.Id == partyId))
            return;

        context.Parties.Add(new V1PartyRecord
        {
            Id = partyId,
            OrganizationId = organizationId,
            Name = $"Party {partyId:N}",
            ResidenceCountry = "UY",
            TaxResidenceCountry = "UY",
            Active = true,
            Version = 1,
            CreatedAtUtc = createdAt.UtcDateTime,
            UpdatedAtUtc = createdAt.UtcDateTime
        });
        await new EfUnitOfWork(context).SaveChangesAsync();
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
                    throw new InvalidOperationException($"Required integration test connection variable {variable} is missing.");
                }

                return null;
            }

            var connectionBuilder = new DbConnectionStringBuilder { ConnectionString = baseConnectionString };
            connectionBuilder["Database"] = $"ef_w24_ap_{provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";
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
