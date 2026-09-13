using Xunit;

namespace ArchitectureTests;

public sealed class FiscalCfeEnvelopeDocumentResponseConsultationArchitectureTests
{
    [Fact]
    public void Application_boundary_is_ports_only_append_only_and_does_not_infer_complete_Sobre_resolution()
    {
        var application = Read("src/Application/Fiscal/FiscalCfeEnvelopeDocumentResponseConsultation.cs");

        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseConsultationGateway", application, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseConsultationRepository", application, StringComparison.Ordinal);
        Assert.Contains("ConsultFiscalCfeEnvelopeDocumentResponseUseCase", application, StringComparison.Ordinal);
        Assert.Contains("DGI permits per-CFE results to be produced in one or multiple messages", application, StringComparison.Ordinal);
        Assert.Contains("FiscalCfeEnvelopeAckState.Received", application, StringComparison.Ordinal);
        Assert.Contains("ConsultationTokenSha256", application, StringComparison.Ordinal);
        Assert.Contains("response.Details.Count != response.RespondedCount", application, StringComparison.Ordinal);
        Assert.Contains("expectedDocuments.Contains(identity)", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", application, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Mark", application, StringComparison.Ordinal);
    }

    [Fact]
    public void Infrastructure_adapter_uses_published_ws_efactura_IdReceptor_Token_contract_fail_closed()
    {
        var gateway = Read("src/Infrastructure/Fiscal/DgiWsSecurityFiscalCfeEnvelopeDocumentResponseConsultationGateway.cs");

        Assert.Contains("WS_eFactura.EFACCONSULTARESTADOENVIO", gateway, StringComparison.Ordinal);
        Assert.Contains("ConsultaCFE", gateway, StringComparison.Ordinal);
        Assert.Contains("IdReceptor", gateway, StringComparison.Ordinal);
        Assert.Contains("Token", gateway, StringComparison.Ordinal);
        Assert.Contains("CreateCDataSection", gateway, StringComparison.Ordinal);
        Assert.Contains("ACKCFE_det", gateway, StringComparison.Ordinal);
        Assert.Contains("CantResponden", gateway, StringComparison.Ordinal);
        Assert.Contains("DtdProcessing = DtdProcessing.Prohibit", gateway, StringComparison.Ordinal);
        Assert.Contains("XmlResolver = null", gateway, StringComparison.Ordinal);
        Assert.Contains("FiscalTransport:CfeDocumentResponseConsultation", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("GZip", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("EFACCONSULTARESTADOCFE", gateway, StringComparison.Ordinal);
    }

    [Fact]
    public void Persistence_and_documentation_keep_ACKCFE_evidence_append_only_and_formal_DGI_gate_blocked()
    {
        var model = Read("src/Infrastructure/Persistence/V1/Write/Models/FiscalCfeDocumentResponseConsultationRecord.cs");
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalCfeEnvelopeDocumentResponseConsultationRepository.cs");
        var customizer = Read("src/Infrastructure/Persistence/V1/V1PersistenceCfeDocumentResponseModelCustomizer.cs");
        var configurator = Read("src/Infrastructure/Persistence/V1/V1PersistenceDatabaseConfigurator.cs");
        var services = Read("src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260913223000_V1FiscalCfeDocumentResponseConsultation.cs");
        var documentation = Read("documentation/blueprint-api-implementation/64_FISCAL_CFE_DOCUMENT_RESPONSE_CONSULTATION.md");

        Assert.Contains("v1_fiscal_cfe_document_response_consultations", model, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fcdrc_operation", model, StringComparison.Ordinal);
        Assert.Contains("AsNoTracking", repository, StringComparison.Ordinal);
        Assert.Contains("OnDelete(DeleteBehavior.Restrict)", customizer, StringComparison.Ordinal);
        Assert.Contains("V1PersistenceEnvelopeModelCustomizer", customizer, StringComparison.Ordinal);
        Assert.Contains("V1PersistenceCfeDocumentResponseModelCustomizer", configurator, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseConsultationGateway", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalCfeEnvelopeDocumentResponseConsultationRepository", services, StringComparison.Ordinal);
        Assert.Contains("ConsultFiscalCfeEnvelopeDocumentResponseUseCase", services, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fcdrc_ack_observation", migration, StringComparison.Ordinal);
        Assert.Contains("multiple messages or one response", documentation, StringComparison.Ordinal);
        Assert.Contains("does **not**", documentation, StringComparison.Ordinal);
        Assert.Contains("**BLOCKED BY MISSING PRODUCT CAPABILITIES**", documentation, StringComparison.Ordinal);
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "api-accounting.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Repository root containing api-accounting.sln was not found.");
    }
}
