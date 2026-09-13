using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using Infrastructure.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalCfeEnvelopeDocumentResponseConsultationTests
{
    [Fact]
    public async Task Accepted_ACK_token_queries_partial_ACKCFE_append_only_and_replays_without_second_call()
    {
        var source = Source(FiscalCfeEnvelopeAckState.Received, includeToken: true);
        var consultations = new ConsultationRepository();
        var responseXml = AckCfe(source, respondedCount: 1);
        var gateway = new FakeGateway(ParseResponse(Wrap(responseXml)));
        var useCase = UseCase(source, consultations, gateway);

        var command = new ConsultFiscalCfeEnvelopeDocumentResponseCommand(
            source.Envelope.OrganizationId,
            source.Envelope.IssuerRuc,
            source.Envelope.ReceiverRut,
            source.Envelope.SenderEnvelopeId,
            "ackcfe-op-1");
        var first = await useCase.ExecuteAsync(command);
        var replay = await useCase.ExecuteAsync(command);

        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(first.ConsultationId, replay.ConsultationId);
        Assert.Equal(2, first.EnvelopeCfeCount);
        Assert.Equal(1, first.RespondedCount);
        Assert.Single(first.Details);
        Assert.Equal(101, first.Details[0].CfeType);
        Assert.Equal("A", first.Details[0].Series);
        Assert.Equal(123, first.Details[0].Number);
        Assert.Equal("A", first.Details[0].StateCode);
        Assert.Equal(1, gateway.Calls);
        Assert.Single(consultations.Values);
        Assert.All(source.Documents, document => Assert.Equal(FiscalDocumentStatus.IdentityCreated, document.Status));
    }

    [Theory]
    [InlineData(FiscalCfeEnvelopeAckState.Rejected, true)]
    [InlineData(FiscalCfeEnvelopeAckState.Received, false)]
    public async Task Rejected_ACK_or_missing_token_fails_before_network(
        FiscalCfeEnvelopeAckState state,
        bool includeToken)
    {
        var source = Source(state, includeToken);
        var gateway = new FakeGateway(ParseResponse(Wrap(AckCfe(source, 1))));
        var useCase = UseCase(source, new ConsultationRepository(), gateway);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(new ConsultFiscalCfeEnvelopeDocumentResponseCommand(
                source.Envelope.OrganizationId,
                source.Envelope.IssuerRuc,
                source.Envelope.ReceiverRut,
                source.Envelope.SenderEnvelopeId,
                "blocked-op")));

        Assert.Contains("fiscal.envelope.document_response.", error.Code, StringComparison.Ordinal);
        Assert.Equal(0, gateway.Calls);
    }

    [Fact]
    public async Task ACKCFE_detail_outside_durable_Sobre_fails_closed_without_persistence()
    {
        var source = Source(FiscalCfeEnvelopeAckState.Received, includeToken: true);
        var consultations = new ConsultationRepository();
        var response = ParseResponse(Wrap(AckCfe(source, 1, number: 999)));
        var useCase = UseCase(source, consultations, new FakeGateway(response));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(new ConsultFiscalCfeEnvelopeDocumentResponseCommand(
                source.Envelope.OrganizationId,
                source.Envelope.IssuerRuc,
                source.Envelope.ReceiverRut,
                source.Envelope.SenderEnvelopeId,
                "mismatch-op")));

        Assert.Equal("fiscal.envelope.document_response.external_evidence_invalid", error.Code);
        Assert.Empty(consultations.Values);
    }

    [Fact]
    public void SOAP_envelope_uses_published_IdReceptor_Token_CDATA_contract()
    {
        using var certificate = Certificate();
        var request = new FiscalCfeEnvelopeDocumentResponseConsultationRequest(
            "company-1",
            1516,
            "token-contract-1");
        var envelope = BuildEnvelope(request, certificate);

        var operation = envelope.SelectSingleNode("//*[local-name()='WS_eFactura.EFACCONSULTARESTADOENVIO']");
        var xmlData = envelope.SelectSingleNode("//*[local-name()='Datain']/*[local-name()='xmlData']") as XmlElement;

        Assert.NotNull(operation);
        Assert.NotNull(xmlData);
        Assert.Equal(XmlNodeType.CDATA, xmlData!.FirstChild!.NodeType);
        Assert.Contains("<ConsultaCFE", xmlData.InnerText, StringComparison.Ordinal);
        Assert.Contains("<IdReceptor>1516</IdReceptor>", xmlData.InnerText, StringComparison.Ordinal);
        Assert.Contains("<Token>token-contract-1</Token>", xmlData.InnerText, StringComparison.Ordinal);
        Assert.DoesNotContain("Cfeid", xmlData.InnerText, StringComparison.Ordinal);
        Assert.Contains("BinarySecurityToken", envelope.OuterXml, StringComparison.Ordinal);
    }

    [Fact]
    public void Parser_preserves_partial_ACKCFE_raw_state_and_rejects_DTD()
    {
        var source = Source(FiscalCfeEnvelopeAckState.Received, includeToken: true);
        var parsed = ParseResponse(Wrap(AckCfe(source, 1)));

        Assert.Equal(source.Envelope.IssuerRuc, parsed.IssuerRuc);
        Assert.Equal(source.Envelope.ReceiverRut, parsed.ReceiverRut);
        Assert.Equal(source.Envelope.SenderEnvelopeId, parsed.SenderEnvelopeId);
        Assert.Equal(source.Observation.DgiReceiverId, parsed.DgiReceiverId);
        Assert.Equal(2, parsed.EnvelopeCfeCount);
        Assert.Equal(1, parsed.RespondedCount);
        Assert.Single(parsed.Details);
        Assert.Equal("A", parsed.Details[0].StateCode);

        const string unsafeXml = "<!DOCTYPE x [<!ENTITY boom 'x'>]><x>&boom;</x>";
        Assert.Throws<FiscalCfeEnvelopeDocumentResponseConsultationException>(() => ParseResponse(unsafeXml));
    }

    private static ConsultFiscalCfeEnvelopeDocumentResponseUseCase UseCase(
        SourceBundle source,
        ConsultationRepository consultations,
        FakeGateway gateway) =>
        new(
            new EnvelopeRepository(source.Envelope),
            new SubmissionRepository(source.Submission),
            new AckRepository(source.Observation),
            new DocumentRepository(source.Documents),
            consultations,
            gateway,
            new NeverConflictClassifier(),
            new FixedClock(new DateTimeOffset(2026, 9, 13, 22, 30, 0, TimeSpan.Zero)),
            new InlineTransactionManager(),
            new CountingUnitOfWork());

    private static SourceBundle Source(FiscalCfeEnvelopeAckState state, bool includeToken)
    {
        var documents = new[] { Document(123), Document(124) };
        const string envelopeXml = "<EnvioCFE xmlns=\"http://cfe.dgi.gub.uy\"><Caratula /></EnvioCFE>";
        const string ackXml = "<ACKSobre xmlns=\"http://cfe.dgi.gub.uy\"><Caratula /></ACKSobre>";
        var envelope = new StoredFiscalCfeEnvelope(
            Guid.NewGuid(),
            "company-1",
            "214844360018",
            "219999820013",
            3009,
            new DateTimeOffset(2026, 9, 13, 19, 0, 0, TimeSpan.FromHours(-3)),
            documents.Select(x => x.Id).ToArray(),
            "envelope-op",
            documents.Length,
            "thumbprint-1",
            "serial-1",
            envelopeXml,
            Sha256(envelopeXml),
            "dgi-fe-v1.44.2",
            "05",
            new string('a', 64));
        var submission = new StoredFiscalCfeEnvelopeSubmission(
            Guid.NewGuid(),
            envelope.Id,
            envelope.OrganizationId,
            envelope.IssuerRuc,
            envelope.ReceiverRut,
            envelope.SenderEnvelopeId,
            "submission-op",
            envelope.EnvelopeSha256,
            FiscalCfeEnvelopeSubmissionState.ResponseReceived,
            1,
            new DateTimeOffset(2026, 9, 13, 22, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 13, 22, 1, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, 13, 22, 1, 1, TimeSpan.Zero),
            ackXml,
            Sha256(ackXml),
            null);
        var observation = new StoredFiscalCfeEnvelopeAckObservation(
            Guid.NewGuid(),
            submission.Id,
            envelope.Id,
            envelope.OrganizationId,
            envelope.IssuerRuc,
            envelope.ReceiverRut,
            envelope.SenderEnvelopeId,
            submission.ResponseSha256!,
            1516,
            1516,
            documents.Length,
            state,
            "2026-09-13T19:01:00-03:00",
            "2026-09-13T19:01:01-03:00",
            includeToken ? "token-doc-response" : null,
            includeToken ? "2026-09-13T19:01:02-03:00" : null,
            state == FiscalCfeEnvelopeAckState.Rejected
                ? "[{\"Code\":\"S01\",\"Glosa\":\"Sobre rechazado\",\"Detail\":null}]"
                : "[]",
            new DateTimeOffset(2026, 9, 13, 22, 2, 0, TimeSpan.Zero));
        return new(envelope, submission, observation, documents);
    }

    private static string AckCfe(SourceBundle source, int respondedCount, long number = 123) =>
        $"<ACKCFE xmlns=\"http://cfe.dgi.gub.uy\"><Caratula>" +
        $"<RUCReceptor>{source.Envelope.ReceiverRut}</RUCReceptor>" +
        $"<RUCEmisor>{source.Envelope.IssuerRuc}</RUCEmisor>" +
        "<IDRespuesta>1516</IDRespuesta>" +
        $"<IDEmisor>{source.Envelope.SenderEnvelopeId}</IDEmisor>" +
        $"<IDReceptor>{source.Observation.DgiReceiverId}</IDReceptor>" +
        $"<CantenSobre>{source.Envelope.CfeCount}</CantenSobre>" +
        $"<CantResponden>{respondedCount}</CantResponden>" +
        $"<CantCFEAceptados>{respondedCount}</CantCFEAceptados>" +
        "<CantCFERechazados>0</CantCFERechazados><CantCFEObservados>0</CantCFEObservados><CantOtrosRechazados>0</CantOtrosRechazados>" +
        "</Caratula>" +
        (respondedCount == 0 ? string.Empty :
            $"<ACKCFE_det ordinal=\"1\"><TipoCFE>101</TipoCFE><Serie>A</Serie><NroCFE>{number}</NroCFE><Estado>A</Estado></ACKCFE_det>") +
        "</ACKCFE>";

    private static string Wrap(string ack) =>
        $"<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:dgi=\"http://dgi.gub.uy\"><soapenv:Body><dgi:WS_eFactura.EFACCONSULTARESTADOENVIOResponse><dgi:DataOut><dgi:xmlData><![CDATA[{ack}]]></dgi:xmlData></dgi:DataOut></dgi:WS_eFactura.EFACCONSULTARESTADOENVIOResponse></soapenv:Body></soapenv:Envelope>";

    private static XmlDocument BuildEnvelope(
        FiscalCfeEnvelopeDocumentResponseConsultationRequest request,
        X509Certificate2 certificate)
    {
        var method = typeof(DgiWsSecurityFiscalCfeEnvelopeDocumentResponseConsultationGateway).GetMethod(
            "BuildSoapEnvelope",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BuildSoapEnvelope was not found.");
        try
        {
            return (XmlDocument)(method.Invoke(null, new object[] { request, certificate })
                ?? throw new InvalidOperationException("SOAP envelope was not returned."));
        }
        catch (TargetInvocationException ex) when (ex.InnerException is Exception inner)
        {
            throw inner;
        }
    }

    private static FiscalCfeEnvelopeDocumentResponseConsultationResponse ParseResponse(string soapResponse)
    {
        var method = typeof(DgiWsSecurityFiscalCfeEnvelopeDocumentResponseConsultationGateway).GetMethod(
            "ParseResponse",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ParseResponse was not found.");
        try
        {
            return (FiscalCfeEnvelopeDocumentResponseConsultationResponse)(method.Invoke(null, new object[] { soapResponse })
                ?? throw new InvalidOperationException("ACKCFE response was not returned."));
        }
        catch (TargetInvocationException ex)
            when (ex.InnerException is FiscalCfeEnvelopeDocumentResponseConsultationException inner)
        {
            throw inner;
        }
    }

    private static FiscalDocument Document(long number) =>
        FiscalDocument.CreateIdentity(
            Guid.NewGuid(),
            "company-1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            CfeFamily.ETicket,
            "A",
            number,
            "CAE-ACKCFE-001",
            1,
            1000,
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            new DateOnly(2026, 9, 13),
            "loc-1",
            "term-1",
            null,
            "25.2",
            new string('b', 64),
            new string('c', 64),
            "UYU",
            100m,
            22m,
            122m,
            new DateTimeOffset(2026, 9, 13, 21, 0, 0, TimeSpan.Zero));

    private static X509Certificate2 Certificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=eFactura ACKCFE consultation test",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed record SourceBundle(
        StoredFiscalCfeEnvelope Envelope,
        StoredFiscalCfeEnvelopeSubmission Submission,
        StoredFiscalCfeEnvelopeAckObservation Observation,
        FiscalDocument[] Documents);

    private sealed class EnvelopeRepository(StoredFiscalCfeEnvelope value) : IFiscalCfeEnvelopeRepository
    {
        public Task<StoredFiscalCfeEnvelope?> GetByOperationIdAsync(string organizationId, string operationId, CancellationToken cancellationToken = default) => Task.FromResult<StoredFiscalCfeEnvelope?>(value);
        public Task<StoredFiscalCfeEnvelope?> GetByIdentityAsync(string organizationId, string issuerRuc, string receiverRut, long senderEnvelopeId, CancellationToken cancellationToken = default) =>
            Task.FromResult<StoredFiscalCfeEnvelope?>(value.OrganizationId == organizationId && value.IssuerRuc == issuerRuc && value.ReceiverRut == receiverRut && value.SenderEnvelopeId == senderEnvelopeId ? value : null);
        public Task AddAsync(StoredFiscalCfeEnvelope envelope, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class SubmissionRepository(StoredFiscalCfeEnvelopeSubmission value) : IFiscalCfeEnvelopeSubmissionRepository
    {
        public Task<StoredFiscalCfeEnvelopeSubmission?> GetByOperationIdAsync(string organizationId, string operationId, CancellationToken cancellationToken = default) => Task.FromResult<StoredFiscalCfeEnvelopeSubmission?>(value);
        public Task<StoredFiscalCfeEnvelopeSubmission?> GetByEnvelopeIdAsync(Guid envelopeId, CancellationToken cancellationToken = default) => Task.FromResult<StoredFiscalCfeEnvelopeSubmission?>(value.EnvelopeId == envelopeId ? value : null);
        public Task AddAsync(StoredFiscalCfeEnvelopeSubmission submission, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(StoredFiscalCfeEnvelopeSubmission submission, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class AckRepository(StoredFiscalCfeEnvelopeAckObservation value) : IFiscalCfeEnvelopeAckObservationRepository
    {
        public Task<StoredFiscalCfeEnvelopeAckObservation?> GetBySubmissionIdAsync(Guid submissionId, CancellationToken cancellationToken = default) => Task.FromResult<StoredFiscalCfeEnvelopeAckObservation?>(value.SubmissionId == submissionId ? value : null);
        public Task AddAsync(StoredFiscalCfeEnvelopeAckObservation observation, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DocumentRepository(IReadOnlyList<FiscalDocument> values) : IFiscalDocumentRepository
    {
        public Task<FiscalDocument?> GetAsync(string organizationId, Guid fiscalDocumentId, CancellationToken cancellationToken = default) => Task.FromResult(values.SingleOrDefault(x => x.OrganizationId == organizationId && x.Id == fiscalDocumentId));
        public Task<FiscalDocument?> GetByFiscalizationRequestAsync(string organizationId, Guid fiscalizationRequestId, CancellationToken cancellationToken = default) => Task.FromResult(values.SingleOrDefault(x => x.OrganizationId == organizationId && x.FiscalizationRequestId == fiscalizationRequestId));
        public Task AddAsync(FiscalDocument document, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ConsultationRepository : IFiscalCfeEnvelopeDocumentResponseConsultationRepository
    {
        public List<StoredFiscalCfeEnvelopeDocumentResponseConsultation> Values { get; } = [];
        public Task<StoredFiscalCfeEnvelopeDocumentResponseConsultation?> GetByOperationAsync(string organizationId, string operationId, CancellationToken cancellationToken = default) => Task.FromResult(Values.SingleOrDefault(x => x.OrganizationId == organizationId && x.OperationId == operationId));
        public Task AddAsync(StoredFiscalCfeEnvelopeDocumentResponseConsultation consultation, CancellationToken cancellationToken = default)
        {
            Values.Add(consultation);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGateway(params FiscalCfeEnvelopeDocumentResponseConsultationResponse[] responses) : IFiscalCfeEnvelopeDocumentResponseConsultationGateway
    {
        private readonly Queue<FiscalCfeEnvelopeDocumentResponseConsultationResponse> _responses = new(responses);
        public int Calls { get; private set; }
        public Task<FiscalCfeEnvelopeDocumentResponseConsultationResponse> QueryAsync(FiscalCfeEnvelopeDocumentResponseConsultationRequest request, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(_responses.Dequeue());
        }
    }

    private sealed class NeverConflictClassifier : IFiscalCfeEnvelopePersistenceConflictClassifier
    {
        public bool IsUniqueConstraintConflict(Exception exception) => false;
    }

    private sealed class FixedClock(DateTimeOffset now) : IFiscalCfeEnvelopeTransportClock
    {
        public DateTimeOffset UtcNow => now;
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
}
