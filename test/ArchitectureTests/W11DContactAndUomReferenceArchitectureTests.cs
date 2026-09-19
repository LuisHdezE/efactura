using System.Text;
using Xunit;

namespace ArchitectureTests;

public sealed class W11DContactAndUomReferenceArchitectureTests
{
    [Fact]
    public void Reference_data_controller_exposes_W1_1D_routes_with_exact_permissions()
    {
        var controller = Read("src/WebApi/Controllers/V1/ReferenceDataController.cs");

        var contacts = ActionSlice(controller, "[HttpGet(\"contact-types\", Name = \"listContactTypes\")]");
        var units = ActionSlice(controller, "[HttpGet(\"units-of-measure\", Name = \"listUnitsOfMeasure\")]");

        Assert.Contains("[RequirePermission(Permissions.PartiesRead)]", contacts, StringComparison.Ordinal);
        Assert.Contains("[RequirePermission(Permissions.CatalogRead)]", units, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status403Forbidden", contacts, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status403Forbidden", units, StringComparison.Ordinal);
    }

    [Fact]
    public void Contact_type_catalog_preserves_the_release1_legacy_compatibility_defaults_without_hardcoding_party_domain_enum()
    {
        var provider = Read("src/Infrastructure/ReferenceData/Release1ReferenceDataCatalog.cs");
        var party = Read("src/Domain/Parties/Party.cs");

        Assert.Contains("new(\"PHONE\", \"Phone\")", provider, StringComparison.Ordinal);
        Assert.Contains("new(\"MOBILE\", \"Mobile\")", provider, StringComparison.Ordinal);
        Assert.Contains("new(\"EMAIL\", \"Email\")", provider, StringComparison.Ordinal);
        Assert.Contains("new(\"FAX\", \"Fax\")", provider, StringComparison.Ordinal);
        Assert.Contains("string typeCode", party, StringComparison.Ordinal);
        Assert.DoesNotContain("enum PartyContactType", party, StringComparison.Ordinal);
    }

    [Fact]
    public void Unit_of_measure_reference_is_a_scoped_runtime_projection_not_a_fabricated_DGI_enumeration()
    {
        var application = Read("src/Application/ReferenceData/ReferenceDataApplication.cs");
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfCommercialItemRepository.cs");
        var evidence = Read("documentation/blueprint-api-implementation/30_FISCAL_UNIT_OF_MEASURE_EVIDENCE_PREREQUISITE.md");

        Assert.Contains("IUnitOfMeasureReferenceReader", application, StringComparison.Ordinal);
        Assert.Contains("actor.CompanyScopes", application, StringComparison.Ordinal);
        Assert.Contains("unit.Length <= 4", application, StringComparison.Ordinal);
        Assert.Contains("release-1/runtime-projection", application, StringComparison.Ordinal);

        Assert.Contains("x.Active && scopes.Contains(x.OrganizationId)", repository, StringComparison.Ordinal);
        Assert.Contains(".Select(x => x.Unit)", repository, StringComparison.Ordinal);
        Assert.Contains(".Distinct()", repository, StringComparison.Ordinal);
        Assert.Contains(".OrderBy(unit => unit)", repository, StringComparison.Ordinal);

        Assert.Contains("does not reinterpret the catalog as a DGI code table", evidence, StringComparison.Ordinal);
        Assert.Contains("maximum length of four characters", evidence, StringComparison.Ordinal);
        Assert.Contains("no automatic replacement with `N/A`", evidence, StringComparison.Ordinal);
    }

    [Fact]
    public void W1_1D_application_contract_stays_provider_neutral()
    {
        var application = Read("src/Application/ReferenceData/ReferenceDataApplication.cs");

        Assert.DoesNotContain("Npgsql", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Dapper", application, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", application, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplicationCore", application, StringComparison.Ordinal);
    }

    private static string ActionSlice(string source, string routeMarker)
    {
        var start = source.IndexOf(routeMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Missing route marker {routeMarker}.");
        var next = source.IndexOf("[HttpGet(", start + routeMarker.Length, StringComparison.Ordinal);
        return next < 0 ? source[start..] : source[start..next];
    }

    private static string Read(string path) => File.ReadAllText(Full(path), Encoding.UTF8);

    private static string Full(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return Path.GetFullPath(full);
    }
}
