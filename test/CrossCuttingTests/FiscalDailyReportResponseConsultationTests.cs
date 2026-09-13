using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Fiscal;
using Infrastructure.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalDailyReportResponseConsultationTests
{
    private static readonly DateOnly SummaryDate = new(2026, 9, 11);

    [Fact]
    public async Task Known_root_receiver_is_persisted_and_operation_replay_does_not_query_DGI_twice()
    {
        var target = RootTarget("receiver-root", "AR");
        var targets = new TargetReader(target);
        var repository = new ConsultationRepository();
        var gateway = new FakeGateway(Response("receiver-root", "AR"));
        var clock = new FixedClock(new DateTimeOffset(2026, 9, 12, 22, 0, 0, TimeSpan.Zero));
        var useCase = UseCase(targets, repository, gateway, clock);

        var first = await useCase.ExecuteAsync(Command("receiver-root", "consult-op-1"));
        var replay = await useCase.ExecuteAsync(Command("receiver-root", "consult-op-1"));

        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(FiscalDailyReportConsultationTargetKind.RootSubmission, first.TargetKind);
        Assert.Equal(target.TargetId, first.TargetId);
        Assert.Equal("AR", first.AckStateCode);
        Assert.Equal(FiscalDailyReportConsultationConsistency.MatchesImmediateAck, first.Consistency);
        Assert.Equal(64, first.AckXmlHash.Length);
        Assert.Equal(1, gateway.Calls);
        Assert.Single(repository.Values);
    }

    [Fact]
    public async Task Known_BR_correction_receiver_is_linked_to_revision_without_mutating_transport_state()
    {
        var target = CorrectionTarget("receiver-correction", "BR", 3);
        var targets = new TargetReader(target);
        var repository = new ConsultationRepository();
        var gateway = new FakeGateway(Response("receiver-correction", "BR"));
        var useCase = UseCase(targets, repository, gateway, new FixedClock(DateTimeOffset.UtcNow));

        var result = await useCase.ExecuteAsync(Command("receiver-correction", "consult-op-br"));

        Assert.Equal(FiscalDailyReportConsultationTargetKind.BrCorrectionRevision, result.TargetKind);
        Assert.Equal(3, result.LocalRevision);
        Assert.Equal(FiscalDailyReportConsultationConsistency.MatchesImmediateAck, result.Consistency);
        var stored = Assert.Single(repository.Values);
        Assert.Null(stored.RootSubmissionId);
        Assert.Equal(target.TargetId, stored.BrCorrectionRevisionId);
        Assert.Equal("BR", target.ImmediateAckStateCode);
    }

    [Fact]
    public async Task Unknown_receiver_fails_closed_before_network()
    {
        var gateway = new FakeGateway(Response("unused", "AR"));
        var useCase = UseCase(
            new TargetReader(),
            new ConsultationRepository(),
            gateway,
            new FixedClock(DateTimeOffset.UtcNow));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(Command("missing-receiver", "consult-missing")));

        Assert.Equal("fiscal.daily_report.consultation.receiver_not_known", error.Code);
        Assert.Equal(0, gateway.Calls);
    }

    [Fact]
    public async Task Ambiguous_receiver_fails_closed_before_network()
    {
        var gateway = new FakeGateway(Response("receiver-shared", "AR"));
        var useCase = UseCase(
            new TargetReader(
                RootTarget("receiver-shared", "AR"),
                CorrectionTarget("receiver-shared", "BR", 2)),
            new ConsultationRepository(),
            gateway,
            new FixedClock(DateTimeOffset.UtcNow));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(Command("receiver-shared", "consult-ambiguous")));

        Assert.Equal("fiscal.daily_report.consultation.receiver_ambiguous", error.Code);
        Assert.Equal(0, gateway.Calls);
    }

    [Fact]
    public async Task Conflicting_original_ACK_is_recorded_as_evidence_and_does_not_rewrite_local_ACK()
    {
        var target = RootTarget("receiver-conflict", "BR");
        var repository = new ConsultationRepository();
        var useCase = UseCase(
            new TargetReader(target),
            repository,
            new FakeGateway(Response("receiver-conflict", "AR")),
            new FixedClock(DateTimeOffset.UtcNow));

        var result = await useCase.ExecuteAsync(Command("receiver-conflict", "consult-conflict"));

        Assert.Equal(FiscalDailyReportConsultationConsistency.ConflictsWithImmediateAck, result.Consistency);
        Assert.Equal("AR", result.AckStateCode);
        Assert.Equal("BR", target.ImmediateAckStateCode);
        Assert.Single(repository.Values);
    }

    [Fact]
    public async Task Gateway_receiver_mismatch_is_rejected_without_persisting_evidence()
    {
        var repository = new ConsultationRepository();
        var useCase = UseCase(
            new TargetReader(RootTarget("receiver-requested", "AR")),
            repository,
            new FakeGateway(Response("receiver-other", "AR")),
            new FixedClock(DateTimeOffset.UtcNow));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(Command("receiver-requested", "consult-mismatch")));

        Assert.Equal("fiscal.daily_report.consultation.receiver_mismatch", error.Code);
        Assert.Empty(repository.Values);
    }

    [Fact]
    public async Task Reusing_operation_id_for_different_receiver_fails_before_network()
    {
        var repository = new ConsultationRepository();
        repository.Values.Add(new StoredFiscalDailyReportResponseConsultation(
            Guid.NewGuid(), Guid.NewGuid(), null,
            "company-1", "214748364700", SummaryDate, 1, null,
            "same-op", "receiver-a", "AR", Ack("receiver-a", "AR"), new string('a', 64),
            FiscalDailyReportConsultationConsistency.MatchesImmediateAck,
            DateTimeOffset.UtcNow));
        var gateway = new FakeGateway(Response("receiver-b", "AR"));
        var useCase = UseCase(
            new TargetReader(RootTarget("receiver-b", "AR")),
            repository,
            gateway,
            new FixedClock(DateTimeOffset.UtcNow));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(Command("receiver-b", "same-op")));

        Assert.Equal("fiscal.daily_report.consultation.operation_replay_mismatch", error.Code);
        Assert.Equal(0, gateway.Calls);
    }

    [Fact]
    public void SOAP_envelope_uses_published_consultation_operation_and_IdReceptor()
    {
        using var certificate = Certificate();
        var envelope = BuildEnvelope("receiver-soap", certificate);

        var operation = envelope.SelectSingleNode(
            "//*[local-name()='WS_eFactura_Consultas.EFACCONSULTARRESPUESTAREPORTE']");
        var receiver = envelope.SelectSingleNode("//*[local-name()='Consultarrespuestareporte']/*[local-name()='IdReceptor']");

        Assert.NotNull(operation);
        Assert.NotNull(receiver);
        Assert.Equal("receiver-soap", receiver!.InnerText);
        Assert.Contains("BinarySecurityToken", envelope.OuterXml, StringComparison.Ordinal);
        Assert.DoesNotContain("EFACCONSULTARENVIOSREPORTE", envelope.OuterXml, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("AR")]
    [InlineData("BR")]
    public void Consultation_parser_accepts_original_ACKRepDiario_AR_or_BR(string state)
    {
        var parsed = ParseResponse(
            SoapResponse("receiver-parse", state),
            "receiver-parse");

        Assert.Equal("receiver-parse", parsed.DgiReceiverId);
        Assert.Equal(state, parsed.AckStateCode);
        Assert.Contains("ACKRepDiario", parsed.AckXml, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("DR")]
    [InlineData("ER")]
    [InlineData("FR")]
    public void Consultation_parser_rejects_later_processing_states_as_original_ACK(string state)
    {
        Assert.Throws<FiscalDailyReportResponseConsultationException>(() =>
            ParseResponse(SoapResponse("receiver-later", state), "receiver-later"));
    }

    [Fact]
    public void Consultation_parser_rejects_receiver_mismatch_and_DTD()
    {
        Assert.Throws<FiscalDailyReportResponseConsultationException>(() =>
            ParseResponse(SoapResponse("receiver-returned", "AR"), "receiver-requested"));

        const string unsafeXml = "<!DOCTYPE x [<!ENTITY boom 'x'>]><x>&boom;</x>";
        Assert.Throws<FiscalDailyReportResponseConsultationException>(() =>
            ParseResponse(unsafeXml, "receiver"));
    }

    private static XmlDocument BuildEnvelope(string receiver, X509Certificate2 certificate)
    {
        var method = typeof(DgiWsSecurityFiscalDailyReportResponseConsultationGateway).GetMethod(
            "BuildSoapEnvelope",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BuildSoapEnvelope was not found.");

        return (XmlDocument)(method.Invoke(null, new object[] { receiver, certificate })
            ?? throw new InvalidOperationException("SOAP envelope was not returned."));
    }

    private static FiscalDailyReportResponseConsultationResponse ParseResponse(
        string soapResponse,
        string expectedReceiverId)
    {
        var method = typeof(DgiWsSecurityFiscalDailyReportResponseConsultationGateway).GetMethod(
            "ParseResponse",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ParseResponse was not found.");

        try
        {
            return (FiscalDailyReportResponseConsultationResponse)(method.Invoke(
                null,
                new object[] { soapResponse, expectedReceiverId })
                ?? throw new InvalidOperationException("Consultation response was not returned."));
        }
        catch (TargetInvocationException ex)
            when (ex.InnerException is FiscalDailyReportResponseConsultationException inner)
        {
            throw inner;
        }
    }

    private static ConsultFiscalDailyReportResponseUseCase UseCase(
        IFiscalDailyReportConsultationTargetReader targets,
        IFiscalDailyReportResponseConsultationRepository repository,
        IFiscalDailyReportResponseConsultationGateway gateway,
        IFiscalDailyReportTransportClock clock) =>
        new(targets, repository, gateway, clock, new InlineTransactionManager(), new CountingUnitOfWork());

    private static ConsultFiscalDailyReportResponseCommand Command(string receiver, string operationId) =>
        new("company-1", receiver, operationId);

    private static FiscalDailyReportConsultationTarget RootTarget(string receiver, string? immediateState) =>
        new(
            FiscalDailyReportConsultationTargetKind.RootSubmission,
            Guid.NewGuid(),
            "company-1",
            "214748364700",
            SummaryDate,
            1,
            null,
            receiver,
            immediateState);

    private static FiscalDailyReportConsultationTarget CorrectionTarget(
        string receiver,
        string? immediateState,
        int revision) =>
        new(
            FiscalDailyReportConsultationTargetKind.BrCorrectionRevision,
            Guid.NewGuid(),
            "company-1",
            "214748364700",
            SummaryDate,
            1,
            revision,
            receiver,
            immediateState);

    private static FiscalDailyReportResponseConsultationResponse Response(string receiver, string state) =>
        new(receiver, state, Ack(receiver, state));

    private static string Ack(string receiver, string state) =>
        $"<ACKRepDiario xmlns=\"http://cfe.dgi.gub.uy\"><Caratula><IDReceptor>{receiver}</IDReceptor></Caratula><Detalle><Estado>{state}</Estado></Detalle></ACKRepDiario>";

    private static string SoapResponse(string receiver, string state) =>
        $"<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:dgi=\"http://dgi.gub.uy\"><soapenv:Body><dgi:WS_eFactura_Consultas.EFACCONSULTARRESPUESTAREPORTEResponse><dgi:xmlData><![CDATA[{Ack(receiver, state)}]]></dgi:xmlData></dgi:WS_eFactura_Consultas.EFACCONSULTARRESPUESTAREPORTEResponse></soapenv:Body></soapenv:Envelope>";

    private static X509Certificate2 Certificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=eFactura consultation test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    private sealed class TargetReader(params FiscalDailyReportConsultationTarget[] values) : IFiscalDailyReportConsultationTargetReader
    {
        private readonly IReadOnlyList<FiscalDailyReportConsultationTarget> _values = values;

        public Task<IReadOnlyList<FiscalDailyReportConsultationTarget>> FindByReceiverIdAsync(
            string organizationId,
            string dgiReceiverId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FiscalDailyReportConsultationTarget>>(
                _values.Where(x => x.OrganizationId == organizationId && x.DgiReceiverId == dgiReceiverId).ToArray());
    }

    private sealed class ConsultationRepository : IFiscalDailyReportResponseConsultationRepository
    {
        public List<StoredFiscalDailyReportResponseConsultation> Values { get; } = [];

        public Task<StoredFiscalDailyReportResponseConsultation?> GetByOperationIdAsync(
            string organizationId,
            string operationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Values.SingleOrDefault(x => x.OrganizationId == organizationId && x.OperationId == operationId));

        public Task AddAsync(
            StoredFiscalDailyReportResponseConsultation consultation,
            CancellationToken cancellationToken = default)
        {
            Values.Add(consultation);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGateway(params FiscalDailyReportResponseConsultationResponse[] responses) : IFiscalDailyReportResponseConsultationGateway
    {
        private readonly Queue<FiscalDailyReportResponseConsultationResponse> _responses = new(responses);
        public int Calls { get; private set; }

        public Task<FiscalDailyReportResponseConsultationResponse> QueryAsync(
            FiscalDailyReportResponseConsultationRequest request,
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
