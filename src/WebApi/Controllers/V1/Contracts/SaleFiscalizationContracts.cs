namespace WebApi.Controllers.V1.Contracts;

public sealed record SaleFiscalDocumentIdentityDto(
    string Id,
    int CfeTypeCode,
    string CfeType,
    string Series,
    long Number,
    DateOnly FiscalDate,
    DateTimeOffset IdentityCreatedAtUtc);

public sealed record SaleFiscalizationStatusDto(
    string SaleId,
    string SaleStatus,
    string WorkflowStatus,
    string? FiscalizationRequestId,
    long? FiscalizationVersion,
    DateTimeOffset? RequestedAtUtc,
    int? CfeFamilyCode,
    string? CfeFamily,
    string? FormatVersion,
    SaleFiscalDocumentIdentityDto? FiscalDocument,
    string StatusAuthority);
