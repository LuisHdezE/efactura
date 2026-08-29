using Xunit;

namespace ArchitectureTests;

public sealed class SalesFiscalPreviewArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Sales_controller_exposes_only_API_SAL_001_to_006_in_this_slice()
    {
        var content = Read("src/WebApi/Controllers/V1/SalesController.cs");

        Assert.Contains("[Route(\"api/v1/sales\")]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpPost]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"{saleId:guid}\")]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpPatch(\"{saleId:guid}\")]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpPost(\"{saleId:guid}/validate\")]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"{saleId:guid}/fiscal-preview\")]", content, StringComparison.Ordinal);

        Assert.DoesNotContain("/confirm", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/cancel", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/fiscalization", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sales_public_contract_does_not_accept_authoritative_export_or_Article34_booleans()
    {
        var contracts = Read("src/WebApi/Controllers/V1/Contracts/SalesContracts.cs");
        var controller = Read("src/WebApi/Controllers/V1/SalesController.cs");

        Assert.DoesNotContain("GoodsExportConfirmed", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("ExclusiveUseAbroad", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("RecipientIsPersonAbroad", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("ForeignEconomicRelation", contracts, StringComparison.Ordinal);
        Assert.Contains("GoodsExportConfirmed: false", controller, StringComparison.Ordinal);
        Assert.Contains("SaleRegulatoryFactStatus.Unknown", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Sales_domain_remains_framework_free_and_does_not_depend_on_Taxation_or_Fiscal_modules()
    {
        var content = Read("src/Domain/Sales/Sale.cs");

        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.AspNetCore", content, StringComparison.Ordinal);
        Assert.DoesNotContain("EFactura.Domain.Taxation", content, StringComparison.Ordinal);
        Assert.DoesNotContain("EFactura.Domain.Fiscal", content, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Sales_write_repository_keeps_transaction_and_SaveChanges_ownership_outside_repository()
    {
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfSaleRepository.cs");

        Assert.DoesNotContain("SaveChanges", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTransaction", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("Commit", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("Rollback", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("using Dapper", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecuteSql", repository, StringComparison.Ordinal);
    }

    [Fact]
    public void Cfe_selector_cannot_override_an_unresolved_eligibility_decision()
    {
        var selector = Read("src/Domain/Fiscal/CfeSelection.cs");
        var reviewGuard = selector.IndexOf("eligibility.Status == CfeEligibilityStatus.RequiresReview", StringComparison.Ordinal);
        var exportSelection = selector.IndexOf("treatment.Classification == TaxTreatmentClassification.ExportServices", StringComparison.Ordinal);

        Assert.True(reviewGuard >= 0);
        Assert.True(exportSelection > reviewGuard);
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
        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
