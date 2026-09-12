using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using Infrastructure.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalDailyReportBrCorrectionLifecycleTests
{
    private static readonly DateOnly SummaryDate = new(2026, 9, 11);

    [Fact]
    public async Task Ordinary_BR_prepares_revision_two_and_operation_replay_never_resigns_or_mutates_root()
    {
        var originalAck = BrAck("R04", "No cumple validaciones según Formato de Reporte");
        var root = Root(FiscalDailyReportSubmissionState.Rejected, "BR", originalAck);
        var submissions = new SubmissionRepository(root);
        var revisions = new RevisionRepository();
        var signer = new CountingSigner();
        var sut = Prepare(submissions, revisions, signer);
        var command = new PrepareFiscalDailyReportBrCorrectionCommand(Projection(), "br-correction-op-1", "business-data-corrected");

        var first = await sut.ExecuteAsync(command);
        var replay = await sut.ExecuteAsync(command);

        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(2, first.LocalRevision);
        Assert.Equal(root.Id, first.RootSubmissionId);
        Assert.Equal(1, first.Sequence);
        Assert.Equal(first.RevisionId, replay.RevisionId);
        Assert.Equal(first.SignedArtifactId, replay.SignedArtifactId);
        Assert.Equal(1, signer.Calls);

        var unchanged = await submissions.GetByIdentityAsync(root.OrganizationId, root.IssuerRuc, root.SummaryDate, root.Sequence);
        Assert.NotNull(unchanged);
        Assert.Equal(FiscalDailyReportSubmissionState.Rejected, unchanged!.State);
        Assert.Equal("BR", unchanged.AckStateCode);
        Assert.Equal(originalAck, unchanged.AckXml);
    }

    [Fact]
    public async Task R05_blocks_same_sequence_correction_fail_closed()
    {
        var submissions = new SubmissionRepository(Root(
            FiscalDailyReportSubmissionState.Rejected,
            "BR",
            BrAck("R05", "La secuencia indicada en el reporte no es correcta")));
        var signer = new CountingSigner();

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            Prepare(submissions, new RevisionRepository(), signer).ExecuteAsync(
                new PrepareFiscalDailyReportBrCorrectionCommand(Projection(), "br-r05", "sequence-review")));

        Assert.Equal("fiscal.daily_report.br_correction.r05_reconciliation_required", error.Code);
        Assert.Equal(0, signer.Calls);
    }

    [Fact]
    public async Task AR_source_cannot_create_same_sequence_correction()
    {
        var submissions = new SubmissionRepository(Root(
            FiscalDailyReportSubmissionState.Received,
            "AR",
            ArAck()));
        var signer = new CountingSigner();

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            Prepare(submissions, new RevisionRepository(), signer).ExecuteAsync(
                new PrepareFiscalDailyReportBrCorrectionCommand(Projection(), "br-after-ar", "not-allowed")));

        Assert.Equal("fiscal.daily_report.br_correction.br_required", error.Code);
        Assert.Equal(0, signer.Calls);
    }

    [Fact]
    public async Task Unknown_source_requires_reconciliation_before_new_correction()
    {
        var submissions = new SubmissionRepository(Root(
            FiscalDailyReportSubmissionState.Unknown,
            null,
            null));
        var signer = new CountingSigner();

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            Prepare(submissions, new RevisionRepository(), signer).ExecuteAsync(
                new PrepareFiscalDailyReportBrCorrectionCommand(Projection(), "br-after-unknown", "not-safe")));

        Assert.Equal("fiscal.daily_report.br_correction.reconciliation_required", error.Code);
        Assert.Equal(0, signer.Calls);
    }

    [Fact]
    public async Task Rejected_revision_two_authorizes_revision_three_under_same_SecEnvio_with_new_evidence_ids()
    {
        var root = Root(FiscalDailyReportSubmissionState.Rejected, "BR", BrAck("R04", "Primera observación"));
        var submissions = new SubmissionRepository(root);
        var revisions = new RevisionRepository();
        var signer = new CountingSigner();
        var clock = new FixedTransportClock(new DateTimeOffset(2026, 9, 12, 18, 0, 0, TimeSpan.Zero));
        var prepare = Prepare(submissions, revisions, signer, clock);

        var revision2 = await prepare.ExecuteAsync(new PrepareFiscalDailyReportBrCorrectionCommand(
            Projection(), "br-revision-2", "fix-1"));

        var gateway = new FakeGateway(new FiscalDailyReportTransportResponse(
            "BR", "receiver-r2", BrAck("R04", "Segunda observación")));
        var rejected = await Dispatch(submissions, revisions, gateway, clock).ExecuteAsync(
            new DispatchFiscalDailyReportBrCorrectionCommand("company-1", "214748364700", SummaryDate, 1, 2));
        Assert.Equal(FiscalDailyReportSubmissionState.Rejected, rejected.State);

        var revision3 = await prepare.ExecuteAsync(new PrepareFiscalDailyReportBrCorrectionCommand(
            Projection(), "br-revision-3", "fix-2"));

        var stored2 = await revisions.GetByRevisionAsync("company-1", "214748364700", SummaryDate, 1, 2);
        var stored3 = await revisions.GetByRevisionAsync("company-1", "214748364700", SummaryDate, 1, 3);
        Assert.NotNull(stored2);
        Assert.NotNull(stored3);
        Assert.Equal(3, revision3.LocalRevision);
        Assert.Equal(1, revision3.Sequence);
        Assert.Equal(revision2.RevisionId, revision3.PreviousRevisionId);
        Assert.Equal(root.Id, revision3.RootSubmissionId);
        Assert.NotEqual(stored2!.SignedArtifactId, stored3!.SignedArtifactId);
        Assert.NotEqual(stored2.SigningEvidenceId, stored3.SigningEvidenceId);
        Assert.Equal(2, signer.Calls);
    }

    [Fact]
    public async Task Accepted_correction_is_terminal_and_dispatch_replay_never_calls_gateway_twice()
    {
        var root = Root(FiscalDailyReportSubmissionState.Rejected, "BR", BrAck("R04", "Corregir datos"));
        var submissions = new SubmissionRepository(root);
        var revisions = new RevisionRepository();
        var clock = new FixedTransportClock(DateTimeOffset.UtcNow);
        await Prepare(submissions, revisions, new CountingSigner(), clock).ExecuteAsync(
            new PrepareFiscalDailyReportBrCorrectionCommand(Projection(), "br-terminal", "fix"));

        var gateway = new FakeGateway(new FiscalDailyReportTransportResponse("AR", "receiver-ar", ArAck()));
        var sut = Dispatch(submissions, revisions, gateway, clock);
        var first = await sut.ExecuteAsync(new DispatchFiscalDailyReportBrCorrectionCommand(
            "company-1", "214748364700", SummaryDate, 1, 2));
        var replay = await sut.ExecuteAsync(new DispatchFiscalDailyReportBrCorrectionCommand(
            "company-1", "214748364700", SummaryDate, 1, 2));

        Assert.Equal(FiscalDailyReportSubmissionState.Received, first.State);
        Assert.Equal("AR", first.AckStateCode);
        Assert.True(replay.Replayed);
        Assert.Equal(1, gateway.Calls);
    }

    private static PrepareFiscalDailyReportBrCorrectionUseCase Prepare(
        IFiscalDailyReportSubmissionRepository submissions,
        IFiscalDailyReportBrCorrectionRepository revisions,
        IFiscalDailyReportSignatureProvider signer,
        IFiscalDailyReportTransportClock? transportClock = null) =>
        new(
            submissions,
            revisions,
            new DgiFiscalDailyReportBrAckEvidenceParser(),
            new DeterministicUnsignedDailyReportXmlBuilder(),
            signer,
            new AlwaysValidSchemaValidator(),
            new FixedSigningTime(new DateTimeOffset(2026, 9, 12, 15, 30, 0, TimeSpan.FromHours(-3))),
            transportClock ?? new FixedTransportClock(new DateTimeOffset(2026, 9, 12, 18, 30, 0, TimeSpan.Zero)),
            new InlineTransactionManager(),
            new CountingUnitOfWork(),
            new NoOpAuditWriter(),
            new NoOpOutboxWriter(),
            new FixedActorContextAccessor(),
            new FixedCorrelationContextAccessor());

    private static DispatchFiscalDailyReportBrCorrectionUseCase Dispatch(
        IFiscalDailyReportSubmissionRepository submissions,
        IFiscalDailyReportBrCorrectionRepository revisions,
        IFiscalDailyReportTransportGateway gateway,
        IFiscalDailyReportTransportClock clock) =>
        new(
            submissions,
            revisions,
            new DgiFiscalDailyReportBrAckEvidenceParser(),
            gateway,
            clock,
            new InlineTransactionManager(),
            new CountingUnitOfWork(),
            new NoOpAuditWriter(),
            new NoOpOutboxWriter(),
            new FixedActorContextAccessor(),
            new FixedCorrelationContextAccessor());

    private static StoredFiscalDailyReportSubmission Root(
        FiscalDailyReportSubmissionState state,
        string? ackState,
        string? ackXml) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "company-1",
            "214748364700",
            SummaryDate,
            1,
            "root-operation",
            Fingerprint('9'),
            state,
            1,
            new DateTimeOffset(2026, 9, 12, 18, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 12, 18, 1, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 12, 18, 2, 0, TimeSpan.Zero),
            "root-receiver",
            ackState,
            ackXml,
            null);

    private static FiscalDailyReportWireProjection Projection()
    {
        var row = new FiscalDailyReportWireAmountRow(
            CfeFamily.EFactura,
            SummaryDate,
            "0001",
            false,
            0m, 0m, 0m, 0m, 0m, 100m, 0m, 0m, 22m, 0m,
            null, 22m, 122m, 0m, 0m);
        var counter = new FiscalDailyReportWireTypeCounters(
            CfeFamily.EFactura,
            1,
            0,
            0,
            1,
            [new FiscalDailyReportNumberRange("A", 1, 1)],
            Array.Empty<FiscalDailyReportNumberRange>());
        return new FiscalDailyReportWireProjection(
            "company-1",
            "214748364700",
            SummaryDate,
            1,
            1,
            [row],
            [counter],
            Fingerprint('a'),
            Fingerprint('b'),
            Fingerprint('c'));
    }

    private static string BrAck(string code, string glosa) =>
        $"<ACKRepDiario xmlns=\"http://cfe.dgi.gub.uy\"><Caratula><IDReceptor>receiver</IDReceptor></Caratula><Detalle><Estado>BR</Estado><MotivosRechazo><Motivo>{code}</Motivo><Glosa>{glosa}</Glosa></MotivosRechazo></Detalle></ACKRepDiario>";

    private static string ArAck() =>
        "<ACKRepDiario xmlns=\"http://cfe.dgi.gub.uy\"><Caratula><IDReceptor>receiver</IDReceptor></Caratula><Detalle><Estado>AR</Estado></Detalle></ACKRepDiario>";

    private static string Fingerprint(char value) => new(value, 64);

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class SubmissionRepository(params StoredFiscalDailyReportSubmission[] values)
        : IFiscalDailyReportSubmissionRepository
    {
        private readonly List<StoredFiscalDailyReportSubmission> _values = [.. values];

        public Task<StoredFiscalDailyReportSubmission?> GetByOperationIdAsync(
            string organizationId, string operationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.SingleOrDefault(x => x.OrganizationId == organizationId && x.OperationId == operationId));

        public Task<StoredFiscalDailyReportSubmission?> GetByIdentityAsync(
            string organizationId, string issuerRuc, DateOnly summaryDate, int sequence,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.SingleOrDefault(x =>
                x.OrganizationId == organizationId && x.IssuerRuc == issuerRuc
                && x.SummaryDate == summaryDate && x.Sequence == sequence));

        public Task AddAsync(StoredFiscalDailyReportSubmission submission, CancellationToken cancellationToken = default)
        {
            _values.Add(submission);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(StoredFiscalDailyReportSubmission submission, CancellationToken cancellationToken = default)
        {
            var index = _values.FindIndex(x => x.Id == submission.Id);
            if (index < 0) throw new InvalidOperationException("missing submission");
            _values[index] = submission;
            return Task.CompletedTask;
        }
    }

    private sealed class RevisionRepository : IFiscalDailyReportBrCorrectionRepository
    {
        private readonly List<StoredFiscalDailyReportBrCorrectionRevision> _values = [];

        public Task<StoredFiscalDailyReportBrCorrectionRevision?> GetByOperationIdAsync(
            string organizationId, string operationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.SingleOrDefault(x => x.OrganizationId == organizationId && x.OperationId == operationId));

        public Task<StoredFiscalDailyReportBrCorrectionRevision?> GetLatestAsync(
            string organizationId, string issuerRuc, DateOnly summaryDate, int sequence,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_values
                .Where(x => x.OrganizationId == organizationId && x.IssuerRuc == issuerRuc
                    && x.SummaryDate == summaryDate && x.Sequence == sequence)
                .OrderByDescending(x => x.LocalRevision)
                .FirstOrDefault());

        public Task<StoredFiscalDailyReportBrCorrectionRevision?> GetByRevisionAsync(
            string organizationId, string issuerRuc, DateOnly summaryDate, int sequence, int localRevision,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.SingleOrDefault(x =>
                x.OrganizationId == organizationId && x.IssuerRuc == issuerRuc
                && x.SummaryDate == summaryDate && x.Sequence == sequence && x.LocalRevision == localRevision));

        public Task AddAsync(StoredFiscalDailyReportBrCorrectionRevision revision, CancellationToken cancellationToken = default)
        {
            _values.Add(revision);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(StoredFiscalDailyReportBrCorrectionRevision revision, CancellationToken cancellationToken = default)
        {
            var index = _values.FindIndex(x => x.Id == revision.Id);
            if (index < 0) throw new InvalidOperationException("missing revision");
            _values[index] = revision;
            return Task.CompletedTask;
        }
    }

    private sealed class CountingSigner : IFiscalDailyReportSignatureProvider
    {
        public int Calls { get; private set; }

        public Task<FiscalDailyReportSignatureResult> SignAsync(
            FiscalDailyReportSignatureRequest request,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            var signed = request.UnsignedXml + $"<!--test-signature-{Calls}-->";
            return Task.FromResult(new FiscalDailyReportSignatureResult(
                signed,
                Sha256(signed),
                "test-report-profile",
                "thumbprint",
                "serial"));
        }
    }

    private sealed class AlwaysValidSchemaValidator : IFiscalDailyReportSignedSchemaValidator
    {
        public FiscalDailyReportSignedSchemaValidationResult Validate(string signedXml) =>
            new(
                FiscalDailyReportSignedSchemaValidationStatus.Valid,
                "dgi-fe-1.44.2-reporte-13.2",
                "13.2",
                "1.44.2",
                Fingerprint('e'),
                Array.Empty<FiscalDailyReportSignedSchemaValidationError>());
    }

    private sealed class FixedSigningTime(DateTimeOffset value) : IFiscalSigningTimeSource
    {
        public DateTimeOffset GetSigningTimestamp() => value;
    }

    private sealed class FixedTransportClock(DateTimeOffset value) : IFiscalDailyReportTransportClock
    {
        public DateTimeOffset UtcNow => value.ToUniversalTime();
    }

    private sealed class FakeGateway(params object[] results) : IFiscalDailyReportTransportGateway
    {
        private readonly Queue<object> _results = new(results);
        public int Calls { get; private set; }

        public Task<FiscalDailyReportTransportResponse> SendAsync(
            FiscalDailyReportTransportRequest request,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            var next = _results.Dequeue();
            return next switch
            {
                FiscalDailyReportTransportResponse response => Task.FromResult(response),
                Exception error => Task.FromException<FiscalDailyReportTransportResponse>(error),
                _ => throw new InvalidOperationException()
            };
        }
    }

    private sealed class InlineTransactionManager : ITransactionManager
    {
        public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
    }

    private sealed class CountingUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class NoOpAuditWriter : IAuditWriter
    {
        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpOutboxWriter : IOutboxWriter
    {
        public Task EnqueueAsync<TEvent>(TEvent integrationEvent, OutboxContext context, CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent => Task.CompletedTask;
    }

    private sealed class FixedActorContextAccessor : IActorContextAccessor
    {
        public ActorContext Current { get; } = new(
            "actor-1", "Test Actor", true,
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal) { "company-1" },
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            "device-1");
    }

    private sealed class FixedCorrelationContextAccessor : ICorrelationContextAccessor
    {
        public CorrelationContext Current { get; } = new("corr-br-correction", "trace-br-correction");
    }
}
