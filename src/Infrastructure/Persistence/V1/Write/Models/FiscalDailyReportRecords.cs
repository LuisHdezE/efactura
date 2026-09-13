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

    [InverseProperty(nameof(V1FiscalDailyReportBrCorrectionRevisionRecord.RootSubmission))]
    public ICollection<V1FiscalDailyReportBrCorrectionRevisionRecord> BrCorrectionRevisions { get; set; }
        = new List<V1FiscalDailyReportBrCorrectionRevisionRecord>();

    [InverseProperty(nameof(V1FiscalDailyReportResponseConsultationRecord.RootSubmission))]
    public ICollection<V1FiscalDailyReportResponseConsultationRecord> ResponseConsultations { get; set; }
        = new List<V1FiscalDailyReportResponseConsultationRecord>();

    [InverseProperty(nameof(V1FiscalDailyReportReceiverDiscoveryRecord.RootSubmission))]
    public ICollection<V1FiscalDailyReportReceiverDiscoveryRecord> ReceiverDiscoveries { get; set; }
        = new List<V1FiscalDailyReportReceiverDiscoveryRecord>();
}

[Table("v1_fdr_br_revisions")]
[Index(nameof(OrganizationId), nameof(OperationId), IsUnique = true, Name = "UX_v1_fdr_br_revision_operation")]
[Index(nameof(OrganizationId), nameof(IssuerRuc), nameof(SummaryDate), nameof(Sequence), nameof(LocalRevision), IsUnique = true, Name = "UX_v1_fdr_br_revision_identity")]
[Index(nameof(PreviousRevisionId), IsUnique = true, Name = "UX_v1_fdr_br_revision_previous")]
[Index(nameof(RootSubmissionId), Name = "IX_v1_fdr_br_revision_root_submission")]
[Index(nameof(RootSignedArtifactId), Name = "IX_v1_fdr_br_revision_root_artifact")]
[Index(nameof(SignedArtifactId), IsUnique = true, Name = "UX_v1_fdr_br_revision_signed_artifact")]
[Index(nameof(SigningEvidenceId), IsUnique = true, Name = "UX_v1_fdr_br_revision_signing_evidence")]
public sealed class V1FiscalDailyReportBrCorrectionRevisionRecord
{
    [Key]
    public Guid Id { get; set; }

    public Guid RootSubmissionId { get; set; }
    public Guid RootSignedArtifactId { get; set; }
    public Guid? PreviousRevisionId { get; set; }
    public Guid SigningEvidenceId { get; set; }
    public Guid SignedArtifactId { get; set; }

    [MaxLength(200)]
    public string OrganizationId { get; set; } = string.Empty;

    [MaxLength(12)]
    public string IssuerRuc { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime SummaryDate { get; set; }

    public int Sequence { get; set; }
    public int LocalRevision { get; set; }

    [MaxLength(120)]
    public string OperationId { get; set; } = string.Empty;

    [MaxLength(120)]
    public string CorrectionReasonCode { get; set; } = string.Empty;

    [MaxLength(64)]
    public string SourceAckXmlHash { get; set; } = string.Empty;

    public string SourceAckReasonsJson { get; set; } = string.Empty;

    [MaxLength(40)]
    public string FunctionalFormatVersion { get; set; } = string.Empty;

    [MaxLength(64)]
    public string ProjectionFingerprint { get; set; } = string.Empty;

    [MaxLength(64)]
    public string UnsignedContentHash { get; set; } = string.Empty;

    [MaxLength(64)]
    public string SignedContentHash { get; set; } = string.Empty;

    [Precision(0)]
    public DateTimeOffset SigningTimestamp { get; set; }

    public int SigningOffsetMinutes { get; set; }

    [MaxLength(120)]
    public string SignatureProfileId { get; set; } = string.Empty;

    [MaxLength(160)]
    public string CertificateThumbprint { get; set; } = string.Empty;

    [MaxLength(160)]
    public string CertificateSerialNumber { get; set; } = string.Empty;

    [MaxLength(120)]
    public string SchemaSetId { get; set; } = string.Empty;

    [MaxLength(40)]
    public string SchemaFunctionalFormatVersion { get; set; } = string.Empty;

    [MaxLength(40)]
    public string SchemaArchiveVersion { get; set; } = string.Empty;

    [MaxLength(64)]
    public string SchemaSetFingerprint { get; set; } = string.Empty;

    public string SignedXml { get; set; } = string.Empty;

    [Precision(0)]
    public DateTimeOffset CreatedAtUtc { get; set; }

    [MaxLength(64)]
    public string RevisionFingerprint { get; set; } = string.Empty;

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
    public string? AckReasonsJson { get; set; }

    [MaxLength(160)]
    public string? FailureCode { get; set; }

    [ForeignKey(nameof(RootSubmissionId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    [InverseProperty(nameof(V1FiscalDailyReportSubmissionRecord.BrCorrectionRevisions))]
    public V1FiscalDailyReportSubmissionRecord RootSubmission { get; set; } = null!;

    [ForeignKey(nameof(RootSignedArtifactId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public V1FiscalDailyReportSignedArtifactRecord RootSignedArtifact { get; set; } = null!;

    [ForeignKey(nameof(PreviousRevisionId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    public V1FiscalDailyReportBrCorrectionRevisionRecord? PreviousRevision { get; set; }

    [InverseProperty(nameof(V1FiscalDailyReportResponseConsultationRecord.BrCorrectionRevision))]
    public ICollection<V1FiscalDailyReportResponseConsultationRecord> ResponseConsultations { get; set; }
        = new List<V1FiscalDailyReportResponseConsultationRecord>();

    [InverseProperty(nameof(V1FiscalDailyReportReceiverDiscoveryRecord.BrCorrectionRevision))]
    public ICollection<V1FiscalDailyReportReceiverDiscoveryRecord> ReceiverDiscoveries { get; set; }
        = new List<V1FiscalDailyReportReceiverDiscoveryRecord>();
}

[Table("v1_fdr_response_consultations")]
[Index(nameof(OrganizationId), nameof(OperationId), IsUnique = true, Name = "UX_v1_fdr_cons_operation")]
[Index(nameof(OrganizationId), nameof(DgiReceiverId), Name = "IX_v1_fdr_cons_receiver")]
[Index(nameof(RootSubmissionId), Name = "IX_v1_fdr_cons_root")]
[Index(nameof(BrCorrectionRevisionId), Name = "IX_v1_fdr_cons_br")]
public sealed class V1FiscalDailyReportResponseConsultationRecord
{
    [Key]
    public Guid Id { get; set; }

    public Guid? RootSubmissionId { get; set; }
    public Guid? BrCorrectionRevisionId { get; set; }

    [MaxLength(200)]
    public string OrganizationId { get; set; } = string.Empty;

    [MaxLength(12)]
    public string IssuerRuc { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime SummaryDate { get; set; }

    public int Sequence { get; set; }
    public int? LocalRevision { get; set; }

    [MaxLength(120)]
    public string OperationId { get; set; } = string.Empty;

    [MaxLength(120)]
    public string DgiReceiverId { get; set; } = string.Empty;

    [MaxLength(8)]
    public string AckStateCode { get; set; } = string.Empty;

    public string AckXml { get; set; } = string.Empty;

    [MaxLength(64)]
    public string AckXmlHash { get; set; } = string.Empty;

    public int Consistency { get; set; }

    [Precision(0)]
    public DateTimeOffset ConsultedAtUtc { get; set; }

    [ForeignKey(nameof(RootSubmissionId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    [InverseProperty(nameof(V1FiscalDailyReportSubmissionRecord.ResponseConsultations))]
    public V1FiscalDailyReportSubmissionRecord? RootSubmission { get; set; }

    [ForeignKey(nameof(BrCorrectionRevisionId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    [InverseProperty(nameof(V1FiscalDailyReportBrCorrectionRevisionRecord.ResponseConsultations))]
    public V1FiscalDailyReportBrCorrectionRevisionRecord? BrCorrectionRevision { get; set; }
}

[Table("v1_fdr_receiver_discoveries")]
[Index(nameof(OrganizationId), nameof(OperationId), IsUnique = true, Name = "UX_v1_fdr_disc_operation")]
[Index(nameof(OrganizationId), nameof(DgiReceiverId), IsUnique = true, Name = "UX_v1_fdr_disc_receiver")]
[Index(nameof(RootSubmissionId), IsUnique = true, Name = "UX_v1_fdr_disc_root")]
[Index(nameof(BrCorrectionRevisionId), IsUnique = true, Name = "UX_v1_fdr_disc_br")]
[Index(nameof(OrganizationId), nameof(IssuerRuc), nameof(SummaryDate), nameof(Sequence), Name = "IX_v1_fdr_disc_identity")]
public sealed class V1FiscalDailyReportReceiverDiscoveryRecord
{
    [Key]
    public Guid Id { get; set; }

    public Guid? RootSubmissionId { get; set; }
    public Guid? BrCorrectionRevisionId { get; set; }

    [MaxLength(200)]
    public string OrganizationId { get; set; } = string.Empty;

    [MaxLength(12)]
    public string IssuerRuc { get; set; } = string.Empty;

    [Column(TypeName = "date")]
    public DateTime SummaryDate { get; set; }

    public int Sequence { get; set; }
    public int? LocalRevision { get; set; }

    [MaxLength(120)]
    public string OperationId { get; set; } = string.Empty;

    [MaxLength(120)]
    public string DgiEmitterId { get; set; } = string.Empty;

    [MaxLength(120)]
    public string DgiReceiverId { get; set; } = string.Empty;

    [MaxLength(8)]
    public string DgiStateCode { get; set; } = string.Empty;

    [MaxLength(80)]
    public string DgiReceptionTimestampText { get; set; } = string.Empty;

    public string EvidenceXml { get; set; } = string.Empty;

    [MaxLength(64)]
    public string EvidenceXmlHash { get; set; } = string.Empty;

    [Precision(0)]
    public DateTimeOffset DiscoveredAtUtc { get; set; }

    [ForeignKey(nameof(RootSubmissionId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    [InverseProperty(nameof(V1FiscalDailyReportSubmissionRecord.ReceiverDiscoveries))]
    public V1FiscalDailyReportSubmissionRecord? RootSubmission { get; set; }

    [ForeignKey(nameof(BrCorrectionRevisionId))]
    [DeleteBehavior(DeleteBehavior.Restrict)]
    [InverseProperty(nameof(V1FiscalDailyReportBrCorrectionRevisionRecord.ReceiverDiscoveries))]
    public V1FiscalDailyReportBrCorrectionRevisionRecord? BrCorrectionRevision { get; set; }
}
