using System.Xml.Linq;
using Xunit;

namespace ArchitectureTests;

public sealed class ProjectDependencyTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    private static readonly string[] ForbiddenInwardPackages =
    {
        "Microsoft.EntityFrameworkCore",
        "Dapper",
        "Npgsql",
        "MySql.Data",
        "MySql.EntityFrameworkCore",
        "Oracle.EntityFrameworkCore",
        "System.Data.SqlClient",
        "Serilog",
        "Microsoft.ApplicationInsights",
        "StackExchange.Redis",
        "Microsoft.AspNetCore",
        "Azure."
    };

    private static readonly string[] ForbiddenInwardSourceTokens =
    {
        "Microsoft.EntityFrameworkCore",
        "Dapper",
        "Npgsql",
        "MySql",
        "Oracle.EntityFrameworkCore",
        "System.Data.SqlClient",
        "Serilog",
        "Microsoft.ApplicationInsights",
        "StackExchange.Redis",
        "Microsoft.AspNetCore",
        "Azure."
    };

    [Fact]
    public void Domain_has_no_package_or_project_dependencies()
    {
        var project = LoadProject("src/Domain/Domain.csproj");

        Assert.Empty(GetIncludes(project, "PackageReference"));
        Assert.Empty(GetIncludes(project, "ProjectReference"));
    }

    [Fact]
    public void Application_depends_only_on_Domain_at_project_level()
    {
        var project = LoadProject("src/Application/Application.csproj");
        var references = GetIncludes(project, "ProjectReference")
            .Select(NormalizePath)
            .ToArray();

        Assert.Equal(new[] { "../Domain/Domain.csproj" }, references);
        AssertNoForbiddenPackages(project, "Application");
    }

    [Fact]
    public void Infrastructure_points_inward_and_never_references_WebApi()
    {
        var project = LoadProject("src/Infrastructure/Infrastructure.csproj");
        var references = GetIncludes(project, "ProjectReference")
            .Select(NormalizePath)
            .ToArray();

        Assert.Contains("../Domain/Domain.csproj", references);
        Assert.Contains("../Application/Application.csproj", references);
        Assert.DoesNotContain(references, value => value.Contains("WebApi", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void WebApi_references_Application_and_Infrastructure_as_outer_composition_layer()
    {
        var project = LoadProject("src/WebApi/WebApi.csproj");
        var references = GetIncludes(project, "ProjectReference")
            .Select(NormalizePath)
            .ToArray();

        Assert.Contains("../Application/Application.csproj", references);
        Assert.Contains("../Infrastructure/Infrastructure.csproj", references);
    }

    [Fact]
    public void New_inward_source_does_not_import_outer_frameworks_or_providers()
    {
        AssertSourceHasNoForbiddenTokens("src/Domain");
        AssertSourceHasNoForbiddenTokens("src/Application");
    }

    private static void AssertNoForbiddenPackages(XDocument project, string layer)
    {
        var packages = GetIncludes(project, "PackageReference").ToArray();
        var forbidden = packages
            .Where(package => ForbiddenInwardPackages.Any(prefix => package.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.True(forbidden.Length == 0, $"{layer} contains forbidden outer/provider packages: {string.Join(", ", forbidden)}");
    }

    private static void AssertSourceHasNoForbiddenTokens(string relativeDirectory)
    {
        var directory = Path.Combine(RepositoryRoot, relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);
            foreach (var token in ForbiddenInwardSourceTokens)
            {
                if (content.Contains(token, StringComparison.Ordinal))
                {
                    violations.Add($"{Path.GetRelativePath(RepositoryRoot, file)} -> {token}");
                }
            }
        }

        Assert.True(violations.Count == 0, $"Forbidden outer dependencies found in inward source:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    private static XDocument LoadProject(string relativePath)
    {
        var path = Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return XDocument.Load(path);
    }

    private static IEnumerable<string> GetIncludes(XDocument project, string elementName) =>
        project.Descendants(elementName)
            .Select(element => (string?)element.Attribute("Include"))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!);

    private static string NormalizePath(string path) => path.Replace('\\', '/');

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

        throw new DirectoryNotFoundException("Could not locate repository root containing api-accounting.sln.");
    }
}
