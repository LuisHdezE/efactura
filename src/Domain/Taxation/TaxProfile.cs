using EFactura.Domain.Common;

namespace EFactura.Domain.Taxation;

public enum TaxTreatmentKind
{
    Exempt = 1,
    VatMinimum = 2,
    VatBasic = 3,
    VatOther = 4,
    ExportOrAssimilated = 10,
    VatSuspended = 12
}

public enum TaxJurisdictionKind
{
    DomesticUruguay = 1,
    ExportGoods = 2,
    ExportServices = 3
}

public enum ExportServiceQualification
{
    NotApplicable = 0,
    Unknown = 1,
    Qualifies = 2,
    DoesNotQualify = 3
}

public enum TaxDecisionStatus
{
    Resolved = 1,
    RequiresRuleQualification = 2,
    Unsupported = 3
}

public sealed record TaxRuleReference(
    string RuleId,
    string Version,
    string Authority,
    string SourceReference,
    string SourceUri);

public sealed record TaxDecision(
    TaxDecisionStatus Status,
    TaxJurisdictionKind Jurisdiction,
    TaxTreatmentKind? Treatment,
    decimal? RatePercent,
    int? CfeBillingIndicator,
    Guid? TaxProfileId,
    string? TaxProfileCode,
    string? TaxProfileRuleVersion,
    IReadOnlyCollection<TaxRuleReference> RuleReferences,
    IReadOnlyCollection<string> MissingFacts,
    IReadOnlyCollection<string> Warnings);

public sealed record TaxResolutionContext(
    DateOnly TransactionDate,
    TaxJurisdictionKind Jurisdiction,
    ExportServiceQualification ExportServiceQualification = ExportServiceQualification.NotApplicable,
    TaxRuleReference? ExportServiceQualificationRule = null);

public sealed class TaxProfile
{
    private TaxProfile(
        Guid id,
        string? organizationId,
        string code,
        string name,
        TaxTreatmentKind treatment,
        decimal? ratePercent,
        int cfeBillingIndicator,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        string ruleVersion,
        string sourceAuthority,
        string sourceReference,
        string sourceUri,
        string cfeSpecificationVersion,
        DateTimeOffset verifiedAt,
        bool active,
        long version)
    {
        Id = id;
        OrganizationId = NormalizeOptional(organizationId, 200);
        Code = NormalizeRequired(code, 80, "tax.profile.code_required").ToUpperInvariant();
        Name = NormalizeRequired(name, 200, "tax.profile.name_required");
        Treatment = treatment;
        RatePercent = ratePercent;
        CfeBillingIndicator = cfeBillingIndicator;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        RuleVersion = NormalizeRequired(ruleVersion, 80, "tax.profile.rule_version_required");
        SourceAuthority = NormalizeRequired(sourceAuthority, 120, "tax.profile.source_authority_required");
        SourceReference = NormalizeRequired(sourceReference, 500, "tax.profile.source_reference_required");
        SourceUri = NormalizeRequired(sourceUri, 1000, "tax.profile.source_uri_required");
        CfeSpecificationVersion = NormalizeRequired(cfeSpecificationVersion, 40, "tax.profile.cfe_spec_required");
        VerifiedAt = verifiedAt;
        Active = active;
        Version = version;

        ValidateValidity();
        ValidateTreatmentShape();
    }

    public Guid Id { get; }
    public string? OrganizationId { get; }
    public string Code { get; }
    public string Name { get; }
    public TaxTreatmentKind Treatment { get; }
    public decimal? RatePercent { get; }
    public int CfeBillingIndicator { get; }
    public DateOnly EffectiveFrom { get; }
    public DateOnly? EffectiveTo { get; }
    public string RuleVersion { get; }
    public string SourceAuthority { get; }
    public string SourceReference { get; }
    public string SourceUri { get; }
    public string CfeSpecificationVersion { get; }
    public DateTimeOffset VerifiedAt { get; }
    public bool Active { get; }
    public long Version { get; }

    public bool IsSystemProfile => OrganizationId is null;

    public bool IsEffectiveOn(DateOnly date) =>
        Active && date >= EffectiveFrom && (!EffectiveTo.HasValue || date <= EffectiveTo.Value);

    public bool IsUsableBy(string organizationId, DateOnly date) =>
        IsEffectiveOn(date) && (IsSystemProfile || string.Equals(OrganizationId, organizationId, StringComparison.Ordinal));

    public TaxRuleReference ToRuleReference() =>
        new(
            $"tax-profile:{Code}",
            RuleVersion,
            SourceAuthority,
            SourceReference,
            SourceUri);

    public static TaxProfile Create(
        Guid id,
        string? organizationId,
        string code,
        string name,
        TaxTreatmentKind treatment,
        decimal? ratePercent,
        int cfeBillingIndicator,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        string ruleVersion,
        string sourceAuthority,
        string sourceReference,
        string sourceUri,
        string cfeSpecificationVersion,
        DateTimeOffset verifiedAt) =>
        new(
            id,
            organizationId,
            code,
            name,
            treatment,
            ratePercent,
            cfeBillingIndicator,
            effectiveFrom,
            effectiveTo,
            ruleVersion,
            sourceAuthority,
            sourceReference,
            sourceUri,
            cfeSpecificationVersion,
            verifiedAt,
            true,
            1);

    public static TaxProfile Rehydrate(
        Guid id,
        string? organizationId,
        string code,
        string name,
        TaxTreatmentKind treatment,
        decimal? ratePercent,
        int cfeBillingIndicator,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo,
        string ruleVersion,
        string sourceAuthority,
        string sourceReference,
        string sourceUri,
        string cfeSpecificationVersion,
        DateTimeOffset verifiedAt,
        bool active,
        long version) =>
        new(
            id,
            organizationId,
            code,
            name,
            treatment,
            ratePercent,
            cfeBillingIndicator,
            effectiveFrom,
            effectiveTo,
            ruleVersion,
            sourceAuthority,
            sourceReference,
            sourceUri,
            cfeSpecificationVersion,
            verifiedAt,
            active,
            version);

    private void ValidateValidity()
    {
        if (EffectiveTo.HasValue && EffectiveTo.Value < EffectiveFrom)
        {
            throw new DomainRuleException(
                "tax.profile.invalid_effective_range",
                "Tax profile effectiveTo cannot be earlier than effectiveFrom.");
        }
    }

    private void ValidateTreatmentShape()
    {
        var expectedIndicator = Treatment switch
        {
            TaxTreatmentKind.Exempt => 1,
            TaxTreatmentKind.VatMinimum => 2,
            TaxTreatmentKind.VatBasic => 3,
            TaxTreatmentKind.VatOther => 4,
            TaxTreatmentKind.ExportOrAssimilated => 10,
            TaxTreatmentKind.VatSuspended => 12,
            _ => throw new DomainRuleException("tax.profile.unsupported_treatment", "Unsupported tax treatment.")
        };

        if (CfeBillingIndicator != expectedIndicator)
        {
            throw new DomainRuleException(
                "tax.profile.cfe_indicator_mismatch",
                "The CFE billing indicator does not match the tax treatment.");
        }

        var requiresRate = Treatment is TaxTreatmentKind.VatMinimum or TaxTreatmentKind.VatBasic or TaxTreatmentKind.VatOther;
        if (requiresRate && (!RatePercent.HasValue || RatePercent.Value <= 0m || RatePercent.Value > 100m))
        {
            throw new DomainRuleException(
                "tax.profile.rate_required",
                "The selected VAT treatment requires a positive percentage rate.");
        }

        if (!requiresRate && RatePercent.HasValue)
        {
            throw new DomainRuleException(
                "tax.profile.rate_not_applicable",
                "This tax treatment must not be represented as a percentage VAT rate.");
        }
    }

    private static string NormalizeRequired(string value, int maxLength, string code)
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

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainRuleException("tax.profile.organization_too_long", $"Organization scope cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}

public sealed class TaxTreatmentResolver
{
    public TaxDecision Resolve(TaxProfile profile, TaxResolutionContext context)
    {
        if (!profile.IsEffectiveOn(context.TransactionDate))
        {
            return new TaxDecision(
                TaxDecisionStatus.Unsupported,
                context.Jurisdiction,
                null,
                null,
                null,
                profile.Id,
                profile.Code,
                profile.RuleVersion,
                new[] { profile.ToRuleReference() },
                Array.Empty<string>(),
                new[] { "tax_profile_not_effective_on_transaction_date" });
        }

        if (context.Jurisdiction == TaxJurisdictionKind.DomesticUruguay)
        {
            return FromProfile(profile, context.Jurisdiction);
        }

        if (context.Jurisdiction == TaxJurisdictionKind.ExportGoods)
        {
            return new TaxDecision(
                TaxDecisionStatus.RequiresRuleQualification,
                context.Jurisdiction,
                null,
                null,
                null,
                profile.Id,
                profile.Code,
                profile.RuleVersion,
                new[] { profile.ToRuleReference() },
                new[] { "exportGoodsApplicability" },
                new[] { "export_goods_rule_slice_pending" });
        }

        if (context.ExportServiceQualification == ExportServiceQualification.Unknown)
        {
            return new TaxDecision(
                TaxDecisionStatus.RequiresRuleQualification,
                context.Jurisdiction,
                null,
                null,
                null,
                profile.Id,
                profile.Code,
                profile.RuleVersion,
                new[] { profile.ToRuleReference() },
                new[] { "article34Qualification" },
                Array.Empty<string>());
        }

        if (context.ExportServiceQualification == ExportServiceQualification.Qualifies)
        {
            if (context.ExportServiceQualificationRule is null)
            {
                return new TaxDecision(
                    TaxDecisionStatus.RequiresRuleQualification,
                    context.Jurisdiction,
                    null,
                    null,
                    null,
                    profile.Id,
                    profile.Code,
                    profile.RuleVersion,
                    new[] { profile.ToRuleReference() },
                    new[] { "article34RuleReference" },
                    new[] { "export_service_cannot_be_qualified_without_rule_provenance" });
            }

            return new TaxDecision(
                TaxDecisionStatus.Resolved,
                context.Jurisdiction,
                TaxTreatmentKind.ExportOrAssimilated,
                null,
                10,
                profile.Id,
                profile.Code,
                profile.RuleVersion,
                new[] { profile.ToRuleReference(), context.ExportServiceQualificationRule },
                Array.Empty<string>(),
                Array.Empty<string>());
        }

        if (context.ExportServiceQualification == ExportServiceQualification.DoesNotQualify)
        {
            return FromProfile(profile, context.Jurisdiction);
        }

        return new TaxDecision(
            TaxDecisionStatus.RequiresRuleQualification,
            context.Jurisdiction,
            null,
            null,
            null,
            profile.Id,
            profile.Code,
            profile.RuleVersion,
            new[] { profile.ToRuleReference() },
            new[] { "article34Qualification" },
            Array.Empty<string>());
    }

    private static TaxDecision FromProfile(TaxProfile profile, TaxJurisdictionKind jurisdiction) =>
        new(
            TaxDecisionStatus.Resolved,
            jurisdiction,
            profile.Treatment,
            profile.RatePercent,
            profile.CfeBillingIndicator,
            profile.Id,
            profile.Code,
            profile.RuleVersion,
            new[] { profile.ToRuleReference() },
            Array.Empty<string>(),
            Array.Empty<string>());
}
