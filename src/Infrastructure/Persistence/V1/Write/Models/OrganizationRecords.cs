namespace Infrastructure.Persistence.V1.Write.Models;

public sealed class V1CompanyFiscalProfileRecord
{
    public string OrganizationId { get; set; } = string.Empty;
    public string Ruc { get; set; } = string.Empty;
    public string LegalName { get; set; } = string.Empty;
    public string? CommercialName { get; set; }
    public long Version { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class V1FiscalLocationRecord
{
    public string Id { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DgiBranchCode { get; set; } = string.Empty;
    public string FiscalAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public bool Active { get; set; }
    public long Version { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
