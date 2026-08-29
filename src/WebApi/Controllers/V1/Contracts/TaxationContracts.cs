namespace WebApi.Controllers.V1.Contracts;

public sealed record TaxProfileDto(
    string Id,
    long Version,
    string Code,
    string Name,
    string TreatmentCode,
    decimal RatePercent,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string SourceName,
    string SourceReference,
    string SourceVersion,
    bool Active);
