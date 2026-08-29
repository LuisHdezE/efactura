using System.Data.Common;
using EFactura.Application.Taxation;
using EFactura.Domain.Common;
using EFactura.Domain.Taxation;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class TaxationPersistenceTests
{
    [Fact]
    public void Tax_profile_rejects_a_CFE_indicator_that_does_not_match_its_treatment()
    {
        var exception = Assert.Throws<DomainRuleException>(() => TaxProfile.Create(
            Guid.NewGuid(),
            null,
            "BAD",
            "Invalid profile",
            TaxTreatmentKind.VatBasic,
            22m,
            2,
            new DateOnly(2007, 7, 1),
            null,
            "test-v1",
            "test",
            "test",
            "https://example.invalid",
            "25.2",
            DateTimeOffset.UtcNow));

        Assert.Equal("tax.profile.cfe_indicator_mismatch", exception.Code);
    }

    [Fact]
    public void Export_service_cannot_be_resolved_without_article_34_rule_provenance()
    {
        var profile = CreateBasicProfile();
        var resolver = new TaxTreatmentResolver();

        var unresolved = resolver.Resolve(
            profile,
            new TaxResolutionContext(
                new DateOnly(2026, 8, 29),
                TaxJurisdictionKind.ExportServices,
                ExportServiceQualification.Qualifies));

        Assert.Equal(TaxDecisionStatus.RequiresRuleQualification, unresolved.Status);
        Assert.Contains("article34RuleReference", unresolved.MissingFacts);

        var qualified = resolver.Resolve(
            profile,
            new TaxResolutionContext(
                new DateOnly(2026, 8, 29),
                TaxJurisdictionKind.ExportServices,
                ExportServiceQualification.Qualifies,
                new TaxRuleReference(
                    "UY-IVA-EXPORT-SERVICE-TEST",
                    "test-v1",
                    "Test qualification policy",
                    "Article 34 test fixture",
                    "https://example.invalid/article34")));

        Assert.Equal(TaxDecisionStatus.Resolved, qualified.Status);
        Assert.Equal(TaxTreatmentKind.ExportOrAssimilated, qualified.Treatment);
        Assert.Null(qualified.RatePercent);
        Assert.Equal(10, qualified.CfeBillingIndicator);
        Assert.Equal(2, qualified.RuleReferences.Count);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Migration_materializes_the_same_current_base_profiles_on_both_engines(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
        {
            return;
        }

        await using var context = database.CreateContext();
        var repository = new EfTaxProfileRepository(context);

        var current = await repository.SearchUsableAsync(
            new TaxProfileSearchRequest("company-1", new DateOnly(2026, 8, 29), 1, 100));

        Assert.Equal(2, current.Total);

        var basic = Assert.Single(current.Items.Where(x => x.Code == "UY-IVA-BASIC-22"));
        Assert.Equal(TaxTreatmentKind.VatBasic, basic.Treatment);
        Assert.Equal(22m, basic.RatePercent);
        Assert.Equal(3, basic.CfeBillingIndicator);
        Assert.Equal("25.2", basic.CfeSpecificationVersion);
        Assert.Equal(new DateOnly(2007, 7, 1), basic.EffectiveFrom);
        Assert.True(basic.IsSystemProfile);

        var minimum = Assert.Single(current.Items.Where(x => x.Code == "UY-IVA-MINIMUM-10"));
        Assert.Equal(TaxTreatmentKind.VatMinimum, minimum.Treatment);
        Assert.Equal(10m, minimum.RatePercent);
        Assert.Equal(2, minimum.CfeBillingIndicator);

        var beforeEffectiveDate = await repository.SearchUsableAsync(
            new TaxProfileSearchRequest("company-1", new DateOnly(2007, 6, 30), 1, 100));
        Assert.Equal(0, beforeEffectiveDate.Total);
    }

    private static TaxProfile CreateBasicProfile() =>
        TaxProfile.Create(
            Guid.NewGuid(),
            null,
            "UY-IVA-BASIC-22",
            "IVA tasa básica 22%",
            TaxTreatmentKind.VatBasic,
            22m,
            3,
            new DateOnly(2007, 7, 1),
            null,
            "UY-IVA-RATES-2007-07-01",
            "Uruguay - IMPO / Dirección General Impositiva",
            "Test fixture mirroring accepted baseline",
            "https://www.impo.com.uy/bases/todgi-2023/10-2024/10",
            "25.2",
            new DateTimeOffset(2026, 8, 29, 0, 0, 0, TimeSpan.Zero));

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly DbContextOptions<V1PersistenceDbContext> _options;

        private TestDatabase(DbContextOptions<V1PersistenceDbContext> options)
        {
            _options = options;
        }

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
            connectionBuilder["Database"] = $"efactura_tax_{provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";

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
