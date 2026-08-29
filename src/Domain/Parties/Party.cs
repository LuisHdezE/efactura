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

    private Party(
        Guid id,
        string organizationId,
        PartyKind kind,
        string name,
        string residenceCountry,
        string taxResidenceCountry,
        IEnumerable<PartyRole> roles,
        IEnumerable<PartyFiscalIdentity> fiscalIdentities,
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
        Active = active;
        Version = version;
        ValidateRoles();
        ValidateIdentityUniqueness();
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

    public static Party Create(
        Guid id,
        string organizationId,
        PartyKind kind,
        string name,
        string residenceCountry,
        string taxResidenceCountry,
        IEnumerable<PartyRole> roles,
        IEnumerable<PartyFiscalIdentity>? fiscalIdentities = null) =>
        new(
            id,
            organizationId,
            kind,
            name,
            residenceCountry,
            taxResidenceCountry,
            roles,
            fiscalIdentities ?? Array.Empty<PartyFiscalIdentity>(),
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
        bool active,
        long version) =>
        new(id, organizationId, kind, name, residenceCountry, taxResidenceCountry, roles, fiscalIdentities, active, version);

    public void UpdateMasterData(
        PartyKind kind,
        string name,
        string residenceCountry,
        string taxResidenceCountry,
        long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        Kind = kind;
        Name = NormalizeRequired(name, 250, "party.name_required");
        ResidenceCountry = NormalizeCountry(residenceCountry, "party.invalid_residence_country");
        TaxResidenceCountry = NormalizeCountry(taxResidenceCountry, "party.invalid_tax_residence_country");
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
