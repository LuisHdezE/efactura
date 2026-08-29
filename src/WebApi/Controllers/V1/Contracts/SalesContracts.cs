namespace WebApi.Controllers.V1.Contracts;

public sealed record SaleLineRequest(
    string ItemId,
    decimal Quantity,
    decimal UnitPrice,
    string? ServicePerformanceScope = null,
    string? ServiceUseCountry = null,
    string? ExportServiceKind = null);

public sealed record SaleCreateRequest(
    string Intent,
    string CurrencyCode,
    DateOnly EffectiveOn,
    IReadOnlyCollection<SaleLineRequest> Lines,
    string? LocationId = null,
    string? TerminalId = null,
    string? CustomerPartyId = null,
    string? DeliveryCountry = null);

public sealed record SaleDraftUpdateRequest(
    long ExpectedVersion,
    string Intent,
    string CurrencyCode,
    DateOnly EffectiveOn,
    IReadOnlyCollection<SaleLineRequest> Lines,
    string? CustomerPartyId = null,
    string? DeliveryCountry = null);

public sealed record SaleValidateRequest(long ExpectedVersion);

public sealed record SaleLineDto(
    string Id,
    string ItemId,
    string ItemCode,
    string ItemName,
    string Kind,
    decimal Quantity,
    decimal UnitPrice,
    decimal NetAmount,
    string? TaxProfileId);

public sealed record SaleDto(
    string Id,
    long Version,
    string Status,
    string OrganizationId,
    string? LocationId,
    string? TerminalId,
    string? CustomerPartyId,
    string Intent,
    string CurrencyCode,
    DateOnly EffectiveOn,
    string? DeliveryCountry,
    decimal NetAmount,
    string? ValidationFingerprint,
    DateTimeOffset? ValidatedAtUtc,
    IReadOnlyCollection<SaleLineDto> Lines);

public sealed record FiscalRuleReferenceDto(
    string RuleId,
    string SourceName,
    string SourceReference,
    string SourceVersion,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string? Clause);

public sealed record SaleFiscalPreviewLineDto(
    string LineId,
    string ItemCode,
    decimal NetAmount,
    string TaxTreatmentStatus,
    string TaxTreatment,
    string TreatmentCode,
    string TaxRateStatus,
    string VatLiability,
    string VatRateKind,
    decimal? AppliedRatePercent,
    decimal? PreviewTaxAmount,
    IReadOnlyCollection<string> Reasons,
    IReadOnlyCollection<string> MissingFacts,
    IReadOnlyCollection<FiscalRuleReferenceDto> RuleReferences);

public sealed record CfeCandidateDto(
    int FamilyCode,
    string Family,
    string ReceiverIdentification,
    IReadOnlyCollection<string> Reasons);

public sealed record CfeDecisionDto(
    string EligibilityStatus,
    string SelectionStatus,
    int? SelectedFamilyCode,
    string? SelectedFamily,
    IReadOnlyCollection<CfeCandidateDto> Candidates,
    IReadOnlyCollection<string> Reasons,
    IReadOnlyCollection<string> MissingFacts,
    string FormatVersion);

public sealed record SaleFiscalPreviewDto(
    string SaleId,
    long SaleVersion,
    string CurrencyCode,
    decimal NetAmount,
    decimal? PreviewTaxAmount,
    decimal? PreviewTotalAmount,
    IReadOnlyCollection<SaleFiscalPreviewLineDto> Lines,
    string OverallTaxTreatmentStatus,
    string OverallTaxTreatment,
    string TreatmentCode,
    CfeDecisionDto Cfe,
    bool ReadyForValidation,
    string ValidationFingerprint,
    IReadOnlyCollection<string> Findings,
    string ArithmeticAuthority);

public sealed record SaleValidationDto(
    bool Valid,
    bool Replayed,
    SaleDto Sale,
    SaleFiscalPreviewDto Preview);
