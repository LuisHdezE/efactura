using Xunit;

namespace ArchitectureTests;

public sealed class W22SaleCancellationArchitectureTests
{
    [Fact]
    public void Cancellation_http_surface_matches_locked_contract()
    {
        var controller = Read("src/WebApi/Controllers/V1/SaleCancellationController.cs");
        var contracts = Read("src/WebApi/Controllers/V1/Contracts/SaleCancellationContracts.cs");

        Assert.Contains("api/v1/sales/{saleId:guid}/cancel", controller, StringComparison.Ordinal);
        Assert.Contains("HttpPost(Name = \"cancelSale\")", controller, StringComparison.Ordinal);
        Assert.Contains("Permissions.SalesCancel", controller, StringComparison.Ordinal);
        Assert.Contains("V1RequestContract.RequireIdempotencyKey", controller, StringComparison.Ordinal);
        Assert.Contains("SaleCancelRequest", contracts, StringComparison.Ordinal);
        Assert.Contains("SaleCancellationDto", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpDelete", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpPatch", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Cancellation_transaction_is_narrow_and_does_not_reverse_confirmation_effects()
    {
        var source = Read("src/Application/Sales/SaleCancellation.cs");
        var registrations = Read("src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs");

        Assert.Contains("CancelSaleUseCase", source, StringComparison.Ordinal);
        Assert.Contains("ITransactionManager", source, StringComparison.Ordinal);
        Assert.Contains("IUnitOfWork", source, StringComparison.Ordinal);
        Assert.Contains("IIdempotencyStore", source, StringComparison.Ordinal);
        Assert.Contains("IAuditWriter", source, StringComparison.Ordinal);
        Assert.Contains("IOutboxWriter", source, StringComparison.Ordinal);
        Assert.Contains("SALE_CANCELLED", source, StringComparison.Ordinal);
        Assert.Contains("SaleCancelledIntegrationEvent", source, StringComparison.Ordinal);
        Assert.Contains("sales.cancellation.irreversible_boundary_crossed", source, StringComparison.Ordinal);
        Assert.Contains("sales.already_cancelled", source, StringComparison.Ordinal);
        Assert.Contains("services.AddScoped<CancelSaleUseCase>()", registrations, StringComparison.Ordinal);

        Assert.DoesNotContain("IPaymentRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IReceivableRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IInventoryRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SaleStockConsumer", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalizationRequestRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalDocumentRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalNumberAllocator", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Dgi", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Domain_exposes_cancelled_as_terminal_persistable_state()
    {
        var domain = Read("src/Domain/Sales/Sale.cs");
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfSaleRepository.cs");

        Assert.Contains("Cancelled = 4", domain, StringComparison.Ordinal);
        Assert.Contains("public void MarkCancelled", domain, StringComparison.Ordinal);
        Assert.Contains("sales.cancelled_terminal", domain, StringComparison.Ordinal);
        Assert.Contains("record.Status = (int)sale.Status", repository, StringComparison.Ordinal);
        Assert.Contains("(SaleStatus)record.Status", repository, StringComparison.Ordinal);
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src")) && Directory.Exists(Path.Combine(current.FullName, "test")))
                return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("Repository root was not found.");
    }
}
