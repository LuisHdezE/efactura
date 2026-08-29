namespace EFactura.Domain.Taxation;

public enum TaxRateResolutionStatus
{
    Resolved = 1,
    RequiresReview = 2
}

public enum VatLiabilityKind
{
    VatDue = 1,
    NoVatDue = 2,
    RequiresReview = 3
}

public enum VatRateKind
{
    Basic = 1,
    Minimum = 2,
    Exempt = 3,
    Export = 4,
    OutsideTerritorialScope = 5,
    Unsupported = 6
}

public sealed class VatRateRule
{
    public VatRateRule(
        string code,
        VatRateKind kind,
        decimal ratePercent,
        RegulatoryRuleEvidence evidence)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("VAT rate rule code is required.", nameof(code));
        }

        if (ratePercent < 0m || ratePercent > 100m)
        {
            throw new ArgumentOutOfRangeException(nameof(ratePercent));
        }

        Code = code.Trim().ToUpperInvariant();
        Kind = kind;
        RatePercent = ratePercent;
        Evidence = evidence ?? throw new ArgumentNullException(nameof(evidence));
    }

    public string Code { get; }
    public VatRateKind Kind { get; }
    public decimal RatePercent { get; }
    public RegulatoryRuleEvidence Evidence { get; }

    public bool Covers(DateOnly date) => Evidence.Covers(date);
}

public sealed record TaxRateResolution(
    TaxRateResolutionStatus Status,
    VatLiabilityKind Liability,
    VatRateKind RateKind,
    decimal? AppliedRatePercent,
    Guid? TaxProfileId,
    string? TaxProfileCode,
    string TreatmentCode,
    IReadOnlyCollection<string> Reasons,
    IReadOnlyCollection<string> MissingFacts,
    IReadOnlyCollection<RegulatoryRuleEvidence> RuleEvidence,
    string RateRulePackVersion);
