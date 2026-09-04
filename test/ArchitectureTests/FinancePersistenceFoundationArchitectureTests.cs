using Xunit;

namespace ArchitectureTests;

public sealed class FinancePersistenceFoundationArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Finance_domain_remains_persistence_and_transport_agnostic()
    {
        var payments = Read("src/Domain/Payments/Payments.cs");
        var receivables = Read("src/Domain/Receivables/Receivables.cs");
        var combined = payments + receivables;

        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("WebApi", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", combined, StringComparison.Ordinal);
    }

    [Fact]
    public void Application_finance_ports_depend_on_domain_not_ef_records()
    {
        var payments = Read("src/Application/Payments/PaymentPersistenceContracts.cs");
        var receivables = Read("src/Application/Receivables/ReceivablePersistenceContracts.cs");
        var combined = payments + receivables;

        Assert.Contains("IPaymentMethodRepository", payments, StringComparison.Ordinal);
        Assert.Contains("IPaymentRepository", payments, StringComparison.Ordinal);
        Assert.Contains("IReceivableRepository", receivables, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("V1Payment", combined, StringComparison.Ordinal);
    }

    [Fact]
    public void Finance_persistence_keeps_sale_plan_evidence_and_server_owned_receivable_amount()
    {
        var records = Read("src/Infrastructure/Persistence/V1/Write/Models/FinanceRecords.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260904002000_V1SaleSettlementFinance.cs");

        Assert.Contains("PaymentMethodVersion", records, StringComparison.Ordinal);
        Assert.Contains("ConfirmationFingerprint", records, StringComparison.Ordinal);
        Assert.Contains("SettlementFingerprint", records, StringComparison.Ordinal);
        Assert.Contains("OriginalAmount", records, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenBalance", records, StringComparison.Ordinal);
        Assert.Contains("UX_v1_pay_sale_plan_seq", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_ar_org_sale", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void Finance_persistence_stays_out_of_transport_cash_and_fiscal_workflow_boundaries()
    {
        var paymentRepository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfPaymentRepository.cs");
        var receivableRepository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfReceivableRepository.cs");
        var combined = paymentRepository + receivableRepository;

        Assert.DoesNotContain("SalesController", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("CashShift", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalNumberAllocator", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("Cae", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FiscalDocument", combined, StringComparison.Ordinal);
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
