namespace Infrastructure.Persistence.V1.Write.Models;

public sealed class V1FiscalDailyReportSigningEvidenceRecord
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string IssuerRuc { get; set; } = string.Empty;
    public DateTime SummaryDate { get; set; }
    public int Sequence { get; set; }
    public string FunctionalFormatVersion { get; set; } = string.Empty;
    public string ProjectionFingerprint { get; set; } = string.Empty;
    public string UnsignedContentHash { get; set; } = string.Empty;
    public DateTimeOffset SigningTimestamp { get; set; }
}

public sealed class V1FiscalDailyReportSignedArtifactRecord
{
    public Guid Id { get; set; }
    public Guid SigningEvidenceId { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string IssuerRuc { get; set; } = string.Empty;
    public DateTime SummaryDate { get; set; }
    public int Sequence { get; set; }
    public string FunctionalFormatVersion { get; set; } = string.Empty;
    public string ProjectionFingerprint { get; set; } = string.Empty;
    public string UnsignedContentHash { get; set; } = string.Empty;
    public string SignedContentHash { get; set; } = string.Empty;
    public DateTimeOffset SigningTimestamp { get; set; }
    public string SignatureProfileId { get; set; } = string.Empty;
    public string CertificateThumbprint { get; set; } = string.Empty;
    public string CertificateSerialNumber { get; set; } = string.Empty;
    public string SchemaSetId { get; set; } = string.Empty;
    public string SchemaFunctionalFormatVersion { get; set; } = string.Empty;
    public string SchemaArchiveVersion { get; set; } = string.Empty;
    public string SchemaSetFingerprint { get; set; } = string.Empty;
    public string SignedXml { get; set; } = string.Empty;
}
