namespace WebApi.Controllers.V1.Contracts;

public sealed record ReferenceDataCollectionDto<T>(
    string SourceName,
    string SourceVersion,
    IReadOnlyList<T> Items);

public sealed record UruguayDepartmentDto(string Name);

public sealed record FiscalIdentityTypeDto(
    int Code,
    string Name,
    string CountryRule,
    IReadOnlyList<string> AllowedIssuingCountryCodes,
    bool AllowsOtherIsoCountry,
    bool AllowsSpecialCountryFallback);
