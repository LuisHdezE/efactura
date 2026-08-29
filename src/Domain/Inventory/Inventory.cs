using EFactura.Domain.Common;

namespace EFactura.Domain.Inventory;

public enum StockMovementKind
{
    Adjustment = 1
}

public sealed class StockMovement
{
    private StockMovement(
        Guid id,
        Guid positionId,
        string organizationId,
        Guid itemId,
        string locationId,
        StockMovementKind kind,
        decimal quantityBefore,
        decimal quantityDelta,
        decimal quantityAfter,
        long positionVersionAfter,
        string reasonCode,
        string? explanation,
        DateTimeOffset occurredAtUtc)
    {
        Id = id;
        PositionId = positionId;
        OrganizationId = organizationId;
        ItemId = itemId;
        LocationId = locationId;
        Kind = kind;
        QuantityBefore = quantityBefore;
        QuantityDelta = quantityDelta;
        QuantityAfter = quantityAfter;
        PositionVersionAfter = positionVersionAfter;
        ReasonCode = reasonCode;
        Explanation = explanation;
        OccurredAtUtc = occurredAtUtc;
    }

    public Guid Id { get; }
    public Guid PositionId { get; }
    public string OrganizationId { get; }
    public Guid ItemId { get; }
    public string LocationId { get; }
    public StockMovementKind Kind { get; }
    public decimal QuantityBefore { get; }
    public decimal QuantityDelta { get; }
    public decimal QuantityAfter { get; }
    public long PositionVersionAfter { get; }
    public string ReasonCode { get; }
    public string? Explanation { get; }
    public DateTimeOffset OccurredAtUtc { get; }

    public static StockMovement CreateAdjustment(
        Guid id,
        Guid positionId,
        string organizationId,
        Guid itemId,
        string locationId,
        decimal quantityBefore,
        decimal quantityDelta,
        decimal quantityAfter,
        long positionVersionAfter,
        string reasonCode,
        string? explanation,
        DateTimeOffset occurredAtUtc) =>
        new(
            id,
            positionId,
            Required(organizationId, 200, "inventory.organization_required"),
            itemId,
            Required(locationId, 200, "inventory.location_required"),
            StockMovementKind.Adjustment,
            quantityBefore,
            quantityDelta,
            quantityAfter,
            positionVersionAfter,
            Required(reasonCode, 80, "inventory.adjustment_reason_required"),
            Optional(explanation, 1000),
            occurredAtUtc);

    public static StockMovement Rehydrate(
        Guid id,
        Guid positionId,
        string organizationId,
        Guid itemId,
        string locationId,
        StockMovementKind kind,
        decimal quantityBefore,
        decimal quantityDelta,
        decimal quantityAfter,
        long positionVersionAfter,
        string reasonCode,
        string? explanation,
        DateTimeOffset occurredAtUtc) =>
        new(id, positionId, organizationId, itemId, locationId, kind, quantityBefore, quantityDelta,
            quantityAfter, positionVersionAfter, reasonCode, explanation, occurredAtUtc);

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainRuleException(code, "Required inventory value is missing.");
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw new DomainRuleException(code, $"Inventory value cannot exceed {max} characters.");
        return normalized;
    }

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw new DomainRuleException("inventory.value_too_long", $"Inventory value cannot exceed {max} characters.");
        return normalized;
    }
}

public sealed class InventoryPosition
{
    private InventoryPosition(
        Guid id,
        string organizationId,
        Guid itemId,
        string locationId,
        decimal quantity,
        long version)
    {
        Id = id;
        OrganizationId = Required(organizationId, 200, "inventory.organization_required");
        ItemId = itemId;
        LocationId = Required(locationId, 200, "inventory.location_required");
        Quantity = quantity;
        Version = version;
    }

    public Guid Id { get; }
    public string OrganizationId { get; }
    public Guid ItemId { get; }
    public string LocationId { get; }
    public decimal Quantity { get; private set; }
    public long Version { get; private set; }

    public static InventoryPosition Create(Guid id, string organizationId, Guid itemId, string locationId) =>
        new(id, organizationId, itemId, locationId, 0m, 1);

    public static InventoryPosition Rehydrate(
        Guid id,
        string organizationId,
        Guid itemId,
        string locationId,
        decimal quantity,
        long version) =>
        new(id, organizationId, itemId, locationId, quantity, version);

    public StockMovement ApplyAdjustment(
        decimal quantityDelta,
        string reasonCode,
        string? explanation,
        DateTimeOffset occurredAtUtc,
        long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new DomainRuleException("concurrency.stale_version", "The inventory position changed before this adjustment was applied.");
        if (quantityDelta == 0m)
            throw new DomainRuleException("inventory.adjustment_zero_delta", "Stock adjustment quantity delta cannot be zero.");

        var before = Quantity;
        Quantity += quantityDelta;
        Version++;

        return StockMovement.CreateAdjustment(
            Guid.NewGuid(), Id, OrganizationId, ItemId, LocationId,
            before, quantityDelta, Quantity, Version, reasonCode, explanation, occurredAtUtc);
    }

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainRuleException(code, "Required inventory value is missing.");
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw new DomainRuleException(code, $"Inventory value cannot exceed {max} characters.");
        return normalized;
    }
}
