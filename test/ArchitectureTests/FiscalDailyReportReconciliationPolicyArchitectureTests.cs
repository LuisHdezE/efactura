using Xunit;

namespace ArchitectureTests;

public sealed class FiscalDailyReportReconciliationPolicyArchitectureTests
{
    [Fact]
    public void Application_policy_is_evidence_driven_and_side_effect_free()
    {
        var source = Read("src/Application/Fiscal/FiscalDailyReportReconciliationPolicy.cs");

        Assert.Contains("IFiscalDailyReportLatestObservationReader", source, StringComparison.Ordinal);
        Assert.Contains("FiscalDailyReportReconciliationDisposition.Consistent", source, StringComparison.Ordinal);
        Assert.Contains("FiscalDailyReportReconciliationDisposition.ManualReviewRequired", source, StringComparison.Ordinal);
        Assert.Contains("FiscalDailyReportReconciliationDisposition.ReliquidatedExternally", source, StringComparison.Ordinal);
        Assert.Contains("AutomaticReliquidationAuthorized: false", source, StringComparison.Ordinal);
        Assert.Contains("AutomaticLocalMutationAuthorized: false", source, StringComparison.Ordinal);
        Assert.Contains("dgi_inconsistencies_require_analysis", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IUnitOfWork", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ITransactionManager", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalDailyReportVersionRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AllocateFiscalDailyReportVersionUseCase", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Persistence_reader_selects_latest_observation_without_mutation()
    {
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalDailyReportLaterStateObservationRepository.cs");

        Assert.Contains("IFiscalDailyReportLatestObservationReader", repository, StringComparison.Ordinal);
        Assert.Contains("GetLatestByReceiverIdAsync", repository, StringComparison.Ordinal);
        Assert.Contains("OrderByDescending(x => x.ObservedAtUtc)", repository, StringComparison.Ordinal);
        Assert.Contains("ThenByDescending(x => x.Id)", repository, StringComparison.Ordinal);
        Assert.Contains("AsNoTracking()", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("UPDATE ", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("DELETE ", repository, StringComparison.Ordinal);
        Assert.DoesNotContain(".SaveChanges", repository, StringComparison.Ordinal);
    }

    [Fact]
    public void Documentation_keeps_ER_review_manual_and_FR_non_mutating()
    {
        var docs = Read("documentation/blueprint-api-implementation/55_FISCAL_DAILY_REPORT_RECONCILIATION_POLICY.md");

        Assert.Contains("`DR` -> `Consistent`", docs, StringComparison.Ordinal);
        Assert.Contains("`ER` -> `ManualReviewRequired`", docs, StringComparison.Ordinal);
        Assert.Contains("`FR` -> `ReliquidatedExternally`", docs, StringComparison.Ordinal);
        Assert.Contains("does not authorize automatic reliquidation", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not mutate", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", docs, StringComparison.Ordinal);
    }

    private static string Read(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return File.ReadAllText(Path.GetFullPath(full));
    }
}
