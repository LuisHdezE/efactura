namespace WebApi.Controllers.V1.Contracts;

public sealed record PosBootstrapTerminalDto(
    string TerminalId,
    string Code,
    string Name,
    long TerminalVersion);

public sealed record PosBootstrapContextDto(
    string LocationId,
    string LocationName,
    string DgiBranchCode,
    long LocationVersion,
    IReadOnlyList<PosBootstrapTerminalDto> Terminals);

public sealed record PosBootstrapDto(
    string OrganizationId,
    DateTimeOffset GeneratedAtUtc,
    IReadOnlyList<PosBootstrapContextDto> Contexts);
