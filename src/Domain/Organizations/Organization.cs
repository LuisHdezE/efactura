using EFactura.Domain.Common;

namespace EFactura.Domain.Organizations;

/// <summary>
/// Mutable issuer master data. Historical CFE documents must snapshot these values and never
/// dereference this aggregate after issuance.
/// </summary>
public sealed class CompanyFiscalProfile
{
    private CompanyFiscalProfile(
        string organizationId,
        string ruc,
        string legalName,
        string? commercialName,
        long version)
    {
        OrganizationId = Required(organizationId, 200, "organization.company.organization_required");
        Ruc = RucValue(ruc);
        LegalName = Required(legalName, 150, "organization.company.legal_name_required");
        CommercialName = Optional(commercialName, 30);
        if (version <= 0)
            throw Rule("organization.company.version_invalid", "Company fiscal profile version must be positive.");
        Version = version;
    }

    public string OrganizationId { get; }
    public string Ruc { get; private set; }
    public string LegalName { get; private set; }
    public string? CommercialName { get; private set; }
    public long Version { get; private set; }

    public static CompanyFiscalProfile Create(
        string organizationId,
        string ruc,
        string legalName,
        string? commercialName) =>
        new(organizationId, ruc, legalName, commercialName, 1);

    public static CompanyFiscalProfile Rehydrate(
        string organizationId,
        string ruc,
        string legalName,
        string? commercialName,
        long version) =>
        new(organizationId, ruc, legalName, commercialName, version);

    public void Update(
        string ruc,
        string legalName,
        string? commercialName,
        long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        Ruc = RucValue(ruc);
        LegalName = Required(legalName, 150, "organization.company.legal_name_required");
        CommercialName = Optional(commercialName, 30);
        Version++;
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw Rule("concurrency.stale_version", "The company fiscal profile changed before this operation was applied.");
    }

    private static string RucValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule("organization.company.ruc_required", "Issuer RUC is required.");

        var normalized = value.Trim();
        if (normalized.Length != 12 || normalized.Any(ch => ch < '0' || ch > '9'))
        {
            throw Rule(
                "organization.company.ruc_invalid",
                "Issuer RUC must contain exactly 12 decimal digits.");
        }

        return normalized;
    }

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule(code, "Required issuer-profile value is missing.");

        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule(code, $"Issuer-profile value cannot exceed {max} characters.");

        return normalized;
    }

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule("organization.company.value_too_long", $"Issuer-profile value cannot exceed {max} characters.");

        return normalized;
    }

    private static DomainRuleException Rule(string code, string message) => new(code, message);
}

/// <summary>
/// Operational/fiscal branch master used by sales, CAE allocation and later CFE issuer snapshots.
/// The DGI branch code is stored as four digits so leading zeroes cannot be lost.
/// </summary>
public sealed class FiscalLocation
{
    private FiscalLocation(
        string id,
        string organizationId,
        string name,
        string dgiBranchCode,
        string fiscalAddress,
        string city,
        string department,
        bool active,
        long version)
    {
        Id = Required(id, 200, "organization.location.id_required");
        OrganizationId = Required(organizationId, 200, "organization.location.organization_required");
        Name = Required(name, 120, "organization.location.name_required");
        DgiBranchCode = BranchCode(dgiBranchCode);
        FiscalAddress = Required(fiscalAddress, 70, "organization.location.fiscal_address_required");
        City = Required(city, 30, "organization.location.city_required");
        Department = Required(department, 30, "organization.location.department_required");
        if (version <= 0)
            throw Rule("organization.location.version_invalid", "Location version must be positive.");
        Active = active;
        Version = version;
    }

    public string Id { get; }
    public string OrganizationId { get; }
    public string Name { get; private set; }
    public string DgiBranchCode { get; private set; }
    public string FiscalAddress { get; private set; }
    public string City { get; private set; }
    public string Department { get; private set; }
    public bool Active { get; private set; }
    public long Version { get; private set; }

    public static FiscalLocation Create(
        string id,
        string organizationId,
        string name,
        string dgiBranchCode,
        string fiscalAddress,
        string city,
        string department) =>
        new(id, organizationId, name, dgiBranchCode, fiscalAddress, city, department, true, 1);

    public static FiscalLocation Rehydrate(
        string id,
        string organizationId,
        string name,
        string dgiBranchCode,
        string fiscalAddress,
        string city,
        string department,
        bool active,
        long version) =>
        new(id, organizationId, name, dgiBranchCode, fiscalAddress, city, department, active, version);

    public void Update(
        string name,
        string dgiBranchCode,
        string fiscalAddress,
        string city,
        string department,
        bool active,
        long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        Name = Required(name, 120, "organization.location.name_required");
        DgiBranchCode = BranchCode(dgiBranchCode);
        FiscalAddress = Required(fiscalAddress, 70, "organization.location.fiscal_address_required");
        City = Required(city, 30, "organization.location.city_required");
        Department = Required(department, 30, "organization.location.department_required");
        Active = active;
        Version++;
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw Rule("concurrency.stale_version", "The fiscal location changed before this operation was applied.");
    }

    private static string BranchCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule("organization.location.branch_code_required", "DGI branch code is required.");

        var normalized = value.Trim();
        if (normalized.Length != 4 || normalized.Any(ch => ch < '0' || ch > '9'))
        {
            throw Rule(
                "organization.location.branch_code_invalid",
                "DGI branch code must contain exactly four decimal digits.");
        }

        return normalized;
    }

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule(code, "Required location value is missing.");

        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule(code, $"Location value cannot exceed {max} characters.");

        return normalized;
    }

    private static DomainRuleException Rule(string code, string message) => new(code, message);
}
