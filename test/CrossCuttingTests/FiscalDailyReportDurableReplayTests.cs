using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalDailyReportDurableReplayTests
{
    [Fact]
    public async Task Prepare_freezes_whole_second_timestamp_once_and_replay_never_reads_clock_again()
    {
        var evidence = new InMemoryEvidenceRepository();
        var clock = new CountingClock(new DateTimeOffset(2026, 9, 12, 0, 15, 31, 987, TimeSpan.FromHours(-3)));
        var sut = Prepare(evidence, clock);
        var projection = Projection();

        var first = await sut.ExecuteAsync(new PrepareFiscalDailyReportSigningEvidenceCommand(projection));
        var replay = await sut.ExecuteAsync(new PrepareFiscalDailyReportSigningEvidenceCommand(projection));

        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(first.SigningEvidenceId, replay.SigningEvidenceId);
        Assert.Equal(new DateTimeOffset(2026, 9, 12, 0, 15, 31, TimeSpan.FromHours(-3)), replay.SigningTimestamp);
        Assert.Equal(1, clock.Calls);
    }

    [Fact]
    public async Task Changed_projection_fingerprint_fails_closed_on_evidence_replay_without_new_clock_access()
    {
        var evidence = new InMemoryEvidenceRepository();
        var clock = new CountingClock(new DateTimeOffset(2026, 9, 12, 0, 15, 31, TimeSpan.FromHours(-3)));
        var sut = Prepare(evidence, clock);
        var projection = Projection();
        await sut.ExecuteAsync(new PrepareFiscalDailyReportSigningEvidenceCommand(projection));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() => sut.ExecuteAsync(
            new PrepareFiscalDailyReportSigningEvidenceCommand(
                projection with { ProjectionFingerprint = Fingerprint('d') })));

        Assert.Equal("fiscal.daily_report.signing_evidence.replay_mismatch", error.Code);
        Assert.Equal(1, clock.Calls);
    }

    [Fact]
    public async Task Signed_artifact_replay_returns_persisted_result_without_crossing_signer_again()
    {
        var evidence = new InMemoryEvidenceRepository();
        var artifacts = new InMemoryArtifactRepository();
        var clock = new CountingClock(new DateTimeOffset(2026, 9, 12, 0, 15, 31, TimeSpan.FromHours(-3)));
        var projection = Projection();
        await Prepare(evidence, clock).ExecuteAsync(new PrepareFiscalDailyReportSigningEvidenceCommand(projection));
        var signer = new CountingSigner();
        var sut = Sign(evidence, artifacts, signer);

        var first = await sut.ExecuteAsync(new SignFiscalDailyReportCommand(projection));
        var replay = await sut.ExecuteAsync(new SignFiscalDailyReportCommand(projection));

        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(first.SignedArtifactId, replay.SignedArtifactId);
        Assert.Equal(first.SignedContentHash, replay.SignedContentHash);
        Assert.Equal(1, signer.Calls);
    }

    private static PrepareFiscalDailyReportSigningEvidenceUseCase Prepare(
        IFiscalDailyReportSigningEvidenceRepository evidence,
        IFiscalSigningTimeSource clock) =>
        new(
            new DeterministicUnsignedDailyReportXmlBuilder(),
            evidence,
            clock,
            new InlineTransactionManager(),
            new CountingUnitOfWork(),
            new NoOpAuditWriter(),
            new NoOpOutboxWriter(),
            new FixedActorContextAccessor(),
            new FixedCorrelationContextAccessor());

    private static SignFiscalDailyReportUseCase Sign(
        IFiscalDailyReportSigningEvidenceRepository evidence,
        IFiscalDailyReportSignedArtifactRepository artifacts,
        IFiscalDailyReportSignatureProvider signer) =>
        new(
            new DeterministicUnsignedDailyReportXmlBuilder(),
            evidence,
            artifacts,
            signer,
            new AlwaysValidSchemaValidator(),
            new InlineTransactionManager(),
            new CountingUnitOfWork(),
            new NoOpAuditWriter(),
            new NoOpOutboxWriter(),
            new FixedActorContextAccessor(),
            new FixedCorrelationContextAccessor());

    private static FiscalDailyReportWireProjection Projection()
    {
        var row = new FiscalDailyReportWireAmountRow(
            CfeFamily.EFactura,
            new DateOnly(2026, 9, 11),
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
            new DateOnly(2026, 9, 11),
            1,
            1,
            [row],
            [counter],
            Fingerprint('a'),
            Fingerprint('b'),
            Fingerprint('c'));
    }

    private static string Fingerprint(char value) => new(value, 64);
    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class InMemoryEvidenceRepository : IFiscalDailyReportSigningEvidenceRepository
    {
        private StoredFiscalDailyReportSigningEvidence? _value;

        public Task<StoredFiscalDailyReportSigningEvidence?> GetByIdentityAsync(
            string organizationId,
            string issuerRuc,
            DateOnly summaryDate,
            int sequence,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _value is not null
                && _value.OrganizationId == organizationId
                && _value.IssuerRuc == issuerRuc
                && _value.SummaryDate == summaryDate
                && _value.Sequence == sequence
                    ? _value
                    : null);

        public Task AddAsync(StoredFiscalDailyReportSigningEvidence evidence, CancellationToken cancellationToken = default)
        {
            _value = evidence;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryArtifactRepository : IFiscalDailyReportSignedArtifactRepository
    {
        private StoredFiscalDailyReportSignedArtifact? _value;

        public Task<StoredFiscalDailyReportSignedArtifact?> GetByIdentityAsync(
            string organizationId,
            string issuerRuc,
            DateOnly summaryDate,
            int sequence,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _value is not null
                && _value.OrganizationId == organizationId
                && _value.IssuerRuc == issuerRuc
                && _value.SummaryDate == summaryDate
                && _value.Sequence == sequence
                    ? _value
                    : null);

        public Task AddAsync(StoredFiscalDailyReportSignedArtifact artifact, CancellationToken cancellationToken = default)
        {
            _value = artifact;
            return Task.CompletedTask;
        }
    }

    private sealed class CountingClock(DateTimeOffset value) : IFiscalSigningTimeSource
    {
        public int Calls { get; private set; }
        public DateTimeOffset GetSigningTimestamp()
        {
            Calls++;
            return value;
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
            var signed = request.UnsignedXml + "<!--test-signature-envelope-->";
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
            "actor-1",
            "Test Actor",
            true,
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal) { "company-1" },
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            "device-1");
    }

    private sealed class FixedCorrelationContextAccessor : ICorrelationContextAccessor
    {
        public CorrelationContext Current { get; } = new("corr-report-replay", "trace-report-replay");
    }
}
