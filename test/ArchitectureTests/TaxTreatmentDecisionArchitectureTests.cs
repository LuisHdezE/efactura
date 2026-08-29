using Xunit;

namespace ArchitectureTests;

public sealed class TaxTreatmentDecisionArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Tax_treatment_engine_does_not_use_foreign_shortcut_or_calculate_rates()
    {
        var domain = Read("src/Domain/Taxation/TaxTreatmentDecision.cs");

        Assert.Contains("TaxTreatmentDecisionEngine", domain, StringComparison.Ordinal);
        Assert.Contains("ExportGoods", domain, StringComparison.Ordinal);
        Assert.Contains("ExportServices", domain, StringComparison.Ordinal);
        Assert.Contains("OutsideVatTerritorialScope", domain, StringComparison.Ordinal);
        Assert.Contains("RequiresReview", domain, StringComparison.Ordinal);

        Assert.DoesNotContain("IsForeign", domain, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RatePercent", domain, StringComparison.Ordinal);
        Assert.DoesNotContain("TaxProfile", domain, StringComparison.Ordinal);
        Assert.DoesNotContain("Cfe", domain, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Tax_treatment_application_owns_rule_provider_and_export_service_evaluator_ports()
    {
        var application = Read("src/Application/Taxation/TaxTreatmentDecisionApplication.cs");

        Assert.Contains("ITaxTreatmentRulePackProvider", application, StringComparison.Ordinal);
        Assert.Contains("IExportServiceEligibilityEvaluator", application, StringComparison.Ordinal);
        Assert.Contains("ResolveTaxTreatmentUseCase", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure", application, StringComparison.Ordinal);
        Assert.DoesNotContain("EntityFramework", application, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", application, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MySql", application, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Receiver_uruguayan_ruc_is_derived_from_typed_identity_not_stored_as_foreign_flag()
    {
        var domain = Read("src/Domain/Taxation/TaxTreatmentDecision.cs");

        Assert.Contains("HasUruguayanRuc", domain, StringComparison.Ordinal);
        Assert.Contains("TypeCode", domain, StringComparison.Ordinal);
        Assert.Contains("IssuingCountry", domain, StringComparison.Ordinal);
        Assert.DoesNotContain("bool IsForeign", domain, StringComparison.OrdinalIgnoreCase);
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "api-accounting.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from architecture test output directory.");
    }
}
