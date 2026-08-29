using EFactura.Domain.Common;

namespace EFactura.Domain.Taxation;

public sealed class TaxProfile
{
    private TaxProfile(
        Guid id,
        string organizationId,
        string code,
        string name,
        string treatmentCode,
        decimal ratePercent,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        string sourceName,
        string sourceReference,
        string sourceVersion,
        bool active,
        long version)
    {
        Id = id;
        OrganizationId = Required(organizationId, 200, "tax.organization_required");
        Code = Required(code, 80, "tax.profile.code_required").ToUpperInvariant();
        Name = Required(name, 250, "tax.profile.name_required");
        TreatmentCode = Required(treatmentCode, 80, "tax.profile.treatment_required").ToUpperInvariant();
        RatePercent = ratePercent;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        SourceName = Required(sourceName, 250, "tax.profile.source_name_required");
        SourceReference = Required(sourceReference, 1000, "tax.profile.source_reference_required");
        SourceVersion = Required(sourceVersion, 120, "tax.profile.source_version_required");
        Active = active;
        Version = version;
        Validate();
    }

    public Guid Id { get; }
    public string OrganizationId { get; }
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string TreatmentCode { get; private set; }
    public decimal RatePercent { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public string SourceName { get; private set; }
    public string SourceReference { get; private set; }
    public string SourceVersion { get; private set; }
    public bool Active { get; private set; }
    public long Version { get; private set; }

    public static TaxProfile Create(
        Guid id,
        string organizationId,
        string code,
        string name,
        string treatmentCode,
        decimal ratePercent,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        string sourceName,
        string sourceReference,
        string sourceVersion) =>
        new(id, organizationId, code, name, treatmentCode, ratePercent, effectiveFrom, effectiveTo,
            sourceName, sourceReference, sourceVersion, true, 1);

    public static TaxProfile Rehydrate(
        Guid id,
        string organizationId,
        string code,
        string name,
        string treatmentCode,
        decimal ratePercent,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        string sourceName,
        string sourceReference,
        string sourceVersion,
        bool active,
        long version) =>
        new(id, organizationId, code, name, treatmentCode, ratePercent, effectiveFrom, effectiveTo,
            sourceName, sourceReference, sourceVersion, active, version);

    public bool IsEffectiveOn(DateOnly date) =>
        Active && EffectiveFrom <= date && (!EffectiveTo.HasValue || date <= EffectiveTo.Value);

    private void Validate()
    {
        if (RatePercent < 0m || RatePercent > 100m)
        {
            throw new DomainRuleException("tax.profile.invalid_rate", "Tax profile rate must be between 0 and 100 percent.");
        }

        if (EffectiveTo.HasValue && EffectiveTo.Value < EffectiveFrom)
        {
            throw new DomainRuleException("tax.profile.invalid_effective_range", "Tax profile effectiveTo cannot be earlier than effectiveFrom.");
        }
    }

    private static string Required(string value, int maxLength, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException(code, "Required tax profile value is missing.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainRuleException(code, $"Tax profile value cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
