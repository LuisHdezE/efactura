using Xunit;

namespace ArchitectureTests;

public sealed class FiscalDailyReportHighValueAutomationArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void High_value_automation_stays_domain_only_and_fail_closed_for_unpinned_currency_semantics()
    {
        var source = Read("src/Domain/Fiscal/FiscalDailyReportHighValueAutomation.cs");

        Assert.DoesNotContain("System.Xml", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", source, StringComparison.Ordinal);
        Assert.Contains("CurrentThresholdUi = 5_000m", source, StringComparison.Ordinal);
        Assert.Contains("new(2022, 11, 1)", source, StringComparison.Ordinal);
        Assert.Contains("new DateOnly(reportDate.Year - 1, 12, 31)", source, StringComparison.Ordinal);
        Assert.Contains("document.NetAmount > annualUiQuote.ThresholdUyu", source, StringComparison.Ordinal);
        Assert.Contains("high_value_foreign_currency_policy_required", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Cfe_eligibility_and_daily_report_share_one_5000_UI_threshold_policy()
    {
        var eligibility = Read("src/Application/Fiscal/UruguayCfe25_2EligibilityPreparation.cs");

        Assert.Contains("UruguayCfeHighValueThresholdPolicy.CurrentThresholdUi", eligibility, StringComparison.Ordinal);
        Assert.Contains("UruguayCfeHighValueThresholdPolicy.CurrentThresholdEffectiveFrom", eligibility, StringComparison.Ordinal);
        Assert.DoesNotContain("        5000m,", eligibility, StringComparison.Ordinal);
    }

    [Fact]
    public void High_value_automation_documentation_keeps_serializer_and_quantization_gates_explicit()
    {
        var document = Read("documentation/blueprint-api-implementation/44_FISCAL_DAILY_REPORT_HIGH_VALUE_AUTOMATION.md");

        Assert.Contains("31/12", document, StringComparison.Ordinal);
        Assert.Contains("5.000 UI", document, StringComparison.Ordinal);
        Assert.Contains("strictly greater", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("foreign-currency", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("quantization", document, StringComparison.OrdinalIgnoreCase);
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
