using Xunit;

namespace ArchitectureTests;

public sealed class SaleConfirmationPlanningArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Confirmation_planning_is_application_owned_and_consumes_authoritative_CFE_arithmetic_and_selection_provenance()
    {
        var content = Read("src/Application/Sales/SaleConfirmationPlanning.cs");

        Assert.Contains("CfeArithmeticCalculator", content, StringComparison.Ordinal);
        Assert.Contains("UruguayCfe25_2ArithmeticCatalog.Current", content, StringComparison.Ordinal);
        Assert.Contains("CfeSelectionResult", content, StringComparison.Ordinal);
        Assert.Contains("selection.RuleEvidence", content, StringComparison.Ordinal);
        Assert.Contains("selection.FormatVersion", content, StringComparison.Ordinal);
        Assert.Contains("SaleStatus.Validated", content, StringComparison.Ordinal);
        Assert.Contains("ValidationFingerprint", content, StringComparison.Ordinal);
        Assert.Contains("ConfirmationFingerprint", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", content, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Confirmation_plan_preserves_inventory_expectations_without_mutating_stock()
    {
        var content = Read("src/Application/Sales/SaleConfirmationPlanning.cs");

        Assert.Contains("InventoryAvailabilityResult", content, StringComparison.Ordinal);
        Assert.Contains("PositionVersion", content, StringComparison.Ordinal);
        Assert.Contains("inventory_quantity_mismatch", content, StringComparison.Ordinal);
        Assert.DoesNotContain("IInventoryRepository", content, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplyAdjustment", content, StringComparison.Ordinal);
        Assert.DoesNotContain("SavePositionAsync", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Planning_slice_does_not_cross_the_irreversible_confirmation_boundary()
    {
        var planner = Read("src/Application/Sales/SaleConfirmationPlanning.cs");
        var controller = Read("src/WebApi/Controllers/V1/SalesController.cs");

        Assert.DoesNotContain("IFiscalNumberAllocator", planner, StringComparison.Ordinal);
        Assert.DoesNotContain("IUnitOfWork", planner, StringComparison.Ordinal);
        Assert.DoesNotContain("IOutboxWriter", planner, StringComparison.Ordinal);
        Assert.DoesNotContain("Receivable", planner, StringComparison.Ordinal);
        Assert.DoesNotContain("Payment", planner, StringComparison.Ordinal);
        Assert.DoesNotContain("/confirm", controller, StringComparison.OrdinalIgnoreCase);
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
