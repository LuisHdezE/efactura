namespace Infrastructure.Persistence.V1.Write.Models;

public sealed class V1PartyRecord
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public int Kind { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ResidenceCountry { get; set; } = string.Empty;
    public string TaxResidenceCountry { get; set; } = string.Empty;
    public bool Active { get; set; }
    public long Version { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<V1PartyRoleRecord> Roles { get; set; } = new();
    public List<V1PartyFiscalIdentityRecord> FiscalIdentities { get; set; } = new();
}

public sealed class V1PartyRoleRecord
{
    public Guid PartyId { get; set; }
    public int Role { get; set; }
    public V1PartyRecord Party { get; set; } = null!;
}

public sealed class V1PartyFiscalIdentityRecord
{
    public Guid Id { get; set; }
    public Guid PartyId { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
    public string IssuingCountry { get; set; } = string.Empty;
    public DateTime? ValidFromUtc { get; set; }
    public DateTime? ValidToUtc { get; set; }
    public bool Active { get; set; }
    public V1PartyRecord Party { get; set; } = null!;
}

public sealed class V1CommercialItemRecord
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Kind { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool TrackInventory { get; set; }
    public Guid? TaxProfileId { get; set; }
    public Guid? CategoryId { get; set; }
    public bool Active { get; set; }
    public long Version { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public V1ItemCategoryRecord? Category { get; set; }
}

public sealed class V1ItemCategoryRecord
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Active { get; set; }
    public long Version { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<V1CommercialItemRecord> Items { get; set; } = new();
}
