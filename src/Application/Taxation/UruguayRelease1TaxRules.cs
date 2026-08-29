using EFactura.Application.Common.Errors;
using EFactura.Domain.Taxation;

namespace EFactura.Application.Taxation;

public enum ExportServiceRuleFamily
{
    Unknown = 0,
    Article34Numeral11 = 11,
    OtherArticle34 = 999
}

public enum Article34Numeral11ServiceKind
{
    AdvisoryOrTechnical = 1,
    CustomSoftware = 2,
    SoftwareLicense = 3,
    SoftwareRightsAssignment = 4
}

public enum RegulatoryFactStatus
{
    Unknown = 0,
    Confirmed = 1,
    NotMet = 2
}

public sealed record Article34Numeral11Facts(
    Article34Numeral11ServiceKind ServiceKind,
    RegulatoryFactStatus RecipientIsPersonAbroad,
    RegulatoryFactStatus ExclusiveUseAbroad,
    RegulatoryFactStatus ForeignEconomicRelation = RegulatoryFactStatus.Unknown,
    RegulatoryFactStatus RecipientInstalledInFreeZone = RegulatoryFactStatus.Unknown,
    RegulatoryFactStatus ProviderFromNonFreeNationalTerritory = RegulatoryFactStatus.Unknown);

public sealed record ExportServiceEvaluationContext(
    ExportServiceRuleFamily RuleFamily,
    Article34Numeral11Facts? Article34Numeral11 = null);

public sealed class UruguayRelease1TaxTreatmentRulePackProvider : ITaxTreatmentRulePackProvider
{
    public static readonly DateOnly SupportedFrom = new(2024, 5, 16);
    public const string PackVersion = "UY-IVA-R1-2026.08.29";

    private static readonly TaxTreatmentRulePack Pack = new(
        PackVersion,
        UruguayRelease1RegulatoryCatalog.Territoriality,
        UruguayRelease1RegulatoryCatalog.ExportGoods);

    public Task<TaxTreatmentRulePack> GetAsync(
        string organizationId,
        DateOnly effectiveOn,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(organizationId))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                "tax.organization_required",
                "Organization is required for tax rule-pack resolution.");
        }

        if (effectiveOn < SupportedFrom)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                "tax.rule_pack_date_unsupported",
                $"The Release-1 Uruguay tax rule pack supports operations on or after {SupportedFrom:yyyy-MM-dd}. Historical dates require an explicitly verified historical rule pack.");
        }

        return Task.FromResult(Pack);
    }
}

public sealed class Article34Numeral11ExportServiceEligibilityEvaluator : IExportServiceEligibilityEvaluator
{
    public Task<ExportServiceEligibilityEvaluation> EvaluateAsync(
        TaxTransactionFacts facts,
        ExportServiceEvaluationContext? context,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(facts);

        if (facts.OperationKind != TaxOperationKind.Services)
        {
            return Task.FromResult(ExportServiceEligibilityEvaluation.NotEvaluated());
        }

        if (context is null || context.RuleFamily == ExportServiceRuleFamily.Unknown)
        {
            return Task.FromResult(new ExportServiceEligibilityEvaluation(
                ExportServiceEligibilityStatus.InsufficientEvidence,
                reasons: new[] { "tax.reason.export_service_rule_family_missing" },
                missingEvidence: new[] { "export_service_rule_family" }));
        }

        if (context.RuleFamily != ExportServiceRuleFamily.Article34Numeral11)
        {
            return Task.FromResult(new ExportServiceEligibilityEvaluation(
                ExportServiceEligibilityStatus.UnsupportedScenario,
                reasons: new[] { "tax.reason.article34_rule_family_not_supported_in_release1" }));
        }

        if (context.Article34Numeral11 is null)
        {
            return Task.FromResult(new ExportServiceEligibilityEvaluation(
                ExportServiceEligibilityStatus.InsufficientEvidence,
                reasons: new[] { "tax.reason.article34_11_facts_missing" },
                missingEvidence: new[] { "article34_numeral11_facts" }));
        }

        return Task.FromResult(EvaluateNumeral11(context.Article34Numeral11));
    }

    private static ExportServiceEligibilityEvaluation EvaluateNumeral11(Article34Numeral11Facts facts) =>
        facts.ServiceKind switch
        {
            Article34Numeral11ServiceKind.AdvisoryOrTechnical => EvaluateLiteralA(facts),
            Article34Numeral11ServiceKind.CustomSoftware => EvaluateSoftwareLiteral(
                facts,
                UruguayRelease1RegulatoryCatalog.Article34_11_B,
                "tax.reason.article34_11_b_qualified"),
            Article34Numeral11ServiceKind.SoftwareLicense => EvaluateSoftwareLiteral(
                facts,
                UruguayRelease1RegulatoryCatalog.Article34_11_C,
                "tax.reason.article34_11_c_qualified"),
            Article34Numeral11ServiceKind.SoftwareRightsAssignment => EvaluateSoftwareLiteral(
                facts,
                UruguayRelease1RegulatoryCatalog.Article34_11_D,
                "tax.reason.article34_11_d_qualified"),
            _ => new ExportServiceEligibilityEvaluation(
                ExportServiceEligibilityStatus.UnsupportedScenario,
                reasons: new[] { "tax.reason.article34_11_service_kind_not_supported" })
        };

    private static ExportServiceEligibilityEvaluation EvaluateLiteralA(Article34Numeral11Facts facts)
    {
        var rule = UruguayRelease1RegulatoryCatalog.Article34_11_A;
        var missing = new List<string>();

        AddMissingIfUnknown(facts.RecipientIsPersonAbroad, "recipient_is_person_abroad", missing);
        AddMissingIfUnknown(facts.ForeignEconomicRelation, "foreign_activity_asset_or_right_relation", missing);
        AddMissingIfUnknown(facts.ExclusiveUseAbroad, "exclusive_use_abroad", missing);

        if (missing.Count > 0)
        {
            return Incomplete(rule, missing, "tax.reason.article34_11_a_evidence_incomplete");
        }

        if (facts.RecipientIsPersonAbroad == RegulatoryFactStatus.NotMet)
        {
            return NotQualified(rule, "tax.reason.article34_11_a_recipient_not_person_abroad");
        }

        if (facts.ForeignEconomicRelation == RegulatoryFactStatus.NotMet)
        {
            return NotQualified(rule, "tax.reason.article34_11_a_foreign_relation_not_met");
        }

        if (facts.ExclusiveUseAbroad == RegulatoryFactStatus.NotMet)
        {
            return NotQualified(rule, "tax.reason.article34_11_a_exclusive_use_abroad_not_met");
        }

        return Qualified(rule, "tax.reason.article34_11_a_qualified");
    }

    private static ExportServiceEligibilityEvaluation EvaluateSoftwareLiteral(
        Article34Numeral11Facts facts,
        RegulatoryRuleEvidence rule,
        string qualifiedReason)
    {
        if (facts.RecipientInstalledInFreeZone == RegulatoryFactStatus.Confirmed)
        {
            if (facts.ProviderFromNonFreeNationalTerritory == RegulatoryFactStatus.Confirmed)
            {
                return Qualified(rule, "tax.reason.article34_11_free_zone_path_qualified");
            }

            if (facts.ProviderFromNonFreeNationalTerritory == RegulatoryFactStatus.Unknown)
            {
                return Incomplete(
                    rule,
                    new[] { "provider_from_non_free_national_territory" },
                    "tax.reason.article34_11_free_zone_provider_origin_incomplete");
            }
        }

        var exteriorMissing = new List<string>();
        AddMissingIfUnknown(facts.RecipientIsPersonAbroad, "recipient_is_person_abroad", exteriorMissing);
        AddMissingIfUnknown(facts.ExclusiveUseAbroad, "exclusive_use_abroad", exteriorMissing);

        if (exteriorMissing.Count == 0
            && facts.RecipientIsPersonAbroad == RegulatoryFactStatus.Confirmed
            && facts.ExclusiveUseAbroad == RegulatoryFactStatus.Confirmed)
        {
            return Qualified(rule, qualifiedReason);
        }

        var exteriorExplicitlyFails =
            facts.RecipientIsPersonAbroad == RegulatoryFactStatus.NotMet
            || facts.ExclusiveUseAbroad == RegulatoryFactStatus.NotMet;

        var freeZoneExplicitlyFails =
            facts.RecipientInstalledInFreeZone == RegulatoryFactStatus.NotMet
            || (facts.RecipientInstalledInFreeZone == RegulatoryFactStatus.Confirmed
                && facts.ProviderFromNonFreeNationalTerritory == RegulatoryFactStatus.NotMet);

        if (exteriorExplicitlyFails && freeZoneExplicitlyFails)
        {
            return NotQualified(rule, "tax.reason.article34_11_software_paths_not_met");
        }

        var missing = new HashSet<string>(exteriorMissing, StringComparer.Ordinal);
        if (facts.RecipientInstalledInFreeZone == RegulatoryFactStatus.Unknown)
        {
            missing.Add("recipient_installed_in_free_zone");
        }
        else if (facts.RecipientInstalledInFreeZone == RegulatoryFactStatus.Confirmed
                 && facts.ProviderFromNonFreeNationalTerritory == RegulatoryFactStatus.Unknown)
        {
            missing.Add("provider_from_non_free_national_territory");
        }

        return Incomplete(
            rule,
            missing.Count == 0 ? new[] { "article34_numeral11_supporting_evidence" } : missing,
            "tax.reason.article34_11_software_evidence_incomplete");
    }

    private static void AddMissingIfUnknown(
        RegulatoryFactStatus status,
        string key,
        ICollection<string> missing)
    {
        if (status == RegulatoryFactStatus.Unknown)
        {
            missing.Add(key);
        }
    }

    private static ExportServiceEligibilityEvaluation Qualified(RegulatoryRuleEvidence rule, string reason) =>
        new(
            ExportServiceEligibilityStatus.Qualified,
            new[] { rule },
            new[] { reason });

    private static ExportServiceEligibilityEvaluation NotQualified(RegulatoryRuleEvidence rule, string reason) =>
        new(
            ExportServiceEligibilityStatus.NotQualified,
            new[] { rule },
            new[] { reason });

    private static ExportServiceEligibilityEvaluation Incomplete(
        RegulatoryRuleEvidence rule,
        IEnumerable<string> missing,
        string reason) =>
        new(
            ExportServiceEligibilityStatus.InsufficientEvidence,
            new[] { rule },
            new[] { reason },
            missing);
}

public static class UruguayRelease1RegulatoryCatalog
{
    private static readonly DateOnly SupportBoundary = UruguayRelease1TaxTreatmentRulePackProvider.SupportedFrom;

    public static readonly RegulatoryRuleEvidence Territoriality = new(
        "UY-IVA-T10-ART5-TERRITORIALITY",
        "IMPO - T.O. 2023 Título 10",
        "https://www.impo.com.uy/bases/todgi2023/101-2024/5_T10",
        "T.O. 2023 approved by Decreto 101/024; consolidated text reviewed 2026-08-29",
        SupportBoundary,
        clause: "Artículo 5 - Territorialidad");

    public static readonly RegulatoryRuleEvidence ExportGoods = new(
        "UY-IVA-T10-ART5-EXPORT-GOODS",
        "IMPO - T.O. 2023 Título 10",
        "https://www.impo.com.uy/bases/todgi2023/101-2024/5_T10",
        "T.O. 2023 approved by Decreto 101/024; consolidated text reviewed 2026-08-29",
        SupportBoundary,
        clause: "Artículo 5 - exportaciones de bienes no gravadas");

    public static readonly RegulatoryRuleEvidence Article34_11_A = Article34Literal(
        "UY-IVA-D220-ART34-11-A",
        "Artículo 34 numeral 11 literal a");

    public static readonly RegulatoryRuleEvidence Article34_11_B = Article34Literal(
        "UY-IVA-D220-ART34-11-B",
        "Artículo 34 numeral 11 literal b y párrafo de zona franca");

    public static readonly RegulatoryRuleEvidence Article34_11_C = Article34Literal(
        "UY-IVA-D220-ART34-11-C",
        "Artículo 34 numeral 11 literal c y párrafo de zona franca");

    public static readonly RegulatoryRuleEvidence Article34_11_D = Article34Literal(
        "UY-IVA-D220-ART34-11-D",
        "Artículo 34 numeral 11 literal d y párrafo de zona franca");

    private static RegulatoryRuleEvidence Article34Literal(string ruleId, string clause) =>
        new(
            ruleId,
            "IMPO - Decreto 220/998 actualizado",
            "https://www.impo.com.uy/bases/decretos/220-1998/34",
            "Artículo 34 current consolidated text reviewed 2026-08-29",
            SupportBoundary,
            clause: clause);
}
