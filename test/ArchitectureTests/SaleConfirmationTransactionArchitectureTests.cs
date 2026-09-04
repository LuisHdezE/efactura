using Xunit;

namespace ArchitectureTests;

public sealed class SaleConfirmationTransactionArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Confirmation_use_case_owns_one_local_transaction_and_all_required_atomic_effects()
    {
        var source = Read("src/Application/Sales/SaleConfirmationTransaction.cs");

        Assert.Contains("ITransactionManager", source, StringComparison.Ordinal);
        Assert.Contains("IUnitOfWork", source, StringComparison.Ordinal);
        Assert.Contains("IIdempotencyStore", source, StringComparison.Ordinal);
        Assert.Contains("IAuditWriter", source, StringComparison.Ordinal);
        Assert.Contains("IOutboxWriter", source, StringComparison.Ordinal);
        Assert.Contains("IPaymentRepository", source, StringComparison.Ordinal);
        Assert.Contains("IReceivableRepository", source, StringComparison.Ordinal);
        Assert.Contains("SaleStockConsumer", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalizationRequestRepository", source, StringComparison.Ordinal);
        Assert.Contains("SaleConfirmedIntegrationEvent", source, StringComparison.Ordinal);
        Assert.Contains("FiscalizationRequestedIntegrationEvent", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Confirmation_evidence_is_server_recomputed_and_not_rebuilt_from_public_preview_dto()
    {
        var source = Read("src/Application/Sales/SaleConfirmationTransaction.cs");

        Assert.Contains("ISaleConfirmationEvidenceResolver", source, StringComparison.Ordinal);
        Assert.Contains("ResolveTaxTreatmentUseCase", source, StringComparison.Ordinal);
        Assert.Contains("ResolveTaxRateUseCase", source, StringComparison.Ordinal);
        Assert.Contains("PrepareCfeEligibilityUseCase", source, StringComparison.Ordinal);
        Assert.Contains("SelectCfeUseCase", source, StringComparison.Ordinal);
        Assert.Contains("IInventoryAvailabilityChecker", source, StringComparison.Ordinal);
        Assert.Contains("SaleConfirmationPlanner", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SaleFiscalPreviewLineView", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Confirmation_transaction_stops_before_cae_document_xml_signing_and_transport()
    {
        var source = Read("src/Application/Sales/SaleConfirmationTransaction.cs");

        Assert.DoesNotContain("IFiscalNumberAllocator", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ICaeRepository", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CaeAuthorization", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FiscalNumberReservation", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalXmlBuilder", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalValidator", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalSigner", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalTransportGateway", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalArtifactStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SignXml", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SendToDgi", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Public_confirm_route_delegates_to_the_accepted_transaction_boundary()
    {
        var controller = Read("src/WebApi/Controllers/V1/SalesController.cs");
        var sale = Read("src/Domain/Sales/Sale.cs");

        Assert.Contains("Confirmed = 3", sale, StringComparison.Ordinal);
        Assert.Contains("sales.confirmed_immutable", sale, StringComparison.Ordinal);
        Assert.Contains("[HttpPost(\"{saleId:guid}/confirm\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[RequirePermission(Permissions.SalesConfirm)]", controller, StringComparison.Ordinal);
        Assert.Contains("ConfirmSaleUseCase", controller, StringComparison.Ordinal);
        Assert.Contains("V1RequestContract.RequireIdempotencyKey(Request)", controller, StringComparison.Ordinal);
        Assert.Contains("V1RequestContract.ComputeRequestHash(request)", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalNumberAllocator", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("CaeAuthorization", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("FiscalDocument", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", controller, StringComparison.Ordinal);
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

        throw new DirectoryNotFoundException("Could not locate repository root from architecture test output directory.");
    }
}
