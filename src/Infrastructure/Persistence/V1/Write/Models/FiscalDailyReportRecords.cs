using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

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
    public int SigningOffsetMinutes { get; set; }
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
    public int SigningOffsetMinutes { get; set; }
    public string SignatureProfileId { get; set; } = string.Empty;
    public string CertificateThumbprint { get; set; } = string.Empty;
    public string CertificateSerialNumber { get; set; } = string.Empty;
    public string SchemaSetId { get; set; } = string.Empty;
    public string SchemaFunctionalFormatVersion { get; set; } = string.Empty;
    public string SchemaArchiveVersion { get; set; } = string.Empty;
    public string SchemaSetFingerprint { get; set; } = string.Empty;
    public string SignedXml { get; set; } = string.Empty;
    public ICollection<V1FiscalDailyReportSubmissionRecord> Submissions { get; set; } = new List<V1FiscalDailyReportSubmissionRecord>();
}

[Table("v1_fiscal_daily_report_submissions")]
[Index(nameof(OrganizationId), nameof(IssuerRuc), nameof(SummaryDate), nameof(Sequence), IsUnique = true, Name = "UX_v1_fdr_submission_identity")]
[Index(nameof(OrganizationId), nameof(OperationId), IsUnique = true, Name = "UX_v1_fdr_submission_operation")]
[Index(nameof(SignedArtifactId), IsUnique = true, Name = "UX_v1_fdr_submission_artifact")]
public sealed class V1FiscalDailyReportSubmissionRecord
{
    [Key]
    public Guid Id { get; set; }

    public Guid SignedArtifactId { get; set; }

    [MaxLength(200)]
    public string OrganizationId { get; set; } = string.Empty;

    [MaxLength(12)]
    public string IssuerRuc { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime SummaryDate { get; set; }

    public int Sequence { get; set; }

    [MaxLength(120)]
    public string OperationId { get; set; } = string.Empty;

    [MaxLength(64)]
    public string SignedContentHash { get; set; } = string.Empty;

    public int State { get; set; }
    public int AttemptCount { get; set; }

    [Precision(0)]
    public DateTimeOffset PreparedAtUtc { get; set; }

    [Precision(0)]
    public DateTimeOffset? LastAttemptAtUtc { get; set; }

    [Precision(0)]
    public DateTimeOffset? CompletedAtUtc { get; set; }

    [MaxLength(120)]
    public string? DgiReceiverId { get; set; }

    [MaxLength(8)]
    public string? AckStateCode { get; set; }

    public string? AckXml { get; set; }

    [MaxLength(160)]
    public string? FailureCode { get; set; }

    public V1FiscalDailyReportSignedArtifactRecord SignedArtifact { get; set; } = null!;
}
