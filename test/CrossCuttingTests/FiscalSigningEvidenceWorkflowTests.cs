using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Taxation;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalSigningEvidenceWorkflowTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string SettlementFingerprint = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public async Task First_prepare_establishes_timestamp_once_and_replay_reuses_it_without_clock_access()
    {
        var fixture = Fixture();
        var evidence = new InMemorySigningEvidenceRepository();
        var clock = new CountingSigningTimeSource(
            new DateTimeOffset(2026, 9, 9, 13, 14, 15, 987, TimeSpan.FromHours(-3)));
        var builder = new MutableXmlBuilder("<CFE><eFact /></CFE>");
        var sut = UseCase(fixture, builder, evidence, clock);

        var first = await sut.ExecuteAsync(
            new PrepareFiscalSigningEvidenceCommand("company-1", fixture.Document.Id));
        var replay = await sut.ExecuteAsync(
            new PrepareFiscalSigningEvidenceCommand("company-1", fixture.Document.Id));

        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(first.SigningEvidenceId, replay.SigningEvidenceId);
        Assert.Equal(first.SigningTimestamp, replay.SigningTimestamp);
        Assert.Equal(new DateTimeOffset(2026, 9, 9, 13, 14, 15, TimeSpan.FromHours(-3)), replay.SigningTimestamp);
        Assert.Equal(first.UnsignedContentHash, replay.UnsignedContentHash);
        Assert.Equal(1, clock.Calls);
    }

    [Fact]
    public async Task Replay_with_different_unsigned_bytes_fails_closed_without_creating_new_timestamp()
    {
        var fixture = Fixture();
        var evidence = new InMemorySigningEvidenceRepository();
        var clock = new CountingSigningTimeSource(
            new DateTimeOffset(2026, 9, 9, 13, 14, 15, TimeSpan.FromHours(-3)));
        var builder = new MutableXmlBuilder("<CFE><eFact /></CFE>");
        var sut = UseCase(fixture, builder, evidence, clock);

        await sut.ExecuteAsync(
            new PrepareFiscalSigningEvidenceCommand("company-1", fixture.Document.Id));
        builder.Xml = "<CFE><eFact><Changed /></eFact></CFE>";

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() => sut.ExecuteAsync(
            new PrepareFiscalSigningEvidenceCommand("company-1", fixture.Document.Id)));

        Assert.Equal("fiscal.signing.replay_evidence_mismatch", error.Code);
        Assert.Equal(1, clock.Calls);
    }

    private static PrepareFiscalSigningEvidenceUseCase UseCase(
        SigningFixture fixture,
        IFiscalXmlBuilder builder,
        IFiscalSigningEvidenceRepository signingEvidence,
        IFiscalSigningTimeSource clock) =>
        new(
            new FixedFiscalDocumentRepository(fixture.Document),
            new FixedFiscalSnapshotRepository(fixture.StoredSnapshot),
            builder,
            signingEvidence,
            clock,
            new InlineTransactionManager(),
            new CountingUnitOfWork(),
            new NoOpAuditWriter(),
            new NoOpOutboxWriter(),
            new FixedActorContextAccessor(),
            new FixedCorrelationContextAccessor());

    private static SigningFixture Fixture()
    {
        var saleId = Guid.Parse("50000000-0000-0000-0000-000000000001");
        var lineId = Guid.Parse("50000000-0000-0000-0000-000000000002");
        var documentId = Guid.Parse("50000000-0000-0000-0000-000000000003");
        var requestId = Guid.Parse("50000000-0000-0000-0000-000000000004");
        var rule = new RegulatoryRuleEvidence(
            "TEST-SIGN-RULE",
            "DGI test evidence",
            "https://example.invalid/dgi-rule",
            "25.2-test",
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            "test clause");
        var calculation = new CfeArithmeticResult(
            "UYU",
            "25.2",
            "UY-CFE-25.2-ARITH-R1",
            new[]
            {
                new CfeArithmeticLineResult(
                    lineId,
                    100m,
                    VatLiabilityKind.VatDue,
                    VatRateKind.Basic,
                    22m,
                    new[] { rule },
                    "UY-VAT-RATE-R1")
            },
            new CfeArithmeticTotals(100m, 0m, 100m, 0m, 0m, 22m, 22m, 122m),
            new[] { rule });
        var settlement = new FiscalSettlementEvidence(
            FiscalSettlementKind.ImmediatePayment,
            FiscalPaymentForm.Cash,
            null);
        var fiscal = FiscalConfirmationEvidence.Capture(
            CfeFamily.EFactura,
            ReceiverIdentificationRequirement.Required,
            "25.2",
            Confirmation,
            calculation,
            new[] { rule },
            settlement);
        var issuer = new FiscalIssuerContentSnapshot(
            "214748364700",
            "Empresa Snapshot S.A.",
            "Empresa Snapshot",
            3,
            "loc-1",
            "0001",
            "Av. Italia 1234",
            "Montevideo",
            "Montevideo",
            4);
        var receiver = new FiscalReceiverContentSnapshot(
            Guid.Parse("50000000-0000-0000-0000-000000000005"),
            "Cliente Snapshot S.A.",
            "UY",
            "UY",
            5,
            new FiscalReceiverIdentitySnapshot("2", "219999990017", "UY"),
            new FiscalReceiverAddressSnapshot(
                Guid.Parse("50000000-0000-0000-0000-000000000006"),
                FiscalReceiverAddressKind.Fiscal,
                "18 de Julio 2000",
                "Montevideo",
                "Montevideo",
                "UY",
                "11200"));
        var snapshot = FiscalContentSnapshot.Create(
            "company-1",
            saleId,
            CfeFamily.EFactura,
            ReceiverIdentificationRequirement.Required,
            "25.2",
            new DateOnly(2026, 9, 9),
            Confirmation,
            SettlementFingerprint,
            issuer,
            receiver,
            new[]
            {
                new FiscalContentLineSnapshot(
                    1,
                    lineId,
                    Guid.Parse("50000000-0000-0000-0000-000000000007"),
                    "ITEM-001",
                    "Producto confirmado",
                    FiscalContentLineKind.Goods,
                    1m,
                    100m,
                    0m,
                    0m,
                    fiscal.Lines.Single(),
                    "UNI")
            },
            fiscal);
        var document = FiscalDocument.CreateIdentity(
            documentId,
            "company-1",
            requestId,
            saleId,
            Guid.Parse("50000000-0000-0000-0000-000000000008"),
            Guid.Parse("50000000-0000-0000-0000-000000000009"),
            null,
            CfeFamily.EFactura,
            "A",
            100,
            "90260000001",
            1,
            1000,
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            new DateOnly(2026, 9, 9),
            "loc-1",
            "terminal-1",
            ReceiverIdentificationRequirement.Required,
            "25.2",
            Confirmation,
            SettlementFingerprint,
            "UYU",
            100m,
            22m,
            122m,
            new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.FromHours(-3)));
        var stored = new StoredFiscalContentSnapshot(
            Guid.Parse("50000000-0000-0000-0000-000000000010"),
            "company-1",
            documentId,
            requestId,
            saleId,
            snapshot,
            new DateTimeOffset(2026, 9, 9, 12, 1, 0, TimeSpan.FromHours(-3)));
        return new SigningFixture(document, stored);
    }

    private sealed record SigningFixture(
        FiscalDocument Document,
        StoredFiscalContentSnapshot StoredSnapshot);

    private sealed class FixedFiscalDocumentRepository : IFiscalDocumentRepository
    {
        private readonly FiscalDocument _document;
        public FixedFiscalDocumentRepository(FiscalDocument document) => _document = document;

        public Task<FiscalDocument?> GetAsync(string organizationId, Guid fiscalDocumentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<FiscalDocument?>(
                organizationId == _document.OrganizationId && fiscalDocumentId == _document.Id ? _document : null);

        public Task<FiscalDocument?> GetByFiscalizationRequestAsync(string organizationId, Guid fiscalizationRequestId, CancellationToken cancellationToken = default) =>
            Task.FromResult<FiscalDocument?>(
                organizationId == _document.OrganizationId && fiscalizationRequestId == _document.FiscalizationRequestId ? _document : null);

        public Task AddAsync(FiscalDocument document, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FixedFiscalSnapshotRepository : IFiscalContentSnapshotRepository
    {
        private readonly StoredFiscalContentSnapshot _snapshot;
        public FixedFiscalSnapshotRepository(StoredFiscalContentSnapshot snapshot) => _snapshot = snapshot;

        public Task<StoredFiscalContentSnapshot?> GetByFiscalDocumentAsync(string organizationId, Guid fiscalDocumentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<StoredFiscalContentSnapshot?>(
                organizationId == _snapshot.OrganizationId && fiscalDocumentId == _snapshot.FiscalDocumentId ? _snapshot : null);

        public Task AddAsync(StoredFiscalContentSnapshot snapshot, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MutableXmlBuilder : IFiscalXmlBuilder
    {
        public MutableXmlBuilder(string xml) => Xml = xml;
        public string Xml { get; set; }

        public UnsignedCfeArtifact Build(FiscalDocument document, FiscalContentSnapshot snapshot) =>
            new(document.CfeType, snapshot.FormatVersion, snapshot.ContentFingerprint, Xml);
    }

    private sealed class InMemorySigningEvidenceRepository : IFiscalSigningEvidenceRepository
    {
        private FiscalSigningEvidence? _evidence;

        public Task<FiscalSigningEvidence?> GetByFiscalDocumentAsync(string organizationId, Guid fiscalDocumentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _evidence is not null
                && _evidence.OrganizationId == organizationId
                && _evidence.FiscalDocumentId == fiscalDocumentId
                    ? _evidence
                    : null);

        public Task AddAsync(FiscalSigningEvidence evidence, CancellationToken cancellationToken = default)
        {
            _evidence = evidence;
            return Task.CompletedTask;
        }
    }

    private sealed class CountingSigningTimeSource : IFiscalSigningTimeSource
    {
        private readonly DateTimeOffset _value;
        public CountingSigningTimeSource(DateTimeOffset value) => _value = value;
        public int Calls { get; private set; }

        public DateTimeOffset GetSigningTimestamp()
        {
            Calls++;
            return _value;
        }
    }

    private sealed class InlineTransactionManager : ITransactionManager
    {
        public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);

        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);
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
            new HashSet<string>(StringComparer.Ordinal) { "loc-1" },
            new HashSet<string>(StringComparer.Ordinal) { "terminal-1" },
            "device-1");
    }

    private sealed class FixedCorrelationContextAccessor : ICorrelationContextAccessor
    {
        public CorrelationContext Current { get; } = new("corr-signing", "trace-signing");
    }
}
