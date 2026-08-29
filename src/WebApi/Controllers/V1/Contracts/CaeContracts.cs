namespace WebApi.Controllers.V1.Contracts;

public sealed record CaeImportRequest(
    int CfeType,
    string AuthorizationNumber,
    string Series,
    long RangeFrom,
    long RangeTo,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    string SourceArtifactId,
    string SourceArtifactHash,
    string SourceName,
    string SourceReference);

public sealed record CaeActivateRequest(long ExpectedVersion);

public sealed record CaeAllocationCreateRequest(
    string LocationId,
    string? TerminalId,
    long RangeFrom,
    long RangeTo,
    long ExpectedVersion);

public sealed record CaeAllocationCloseRequest(long ExpectedVersion);

public sealed record CaeAuthorizationDto(
    string Id,
    long Version,
    int CfeType,
    string AuthorizationNumber,
    string Series,
    long RangeFrom,
    long RangeTo,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    string Status,
    string VerificationMethod,
    string SourceArtifactId,
    string SourceArtifactHash,
    string SourceName,
    string SourceReference,
    DateTimeOffset ImportedAtUtc,
    DateTimeOffset? ActivatedAtUtc,
    string? AlertCode,
    bool Replayed = false);

public sealed record CaeAllocationDto(
    string Id,
    string CaeAuthorizationId,
    long Version,
    string LocationId,
    string? TerminalId,
    long RangeFrom,
    long RangeTo,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    bool Replayed = false);
