using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Fiscal;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class FiscalDailyReportSequenceLifecyclePersistenceTests
{
    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Version_repository_persists_sequence_lineage_and_operation_idempotency(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;
        await SeedCompanyAsync(database);

        FiscalDailyReportVersionAllocationResult first;
        await using (var context = database.CreateContext())
            first = await UseCase(context).ExecuteAsync(Command("op-1", FiscalDailyReportRevisionKind.Initial));

        Assert.Equal(1, first.Snapshot.Sequence);

        FiscalDailyReportVersionAllocationResult replay;
        await using (var context = database.CreateContext())
            replay = await UseCase(context).ExecuteAsync(Command("op-1", FiscalDailyReportRevisionKind.Initial));

        Assert.True(replay.Replayed);
        Assert.Equal(first.VersionId, replay.VersionId);

        FiscalDailyReportVersionAllocationResult correction;
        await using (var context = database.CreateContext())
            correction = await UseCase(context).ExecuteAsync(Command("op-2", FiscalDailyReportRevisionKind.Correction, "detected-discrepancy"));

        Assert.Equal(2, correction.Snapshot.Sequence);
        Assert.Equal(first.VersionId, correction.PreviousVersionId);

        await using var verify = database.CreateContext();
        var rows = await verify.Set<V1FiscalDailyReportVersionRecord>().AsNoTracking().OrderBy(x => x.Sequence).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { 1, 2 }, rows.Select(x => x.Sequence).ToArray());
        Assert.Equal(rows[0].Id, rows[1].PreviousVersionId);
        Assert.Equal(2, rows.Select(x => x.OperationId).Distinct().Count());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Concurrent_initial_allocations_cannot_create_two_sequence_one_versions(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;
        await SeedCompanyAsync(database);

        async Task<(bool Success, string? Code)> Run(string operation)
        {
            try
            {
                await using var context = database.CreateContext();
                _ = await UseCase(context).ExecuteAsync(Command(operation, FiscalDailyReportRevisionKind.Initial));
                return (true, null);
            }
            catch (EFactura.Application.Common.Errors.ApplicationProblemException ex)
            {
                return (false, ex.Code);
            }
        }

        var results = await Task.WhenAll(Run("concurrent-1"), Run("concurrent-2"));
        Assert.Single(results.Where(x => x.Success));
        Assert.Single(results.Where(x => !x.Success));
        Assert.Equal("fiscal.daily_report.version.initial_already_exists", results.Single(x => !x.Success).Code);

        await using var verify = database.CreateContext();
        var rows = await verify.Set<V1FiscalDailyReportVersionRecord>().AsNoTracking().ToListAsync();
        Assert.Single(rows);
        Assert.Equal(1, rows.Single().Sequence);
    }

    private static AllocateFiscalDailyReportVersionUseCase UseCase(V1PersistenceDbContext context) =>
        new(
            new EfFiscalDailyReportVersionRepository(context),
            new AlwaysSignedArtifacts(),
            new EfTransactionManager(context),
            new EfUnitOfWork(context),
            new NoOpAudit(),
            new NoOpOutbox(),
            new Actors(),
            new Correlations());

    private static AllocateFiscalDailyReportVersionCommand Command(string operation, FiscalDailyReportRevisionKind kind, string? reason = null) =>
        new("company-1", "214748364700", new DateOnly(2026, 9, 12), operation, kind, reason);

    private static async Task SeedCompanyAsync(TestDatabase database)
    {
        await using var context = database.CreateContext();
        if (await context.Set<V1CompanyFiscalProfileRecord>().AnyAsync()) return;
        var now = DateTimeOffset.UtcNow;
        context.Set<V1CompanyFiscalProfileRecord>().Add(new V1CompanyFiscalProfileRecord
        {
            OrganizationId = "company-1",
            Ruc = "214748364700",
            LegalName = "Sequence Test Company",
            Version = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await context.SaveChangesAsync();
    }

    private sealed class AlwaysSignedArtifacts : IFiscalDailyReportSignedArtifactRepository
    {
        public Task<StoredFiscalDailyReportSignedArtifact?> GetByIdentityAsync(string organizationId, string issuerRuc, DateOnly summaryDate, int sequence, CancellationToken cancellationToken = default) =>
            Task.FromResult<StoredFiscalDailyReportSignedArtifact?>(new(
                Guid.NewGuid(), Guid.NewGuid(), organizationId, issuerRuc, summaryDate, sequence,
                "13.2", new string('a', 64), new string('b', 64), new string('c', 64), DateTimeOffset.UtcNow,
                "profile", "thumb", "serial", "schema", "13.2", "1.44.2", new string('d', 64), "<signed/>"));
        public Task AddAsync(StoredFiscalDailyReportSignedArtifact artifact, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
    private sealed class NoOpAudit : IAuditWriter { public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default) => Task.CompletedTask; }
    private sealed class NoOpOutbox : IOutboxWriter { public Task EnqueueAsync<TEvent>(TEvent integrationEvent, OutboxContext context, CancellationToken cancellationToken = default) where TEvent : IIntegrationEvent => Task.CompletedTask; }
    private sealed class Actors : IActorContextAccessor { public ActorContext Current { get; } = new("actor", "Actor", true, new HashSet<string>(), new HashSet<string> { "company-1" }, new HashSet<string>(), new HashSet<string>(), "device"); }
    private sealed class Correlations : ICorrelationContextAccessor { public CorrelationContext Current { get; } = new("corr", "trace"); }
}
