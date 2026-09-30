using Xunit;

namespace ArchitectureTests;

public sealed class FiscalCfeStateEvidenceApiArchitectureTests
{
    [Fact]
    public void API_FIS_011_is_explicit_idempotent_and_requires_regularization_permission()
    {
        var inventory = Read("documentation/blueprint-api-contract/02A_ENDPOINT_INVENTORY_CORE.md");
        var controller = Read("src/WebApi/Controllers/V1/FiscalCfeStateEvidenceController.cs");

        Assert.Contains("API-FIS-011", inventory, StringComparison.Ordinal);
        Assert.Contains("collectFiscalDocumentDgiStateEvidence", inventory, StringComparison.Ordinal);
        Assert.Contains("POST `/api/v1/fiscal-documents/{fiscalDocumentId}/dgi-state-evidence`", inventory, StringComparison.Ordinal);
        Assert.Contains("`fiscal.regularization.manage` | REQUIRED", inventory, StringComparison.Ordinal);

        Assert.Contains("[HttpPost(\"{fiscalDocumentId:guid}/dgi-state-evidence\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[RequirePermission(Permissions.FiscalRegularizationManage)]", controller, StringComparison.Ordinal);
        Assert.Contains("V1RequestContract.RequireIdempotencyKey(Request)", controller, StringComparison.Ordinal);
        Assert.Contains("V1RequestContract.ComputeRequestHash", controller, StringComparison.Ordinal);
        Assert.Contains("ConsultFiscalCfeStateUseCase", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalCfeStateConsultationGateway", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Public_contract_keeps_EstadoCFE_external_and_excludes_token_xml_and_DGI_correlation_ids()
    {
        var contracts = Read("src/WebApi/Controllers/V1/Contracts/FiscalCfeStateEvidenceContracts.cs");
        var controller = Read("src/WebApi/Controllers/V1/FiscalCfeStateEvidenceController.cs");

        Assert.Contains("ExternalStateCode", contracts, StringComparison.Ordinal);
        Assert.Contains("StateMeaningResolved", contracts, StringComparison.Ordinal);
        Assert.Contains("LocalLifecycleMutationAuthorized", contracts, StringComparison.Ordinal);
        Assert.Contains("StateMeaningResolved: false", controller, StringComparison.Ordinal);
        Assert.Contains("LocalLifecycleMutationAuthorized: false", controller, StringComparison.Ordinal);

        Assert.DoesNotContain("ConsultationToken", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("ConsultationAvailableAtText", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("ResponseXml", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("DgiSenderId", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("DgiReceiverId", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("IdEmisor", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("IdReceptor", contracts, StringComparison.Ordinal);
    }

    [Fact]
    public void Existing_application_consultation_remains_the_only_active_DGI_boundary_and_is_registered_in_DI()
    {
        var application = Read("src/Application/Fiscal/FiscalCfeStateConsultation.cs");
        var registrations = Read("src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs");
        var controller = Read("src/WebApi/Controllers/V1/FiscalCfeStateEvidenceController.cs");

        Assert.Contains("ConsultFiscalCfeStateUseCase", application, StringComparison.Ordinal);
        Assert.Contains("stores the returned evidence append-only", application, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("deliberately not translated into a local lifecycle semantic", application, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("services.AddScoped<ConsultFiscalCfeStateUseCase>();", registrations, StringComparison.Ordinal);
        Assert.Contains("ConsultFiscalCfeStateUseCase", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalDocumentRepository", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalCfeStateConsultationRepository", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Documentation_preserves_raw_EstadoCFE_non_semantics_and_no_local_mutation()
    {
        var docs = Read("documentation/blueprint-api-implementation/71_FISCAL_CFE_STATE_EVIDENCE_API.md");

        Assert.Contains("Status: GOVERNED IMPLEMENTATION CANDIDATE", docs, StringComparison.Ordinal);
        Assert.Contains("externalStateCode", docs, StringComparison.Ordinal);
        Assert.Contains("StateMeaningResolved = false", docs, StringComparison.Ordinal);
        Assert.Contains("LocalLifecycleMutationAuthorized = false", docs, StringComparison.Ordinal);
        Assert.Contains("does not trigger a second consultation from the returned Token", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("must not be reused as the taxonomy for this separate `EstadoCFE` field", docs, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", docs, StringComparison.Ordinal);
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "api-accounting.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Repository root containing api-accounting.sln was not found.");
    }
}
