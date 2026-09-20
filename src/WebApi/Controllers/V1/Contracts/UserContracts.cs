namespace WebApi.Controllers.V1.Contracts;

public sealed record UserDto(
    string Id,
    string IdentityProvider,
    string ExternalSubject,
    string DisplayName,
    string? Email,
    bool Active,
    long Version,
    IReadOnlyList<string> LocationScopes,
    IReadOnlyList<string> TerminalScopes,
    IReadOnlyList<string> RoleIds);

public sealed record CreateUserRequest(
    string IdentityProvider,
    string ExternalSubject,
    string DisplayName,
    string? Email,
    IReadOnlyCollection<string> LocationScopes,
    IReadOnlyCollection<string> TerminalScopes);

public sealed record UpdateUserRequest(
    string? DisplayName,
    string? Email,
    bool? Active,
    IReadOnlyCollection<string>? LocationScopes,
    IReadOnlyCollection<string>? TerminalScopes,
    long ExpectedVersion);

public sealed record AssignUserRolesRequest(
    IReadOnlyCollection<string> RoleIds,
    long ExpectedVersion);
