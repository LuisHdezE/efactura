using EFactura.Domain.Common;

namespace EFactura.Domain.Catalog;

public enum CommercialItemKind
{
    Product = 1,
    Service = 2
}

public sealed class CommercialItem
{
    private CommercialItem(
        Guid id,
        string organizationId,
        string code,
        string name,
        string? description,
        CommercialItemKind kind,
        string unit,
        bool trackInventory,
        Guid? taxProfileId,
        Guid? categoryId,
        bool active,
        long version)
    {
        Id = id;
        OrganizationId = NormalizeRequired(organizationId, 200, "catalog.organization_required");
        Code = NormalizeRequired(code, 80, "catalog.code_required").ToUpperInvariant();
        Name = NormalizeRequired(name, 250, "catalog.name_required");
        Description = NormalizeOptional(description, 1000);
        Kind = kind;
        Unit = NormalizeRequired(unit, 40, "catalog.unit_required").ToUpperInvariant();
        TrackInventory = trackInventory;
        TaxProfileId = taxProfileId;
        CategoryId = categoryId;
        Active = active;
        Version = version;
        ValidateInventoryRule();
    }

    public Guid Id { get; }
    public string OrganizationId { get; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public CommercialItemKind Kind { get; private set; }
    public string Unit { get; private set; }
    public bool TrackInventory { get; private set; }
    public Guid? TaxProfileId { get; private set; }
    public Guid? CategoryId { get; private set; }
    public bool Active { get; private set; }
    public long Version { get; private set; }

    public static CommercialItem Create(
        Guid id,
        string organizationId,
        string code,
        string name,
        string? description,
        CommercialItemKind kind,
        string unit,
        bool trackInventory,
        Guid? taxProfileId,
        Guid? categoryId) =>
        new(id, organizationId, code, name, description, kind, unit, trackInventory, taxProfileId, categoryId, true, 1);

    public static CommercialItem Rehydrate(
        Guid id,
        string organizationId,
        string code,
        string name,
        string? description,
        CommercialItemKind kind,
        string unit,
        bool trackInventory,
        Guid? taxProfileId,
        Guid? categoryId,
        bool active,
        long version) =>
        new(id, organizationId, code, name, description, kind, unit, trackInventory, taxProfileId, categoryId, active, version);

    public void Update(
        string code,
        string name,
        string? description,
        CommercialItemKind kind,
        string unit,
        bool trackInventory,
        Guid? taxProfileId,
        Guid? categoryId,
        long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        Code = NormalizeRequired(code, 80, "catalog.code_required").ToUpperInvariant();
        Name = NormalizeRequired(name, 250, "catalog.name_required");
        Description = NormalizeOptional(description, 1000);
        Kind = kind;
        Unit = NormalizeRequired(unit, 40, "catalog.unit_required").ToUpperInvariant();
        TrackInventory = trackInventory;
        TaxProfileId = taxProfileId;
        CategoryId = categoryId;
        ValidateInventoryRule();
        Version++;
    }

    public void Deactivate(long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        if (!Active)
        {
            return;
        }

        Active = false;
        Version++;
    }

    private void ValidateInventoryRule()
    {
        if (Kind == CommercialItemKind.Service && TrackInventory)
        {
            throw new DomainRuleException(
                "catalog.service_inventory_forbidden",
                "A service cannot be configured as inventory tracked.");
        }
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
        {
            throw new DomainRuleException("concurrency.stale_version", "The item changed before this operation was applied.");
        }
    }

    private static string NormalizeRequired(string value, int maxLength, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException(code, "Required catalog value is missing.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainRuleException(code, $"Catalog value cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainRuleException("catalog.value_too_long", $"Catalog value cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}

public sealed class ItemCategory
{
    private ItemCategory(Guid id, string organizationId, string code, string name, bool active, long version)
    {
        Id = id;
        OrganizationId = organizationId.Trim();
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Active = active;
        Version = version;

        if (string.IsNullOrWhiteSpace(OrganizationId) || string.IsNullOrWhiteSpace(Code) || string.IsNullOrWhiteSpace(Name))
        {
            throw new DomainRuleException("catalog.category.required", "Category organization, code and name are required.");
        }
    }

    public Guid Id { get; }
    public string OrganizationId { get; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public bool Active { get; private set; }
    public long Version { get; private set; }

    public static ItemCategory Create(Guid id, string organizationId, string code, string name) =>
        new(id, organizationId, code, name, true, 1);

    public static ItemCategory Rehydrate(Guid id, string organizationId, string code, string name, bool active, long version) =>
        new(id, organizationId, code, name, active, version);
}
