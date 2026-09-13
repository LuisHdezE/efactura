using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Models;

[Table("v1_fiscal_cfe_envelopes")]
[Index(nameof(OrganizationId), nameof(OperationId), IsUnique = true, Name = "UX_v1_fce_operation")]
[Index(nameof(OrganizationId), nameof(IssuerRuc), nameof(ReceiverRut), nameof(SenderEnvelopeId), IsUnique = true, Name = "UX_v1_fce_identity")]
public sealed class V1FiscalCfeEnvelopeRecord
{
    [Key] public Guid Id { get; set; }
    [MaxLength(200)] public string OrganizationId { get; set; } = string.Empty;
    [MaxLength(12)] public string ReceiverRut { get; set; } = string.Empty;
    [MaxLength(12)] public string IssuerRuc { get; set; } = string.Empty;
    public long SenderEnvelopeId { get; set; }
    [Precision(0)] public DateTimeOffset CreatedAtUtc { get; set; }
    public int CreatedAtOffsetMinutes { get; set; }
    public string FiscalDocumentIdsJson { get; set; } = string.Empty;
    [MaxLength(120)] public string OperationId { get; set; } = string.Empty;
    public int CfeCount { get; set; }
    [MaxLength(160)] public string CertificateThumbprint { get; set; } = string.Empty;
    [MaxLength(160)] public string CertificateSerialNumber { get; set; } = string.Empty;
    public string EnvelopeXml { get; set; } = string.Empty;
    [MaxLength(64)] public string EnvelopeSha256 { get; set; } = string.Empty;
    [MaxLength(120)] public string SchemaSetId { get; set; } = string.Empty;
    [MaxLength(40)] public string SchemaVersion { get; set; } = string.Empty;
    [MaxLength(64)] public string SchemaSetFingerprint { get; set; } = string.Empty;
}

[Table("v1_fiscal_cfe_envelope_submissions")]
[Index(nameof(OrganizationId), nameof(OperationId), IsUnique = true, Name = "UX_v1_fces_operation")]
[Index(nameof(EnvelopeId), IsUnique = true, Name = "UX_v1_fces_envelope")]
public sealed class V1FiscalCfeEnvelopeSubmissionRecord
{
    [Key] public Guid Id { get; set; }
    public Guid EnvelopeId { get; set; }
    [MaxLength(200)] public string OrganizationId { get; set; } = string.Empty;
    [MaxLength(12)] public string IssuerRuc { get; set; } = string.Empty;
    [MaxLength(12)] public string ReceiverRut { get; set; } = string.Empty;
    public long SenderEnvelopeId { get; set; }
    [MaxLength(120)] public string OperationId { get; set; } = string.Empty;
    [MaxLength(64)] public string EnvelopeSha256 { get; set; } = string.Empty;
    public int State { get; set; }
    public int AttemptCount { get; set; }
    [Precision(0)] public DateTimeOffset PreparedAtUtc { get; set; }
    [Precision(0)] public DateTimeOffset? LastAttemptAtUtc { get; set; }
    [Precision(0)] public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? ResponseXml { get; set; }
    [MaxLength(64)] public string? ResponseSha256 { get; set; }
    [MaxLength(160)] public string? FailureCode { get; set; }
}

[Table("v1_fiscal_cfe_envelope_ack_observations")]
[Index(nameof(SubmissionId), IsUnique = true, Name = "UX_v1_fceao_submission")]
public sealed class V1FiscalCfeEnvelopeAckObservationRecord
{
    [Key] public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid EnvelopeId { get; set; }
    [MaxLength(200)] public string OrganizationId { get; set; } = string.Empty;
    [MaxLength(12)] public string IssuerRuc { get; set; } = string.Empty;
    [MaxLength(12)] public string ReceiverRut { get; set; } = string.Empty;
    public long SenderEnvelopeId { get; set; }
    [MaxLength(64)] public string ResponseSha256 { get; set; } = string.Empty;
    public long DgiResponseId { get; set; }
    public long DgiReceiverId { get; set; }
    public int CfeCount { get; set; }
    public int State { get; set; }
    [MaxLength(80)] public string ReceptionTimestampText { get; set; } = string.Empty;
    [MaxLength(80)] public string SigningTimestampText { get; set; } = string.Empty;
    public string? ConsultationToken { get; set; }
    [MaxLength(80)] public string? ConsultationAvailableAtText { get; set; }
    public string RejectionReasonsJson { get; set; } = "[]";
    [Precision(0)] public DateTimeOffset ObservedAtUtc { get; set; }
}

[Table("v1_fiscal_cfe_envelope_ack_signature_verifications")]
[Index(nameof(AckObservationId), IsUnique = true, Name = "UX_v1_fceasv_ack_observation")]
public sealed class V1FiscalCfeEnvelopeAckSignatureVerificationRecord
{
    [Key] public Guid Id { get; set; }
    public Guid AckObservationId { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid EnvelopeId { get; set; }
    [MaxLength(200)] public string OrganizationId { get; set; } = string.Empty;
    [MaxLength(64)] public string ResponseSha256 { get; set; } = string.Empty;
    [MaxLength(120)] public string VerificationProfileId { get; set; } = string.Empty;
    [MaxLength(64)] public string CertificateSha256 { get; set; } = string.Empty;
    [MaxLength(160)] public string CertificateThumbprint { get; set; } = string.Empty;
    [MaxLength(160)] public string CertificateSerialNumber { get; set; } = string.Empty;
    [MaxLength(1024)] public string CertificateSubject { get; set; } = string.Empty;
    [MaxLength(1024)] public string CertificateIssuer { get; set; } = string.Empty;
    [MaxLength(300)] public string CanonicalizationMethod { get; set; } = string.Empty;
    [MaxLength(300)] public string SignatureMethod { get; set; } = string.Empty;
    [MaxLength(300)] public string DigestMethod { get; set; } = string.Empty;
    [MaxLength(512)] public string ReferenceUri { get; set; } = string.Empty;
    public string ReferenceTransformsJson { get; set; } = "[]";
    public bool CertificateTrustValidated { get; set; }
    [Precision(0)] public DateTimeOffset VerifiedAtUtc { get; set; }
}

[Table("v1_fiscal_cfe_envelope_ack_certificate_trust_validations")]
[Index(nameof(OrganizationId), nameof(OperationId), IsUnique = true, Name = "UX_v1_fceactv_operation")]
[Index(nameof(SignatureVerificationId), Name = "IX_v1_fceactv_signature_verification")]
public sealed class V1FiscalCfeEnvelopeAckCertificateTrustValidationRecord
{
    [Key] public Guid Id { get; set; }
    public Guid SignatureVerificationId { get; set; }
    public Guid AckObservationId { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid EnvelopeId { get; set; }
    [MaxLength(200)] public string OrganizationId { get; set; } = string.Empty;
    [MaxLength(120)] public string OperationId { get; set; } = string.Empty;
    [MaxLength(64)] public string ResponseSha256 { get; set; } = string.Empty;
    [MaxLength(120)] public string ValidationProfileId { get; set; } = string.Empty;
    [MaxLength(64)] public string CertificateSha256 { get; set; } = string.Empty;
    [MaxLength(64)] public string TrustedRootSha256 { get; set; } = string.Empty;
    public string ChainCertificateSha256Json { get; set; } = "[]";
    [MaxLength(40)] public string RevocationMode { get; set; } = string.Empty;
    public bool PkiUruguayTrustValidated { get; set; }
    public bool DgiIdentityValidated { get; set; }
    [Precision(0)] public DateTimeOffset ValidatedAtUtc { get; set; }
}
