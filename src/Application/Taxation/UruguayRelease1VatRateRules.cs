using EFactura.Application.Common.Errors;
using EFactura.Domain.Taxation;

namespace EFactura.Application.Taxation;

public sealed record VatRateRulePack(
    string Version,
    DateOnly SupportedFrom,
    VatRateRule Basic,
    VatRateRule Minimum);

public interface IVatRateRulePackProvider
{
    Task<VatRateRulePack> GetAsync(
        DateOnly effectiveOn,
        CancellationToken cancellationToken = default);
}

public static class UruguayRelease1VatRateCatalog
{
    public const string PackVersion = "UY-IVA-RATE-R1-2026.08.29";
    public const string BasicTreatmentCode = "VAT_BASIC";
    public const string MinimumTreatmentCode = "VAT_MINIMUM";
    public const string ExemptTreatmentCode = "VAT_EXEMPT";
    public static readonly DateOnly SupportedFrom = new(2024, 5, 16);

    public static readonly VatRateRule Basic = new(
        "UY-IVA-T10-ART34-BASIC-22",
        VatRateKind.Basic,
        22m,
        new RegulatoryRuleEvidence(
            "UY-IVA-T10-ART34-BASIC-22",
            "IMPO - T.O. 2023 Título 10",
            "https://www.impo.com.uy/bases/todgi2023/101-2024/34_T10",
            "reviewed 2026-08-29",
            SupportedFrom,
            clause: "Artículo 34 literal A - tasa básica 22%"));

    public static readonly VatRateRule Minimum = new(
        "UY-IVA-T10-ART34-MINIMUM-10",
        VatRateKind.Minimum,
        10m,
        new RegulatoryRuleEvidence(
            "UY-IVA-T10-ART34-MINIMUM-10",
            "IMPO - T.O. 2023 Título 10",
            "https://www.impo.com.uy/bases/todgi2023/101-2024/34_T10",
            "reviewed 2026-08-29",
            SupportedFrom,
            clause: "Artículo 34 literal B - tasa mínima 10%"));
}

public sealed class UruguayRelease1VatRateRulePackProvider : IVatRateRulePackProvider
{
    private static readonly VatRateRulePack Pack = new(
        UruguayRelease1VatRateCatalog.PackVersion,
        UruguayRelease1VatRateCatalog.SupportedFrom,
        UruguayRelease1VatRateCatalog.Basic,
        UruguayRelease1VatRateCatalog.Minimum);

    public Task<VatRateRulePack> GetAsync(
        DateOnly effectiveOn,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (effectiveOn < Pack.SupportedFrom)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                "tax.rate_rule_pack_date_unsupported",
                $"The Release-1 VAT rate rule pack supports operations on or after {Pack.SupportedFrom:yyyy-MM-dd}. Historical dates require an explicitly verified historical rate pack.");
        }

        return Task.FromResult(Pack);
    }
}

public sealed record ResolveTaxRateRequest(
    string OrganizationId,
    DateOnly EffectiveOn,
    TaxTreatmentDecision TaxTreatment,
    Guid? TaxProfileId);

public sealed class ResolveTaxRateUseCase
{
    private readonly ITaxProfileRepository _profiles;
    private readonly IVatRateRulePackProvider _rateRules;

    public ResolveTaxRateUseCase(
        ITaxProfileRepository profiles,
        IVatRateRulePackProvider rateRules)
    {
        _profiles = profiles;
        _rateRules = rateRules;
    }

    public async Task<TaxRateResolution> ExecuteAsync(
        ResolveTaxRateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.TaxTreatment);

        if (string.IsNullOrWhiteSpace(request.OrganizationId))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                "tax.organization_required",
                "Organization is required for VAT rate resolution.");
        }

        var pack = await _rateRules.GetAsync(request.EffectiveOn, cancellationToken);

        if (request.TaxTreatment.Status == TaxDecisionStatus.RequiresReview)
        {
            return Review(
                pack,
                request.TaxTreatment.TreatmentCode,
                new[] { "tax.rate.tax_treatment_requires_review" },
                request.TaxTreatment.MissingFacts,
                request.TaxTreatment.RuleEvidence);
        }

        return request.TaxTreatment.Classification switch
        {
            TaxTreatmentClassification.ExportGoods => NoVatDue(
                pack,
                VatRateKind.Export,
                "EXPORT_GOODS",
                "tax.rate.no_vat_due_export_goods",
                request.TaxTreatment.RuleEvidence),

            TaxTreatmentClassification.ExportServices => NoVatDue(
                pack,
                VatRateKind.Export,
                "EXPORT_SERVICES",
                "tax.rate.no_vat_due_export_services",
                request.TaxTreatment.RuleEvidence),

            TaxTreatmentClassification.OutsideVatTerritorialScope => NoVatDue(
                pack,
                VatRateKind.OutsideTerritorialScope,
                "OUTSIDE_VAT_SCOPE",
                "tax.rate.no_vat_due_outside_territorial_scope",
                request.TaxTreatment.RuleEvidence),

            TaxTreatmentClassification.Domestic => await ResolveDomesticAsync(request, pack, cancellationToken),

            _ => Review(
                pack,
                request.TaxTreatment.TreatmentCode,
                new[] { "tax.rate.tax_treatment_unsupported" },
                new[] { "supported_tax_treatment" },
                request.TaxTreatment.RuleEvidence)
        };
    }

    private async Task<TaxRateResolution> ResolveDomesticAsync(
        ResolveTaxRateRequest request,
        VatRateRulePack pack,
        CancellationToken cancellationToken)
    {
        if (!request.TaxProfileId.HasValue)
        {
            return Review(
                pack,
                "DOMESTIC",
                new[] { "tax.rate.domestic_profile_required" },
                new[] { "tax_profile_id" },
                request.TaxTreatment.RuleEvidence);
        }

        var profile = await _profiles.GetAsync(
            request.OrganizationId,
            request.TaxProfileId.Value,
            cancellationToken);

        if (profile is null)
        {
            return Review(
                pack,
                "DOMESTIC",
                new[] { "tax.rate.profile_not_found" },
                new[] { "valid_tax_profile" },
                request.TaxTreatment.RuleEvidence);
        }

        var profileEvidence = ToEvidence(profile);
        var evidence = request.TaxTreatment.RuleEvidence.Append(profileEvidence).ToArray();

        if (!profile.IsEffectiveOn(request.EffectiveOn))
        {
            return Review(
                pack,
                profile.TreatmentCode,
                new[] { "tax.rate.profile_not_effective" },
                new[] { "effective_tax_profile" },
                evidence,
                profile);
        }

        return profile.TreatmentCode switch
        {
            UruguayRelease1VatRateCatalog.BasicTreatmentCode => ResolveTaxedProfile(
                pack,
                profile,
                UruguayRelease1VatRateCatalog.Basic,
                evidence),

            UruguayRelease1VatRateCatalog.MinimumTreatmentCode => ResolveTaxedProfile(
                pack,
                profile,
                UruguayRelease1VatRateCatalog.Minimum,
                evidence),

            UruguayRelease1VatRateCatalog.ExemptTreatmentCode => Review(
                pack,
                profile.TreatmentCode,
                new[] { "tax.rate.exemption_rule_not_supported_release1" },
                new[] { "specific_effective_exemption_rule" },
                evidence,
                profile,
                VatRateKind.Exempt),

            _ => Review(
                pack,
                profile.TreatmentCode,
                new[] { "tax.rate.profile_treatment_not_supported_release1" },
                new[] { "supported_tax_profile_treatment" },
                evidence,
                profile)
        };
    }

    private static TaxRateResolution ResolveTaxedProfile(
        VatRateRulePack pack,
        TaxProfile profile,
        VatRateRule rule,
        IReadOnlyCollection<RegulatoryRuleEvidence> evidence)
    {
        if (!rule.Covers(profile.EffectiveFrom))
        {
            return Review(
                pack,
                profile.TreatmentCode,
                new[] { "tax.rate.rule_not_effective_for_profile" },
                new[] { "rate_rule_effective_date" },
                evidence,
                profile,
                rule.Kind);
        }

        if (profile.RatePercent != rule.RatePercent)
        {
            return Review(
                pack,
                profile.TreatmentCode,
                new[] { "tax.rate.profile_rate_mismatch" },
                new[] { "profile_rate_matching_authoritative_rule" },
                evidence.Append(rule.Evidence).ToArray(),
                profile,
                rule.Kind);
        }

        return new TaxRateResolution(
            TaxRateResolutionStatus.Resolved,
            VatLiabilityKind.VatDue,
            rule.Kind,
            rule.RatePercent,
            profile.Id,
            profile.Code,
            profile.TreatmentCode,
            new[] { "tax.rate.authoritative_profile_and_rate_match" },
            Array.Empty<string>(),
            evidence.Append(rule.Evidence).ToArray(),
            pack.Version);
    }

    private static TaxRateResolution NoVatDue(
        VatRateRulePack pack,
        VatRateKind kind,
        string treatmentCode,
        string reason,
        IReadOnlyCollection<RegulatoryRuleEvidence> evidence) =>
        new(
            TaxRateResolutionStatus.Resolved,
            VatLiabilityKind.NoVatDue,
            kind,
            0m,
            null,
            null,
            treatmentCode,
            new[] { reason, "tax.rate.zero_is_computational_not_zero_rate_vat" },
            Array.Empty<string>(),
            evidence,
            pack.Version);

    private static TaxRateResolution Review(
        VatRateRulePack pack,
        string treatmentCode,
        IReadOnlyCollection<string> reasons,
        IReadOnlyCollection<string> missingFacts,
        IReadOnlyCollection<RegulatoryRuleEvidence> evidence,
        TaxProfile? profile = null,
        VatRateKind kind = VatRateKind.Unsupported) =>
        new(
            TaxRateResolutionStatus.RequiresReview,
            VatLiabilityKind.RequiresReview,
            kind,
            null,
            profile?.Id,
            profile?.Code,
            treatmentCode,
            reasons,
            missingFacts,
            evidence,
            pack.Version);

    private static RegulatoryRuleEvidence ToEvidence(TaxProfile profile) =>
        new(
            $"TAX-PROFILE-{profile.Id:N}",
            profile.SourceName,
            profile.SourceReference,
            profile.SourceVersion,
            profile.EffectiveFrom,
            profile.EffectiveTo,
            profile.Code);
}
