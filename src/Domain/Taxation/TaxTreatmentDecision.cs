using EFactura.Domain.Common;

namespace EFactura.Domain.Taxation;

public enum TaxOperationKind
{
    Goods = 1,
    Services = 2,
    Mixed = 3
}

public enum GoodsMovementScope
{
    Unknown = 0,
    DomesticDelivery = 1,
    ExportConfirmed = 2
}

public enum ServicePerformanceScope
{
    UnknownOrMixed = 0,
    EntirelyInUruguay = 1,
    EntirelyOutsideUruguay = 2
}

public enum ExportServiceEligibilityStatus
{
    NotEvaluated = 0,
    Qualified = 1,
    NotQualified = 2,
    InsufficientEvidence = 3,
    UnsupportedScenario = 4
}

public enum TaxTreatmentClassification
{
    Domestic = 1,
    ExportGoods = 2,
    ExportServices = 3,
    OutsideVatTerritorialScope = 4,
    RequiresReview = 5
}

public enum TaxDecisionStatus
{
    Resolved = 1,
    RequiresReview = 2
}

public sealed class ReceiverFiscalIdentityFact
{
    public ReceiverFiscalIdentityFact(string typeCode, string issuingCountry)
    {
        TypeCode = Required(typeCode, 32, "tax.receiver.identity_type_required").ToUpperInvariant();
        IssuingCountry = NormalizeCountry(issuingCountry, "tax.receiver.identity_country_invalid");
    }

    public string TypeCode { get; }
    public string IssuingCountry { get; }

    private static string Required(string value, int maxLength, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException(code, "Required receiver fiscal identity value is missing.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainRuleException(code, $"Receiver fiscal identity value cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static string NormalizeCountry(string value, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException(code, "Country is required.");
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized == "99")
        {
            return normalized;
        }

        if (normalized.Length != 2 || normalized.Any(ch => ch < 'A' || ch > 'Z'))
        {
            throw new DomainRuleException(code, "Country must be an ISO alpha-2 code or the accepted 99 marker.");
        }

        return normalized;
    }
}

public sealed class ReceiverTaxFacts
{
    private readonly IReadOnlyCollection<ReceiverFiscalIdentityFact> _fiscalIdentities;

    public ReceiverTaxFacts(
        string residenceCountry,
        string taxResidenceCountry,
        IEnumerable<ReceiverFiscalIdentityFact>? fiscalIdentities = null)
    {
        ResidenceCountry = NormalizeCountry(residenceCountry, "tax.receiver.residence_country_invalid");
        TaxResidenceCountry = NormalizeCountry(taxResidenceCountry, "tax.receiver.tax_residence_country_invalid");
        _fiscalIdentities = (fiscalIdentities ?? Array.Empty<ReceiverFiscalIdentityFact>()).ToArray();
    }

    public string ResidenceCountry { get; }
    public string TaxResidenceCountry { get; }
    public IReadOnlyCollection<ReceiverFiscalIdentityFact> FiscalIdentities => _fiscalIdentities;

    public bool HasUruguayanRuc =>
        _fiscalIdentities.Any(identity =>
            string.Equals(identity.TypeCode, "2", StringComparison.OrdinalIgnoreCase)
            && string.Equals(identity.IssuingCountry, "UY", StringComparison.OrdinalIgnoreCase));

    private static string NormalizeCountry(string value, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException(code, "Country is required.");
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized == "99")
        {
            return normalized;
        }

        if (normalized.Length != 2 || normalized.Any(ch => ch < 'A' || ch > 'Z'))
        {
            throw new DomainRuleException(code, "Country must be an ISO alpha-2 code or the accepted 99 marker.");
        }

        return normalized;
    }
}

public sealed class RegulatoryRuleEvidence
{
    public RegulatoryRuleEvidence(
        string ruleId,
        string sourceName,
        string sourceReference,
        string sourceVersion,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null,
        string? clause = null)
    {
        RuleId = Required(ruleId, 120, "tax.rule.rule_id_required");
        SourceName = Required(sourceName, 250, "tax.rule.source_name_required");
        SourceReference = Required(sourceReference, 1000, "tax.rule.source_reference_required");
        SourceVersion = Required(sourceVersion, 200, "tax.rule.source_version_required");
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        Clause = string.IsNullOrWhiteSpace(clause) ? null : clause.Trim();

        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
        {
            throw new DomainRuleException("tax.rule.invalid_effective_range", "Regulatory rule effective range is invalid.");
        }
    }

    public string RuleId { get; }
    public string SourceName { get; }
    public string SourceReference { get; }
    public string SourceVersion { get; }
    public DateOnly EffectiveFrom { get; }
    public DateOnly? EffectiveTo { get; }
    public string? Clause { get; }

    public bool Covers(DateOnly date) =>
        EffectiveFrom <= date && (!EffectiveTo.HasValue || date <= EffectiveTo.Value);

    private static string Required(string value, int maxLength, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainRuleException(code, "Required regulatory rule evidence value is missing.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainRuleException(code, $"Regulatory rule evidence value cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}

public sealed class TaxTreatmentRulePack
{
    public TaxTreatmentRulePack(
        string version,
        RegulatoryRuleEvidence territorialityRule,
        RegulatoryRuleEvidence exportGoodsRule)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new DomainRuleException("tax.rule_pack.version_required", "Tax rule-pack version is required.");
        }

        Version = version.Trim();
        TerritorialityRule = territorialityRule ?? throw new ArgumentNullException(nameof(territorialityRule));
        ExportGoodsRule = exportGoodsRule ?? throw new ArgumentNullException(nameof(exportGoodsRule));
    }

    public string Version { get; }
    public RegulatoryRuleEvidence TerritorialityRule { get; }
    public RegulatoryRuleEvidence ExportGoodsRule { get; }
}

public sealed class TaxTransactionFacts
{
    private readonly IReadOnlyCollection<string> _evidenceReferences;

    public TaxTransactionFacts(
        string organizationId,
        DateOnly effectiveOn,
        TaxOperationKind operationKind,
        ReceiverTaxFacts receiver,
        GoodsMovementScope goodsMovementScope = GoodsMovementScope.Unknown,
        ServicePerformanceScope servicePerformanceScope = ServicePerformanceScope.UnknownOrMixed,
        string? deliveryCountry = null,
        string? serviceUseCountry = null,
        IEnumerable<string>? evidenceReferences = null)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
        {
            throw new DomainRuleException("tax.organization_required", "Organization is required for tax treatment resolution.");
        }

        OrganizationId = organizationId.Trim();
        EffectiveOn = effectiveOn;
        OperationKind = operationKind;
        Receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
        GoodsMovementScope = goodsMovementScope;
        ServicePerformanceScope = servicePerformanceScope;
        DeliveryCountry = NormalizeOptionalCountry(deliveryCountry, "tax.delivery_country_invalid");
        ServiceUseCountry = NormalizeOptionalCountry(serviceUseCountry, "tax.service_use_country_invalid");
        _evidenceReferences = (evidenceReferences ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public string OrganizationId { get; }
    public DateOnly EffectiveOn { get; }
    public TaxOperationKind OperationKind { get; }
    public ReceiverTaxFacts Receiver { get; }
    public GoodsMovementScope GoodsMovementScope { get; }
    public ServicePerformanceScope ServicePerformanceScope { get; }
    public string? DeliveryCountry { get; }
    public string? ServiceUseCountry { get; }
    public IReadOnlyCollection<string> EvidenceReferences => _evidenceReferences;

    private static string? NormalizeOptionalCountry(string? value, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized == "99")
        {
            return normalized;
        }

        if (normalized.Length != 2 || normalized.Any(ch => ch < 'A' || ch > 'Z'))
        {
            throw new DomainRuleException(code, "Country must be an ISO alpha-2 code or the accepted 99 marker.");
        }

        return normalized;
    }
}

public sealed class ExportServiceEligibilityEvaluation
{
    public ExportServiceEligibilityEvaluation(
        ExportServiceEligibilityStatus status,
        IEnumerable<RegulatoryRuleEvidence>? ruleEvidence = null,
        IEnumerable<string>? reasons = null,
        IEnumerable<string>? missingEvidence = null)
    {
        Status = status;
        RuleEvidence = (ruleEvidence ?? Array.Empty<RegulatoryRuleEvidence>()).ToArray();
        Reasons = (reasons ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToArray();
        MissingEvidence = (missingEvidence ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToArray();

        if (status == ExportServiceEligibilityStatus.Qualified && RuleEvidence.Count == 0)
        {
            throw new DomainRuleException(
                "tax.export_service.rule_evidence_required",
                "A qualified export-of-services evaluation must include regulatory rule evidence.");
        }
    }

    public ExportServiceEligibilityStatus Status { get; }
    public IReadOnlyCollection<RegulatoryRuleEvidence> RuleEvidence { get; }
    public IReadOnlyCollection<string> Reasons { get; }
    public IReadOnlyCollection<string> MissingEvidence { get; }

    public static ExportServiceEligibilityEvaluation NotEvaluated() =>
        new(ExportServiceEligibilityStatus.NotEvaluated);
}

public sealed record TaxTreatmentDecision(
    TaxDecisionStatus Status,
    TaxTreatmentClassification Classification,
    string TreatmentCode,
    IReadOnlyCollection<string> Reasons,
    IReadOnlyCollection<string> MissingFacts,
    IReadOnlyCollection<RegulatoryRuleEvidence> RuleEvidence,
    string RulePackVersion);

public sealed class TaxTreatmentDecisionEngine
{
    public TaxTreatmentDecision Resolve(
        TaxTransactionFacts facts,
        TaxTreatmentRulePack rulePack,
        ExportServiceEligibilityEvaluation? exportServiceEligibility = null)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(rulePack);

        EnsureEffective(rulePack.TerritorialityRule, facts.EffectiveOn);

        if (facts.OperationKind == TaxOperationKind.Mixed)
        {
            return Review(
                rulePack,
                new[] { "tax.reason.mixed_requires_line_level_resolution" },
                new[] { "line_level_operation_kind" },
                new[] { rulePack.TerritorialityRule });
        }

        return facts.OperationKind switch
        {
            TaxOperationKind.Goods => ResolveGoods(facts, rulePack),
            TaxOperationKind.Services => ResolveServices(
                facts,
                rulePack,
                exportServiceEligibility ?? ExportServiceEligibilityEvaluation.NotEvaluated()),
            _ => Review(
                rulePack,
                new[] { "tax.reason.operation_kind_unsupported" },
                new[] { "operation_kind" },
                new[] { rulePack.TerritorialityRule })
        };
    }

    private static TaxTreatmentDecision ResolveGoods(TaxTransactionFacts facts, TaxTreatmentRulePack rulePack)
    {
        return facts.GoodsMovementScope switch
        {
            GoodsMovementScope.DomesticDelivery => Resolved(
                TaxTreatmentClassification.Domestic,
                "DOMESTIC",
                rulePack,
                new[] { "tax.reason.goods_domestic_delivery" },
                new[] { rulePack.TerritorialityRule }),

            GoodsMovementScope.ExportConfirmed => ResolveConfirmedGoodsExport(facts, rulePack),

            _ => Review(
                rulePack,
                new[] { "tax.reason.goods_export_evidence_missing" },
                new[] { "goods_movement_scope" },
                new[] { rulePack.TerritorialityRule })
        };
    }

    private static TaxTreatmentDecision ResolveConfirmedGoodsExport(
        TaxTransactionFacts facts,
        TaxTreatmentRulePack rulePack)
    {
        EnsureEffective(rulePack.ExportGoodsRule, facts.EffectiveOn);
        return Resolved(
            TaxTreatmentClassification.ExportGoods,
            "EXPORT_GOODS",
            rulePack,
            new[] { "tax.reason.goods_export_confirmed" },
            new[] { rulePack.TerritorialityRule, rulePack.ExportGoodsRule });
    }

    private static TaxTreatmentDecision ResolveServices(
        TaxTransactionFacts facts,
        TaxTreatmentRulePack rulePack,
        ExportServiceEligibilityEvaluation exportServiceEligibility)
    {
        if (facts.ServicePerformanceScope == ServicePerformanceScope.EntirelyOutsideUruguay)
        {
            return Resolved(
                TaxTreatmentClassification.OutsideVatTerritorialScope,
                "OUTSIDE_VAT_SCOPE",
                rulePack,
                new[] { "tax.reason.service_performed_entirely_outside_uruguay" },
                new[] { rulePack.TerritorialityRule });
        }

        if (facts.ServicePerformanceScope == ServicePerformanceScope.UnknownOrMixed)
        {
            return Review(
                rulePack,
                new[] { "tax.reason.service_performance_scope_incomplete" },
                new[] { "service_performance_scope" },
                new[] { rulePack.TerritorialityRule });
        }

        return exportServiceEligibility.Status switch
        {
            ExportServiceEligibilityStatus.Qualified => ResolveQualifiedExportService(
                facts,
                rulePack,
                exportServiceEligibility),

            ExportServiceEligibilityStatus.NotQualified => Resolved(
                TaxTreatmentClassification.Domestic,
                "DOMESTIC",
                rulePack,
                exportServiceEligibility.Reasons.Count == 0
                    ? new[] { "tax.reason.service_export_rule_not_qualified" }
                    : exportServiceEligibility.Reasons,
                new[] { rulePack.TerritorialityRule }.Concat(exportServiceEligibility.RuleEvidence).ToArray()),

            ExportServiceEligibilityStatus.InsufficientEvidence => Review(
                rulePack,
                exportServiceEligibility.Reasons.Count == 0
                    ? new[] { "tax.reason.service_export_evidence_incomplete" }
                    : exportServiceEligibility.Reasons,
                exportServiceEligibility.MissingEvidence.Count == 0
                    ? new[] { "article34_supporting_evidence" }
                    : exportServiceEligibility.MissingEvidence,
                new[] { rulePack.TerritorialityRule }.Concat(exportServiceEligibility.RuleEvidence).ToArray()),

            ExportServiceEligibilityStatus.UnsupportedScenario => Review(
                rulePack,
                exportServiceEligibility.Reasons.Count == 0
                    ? new[] { "tax.reason.service_export_rule_not_supported" }
                    : exportServiceEligibility.Reasons,
                new[] { "supported_article34_rule" },
                new[] { rulePack.TerritorialityRule }.Concat(exportServiceEligibility.RuleEvidence).ToArray()),

            _ => Review(
                rulePack,
                new[] { "tax.reason.service_export_rule_not_evaluated" },
                new[] { "article34_eligibility" },
                new[] { rulePack.TerritorialityRule })
        };
    }

    private static TaxTreatmentDecision ResolveQualifiedExportService(
        TaxTransactionFacts facts,
        TaxTreatmentRulePack rulePack,
        ExportServiceEligibilityEvaluation evaluation)
    {
        foreach (var rule in evaluation.RuleEvidence)
        {
            EnsureEffective(rule, facts.EffectiveOn);
        }

        return Resolved(
            TaxTreatmentClassification.ExportServices,
            "EXPORT_SERVICES",
            rulePack,
            evaluation.Reasons.Count == 0
                ? new[] { "tax.reason.service_article34_qualified" }
                : evaluation.Reasons,
            new[] { rulePack.TerritorialityRule }.Concat(evaluation.RuleEvidence).ToArray());
    }

    private static TaxTreatmentDecision Resolved(
        TaxTreatmentClassification classification,
        string treatmentCode,
        TaxTreatmentRulePack rulePack,
        IReadOnlyCollection<string> reasons,
        IReadOnlyCollection<RegulatoryRuleEvidence> evidence) =>
        new(
            TaxDecisionStatus.Resolved,
            classification,
            treatmentCode,
            reasons,
            Array.Empty<string>(),
            DistinctEvidence(evidence),
            rulePack.Version);

    private static TaxTreatmentDecision Review(
        TaxTreatmentRulePack rulePack,
        IReadOnlyCollection<string> reasons,
        IReadOnlyCollection<string> missingFacts,
        IReadOnlyCollection<RegulatoryRuleEvidence> evidence) =>
        new(
            TaxDecisionStatus.RequiresReview,
            TaxTreatmentClassification.RequiresReview,
            "REQUIRES_REVIEW",
            reasons,
            missingFacts,
            DistinctEvidence(evidence),
            rulePack.Version);

    private static IReadOnlyCollection<RegulatoryRuleEvidence> DistinctEvidence(
        IEnumerable<RegulatoryRuleEvidence> evidence) =>
        evidence
            .GroupBy(rule => $"{rule.RuleId}|{rule.SourceVersion}", StringComparer.Ordinal)
            .Select(group => group.First())
            .ToArray();

    private static void EnsureEffective(RegulatoryRuleEvidence rule, DateOnly date)
    {
        if (!rule.Covers(date))
        {
            throw new DomainRuleException(
                "tax.rule_not_effective",
                $"Regulatory rule {rule.RuleId} is not effective on {date:yyyy-MM-dd}.");
        }
    }
}
