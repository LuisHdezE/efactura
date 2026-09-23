using System.Data.Common;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Receivables;
using EFactura.Domain.Receivables;
using EFactura.Domain.Sales;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Infrastructure.Persistence.V1.Transactions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class W24ReceivableBalancePersistenceTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Settlement = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Party_scoped_projection_round_trips_append_only_effects_and_currency_buckets(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var partyId = Guid.NewGuid();
        var uyu = await SeedReceivableAsync(database, "company-1", partyId, 100m, "UYU", new DateOnly(2026, 8, 23));
        var usd = await SeedReceivableAsync(database, "company-1", partyId, 50m, "USD", new DateOnly(2026, 6, 24));
        _ = await SeedReceivableAsync(database, "company-2", Guid.NewGuid(), 900m, "UYU", new DateOnly(2026, 1, 1));

        var asOf = new DateTimeOffset(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);
        await using (var context = database.CreateContext())
        {
            var ledger = new EfReceivableBalanceRepository(context);
            var allocation = ReceivableBalanceEffect.CreateCollectionAllocation(
                Guid.NewGuid(), "company-1", uyu, 40m, "collection-1", 0, asOf.AddDays(-4));

            await ledger.AddAsync(ReceivableBalanceEffect.CreateAdjustment(
                Guid.NewGuid(), "company-1", uyu, 25m, true, "adjustment-plus", 0, asOf.AddDays(-5)));
            await ledger.AddAsync(allocation);
            await ledger.AddAsync(ReceivableBalanceEffect.CreateCollectionReversal(
                Guid.NewGuid(), "company-1", uyu, 40m, "collection-reversal-1", 0, allocation.Id, asOf.AddDays(-3)));
            await ledger.AddAsync(ReceivableBalanceEffect.CreateAdjustment(
                Guid.NewGuid(), "company-1", uyu, 5m, false, "adjustment-minus", 0, asOf.AddDays(-2)));
            await ledger.AddAsync(ReceivableBalanceEffect.CreateCollectionAllocation(
                Guid.NewGuid(), "company-1", usd, 50m, "collection-usd", 0, asOf.AddDays(-1)));

            await new EfUnitOfWork(context).SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var repository = new EfReceivableBalanceRepository(context);
            var model = new PartyReceivableAccountReadModel(repository);
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
    public async Task Database_rejects_second_reversal_of_same_allocation(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var partyId = Guid.NewGuid();
        var receivableId = await SeedReceivableAsync(
            database,
            "company-1",
            partyId,
            100m,
            "UYU",
            new DateOnly(2026, 9, 1));
        var now = new DateTimeOffset(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

        Guid allocationId;
        await using (var context = database.CreateContext())
        {
            var ledger = new EfReceivableBalanceRepository(context);
            var allocation = ReceivableBalanceEffect.CreateCollectionAllocation(
                Guid.NewGuid(), "company-1", receivableId, 25m, "collection-1", 0, now.AddMinutes(-2));
            allocationId = allocation.Id;
            await ledger.AddAsync(allocation);
            await ledger.AddAsync(ReceivableBalanceEffect.CreateCollectionReversal(
                Guid.NewGuid(), "company-1", receivableId, 25m, "reversal-a", 0, allocationId, now.AddMinutes(-1)));
            await new EfUnitOfWork(context).SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var ledger = new EfReceivableBalanceRepository(context);
            await ledger.AddAsync(ReceivableBalanceEffect.CreateCollectionReversal(
                Guid.NewGuid(), "company-1", receivableId, 25m, "reversal-b", 0, allocationId, now));

            await Assert.ThrowsAsync<DbUpdateException>(
                () => new EfUnitOfWork(context).SaveChangesAsync());
        }

        await using var verification = database.CreateContext();
        Assert.Equal(
            1,
            await verification.Set<V1ReceivableBalanceEffectRecord>()
                .CountAsync(x => x.ReversesEffectId == allocationId));
    }

    private static async Task<Guid> SeedReceivableAsync(
        TestDatabase database,
        string organizationId,
        Guid partyId,
        decimal amount,
        string currency,
        DateOnly dueDate)
    {
        var saleId = Guid.NewGuid();
        var receivableId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

        await using var context = database.CreateContext();
        if (!await context.Parties.AnyAsync(x => x.Id == partyId))
        {
            context.Parties.Add(new V1PartyRecord
            {
                Id = partyId,
                OrganizationId = organizationId,
                Name = $"Party {partyId:N}",
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
            OrganizationId = organizationId,
            LocationId = "loc-1",
            TerminalId = "term-1",
            CustomerPartyId = partyId,
            Intent = (int)SaleCommercialIntent.ConsumerFinal,
            CurrencyCode = currency,
            EffectiveOnUtc = now.UtcDateTime,
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
            partyId,
            saleId,
            amount,
            currency,
            DateOnly.FromDateTime(now.UtcDateTime),
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
                {
                    throw new InvalidOperationException($"Required integration test connection variable {variable} is missing.");
                }

                return null;
            }

            var connectionBuilder = new DbConnectionStringBuilder { ConnectionString = baseConnectionString };
            connectionBuilder["Database"] = $"ef_w24_{provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";
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
