using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Security;

namespace EFactura.Application.IdentityAccess;

public sealed record CurrentActorView(
    string? ActorId,
    string? DisplayName,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> CompanyScopes,
    IReadOnlyList<string> LocationScopes,
    IReadOnlyList<string> TerminalScopes);

public sealed class GetCurrentActorUseCase
{
    private readonly IActorContextAccessor _actors;

    public GetCurrentActorUseCase(IActorContextAccessor actors)
    {
        _actors = actors;
    }

    public CurrentActorView Execute()
    {
        var actor = _actors.Current;
        IdentityAccessAuthorization.EnsureAuthenticated(actor);

        return new CurrentActorView(
            actor.ActorId,
            actor.DisplayName,
            Order(actor.Permissions),
            Order(actor.CompanyScopes),
            Order(actor.LocationScopes),
            Order(actor.TerminalScopes));
    }

    private static IReadOnlyList<string> Order(IEnumerable<string> values) =>
        values.OrderBy(value => value, StringComparer.Ordinal).ToArray();
}

public sealed class ListPermissionsUseCase
{
    private readonly IActorContextAccessor _actors;

    public ListPermissionsUseCase(IActorContextAccessor actors)
    {
        _actors = actors;
    }

    public IReadOnlyList<string> Execute()
    {
        var actor = _actors.Current;
        IdentityAccessAuthorization.EnsurePermission(actor, Permissions.SecurityRolesRead);

        return Permissions.All
            .OrderBy(permission => permission, StringComparer.Ordinal)
            .ToArray();
    }
}

internal static class IdentityAccessAuthorization
{
    public static void EnsureAuthenticated(ActorContext actor)
    {
        if (!actor.IsAuthenticated)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.AuthenticationRequired,
                "authentication_required",
                "A valid bearer token is required for this operation.");
        }
    }

    public static void EnsurePermission(ActorContext actor, string permission)
    {
        EnsureAuthenticated(actor);

        if (!actor.HasPermission(permission))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "permission_denied",
                "The authenticated actor does not have permission for this operation.");
        }
    }
}
