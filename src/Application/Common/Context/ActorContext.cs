namespace EFactura.Application.Common.Context;

public sealed record ActorContext(
    string? ActorId,
    string? DisplayName,
    bool IsAuthenticated,
    IReadOnlySet<string> Permissions,
    IReadOnlySet<string> CompanyScopes,
    IReadOnlySet<string> LocationScopes,
    IReadOnlySet<string> TerminalScopes,
    string? DeviceId)
{
    public static ActorContext Anonymous { get; } = new(
        null,
        null,
        false,
        new HashSet<string>(StringComparer.Ordinal),
        new HashSet<string>(StringComparer.Ordinal),
        new HashSet<string>(StringComparer.Ordinal),
        new HashSet<string>(StringComparer.Ordinal),
        null);

    public bool HasPermission(string permission) => Permissions.Contains(permission);
}

public interface IActorContextAccessor
{
    ActorContext Current { get; }
}
