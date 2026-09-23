using System.Data.Common;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Parties;
using EFactura.Application.Payables;
using EFactura.Application.Receivables;
using EFactura.Domain.Parties;
using EFactura.Domain.Payables;
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

public sealed class W24PartyAccountSummaryPersistenceTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Settlement = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Dual_role_party_composes_provider_backed_ar_and_ap_without_cross_organization_leakage(
        V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var partyId = Guid.NewGuid();
        var asOf = new DateTimeOffset(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);
        var receivableId = Guid.NewGuid();
        var payableId = Guid.NewGuid();
        var saleId = Guid.NewGuid();
        var saleDate = new DateOnly(2026, 8, 1);
        var saleAt = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);

        await using (var context = database.CreateContext())
        {
            var parties = new EfPartyRepository(context);
            await parties.AddAsync(Party.Create(
                partyId,
                "company-1",
                PartyKind.Organization,
                "Dual-role account summary party",
                "UY",
                "UY",
                new[] { PartyRole.Customer, PartyRole.Supplier }));

            context.Sales.Add(new V1SaleRecord
            {
                Id = saleId,
                OrganizationId = "company-1",
                LocationId = "loc-1",
                TerminalId = "term-1",
                CustomerPartyId = partyId,
                Intent = (int)SaleCommercialIntent.ConsumerFinal,
                CurrencyCode = "UYU",
                EffectiveOnUtc = saleAt.UtcDateTime,
                DeliveryCountry = "UY",
                GoodsExportConfirmed = false,
                Status = (int)SaleStatus.Validated,
                ValidationFingerprint = Confirmation,
                ValidatedAtUtc = saleAt,
                Version = 2,
                CreatedAtUtc = saleAt,
                UpdatedAtUtc = saleAt
            });

            await new EfReceivableRepository(context).AddAsync(Receivable.CreateFromSale(
                receivableId,
                "company-1",
                partyId,
                saleId,
                100m,
                "UYU",
                saleDate,
                new DateOnly(2026, 8, 23),
                Confirmation,
                Settlement,
                saleAt));

            await new EfPayableRepository(context).AddAsync(Payable.CreateFromPurchaseReceipt(
                payableId,
                "company-1",
                partyId,
                "purchase-receipt-1",
                80m,
                "USD",
                new DateOnly(2026, 8, 15),
                new DateOnly(2026, 9, 10),
                new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero)));

            await new EfUnitOfWork(context).SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var receivableLedger = new EfReceivableBalanceRepository(context);
            await receivableLedger.AddAsync(ReceivableBalanceEffect.CreateCollectionAllocation(
                Guid.NewGuid(),
                "company-1",
                receivableId,
                25m,
                "collection-1",
                0,
                asOf.AddDays(-2)));

            var payableLedger = new EfPayableBalanceRepository(context);
            await payableLedger.AddAsync(PayableBalanceEffect.CreateSupplierPaymentAllocation(
                Guid.NewGuid(),
                "company-1",
                payableId,
                30m,
                "supplier-payment-1",
                0,
                asOf.AddDays(-1)));

            await new EfUnitOfWork(context).SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var receivableRepository = new EfReceivableBalanceRepository(context);
            var payableRepository = new EfPayableBalanceRepository(context);
            var sut = new PartyAccountSummaryReadModel(
                new EfPartyRepository(context),
                new PartyReceivableAccountReadModel(receivableRepository),
                new PartyPayableAccountReadModel(payableRepository));

            var result = await sut.GetAsync("company-1", partyId, asOf);

            Assert.True(result.ReceivablesApplicable);
            Assert.True(result.PayablesApplicable);
            Assert.Equal(asOf, result.AsOfUtc);

            var receivable = Assert.Single(result.Receivables);
            Assert.Equal("UYU", receivable.CurrencyCode);
            Assert.Equal(75m, receivable.Outstanding);
            Assert.Equal(75m, receivable.Overdue);

            var payable = Assert.Single(result.Payables);
            Assert.Equal("USD", payable.CurrencyCode);
            Assert.Equal(50m, payable.Outstanding);
            Assert.Equal(50m, payable.Overdue);

            await Assert.ThrowsAsync<ApplicationProblemException>(
                () => sut.GetAsync("company-2", partyId, asOf));
        }
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
            connectionBuilder["Database"] = $"ef_w24_summary_{provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";
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
