using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Models;

[Table("v1_fiscal_cfe_document_response_certificate_trust_validations")]
[Index(nameof(OrganizationId), nameof(OperationId), IsUnique = true, Name = "UX_v1_fcdrctv_operation")]
[Index(nameof(SignatureVerificationId), Name = "IX_v1_fcdrctv_signature_verification")]
[Index(nameof(ConsultationId), Name = "IX_v1_fcdrctv_consultation")]
[Index(nameof(AckObservationId), Name = "IX_v1_fcdrctv_ack_observation")]
[Index(nameof(SubmissionId), Name = "IX_v1_fcdrctv_submission")]
[Index(nameof(EnvelopeId), Name = "IX_v1_fcdrctv_envelope")]
public sealed class V1FiscalCfeDocumentResponseCertificateTrustValidationRecord
{
    [Key] public Guid Id { get; set; }
    public Guid SignatureVerificationId { get; set; }
    public Guid ConsultationId { get; set; }
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
