using Xunit;

namespace ArchitectureTests;

public sealed class SaleLocalEffectsArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Fiscalization_request_domain_remains_framework_and_transport_agnostic()
    {
        var domain = Read("src/Domain/Fiscal/FiscalizationRequest.cs");

        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", domain, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", domain, StringComparison.Ordinal);
        Assert.DoesNotContain("WebApi", domain, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", domain, StringComparison.Ordinal);
    }

    [Fact]
    public void Sale_stock_consumer_stages_effects_without_owning_outer_transaction_or_reliability_writes()
    {
        var consumer = Read("src/Application/Inventory/SaleStockConsumption.cs");

        Assert.Contains("IInventoryRepository", consumer, StringComparison.Ordinal);
        Assert.DoesNotContain("ITransactionManager", consumer, StringComparison.Ordinal);
        Assert.DoesNotContain("IUnitOfWork", consumer, StringComparison.Ordinal);
        Assert.DoesNotContain("IIdempotencyStore", consumer, StringComparison.Ordinal);
        Assert.DoesNotContain("IAuditWriter", consumer, StringComparison.Ordinal);
        Assert.DoesNotContain("IOutboxWriter", consumer, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalNumberAllocator", consumer, StringComparison.Ordinal);
    }

    [Fact]
    public void Fiscalization_work_item_is_not_a_cae_or_fiscal_document_surrogate()
    {
        var domain = Read("src/Domain/Fiscal/FiscalizationRequest.cs");
        var record = Read("src/Infrastructure/Persistence/V1/Write/Models/FiscalizationRecords.cs");
        var combined = domain + record;

        Assert.Contains("ConfirmationFingerprint", combined, StringComparison.Ordinal);
        Assert.Contains("SettlementFingerprint", combined, StringComparison.Ordinal);
        Assert.Contains("CfeFamily", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("CaeAuthorization", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("FiscalNumber", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("Series", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("Xml", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Signature", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Artifact", combined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Local_effect_components_remain_staging_only_while_public_confirm_route_stays_closed()
    {
        var controller = Read("src/WebApi/Controllers/V1/SalesController.cs");
        var consumer = Read("src/Application/Inventory/SaleStockConsumption.cs");
        var fiscalization = Read("src/Domain/Fiscal/FiscalizationRequest.cs");
        var localEffects = consumer + Environment.NewLine + fiscalization;

        Assert.DoesNotContain("/confirm", controller, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HttpPost(\"{saleId:guid}/confirm\"", controller, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ISaleRepository", localEffects, StringComparison.Ordinal);
        Assert.DoesNotContain("MarkConfirmed", localEffects, StringComparison.Ordinal);
        Assert.DoesNotContain("SaleStatus.Confirmed", localEffects, StringComparison.Ordinal);
        Assert.DoesNotContain("ITransactionManager", localEffects, StringComparison.Ordinal);
        Assert.DoesNotContain("IIdempotencyStore", localEffects, StringComparison.Ordinal);
        Assert.DoesNotContain("IOutboxWriter", localEffects, StringComparison.Ordinal);
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
