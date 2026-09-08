namespace WebApi.Controllers.V1.Contracts;

public sealed record PageResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long Total);

public sealed record PartyFiscalIdentityRequest(
    string TypeCode,
    string Number,
    string IssuingCountry,
    DateOnly? ValidFrom = null,
    DateOnly? ValidTo = null);

public sealed record PartyAddressRequest(
    string Kind,
    string AddressLine,
    string City,
    string CountryCode,
    string? Region = null,
    string? PostalCode = null,
    bool Primary = false);

public sealed record PartyContactRequest(
    string TypeCode,
    string Value,
    bool Primary = false);

public sealed record PartyCreateRequest(
    string Kind,
    string Name,
    string ResidenceCountry,
    string TaxResidenceCountry,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<PartyFiscalIdentityRequest>? FiscalIdentities = null,
    IReadOnlyCollection<PartyAddressRequest>? Addresses = null,
    IReadOnlyCollection<PartyContactRequest>? Contacts = null);

public sealed record PartyUpdateRequest(
    long ExpectedVersion,
    string? Kind = null,
    string? Name = null,
    string? ResidenceCountry = null,
    string? TaxResidenceCountry = null,
    IReadOnlyCollection<PartyAddressRequest>? Addresses = null,
    IReadOnlyCollection<PartyContactRequest>? Contacts = null);

public sealed record PartyFiscalIdentityCreateRequest(
    long ExpectedVersion,
    string TypeCode,
    string Number,
    string IssuingCountry,
    DateOnly? ValidFrom = null,
    DateOnly? ValidTo = null);

public sealed record PartyFiscalIdentityUpdateRequest(
    long ExpectedVersion,
    string TypeCode,
    string Number,
    string IssuingCountry,
    bool Active,
    DateOnly? ValidFrom = null,
    DateOnly? ValidTo = null);

public sealed record PartyRolesUpdateRequest(
    long ExpectedVersion,
    IReadOnlyCollection<string> Roles);

public sealed record PartyFiscalIdentityDto(
    string Id,
    string TypeCode,
    string Number,
    string IssuingCountry,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    bool Active);

public sealed record PartyAddressDto(
    string Id,
    string Kind,
    string AddressLine,
    string City,
    string? Region,
    string CountryCode,
    string? PostalCode,
    bool Primary);

public sealed record PartyContactDto(
    string Id,
    string TypeCode,
    string Value,
    bool Primary);

public sealed record PartyDto(
    string Id,
    long Version,
    bool Active,
    string Kind,
    string Name,
    string ResidenceCountry,
    string TaxResidenceCountry,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<PartyFiscalIdentityDto> FiscalIdentities,
    IReadOnlyCollection<PartyAddressDto> Addresses,
    IReadOnlyCollection<PartyContactDto> Contacts);

public sealed record CommercialItemCreateRequest(
    string Code,
    string Name,
    string Kind,
    string Unit,
    bool TrackInventory,
    string? Description = null,
    string? TaxProfileId = null,
    string? CategoryId = null);

public sealed record CommercialItemUpdateRequest(
    long ExpectedVersion,
    string? Code = null,
    string? Name = null,
    string? Description = null,
    string? Kind = null,
    string? Unit = null,
    bool? TrackInventory = null,
    string? TaxProfileId = null,
    string? CategoryId = null);

public sealed record CommercialItemDeactivateRequest(long ExpectedVersion);

public sealed record CommercialItemDto(
    string Id,
    long Version,
    bool Active,
    string Code,
    string Name,
    string? Description,
    string Kind,
    string Unit,
    bool TrackInventory,
    string? TaxProfileId,
    string? CategoryId);

public sealed record ItemCategoryCreateRequest(string Code, string Name);

public sealed record ItemCategoryUpdateRequest(
    long ExpectedVersion,
    string? Code = null,
    string? Name = null,
    bool? Active = null);

public sealed record ItemCategoryDto(
    string Id,
    long Version,
    bool Active,
    string Code,
    string Name);
