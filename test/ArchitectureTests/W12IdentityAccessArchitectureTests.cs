using System.Text;
using Xunit;

namespace ArchitectureTests;

public sealed class W12IdentityAccessArchitectureTests
{
    [Fact]
    public void Identity_access_controller_exposes_exact_W1_2_routes_and_permissions()
    {
        var controller = Read("src/WebApi/Controllers/V1/IdentityAccessController.cs");

        Assert.Contains("[Route(\"api/v1\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[Authorize]", controller, StringComparison.Ordinal);

        var me = ActionSlice(controller, "[HttpGet(\"me\", Name = \"getCurrentActor\")]");
        var permissions = ActionSlice(controller, "[HttpGet(\"permissions\", Name = \"listPermissions\")]");

        Assert.DoesNotContain("RequirePermission", me, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status401Unauthorized", me, StringComparison.Ordinal);

        Assert.Contains("[RequirePermission(Permissions.SecurityRolesRead)]", permissions, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status401Unauthorized", permissions, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status403Forbidden", permissions, StringComparison.Ordinal);
    }

    [Fact]
    public void Current_actor_projection_reuses_existing_actor_context_and_minimizes_output()
    {
        var application = Read("src/Application/IdentityAccess/IdentityAccessApplication.cs");
        var contracts = Read("src/WebApi/Controllers/V1/Contracts/IdentityAccessContracts.cs");

        Assert.Contains("IActorContextAccessor", application, StringComparison.Ordinal);
        Assert.Contains("actor.ActorId", application, StringComparison.Ordinal);
        Assert.Contains("actor.DisplayName", application, StringComparison.Ordinal);
        Assert.Contains("actor.Permissions", application, StringComparison.Ordinal);
        Assert.Contains("actor.CompanyScopes", application, StringComparison.Ordinal);
        Assert.Contains("actor.LocationScopes", application, StringComparison.Ordinal);
        Assert.Contains("actor.TerminalScopes", application, StringComparison.Ordinal);
        Assert.DoesNotContain("actor.DeviceId", application, StringComparison.Ordinal);
        Assert.DoesNotContain("DeviceId", contracts, StringComparison.Ordinal);
    }

    [Fact]
    public void Permission_catalog_projects_the_single_canonical_permission_source_deterministically()
    {
        var application = Read("src/Application/IdentityAccess/IdentityAccessApplication.cs");
        var permissions = Read("src/Application/Common/Security/Permissions.cs");

        Assert.Contains("Permissions.SecurityRolesRead", application, StringComparison.Ordinal);
        Assert.Contains("Permissions.All", application, StringComparison.Ordinal);
        Assert.Contains("OrderBy(permission => permission, StringComparer.Ordinal)", application, StringComparison.Ordinal);
        Assert.Contains("public static IReadOnlySet<string> All", permissions, StringComparison.Ordinal);
        Assert.Contains("SecurityRolesRead = \"security.roles.read\"", permissions, StringComparison.Ordinal);
    }

    [Fact]
    public void W1_2_stays_application_and_web_only_without_persistence_or_legacy_dependencies()
    {
        var application = Read("src/Application/IdentityAccess/IdentityAccessApplication.cs");
        var controller = Read("src/WebApi/Controllers/V1/IdentityAccessController.cs");

        Assert.DoesNotContain("DbContext", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Npgsql", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Dapper", application, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplicationCore", application, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplicationCore", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_registers_both_W1_2_use_cases()
    {
        var program = Read("src/WebApi/Program.cs");

        Assert.Contains("builder.Services.AddScoped<GetCurrentActorUseCase>();", program, StringComparison.Ordinal);
        Assert.Contains("builder.Services.AddScoped<ListPermissionsUseCase>();", program, StringComparison.Ordinal);
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
