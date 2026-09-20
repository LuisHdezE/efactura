using System.Text;
using Xunit;

namespace ArchitectureTests;

public sealed class W14UsersArchitectureTests
{
    [Fact]
    public void Users_controller_exposes_exact_W1_4_contract()
    {
        var controller = Read("src/WebApi/Controllers/V1/UsersController.cs");

        Assert.Contains("[Route(\"api/v1/users\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[Authorize]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(Name = \"listUsers\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"{userId}\", Name = \"getUser\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpPost(Name = \"createUser\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpPatch(\"{userId}\", Name = \"updateUser\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpPut(\"{userId}/roles\", Name = \"assignUserRoles\")]", controller, StringComparison.Ordinal);

        Assert.Equal(2, Count(controller, "[RequirePermission(Permissions.SecurityUsersRead)]"));
        Assert.Equal(2, Count(controller, "[RequirePermission(Permissions.SecurityUsersManage)]"));
        Assert.Equal(1, Count(controller, "[RequirePermission(Permissions.SecurityManageRoles)]"));
        Assert.Equal(3, Count(controller, "V1RequestContract.RequireIdempotencyKey(Request)"));
        Assert.Equal(3, Count(controller, "V1RequestContract.ComputeRequestHash(request)"));
    }

    [Fact]
    public void User_application_preserves_company_scope_permissions_and_self_escalation_guards()
    {
        var application = Read("src/Application/IdentityAccess/UserAccessApplication.cs");

        Assert.Contains("Permissions.SecurityUsersRead", application, StringComparison.Ordinal);
        Assert.Contains("Permissions.SecurityUsersManage", application, StringComparison.Ordinal);
        Assert.Contains("Permissions.SecurityManageRoles", application, StringComparison.Ordinal);
        Assert.Contains("actor.CompanyScopes.Contains(organizationId)", application, StringComparison.Ordinal);
        Assert.Contains("identity.user.self_escalation_forbidden", application, StringComparison.Ordinal);
        Assert.Contains("identity.user.scope_invalid", application, StringComparison.Ordinal);
        Assert.Contains("identity.user.role_invalid", application, StringComparison.Ordinal);
        Assert.Contains("identity.user.identity_duplicate", application, StringComparison.Ordinal);
        Assert.Contains("concurrency_conflict", application, StringComparison.Ordinal);
        Assert.Contains("idempotency_key_reused", application, StringComparison.Ordinal);
    }

    [Fact]
    public void Security_user_is_provider_neutral_and_contains_no_credentials_or_tokens()
    {
        var domain = Read("src/Domain/IdentityAccess/SecurityUser.cs");
        var records = Read("src/Infrastructure/Persistence/V1/Write/Models/SecurityUserRecords.cs");
        var combined = domain + records;

        Assert.Contains("class SecurityUser", domain, StringComparison.Ordinal);
        Assert.Contains("IdentityProvider", domain, StringComparison.Ordinal);
        Assert.Contains("ExternalSubject", domain, StringComparison.Ordinal);
        Assert.DoesNotContain("PasswordHash", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RefreshToken", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SigningSecret", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ClientSecret", combined, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BearerToken", combined, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void W1_4_persistence_is_additive_and_references_existing_security_roles()
    {
        var customizer = Read("src/Infrastructure/Persistence/V1/V1PersistenceSecurityUserModelCustomizer.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260920183000_V1SecurityUsers.cs");

        Assert.Contains("V1PersistenceSecurityRoleModelCustomizer", customizer, StringComparison.Ordinal);
        Assert.Contains("_baseline.Customize(modelBuilder, context)", customizer, StringComparison.Ordinal);
        Assert.Contains("v1_security_users", customizer, StringComparison.Ordinal);
        Assert.Contains("v1_security_user_location_scopes", customizer, StringComparison.Ordinal);
        Assert.Contains("v1_security_user_terminal_scopes", customizer, StringComparison.Ordinal);
        Assert.Contains("v1_security_user_roles", customizer, StringComparison.Ordinal);
        Assert.Contains("UX_v1_security_user_org_identity", customizer, StringComparison.Ordinal);
        Assert.Contains("FK_v1_security_user_role_role", customizer, StringComparison.Ordinal);
        Assert.Contains("principalTable: \"v1_security_roles\"", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("DropTable(name: \"v1_security_roles\")", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("DropTable(name: \"v1_security_role_permissions\")", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void Security_user_model_customizer_is_terminal_without_breaking_W1_3_lineage()
    {
        var customizer = Read("src/Infrastructure/Persistence/V1/V1PersistenceSecurityUserModelCustomizer.cs");
        var configurator = Read("src/Infrastructure/Persistence/V1/V1PersistenceDatabaseConfigurator.cs");

        Assert.Contains("V1PersistenceSecurityRoleModelCustomizer", customizer, StringComparison.Ordinal);
        Assert.Contains("ReplaceService<IModelCustomizer, V1PersistenceSecurityUserModelCustomizer>()", configurator, StringComparison.Ordinal);
        Assert.Contains("V1PersistenceSecurityRoleModelCustomizer ->", configurator, StringComparison.Ordinal);
        Assert.Contains("V1PersistenceSecurityUserModelCustomizer", configurator, StringComparison.Ordinal);
    }

    [Fact]
    public void User_mutations_are_idempotent_audited_outboxed_and_versioned()
    {
        var application = Read("src/Application/IdentityAccess/UserAccessApplication.cs");
        var domain = Read("src/Domain/IdentityAccess/SecurityUser.cs");

        Assert.Contains("IIdempotencyStore", application, StringComparison.Ordinal);
        Assert.Contains("ITransactionManager", application, StringComparison.Ordinal);
        Assert.Contains("IUnitOfWork", application, StringComparison.Ordinal);
        Assert.Contains("IAuditWriter", application, StringComparison.Ordinal);
        Assert.Contains("IOutboxWriter", application, StringComparison.Ordinal);
        Assert.Contains("SECURITY_USER_CREATED", application, StringComparison.Ordinal);
        Assert.Contains("SECURITY_USER_UPDATED", application, StringComparison.Ordinal);
        Assert.Contains("SECURITY_USER_ROLES_CHANGED", application, StringComparison.Ordinal);
        Assert.Contains("SecurityUserChangedIntegrationEvent", application, StringComparison.Ordinal);
        Assert.Contains("SecurityUserRolesChangedIntegrationEvent", application, StringComparison.Ordinal);
        Assert.Contains("Version++", domain, StringComparison.Ordinal);
        Assert.Contains("concurrency.stale_version", domain, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_registers_the_W1_4_user_slice()
    {
        var program = Read("src/WebApi/Program.cs");

        Assert.Contains("AddScoped<ISecurityUserRepository, EfSecurityUserRepository>()", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<ListUsersUseCase>()", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<GetUserUseCase>()", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<CreateUserUseCase>()", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<UpdateUserUseCase>()", program, StringComparison.Ordinal);
        Assert.Contains("AddScoped<AssignUserRolesUseCase>()", program, StringComparison.Ordinal);
    }

    [Fact]
    public void W1_4_readiness_contract_remains_locked_to_five_operations()
    {
        var readiness = Read("documentation/api-completion-matrix/W1_4_USERS_READINESS.md");

        Assert.Contains("Scope: `API-IAM-002..005` and `API-IAM-010` only.", readiness, StringComparison.Ordinal);
        Assert.Contains("PATCH `/api/v1/users/{userId}`", readiness, StringComparison.Ordinal);
        Assert.Contains("PUT `/api/v1/users/{userId}/roles`", readiness, StringComparison.Ordinal);
        Assert.Contains("Wave 1: `26 / 30`", readiness, StringComparison.Ordinal);
        Assert.Contains("global public v1: `60 / 194`", readiness, StringComparison.Ordinal);
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
