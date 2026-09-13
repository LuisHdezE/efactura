using Xunit;

namespace ArchitectureTests;

public sealed class FiscalDailyReportReceiverDiscoveryArchitectureTests
{
    [Fact]
    public void Application_owns_discovery_policy_without_HTTP_certificate_or_infrastructure_dependencies()
    {
        var source = Read("src/Application/Fiscal/FiscalDailyReportReceiverDiscovery.cs");

        Assert.Contains("IFiscalDailyReportReceiverDiscoveryGateway", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalDailyReportReceiverDiscoveryTargetReader", source, StringComparison.Ordinal);
        Assert.Contains("GetKnownReceiverIdsAsync", source, StringComparison.Ordinal);
        Assert.Contains("novel.Length == 0", source, StringComparison.Ordinal);
        Assert.Contains("novel.Length != 1", source, StringComparison.Ordinal);
        Assert.Contains("SHA256.HashData", source, StringComparison.Ordinal);
        Assert.Contains("target.State != FiscalDailyReportSubmissionState.Unknown", source, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderBy", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("X509Certificate", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Security.Cryptography.Xml", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalDailyReportSubmissionRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalDailyReportBrCorrectionRepository", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DGI_gateway_is_infrastructure_only_and_uses_published_report_collection_operation()
    {
        var gateway = Read("src/Infrastructure/Fiscal/DgiWsSecurityFiscalDailyReportReceiverDiscoveryGateway.cs");

        Assert.Contains("WS_eFactura_Consultas.EFACCONSULTARENVIOSREPORTE", gateway, StringComparison.Ordinal);
        Assert.Contains("Consultaenviosreporte", gateway, StringComparison.Ordinal);
        Assert.Contains("FechaResumen", gateway, StringComparison.Ordinal);
        Assert.Contains("Secuencia", gateway, StringComparison.Ordinal);
        Assert.Contains("Ackconsultaenviosreporte", gateway, StringComparison.Ordinal);
        Assert.Contains("DatosReporte", gateway, StringComparison.Ordinal);
        Assert.Contains("IdReceptor", gateway, StringComparison.Ordinal);
        Assert.Contains("FechaHoraRecepcion", gateway, StringComparison.Ordinal);
        Assert.Contains("BinarySecurityToken", gateway, StringComparison.Ordinal);
        Assert.Contains("XmlDsigExcC14NTransformUrl", gateway, StringComparison.Ordinal);
        Assert.Contains("XmlDsigRSASHA1Url", gateway, StringComparison.Ordinal);
        Assert.Contains("XmlDsigSHA1Url", gateway, StringComparison.Ordinal);
        Assert.Contains("FiscalTransport:DailyReportReceiverDiscovery", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("document.CreateElement(\"dgi\", \"IdEmisor\"", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("EFACCONSULTARRESPUESTAREPORTE", gateway, StringComparison.Ordinal);
    }

    [Fact]
    public void Discovery_persistence_is_append_only_unique_and_chains_into_response_consultation()
    {
        var model = Read("src/Infrastructure/Persistence/V1/Write/Models/FiscalDailyReportRecords.cs");
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalDailyReportReceiverDiscoveryRepository.cs");
        var consultationRepository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalDailyReportResponseConsultationRepository.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260913003500_V1FiscalDailyReportReceiverDiscovery.cs");

        Assert.Contains("v1_fdr_receiver_discoveries", model, StringComparison.Ordinal);
        Assert.Contains("DgiEmitterId", model, StringComparison.Ordinal);
        Assert.Contains("DgiReceiverId", model, StringComparison.Ordinal);
        Assert.Contains("DgiReceptionTimestampText", model, StringComparison.Ordinal);
        Assert.Contains("EvidenceXmlHash", model, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fdr_disc_operation", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fdr_disc_receiver", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fdr_disc_root", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fdr_disc_br", migration, StringComparison.Ordinal);
        Assert.Contains("AsNoTracking()", repository, StringComparison.Ordinal);
        Assert.Contains("V1FiscalDailyReportReceiverDiscoveryRecord", consultationRepository, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTransaction", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("Update(", repository, StringComparison.Ordinal);
    }

    [Fact]
    public void Documentation_preserves_accepted_fail_closed_discovery_with_PR81_observation_and_separate_PR82_policy()
    {
        var docs = Read("documentation/blueprint-api-implementation/53_FISCAL_DAILY_REPORT_RECEIVER_DISCOVERY.md");
        var checkpoint = Read("documentation/BLUEPRINT_CURRENT_STATE.md");

        Assert.Contains("Status: ACCEPTED IMPLEMENTATION", docs, StringComparison.Ordinal);
        Assert.Contains("main@7e930d8ffe1978da24ea8365b9661f74162d51b5", docs, StringComparison.Ordinal);
        Assert.Contains("EFACCONSULTARENVIOSREPORTE", docs, StringComparison.Ordinal);
        Assert.Contains("FechaResumen + Secuencia", docs, StringComparison.Ordinal);
        Assert.Contains("deliberately omits optional `IdEmisor`", docs, StringComparison.Ordinal);
        Assert.Contains("No time-based heuristic is allowed", docs, StringComparison.Ordinal);
        Assert.Contains("zero unaccounted receiver ids", docs, StringComparison.Ordinal);
        Assert.Contains("more than one unaccounted receiver id", docs, StringComparison.Ordinal);
        Assert.Contains("does not update the root submission or correction revision", docs, StringComparison.Ordinal);
        Assert.Contains("Document 54", docs, StringComparison.Ordinal);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", docs, StringComparison.Ordinal);

        Assert.Contains("main@700d0a424d6a79b52844b5fa99446d49623c758f", checkpoint, StringComparison.Ordinal);
        Assert.Contains("merge of PR #81", checkpoint, StringComparison.Ordinal);
        Assert.Contains("Current pending governed increment: PR #82", checkpoint, StringComparison.Ordinal);
        Assert.Contains("Append-only observation of DGI Reporte Diario later states `DR`, `ER` and `FR`", checkpoint, StringComparison.Ordinal);
        Assert.Contains("never selects by timestamp or returned collection order", checkpoint, StringComparison.Ordinal);
        Assert.Contains("AutomaticReliquidationAuthorized = false", checkpoint, StringComparison.Ordinal);
    }

    private static string Read(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return File.ReadAllText(Path.GetFullPath(full));
    }
}
