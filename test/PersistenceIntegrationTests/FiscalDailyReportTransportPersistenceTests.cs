using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
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

public sealed class FiscalDailyReportTransportPersistenceTests
{
    private static readonly DateOnly SummaryDate = new(2026, 9, 11);
    private static readonly DateTimeOffset FiscalSigningTimestamp =
        new(2026, 9, 12, 3, 15, 31, TimeSpan.FromHours(-3));

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Durable_signing_timestamp_round_trips_Uruguay_offset_portably(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var evidenceId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        await using (var context = database.CreateContext())
        {
            var evidenceRepository = new EfFiscalDailyReportSigningEvidenceRepository(context);
            var artifactRepository = new EfFiscalDailyReportSignedArtifactRepository(context);
            await evidenceRepository.AddAsync(new StoredFiscalDailyReportSigningEvidence(
                evidenceId,
                "company-transport",
                "214748364700",
                SummaryDate,
                1,
                "13.2",
                new string('a', 64),
                new string('b', 64),
                FiscalSigningTimestamp));
            await artifactRepository.AddAsync(new StoredFiscalDailyReportSignedArtifact(
                artifactId,
                evidenceId,
                "company-transport",
                "214748364700",
                SummaryDate,
                1,
                "13.2",
                new string('a', 64),
                new string('b', 64),
                new string('c', 64),
                FiscalSigningTimestamp,
                "dgi-daily-report-sha256-evidence-backed-v1",
                "thumbprint",
                "serial",
                "dgi-daily-report-v13.2-xsd-v1.44.2-signed",
                "13.2",
                "1.44.2",
                new string('d', 64),
                "<Reporte xmlns=\"http://cfe.dgi.gub.uy\" />"));
            await context.SaveChangesAsync();
        }

        await using var verify = database.CreateContext();
        var evidence = await new EfFiscalDailyReportSigningEvidenceRepository(verify)
            .GetByIdentityAsync("company-transport", "214748364700", SummaryDate, 1);
        var artifact = await new EfFiscalDailyReportSignedArtifactRepository(verify)
            .GetByIdentityAsync("company-transport", "214748364700", SummaryDate, 1);
        Assert.NotNull(evidence);
        Assert.NotNull(artifact);
        Assert.Equal(FiscalSigningTimestamp, evidence!.SigningTimestamp);
        Assert.Equal(FiscalSigningTimestamp, artifact!.SigningTimestamp);

        var persistedEvidence = await verify.Set<V1FiscalDailyReportSigningEvidenceRecord>()
            .AsNoTracking()
            .SingleAsync();
        var persistedArtifact = await verify.Set<V1FiscalDailyReportSignedArtifactRecord>()
            .AsNoTracking()
            .SingleAsync();
        Assert.Equal(TimeSpan.Zero, persistedEvidence.SigningTimestamp.Offset);
        Assert.Equal(TimeSpan.Zero, persistedArtifact.SigningTimestamp.Offset);
        Assert.Equal(-180, persistedEvidence.SigningOffsetMinutes);
        Assert.Equal(-180, persistedArtifact.SigningOffsetMinutes);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Submission_round_trips_AR_with_exact_signed_artifact_identity(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;

        var artifactId = await SeedSignedArtifactAsync(database);
        FiscalDailyReportSubmissionResult result;

        await using (var context = database.CreateContext())
        {
            var artifacts = new EfFiscalDailyReportSignedArtifactRepository(context);
            var submissions = new EfFiscalDailyReportSubmissionRepository(context);
            var clock = Clock();
            var prepare = Prepare(context, artifacts, submissions, clock);
            var dispatch = Dispatch(context, artifacts, submissions, new ArGateway(), clock);

            var prepared = await prepare.ExecuteAsync(new PrepareFiscalDailyReportSubmissionCommand(
                "company-transport",
                "214748364700",
                SummaryDate,
                1,
                "transport-op-1"));
            Assert.Equal(artifactId, prepared.SignedArtifactId);
            Assert.Equal(FiscalDailyReportSubmissionState.Prepared, prepared.State);

            result = await dispatch.ExecuteAsync(new DispatchFiscalDailyReportSubmissionCommand(
                "company-transport",
                "214748364700",
                SummaryDate,
                1));
        }

        Assert.Equal(FiscalDailyReportSubmissionState.Received, result.State);
        Assert.Equal("AR", result.AckStateCode);
        Assert.Equal("dgi-receiver-77", result.DgiReceiverId);
        Assert.Equal(1, result.AttemptCount);

        await using var verify = database.CreateContext();
        var row = await verify.Set<V1FiscalDailyReportSubmissionRecord>()
            .AsNoTracking()
            .SingleAsync();
        Assert.Equal(artifactId, row.SignedArtifactId);
        Assert.Equal("company-transport", row.OrganizationId);
        Assert.Equal("214748364700", row.IssuerRuc);
        Assert.Equal(SummaryDate.ToDateTime(TimeOnly.MinValue), row.SummaryDate);
        Assert.Equal(1, row.Sequence);
        Assert.Equal("transport-op-1", row.OperationId);
        Assert.Equal(new string('c', 64), row.SignedContentHash);
        Assert.Equal((int)FiscalDailyReportSubmissionState.Received, row.State);
        Assert.Equal(1, row.AttemptCount);
        Assert.Equal("AR", row.AckStateCode);
        Assert.Equal("dgi-receiver-77", row.DgiReceiverId);
        Assert.Contains("ACKRepDiario", row.AckXml, StringComparison.Ordinal);
        Assert.Null(row.FailureCode);
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Concurrent_dispatchers_cross_the_network_boundary_only_once(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null) return;
        await SeedSignedArtifactAsync(database);

        await using (var prepareContext = database.CreateContext())
        {
            var artifacts = new EfFiscalDailyReportSignedArtifactRepository(prepareContext);
            var submissions = new EfFiscalDailyReportSubmissionRepository(prepareContext);
            await Prepare(prepareContext, artifacts, submissions, Clock()).ExecuteAsync(
                new PrepareFiscalDailyReportSubmissionCommand(
                    "company-transport",
                    "214748364700",
                    SummaryDate,
                    1,
                    "transport-concurrent-op"));
        }

        var gateway = new BlockingArGateway();
        await using var firstContext = database.CreateContext();
        var firstArtifacts = new EfFiscalDailyReportSignedArtifactRepository(firstContext);
        var firstSubmissions = new EfFiscalDailyReportSubmissionRepository(firstContext);
        var firstDispatch = Dispatch(firstContext, firstArtifacts, firstSubmissions, gateway, Clock());
        var command = new DispatchFiscalDailyReportSubmissionCommand(
            "company-transport",
            "214748364700",
            SummaryDate,
            1);

        var firstTask = firstDispatch.ExecuteAsync(command);
        await gateway.Entered.WaitAsync(TimeSpan.FromSeconds(15));

        await using (var secondContext = database.CreateContext())
        {
            var secondArtifacts = new EfFiscalDailyReportSignedArtifactRepository(secondContext);
            var secondSubmissions = new EfFiscalDailyReportSubmissionRepository(secondContext);
            var secondDispatch = Dispatch(secondContext, secondArtifacts, secondSubmissions, gateway, Clock());
            var conflict = await Assert.ThrowsAsync<ApplicationProblemException>(() => secondDispatch.ExecuteAsync(command));
            Assert.Equal("fiscal.daily_report.transport.reconciliation_required", conflict.Code);
        }

        Assert.Equal(1, gateway.Calls);
        gateway.Release();
        var first = await firstTask;
        Assert.Equal(FiscalDailyReportSubmissionState.Received, first.State);
        Assert.Equal(1, gateway.Calls);

        await using var verify = database.CreateContext();
        var row = await verify.Set<V1FiscalDailyReportSubmissionRecord>()
            .AsNoTracking()
            .SingleAsync();
        Assert.Equal((int)FiscalDailyReportSubmissionState.Received, row.State);
        Assert.Equal(1, row.AttemptCount);
    }

    private static PrepareFiscalDailyReportSubmissionUseCase Prepare(
        V1PersistenceDbContext context,
        IFiscalDailyReportSignedArtifactRepository artifacts,
        IFiscalDailyReportSubmissionRepository submissions,
        IFiscalDailyReportTransportClock clock) =>
        new(
            artifacts,
            submissions,
            clock,
            new EfTransactionManager(context),
            new EfUnitOfWork(context),
            new NoOpAudit(),
            new NoOpOutbox(),
            new Actors(),
            new Correlations());

    private static DispatchFiscalDailyReportSubmissionUseCase Dispatch(
        V1PersistenceDbContext context,
        IFiscalDailyReportSignedArtifactRepository artifacts,
        IFiscalDailyReportSubmissionRepository submissions,
        IFiscalDailyReportTransportGateway gateway,
        IFiscalDailyReportTransportClock clock) =>
        new(
            artifacts,
            submissions,
            gateway,
            clock,
            new EfTransactionManager(context),
            new EfUnitOfWork(context),
            new NoOpAudit(),
            new NoOpOutbox(),
            new Actors(),
            new Correlations());

    private static IFiscalDailyReportTransportClock Clock() =>
        new FixedTransportClock(new DateTimeOffset(2026, 9, 12, 6, 45, 12, TimeSpan.Zero));

    private static async Task<Guid> SeedSignedArtifactAsync(TestDatabase database)
    {
        await using var context = database.CreateContext();
        var evidenceId = Guid.NewGuid();
        var artifactId = Guid.NewGuid();
        context.Set<V1FiscalDailyReportSigningEvidenceRecord>().Add(new V1FiscalDailyReportSigningEvidenceRecord
        {
            Id = evidenceId,
            OrganizationId = "company-transport",
            IssuerRuc = "214748364700",
            SummaryDate = SummaryDate.ToDateTime(TimeOnly.MinValue),
            Sequence = 1,
            FunctionalFormatVersion = "13.2",
            ProjectionFingerprint = new string('a', 64),
            UnsignedContentHash = new string('b', 64),
            SigningTimestamp = FiscalSigningTimestamp.ToUniversalTime(),
            SigningOffsetMinutes = -180
        });
        context.Set<V1FiscalDailyReportSignedArtifactRecord>().Add(new V1FiscalDailyReportSignedArtifactRecord
        {
            Id = artifactId,
            SigningEvidenceId = evidenceId,
            OrganizationId = "company-transport",
            IssuerRuc = "214748364700",
            SummaryDate = SummaryDate.ToDateTime(TimeOnly.MinValue),
            Sequence = 1,
            FunctionalFormatVersion = "13.2",
            ProjectionFingerprint = new string('a', 64),
            UnsignedContentHash = new string('b', 64),
            SignedContentHash = new string('c', 64),
            SigningTimestamp = FiscalSigningTimestamp.ToUniversalTime(),
            SigningOffsetMinutes = -180,
            SignatureProfileId = "dgi-daily-report-sha256-evidence-backed-v1",
            CertificateThumbprint = "thumbprint",
            CertificateSerialNumber = "serial",
            SchemaSetId = "dgi-daily-report-v13.2-xsd-v1.44.2-signed",
            SchemaFunctionalFormatVersion = "13.2",
            SchemaArchiveVersion = "1.44.2",
            SchemaSetFingerprint = new string('d', 64),
            SignedXml = "<Reporte xmlns=\"http://cfe.dgi.gub.uy\" />"
        });
        await context.SaveChangesAsync();
        return artifactId;
    }

    private sealed class ArGateway : IFiscalDailyReportTransportGateway
    {
        public Task<FiscalDailyReportTransportResponse> SendAsync(
            FiscalDailyReportTransportRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Response());
    }

    private sealed class BlockingArGateway : IFiscalDailyReportTransportGateway
    {
        private readonly TaskCompletionSource<bool> _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _calls;

        public Task Entered => _entered.Task;
        public int Calls => Volatile.Read(ref _calls);

        public async Task<FiscalDailyReportTransportResponse> SendAsync(
            FiscalDailyReportTransportRequest request,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _calls);
            _entered.TrySetResult(true);
            await _release.Task.WaitAsync(cancellationToken);
            return Response();
        }

        public void Release() => _release.TrySetResult(true);
    }

    private static FiscalDailyReportTransportResponse Response() =>
        new(
            "AR",
            "dgi-receiver-77",
            "<ACKRepDiario xmlns=\"http://cfe.dgi.gub.uy\"><Caratula><IDReceptor>dgi-receiver-77</IDReceptor></Caratula><Detalle><Estado>AR</Estado></Detalle></ACKRepDiario>");

    private sealed class FixedTransportClock(DateTimeOffset now) : IFiscalDailyReportTransportClock
    {
        public DateTimeOffset UtcNow => now;
    }

    private sealed class NoOpAudit : IAuditWriter
    {
        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpOutbox : IOutboxWriter
    {
        public Task EnqueueAsync<TEvent>(TEvent integrationEvent, OutboxContext context, CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent => Task.CompletedTask;
    }

    private sealed class Actors : IActorContextAccessor
    {
        public ActorContext Current { get; } = new(
            "actor",
            "Actor",
            true,
            new HashSet<string>(),
            new HashSet<string> { "company-transport" },
            new HashSet<string>(),
            new HashSet<string>(),
            "device");
    }

    private sealed class Correlations : ICorrelationContextAccessor
    {
        public CorrelationContext Current { get; } = new("corr-transport", "trace-transport");
    }
}
