using Xunit;

namespace ArchitectureTests;

public sealed class PartiesCatalogApiContractTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Parties_controller_exposes_only_the_approved_PTY_001_to_007_route_family()
    {
        var content = Read("src/WebApi/Controllers/V1/PartiesController.cs");

        Assert.Contains("[Route(\"api/v1/parties\")]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"{partyId:guid}\")]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpPost]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpPatch(\"{partyId:guid}\")]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpPost(\"{partyId:guid}/fiscal-identities\")]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpPut(\"{partyId:guid}/fiscal-identities/{identityId:guid}\")]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpPut(\"{partyId:guid}/roles\")]", content, StringComparison.Ordinal);
        Assert.DoesNotContain("account-summary", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Catalog_controller_exposes_the_approved_CAT_001_to_009_surface_without_inventing_tax_mutations()
    {
        var items = Read("src/WebApi/Controllers/V1/ItemsController.cs");
        var categories = Read("src/WebApi/Controllers/V1/ItemCategoriesController.cs");
        var taxProfiles = Read("src/WebApi/Controllers/V1/TaxProfilesController.cs");

        Assert.Contains("[Route(\"api/v1/items\")]", items, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"{itemId:guid}\")]", items, StringComparison.Ordinal);
        Assert.Contains("[HttpPatch(\"{itemId:guid}\")]", items, StringComparison.Ordinal);
        Assert.Contains("[HttpPost(\"{itemId:guid}/deactivate\")]", items, StringComparison.Ordinal);

        Assert.Contains("[Route(\"api/v1/item-categories\")]", categories, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", categories, StringComparison.Ordinal);
        Assert.Contains("[HttpPost]", categories, StringComparison.Ordinal);
        Assert.Contains("[HttpPatch(\"{categoryId:guid}\")]", categories, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpGet(\"{categoryId:guid}\")]", categories, StringComparison.Ordinal);

        Assert.Contains("[Route(\"api/v1/tax-profiles\")]", taxProfiles, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", taxProfiles, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPost", taxProfiles, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPatch", taxProfiles, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPut", taxProfiles, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpDelete", taxProfiles, StringComparison.Ordinal);
    }

    [Fact]
    public void Mutable_Parties_and_Catalog_controllers_require_idempotency_through_the_v1_request_contract()
    {
        var parties = Read("src/WebApi/Controllers/V1/PartiesController.cs");
        var items = Read("src/WebApi/Controllers/V1/ItemsController.cs");
        var categories = Read("src/WebApi/Controllers/V1/ItemCategoriesController.cs");

        Assert.Contains("V1RequestContract.RequireIdempotencyKey(Request)", parties, StringComparison.Ordinal);
        Assert.Contains("V1RequestContract.RequireIdempotencyKey(Request)", items, StringComparison.Ordinal);
        Assert.Contains("V1RequestContract.RequireIdempotencyKey(Request)", categories, StringComparison.Ordinal);

        Assert.Contains("V1RequestContract.ComputeRequestHash(request)", parties, StringComparison.Ordinal);
        Assert.Contains("V1RequestContract.ComputeRequestHash(request)", items, StringComparison.Ordinal);
        Assert.Contains("V1RequestContract.ComputeRequestHash(request)", categories, StringComparison.Ordinal);
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "api-accounting.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from the architecture test output directory.");
    }
}
