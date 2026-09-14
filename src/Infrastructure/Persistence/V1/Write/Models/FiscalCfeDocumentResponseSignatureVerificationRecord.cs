using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Models;

[Table("v1_fiscal_cfe_document_response_signature_verifications")]
[Index(nameof(ConsultationId), IsUnique = true, Name = "UX_v1_fcdrsv_consultation")]
[Index(nameof(AckObservationId), Name = "IX_v1_fcdrsv_ack_observation")]
[Index(nameof(SubmissionId), Name = "IX_v1_fcdrsv_submission")]
[Index(nameof(EnvelopeId), Name = "IX_v1_fcdrsv_envelope")]
public sealed class V1FiscalCfeDocumentResponseSignatureVerificationRecord
{
    [Key] public Guid Id { get; set; }
    public Guid ConsultationId { get; set; }
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
