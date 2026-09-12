using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalDailyReportTransportTests
{
    private static readonly DateOnly SummaryDate = new(2026, 9, 11);

    [Fact]
    public async Task AR_is_persisted_and_dispatch_replay_never_calls_gateway_twice()
    {
        var artifacts = new ArtifactRepository(Artifact(1));
        var submissions = new SubmissionRepository();
        var clock = new FixedClock(new DateTimeOffset(2026, 9, 12, 3, 0, 0, TimeSpan.Zero));
        var gateway = new FakeGateway(new FiscalDailyReportTransportResponse("AR", "receiver-1", Ack("AR", "receiver-1")));

        var prepared = await Prepare(artifacts, submissions, clock).ExecuteAsync(Command(1, "op-1"));
        var first = await Dispatch(artifacts, submissions, gateway, clock).ExecuteAsync(DispatchCommand(1));
        var replay = await Dispatch(artifacts, submissions, gateway, clock).ExecuteAsync(DispatchCommand(1));

        Assert.False(prepared.Replayed);
        Assert.Equal(FiscalDailyReportSubmissionState.Received, first.State);
        Assert.Equal("AR", first.AckStateCode);
        Assert.Equal("receiver-1", first.DgiReceiverId);
        Assert.True(replay.Replayed);
        Assert.Equal(1, gateway.Calls);
    }

    [Fact]
    public async Task BR_is_persisted_and_never_blindly_resent()
    {
        var artifacts = new ArtifactRepository(Artifact(1));
        var submissions = new SubmissionRepository();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var gateway = new FakeGateway(new FiscalDailyReportTransportResponse("BR", "receiver-br", Ack("BR", "receiver-br")));

        await Prepare(artifacts, submissions, clock).ExecuteAsync(Command(1, "op-br"));
        var rejected = await Dispatch(artifacts, submissions, gateway, clock).ExecuteAsync(DispatchCommand(1));
        var replay = await Dispatch(artifacts, submissions, gateway, clock).ExecuteAsync(DispatchCommand(1));

        Assert.Equal(FiscalDailyReportSubmissionState.Rejected, rejected.State);
        Assert.Equal("BR", rejected.AckStateCode);
        Assert.True(replay.Replayed);
        Assert.Equal(1, gateway.Calls);
    }

    [Fact]
    public async Task Sequence_two_is_blocked_until_sequence_one_has_durable_AR()
    {
        var artifacts = new ArtifactRepository(Artifact(2));
        var submissions = new SubmissionRepository();
        var clock = new FixedClock(DateTimeOffset.UtcNow);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            Prepare(artifacts, submissions, clock).ExecuteAsync(Command(2, "op-2")));

        Assert.Equal("fiscal.daily_report.transport.previous_sequence_not_received", error.Code);

        await submissions.AddAsync(new StoredFiscalDailyReportSubmission(
            Guid.NewGuid(), Guid.NewGuid(), "company-1", "214748364700", SummaryDate, 1,
            "prior-op", Hash('1'), FiscalDailyReportSubmissionState.Received, 1,
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
            "receiver-1", "AR", Ack("AR", "receiver-1"), null));

        var prepared = await Prepare(artifacts, submissions, clock).ExecuteAsync(Command(2, "op-2"));
        Assert.Equal(FiscalDailyReportSubmissionState.Prepared, prepared.State);
    }

    [Fact]
    public async Task Ambiguous_delivery_becomes_Unknown_and_requires_reconciliation_before_retry()
    {
        var artifacts = new ArtifactRepository(Artifact(1));
        var submissions = new SubmissionRepository();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var gateway = new FakeGateway(new FiscalDailyReportTransportException(
            "transport.timeout", "timeout", deliveryAmbiguous: true));

        await Prepare(artifacts, submissions, clock).ExecuteAsync(Command(1, "op-unknown"));
        var unknown = await Dispatch(artifacts, submissions, gateway, clock).ExecuteAsync(DispatchCommand(1));
        Assert.Equal(FiscalDailyReportSubmissionState.Unknown, unknown.State);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            Dispatch(artifacts, submissions, gateway, clock).ExecuteAsync(DispatchCommand(1)));
        Assert.Equal("fiscal.daily_report.transport.reconciliation_required", error.Code);
        Assert.Equal(1, gateway.Calls);
    }

    [Fact]
    public async Task Failure_known_before_delivery_returns_to_Prepared_and_can_retry()
    {
        var artifacts = new ArtifactRepository(Artifact(1));
        var submissions = new SubmissionRepository();
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var gateway = new FakeGateway(
            new FiscalDailyReportTransportException("transport.config", "config", deliveryAmbiguous: false),
            new FiscalDailyReportTransportResponse("AR", "receiver-2", Ack("AR", "receiver-2")));

        await Prepare(artifacts, submissions, clock).ExecuteAsync(Command(1, "op-retry"));
        var reset = await Dispatch(artifacts, submissions, gateway, clock).ExecuteAsync(DispatchCommand(1));
        var received = await Dispatch(artifacts, submissions, gateway, clock).ExecuteAsync(DispatchCommand(1));

        Assert.Equal(FiscalDailyReportSubmissionState.Prepared, reset.State);
        Assert.Equal("transport.config", reset.FailureCode);
        Assert.Equal(FiscalDailyReportSubmissionState.Received, received.State);
        Assert.Equal(2, gateway.Calls);
        Assert.Equal(2, received.AttemptCount);
    }

    private static PrepareFiscalDailyReportSubmissionUseCase Prepare(
        IFiscalDailyReportSignedArtifactRepository artifacts,
        IFiscalDailyReportSubmissionRepository submissions,
        IFiscalDailyReportTransportClock clock) =>
        new(
            artifacts,
            submissions,
            clock,
            new InlineTransactionManager(),
            new CountingUnitOfWork(),
            new NoOpAuditWriter(),
            new NoOpOutboxWriter(),
            new FixedActorContextAccessor(),
            new FixedCorrelationContextAccessor());

    private static DispatchFiscalDailyReportSubmissionUseCase Dispatch(
        IFiscalDailyReportSignedArtifactRepository artifacts,
        IFiscalDailyReportSubmissionRepository submissions,
        IFiscalDailyReportTransportGateway gateway,
        IFiscalDailyReportTransportClock clock) =>
        new(
            artifacts,
            submissions,
            gateway,
            clock,
            new InlineTransactionManager(),
            new CountingUnitOfWork(),
            new NoOpAuditWriter(),
            new NoOpOutboxWriter(),
            new FixedActorContextAccessor(),
            new FixedCorrelationContextAccessor());

    private static PrepareFiscalDailyReportSubmissionCommand Command(int sequence, string operationId) =>
        new("company-1", "214748364700", SummaryDate, sequence, operationId);

    private static DispatchFiscalDailyReportSubmissionCommand DispatchCommand(int sequence) =>
        new("company-1", "214748364700", SummaryDate, sequence);

    private static StoredFiscalDailyReportSignedArtifact Artifact(int sequence) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "company-1",
            "214748364700",
            SummaryDate,
            sequence,
            "13.2",
            Hash('a'),
            Hash('b'),
            Hash((char)('c' + sequence)),
            new DateTimeOffset(2026, 9, 12, 0, 0, sequence, TimeSpan.FromHours(-3)),
            "report-profile",
            "thumbprint",
            "serial",
            "schema-set",
            "13.2",
            "1.44.2",
            Hash('e'),
            $"<Reporte sequence=\"{sequence}\" />");

    private static string Ack(string state, string receiver) =>
        $"<ACKRepDiario xmlns=\"http://cfe.dgi.gub.uy\"><Caratula><IDReceptor>{receiver}</IDReceptor></Caratula><Detalle><Estado>{state}</Estado></Detalle></ACKRepDiario>";

    private static string Hash(char value) => new(value, 64);

    private sealed class ArtifactRepository(params StoredFiscalDailyReportSignedArtifact[] values) : IFiscalDailyReportSignedArtifactRepository
    {
        private readonly List<StoredFiscalDailyReportSignedArtifact> _values = [.. values];

        public Task<StoredFiscalDailyReportSignedArtifact?> GetByIdentityAsync(
            string organizationId, string issuerRuc, DateOnly summaryDate, int sequence,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.SingleOrDefault(x =>
                x.OrganizationId == organizationId && x.IssuerRuc == issuerRuc
                && x.SummaryDate == summaryDate && x.Sequence == sequence));

        public Task AddAsync(StoredFiscalDailyReportSignedArtifact artifact, CancellationToken cancellationToken = default)
        {
            _values.Add(artifact);
            return Task.CompletedTask;
        }
    }

    private sealed class SubmissionRepository : IFiscalDailyReportSubmissionRepository
    {
        private readonly List<StoredFiscalDailyReportSubmission> _values = [];

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

    private sealed class FakeGateway : IFiscalDailyReportTransportGateway
    {
        private readonly Queue<object> _results;
        public int Calls { get; private set; }

        public FakeGateway(params object[] results) => _results = new Queue<object>(results);

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

    private sealed class FixedClock(DateTimeOffset now) : IFiscalDailyReportTransportClock
    {
        public DateTimeOffset UtcNow => now.ToUniversalTime();
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
        public CorrelationContext Current { get; } = new("corr-report-transport", "trace-report-transport");
    }
}
