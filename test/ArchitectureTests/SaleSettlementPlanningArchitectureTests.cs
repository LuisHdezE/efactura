using Xunit;

namespace ArchitectureTests;

public sealed class SaleSettlementPlanningArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Settlement_planning_is_chained_to_authoritative_confirmation_instead_of_accepting_client_total()
    {
        var content = Read("src/Application/Sales/SaleSettlementPlanning.cs");

        Assert.Contains("SaleConfirmationPlan Confirmation", content, StringComparison.Ordinal);
        Assert.Contains("request.Confirmation.FiscalCalculation.Totals.TotalAmount", content, StringComparison.Ordinal);
        Assert.Contains("ConfirmationFingerprint", content, StringComparison.Ordinal);
        Assert.DoesNotContain("ClientTotal", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", content, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Credit_amount_is_server_derived_from_residual_and_advanced_credit_policy_remains_outside_slice()
    {
        var content = Read("src/Application/Sales/SaleSettlementPlanning.cs");

        Assert.Contains("var residual = totalAmount - immediateTotal", content, StringComparison.Ordinal);
        Assert.Contains("PlannedSaleReceivable", content, StringComparison.Ordinal);
        Assert.Contains("overpayment_not_supported", content, StringComparison.Ordinal);
        Assert.DoesNotContain("ReceivableAmount", content, StringComparison.Ordinal);
        Assert.DoesNotContain("CreditLimit", content, StringComparison.Ordinal);
        Assert.DoesNotContain("SupervisorApproval", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Settlement_planning_does_not_cross_persistence_cash_inventory_or_public_confirm_boundaries()
    {
        var planner = Read("src/Application/Sales/SaleSettlementPlanning.cs");
        var controller = Read("src/WebApi/Controllers/V1/SalesController.cs");

        Assert.DoesNotContain("IUnitOfWork", planner, StringComparison.Ordinal);
        Assert.DoesNotContain("ITransactionManager", planner, StringComparison.Ordinal);
        Assert.DoesNotContain("IIdempotencyStore", planner, StringComparison.Ordinal);
        Assert.DoesNotContain("IOutboxWriter", planner, StringComparison.Ordinal);
        Assert.DoesNotContain("ISaleRepository", planner, StringComparison.Ordinal);
        Assert.DoesNotContain("IInventoryRepository", planner, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalNumberAllocator", planner, StringComparison.Ordinal);
        Assert.DoesNotContain("CashShift", planner, StringComparison.Ordinal);
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
