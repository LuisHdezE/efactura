using System.Data.Common;
using EFactura.Domain.Common;
using EFactura.Domain.Parties;
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

public sealed class W24ReceivableAccountReadModelPersistenceTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Settlement = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private static readonly DateOnly AsOf = new(2026, 9, 23);

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Party_projection_applies_adjustments_allocations_reversals_and_currency_isolation(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var customerId = Guid.NewGuid();
        var otherCustomerId = Guid.NewGuid();
        await SeedPartyAsync(database, "company-1", customerId);
        await SeedPartyAsync(database, "company-2", otherCustomerId);

        var uyu = await SeedReceivableAsync(database, "company-1", customerId, 1000m, "UYU", AsOf.AddDays(1));
        var usd = await SeedReceivableAsync(database, "company-1", customerId, 200m, "USD", AsOf.AddDays(-31));
        var settled = await SeedReceivableAsync(database, "company-1", customerId, 500m, "UYU", AsOf.AddDays(-1));
        _ = await SeedReceivableAsync(database, "company-2", otherCustomerId, 999m, "UYU", AsOf.AddDays(-91));

        await AppendAsync(database, ReceivableBalanceFact.AdjustmentIncrease(
            Guid.NewGuid(), "company-1", uyu, 100m, AsOf.AddDays(-5), DateTimeOffset.UtcNow));
        await AppendAsync(database, ReceivableBalanceFact.AdjustmentDecrease(
            Guid.NewGuid(), "company-1", uyu, 50m, AsOf.AddDays(-4), DateTimeOffset.UtcNow));
        var allocationId = Guid.NewGuid();
        await AppendAsync(database, ReceivableBalanceFact.CollectionAllocation(
            allocationId, "company-1", uyu, 400m, AsOf.AddDays(-3), DateTimeOffset.UtcNow));
        await AppendAsync(database, ReceivableBalanceFact.CollectionReversal(
            Guid.NewGuid(), "company-1", uyu, allocationId, 400m, AsOf.AddDays(-2), DateTimeOffset.UtcNow));

        await AppendAsync(database, ReceivableBalanceFact.CollectionAllocation(
            Guid.NewGuid(), "company-1", usd, 50m, AsOf.AddDays(-2), DateTimeOffset.UtcNow));
        await AppendAsync(database, ReceivableBalanceFact.CollectionAllocation(
            Guid.NewGuid(), "company-1", settled, 500m, AsOf.AddDays(-1), DateTimeOffset.UtcNow));

        await using var context = database.CreateContext();
        var summary = await new EfReceivableAccountStore(context)
            .GetPartySummaryAsync("company-1", customerId, AsOf);

        Assert.Collection(summary,
            usdBucket =>
            {
                Assert.Equal("USD", usdBucket.CurrencyCode);
                Assert.Equal(150m, usdBucket.Outstanding);
                Assert.Equal(150m, usdBucket.Overdue);
                Assert.Equal(0m, usdBucket.Aging.Current);
                Assert.Equal(150m, usdBucket.Aging.Days31To60);
            },
            uyuBucket =>
            {
                Assert.Equal("UYU", uyuBucket.CurrencyCode);
                Assert.Equal(1050m, uyuBucket.Outstanding);
                Assert.Equal(0m, uyuBucket.Overdue);
                Assert.Equal(1050m, uyuBucket.Aging.Current);
                Assert.Equal(0m, uyuBucket.Aging.Days1To30);
            });
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Aging_boundaries_use_only_remaining_outstanding_amount(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var customerId = Guid.NewGuid();
        await SeedPartyAsync(database, "company-1", customerId);

        foreach (var offset in new[] { 0, -1, -30, -31, -60, -61, -90, -91 })
            await SeedReceivableAsync(database, "company-1", customerId, 10m, "UYU", AsOf.AddDays(offset));

        await using var context = database.CreateContext();
        var bucket = Assert.Single(await new EfReceivableAccountStore(context)
            .GetPartySummaryAsync("company-1", customerId, AsOf));

        Assert.Equal(80m, bucket.Outstanding);
        Assert.Equal(70m, bucket.Overdue);
        Assert.Equal(10m, bucket.Aging.Current);
        Assert.Equal(20m, bucket.Aging.Days1To30);
        Assert.Equal(20m, bucket.Aging.Days31To60);
        Assert.Equal(20m, bucket.Aging.Days61To90);
        Assert.Equal(10m, bucket.Aging.Days91Plus);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Fact_store_fails_closed_for_cross_org_over_settlement_and_duplicate_reversal(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var customerId = Guid.NewGuid();
        await SeedPartyAsync(database, "company-1", customerId);
        var receivableId = await SeedReceivableAsync(
            database, "company-1", customerId, 100m, "UYU", AsOf.AddDays(-1));

        await using (var context = database.CreateContext())
        {
            var store = new EfReceivableAccountStore(context);
            var crossOrg = await Assert.ThrowsAsync<DomainRuleException>(() => store.AppendAsync(
                ReceivableBalanceFact.CollectionAllocation(
                    Guid.NewGuid(), "company-2", receivableId, 10m, AsOf, DateTimeOffset.UtcNow)));
            Assert.Equal("receivables.not_found", crossOrg.Code);
        }

        await using (var context = database.CreateContext())
        {
            var store = new EfReceivableAccountStore(context);
            var excessive = await Assert.ThrowsAsync<DomainRuleException>(() => store.AppendAsync(
                ReceivableBalanceFact.CollectionAllocation(
                    Guid.NewGuid(), "company-1", receivableId, 101m, AsOf, DateTimeOffset.UtcNow)));
            Assert.Equal("receivables.balance_negative", excessive.Code);
        }

        var allocationId = Guid.NewGuid();
        await AppendAsync(database, ReceivableBalanceFact.CollectionAllocation(
            allocationId, "company-1", receivableId, 60m, AsOf, DateTimeOffset.UtcNow));
        await AppendAsync(database, ReceivableBalanceFact.CollectionReversal(
            Guid.NewGuid(), "company-1", receivableId, allocationId, 60m, AsOf, DateTimeOffset.UtcNow));

        await using (var context = database.CreateContext())
        {
            var store = new EfReceivableAccountStore(context);
            var duplicate = await Assert.ThrowsAsync<DomainRuleException>(() => store.AppendAsync(
                ReceivableBalanceFact.CollectionReversal(
                    Guid.NewGuid(), "company-1", receivableId, allocationId, 60m, AsOf, DateTimeOffset.UtcNow)));
            Assert.Equal("receivables.collection_reversal_duplicate", duplicate.Code);
        }
    }

    private static async Task AppendAsync(TestDatabase database, ReceivableBalanceFact fact)
    {
        await using var context = database.CreateContext();
        await new EfReceivableAccountStore(context).AppendAsync(fact);
        await new EfUnitOfWork(context).SaveChangesAsync();
    }

    private static async Task SeedPartyAsync(TestDatabase database, string organizationId, Guid partyId)
    {
        var now = DateTimeOffset.UtcNow;
        await using var context = database.CreateContext();
        context.Parties.Add(new V1PartyRecord
        {
            Id = partyId,
            OrganizationId = organizationId,
            Name = "Customer",
            ResidenceCountry = "UY",
            TaxResidenceCountry = "UY",
            Active = true,
            Version = 1,
            CreatedAtUtc = now.UtcDateTime,
            UpdatedAtUtc = now.UtcDateTime,
            Roles =
            [
                new V1PartyRoleRecord
                {
                    PartyId = partyId,
                    Role = (int)PartyRole.Customer
                }
            ]
        });
        await new EfUnitOfWork(context).SaveChangesAsync();
    }

    private static async Task<Guid> SeedReceivableAsync(
        TestDatabase database,
        string organizationId,
        Guid customerId,
        decimal amount,
        string currencyCode,
        DateOnly dueDate)
    {
        var now = DateTimeOffset.UtcNow;
        var saleId = Guid.NewGuid();
        var receivableId = Guid.NewGuid();
        var saleEffectiveOn = new DateOnly(2026, 1, 1);

        await using var context = database.CreateContext();
        context.Sales.Add(new V1SaleRecord
        {
            Id = saleId,
            OrganizationId = organizationId,
            LocationId = "loc-1",
            TerminalId = "term-1",
            CustomerPartyId = customerId,
            Intent = (int)SaleCommercialIntent.ConsumerFinal,
            CurrencyCode = currencyCode,
            EffectiveOnUtc = saleEffectiveOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            DeliveryCountry = "UY",
            GoodsExportConfirmed = false,
            Status = (int)SaleStatus.Validated,
            ValidationFingerprint = Confirmation,
            ValidatedAtUtc = now,
            Version = 2,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        await new EfReceivableRepository(context).AddAsync(Receivable.CreateFromSale(
            receivableId,
            organizationId,
            customerId,
            saleId,
            amount,
            currencyCode,
            saleEffectiveOn,
            dueDate,
            Confirmation,
            Settlement,
            now));

        await new EfUnitOfWork(context).SaveChangesAsync();
        return receivableId;
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
            connectionBuilder["Database"] = $"ef_w24_ar_{provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";
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
