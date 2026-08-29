using System.Data.Common;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Common.Security;
using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class CaeNumberingPersistenceTests
{
    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Concurrent_reservations_never_duplicate_company_cfe_series_number(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        await SeedActiveCaeAsync(database, 1000, 1100);

        var firstTask = ReserveWithRetryAsync(database, "sale-confirm-1", "loc-1", "term-1");
        var secondTask = ReserveWithRetryAsync(database, "sale-confirm-2", "loc-2", "term-2");
        var results = await Task.WhenAll(firstTask, secondTask);

        Assert.Equal(2, results.Select(x => x.Number).Distinct().Count());
        Assert.All(results, x => Assert.Equal(CfeFamily.EFactura, x.CfeType));

        await using var verification = database.CreateContext();
        var persisted = await verification.FiscalNumberReservations
            .AsNoTracking()
            .OrderBy(x => x.Number)
            .ToListAsync();
        Assert.Equal(2, persisted.Count);
        Assert.Equal(2, persisted.Select(x => new { x.OrganizationId, x.CfeType, x.Series, x.Number }).Distinct().Count());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Reservation_plus_audit_plus_outbox_roll_back_after_post_flush_failure(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var caeId = await SeedActiveCaeAsync(database, 2000, 2010);

        await using (var context = database.CreateContext())
        {
            var repository = new EfCaeRepository(context);
            var allocator = Allocator(context, repository, "corr-cae-rollback");
            var tx = new EfTransactionManager(context);
            var uow = new EfUnitOfWork(context);

            await Assert.ThrowsAsync<InjectedFailure>(() => tx.ExecuteAsync(async ct =>
            {
                _ = await allocator.ReserveAsync(new FiscalNumberReservationRequest(
                    "company-1", CfeFamily.EFactura, Today(), "sale-rollback",
                    "loc-1", "term-1"), ct);
                await uow.SaveChangesAsync(ct);
                throw new InjectedFailure();
            }));
        }

        await using (var verification = database.CreateContext())
        {
            Assert.Empty(await verification.FiscalNumberReservations.AsNoTracking().ToListAsync());
            Assert.Empty(await verification.AuditEvents
                .AsNoTracking()
                .Where(x => x.EventName == "fiscal.number.reserved")
                .ToListAsync());
            Assert.Empty(await verification.OutboxMessages.AsNoTracking().ToListAsync());

            var authorization = await verification.CaeAuthorizations
                .AsNoTracking()
                .SingleAsync(x => x.Id == caeId);
            Assert.Equal(2000, authorization.NextNumber);
        }

        var successful = await ReserveWithRetryAsync(database, "sale-after-rollback", "loc-1", "term-1");
        Assert.Equal(2000, successful.Number);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Expired_CAE_cannot_reserve_and_creates_no_partial_effect(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        await SeedCaeAsync(
            database,
            3000,
            3010,
            Today().AddDays(-10),
            Today().AddDays(-1),
            CaeAuthorizationStatus.Active,
            version: 2);

        var exception = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => ReserveOnceAsync(database, "expired-sale", "loc-1", "term-1"));

        Assert.Equal("cae.expired", exception.Code);
        await using var verification = database.CreateContext();
        Assert.Empty(await verification.FiscalNumberReservations.AsNoTracking().ToListAsync());
        Assert.Empty(await verification.AuditEvents
            .AsNoTracking()
            .Where(x => x.EventName == "fiscal.number.reserved")
            .ToListAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Exhausted_CAE_cannot_reuse_number_and_emits_actionable_alert_evidence(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        await SeedActiveCaeAsync(database, 4000, 4000);

        var first = await ReserveWithRetryAsync(database, "single-number-sale", "loc-1", "term-1");
        Assert.Equal(4000, first.Number);
        Assert.True(first.AuthorizationExhausted);

        var exception = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => ReserveOnceAsync(database, "second-sale", "loc-2", "term-2"));
        Assert.Equal("cae.exhausted", exception.Code);

        await using var verification = database.CreateContext();
        var reservations = await verification.FiscalNumberReservations.AsNoTracking().ToListAsync();
        Assert.Single(reservations);
        Assert.Equal(4000, reservations.Single().Number);
        var authorization = await verification.CaeAuthorizations.AsNoTracking().SingleAsync();
        Assert.Equal((int)CaeAuthorizationStatus.Exhausted, authorization.Status);
        Assert.Contains(
            await verification.AuditEvents.AsNoTracking().Select(x => x.EventName).ToListAsync(),
            x => x == "cae.exhausted");
        Assert.True(await verification.OutboxMessages.AsNoTracking().AnyAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Operational_allocation_partitions_range_without_breaking_global_uniqueness(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var caeId = await SeedActiveCaeAsync(database, 5000, 5010);
        await CreateAllocationAsync(database, caeId, "loc-1", "term-1", 5000, 5002);

        var allocated = await ReserveWithRetryAsync(database, "allocated-sale", "loc-1", "term-1");
        var direct = await ReserveWithRetryAsync(database, "direct-sale", "loc-2", "term-2");

        Assert.NotNull(allocated.AllocationId);
        Assert.Equal(5000, allocated.Number);
        Assert.Null(direct.AllocationId);
        Assert.Equal(5003, direct.Number);
        Assert.NotEqual(allocated.Number, direct.Number);

        await using var verification = database.CreateContext();
        Assert.Equal(2, await verification.FiscalNumberReservations.CountAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Multiple_disjoint_allocations_for_same_terminal_are_consumed_deterministically(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var caeId = await SeedActiveCaeAsync(database, 5200, 5210);
        await CreateAllocationAsync(database, caeId, "loc-1", "term-1", 5200, 5200);
        await CreateAllocationAsync(database, caeId, "loc-1", "term-1", 5202, 5203);

        var first = await ReserveWithRetryAsync(database, "multi-allocation-sale-1", "loc-1", "term-1");
        var second = await ReserveWithRetryAsync(database, "multi-allocation-sale-2", "loc-1", "term-1");

        Assert.Equal(5200, first.Number);
        Assert.Equal(5202, second.Number);
        Assert.NotEqual(first.AllocationId, second.AllocationId);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Overlapping_allocation_is_rejected_and_stale_version_does_not_mutate(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var caeId = await SeedActiveCaeAsync(database, 6000, 6100);
        var first = await CreateAllocationAsync(database, caeId, "loc-1", null, 6020, 6030);

        await using var context = database.CreateContext();
        var repository = new EfCaeRepository(context);
        var authorization = await repository.GetAuthorizationAsync("company-1", caeId)
            ?? throw new InvalidOperationException("Seeded CAE was not found.");
        var allocations = await repository.GetAllocationsAsync("company-1", caeId);

        var overlap = Assert.Throws<EFactura.Domain.Common.DomainRuleException>(() =>
            authorization.CreateAllocation(
                "loc-2", null, 6025, 6040, allocations,
                authorization.Version, Today(), DateTimeOffset.UtcNow));
        Assert.Equal("cae.allocation_overlap", overlap.Code);

        var stale = Assert.Throws<EFactura.Domain.Common.DomainRuleException>(() =>
            authorization.CreateAllocation(
                "loc-2", null, 6040, 6050, allocations,
                first.CaeAuthorizationVersionBefore, Today(), DateTimeOffset.UtcNow));
        Assert.Equal("concurrency.stale_version", stale.Code);

        await using var verification = database.CreateContext();
        Assert.Single(await verification.CaeAllocations.AsNoTracking().ToListAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task CAE_import_is_idempotent_and_commits_authorization_audit_outbox_and_key_once(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
            return;

        var command = new ImportCaeAuthorizationCommand(
            "company-1", CfeFamily.EFactura, "AUTH-IDEM-1", "A",
            7000, 7010, Today().AddDays(-1), Today().AddDays(30),
            "artifact-idem-1", new string('a', 64), "DGI CAE", "test://cae/artifact-idem-1",
            "cae-import-idem-key", "cae-import-idem-hash");

        CaeAuthorizationMutationResult first;
        await using (var context = database.CreateContext())
            first = await ImportUseCase(context, "corr-cae-import-1").ExecuteAsync(command);

        CaeAuthorizationMutationResult replay;
        await using (var context = database.CreateContext())
            replay = await ImportUseCase(context, "corr-cae-import-2").ExecuteAsync(command);

        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(first.Authorization.Id, replay.Authorization.Id);

        await using var verification = database.CreateContext();
        Assert.Single(await verification.CaeAuthorizations.AsNoTracking().ToListAsync());
        Assert.Equal(2, await verification.AuditEvents
            .AsNoTracking()
            .CountAsync(x => x.EventName == "cae.imported" || x.EventName == "cae.verified"));
        Assert.Single(await verification.OutboxMessages.AsNoTracking().ToListAsync());
        Assert.Single(await verification.IdempotencyRecords.AsNoTracking().ToListAsync());
    }

    private static ImportCaeAuthorizationUseCase ImportUseCase(V1PersistenceDbContext context, string correlationId) => new(
        new EfCaeRepository(context),
        new Release1CaeMetadataVerifier(),
        new EfTransactionManager(context),
        new EfUnitOfWork(context),
        new EfIdempotencyStore(context),
        new EfAuditWriter(context),
        new EfOutboxWriter(context),
        Actor(Permissions.FiscalManageCae),
        Correlation(correlationId));

    private static FiscalNumberAllocator Allocator(
        V1PersistenceDbContext context,
        ICaeRepository repository,
        string correlationId) => new(
        repository,
        new EfAuditWriter(context),
        new EfOutboxWriter(context),
        Actor(Permissions.SalesConfirm),
        Correlation(correlationId));

    private static async Task<FiscalNumberReservationResult> ReserveWithRetryAsync(
        TestDatabase database,
        string operationId,
        string locationId,
        string terminalId)
    {
        Exception? last = null;
        for (var attempt = 0; attempt < 4; attempt++)
        {
            try
            {
                return await ReserveOnceAsync(database, operationId, locationId, terminalId);
            }
            catch (ApplicationProblemException ex) when (ex.Code == "concurrency_conflict")
            {
                last = ex;
                await Task.Delay(25 * (attempt + 1));
            }
        }
        throw last ?? new InvalidOperationException("Fiscal reservation retry loop failed unexpectedly.");
    }

    private static async Task<FiscalNumberReservationResult> ReserveOnceAsync(
        TestDatabase database,
        string operationId,
        string locationId,
        string terminalId)
    {
        await using var context = database.CreateContext();
        var repository = new EfCaeRepository(context);
        var allocator = Allocator(context, repository, $"corr-{operationId}");
        var tx = new EfTransactionManager(context);
        var uow = new EfUnitOfWork(context);

        return await tx.ExecuteAsync(async ct =>
        {
            var result = await allocator.ReserveAsync(new FiscalNumberReservationRequest(
                "company-1", CfeFamily.EFactura, Today(), operationId,
                locationId, terminalId), ct);
            await uow.SaveChangesAsync(ct);
            return result;
        });
    }

    private static async Task<Guid> SeedActiveCaeAsync(TestDatabase database, long rangeFrom, long rangeTo)
    {
        return await SeedCaeAsync(
            database, rangeFrom, rangeTo,
            Today().AddDays(-1), Today().AddDays(30),
            CaeAuthorizationStatus.Active, version: 2);
    }

    private static async Task<Guid> SeedCaeAsync(
        TestDatabase database,
        long rangeFrom,
        long rangeTo,
        DateOnly validFrom,
        DateOnly validTo,
        CaeAuthorizationStatus status,
        long version)
    {
        await using var context = database.CreateContext();
        var authorization = CaeAuthorization.Rehydrate(
            Guid.NewGuid(),
            "company-1",
            CfeFamily.EFactura,
            $"AUTH-{rangeFrom}",
            "A",
            rangeFrom,
            rangeTo,
            validFrom,
            validTo,
            status,
            "METADATA_CONSISTENCY_V1",
            $"artifact-{rangeFrom}",
            rangeFrom.ToString("x").PadLeft(64, '0'),
            "DGI CAE",
            $"test://cae/{rangeFrom}",
            rangeFrom,
            version,
            DateTimeOffset.UtcNow.AddDays(-1),
            status == CaeAuthorizationStatus.Active ? DateTimeOffset.UtcNow.AddDays(-1) : null);
        await new EfCaeRepository(context).AddAuthorizationAsync(authorization);
        await new EfUnitOfWork(context).SaveChangesAsync();
        return authorization.Id;
    }

    private static async Task<AllocationSeedResult> CreateAllocationAsync(
        TestDatabase database,
        Guid caeId,
        string locationId,
        string? terminalId,
        long rangeFrom,
        long rangeTo)
    {
        await using var context = database.CreateContext();
        var repository = new EfCaeRepository(context);
        var authorization = await repository.GetAuthorizationAsync("company-1", caeId)
            ?? throw new InvalidOperationException("Seeded CAE was not found.");
        var before = authorization.Version;
        var existing = await repository.GetAllocationsAsync("company-1", caeId);
        var allocation = authorization.CreateAllocation(
            locationId, terminalId, rangeFrom, rangeTo,
            existing, before, Today(), DateTimeOffset.UtcNow);
        await repository.SaveAuthorizationAsync(authorization);
        await repository.AddAllocationAsync(allocation);
        await new EfUnitOfWork(context).SaveChangesAsync();
        return new AllocationSeedResult(allocation.Id, before, authorization.Version);
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);

    private static FixedActorContextAccessor Actor(params string[] permissions) =>
        new(new ActorContext(
            "actor-1",
            "CAE Numbering Tester",
            true,
            new HashSet<string>(permissions, StringComparer.Ordinal),
            new HashSet<string>(new[] { "company-1" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "loc-1", "loc-2" }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "term-1", "term-2" }, StringComparer.Ordinal),
            null));

    private static FixedCorrelationContextAccessor Correlation(string correlationId) =>
        new(new CorrelationContext(correlationId, $"trace-{correlationId}"));

    private sealed record AllocationSeedResult(
        Guid AllocationId,
        long CaeAuthorizationVersionBefore,
        long CaeAuthorizationVersionAfter);

    private sealed class InjectedFailure : Exception
    {
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
                if (string.Equals(
                    Environment.GetEnvironmentVariable("PERSISTENCE_INTEGRATION_REQUIRED"),
                    "true",
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Required integration test connection variable {variable} is missing.");
                }
                return null;
            }

            var connectionBuilder = new DbConnectionStringBuilder { ConnectionString = baseConnectionString };
            connectionBuilder["Database"] =
                $"ef_cae_{provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";
            var optionsBuilder = new DbContextOptionsBuilder<V1PersistenceDbContext>();
            V1PersistenceDatabaseConfigurator.Configure(
                optionsBuilder, provider, connectionBuilder.ConnectionString);

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
