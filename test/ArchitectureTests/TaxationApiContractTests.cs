using System.Text.RegularExpressions;
using Xunit;

namespace ArchitectureTests;

public sealed class TaxationApiContractTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Tax_profile_controller_exposes_only_the_approved_read_contract()
    {
        var path = Path.Combine(
            RepositoryRoot,
            "src",
            "WebApi",
            "Controllers",
            "V1",
            "TaxProfilesController.cs");

        var source = File.ReadAllText(path);

        Assert.Contains("[Route(\"api/v1/tax-profiles\")]", source, StringComparison.Ordinal);
        Assert.Equal(1, Regex.Matches(source, @"\[HttpGet(?:\([^\]]*\))?\]").Count);
        Assert.DoesNotContain("[HttpPost", source, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPut", source, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpPatch", source, StringComparison.Ordinal);
        Assert.DoesNotContain("[HttpDelete", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(Permissions.CatalogRead)", source, StringComparison.Ordinal);
    }

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

        throw new InvalidOperationException("Repository root could not be located.");
    }
}
