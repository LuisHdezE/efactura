using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Security;
using EFactura.Application.IdentityAccess;
using Xunit;

namespace CrossCuttingTests;

public sealed class W12IdentityAccessTests
{
    [Fact]
    public void GetCurrentActor_projects_existing_context_with_deterministic_sets()
    {
        var actor = new ActorContext(
            "actor-42",
            "Ada Example",
            true,
            Set("sales.read", "catalog.read"),
            Set("company-b", "company-a"),
            Set("location-b", "location-a"),
            Set("terminal-b", "terminal-a"),
            "device-private");

        var result = new GetCurrentActorUseCase(new FakeActorAccessor(actor)).Execute();

        Assert.Equal("actor-42", result.ActorId);
        Assert.Equal("Ada Example", result.DisplayName);
        Assert.Equal(new[] { "catalog.read", "sales.read" }, result.Permissions);
        Assert.Equal(new[] { "company-a", "company-b" }, result.CompanyScopes);
        Assert.Equal(new[] { "location-a", "location-b" }, result.LocationScopes);
        Assert.Equal(new[] { "terminal-a", "terminal-b" }, result.TerminalScopes);
    }

    [Fact]
    public void GetCurrentActor_rejects_anonymous_context()
    {
        var error = Assert.Throws<ApplicationProblemException>(
            () => new GetCurrentActorUseCase(new FakeActorAccessor(ActorContext.Anonymous)).Execute());

        Assert.Equal(ApplicationProblemKind.AuthenticationRequired, error.Kind);
        Assert.Equal("authentication_required", error.Code);
    }

    [Fact]
    public void ListPermissions_requires_security_roles_read()
    {
        var actor = Authenticated(Set("catalog.read"));

        var error = Assert.Throws<ApplicationProblemException>(
            () => new ListPermissionsUseCase(new FakeActorAccessor(actor)).Execute());

        Assert.Equal(ApplicationProblemKind.Forbidden, error.Kind);
        Assert.Equal("permission_denied", error.Code);
    }

    [Fact]
    public void ListPermissions_returns_the_exact_canonical_permission_set_in_ordinal_order()
    {
        var actor = Authenticated(Set(Permissions.SecurityRolesRead));

        var result = new ListPermissionsUseCase(new FakeActorAccessor(actor)).Execute();
        var expected = Permissions.All.OrderBy(permission => permission, StringComparer.Ordinal).ToArray();

        Assert.Equal(68, result.Count);
        Assert.Equal(expected, result);
        Assert.Equal(result.Count, result.Distinct(StringComparer.Ordinal).Count());
    }

    private static ActorContext Authenticated(IReadOnlySet<string> permissions) => new(
        "actor-1",
        "Security Reader",
        true,
        permissions,
        Set("demo-org"),
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
}
