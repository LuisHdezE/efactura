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
    public Guid? FiscalDocumentId { get; set; }
    public DateTimeOffset? IdentityCreatedAtUtc { get; set; }
}

public sealed class V1FiscalDocumentRecord
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public Guid FiscalizationRequestId { get; set; }
    public Guid SaleId { get; set; }
    public Guid FiscalNumberReservationId { get; set; }
    public Guid CaeAuthorizationId { get; set; }
    public Guid? CaeAllocationId { get; set; }
    public int CfeType { get; set; }
    public string Series { get; set; } = string.Empty;
    public long Number { get; set; }
    public string CaeAuthorizationNumber { get; set; } = string.Empty;
    public long CaeRangeFrom { get; set; }
    public long CaeRangeTo { get; set; }
    public DateTime CaeValidFrom { get; set; }
    public DateTime CaeValidTo { get; set; }
    public DateTime FiscalDate { get; set; }
    public string? LocationId { get; set; }
    public string? TerminalId { get; set; }
    public int? ReceiverIdentification { get; set; }
    public string FormatVersion { get; set; } = string.Empty;
    public string ConfirmationFingerprint { get; set; } = string.Empty;
    public string SettlementFingerprint { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal NetAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public int Status { get; set; }
    public DateTimeOffset IdentityCreatedAtUtc { get; set; }
}
