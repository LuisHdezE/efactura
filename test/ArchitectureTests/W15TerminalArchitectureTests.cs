using Xunit;

namespace ArchitectureTests;

public sealed class W15TerminalArchitectureTests
{
    [Fact]
    public void Terminal_http_surface_matches_governed_contract()
    {
        var source = Read("src/WebApi/Controllers/V1/TerminalsController.cs");
        Assert.Contains("api/v1/terminals", source, StringComparison.Ordinal);
        Assert.Contains("listTerminals", source, StringComparison.Ordinal);
        Assert.Contains("registerTerminal", source, StringComparison.Ordinal);
        Assert.Contains("getTerminal", source, StringComparison.Ordinal);
        Assert.Contains("updateTerminal", source, StringComparison.Ordinal);
        Assert.Contains("Permissions.OrganizationRead", source, StringComparison.Ordinal);
        Assert.Contains("Permissions.OrganizationManage", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpDelete", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Terminal_persistence_and_location_dependency_are_explicit()
    {
        var model = Read("src/Infrastructure/Persistence/V1/Write/Models/V1TerminalRecordConfiguration.cs");
        var app = Read("src/Application/Organizations/OrganizationApplication.cs");
        Assert.Contains("UX_v1_terminal_org_code", model, StringComparison.Ordinal);
        Assert.Contains("DeleteBehavior.Restrict", model, StringComparison.Ordinal);
        Assert.Contains("organization.location.active_terminals_exist", app, StringComparison.Ordinal);
        Assert.Contains("active_terminal_dependency", app, StringComparison.Ordinal);
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
