using EFactura.Domain.Taxation;

namespace EFactura.Domain.Fiscal;

public enum CfeFamily
{
    ETicket = 101,
    EFactura = 111,
    EFacturaExportacion = 121
}

public enum FiscalOperationIntent
{
    ConsumerFinal = 1,
    TaxpayerInvoice = 2,
    Export = 3
}

public enum CfeEligibilityStatus
{
    EligibleCandidateSet = 1,
    RequiresReview = 2,
    Ineligible = 3
}

public enum ReceiverIdentificationRequirement
{
    Optional = 1,
    Required = 2
}

public sealed record CfeCandidate(
    CfeFamily Family,
    ReceiverIdentificationRequirement ReceiverIdentification,
    IReadOnlyCollection<string> Reasons);

public sealed record CfeEligibilityResult(
    CfeEligibilityStatus Status,
    IReadOnlyCollection<CfeCandidate> Candidates,
    IReadOnlyCollection<string> Reasons,
    IReadOnlyCollection<string> MissingFacts,
    IReadOnlyCollection<RegulatoryRuleEvidence> RuleEvidence,
    string FormatVersion);

public sealed record CfeEligibilityFacts(
    DateOnly EffectiveOn,
    TaxTreatmentDecision TaxTreatment,
    ReceiverTaxFacts Receiver,
    FiscalOperationIntent OperationIntent,
    decimal? NetAmountUi,
    bool HasRetentionsOrPerceptions);

public sealed class CfeEligibilityRulePack
{
    public CfeEligibilityRulePack(
        string formatVersion,
        DateOnly supportedFrom,
        decimal eTicketIdentificationThresholdUi,
        bool exportServiceStrategyVerifiedCurrent,
        RegulatoryRuleEvidence formatRule,
        RegulatoryRuleEvidence receiverIdentityRule,
        RegulatoryRuleEvidence eTicketThresholdRule,
        RegulatoryRuleEvidence exportServiceStrategyRule)
    {
        if (string.IsNullOrWhiteSpace(formatVersion))
        {
            throw new ArgumentException("CFE format version is required.", nameof(formatVersion));
        }

        if (eTicketIdentificationThresholdUi <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(eTicketIdentificationThresholdUi));
        }

        FormatVersion = formatVersion.Trim();
        SupportedFrom = supportedFrom;
        ETicketIdentificationThresholdUi = eTicketIdentificationThresholdUi;
        ExportServiceStrategyVerifiedCurrent = exportServiceStrategyVerifiedCurrent;
        FormatRule = formatRule ?? throw new ArgumentNullException(nameof(formatRule));
        ReceiverIdentityRule = receiverIdentityRule ?? throw new ArgumentNullException(nameof(receiverIdentityRule));
        ETicketThresholdRule = eTicketThresholdRule ?? throw new ArgumentNullException(nameof(eTicketThresholdRule));
        ExportServiceStrategyRule = exportServiceStrategyRule ?? throw new ArgumentNullException(nameof(exportServiceStrategyRule));
    }

    public string FormatVersion { get; }
    public DateOnly SupportedFrom { get; }
    public decimal ETicketIdentificationThresholdUi { get; }
    public bool ExportServiceStrategyVerifiedCurrent { get; }
    public RegulatoryRuleEvidence FormatRule { get; }
    public RegulatoryRuleEvidence ReceiverIdentityRule { get; }
    public RegulatoryRuleEvidence ETicketThresholdRule { get; }
    public RegulatoryRuleEvidence ExportServiceStrategyRule { get; }
}

public sealed class CfeEligibilityPolicy
{
    public CfeEligibilityResult Prepare(CfeEligibilityFacts facts, CfeEligibilityRulePack rules)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(facts.TaxTreatment);
        ArgumentNullException.ThrowIfNull(facts.Receiver);
        ArgumentNullException.ThrowIfNull(rules);

        if (facts.EffectiveOn < rules.SupportedFrom)
        {
            return Review(
                rules,
                new[] { "fiscal.cfe_format_date_not_supported" },
                new[] { "historical_cfe_format_rule_pack" },
                new[] { rules.FormatRule });
        }

        if (facts.TaxTreatment.Status == TaxDecisionStatus.RequiresReview)
        {
            return Review(
                rules,
                new[] { "fiscal.tax_treatment_requires_review" },
                facts.TaxTreatment.MissingFacts,
                facts.TaxTreatment.RuleEvidence.Append(rules.FormatRule).ToArray());
        }

        return facts.TaxTreatment.Classification switch
        {
            TaxTreatmentClassification.Domestic => PrepareDomestic(facts, rules),
            TaxTreatmentClassification.ExportGoods => PrepareGoodsExport(facts, rules),
            TaxTreatmentClassification.ExportServices => PrepareServiceExport(facts, rules),
            TaxTreatmentClassification.OutsideVatTerritorialScope => Review(
                rules,
                new[] { "fiscal.outside_vat_scope_document_family_requires_specific_rule" },
                new[] { "document_applicability_rule" },
                facts.TaxTreatment.RuleEvidence.Append(rules.FormatRule).ToArray()),
            _ => Review(
                rules,
                new[] { "fiscal.tax_treatment_not_supported" },
                new[] { "supported_tax_treatment" },
                facts.TaxTreatment.RuleEvidence.Append(rules.FormatRule).ToArray())
        };
    }

    private static CfeEligibilityResult PrepareDomestic(
        CfeEligibilityFacts facts,
        CfeEligibilityRulePack rules)
    {
        if (facts.OperationIntent == FiscalOperationIntent.TaxpayerInvoice)
        {
            if (!facts.Receiver.HasUruguayanRuc)
            {
                return new CfeEligibilityResult(
                    CfeEligibilityStatus.Ineligible,
                    Array.Empty<CfeCandidate>(),
                    new[] { "fiscal.efactura_requires_uruguayan_ruc" },
                    new[] { "uruguayan_ruc" },
                    new[] { rules.FormatRule, rules.ReceiverIdentityRule },
                    rules.FormatVersion);
            }

            return Eligible(
                rules,
                new CfeCandidate(
                    CfeFamily.EFactura,
                    ReceiverIdentificationRequirement.Required,
                    new[] { "fiscal.efactura_ruc_receiver_eligible" }),
                new[] { rules.FormatRule, rules.ReceiverIdentityRule });
        }

        if (facts.OperationIntent != FiscalOperationIntent.ConsumerFinal)
        {
            return Review(
                rules,
                new[] { "fiscal.domestic_operation_intent_inconsistent" },
                new[] { "consumer_final_or_taxpayer_invoice_intent" },
                new[] { rules.FormatRule, rules.ReceiverIdentityRule });
        }

        if (!facts.NetAmountUi.HasValue)
        {
            return Review(
                rules,
                new[] { "fiscal.eticket_net_amount_ui_required_for_identification_rule" },
                new[] { "net_amount_ui" },
                new[] { rules.FormatRule, rules.ETicketThresholdRule });
        }

        var identificationRequired = facts.HasRetentionsOrPerceptions
            || facts.NetAmountUi.Value > rules.ETicketIdentificationThresholdUi;

        if (identificationRequired && !HasFormatCompatibleIdentity(facts.Receiver))
        {
            return Review(
                rules,
                new[] { "fiscal.eticket_receiver_identification_required_but_missing_or_incompatible" },
                new[] { "format_compatible_receiver_identity" },
                new[] { rules.FormatRule, rules.ReceiverIdentityRule, rules.ETicketThresholdRule });
        }

        return Eligible(
            rules,
            new CfeCandidate(
                CfeFamily.ETicket,
                identificationRequired
                    ? ReceiverIdentificationRequirement.Required
                    : ReceiverIdentificationRequirement.Optional,
                new[]
                {
                    identificationRequired
                        ? "fiscal.eticket_identification_required"
                        : "fiscal.eticket_identification_optional"
                }),
            new[] { rules.FormatRule, rules.ReceiverIdentityRule, rules.ETicketThresholdRule });
    }

    private static CfeEligibilityResult PrepareGoodsExport(
        CfeEligibilityFacts facts,
        CfeEligibilityRulePack rules)
    {
        if (facts.OperationIntent != FiscalOperationIntent.Export)
        {
            return Review(
                rules,
                new[] { "fiscal.export_goods_intent_mismatch" },
                new[] { "export_operation_intent" },
                facts.TaxTreatment.RuleEvidence.Append(rules.FormatRule).ToArray());
        }

        return Eligible(
            rules,
            new CfeCandidate(
                CfeFamily.EFacturaExportacion,
                ReceiverIdentificationRequirement.Required,
                new[] { "fiscal.export_goods_export_invoice_candidate" }),
            facts.TaxTreatment.RuleEvidence.Append(rules.FormatRule).ToArray());
    }

    private static CfeEligibilityResult PrepareServiceExport(
        CfeEligibilityFacts facts,
        CfeEligibilityRulePack rules)
    {
        var candidates = new List<CfeCandidate>
        {
            new(
                CfeFamily.EFacturaExportacion,
                ReceiverIdentificationRequirement.Required,
                new[] { "fiscal.export_service_export_combo_candidate" })
        };

        candidates.Add(facts.Receiver.HasUruguayanRuc
            ? new CfeCandidate(
                CfeFamily.EFactura,
                ReceiverIdentificationRequirement.Required,
                new[] { "fiscal.export_service_usual_cfe_ruc_candidate" })
            : new CfeCandidate(
                CfeFamily.ETicket,
                ReceiverIdentificationRequirement.Required,
                new[] { "fiscal.export_service_usual_cfe_no_ruc_candidate" }));

        if (!rules.ExportServiceStrategyVerifiedCurrent)
        {
            return new CfeEligibilityResult(
                CfeEligibilityStatus.RequiresReview,
                candidates,
                new[] { "fiscal.export_service_cfe_strategy_currentness_requires_revalidation" },
                new[] { "current_export_service_cfe_strategy_confirmation" },
                facts.TaxTreatment.RuleEvidence
                    .Append(rules.FormatRule)
                    .Append(rules.ReceiverIdentityRule)
                    .Append(rules.ExportServiceStrategyRule)
                    .ToArray(),
                rules.FormatVersion);
        }

        return new CfeEligibilityResult(
            CfeEligibilityStatus.EligibleCandidateSet,
            candidates,
            new[] { "fiscal.export_service_multiple_legal_candidates_require_configured_policy" },
            Array.Empty<string>(),
            facts.TaxTreatment.RuleEvidence
                .Append(rules.FormatRule)
                .Append(rules.ReceiverIdentityRule)
                .Append(rules.ExportServiceStrategyRule)
                .ToArray(),
            rules.FormatVersion);
    }

    private static bool HasFormatCompatibleIdentity(ReceiverTaxFacts receiver) =>
        receiver.FiscalIdentities.Any(IsFormatCompatibleIdentity);

    private static bool IsFormatCompatibleIdentity(ReceiverFiscalIdentityFact identity) =>
        identity.TypeCode switch
        {
            "1" or "2" or "3" => identity.IssuingCountry == "UY",
            "4" or "5" or "7" => true,
            "6" => identity.IssuingCountry is "AR" or "BR" or "CL" or "PY",
            _ => false
        };

    private static CfeEligibilityResult Eligible(
        CfeEligibilityRulePack rules,
        CfeCandidate candidate,
        IReadOnlyCollection<RegulatoryRuleEvidence> evidence) =>
        new(
            CfeEligibilityStatus.EligibleCandidateSet,
            new[] { candidate },
            candidate.Reasons,
            Array.Empty<string>(),
            evidence,
            rules.FormatVersion);

    private static CfeEligibilityResult Review(
        CfeEligibilityRulePack rules,
        IReadOnlyCollection<string> reasons,
        IReadOnlyCollection<string> missingFacts,
        IReadOnlyCollection<RegulatoryRuleEvidence> evidence) =>
        new(
            CfeEligibilityStatus.RequiresReview,
            Array.Empty<CfeCandidate>(),
            reasons,
            missingFacts,
            evidence,
            rules.FormatVersion);
}
