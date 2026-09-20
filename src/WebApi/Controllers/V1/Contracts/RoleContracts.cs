namespace WebApi.Controllers.V1.Contracts;

public sealed record RoleDto(
    string Id,
    string Name,
    string? Description,
    bool Active,
    long Version,
    IReadOnlyList<string> Permissions);

public sealed record CreateRoleRequest(
    string Name,
    string? Description,
    IReadOnlyCollection<string> Permissions);

public sealed record UpdateRoleRequest(
    string Name,
    string? Description,
    bool Active,
    IReadOnlyCollection<string> Permissions,
    long ExpectedVersion);
