namespace Infrastructure.Persistence.V1.Write.Models;

public sealed class V1TaxProfileRecord
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TreatmentCode { get; set; } = string.Empty;
    public decimal RatePercent { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public string SourceName { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public string SourceVersion { get; set; } = string.Empty;
    public bool Active { get; set; }
    public long Version { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
