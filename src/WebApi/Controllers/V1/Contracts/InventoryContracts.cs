namespace WebApi.Controllers.V1.Contracts;

public sealed record StockAdjustmentRequest(
    string ItemId,
    string LocationId,
    decimal QuantityDelta,
    string ReasonCode,
    long ExpectedVersion,
    string? Explanation = null);

public sealed record InventoryPositionDto(
    string Id,
    long Version,
    string ItemId,
    string LocationId,
    decimal Quantity);

public sealed record StockMovementDto(
    string Id,
    string PositionId,
    string ItemId,
    string LocationId,
    string Kind,
    decimal QuantityBefore,
    decimal QuantityDelta,
    decimal QuantityAfter,
    string ReasonCode,
    string? Explanation,
    DateTimeOffset OccurredAtUtc);

public sealed record StockAdjustmentResultDto(
    string PositionId,
    string MovementId,
    long Version,
    decimal Quantity,
    bool Replayed);
