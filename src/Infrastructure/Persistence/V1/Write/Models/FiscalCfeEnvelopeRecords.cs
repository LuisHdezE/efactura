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
