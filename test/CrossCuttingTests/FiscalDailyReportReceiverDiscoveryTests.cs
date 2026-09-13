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

public sealed class FiscalDailyReportReceiverDiscoveryTests
{
    private static readonly DateOnly SummaryDate = new(2026, 9, 11);

    [Fact]
    public async Task Unknown_root_discovers_exactly_one_unaccounted_receiver_and_replays_without_network()
    {
        var target = RootTarget(FiscalDailyReportSubmissionState.Unknown, null);
        var targets = new TargetReader(target, ["receiver-known"]);
        var repository = new DiscoveryRepository();
        var gateway = new FakeGateway(Response(
            Item("emitter-1", "receiver-known", "BR", "2026-09-12T18:00:00"),
            Item("emitter-1", "receiver-new", "AR", "2026-09-12T18:05:00")));
        var useCase = UseCase(targets, repository, gateway);

        var first = await useCase.ExecuteAsync(Command(target, "discover-op-1"));
        var replay = await useCase.ExecuteAsync(Command(target, "discover-op-1"));

        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal("receiver-new", first.DgiReceiverId);
        Assert.Equal("emitter-1", first.DgiEmitterId);
        Assert.Equal("AR", first.DgiStateCode);
        Assert.Equal(64, first.EvidenceXmlHash.Length);
        Assert.Equal(1, gateway.Calls);
        Assert.Single(repository.Values);
    }

    [Fact]
    public async Task Unknown_BR_correction_can_receive_discovered_receiver_without_mutating_local_state()
    {
        var target = CorrectionTarget(FiscalDailyReportSubmissionState.Unknown, null, 2);
        var repository = new DiscoveryRepository();
        var useCase = UseCase(
            new TargetReader(target, []),
            repository,
            new FakeGateway(Response(Item("emitter-br", "receiver-br", "BR", "2026-09-12T19:00:00"))));

        var result = await useCase.ExecuteAsync(Command(target, "discover-br"));

        Assert.Equal(FiscalDailyReportConsultationTargetKind.BrCorrectionRevision, result.TargetKind);
        Assert.Equal(2, result.LocalRevision);
        Assert.Equal("receiver-br", result.DgiReceiverId);
        Assert.Equal(FiscalDailyReportSubmissionState.Unknown, target.State);
        var stored = Assert.Single(repository.Values);
        Assert.Null(stored.RootSubmissionId);
        Assert.Equal(target.TargetId, stored.BrCorrectionRevisionId);
    }

    [Fact]
    public async Task Discovery_is_restricted_to_Unknown_target_without_receiver_id()
    {
        var received = RootTarget(FiscalDailyReportSubmissionState.Received, null);
        var gateway = new FakeGateway(Response(Item("e", "r", "AR", "t")));
        var useCase = UseCase(new TargetReader(received, []), new DiscoveryRepository(), gateway);

        var stateError = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(Command(received, "discover-received")));
        Assert.Equal("fiscal.daily_report.receiver_discovery.target_not_unknown", stateError.Code);

        var alreadyKnown = RootTarget(FiscalDailyReportSubmissionState.Unknown, "receiver-already");
        useCase = UseCase(new TargetReader(alreadyKnown, []), new DiscoveryRepository(), gateway);
        var receiverError = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(Command(alreadyKnown, "discover-known")));
        Assert.Equal("fiscal.daily_report.receiver_discovery.receiver_already_known", receiverError.Code);
        Assert.Equal(0, gateway.Calls);
    }

    [Fact]
    public async Task Zero_unaccounted_receivers_fails_closed()
    {
        var target = RootTarget(FiscalDailyReportSubmissionState.Unknown, null);
        var useCase = UseCase(
            new TargetReader(target, ["receiver-only"]),
            new DiscoveryRepository(),
            new FakeGateway(Response(Item("e", "receiver-only", "AR", "2026-09-12T20:00:00"))));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(Command(target, "discover-none")));

        Assert.Equal("fiscal.daily_report.receiver_discovery.receiver_not_found", error.Code);
    }

    [Fact]
    public async Task Multiple_unaccounted_receivers_fail_closed_without_guessing_by_time()
    {
        var target = RootTarget(FiscalDailyReportSubmissionState.Unknown, null);
        var useCase = UseCase(
            new TargetReader(target, []),
            new DiscoveryRepository(),
            new FakeGateway(Response(
                Item("e", "receiver-a", "BR", "2026-09-12T20:00:00"),
                Item("e", "receiver-b", "AR", "2026-09-12T20:01:00"))));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(Command(target, "discover-ambiguous")));

        Assert.Equal("fiscal.daily_report.receiver_discovery.receiver_ambiguous", error.Code);
    }

    [Fact]
    public async Task Reusing_operation_id_for_different_target_fails_before_network()
    {
        var firstTarget = RootTarget(FiscalDailyReportSubmissionState.Unknown, null);
        var secondTarget = RootTarget(FiscalDailyReportSubmissionState.Unknown, null);
        var repository = new DiscoveryRepository();
        repository.Values.Add(Discovery(firstTarget, "same-op", "receiver-first"));
        var gateway = new FakeGateway(Response(Item("e", "receiver-second", "AR", "t")));
        var useCase = UseCase(new TargetReader(secondTarget, []), repository, gateway);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            useCase.ExecuteAsync(Command(secondTarget, "same-op")));

        Assert.Equal("fiscal.daily_report.receiver_discovery.operation_replay_mismatch", error.Code);
        Assert.Equal(0, gateway.Calls);
    }

    [Fact]
    public void SOAP_envelope_uses_published_operation_FechaResumen_and_Secuencia_without_inventing_IdEmisor()
    {
        using var certificate = Certificate();
        var envelope = BuildEnvelope(SummaryDate, 7, certificate);

        Assert.NotNull(envelope.SelectSingleNode("//*[local-name()='WS_eFactura_Consultas.EFACCONSULTARENVIOSREPORTE']"));
        Assert.Equal("2026-09-11", envelope.SelectSingleNode("//*[local-name()='Consultaenviosreporte']/*[local-name()='FechaResumen']")?.InnerText);
        Assert.Equal("7", envelope.SelectSingleNode("//*[local-name()='Consultaenviosreporte']/*[local-name()='Secuencia']")?.InnerText);
        Assert.Null(envelope.SelectSingleNode("//*[local-name()='Consultaenviosreporte']/*[local-name()='IdEmisor']"));
        Assert.Contains("BinarySecurityToken", envelope.OuterXml, StringComparison.Ordinal);
    }

    [Fact]
    public void Parser_preserves_collection_rows_as_evidence_without_interpreting_later_state_semantics()
    {
        var response = ParseResponse(SoapResponse(
            Item("emitter-a", "receiver-a", "AR", "2026-09-12T20:00:00"),
            Item("emitter-b", "receiver-b", "BR", "2026-09-12T20:01:00")));

        Assert.Equal(2, response.Items.Count);
        Assert.Equal("receiver-a", response.Items[0].DgiReceiverId);
        Assert.Equal("AR", response.Items[0].StateCode);
        Assert.Equal("receiver-b", response.Items[1].DgiReceiverId);
        Assert.Contains("Ackconsultaenviosreporte", response.EvidenceXml, StringComparison.Ordinal);
    }

    [Fact]
    public void Parser_rejects_missing_required_row_fields_and_DTD()
    {
        var missingReceiver = "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:dgi=\"http://dgi.gub.uy\"><soapenv:Body><dgi:WS_eFactura_Consultas.EFACCONSULTARENVIOSREPORTEResponse><dgi:Ackconsultaenviosreporte><dgi:ColeccionDatosReporte><dgi:DatosReporte><dgi:IdEmisor>e</dgi:IdEmisor><dgi:Estado>AR</dgi:Estado><dgi:FechaHoraRecepcion>t</dgi:FechaHoraRecepcion></dgi:DatosReporte></dgi:ColeccionDatosReporte></dgi:Ackconsultaenviosreporte></dgi:WS_eFactura_Consultas.EFACCONSULTARENVIOSREPORTEResponse></soapenv:Body></soapenv:Envelope>";
        Assert.Throws<FiscalDailyReportReceiverDiscoveryException>(() => ParseResponse(missingReceiver));

        const string unsafeXml = "<!DOCTYPE x [<!ENTITY boom 'x'>]><x>&boom;</x>";
        Assert.Throws<FiscalDailyReportReceiverDiscoveryException>(() => ParseResponse(unsafeXml));
    }

    private static DiscoverFiscalDailyReportReceiverUseCase UseCase(
        IFiscalDailyReportReceiverDiscoveryTargetReader targets,
        IFiscalDailyReportReceiverDiscoveryRepository repository,
        IFiscalDailyReportReceiverDiscoveryGateway gateway) =>
        new(
            targets,
            repository,
            gateway,
            new FixedClock(new DateTimeOffset(2026, 9, 13, 0, 30, 0, TimeSpan.Zero)),
            new InlineTransactionManager(),
            new CountingUnitOfWork());

    private static DiscoverFiscalDailyReportReceiverCommand Command(
        FiscalDailyReportReceiverDiscoveryTarget target,
        string operationId) =>
        new(target.OrganizationId, target.Kind, target.TargetId, operationId);

    private static FiscalDailyReportReceiverDiscoveryTarget RootTarget(
        FiscalDailyReportSubmissionState state,
        string? receiver) =>
        new(
            FiscalDailyReportConsultationTargetKind.RootSubmission,
            Guid.NewGuid(),
            "company-1",
            "214748364700",
            SummaryDate,
            1,
            null,
            state,
            receiver);

    private static FiscalDailyReportReceiverDiscoveryTarget CorrectionTarget(
        FiscalDailyReportSubmissionState state,
        string? receiver,
        int revision) =>
        new(
            FiscalDailyReportConsultationTargetKind.BrCorrectionRevision,
            Guid.NewGuid(),
            "company-1",
            "214748364700",
            SummaryDate,
            1,
            revision,
            state,
            receiver);

    private static FiscalDailyReportReceiverDiscoveryItem Item(
        string emitter,
        string receiver,
        string state,
        string receptionTimestamp) =>
        new(emitter, receiver, state, receptionTimestamp);

    private static FiscalDailyReportReceiverDiscoveryResponse Response(
        params FiscalDailyReportReceiverDiscoveryItem[] items) =>
        new("<Ackconsultaenviosreporte />", items);

    private static StoredFiscalDailyReportReceiverDiscovery Discovery(
        FiscalDailyReportReceiverDiscoveryTarget target,
        string operationId,
        string receiver) =>
        new(
            Guid.NewGuid(),
            target.Kind == FiscalDailyReportConsultationTargetKind.RootSubmission ? target.TargetId : null,
            target.Kind == FiscalDailyReportConsultationTargetKind.BrCorrectionRevision ? target.TargetId : null,
            target.OrganizationId,
            target.IssuerRuc,
            target.SummaryDate,
            target.Sequence,
            target.LocalRevision,
            operationId,
            "emitter",
            receiver,
            "AR",
            "2026-09-12T20:00:00",
            "<Ackconsultaenviosreporte />",
            new string('a', 64),
            new DateTimeOffset(2026, 9, 13, 0, 30, 0, TimeSpan.Zero));

    private static XmlDocument BuildEnvelope(DateOnly date, int sequence, X509Certificate2 certificate)
    {
        var method = typeof(DgiWsSecurityFiscalDailyReportReceiverDiscoveryGateway).GetMethod(
            "BuildSoapEnvelope",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BuildSoapEnvelope was not found.");
        return (XmlDocument)(method.Invoke(null, new object[] { date, sequence, certificate })
            ?? throw new InvalidOperationException("SOAP envelope was not returned."));
    }

    private static FiscalDailyReportReceiverDiscoveryResponse ParseResponse(string soapResponse)
    {
        var method = typeof(DgiWsSecurityFiscalDailyReportReceiverDiscoveryGateway).GetMethod(
            "ParseResponse",
            BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("ParseResponse was not found.");
        try
        {
            return (FiscalDailyReportReceiverDiscoveryResponse)(method.Invoke(null, new object[] { soapResponse })
                ?? throw new InvalidOperationException("Discovery response was not returned."));
        }
        catch (TargetInvocationException ex)
            when (ex.InnerException is FiscalDailyReportReceiverDiscoveryException inner)
        {
            throw inner;
        }
    }

    private static string SoapResponse(params FiscalDailyReportReceiverDiscoveryItem[] items)
    {
        var rows = string.Concat(items.Select(item =>
            $"<dgi:DatosReporte><dgi:IdEmisor>{item.DgiEmitterId}</dgi:IdEmisor><dgi:IdReceptor>{item.DgiReceiverId}</dgi:IdReceptor><dgi:Estado>{item.StateCode}</dgi:Estado><dgi:FechaHoraRecepcion>{item.ReceptionTimestampText}</dgi:FechaHoraRecepcion></dgi:DatosReporte>"));
        return $"<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:dgi=\"http://dgi.gub.uy\"><soapenv:Body><dgi:WS_eFactura_Consultas.EFACCONSULTARENVIOSREPORTEResponse><dgi:Ackconsultaenviosreporte><dgi:ColeccionDatosReporte>{rows}</dgi:ColeccionDatosReporte></dgi:Ackconsultaenviosreporte></dgi:WS_eFactura_Consultas.EFACCONSULTARENVIOSREPORTEResponse></soapenv:Body></soapenv:Envelope>";
    }

    private static X509Certificate2 Certificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=eFactura receiver discovery test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
    }

    private sealed class TargetReader(
        FiscalDailyReportReceiverDiscoveryTarget target,
        IReadOnlyCollection<string> known) : IFiscalDailyReportReceiverDiscoveryTargetReader
    {
        public Task<FiscalDailyReportReceiverDiscoveryTarget?> GetTargetAsync(
            string organizationId,
            FiscalDailyReportConsultationTargetKind targetKind,
            Guid targetId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<FiscalDailyReportReceiverDiscoveryTarget?>(
                target.OrganizationId == organizationId && target.Kind == targetKind && target.TargetId == targetId
                    ? target
                    : null);

        public Task<IReadOnlyCollection<string>> GetKnownReceiverIdsAsync(
            string organizationId,
            string issuerRuc,
            DateOnly summaryDate,
            int sequence,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(known);
    }

    private sealed class DiscoveryRepository : IFiscalDailyReportReceiverDiscoveryRepository
    {
        public List<StoredFiscalDailyReportReceiverDiscovery> Values { get; } = [];

        public Task<StoredFiscalDailyReportReceiverDiscovery?> GetByOperationIdAsync(
            string organizationId,
            string operationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Values.SingleOrDefault(x => x.OrganizationId == organizationId && x.OperationId == operationId));

        public Task<StoredFiscalDailyReportReceiverDiscovery?> GetByTargetAsync(
            string organizationId,
            FiscalDailyReportConsultationTargetKind targetKind,
            Guid targetId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Values.SingleOrDefault(x =>
                x.OrganizationId == organizationId
                && (targetKind == FiscalDailyReportConsultationTargetKind.RootSubmission
                    ? x.RootSubmissionId == targetId
                    : x.BrCorrectionRevisionId == targetId)));

        public Task AddAsync(
            StoredFiscalDailyReportReceiverDiscovery discovery,
            CancellationToken cancellationToken = default)
        {
            Values.Add(discovery);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeGateway(params FiscalDailyReportReceiverDiscoveryResponse[] responses) : IFiscalDailyReportReceiverDiscoveryGateway
    {
        private readonly Queue<FiscalDailyReportReceiverDiscoveryResponse> _responses = new(responses);
        public int Calls { get; private set; }

        public Task<FiscalDailyReportReceiverDiscoveryResponse> QueryAsync(
            FiscalDailyReportReceiverDiscoveryRequest request,
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
