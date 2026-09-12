using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalDailyReportSequenceLifecycleTests
{
    [Fact]
    public async Task First_version_is_one_and_operation_replay_does_not_consume_sequence()
    {
        var versions = new InMemoryVersions();
        var sut = UseCase(versions, new AlwaysSignedArtifacts());
        var command = Command("op-initial", FiscalDailyReportRevisionKind.Initial);

        var first = await sut.ExecuteAsync(command);
        var replay = await sut.ExecuteAsync(command);

        Assert.Equal(1, first.Snapshot.Sequence);
        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(first.VersionId, replay.VersionId);
        Assert.Single(versions.Values);
    }

    [Fact]
    public async Task Correction_uses_previous_plus_one_and_requires_prior_signed_artifact()
    {
        var versions = new InMemoryVersions();
        var initial = await UseCase(versions, new AlwaysSignedArtifacts()).ExecuteAsync(
            Command("op-initial", FiscalDailyReportRevisionKind.Initial));

        var unsignedPrior = UseCase(versions, new NeverSignedArtifacts());
        var blocked = await Assert.ThrowsAsync<ApplicationProblemException>(() => unsignedPrior.ExecuteAsync(
            Command("op-correction-blocked", FiscalDailyReportRevisionKind.Correction, "detected-discrepancy")));
        Assert.Equal("fiscal.daily_report.version.prior_not_signed", blocked.Code);

        var correction = await UseCase(versions, new AlwaysSignedArtifacts()).ExecuteAsync(
            Command("op-correction", FiscalDailyReportRevisionKind.Correction, "detected-discrepancy"));
        Assert.Equal(2, correction.Snapshot.Sequence);
        Assert.Equal(initial.VersionId, correction.PreviousVersionId);
        Assert.Equal("detected-discrepancy", correction.ReasonCode);
    }

    [Fact]
    public async Task Initial_cannot_be_allocated_twice_and_sequences_cannot_skip()
    {
        var versions = new InMemoryVersions();
        var sut = UseCase(versions, new AlwaysSignedArtifacts());
        await sut.ExecuteAsync(Command("op-1", FiscalDailyReportRevisionKind.Initial));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            sut.ExecuteAsync(Command("op-2", FiscalDailyReportRevisionKind.Initial)));

        Assert.Equal("fiscal.daily_report.version.initial_already_exists", error.Code);
        Assert.Single(versions.Values);
    }

    [Fact]
    public async Task Reusing_operation_id_with_different_input_fails_closed()
    {
        var versions = new InMemoryVersions();
        var sut = UseCase(versions, new AlwaysSignedArtifacts());
        await sut.ExecuteAsync(Command("same-op", FiscalDailyReportRevisionKind.Initial));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() => sut.ExecuteAsync(
            Command("same-op", FiscalDailyReportRevisionKind.Initial) with { SummaryDate = new DateOnly(2026, 9, 13) }));

        Assert.Equal("fiscal.daily_report.version.operation_replay_mismatch", error.Code);
    }

    [Fact]
    public async Task Fx_reliquidation_requires_pending_previous_marker_and_resolved_replacement()
    {
        var versions = new InMemoryVersions();
        var prior = SeedVersion(sequence: 1, requiresFxReliquidation: true);
        versions.Values.Add(prior);
        var sut = UseCase(versions, new AlwaysSignedArtifacts());

        var result = await sut.ExecuteAsync(Command(
            "fx-close",
            FiscalDailyReportRevisionKind.FxReliquidation,
            "future-date-fx-resolved"));

        Assert.Equal(2, result.Snapshot.Sequence);
        Assert.False(result.RequiresFxReliquidation);
        Assert.Equal(prior.Id, result.PreviousVersionId);
    }

    [Fact]
    public async Task Fx_reliquidation_without_pending_marker_fails_closed()
    {
        var versions = new InMemoryVersions();
        versions.Values.Add(SeedVersion(sequence: 1, requiresFxReliquidation: false));
        var sut = UseCase(versions, new AlwaysSignedArtifacts());

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() => sut.ExecuteAsync(Command(
            "fx-invalid",
            FiscalDailyReportRevisionKind.FxReliquidation,
            "future-date-fx-resolved")));

        Assert.Equal("fiscal.daily_report.version.fx_reliquidation_not_pending", error.Code);
    }

    private static AllocateFiscalDailyReportVersionCommand Command(
        string operationId,
        FiscalDailyReportRevisionKind kind,
        string? reason = null) =>
        new(
            "company-1",
            "214748364700",
            new DateOnly(2026, 9, 12),
            operationId,
            kind,
            reason,
            Array.Empty<FiscalDailyReportDocumentEvidence>(),
            Array.Empty<FiscalDailyReportAnnulmentEvidence>());

    private static AllocateFiscalDailyReportVersionUseCase UseCase(
        IFiscalDailyReportVersionRepository versions,
        IFiscalDailyReportSignedArtifactRepository artifacts) =>
        new(
            versions,
            artifacts,
            new InlineTransactionManager(),
            new UnitOfWork(),
            new Audit(),
            new Outbox(),
            new Actors(),
            new Correlations());

    private static StoredFiscalDailyReportVersion SeedVersion(int sequence, bool requiresFxReliquidation)
    {
        var snapshot = FiscalDailyReportSnapshot.Create(
            "company-1", "214748364700", new DateOnly(2026, 9, 12), sequence);
        var provisional = new StoredFiscalDailyReportVersion(
            Guid.NewGuid(),
            "company-1",
            "214748364700",
            new DateOnly(2026, 9, 12),
            sequence,
            sequence == 1 ? null : Guid.NewGuid(),
            $"seed-{sequence}",
            sequence == 1 ? FiscalDailyReportRevisionKind.Initial : FiscalDailyReportRevisionKind.Correction,
            sequence == 1 ? "initial" : "seed",
            snapshot.ReconciliationFingerprint,
            requiresFxReliquidation,
            new DateTimeOffset(2026, 9, 12, 12, 0, 0, TimeSpan.Zero),
            new string('0', 64));
        return provisional with { VersionFingerprint = provisional.ComputeFingerprint() };
    }

    private sealed class InMemoryVersions : IFiscalDailyReportVersionRepository
    {
        public List<StoredFiscalDailyReportVersion> Values { get; } = [];
        public Task<bool> AcquireOrganizationSequenceLockAsync(string organizationId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<StoredFiscalDailyReportVersion?> GetByOperationIdAsync(string organizationId, string operationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Values.SingleOrDefault(x => x.OrganizationId == organizationId && x.OperationId == operationId));
        public Task<StoredFiscalDailyReportVersion?> GetLatestAsync(string organizationId, string issuerRuc, DateOnly summaryDate, CancellationToken cancellationToken = default) =>
            Task.FromResult(Values.Where(x => x.OrganizationId == organizationId && x.IssuerRuc == issuerRuc && x.SummaryDate == summaryDate).OrderByDescending(x => x.Sequence).FirstOrDefault());
        public Task<StoredFiscalDailyReportVersion?> GetByIdentityAsync(string organizationId, string issuerRuc, DateOnly summaryDate, int sequence, CancellationToken cancellationToken = default) =>
            Task.FromResult(Values.SingleOrDefault(x => x.OrganizationId == organizationId && x.IssuerRuc == issuerRuc && x.SummaryDate == summaryDate && x.Sequence == sequence));
        public Task AddAsync(StoredFiscalDailyReportVersion version, CancellationToken cancellationToken = default)
        {
            Values.Add(version);
            return Task.CompletedTask;
        }
    }

    private sealed class AlwaysSignedArtifacts : IFiscalDailyReportSignedArtifactRepository
    {
        public Task<StoredFiscalDailyReportSignedArtifact?> GetByIdentityAsync(string organizationId, string issuerRuc, DateOnly summaryDate, int sequence, CancellationToken cancellationToken = default) =>
            Task.FromResult<StoredFiscalDailyReportSignedArtifact?>(new(
                Guid.NewGuid(), Guid.NewGuid(), organizationId, issuerRuc, summaryDate, sequence,
                "13.2", new string('a', 64), new string('b', 64), new string('c', 64),
                DateTimeOffset.UtcNow, "profile", "thumb", "serial", "schema", "13.2", "1.44.2", new string('d', 64), "<signed/>"));
        public Task AddAsync(StoredFiscalDailyReportSignedArtifact artifact, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NeverSignedArtifacts : IFiscalDailyReportSignedArtifactRepository
    {
        public Task<StoredFiscalDailyReportSignedArtifact?> GetByIdentityAsync(string organizationId, string issuerRuc, DateOnly summaryDate, int sequence, CancellationToken cancellationToken = default) => Task.FromResult<StoredFiscalDailyReportSignedArtifact?>(null);
        public Task AddAsync(StoredFiscalDailyReportSignedArtifact artifact, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InlineTransactionManager : ITransactionManager
    {
        public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
    }
    private sealed class UnitOfWork : IUnitOfWork { public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1); }
    private sealed class Audit : IAuditWriter { public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default) => Task.CompletedTask; }
    private sealed class Outbox : IOutboxWriter { public Task EnqueueAsync<TEvent>(TEvent integrationEvent, OutboxContext context, CancellationToken cancellationToken = default) where TEvent : IIntegrationEvent => Task.CompletedTask; }
    private sealed class Actors : IActorContextAccessor { public ActorContext Current { get; } = new("actor", "Actor", true, new HashSet<string>(), new HashSet<string> { "company-1" }, new HashSet<string>(), new HashSet<string>(), "device"); }
    private sealed class Correlations : ICorrelationContextAccessor { public CorrelationContext Current { get; } = new("corr", "trace"); }
}
