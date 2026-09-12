using Xunit;

namespace ArchitectureTests;

public sealed class FiscalDailyReportDurableReplayArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Daily_report_evidence_is_durable_before_private_key_and_replay_checks_storage_first()
    {
        var workflow = Read("src/Application/Fiscal/FiscalDailyReportDurableWorkflow.cs");
        var evidenceLookup = workflow.IndexOf("_evidence.GetByIdentityAsync", StringComparison.Ordinal);
        var clock = workflow.IndexOf("_signingTime.GetSigningTimestamp()", StringComparison.Ordinal);
        var artifactLookup = workflow.IndexOf("_artifacts.GetByIdentityAsync", StringComparison.Ordinal);
        var signer = workflow.IndexOf("_signatureProvider.SignAsync", StringComparison.Ordinal);

        Assert.True(evidenceLookup >= 0);
        Assert.True(clock > evidenceLookup);
        Assert.True(artifactLookup >= 0);
        Assert.True(signer > artifactLookup);
        Assert.Contains("return EvidenceResult(existing, true)", workflow, StringComparison.Ordinal);
        Assert.Contains("return ArtifactResult(existing, true)", workflow, StringComparison.Ordinal);
        Assert.Contains("WholeSecond(_signingTime.GetSigningTimestamp())", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTimeOffset.UtcNow", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("EntityFrameworkCore", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void Persistence_uses_report_identity_uniqueness_and_keeps_CFE_artifact_tables_separate()
    {
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260912032000_V1FiscalDailyReportDurableReplay.cs");
        var repositories = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalDailyReportRepositories.cs");
        var model = Read("src/Infrastructure/Persistence/V1/V1PersistenceModelCustomizer.cs");

        Assert.Contains("v1_fiscal_daily_report_signing_evidence", migration, StringComparison.Ordinal);
        Assert.Contains("v1_fiscal_daily_report_signed_artifacts", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fdr_evidence_identity", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fdr_artifact_identity", migration, StringComparison.Ordinal);
        Assert.Contains("OrganizationId", migration, StringComparison.Ordinal);
        Assert.Contains("IssuerRuc", migration, StringComparison.Ordinal);
        Assert.Contains("SummaryDate", migration, StringComparison.Ordinal);
        Assert.Contains("Sequence", migration, StringComparison.Ordinal);
        Assert.Contains("AsNoTracking()", repositories, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", repositories, StringComparison.Ordinal);
        Assert.Contains("ConfigureFiscalDailyReportDurability(modelBuilder)", model, StringComparison.Ordinal);
    }

    [Fact]
    public void Implementation_record_preserves_transport_and_formal_DGI_gates()
    {
        var document = Read("documentation/blueprint-api-implementation/48_FISCAL_DAILY_REPORT_DURABLE_REPLAY.md");

        Assert.Contains("RUC + FechaResumen + SecEnvio", document, StringComparison.Ordinal);
        Assert.Contains("private key", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EFACRECEPCIONREPORTE", document, StringComparison.Ordinal);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", document, StringComparison.Ordinal);
        Assert.DoesNotContain("READY FOR DGI TESTING", document, StringComparison.OrdinalIgnoreCase);
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "api-accounting.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Repository root containing api-accounting.sln was not found.");
    }
}
