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
    public string? ConfirmationEvidenceFingerprint { get; set; }
    public string? ConfirmationEvidenceJson { get; set; }
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

public sealed class V1FiscalContentSnapshotRecord
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public Guid FiscalDocumentId { get; set; }
    public Guid FiscalizationRequestId { get; set; }
    public Guid SaleId { get; set; }
    public string ContentFingerprint { get; set; } = string.Empty;
    public string SnapshotJson { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public sealed class V1FiscalSigningEvidenceRecord
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public Guid FiscalDocumentId { get; set; }
    public string FiscalContentFingerprint { get; set; } = string.Empty;
    public string UnsignedContentHash { get; set; } = string.Empty;
    public DateTimeOffset SigningTimestamp { get; set; }
}

public sealed class V1FiscalSignedArtifactRecord
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public Guid FiscalDocumentId { get; set; }
    public Guid SigningEvidenceId { get; set; }
    public string FiscalContentFingerprint { get; set; } = string.Empty;
    public string UnsignedContentHash { get; set; } = string.Empty;
    public string SigningPayloadHash { get; set; } = string.Empty;
    public string SignedContentHash { get; set; } = string.Empty;
    public DateTimeOffset SigningTimestamp { get; set; }
    public string SignatureProfileId { get; set; } = string.Empty;
    public string CertificateThumbprint { get; set; } = string.Empty;
    public string CertificateSerialNumber { get; set; } = string.Empty;
    public string? SchemaSetId { get; set; }
    public string? SchemaVersion { get; set; }
    public string? SchemaSetFingerprint { get; set; }
    public string SignedXml { get; set; } = string.Empty;
}
