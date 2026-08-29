using System.Data.Common;
using EFactura.Application.Catalog;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Security;
using EFactura.Application.Taxation;
using EFactura.Domain.Catalog;
using EFactura.Domain.Taxation;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class TaxationPersistenceTests
{
    [Fact]
    public void Tax_profile_rejects_invalid_rate_or_effective_range()
    {
        Assert.ThrowsAny<Exception>(() => TaxProfile.Create(
            Guid.NewGuid(), "company-1", "BAD", "Bad", "VAT", 101m,
            new DateOnly(2026, 1, 1), null, "source", "reference", "v1"));

        Assert.ThrowsAny<Exception>(() => TaxProfile.Create(
            Guid.NewGuid(), "company-1", "BAD2", "Bad", "VAT", 10m,
            new DateOnly(2026, 2, 1), new DateOnly(2026, 1, 1), "source", "reference", "v1"));
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Active_effective_profile_can_be_assigned_and_inactive_date_is_rejected(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
        {
            return;
        }

        var profileId = Guid.NewGuid();
        await using (var context = database.CreateContext())
        {
            var repository = new EfTaxProfileRepository(context);
            await repository.AddAsync(TaxProfile.Create(
                profileId,
                "company-1",
                "VAT-CONFIGURED",
                "Configured VAT profile",
                "DOMESTIC_TAXED",
                10m,
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 12, 31),
                "Test authoritative source",
                "urn:test:tax-source",
                "2026-test"));
            await context.SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var validator = new TaxProfileAssignmentValidator(new EfTaxProfileRepository(context));
            await validator.ValidateAssignableAsync("company-1", profileId, new DateOnly(2026, 8, 29));

            var expired = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
                validator.ValidateAssignableAsync("company-1", profileId, new DateOnly(2027, 1, 1)));
            Assert.Equal("tax.profile_not_effective", expired.Code);
        }

        await using (var context = database.CreateContext())
        {
            var actor = new FixedActorContextAccessor(new ActorContext(
                "actor-1",
                "Taxation Integration Tester",
                true,
                new HashSet<string>(new[] { Permissions.CatalogManage }, StringComparer.Ordinal),
                new HashSet<string>(new[] { "company-1" }, StringComparer.Ordinal),
                new HashSet<string>(StringComparer.Ordinal),
                new HashSet<string>(StringComparer.Ordinal),
                null));
            var correlation = new FixedCorrelationContextAccessor(new CorrelationContext("corr-tax-item", "trace-tax-item"));
            var taxRepository = new EfTaxProfileRepository(context);
            var useCase = new CreateCommercialItemUseCase(
                new EfCommercialItemRepository(context),
                new EfTransactionManager(context),
                new EfUnitOfWork(context),
                new EfIdempotencyStore(context),
                new EfAuditWriter(context),
                new EfOutboxWriter(context),
                actor,
                correlation,
                null,
                new TaxProfileAssignmentValidator(taxRepository));

            var created = await useCase.ExecuteAsync(new CreateCommercialItemCommand(
                "company-1",
                "TAXED-001",
                "Tax-profile-backed item",
                null,
                CommercialItemKind.Product,
                "UNIT",
                true,
                profileId,
                null,
                "tax-item-create-1",
                "tax-item-hash-1"));

            Assert.False(created.Replayed);
        }

        await using var verification = database.CreateContext();
        var stored = await verification.CommercialItems.SingleAsync();
        Assert.Equal(profileId, stored.TaxProfileId);
        Assert.Equal(1, await verification.TaxProfiles.CountAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Tax_profile_query_is_effective_date_scoped(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
        {
            return;
        }

        await using var context = database.CreateContext();
        var repository = new EfTaxProfileRepository(context);
        await repository.AddAsync(TaxProfile.Create(
            Guid.NewGuid(), "company-1", "CURRENT", "Current profile", "DOMESTIC_TAXED", 10m,
            new DateOnly(2026, 1, 1), null, "source", "urn:test:current", "2026"));
        await repository.AddAsync(TaxProfile.Create(
            Guid.NewGuid(), "company-1", "FUTURE", "Future profile", "DOMESTIC_TAXED", 11m,
            new DateOnly(2027, 1, 1), null, "source", "urn:test:future", "2027"));
        await context.SaveChangesAsync();

        var result = await repository.SearchAsync(new TaxProfileSearchRequest(
            "company-1", new DateOnly(2026, 8, 29), null, true, 1, 100));

        Assert.Single(result.Items);
        Assert.Equal("CURRENT", result.Items.Single().Code);
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
                if (string.Equals(Environment.GetEnvironmentVariable("PERSISTENCE_INTEGRATION_REQUIRED"), "true", StringComparison.OrdinalIgnoreCase))
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
