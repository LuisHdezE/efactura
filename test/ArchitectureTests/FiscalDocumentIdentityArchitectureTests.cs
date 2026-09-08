using Xunit;

namespace ArchitectureTests;

public sealed class FiscalDocumentIdentityArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Fiscal_document_domain_is_framework_provider_and_transport_free()
    {
        var content = Read("src/Domain/Fiscal/FiscalDocument.cs");

        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.AspNetCore", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Npgsql", content, StringComparison.Ordinal);
        Assert.DoesNotContain("MySql", content, StringComparison.Ordinal);
        Assert.DoesNotContain("XmlDocument", content, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalSigner", content, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalTransportGateway", content, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Identity_use_case_owns_one_local_transaction_and_reuses_existing_CAE_allocator()
    {
        var content = Read("src/Application/Fiscal/FiscalDocumentIdentity.cs");

        Assert.Contains("ITransactionManager", content, StringComparison.Ordinal);
        Assert.Contains("IUnitOfWork", content, StringComparison.Ordinal);
        Assert.Contains("IFiscalNumberAllocator", content, StringComparison.Ordinal);
        Assert.Contains("IFiscalizationRequestRepository", content, StringComparison.Ordinal);
        Assert.Contains("IFiscalDocumentRepository", content, StringComparison.Ordinal);
        Assert.Contains("ISaleRepository", content, StringComparison.Ordinal);
        Assert.Contains("OperationId(request.Id)", content, StringComparison.Ordinal);
        Assert.Contains("FISCAL_DOCUMENT_IDENTITY_CREATED", content, StringComparison.Ordinal);
        Assert.Contains("FiscalDocumentIdentityCreatedIntegrationEvent", content, StringComparison.Ordinal);
        Assert.DoesNotContain("IIdempotencyStore", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Identity_slice_stops_before_XML_signing_artifacts_and_transport()
    {
        var content = Read("src/Application/Fiscal/FiscalDocumentIdentity.cs")
                      + Read("src/Domain/Fiscal/FiscalDocument.cs");

        Assert.DoesNotContain("IFiscalXmlBuilder", content, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalValidator", content, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalSigner", content, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalArtifactStore", content, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalTransportGateway", content, StringComparison.Ordinal);
        Assert.DoesNotContain("DgiTransport", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ProviderSdk", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Fiscal_document_repository_does_not_own_transaction_or_SaveChanges()
    {
        var content = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalDocumentRepository.cs");

        Assert.DoesNotContain("SaveChanges", content, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTransaction", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Commit", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Rollback", content, StringComparison.Ordinal);
        Assert.DoesNotContain("using Dapper", content, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecuteSql", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Fiscal_document_identity_has_request_number_and_reservation_uniqueness_guards()
    {
        var model = Read("src/Infrastructure/Persistence/V1/V1PersistenceModelCustomizer.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260908153000_V1FiscalDocumentIdentity.cs");

        Assert.Contains("UX_v1_fd_org_req", model, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fd_identity", model, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fd_res", model, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fd_org_req", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fd_identity", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fd_res", migration, StringComparison.Ordinal);
        Assert.Equal(3, Count(migration, "unique: true"));
    }

    [Fact]
    public void Sale_confirm_API_still_stops_at_fiscalization_request_receipt()
    {
        var controller = Read("src/WebApi/Controllers/V1/SalesController.cs");
        var contracts = Read("src/WebApi/Controllers/V1/Contracts/SalesContracts.cs");

        Assert.Contains("FiscalizationRequestId", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("FiscalDocumentId", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalNumberAllocator", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("PrepareFiscalDocumentIdentityUseCase", controller, StringComparison.Ordinal);
    }

    private static int Count(string content, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = content.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }
        return count;
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
