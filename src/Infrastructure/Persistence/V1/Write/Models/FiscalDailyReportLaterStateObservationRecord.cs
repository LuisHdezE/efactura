namespace Infrastructure.Persistence.V1.Write.Models;

public sealed class V1FiscalDailyReportLaterStateObservationRecord
{
    public Guid Id { get; set; }
    public Guid? RootSubmissionId { get; set; }
    public Guid? BrCorrectionRevisionId { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string IssuerRuc { get; set; } = string.Empty;
    public DateTime SummaryDate { get; set; }
    public int Sequence { get; set; }
    public int? LocalRevision { get; set; }
    public string OperationId { get; set; } = string.Empty;
    public string DgiEmitterId { get; set; } = string.Empty;
    public string DgiReceiverId { get; set; } = string.Empty;
    public int State { get; set; }
    public string DgiStateCode { get; set; } = string.Empty;
    public string DgiReceptionTimestampText { get; set; } = string.Empty;
    public string EvidenceXml { get; set; } = string.Empty;
    public string EvidenceXmlHash { get; set; } = string.Empty;
    public DateTimeOffset ObservedAtUtc { get; set; }
}
