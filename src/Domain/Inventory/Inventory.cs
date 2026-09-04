using EFactura.Domain.Common;

namespace EFactura.Domain.Inventory;

public enum StockMovementKind
{
    Adjustment = 1,
    SaleConsumption = 2
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
        DateTimeOffset occurredAtUtc,
        Guid? sourceSaleId,
        string? confirmationFingerprint,
        string? settlementFingerprint)
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
        SourceSaleId = sourceSaleId;
        ConfirmationFingerprint = confirmationFingerprint;
        SettlementFingerprint = settlementFingerprint;
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
    public Guid? SourceSaleId { get; }
    public string? ConfirmationFingerprint { get; }
    public string? SettlementFingerprint { get; }

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
            occurredAtUtc,
            null,
            null,
            null);

    public static StockMovement CreateSaleConsumption(
        Guid id,
        Guid positionId,
        string organizationId,
        Guid itemId,
        string locationId,
        Guid sourceSaleId,
        decimal quantityBefore,
        decimal quantityConsumed,
        decimal quantityAfter,
        long positionVersionAfter,
        string confirmationFingerprint,
        string settlementFingerprint,
        DateTimeOffset occurredAtUtc)
    {
        if (id == Guid.Empty)
            throw new DomainRuleException("inventory.movement_id_required", "Stock movement id is required.");
        if (positionId == Guid.Empty)
            throw new DomainRuleException("inventory.position_id_required", "Inventory position id is required.");
        if (itemId == Guid.Empty)
            throw new DomainRuleException("inventory.item_id_required", "Inventory item id is required.");
        if (sourceSaleId == Guid.Empty)
            throw new DomainRuleException("inventory.sale_id_required", "Sale consumption requires a source sale id.");
        if (quantityConsumed <= 0m)
            throw new DomainRuleException("inventory.sale_consumption_quantity_invalid", "Sale stock consumption quantity must be greater than zero.");
        if (quantityAfter < 0m || quantityBefore - quantityConsumed != quantityAfter)
            throw new DomainRuleException("inventory.sale_consumption_quantity_mismatch", "Sale stock consumption quantities are inconsistent.");

        return new StockMovement(
            id,
            positionId,
            Required(organizationId, 200, "inventory.organization_required"),
            itemId,
            Required(locationId, 200, "inventory.location_required"),
            StockMovementKind.SaleConsumption,
            quantityBefore,
            -quantityConsumed,
            quantityAfter,
            positionVersionAfter,
            "SALE_CONFIRMATION",
            null,
            occurredAtUtc,
            sourceSaleId,
            Fingerprint(confirmationFingerprint, "inventory.confirmation_fingerprint_invalid"),
            Fingerprint(settlementFingerprint, "inventory.settlement_fingerprint_invalid"));
    }

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
        DateTimeOffset occurredAtUtc,
        Guid? sourceSaleId = null,
        string? confirmationFingerprint = null,
        string? settlementFingerprint = null) =>
        new(id, positionId, organizationId, itemId, locationId, kind, quantityBefore, quantityDelta,
            quantityAfter, positionVersionAfter, reasonCode, explanation, occurredAtUtc,
            sourceSaleId, confirmationFingerprint, settlementFingerprint);

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

    private static string Fingerprint(string value, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainRuleException(code, "Sale stock movement fingerprint is required.");
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized.Length != 64 || normalized.Any(ch => !Uri.IsHexDigit(ch)))
            throw new DomainRuleException(code, "Sale stock movement fingerprint must be a SHA-256 hexadecimal value.");
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
        var after = before + quantityDelta;
        var movement = StockMovement.CreateAdjustment(
            Guid.NewGuid(), Id, OrganizationId, ItemId, LocationId,
            before, quantityDelta, after, Version + 1, reasonCode, explanation, occurredAtUtc);

        Quantity = after;
        Version++;
        return movement;
    }

    public StockMovement ConsumeForSale(
        Guid saleId,
        decimal quantity,
        string confirmationFingerprint,
        string settlementFingerprint,
        DateTimeOffset occurredAtUtc,
        long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new DomainRuleException("concurrency.stale_version", "The inventory position changed before sale consumption was applied.");
        if (saleId == Guid.Empty)
            throw new DomainRuleException("inventory.sale_id_required", "Sale stock consumption requires a source sale id.");
        if (quantity <= 0m)
            throw new DomainRuleException("inventory.sale_consumption_quantity_invalid", "Sale stock consumption quantity must be greater than zero.");
        if (Quantity < quantity)
            throw new DomainRuleException("inventory.insufficient_stock", "The inventory position no longer has enough quantity to confirm the sale.");

        var before = Quantity;
        var after = before - quantity;
        var movement = StockMovement.CreateSaleConsumption(
            Guid.NewGuid(), Id, OrganizationId, ItemId, LocationId, saleId,
            before, quantity, after, Version + 1,
            confirmationFingerprint, settlementFingerprint, occurredAtUtc);

        Quantity = after;
        Version++;
        return movement;
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
