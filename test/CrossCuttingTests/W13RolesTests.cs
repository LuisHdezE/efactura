using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Security;
using EFactura.Application.IdentityAccess;
using EFactura.Domain.Common;
using EFactura.Domain.IdentityAccess;
using Xunit;

namespace CrossCuttingTests;

public sealed class W13RolesTests
{
    [Fact]
    public void SecurityRole_create_deduplicates_and_orders_permissions()
    {
        var role = SecurityRole.Create(
            "role-1",
            "demo-org",
            "Operator",
            "Operational role",
            new[] { "sales.read", "catalog.read", "sales.read" });

        Assert.Equal(1, role.Version);
        Assert.True(role.Active);
        Assert.Equal(new[] { "catalog.read", "sales.read" }, role.Permissions);
    }

    [Fact]
    public void SecurityRole_replace_is_full_replacement_and_increments_version()
    {
        var role = SecurityRole.Create(
            "role-1",
            "demo-org",
            "Operator",
            null,
            new[] { "catalog.read", "sales.read" });

        role.Replace(
            "Supervisor",
            "Updated",
            false,
            new[] { "audit.read", "catalog.read" },
            expectedVersion: 1);

        Assert.Equal("Supervisor", role.Name);
        Assert.Equal("Updated", role.Description);
        Assert.False(role.Active);
        Assert.Equal(2, role.Version);
        Assert.Equal(new[] { "audit.read", "catalog.read" }, role.Permissions);
    }

    [Fact]
    public void SecurityRole_replace_rejects_stale_version()
    {
        var role = SecurityRole.Create(
            "role-1",
            "demo-org",
            "Operator",
            null,
            new[] { "catalog.read" });

        var error = Assert.Throws<DomainRuleException>(() =>
            role.Replace("Operator", null, true, new[] { "catalog.read" }, expectedVersion: 2));

        Assert.Equal("concurrency.stale_version", error.Code);
    }

    [Fact]
    public async Task ListRoles_requires_security_roles_read()
    {
        var useCase = new ListRolesUseCase(
            new FakeRoleRepository(),
            new FakeActorAccessor(Actor(Set("catalog.read"), Set("demo-org"))));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => useCase.ExecuteAsync("demo-org"));

        Assert.Equal(ApplicationProblemKind.Forbidden, error.Kind);
        Assert.Equal("permission_denied", error.Code);
    }

    [Fact]
    public async Task ListRoles_denies_company_outside_actor_scope()
    {
        var useCase = new ListRolesUseCase(
            new FakeRoleRepository(),
            new FakeActorAccessor(Actor(Set(Permissions.SecurityRolesRead), Set("company-a"))));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => useCase.ExecuteAsync("company-b"));

        Assert.Equal(ApplicationProblemKind.Forbidden, error.Kind);
        Assert.Equal("organization_scope_denied", error.Code);
    }

    [Fact]
    public async Task ListRoles_is_deterministic_by_name_then_id()
    {
        var repository = new FakeRoleRepository(
            SecurityRole.Create("role-z", "demo-org", "Zeta", null, new[] { "sales.read" }),
            SecurityRole.Create("role-b", "demo-org", "alpha", null, new[] { "catalog.read" }),
            SecurityRole.Create("role-a", "demo-org", "Alpha", null, new[] { "audit.read" }));

        var useCase = new ListRolesUseCase(
            repository,
            new FakeActorAccessor(Actor(Set(Permissions.SecurityRolesRead), Set("demo-org"))));

        var result = await useCase.ExecuteAsync("demo-org");

        Assert.Equal(new[] { "role-a", "role-b", "role-z" }, result.Select(x => x.Id));
    }

    [Fact]
    public async Task GetRole_returns_not_found_for_unknown_role()
    {
        var useCase = new GetRoleUseCase(
            new FakeRoleRepository(),
            new FakeActorAccessor(Actor(Set(Permissions.SecurityRolesRead), Set("demo-org"))));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => useCase.ExecuteAsync("demo-org", "missing"));

        Assert.Equal(ApplicationProblemKind.NotFound, error.Kind);
        Assert.Equal("identity.role.not_found", error.Code);
    }

    private static ActorContext Actor(IReadOnlySet<string> permissions, IReadOnlySet<string> companies) => new(
        "actor-w13",
        "W1.3 Tester",
        true,
        permissions,
        companies,
        Set(),
        Set(),
        null);

    private static IReadOnlySet<string> Set(params string[] values) =>
        new HashSet<string>(values, StringComparer.Ordinal);

    private sealed class FakeActorAccessor : IActorContextAccessor
    {
        public FakeActorAccessor(ActorContext current) => Current = current;
        public ActorContext Current { get; }
    }

    private sealed class FakeRoleRepository : ISecurityRoleRepository
    {
        private readonly List<SecurityRole> _roles;

        public FakeRoleRepository(params SecurityRole[] roles) => _roles = roles.ToList();

        public Task<IReadOnlyCollection<SecurityRole>> ListAsync(
            string organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<SecurityRole>>(
                _roles.Where(x => x.OrganizationId == organizationId).ToArray());

        public Task<SecurityRole?> GetAsync(
            string organizationId,
            string roleId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_roles.SingleOrDefault(x => x.OrganizationId == organizationId && x.Id == roleId));

        public Task<bool> NormalizedNameExistsAsync(
            string organizationId,
            string normalizedName,
            string? excludingRoleId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_roles.Any(x =>
                x.OrganizationId == organizationId
                && x.Name.Trim().ToUpperInvariant() == normalizedName
                && x.Id != excludingRoleId));

        public Task AddAsync(SecurityRole role, CancellationToken cancellationToken = default)
        {
            _roles.Add(role);
            return Task.CompletedTask;
        }

        public Task SaveAsync(SecurityRole role, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
