using Xunit;

namespace ArchitectureTests;

public sealed class FiscalDailyReportResponseConsultationArchitectureTests
{
    [Fact]
    public void Application_owns_consultation_policy_without_HTTP_certificate_or_infrastructure_dependencies()
    {
        var source = Read("src/Application/Fiscal/FiscalDailyReportResponseConsultation.cs");

        Assert.Contains("IFiscalDailyReportResponseConsultationGateway", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalDailyReportConsultationTargetReader", source, StringComparison.Ordinal);
        Assert.Contains("NoImmediateAck", source, StringComparison.Ordinal);
        Assert.Contains("MatchesImmediateAck", source, StringComparison.Ordinal);
        Assert.Contains("ConflictsWithImmediateAck", source, StringComparison.Ordinal);
        Assert.Contains("SHA256.HashData", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("X509Certificate", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Security.Cryptography.Xml", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalDailyReportSubmissionRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalDailyReportBrCorrectionRepository", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DGI_gateway_is_infrastructure_only_and_uses_published_original_response_operation()
    {
        var gateway = Read("src/Infrastructure/Fiscal/DgiWsSecurityFiscalDailyReportResponseConsultationGateway.cs");

        Assert.Contains("WS_eFactura_Consultas.EFACCONSULTARRESPUESTAREPORTE", gateway, StringComparison.Ordinal);
        Assert.Contains("Consultarrespuestareporte", gateway, StringComparison.Ordinal);
        Assert.Contains("IdReceptor", gateway, StringComparison.Ordinal);
        Assert.Contains("BinarySecurityToken", gateway, StringComparison.Ordinal);
        Assert.Contains("XmlDsigExcC14NTransformUrl", gateway, StringComparison.Ordinal);
        Assert.Contains("XmlDsigRSASHA1Url", gateway, StringComparison.Ordinal);
        Assert.Contains("XmlDsigSHA1Url", gateway, StringComparison.Ordinal);
        Assert.Contains("FiscalTransport:DailyReportConsultation", gateway, StringComparison.Ordinal);
        Assert.DoesNotContain("EFACCONSULTARENVIOSREPORTE", gateway, StringComparison.Ordinal);
    }

    [Fact]
    public void Consultation_persistence_is_append_only_targeted_and_operation_idempotent()
    {
        var model = Read("src/Infrastructure/Persistence/V1/Write/Models/FiscalDailyReportRecords.cs");
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalDailyReportResponseConsultationRepository.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260912203000_V1FiscalDailyReportResponseConsultation.cs");

        Assert.Contains("v1_fdr_response_consultations", model, StringComparison.Ordinal);
        Assert.Contains("RootSubmissionId", model, StringComparison.Ordinal);
        Assert.Contains("BrCorrectionRevisionId", model, StringComparison.Ordinal);
        Assert.Contains("AckXmlHash", model, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fdr_cons_operation", migration, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fdr_cons_root", migration, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fdr_cons_br", migration, StringComparison.Ordinal);
        Assert.Contains("AsNoTracking()", repository, StringComparison.Ordinal);
        Assert.Contains("V1FiscalDailyReportReceiverDiscoveryRecord", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTransaction", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("Update(", repository, StringComparison.Ordinal);
    }

    [Fact]
    public void Documentation_preserves_accepted_chain_through_PR81_with_separate_PR82_policy_candidate()
    {
        var docs = Read("documentation/blueprint-api-implementation/52_FISCAL_DAILY_REPORT_RESPONSE_CONSULTATION.md");
        var checkpoint = Read("documentation/BLUEPRINT_CURRENT_STATE.md");

        Assert.Contains("Status: ACCEPTED", docs, StringComparison.Ordinal);
        Assert.Contains("known durable DGI `IdReceptor`", docs, StringComparison.Ordinal);
        Assert.Contains("original response", docs, StringComparison.Ordinal);
        Assert.Contains("EFACCONSULTARRESPUESTAREPORTE", docs, StringComparison.Ordinal);
        Assert.Contains("EFACCONSULTARENVIOSREPORTE", docs, StringComparison.Ordinal);
        Assert.Contains("Accepted PR #80 now supplies authoritative discovery", docs, StringComparison.Ordinal);
        Assert.Contains("`DR`, `ER` and `FR`", docs, StringComparison.Ordinal);
        Assert.Contains("Automatic retry from `Unknown` remains forbidden", docs, StringComparison.Ordinal);
        Assert.Contains("Accepted PR #81 / document 54", docs, StringComparison.Ordinal);
        Assert.Contains("Pending PR #82 / document 55", docs, StringComparison.Ordinal);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", docs, StringComparison.Ordinal);
        Assert.Contains("Current pending governed increment: PR #82", checkpoint, StringComparison.Ordinal);
        Assert.Contains("main@700d0a424d6a79b52844b5fa99446d49623c758f", checkpoint, StringComparison.Ordinal);
        Assert.Contains("merge of PR #81", checkpoint, StringComparison.Ordinal);
    }

    private static string Read(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return File.ReadAllText(Path.GetFullPath(full));
    }
}
