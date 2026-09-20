namespace WebApi.Controllers.V1.Contracts;

public sealed record CurrentActorDto(
    string? ActorId,
    string? DisplayName,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> CompanyScopes,
    IReadOnlyList<string> LocationScopes,
    IReadOnlyList<string> TerminalScopes);

public sealed record PermissionDto(string Code);
