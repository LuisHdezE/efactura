using EFactura.Domain.Common;

namespace EFactura.Domain.Parties;

public enum PartyKind
{
    Person = 1,
    Organization = 2
}

public enum PartyRole
{
    Customer = 1,
    Supplier = 2
}

public enum PartyAddressKind
{
    Fiscal = 1,
    Delivery = 2,
    Other = 3
}

public sealed class PartyAddress
{
    private PartyAddress(
        Guid id,
        PartyAddressKind kind,
        string addressLine,
        string city,
        string? region,
        string countryCode,
        string? postalCode,
        bool primary)
    {
        if (id == Guid.Empty)
        {
            throw new DomainRuleException("party.address.id_required", "Party address id is required.");
        }

        if (!Enum.IsDefined(typeof(PartyAddressKind), kind))
        {
            throw new DomainRuleException("party.address.invalid_kind", "Party address kind is invalid.");
        }

        Id = id;
        Kind = kind;
        AddressLine = NormalizeRequired(addressLine, 255, "party.address.line_required");
        City = NormalizeRequired(city, 80, "party.address.city_required");
        Region = NormalizeOptional(region, 100, "party.address.region_invalid");
        CountryCode = NormalizeCountry(countryCode);
        PostalCode = NormalizeOptional(postalCode, 20, "party.address.postal_code_invalid");
        Primary = primary;
    }

    public Guid Id { get; }
    public PartyAddressKind Kind { get; }
    public string AddressLine { get; }
    public string City { get; }
    public string? Region { get; }
    public string CountryCode { get; }
    public string? PostalCode { get; }
    public bool Primary { get; }

    public static PartyAddress Create(
        Guid id,
        PartyAddressKind kind,
        string addressLine,
        string city,
        string? region,
        string countryCode,
        string? postalCode,
        bool primary) =>
        new(id, kind, addressLine, city, region, countryCode, postalCode, primary);

    public static PartyAddress Rehydrate(
        Guid id,
        PartyAddressKind kind,
        string addressLine,
        string city,
        string? region,
        string countryCode,
        string? postalCode,
        bool primary) =>
        new(id, kind, addressLine, city, region, countryCode, postalCode, primary);

    public string NormalizedKey => string.Join(
        "|",
        Kind.ToString(),
        AddressLine.ToUpperInvariant(),
        City.ToUpperInvariant(),
        Region?.ToUpperInvariant() ?? string.Empty,
        CountryCode,
        PostalCode?.ToUpperInvariant() ?? string.Empty);

    private static string NormalizeRequired(string value, int maxLength, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException(code, "Required party address value is missing.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainRuleException(code, $"Party address value cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainRuleException(code, $"Party address value cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeCountry(string value)
    {
        var normalized = NormalizeRequired(value, 2, "party.address.country_required").ToUpperInvariant();
        if (normalized.Length != 2 || normalized.Any(ch => ch < 'A' || ch > 'Z'))
        {
            throw new DomainRuleException("party.address.invalid_country", "Address country must be an ISO alpha-2 code.");
        }

        return normalized;
    }
}

public sealed class PartyContact
{
    private PartyContact(Guid id, string typeCode, string value, bool primary)
    {
        if (id == Guid.Empty)
        {
            throw new DomainRuleException("party.contact.id_required", "Party contact id is required.");
        }

        Id = id;
        TypeCode = NormalizeRequired(typeCode, 80, "party.contact.type_required");
        Value = NormalizeRequired(value, 100, "party.contact.value_required");
        Primary = primary;
    }

    public Guid Id { get; }
    public string TypeCode { get; }
    public string Value { get; }
    public bool Primary { get; }

    public static PartyContact Create(Guid id, string typeCode, string value, bool primary) =>
        new(id, typeCode, value, primary);

    public static PartyContact Rehydrate(Guid id, string typeCode, string value, bool primary) =>
        new(id, typeCode, value, primary);

    public string NormalizedKey => $"{TypeCode.ToUpperInvariant()}|{Value.ToUpperInvariant()}";

    private static string NormalizeRequired(string value, int maxLength, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException(code, "Required party contact value is missing.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainRuleException(code, $"Party contact value cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}

public sealed class PartyFiscalIdentity
{
    private PartyFiscalIdentity(
        Guid id,
        string typeCode,
        string number,
        string issuingCountry,
        DateOnly? validFrom,
        DateOnly? validTo,
        bool active)
    {
        Id = id;
        TypeCode = Normalize(typeCode, 32, "party.fiscal_identity.type_required");
        Number = Normalize(number, 80, "party.fiscal_identity.number_required");
        IssuingCountry = NormalizeCountry(issuingCountry);
        ValidFrom = validFrom;
        ValidTo = validTo;
        Active = active;
        ValidateDates();
    }

    public Guid Id { get; }
    public string TypeCode { get; private set; }
    public string Number { get; private set; }
    public string IssuingCountry { get; private set; }
    public DateOnly? ValidFrom { get; private set; }
    public DateOnly? ValidTo { get; private set; }
    public bool Active { get; private set; }

    public static PartyFiscalIdentity Create(
        Guid id,
        string typeCode,
        string number,
        string issuingCountry,
        DateOnly? validFrom = null,
        DateOnly? validTo = null) =>
        new(id, typeCode, number, issuingCountry, validFrom, validTo, true);

    public static PartyFiscalIdentity Rehydrate(
        Guid id,
        string typeCode,
        string number,
        string issuingCountry,
        DateOnly? validFrom,
        DateOnly? validTo,
        bool active) =>
        new(id, typeCode, number, issuingCountry, validFrom, validTo, active);

    public void Update(
        string typeCode,
        string number,
        string issuingCountry,
        DateOnly? validFrom,
        DateOnly? validTo,
        bool active)
    {
        TypeCode = Normalize(typeCode, 32, "party.fiscal_identity.type_required");
        Number = Normalize(number, 80, "party.fiscal_identity.number_required");
        IssuingCountry = NormalizeCountry(issuingCountry);
        ValidFrom = validFrom;
        ValidTo = validTo;
        Active = active;
        ValidateDates();
    }

    public string NormalizedKey => $"{TypeCode.ToUpperInvariant()}|{Number.ToUpperInvariant()}|{IssuingCountry}";

    private void ValidateDates()
    {
        if (ValidFrom.HasValue && ValidTo.HasValue && ValidTo.Value < ValidFrom.Value)
        {
            throw new DomainRuleException(
                "party.fiscal_identity.invalid_validity",
                "Fiscal identity validTo cannot be earlier than validFrom.");
        }
    }

    private static string Normalize(string value, int maxLength, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException(code, "Required fiscal identity value is missing.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainRuleException(code, $"Fiscal identity value cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeCountry(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException("party.fiscal_identity.country_required", "Issuing country is required.");
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized == "99")
        {
            return normalized;
        }

        if (normalized.Length != 2 || normalized.Any(ch => ch < 'A' || ch > 'Z'))
        {
            throw new DomainRuleException(
                "party.fiscal_identity.invalid_country",
                "Issuing country must be an ISO alpha-2 code or the accepted unknown-country marker 99.");
        }

        return normalized;
    }
}

public sealed class Party
{
    private readonly HashSet<PartyRole> _roles;
    private readonly List<PartyFiscalIdentity> _fiscalIdentities;
    private readonly List<PartyAddress> _addresses;
    private readonly List<PartyContact> _contacts;

    private Party(
        Guid id,
        string organizationId,
        PartyKind kind,
        string name,
        string residenceCountry,
        string taxResidenceCountry,
        IEnumerable<PartyRole> roles,
        IEnumerable<PartyFiscalIdentity> fiscalIdentities,
        IEnumerable<PartyAddress> addresses,
        IEnumerable<PartyContact> contacts,
        bool active,
        long version)
    {
        Id = id;
        OrganizationId = NormalizeRequired(organizationId, 200, "party.organization_required");
        Kind = kind;
        Name = NormalizeRequired(name, 250, "party.name_required");
        ResidenceCountry = NormalizeCountry(residenceCountry, "party.invalid_residence_country");
        TaxResidenceCountry = NormalizeCountry(taxResidenceCountry, "party.invalid_tax_residence_country");
        _roles = new HashSet<PartyRole>(roles);
        _fiscalIdentities = new List<PartyFiscalIdentity>(fiscalIdentities);
        _addresses = new List<PartyAddress>(addresses);
        _contacts = new List<PartyContact>(contacts);
        Active = active;
        Version = version;
        ValidateRoles();
        ValidateIdentityUniqueness();
        ValidateAddressCollection();
        ValidateContactCollection();
    }

    public Guid Id { get; }
    public string OrganizationId { get; }
    public PartyKind Kind { get; private set; }
    public string Name { get; private set; }
    public string ResidenceCountry { get; private set; }
    public string TaxResidenceCountry { get; private set; }
    public bool Active { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<PartyRole> Roles => _roles;
    public IReadOnlyCollection<PartyFiscalIdentity> FiscalIdentities => _fiscalIdentities;
    public IReadOnlyCollection<PartyAddress> Addresses => _addresses;
    public IReadOnlyCollection<PartyContact> Contacts => _contacts;

    public static Party Create(
        Guid id,
        string organizationId,
        PartyKind kind,
        string name,
        string residenceCountry,
        string taxResidenceCountry,
        IEnumerable<PartyRole> roles,
        IEnumerable<PartyFiscalIdentity>? fiscalIdentities = null,
        IEnumerable<PartyAddress>? addresses = null,
        IEnumerable<PartyContact>? contacts = null) =>
        new(
            id,
            organizationId,
            kind,
            name,
            residenceCountry,
            taxResidenceCountry,
            roles,
            fiscalIdentities ?? Array.Empty<PartyFiscalIdentity>(),
            addresses ?? Array.Empty<PartyAddress>(),
            contacts ?? Array.Empty<PartyContact>(),
            true,
            1);

    public static Party Rehydrate(
        Guid id,
        string organizationId,
        PartyKind kind,
        string name,
        string residenceCountry,
        string taxResidenceCountry,
        IEnumerable<PartyRole> roles,
        IEnumerable<PartyFiscalIdentity> fiscalIdentities,
        IEnumerable<PartyAddress> addresses,
        IEnumerable<PartyContact> contacts,
        bool active,
        long version) =>
        new(
            id,
            organizationId,
            kind,
            name,
            residenceCountry,
            taxResidenceCountry,
            roles,
            fiscalIdentities,
            addresses,
            contacts,
            active,
            version);

    public void UpdateMasterData(
        PartyKind kind,
        string name,
        string residenceCountry,
        string taxResidenceCountry,
        long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        ApplyScalarMasterData(kind, name, residenceCountry, taxResidenceCountry);
        Version++;
    }

    public void UpdateMasterData(
        PartyKind kind,
        string name,
        string residenceCountry,
        string taxResidenceCountry,
        IEnumerable<PartyAddress> addresses,
        IEnumerable<PartyContact> contacts,
        long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        ApplyScalarMasterData(kind, name, residenceCountry, taxResidenceCountry);

        _addresses.Clear();
        _addresses.AddRange(addresses);
        _contacts.Clear();
        _contacts.AddRange(contacts);
        ValidateAddressCollection();
        ValidateContactCollection();
        Version++;
    }

    public void SetRoles(IEnumerable<PartyRole> roles, long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        _roles.Clear();
        foreach (var role in roles.Distinct())
        {
            _roles.Add(role);
        }

        ValidateRoles();
        Version++;
    }

    public PartyFiscalIdentity AddFiscalIdentity(
        Guid identityId,
        string typeCode,
        string number,
        string issuingCountry,
        DateOnly? validFrom,
        DateOnly? validTo,
        long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        var identity = PartyFiscalIdentity.Create(identityId, typeCode, number, issuingCountry, validFrom, validTo);
        if (_fiscalIdentities.Any(existing => existing.NormalizedKey == identity.NormalizedKey))
        {
            throw new DomainRuleException(
                "party.fiscal_identity.duplicate",
                "The same fiscal identity is already registered for this party.");
        }

        _fiscalIdentities.Add(identity);
        Version++;
        return identity;
    }

    public void UpdateFiscalIdentity(
        Guid identityId,
        string typeCode,
        string number,
        string issuingCountry,
        DateOnly? validFrom,
        DateOnly? validTo,
        bool active,
        long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        var identity = _fiscalIdentities.SingleOrDefault(x => x.Id == identityId)
            ?? throw new DomainRuleException("party.fiscal_identity.not_found", "Fiscal identity was not found.");

        identity.Update(typeCode, number, issuingCountry, validFrom, validTo, active);
        ValidateIdentityUniqueness();
        Version++;
    }

    private void ApplyScalarMasterData(
        PartyKind kind,
        string name,
        string residenceCountry,
        string taxResidenceCountry)
    {
        Kind = kind;
        Name = NormalizeRequired(name, 250, "party.name_required");
        ResidenceCountry = NormalizeCountry(residenceCountry, "party.invalid_residence_country");
        TaxResidenceCountry = NormalizeCountry(taxResidenceCountry, "party.invalid_tax_residence_country");
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
        {
            throw new DomainRuleException("concurrency.stale_version", "The party changed before this operation was applied.");
        }
    }

    private void ValidateRoles()
    {
        if (_roles.Count == 0)
        {
            throw new DomainRuleException("party.role_required", "A party must have at least one commercial role.");
        }
    }

    private void ValidateIdentityUniqueness()
    {
        var duplicates = _fiscalIdentities
            .GroupBy(x => x.NormalizedKey, StringComparer.Ordinal)
            .Any(group => group.Count() > 1);

        if (duplicates)
        {
            throw new DomainRuleException("party.fiscal_identity.duplicate", "Duplicate fiscal identities are not allowed.");
        }
    }

    private void ValidateAddressCollection()
    {
        if (_addresses.GroupBy(x => x.NormalizedKey, StringComparer.Ordinal).Any(group => group.Count() > 1))
        {
            throw new DomainRuleException("party.address.duplicate", "Duplicate party addresses are not allowed.");
        }

        if (_addresses
            .Where(x => x.Primary)
            .GroupBy(x => x.Kind)
            .Any(group => group.Count() > 1))
        {
            throw new DomainRuleException(
                "party.address.multiple_primary",
                "Only one primary party address is allowed for each address kind.");
        }
    }

    private void ValidateContactCollection()
    {
        if (_contacts.GroupBy(x => x.NormalizedKey, StringComparer.Ordinal).Any(group => group.Count() > 1))
        {
            throw new DomainRuleException("party.contact.duplicate", "Duplicate party contacts are not allowed.");
        }

        if (_contacts
            .Where(x => x.Primary)
            .GroupBy(x => x.TypeCode.ToUpperInvariant(), StringComparer.Ordinal)
            .Any(group => group.Count() > 1))
        {
            throw new DomainRuleException(
                "party.contact.multiple_primary",
                "Only one primary party contact is allowed for each contact type.");
        }
    }

    private static string NormalizeRequired(string value, int maxLength, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException(code, "Required party value is missing.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainRuleException(code, $"Party value cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeCountry(string value, string code)
    {
        var normalized = NormalizeRequired(value, 2, code).ToUpperInvariant();
        if (normalized.Length != 2 || normalized.Any(ch => ch < 'A' || ch > 'Z'))
        {
            throw new DomainRuleException(code, "Country must be an ISO alpha-2 code.");
        }

        return normalized;
    }
}
