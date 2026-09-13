using Xunit;

namespace ArchitectureTests;

public sealed class FiscalDailyReportTransportArchitectureTests
{
    [Fact]
    public void Application_owns_submission_lifecycle_but_not_HTTP_or_certificate_implementation()
    {
        var source = Read("src/Application/Fiscal/FiscalDailyReportTransport.cs");
        Assert.Contains("IFiscalDailyReportTransportGateway", source, StringComparison.Ordinal);
        Assert.Contains("Prepared", source, StringComparison.Ordinal);
        Assert.Contains("InFlight", source, StringComparison.Ordinal);
        Assert.Contains("Unknown", source, StringComparison.Ordinal);
        Assert.Contains("previous_sequence_not_received", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("X509Certificate", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Security.Cryptography.Xml", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DGI_gateway_is_infrastructure_only_and_SHA1_is_transport_scoped()
    {
        var gateway = Read("src/Infrastructure/Fiscal/DgiWsSecurityFiscalDailyReportTransportGateway.cs");
        var reportSigner = Read("src/Infrastructure/Fiscal/XmlDsigFiscalDailyReportSignatureProvider.cs");

        Assert.Contains("EFACRECEPCIONREPORTE", gateway, StringComparison.Ordinal);
        Assert.Contains("BinarySecurityToken", gateway, StringComparison.Ordinal);
        Assert.Contains("XmlDsigExcC14NTransformUrl", gateway, StringComparison.Ordinal);
        Assert.Contains("XmlDsigRSASHA1Url", gateway, StringComparison.Ordinal);
        Assert.Contains("XmlDsigSHA1Url", gateway, StringComparison.Ordinal);
        Assert.Contains("MUST NOT be", gateway, StringComparison.Ordinal);
        Assert.Contains("SHA1", gateway, StringComparison.Ordinal);

        Assert.Contains("profile_sha1_forbidden", reportSigner, StringComparison.Ordinal);
        Assert.Contains("FiscalXmlSignatureProfile.RsaSha256", reportSigner, StringComparison.Ordinal);
        Assert.Contains("FiscalXmlSignatureProfile.Sha256", reportSigner, StringComparison.Ordinal);
        Assert.DoesNotContain("EFACRECEPCIONREPORTE", reportSigner, StringComparison.Ordinal);
    }

    [Fact]
    public void Submission_persistence_is_separate_unique_serialized_and_repository_does_not_commit()
    {
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260912061500_V1FiscalDailyReportTransport.cs");
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalDailyReportSubmissionRepository.cs");

        Assert.Contains("v1_fiscal_daily_report_submissions", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fdr_submission_identity", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fdr_submission_operation", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fdr_submission_artifact", migration, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fdr_submission_artifact", migration, StringComparison.Ordinal);
        Assert.Contains("CurrentTransaction", repository, StringComparison.Ordinal);
        Assert.Contains("FOR UPDATE", repository, StringComparison.Ordinal);
        Assert.Contains("FromSqlInterpolated", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTransaction", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("FromSqlRaw", repository, StringComparison.Ordinal);
    }

    [Fact]
    public void Documentation_recognizes_BR_same_sequence_follow_up_and_keeps_later_reconciliation_gated()
    {
        var docs = Read("documentation/blueprint-api-implementation/50_FISCAL_DAILY_REPORT_TRANSPORT.md");
        Assert.Contains("same `SecEnvio`", docs, StringComparison.Ordinal);
        Assert.Contains("former BR same-sequence gap is addressed by the accepted lifecycle", docs, StringComparison.Ordinal);
        Assert.Contains("51_FISCAL_DAILY_REPORT_BR_SAME_SEQUENCE_CORRECTION.md", docs, StringComparison.Ordinal);
        Assert.Contains("blocks `R05` fail-closed", docs, StringComparison.Ordinal);
        Assert.Contains("EFACCONSULTARRESPUESTAREPORTE", docs, StringComparison.Ordinal);
        Assert.Contains("DR` / `ER` / `FR", docs, StringComparison.Ordinal);
        Assert.Contains("automatic retry from `Unknown`", docs, StringComparison.Ordinal);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", docs, StringComparison.Ordinal);
    }

    private static string Read(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return File.ReadAllText(Path.GetFullPath(full));
    }
}
