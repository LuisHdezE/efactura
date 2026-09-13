using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Models;

[Table("v1_fiscal_cfe_state_consultations")]
[Index(nameof(OrganizationId), nameof(OperationId), IsUnique = true, Name = "UX_v1_fcsc_operation")]
[Index(nameof(FiscalDocumentId), Name = "IX_v1_fcsc_document")]
public sealed class V1FiscalCfeStateConsultationRecord
{
    [Key] public Guid Id { get; set; }
    public Guid FiscalDocumentId { get; set; }
    [MaxLength(200)] public string OrganizationId { get; set; } = string.Empty;
    public int CfeType { get; set; }
    [MaxLength(20)] public string Series { get; set; } = string.Empty;
    public long Number { get; set; }
    [MaxLength(120)] public string OperationId { get; set; } = string.Empty;
    [MaxLength(40)] public string StateCode { get; set; } = string.Empty;
    [MaxLength(120)] public string DgiSenderId { get; set; } = string.Empty;
    [MaxLength(120)] public string DgiReceiverId { get; set; } = string.Empty;
    public string? ConsultationToken { get; set; }
    [MaxLength(80)] public string? ConsultationAvailableAtText { get; set; }
    public string ResponseXml { get; set; } = string.Empty;
    [MaxLength(64)] public string ResponseSha256 { get; set; } = string.Empty;
    [Precision(0)] public DateTimeOffset ConsultedAtUtc { get; set; }
}
