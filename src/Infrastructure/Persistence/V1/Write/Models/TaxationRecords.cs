namespace Infrastructure.Persistence.V1.Write.Models;

public sealed class V1TaxProfileRecord
{
    public Guid Id { get; set; }
    public string? OrganizationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Treatment { get; set; }
    public decimal? RatePercent { get; set; }
    public int CfeBillingIndicator { get; set; }
    public DateTime EffectiveFromUtc { get; set; }
    public DateTime? EffectiveToUtc { get; set; }
    public string RuleVersion { get; set; } = string.Empty;
    public string SourceAuthority { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public string SourceUri { get; set; } = string.Empty;
    public string CfeSpecificationVersion { get; set; } = string.Empty;
    public DateTime VerifiedAtUtc { get; set; }
    public bool Active { get; set; }
    public long Version { get; set; }
}
