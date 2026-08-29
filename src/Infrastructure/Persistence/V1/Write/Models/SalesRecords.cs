namespace Infrastructure.Persistence.V1.Write.Models;

public sealed class V1SaleRecord
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string? LocationId { get; set; }
    public string? TerminalId { get; set; }
    public Guid? CustomerPartyId { get; set; }
    public int Intent { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTime EffectiveOnUtc { get; set; }
    public string? DeliveryCountry { get; set; }
    public bool GoodsExportConfirmed { get; set; }
    public int Status { get; set; }
    public string? ValidationFingerprint { get; set; }
    public DateTimeOffset? ValidatedAtUtc { get; set; }
    public long Version { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public List<V1SaleLineRecord> Lines { get; set; } = new();
}

public sealed class V1SaleLineRecord
{
    public Guid Id { get; set; }
    public Guid SaleId { get; set; }
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public int Kind { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public Guid? TaxProfileId { get; set; }
    public int ServicePerformanceScope { get; set; }
    public string? ServiceUseCountry { get; set; }
    public int ExportServiceKind { get; set; }
    public int RecipientIsPersonAbroad { get; set; }
    public int ExclusiveUseAbroad { get; set; }
    public int ForeignEconomicRelation { get; set; }
    public int RecipientInstalledInFreeZone { get; set; }
    public int ProviderFromNonFreeNationalTerritory { get; set; }
    public V1SaleRecord Sale { get; set; } = null!;
}
