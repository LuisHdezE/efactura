using Xunit;

namespace ArchitectureTests;

public sealed class OrganizationFiscalIssuerProfileArchitectureTests
{
    [Fact]
    public void Organization_domain_is_framework_and_transport_free()
    {
        var source = Read("src/Domain/Organizations/Organization.cs");
        Assert.Contains("CompanyFiscalProfile", source, StringComparison.Ordinal); Assert.Contains("FiscalLocation", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", source, StringComparison.Ordinal); Assert.DoesNotContain("Microsoft.AspNetCore", source, StringComparison.Ordinal); Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal); Assert.DoesNotContain("XDocument", source, StringComparison.Ordinal); Assert.DoesNotContain("XmlDocument", source, StringComparison.Ordinal); Assert.DoesNotContain("certificate", source, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("signature", source, StringComparison.OrdinalIgnoreCase); Assert.DoesNotContain("transport", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Public_contract_implements_only_canonical_API_ORG_001_through_006()
    {
        var company = Read("src/WebApi/Controllers/V1/CompanyController.cs"); var locations = Read("src/WebApi/Controllers/V1/LocationsController.cs");
        Assert.Contains("[Route(\"api/v1/company\")]", company, StringComparison.Ordinal); Assert.Contains("[HttpGet]", company, StringComparison.Ordinal); Assert.Contains("[HttpPatch]", company, StringComparison.Ordinal); Assert.Contains("Permissions.OrganizationRead", company, StringComparison.Ordinal); Assert.Contains("Permissions.OrganizationManage", company, StringComparison.Ordinal);
        Assert.Contains("[Route(\"api/v1/locations\")]", locations, StringComparison.Ordinal); Assert.Contains("[HttpGet]", locations, StringComparison.Ordinal); Assert.Contains("[HttpGet(\"{locationId}\")]", locations, StringComparison.Ordinal); Assert.Contains("[HttpPost]", locations, StringComparison.Ordinal); Assert.Contains("[HttpPatch(\"{locationId}\")]", locations, StringComparison.Ordinal); Assert.Contains("Permissions.OrganizationRead", locations, StringComparison.Ordinal); Assert.Contains("Permissions.OrganizationManage", locations, StringComparison.Ordinal);
    }

    [Fact]
    public void Organization_repository_never_owns_transaction_or_flush()
    {
        var source = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfOrganizationRepository.cs");
        Assert.DoesNotContain("BeginTransaction", source, StringComparison.Ordinal); Assert.DoesNotContain("SaveChanges", source, StringComparison.Ordinal); Assert.DoesNotContain("ITransactionManager", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Persistence_has_portable_branch_uniqueness_guard()
    {
        var model = Read("src/Infrastructure/Persistence/V1/V1PersistenceModelCustomizer.cs"); var unitOfWork = Read("src/Infrastructure/Persistence/V1/Transactions/EfUnitOfWork.cs");
        Assert.Contains("UX_v1_location_org_branch", model, StringComparison.Ordinal); Assert.Contains(".IsUnique()", model, StringComparison.Ordinal); Assert.Contains("UX_v1_location_org_branch", unitOfWork, StringComparison.Ordinal); Assert.Contains("duplicate_branch_code", unitOfWork, StringComparison.Ordinal);
    }

    [Fact]
    public void Organization_application_keeps_signing_and_transport_out_of_scope()
    {
        var source = Read("src/Application/Organizations/OrganizationApplication.cs");
        Assert.DoesNotContain("IFiscalSigner", source, StringComparison.Ordinal); Assert.DoesNotContain("IFiscalTransportGateway", source, StringComparison.Ordinal); Assert.DoesNotContain("IFiscalXmlBuilder", source, StringComparison.Ordinal); Assert.DoesNotContain("CAE", source, StringComparison.Ordinal); Assert.DoesNotContain("private key", source, StringComparison.OrdinalIgnoreCase);
    }

    private static string Read(string relativePath) => File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));
    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src")) && Directory.Exists(Path.Combine(current.FullName, "test"))) return current.FullName;
            current = current.Parent;
        }
        throw new InvalidOperationException("Repository root was not found.");
    }
}
