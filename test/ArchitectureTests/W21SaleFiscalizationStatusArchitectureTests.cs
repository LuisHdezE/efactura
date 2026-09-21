using Xunit;

namespace ArchitectureTests;

public sealed class W21SaleFiscalizationStatusArchitectureTests
{
    [Fact]
    public void Fiscalization_status_http_surface_matches_locked_contract()
    {
        var controller = Read("src/WebApi/Controllers/V1/SaleFiscalizationController.cs");
        var contracts = Read("src/WebApi/Controllers/V1/Contracts/SaleFiscalizationContracts.cs");

        Assert.Contains("api/v1/sales/{saleId:guid}/fiscalization", controller, StringComparison.Ordinal);
        Assert.Contains("getSaleFiscalizationStatus", controller, StringComparison.Ordinal);
        Assert.Contains("Permissions.SalesRead", controller, StringComparison.Ordinal);
        Assert.Contains("LOCAL_WORKFLOW_ONLY_NOT_DGI_ACCEPTANCE", controller, StringComparison.Ordinal);
        Assert.Contains("SaleFiscalizationStatusDto", contracts, StringComparison.Ordinal);
        Assert.Contains("SaleFiscalDocumentIdentityDto", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpPost", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpPatch", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpDelete", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Fiscalization_status_application_path_is_read_only_and_fail_closed()
    {
        var source = Read("src/Application/Sales/SaleFiscalizationStatus.cs");
        var registrations = Read("src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs");

        Assert.Contains("ISaleRepository", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalizationRequestRepository", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalDocumentRepository", source, StringComparison.Ordinal);
        Assert.Contains("fiscalization.inconsistent_state", source, StringComparison.Ordinal);
        Assert.Contains("GetSaleFiscalizationStatusUseCase", registrations, StringComparison.Ordinal);
        Assert.DoesNotContain("ITransactionManager", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IUnitOfWork", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IIdempotencyStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IAuditWriter", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IOutboxWriter", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalCfeStateConsultationGateway", source, StringComparison.Ordinal);
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
