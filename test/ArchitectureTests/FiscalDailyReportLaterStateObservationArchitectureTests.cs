using Xunit;

namespace ArchitectureTests;

public sealed class FiscalDailyReportLaterStateObservationArchitectureTests
{
    [Fact]
    public void Application_owns_later_state_policy_without_transport_or_persistence_dependencies()
    {
        var source = Read("src/Application/Fiscal/FiscalDailyReportLaterStateObservation.cs");

        Assert.Contains("IFiscalDailyReportConsultationTargetReader", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalDailyReportReceiverDiscoveryGateway", source, StringComparison.Ordinal);
        Assert.Contains("\"DR\" => FiscalDailyReportLaterState.Processed", source, StringComparison.Ordinal);
        Assert.Contains("\"ER\" => FiscalDailyReportLaterState.InManagement", source, StringComparison.Ordinal);
        Assert.Contains("\"FR\" => FiscalDailyReportLaterState.Reliquidated", source, StringComparison.Ordinal);
        Assert.Contains("later_state.not_available", source, StringComparison.Ordinal);
        Assert.Contains("later_state.unsupported_state", source, StringComparison.Ordinal);
        Assert.Contains("later_state.receiver_not_returned", source, StringComparison.Ordinal);
        Assert.Contains("later_state.receiver_duplicate", source, StringComparison.Ordinal);
        Assert.Contains("SHA256.HashData", source, StringComparison.Ordinal);
        Assert.DoesNotContain("OrderBy", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("X509Certificate", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalDailyReportSubmissionRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalDailyReportBrCorrectionRepository", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Persistence_is_append_only_migration_owned_and_joins_ambient_transaction()
    {
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalDailyReportLaterStateObservationRepository.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260913012000_V1FiscalDailyReportLaterStateObservation.cs");

        Assert.Contains("v1_fdr_later_state_observations", repository, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO", repository, StringComparison.Ordinal);
        Assert.Contains("SELECT", repository, StringComparison.Ordinal);
        Assert.Contains("CurrentTransaction", repository, StringComparison.Ordinal);
        Assert.Contains("GetDbTransaction", repository, StringComparison.Ordinal);
        Assert.Contains("Npgsql", repository, StringComparison.Ordinal);
        Assert.Contains("MySql", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("UPDATE ", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("DELETE ", repository, StringComparison.Ordinal);

        Assert.Contains("v1_fdr_later_state_observations", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fdr_later_operation", migration, StringComparison.Ordinal);
        Assert.Contains("IX_v1_fdr_later_receiver", migration, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fdr_later_root", migration, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fdr_later_br", migration, StringComparison.Ordinal);
        Assert.Contains("ReferentialAction.Restrict", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("unique: true);\n\n        migrationBuilder.CreateIndex(\n            name: \"IX_v1_fdr_later_receiver\"", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void Documentation_keeps_observation_separate_from_state_changing_reconciliation()
    {
        var docs = Read("documentation/blueprint-api-implementation/54_FISCAL_DAILY_REPORT_LATER_STATE_OBSERVATION.md");
        var checkpoint = Read("documentation/BLUEPRINT_CURRENT_STATE.md");

        Assert.Contains("Status: GOVERNED IMPLEMENTATION CANDIDATE", docs, StringComparison.Ordinal);
        Assert.Contains("`DR` -> `Processed`", docs, StringComparison.Ordinal);
        Assert.Contains("`ER` -> `InManagement`", docs, StringComparison.Ordinal);
        Assert.Contains("`FR` -> `Reliquidated`", docs, StringComparison.Ordinal);
        Assert.Contains("`AR` and `BR` are not persisted as later-state observations", docs, StringComparison.Ordinal);
        Assert.Contains("does **not** rewrite", docs, StringComparison.Ordinal);
        Assert.Contains("Multiple observations of the same receiver are allowed", docs, StringComparison.Ordinal);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", docs, StringComparison.Ordinal);

        Assert.Contains("Current pending governed increment: PR #81", checkpoint, StringComparison.Ordinal);
        Assert.Contains("main@7e930d8ffe1978da24ea8365b9661f74162d51b5", checkpoint, StringComparison.Ordinal);
        Assert.Contains("state-changing local reconciliation semantics", checkpoint, StringComparison.Ordinal);
        Assert.Contains("PR #81 remains pending until exact-head CI is green and human review is complete", checkpoint, StringComparison.Ordinal);
    }

    private static string Read(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return File.ReadAllText(Path.GetFullPath(full));
    }
}
