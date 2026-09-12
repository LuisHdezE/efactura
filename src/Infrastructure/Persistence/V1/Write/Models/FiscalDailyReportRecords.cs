namespace Infrastructure.Persistence.V1.Write.Models;

public sealed class V1FiscalDailyReportVersionRecord
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string IssuerRuc { get; set; } = string.Empty;
    public DateTime SummaryDate { get; set; }
    public int Sequence { get; set; }
    public Guid? PreviousVersionId { get; set; }
    public string OperationId { get; set; } = string.Empty;
    public int RevisionKind { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string ReconciliationFingerprint { get; set; } = string.Empty;
    public bool RequiresFxReliquidation { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string VersionFingerprint { get; set; } = string.Empty;
}

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

public sealed class V1FiscalDailyReportSubmissionRecord
{
    public Guid Id { get; set; }
    public Guid SignedArtifactId { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string IssuerRuc { get; set; } = string.Empty;
    public DateTime SummaryDate { get; set; }
    public int Sequence { get; set; }
    public string OperationId { get; set; } = string.Empty;
    public string SignedContentHash { get; set; } = string.Empty;
    public int State { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset PreparedAtUtc { get; set; }
    public DateTimeOffset? LastAttemptAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? DgiReceiverId { get; set; }
    public string? AckStateCode { get; set; }
    public string? AckXml { get; set; }
    public string? FailureCode { get; set; }
}
