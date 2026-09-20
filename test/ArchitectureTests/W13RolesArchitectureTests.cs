using System.Text;
using Xunit;

namespace ArchitectureTests;

public sealed class W13RolesArchitectureTests
{
    [Fact]
    public void Roles_controller_exposes_exact_W1_3_contract()
    {
        var controller = Read("src/WebApi/Controllers/V1/RolesController.cs");

        Assert.Contains("[Route(\"api/v1/roles\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[Authorize]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(Name = \"listRoles\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"{roleId}\", Name = \"getRole\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpPost(Name = \"createRole\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpPut(\"{roleId}\", Name = \"updateRole\")]", controller, StringComparison.Ordinal);

        Assert.Equal(2, Count(controller, "[RequirePermission(Permissions.SecurityRolesRead)]"));
        Assert.Equal(2, Count(controller, "[RequirePermission(Permissions.SecurityManageRoles)]"));
        Assert.Equal(2, Count(controller, "V1RequestContract.RequireIdempotencyKey(Request)"));
        Assert.Equal(2, Count(controller, "V1RequestContract.ComputeRequestHash(request)"));
    }

    [Fact]
    public void Roles_use_canonical_permission_catalog_and_company_scope()
    {
        var application = Read("src/Application/IdentityAccess/RoleAccessApplication.cs");

        Assert.Contains("new(Permissions.All, StringComparer.Ordinal)", application, StringComparison.Ordinal);
        Assert.Contains("Permissions.SecurityRolesRead", application, StringComparison.Ordinal);
        Assert.Contains("Permissions.SecurityManageRoles", application, StringComparison.Ordinal);
        Assert.Contains("actor.CompanyScopes.Contains(organizationId)", application, StringComparison.Ordinal);
        Assert.Contains("identity.role.permission_unknown", application, StringComparison.Ordinal);
        Assert.Contains("Distinct(StringComparer.Ordinal)", application, StringComparison.Ordinal);
        Assert.Contains("OrderBy(permission => permission, StringComparer.Ordinal)", application, StringComparison.Ordinal);
    }

    [Fact]
    public void Security_roles_are_not_party_roles_and_persistence_is_additive()
    {
        var domain = Read("src/Domain/IdentityAccess/SecurityRole.cs");
        var records = Read("src/Infrastructure/Persistence/V1/Write/Models/SecurityRoleRecords.cs");
        var customizer = Read("src/Infrastructure/Persistence/V1/V1PersistenceSecurityRoleModelCustomizer.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260920040000_V1SecurityRoles.cs");

        Assert.Contains("class SecurityRole", domain, StringComparison.Ordinal);
        Assert.DoesNotContain("PartyRole", domain, StringComparison.Ordinal);
        Assert.DoesNotContain("V1PartyRoleRecord", records, StringComparison.Ordinal);
        Assert.Contains("v1_security_roles", customizer, StringComparison.Ordinal);
        Assert.Contains("v1_security_role_permissions", customizer, StringComparison.Ordinal);
        Assert.DoesNotContain("v1_party_roles", customizer, StringComparison.Ordinal);
        Assert.Contains("migrationBuilder.CreateTable", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("DropTable(name: \"v1_party_roles\")", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void Security_role_model_customizer_preserves_the_accepted_chain()
    {
        var customizer = Read("src/Infrastructure/Persistence/V1/V1PersistenceSecurityRoleModelCustomizer.cs");
        var configurator = Read("src/Infrastructure/Persistence/V1/V1PersistenceDatabaseConfigurator.cs");

        Assert.Contains("V1PersistenceCfeDocumentResponseCertificateTrustModelCustomizer", customizer, StringComparison.Ordinal);
        Assert.Contains("_baseline.Customize(modelBuilder, context)", customizer, StringComparison.Ordinal);
        Assert.Contains("ReplaceService<IModelCustomizer, V1PersistenceSecurityRoleModelCustomizer>()", configurator, StringComparison.Ordinal);
        Assert.Contains("UX_v1_security_role_org_name", customizer, StringComparison.Ordinal);
        Assert.Contains("IX_v1_security_role_org_active", customizer, StringComparison.Ordinal);
    }

    [Fact]
    public void Role_mutations_are_idempotent_audited_outboxed_and_concurrency_guarded()
    {
        var application = Read("src/Application/IdentityAccess/RoleAccessApplication.cs");

        Assert.Contains("IIdempotencyStore", application, StringComparison.Ordinal);
        Assert.Contains("ITransactionManager", application, StringComparison.Ordinal);
        Assert.Contains("IUnitOfWork", application, StringComparison.Ordinal);
        Assert.Contains("IAuditWriter", application, StringComparison.Ordinal);
        Assert.Contains("IOutboxWriter", application, StringComparison.Ordinal);
        Assert.Contains("SECURITY_ROLE_CREATED", application, StringComparison.Ordinal);
        Assert.Contains("SECURITY_ROLE_UPDATED", application, StringComparison.Ordinal);
        Assert.Contains("concurrency_conflict", application, StringComparison.Ordinal);
        Assert.Contains("identity.role.name_duplicate", application, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_registers_the_W1_3_role_slice()
    {
        var program = Read("src/WebApi/Program.cs");

        Assert.Contains("AddScoped<ISecurityRoleRepository, EfSecurityRoleRepository>()", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<ListRolesUseCase>()", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<GetRoleUseCase>()", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<CreateRoleUseCase>()", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<UpdateRoleUseCase>()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void W1_3_readiness_locks_expected_completion_accounting()
    {
        var readiness = Read("documentation/api-completion-matrix/W1_3_ROLES_READINESS.md");

        Assert.Contains(
            "Status: `IMPLEMENTED / CI_GREEN / TEMP_NEON_SCHEMA_VALIDATED / PRODUCTION_SCHEMA_PENDING_APPROVAL`",
            readiness,
            StringComparison.Ordinal);
        Assert.Contains("Wave 1: `21 / 30`", readiness, StringComparison.Ordinal);
        Assert.Contains("global public v1: `55 / 194`", readiness, StringComparison.Ordinal);
        Assert.Contains("remaining `MISSING_HTTP`: `137`", readiness, StringComparison.Ordinal);
        Assert.Contains("remaining contract-collision IDs: `2`", readiness, StringComparison.Ordinal);
        Assert.Contains("remaining non-implemented IDs: `139`", readiness, StringComparison.Ordinal);
        Assert.Contains("production branch remains unchanged", readiness, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("W1.4 Users + role assignment", readiness, StringComparison.Ordinal);
    }

    private static int Count(string source, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }

        return count;
    }

    private static string Read(string path) => File.ReadAllText(Full(path), Encoding.UTF8);

    private static string Full(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return Path.GetFullPath(full);
    }
}
