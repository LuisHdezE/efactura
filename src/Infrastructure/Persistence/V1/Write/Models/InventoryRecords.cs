namespace Infrastructure.Persistence.V1.Write.Models;

public sealed class V1InventoryPositionRecord
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public Guid ItemId { get; set; }
    public string LocationId { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public long Version { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public List<V1StockMovementRecord> Movements { get; set; } = new();
}

public sealed class V1StockMovementRecord
{
    public Guid Id { get; set; }
    public Guid PositionId { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public Guid ItemId { get; set; }
    public string LocationId { get; set; } = string.Empty;
    public int Kind { get; set; }
    public decimal QuantityBefore { get; set; }
    public decimal QuantityDelta { get; set; }
    public decimal QuantityAfter { get; set; }
    public long PositionVersionAfter { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public V1InventoryPositionRecord Position { get; set; } = null!;
}
