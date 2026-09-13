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

public sealed class FiscalCfeStateConsultationTests
{
    [Fact]
    public async Task Durable_fiscal_identity_is_queried_once_and_operation_replay_is_append_only()
    {
        var document = Document();
        var documents = new DocumentRepository(document);
        var consultations = new ConsultationRepository();
        var gateway = new FakeGateway(new FiscalCfeStateConsultationResponse(
            "AE",
            "2020",
            "129",
            "token-state-1",
            "2026-09-13T16:30:00",
            SoapResponse("AE", "2020", "129", includeParameters: true)));
        var useCase = new ConsultFiscalCfeStateUseCase(
            documents,
            consultations,
            gateway,
            new FixedClock(new DateTimeOffset(2026, 9, 13, 16, 0, 0, TimeSpan.Zero)),
            new InlineTransactionManager(),
            new CountingUnitOfWork());

        var command = new ConsultFiscalCfeStateCommand(document.OrganizationId, document.Id, "cfe-state-op-1");
        var first = await useCase.ExecuteAsync(command);
        var replay = await useCase.ExecuteAsync(command);

        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(first.ConsultationId, replay.ConsultationId);
        Assert.Equal(CfeFamily.ETicket, first.CfeType);
        Assert.Equal("A", first.Series);
        Assert.Equal(123, first.Number);
        Assert.Equal("AE", first.StateCode);
        Assert.Equal("token-state-1", first.ConsultationToken);
        Assert.Equal(64, first.ResponseSha256.Length);
        Assert.Equal(1, gateway.Calls);
        Assert.Single(consultations.Values);
        Assert.Equal(FiscalDocumentStatus.IdentityCreated, document.Status);
    }

    [Fact]
    public async Task Reusing_operation_id_for_another_fiscal_document_fails_before_network()
    {
        var firstDocument = Document();
        var secondDocument = Document(Guid.NewGuid(), 124);
        var consultations = new ConsultationRepository();
        consultations.Values.Add(new StoredFiscalCfeStateConsultation(
            Guid.NewGuid(),
            firstDocument.Id,
            firstDocument.OrganizationId,
            firstDocument.CfeType,
            firstDocument.Series,
            firstDocument.Number,
            "same-state-op",
            "AE",
            "2020",
            "129",
            null,
            null,
            "<response />",
            new string('a', 64),
            DateTimeOffset.UtcNow));
        var gateway = new FakeGateway(new FiscalCfeStateConsultationResponse(
            "AE", "2020", "130", null, null, "<response />"));
        var useCase = new ConsultFiscalCfeStateUseCase(
            new DocumentRepository(firstDocument, secondDocument),
            consultations,
            gateway,
            new FixedClock(DateTimeOffset.UtcNow),
            new InlineTransactionManager(),
            new CountingUnitOfWork());

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(new ConsultFiscalCfeStateCommand(
                secondDocument.OrganizationId,
                secondDocument.Id,
                "same-state-op")));

        Assert.Equal("fiscal.cfe_state_consultation.operation_replay_mismatch", error.Code);
        Assert.Equal(0, gateway.Calls);
    }

    [Fact]
    public async Task Token_and_FechaHora_are_fail_closed_as_a_pair()
    {
        var document = Document();
        var consultations = new ConsultationRepository();
        var useCase = new ConsultFiscalCfeStateUseCase(
            new DocumentRepository(document),
            consultations,
            new FakeGateway(new FiscalCfeStateConsultationResponse(
                "AE", "2020", "129", "token-without-date", null, "<response />")),
            new FixedClock(DateTimeOffset.UtcNow),
            new InlineTransactionManager(),
            new CountingUnitOfWork());

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(new ConsultFiscalCfeStateCommand(
                document.OrganizationId,
                document.Id,
                "invalid-pair")));

        Assert.Equal("fiscal.cfe_state_consultation.external_evidence_invalid", error.Code);
        Assert.Empty(consultations.Values);
    }

    [Fact]
    public void SOAP_envelope_uses_published_EFACCONSULTARESTADOCFE_identity_contract()
    {
        using var certificate = Certificate();
        var request = new FiscalCfeStateConsultationRequest("company-1", CfeFamily.ETicket, "a", 123);
        var envelope = BuildEnvelope(request, certificate);

        var operation = envelope.SelectSingleNode(
            "//*[local-name()='WS_eFactura_Consultas.EFACCONSULTARESTADOCFE']");
        var cfeId = envelope.SelectSingleNode("//*[local-name()='Cfeid']");
        var type = cfeId?.SelectSingleNode("./*[local-name()='TipoCFE']");
        var series = cfeId?.SelectSingleNode("./*[local-name()='Serie']");
        var number = cfeId?.SelectSingleNode("./*[local-name()='Nro']");

        Assert.NotNull(operation);
        Assert.NotNull(cfeId);
        Assert.Equal("101", type!.InnerText);
        Assert.Equal("A", series!.InnerText);
        Assert.Equal("123", number!.InnerText);
        Assert.Contains("BinarySecurityToken", envelope.OuterXml, StringComparison.Ordinal);
        Assert.DoesNotContain("Token", envelope.SelectSingleNode("//*[local-name()='Body']")!.InnerXml, StringComparison.Ordinal);
    }

    [Fact]
    public void Parser_preserves_EstadoCFE_and_first_Sobre_parameters_without_interpreting_them()
    {
        var parsed = ParseResponse(SoapResponse("AE", "2020", "129", includeParameters: true));

        Assert.Equal("AE", parsed.StateCode);
        Assert.Equal("2020", parsed.DgiSenderId);
        Assert.Equal("129", parsed.DgiReceiverId);
        Assert.Equal("token-state", parsed.ConsultationToken);
        Assert.Equal("2026-09-13T16:30:00", parsed.ConsultationAvailableAtText);
        Assert.Contains("EFACCONSULTARESTADOCFEResponse", parsed.ResponseXml, StringComparison.Ordinal);
    }

    [Fact]
    public void Parser_rejects_incomplete_parameters_and_DTD()
    {
        var incompleteAck = "<Ackconsultaestadocfe xmlns=\"http://cfe.dgi.gub.uy\"><EstadoCFE>AE</EstadoCFE><IdEmisor>2020</IdEmisor><IdReceptor>129</IdReceptor><ParamConsulta><Token>only-token</Token></ParamConsulta></Ackconsultaestadocfe>";
        var incomplete = Wrap(incompleteAck);
        Assert.Throws<FiscalCfeStateConsultationException>(() => ParseResponse(incomplete));

        const string unsafeXml = "<!DOCTYPE x [<!ENTITY boom 'x'>]><x>&boom;</x>";
        Assert.Throws<FiscalCfeStateConsultationException>(() => ParseResponse(unsafeXml));
    }

    private static XmlDocument BuildEnvelope(
        FiscalCfeStateConsultationRequest request,
        X509Certificate2 certificate)
    {
        var method = typeof(DgiWsSecurityFiscalCfeStateConsultationGateway).GetMethod(
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

    private static FiscalCfeStateConsultationResponse ParseResponse(string soapResponse)
    {
        var method = typeof(DgiWsSecurityFiscalCfeStateConsultationGateway).GetMethod(
            "ParseResponse",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ParseResponse was not found.");

        try
        {
            return (FiscalCfeStateConsultationResponse)(method.Invoke(null, new object[] { soapResponse })
                ?? throw new InvalidOperationException("Consultation response was not returned."));
        }
        catch (TargetInvocationException ex)
            when (ex.InnerException is FiscalCfeStateConsultationException inner)
        {
            throw inner;
        }
    }

    private static string SoapResponse(
        string state,
        string senderId,
        string receiverId,
        bool includeParameters)
    {
        var parameters = includeParameters
            ? "<ParamConsulta><Token>token-state</Token><Fechahora>2026-09-13T16:30:00</Fechahora></ParamConsulta>"
            : string.Empty;
        var ack = $"<Ackconsultaestadocfe xmlns=\"http://cfe.dgi.gub.uy\"><EstadoCFE>{state}</EstadoCFE><IdEmisor>{senderId}</IdEmisor><IdReceptor>{receiverId}</IdReceptor>{parameters}</Ackconsultaestadocfe>";
        return Wrap(ack);
    }

    private static string Wrap(string ack) =>
        $"<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:dgi=\"http://dgi.gub.uy\"><soapenv:Body><dgi:WS_eFactura_Consultas.EFACCONSULTARESTADOCFEResponse><dgi:xmlData><![CDATA[{ack}]]></dgi:xmlData></dgi:WS_eFactura_Consultas.EFACCONSULTARESTADOCFEResponse></soapenv:Body></soapenv:Envelope>";

    private static FiscalDocument Document(Guid? id = null, long number = 123) =>
        FiscalDocument.CreateIdentity(
            id ?? Guid.NewGuid(),
            "company-1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            CfeFamily.ETicket,
            "A",
            number,
            "CAE-STATE-001",
            1,
            1000,
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            new DateOnly(2026, 9, 13),
            "loc-1",
            "term-1",
            null,
            "25.2",
            new string('a', 64),
            new string('b', 64),
            "UYU",
            100m,
            22m,
            122m,
            DateTimeOffset.UtcNow);

    private static X509Certificate2 Certificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=eFactura CFE state consultation test",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    private sealed class DocumentRepository(params FiscalDocument[] documents) : IFiscalDocumentRepository
    {
        private readonly IReadOnlyList<FiscalDocument> _documents = documents;

        public Task<FiscalDocument?> GetAsync(
            string organizationId,
            Guid fiscalDocumentId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_documents.SingleOrDefault(x =>
                x.OrganizationId == organizationId && x.Id == fiscalDocumentId));

        public Task<FiscalDocument?> GetByFiscalizationRequestAsync(
            string organizationId,
            Guid fiscalizationRequestId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_documents.SingleOrDefault(x =>
                x.OrganizationId == organizationId && x.FiscalizationRequestId == fiscalizationRequestId));

        public Task AddAsync(FiscalDocument document, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class ConsultationRepository : IFiscalCfeStateConsultationRepository
    {
        public List<StoredFiscalCfeStateConsultation> Values { get; } = [];

        public Task<StoredFiscalCfeStateConsultation?> GetByOperationIdAsync(
            string organizationId,
            string operationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Values.SingleOrDefault(x =>
                x.OrganizationId == organizationId && x.OperationId == operationId));

        public Task AddAsync(
            StoredFiscalCfeStateConsultation consultation,
            CancellationToken cancellationToken = default)
        {
            Values.Add(consultation);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGateway(params FiscalCfeStateConsultationResponse[] responses) : IFiscalCfeStateConsultationGateway
    {
        private readonly Queue<FiscalCfeStateConsultationResponse> _responses = new(responses);
        public int Calls { get; private set; }

        public Task<FiscalCfeStateConsultationResponse> QueryAsync(
            FiscalCfeStateConsultationRequest request,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(_responses.Dequeue());
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
}
