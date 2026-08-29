namespace WebApi.Controllers.V1.Contracts;

public sealed record TaxProfileDto(
    string Id,
    string Code,
    string Name,
    string Treatment,
    decimal? RatePercent,
    int CfeBillingIndicator,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string RuleVersion,
    string SourceAuthority,
    string SourceReference,
    string SourceUri,
    string CfeSpecificationVersion,
    DateTimeOffset VerifiedAt,
    bool SystemProfile);
