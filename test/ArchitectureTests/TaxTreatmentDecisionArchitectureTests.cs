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

    [Fact]
    public void Release1_regulatory_policy_uses_explicit_facts_and_has_no_rate_or_cfe_leakage()
    {
        var policy = Read("src/Application/Taxation/UruguayRelease1TaxRules.cs");

        Assert.Contains("RecipientIsPersonAbroad", policy, StringComparison.Ordinal);
        Assert.Contains("ExclusiveUseAbroad", policy, StringComparison.Ordinal);
        Assert.Contains("ForeignEconomicRelation", policy, StringComparison.Ordinal);
        Assert.Contains("RecipientInstalledInFreeZone", policy, StringComparison.Ordinal);
        Assert.Contains("ProviderFromNonFreeNationalTerritory", policy, StringComparison.Ordinal);
        Assert.Contains("https://www.impo.com.uy/bases/decretos/220-1998/34", policy, StringComparison.Ordinal);
        Assert.Contains("https://www.impo.com.uy/bases/todgi2023/101-2024/5_T10", policy, StringComparison.Ordinal);

        Assert.DoesNotContain("IsForeign", policy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RatePercent", policy, StringComparison.Ordinal);
        Assert.DoesNotContain("TaxProfile", policy, StringComparison.Ordinal);
        Assert.DoesNotContain("Cfe", policy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("EntityFramework", policy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", policy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MySql", policy, StringComparison.OrdinalIgnoreCase);
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
