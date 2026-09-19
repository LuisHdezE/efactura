namespace WebApi.Controllers.V1.Contracts;

public sealed record ReferenceDataCollectionDto<T>(
    string SourceName,
    string SourceVersion,
    IReadOnlyList<T> Items);

public sealed record CountryDto(
    string Alpha2Code,
    string Name);

public sealed record UruguayDepartmentDto(string Name);

public sealed record FiscalIdentityTypeDto(
    int Code,
    string Name,
    string CountryRule,
    IReadOnlyList<string> AllowedIssuingCountryCodes,
    bool AllowsOtherIsoCountry,
    bool AllowsSpecialCountryFallback);

public sealed record CurrencyDto(
    string AlphabeticCode,
    string Name);

public sealed record FiscalDocumentTypeDto(
    int Code,
    string Name,
    string Family,
    string CorrectionKind,
    int ContingencyCode,
    bool RequiresApplicabilityValidation);

public sealed record InvoiceIndicatorDto(
    int Code,
    string Name,
    string TaxTreatment);
