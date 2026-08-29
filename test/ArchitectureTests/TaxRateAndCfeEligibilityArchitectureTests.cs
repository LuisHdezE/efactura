using Xunit;

namespace ArchitectureTests;

public sealed class TaxRateAndCfeEligibilityArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Vat_rate_resolver_remains_application_owned_and_framework_free()
    {
        var source = Read("src/Application/Taxation/UruguayRelease1VatRateRules.cs");

        Assert.Contains("ResolveTaxRateUseCase", source, StringComparison.Ordinal);
        Assert.Contains("22m", source, StringComparison.Ordinal);
        Assert.Contains("10m", source, StringComparison.Ordinal);
        Assert.Contains("VAT_BASIC", source, StringComparison.Ordinal);
        Assert.Contains("VAT_MINIMUM", source, StringComparison.Ordinal);

        Assert.DoesNotContain("Infrastructure", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", source, StringComparison.Ordinal);
        Assert.DoesNotContain("EntityFramework", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MySql", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HttpContext", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Cfe_eligibility_is_preparation_only_and_cannot_issue_fiscal_documents()
    {
        var domain = Read("src/Domain/Fiscal/CfeEligibility.cs");
        var application = Read("src/Application/Fiscal/UruguayCfe25_2EligibilityPreparation.cs");
        var combined = domain + Environment.NewLine + application;

        Assert.Contains("CfeEligibilityPolicy", domain, StringComparison.Ordinal);
        Assert.Contains("PrepareCfeEligibilityUseCase", application, StringComparison.Ordinal);
        Assert.Contains("5000m", application, StringComparison.Ordinal);
        Assert.Contains("CFE-25.2", application, StringComparison.Ordinal);
        Assert.Contains("exportServiceStrategyVerifiedCurrent: false", application, StringComparison.Ordinal);

        Assert.DoesNotContain("Infrastructure", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("EntityFramework", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Npgsql", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MySql", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ICfeIssuer", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CaeAuthorization", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SignXml", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SendToDgi", combined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Fiscal_receiver_eligibility_does_not_reintroduce_is_foreign_shortcut()
    {
        var domain = Read("src/Domain/Fiscal/CfeEligibility.cs");

        Assert.DoesNotContain("IsForeign", domain, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("HasUruguayanRuc", domain, StringComparison.Ordinal);
        Assert.Contains("IssuingCountry", domain, StringComparison.Ordinal);
        Assert.Contains("TypeCode", domain, StringComparison.Ordinal);
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
