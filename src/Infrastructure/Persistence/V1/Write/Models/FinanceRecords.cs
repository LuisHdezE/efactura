namespace Infrastructure.Persistence.V1.Write.Models;

public sealed class V1PaymentMethodRecord
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public long Version { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

public sealed class V1PaymentRecord
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public Guid SaleId { get; set; }
    public int Sequence { get; set; }
    public Guid PaymentMethodId { get; set; }
    public long PaymentMethodVersion { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public string ConfirmationFingerprint { get; set; } = string.Empty;
    public string SettlementFingerprint { get; set; } = string.Empty;
    public DateTimeOffset RecordedAtUtc { get; set; }
}

public sealed class V1ReceivableRecord
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public Guid CustomerPartyId { get; set; }
    public Guid SaleId { get; set; }
    public decimal OriginalAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public string ConfirmationFingerprint { get; set; } = string.Empty;
    public string SettlementFingerprint { get; set; } = string.Empty;
    public long Version { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
