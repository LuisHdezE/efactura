namespace Infrastructure.Persistence.V1.Write.Models;

public sealed class V1FiscalizationRequestRecord
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public Guid SaleId { get; set; }
    public string? LocationId { get; set; }
    public string? TerminalId { get; set; }
    public int CfeFamily { get; set; }
    public int? ReceiverIdentification { get; set; }
    public string FormatVersion { get; set; } = string.Empty;
    public string ConfirmationFingerprint { get; set; } = string.Empty;
    public string SettlementFingerprint { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal NetAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public int Status { get; set; }
    public long Version { get; set; }
    public DateTimeOffset RequestedAtUtc { get; set; }
}
