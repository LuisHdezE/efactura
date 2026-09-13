using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Models;

[Table("v1_fiscal_cfe_document_response_consultations")]
[Index(nameof(OrganizationId), nameof(OperationId), IsUnique = true, Name = "UX_v1_fcdrc_operation")]
[Index(nameof(AckObservationId), Name = "IX_v1_fcdrc_ack_observation")]
public sealed class V1FiscalCfeDocumentResponseConsultationRecord
{
    [Key] public Guid Id { get; set; }
    public Guid AckObservationId { get; set; }
    public Guid SubmissionId { get; set; }
    public Guid EnvelopeId { get; set; }
    [MaxLength(200)] public string OrganizationId { get; set; } = string.Empty;
    [MaxLength(120)] public string OperationId { get; set; } = string.Empty;
    [MaxLength(64)] public string SourceAckResponseSha256 { get; set; } = string.Empty;
    public long DgiReceiverId { get; set; }
    [MaxLength(64)] public string ConsultationTokenSha256 { get; set; } = string.Empty;
    public long DgiResponseId { get; set; }
    [MaxLength(12)] public string IssuerRuc { get; set; } = string.Empty;
    [MaxLength(12)] public string ReceiverRut { get; set; } = string.Empty;
    public long SenderEnvelopeId { get; set; }
    public int EnvelopeCfeCount { get; set; }
    public int RespondedCount { get; set; }
    public int AcceptedCount { get; set; }
    public int RejectedCount { get; set; }
    public int ObservedCount { get; set; }
    public int OtherRejectedCount { get; set; }
    public string DetailsJson { get; set; } = "[]";
    public string ResponseXml { get; set; } = string.Empty;
    [MaxLength(64)] public string ResponseSha256 { get; set; } = string.Empty;
    [Precision(0)] public DateTimeOffset ConsultedAtUtc { get; set; }
}
