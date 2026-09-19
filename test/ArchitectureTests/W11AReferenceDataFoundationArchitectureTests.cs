using System.Text;
using Xunit;

namespace ArchitectureTests;

public sealed class W11AReferenceDataFoundationArchitectureTests
{
    [Fact]
    public void Reference_data_controller_preserves_the_two_W1_1A_authenticated_routes()
    {
        var controller = Read("src/WebApi/Controllers/V1/ReferenceDataController.cs");

        Assert.Contains("[Authorize]", controller, StringComparison.Ordinal);
        Assert.Contains("[Route(\"api/v1/reference-data\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"uruguay-departments\", Name = \"listUruguayDepartments\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"fiscal-identity-types\", Name = \"listFiscalIdentityTypes\")]", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("RequirePermission", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplicationCore", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Reference_data_application_boundary_is_provider_neutral_and_defends_authentication()
    {
        var application = Read("src/Application/ReferenceData/ReferenceDataApplication.cs");

        Assert.Contains("public interface IReferenceDataCatalog", application, StringComparison.Ordinal);
        Assert.Contains("ListUruguayDepartmentsUseCase", application, StringComparison.Ordinal);
        Assert.Contains("ListFiscalIdentityTypesUseCase", application, StringComparison.Ordinal);
        Assert.Contains("actor.IsAuthenticated", application, StringComparison.Ordinal);
        Assert.Contains("ApplicationProblemKind.AuthenticationRequired", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Npgsql", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Dapper", application, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplicationCore", application, StringComparison.Ordinal);
    }

    [Fact]
    public void Release1_reference_provider_is_static_versioned_and_has_no_legacy_or_database_dependency()
    {
        var provider = Read("src/Infrastructure/ReferenceData/Release1ReferenceDataCatalog.cs");
        var program = Read("src/WebApi/Program.cs");

        Assert.Contains("Release1ReferenceDataCatalog", provider, StringComparison.Ordinal);
        Assert.Contains("\"DGI Formato_CFE\"", provider, StringComparison.Ordinal);
        Assert.Contains("\"25-2\"", provider, StringComparison.Ordinal);
        Assert.Contains("AddReferenceDataFoundation", provider, StringComparison.Ordinal);
        Assert.Contains("builder.Services.AddReferenceDataFoundation();", program, StringComparison.Ordinal);

        Assert.DoesNotContain("Npgsql", provider, StringComparison.Ordinal);
        Assert.DoesNotContain("Dapper", provider, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplicationCore", provider, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", provider, StringComparison.Ordinal);
    }

    private static string Read(string path) => File.ReadAllText(Full(path), Encoding.UTF8);

    private static string Full(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return Path.GetFullPath(full);
    }
}
