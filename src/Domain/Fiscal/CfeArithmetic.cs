using EFactura.Domain.Common;
using EFactura.Domain.Taxation;

namespace EFactura.Domain.Fiscal;

public enum CfeDetailAmountMode
{
    NetOfVat = 1,
    VatIncluded = 2
}

public sealed class CfeArithmeticRulePack
{
    public CfeArithmeticRulePack(
        string version,
        string formatVersion,
        DateOnly supportedFrom,
        int monetaryScale,
        RegulatoryRuleEvidence itemAmountRule,
        RegulatoryRuleEvidence headerTotalsRule,
        RegulatoryRuleEvidence roundingRule,
        RegulatoryRuleEvidence? grossAmountsRule = null)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new DomainRuleException("fiscal.arithmetic.rule_pack_version_required", "CFE arithmetic rule-pack version is required.");
        if (string.IsNullOrWhiteSpace(formatVersion))
            throw new DomainRuleException("fiscal.arithmetic.format_version_required", "CFE format version is required.");
        if (monetaryScale is < 0 or > 8)
            throw new DomainRuleException("fiscal.arithmetic.scale_invalid", "CFE monetary scale is outside the supported range.");

        Version = version.Trim();
        FormatVersion = formatVersion.Trim();
        SupportedFrom = supportedFrom;
        MonetaryScale = monetaryScale;
        ItemAmountRule = itemAmountRule ?? throw new ArgumentNullException(nameof(itemAmountRule));
        HeaderTotalsRule = headerTotalsRule ?? throw new ArgumentNullException(nameof(headerTotalsRule));
        RoundingRule = roundingRule ?? throw new ArgumentNullException(nameof(roundingRule));
        GrossAmountsRule = grossAmountsRule;
    }

    public string Version { get; }
    public string FormatVersion { get; }
    public DateOnly SupportedFrom { get; }
    public int MonetaryScale { get; }
    public RegulatoryRuleEvidence ItemAmountRule { get; }
    public RegulatoryRuleEvidence HeaderTotalsRule { get; }
    public RegulatoryRuleEvidence RoundingRule { get; }
    public RegulatoryRuleEvidence? GrossAmountsRule { get; }

    public decimal Round(decimal value) =>
        decimal.Round(value, MonetaryScale, MidpointRounding.AwayFromZero);
}

public sealed record CfeArithmeticLineInput(
    Guid LineId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal SurchargeAmount,
    TaxRateResolution TaxRate);

public sealed record CfeArithmeticRequest(
    DateOnly EffectiveOn,
    string CurrencyCode,
    IReadOnlyCollection<CfeArithmeticLineInput> Lines,
    CfeDetailAmountMode DetailAmountMode = CfeDetailAmountMode.NetOfVat);

public sealed record CfeArithmeticLineResult(
    Guid LineId,
    decimal ItemAmount,
    VatLiabilityKind VatLiability,
    VatRateKind VatRateKind,
    decimal AppliedRatePercent,
    IReadOnlyCollection<RegulatoryRuleEvidence> RuleEvidence,
    string RateRulePackVersion);

public sealed record CfeArithmeticTotals(
    decimal NetAmount,
    decimal MinimumTaxableAmount,
    decimal BasicTaxableAmount,
    decimal ExportAmount,
    decimal MinimumVatAmount,
    decimal BasicVatAmount,
    decimal VatAmount,
    decimal TotalAmount);

public sealed record CfeArithmeticResult(
    string CurrencyCode,
    string FormatVersion,
    string ArithmeticRulePackVersion,
    CfeDetailAmountMode DetailAmountMode,
    IReadOnlyCollection<CfeArithmeticLineResult> Lines,
    CfeArithmeticTotals Totals,
    IReadOnlyCollection<RegulatoryRuleEvidence> RuleEvidence);

public sealed class CfeArithmeticCalculator
{
    public CfeArithmeticResult Calculate(CfeArithmeticRequest request, CfeArithmeticRulePack rules)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(rules);

        if (request.EffectiveOn < rules.SupportedFrom)
        {
            throw new DomainRuleException(
                "fiscal.arithmetic.rule_pack_date_unsupported",
                "The selected CFE arithmetic rule pack does not cover the requested fiscal date.");
        }

        if (!Enum.IsDefined(request.DetailAmountMode))
        {
            throw new DomainRuleException(
                "fiscal.arithmetic.detail_amount_mode_invalid",
                "The CFE detail amount mode is not supported.");
        }

        EnsureEvidenceCovers(rules.ItemAmountRule, request.EffectiveOn);
        EnsureEvidenceCovers(rules.HeaderTotalsRule, request.EffectiveOn);
        EnsureEvidenceCovers(rules.RoundingRule, request.EffectiveOn);
        if (request.DetailAmountMode == CfeDetailAmountMode.VatIncluded)
        {
            if (rules.GrossAmountsRule is null)
            {
                throw new DomainRuleException(
                    "fiscal.arithmetic.gross_amount_rule_required",
                    "VAT-included CFE arithmetic requires explicit gross-amount regulatory evidence.");
            }
            EnsureEvidenceCovers(rules.GrossAmountsRule, request.EffectiveOn);
        }

        var currencyCode = NormalizeCurrency(request.CurrencyCode);
        if (request.Lines is null || request.Lines.Count == 0)
            throw new DomainRuleException("fiscal.arithmetic.lines_required", "At least one fiscal line is required.");
        if (request.Lines.Any(line => line is null))
            throw new DomainRuleException("fiscal.arithmetic.line_required", "Fiscal lines cannot contain null entries.");
        if (request.Lines.Select(line => line.LineId).Distinct().Count() != request.Lines.Count)
            throw new DomainRuleException("fiscal.arithmetic.line_id_duplicate", "Fiscal line identifiers must be unique.");

        var lineResults = request.Lines.Select(line => CalculateLine(line, request.EffectiveOn, rules)).ToArray();

        var minimumDetailAmount = Sum(lineResults, VatRateKind.Minimum);
        var basicDetailAmount = Sum(lineResults, VatRateKind.Basic);
        var exportAmount = Sum(lineResults, VatRateKind.Export);

        var minimumRate = ResolveBucketRate(lineResults, VatRateKind.Minimum);
        var basicRate = ResolveBucketRate(lineResults, VatRateKind.Basic);
        var minimumTaxable = ResolveTaxableBucket(
            minimumDetailAmount, minimumRate, request.DetailAmountMode, rules);
        var basicTaxable = ResolveTaxableBucket(
            basicDetailAmount, basicRate, request.DetailAmountMode, rules);

        var minimumVat = minimumRate.HasValue
            ? rules.Round(minimumTaxable * minimumRate.Value / 100m)
            : 0m;
        var basicVat = basicRate.HasValue
            ? rules.Round(basicTaxable * basicRate.Value / 100m)
            : 0m;
        var vatAmount = rules.Round(minimumVat + basicVat);
        var netAmount = rules.Round(minimumTaxable + basicTaxable + exportAmount);
        var totalAmount = rules.Round(netAmount + vatAmount);

        var totals = new CfeArithmeticTotals(
            netAmount,
            minimumTaxable,
            basicTaxable,
            exportAmount,
            minimumVat,
            basicVat,
            vatAmount,
            totalAmount);

        IEnumerable<RegulatoryRuleEvidence> evidenceSource = lineResults
            .SelectMany(line => line.RuleEvidence)
            .Append(rules.ItemAmountRule)
            .Append(rules.HeaderTotalsRule)
            .Append(rules.RoundingRule);
        if (request.DetailAmountMode == CfeDetailAmountMode.VatIncluded && rules.GrossAmountsRule is not null)
            evidenceSource = evidenceSource.Append(rules.GrossAmountsRule);

        return new CfeArithmeticResult(
            currencyCode,
            rules.FormatVersion,
            rules.Version,
            request.DetailAmountMode,
            lineResults,
            totals,
            evidenceSource.DistinctBy(item => item.RuleId).ToArray());
    }

    private static CfeArithmeticLineResult CalculateLine(
        CfeArithmeticLineInput line,
        DateOnly effectiveOn,
        CfeArithmeticRulePack rules)
    {
        if (line.LineId == Guid.Empty)
            throw new DomainRuleException("fiscal.arithmetic.line_id_required", "Fiscal line identifier is required.");
        if (line.Quantity <= 0m)
            throw new DomainRuleException("fiscal.arithmetic.quantity_invalid", "Fiscal line quantity must be greater than zero.");
        if (line.UnitPrice < 0m || line.DiscountAmount < 0m || line.SurchargeAmount < 0m)
            throw new DomainRuleException("fiscal.arithmetic.amount_negative", "Fiscal prices, discounts and surcharges cannot be negative.");
        if (line.TaxRate is null)
            throw new DomainRuleException("fiscal.arithmetic.tax_rate_required", "Resolved tax-rate evidence is required for fiscal arithmetic.");

        ValidateResolvedRate(line.TaxRate, effectiveOn);

        var rawItemAmount = (line.Quantity * line.UnitPrice) - line.DiscountAmount + line.SurchargeAmount;
        if (rawItemAmount < 0m)
            throw new DomainRuleException("fiscal.arithmetic.item_amount_negative", "Fiscal item amount cannot become negative after discounts and surcharges.");

        return new CfeArithmeticLineResult(
            line.LineId,
            rules.Round(rawItemAmount),
            line.TaxRate.Liability,
            line.TaxRate.RateKind,
            line.TaxRate.AppliedRatePercent!.Value,
            line.TaxRate.RuleEvidence,
            line.TaxRate.RateRulePackVersion);
    }

    private static decimal ResolveTaxableBucket(
        decimal detailAmount,
        decimal? ratePercent,
        CfeDetailAmountMode detailAmountMode,
        CfeArithmeticRulePack rules)
    {
        if (!ratePercent.HasValue)
            return 0m;

        if (detailAmountMode == CfeDetailAmountMode.NetOfVat)
            return rules.Round(detailAmount);

        if (ratePercent.Value <= 0m)
        {
            throw new DomainRuleException(
                "fiscal.arithmetic.gross_vat_rate_invalid",
                "VAT-included taxable buckets require a positive VAT rate.");
        }

        return rules.Round(detailAmount / (1m + (ratePercent.Value / 100m)));
    }

    private static void ValidateResolvedRate(TaxRateResolution rate, DateOnly effectiveOn)
    {
        if (rate.Status != TaxRateResolutionStatus.Resolved)
            throw new DomainRuleException("fiscal.arithmetic.tax_rate_unresolved", "Fiscal arithmetic requires a resolved tax rate.");
        if (!rate.AppliedRatePercent.HasValue)
            throw new DomainRuleException("fiscal.arithmetic.tax_rate_percent_required", "Resolved fiscal arithmetic requires an applied rate percentage.");
        if (string.IsNullOrWhiteSpace(rate.RateRulePackVersion))
            throw new DomainRuleException("fiscal.arithmetic.tax_rate_pack_required", "Resolved fiscal arithmetic requires tax rule-pack provenance.");
        if (rate.RuleEvidence is null || rate.RuleEvidence.Count == 0)
            throw new DomainRuleException("fiscal.arithmetic.tax_rule_evidence_required", "Resolved fiscal arithmetic requires regulatory rule evidence.");

        foreach (var evidence in rate.RuleEvidence)
            EnsureEvidenceCovers(evidence, effectiveOn);

        if (rate.Liability == VatLiabilityKind.VatDue)
        {
            if (rate.RateKind is not (VatRateKind.Minimum or VatRateKind.Basic)
                || rate.AppliedRatePercent.Value <= 0m)
            {
                throw new DomainRuleException("fiscal.arithmetic.vat_due_rate_invalid", "Release-1 VAT-due arithmetic supports resolved minimum or basic VAT rates only.");
            }
            return;
        }

        if (rate.Liability == VatLiabilityKind.NoVatDue
            && rate.RateKind == VatRateKind.Export
            && rate.AppliedRatePercent.Value == 0m)
        {
            return;
        }

        throw new DomainRuleException(
            "fiscal.arithmetic.tax_treatment_not_supported",
            "This Release-1 CFE arithmetic slice does not yet support the resolved tax treatment supplied for the line.");
    }

    private static decimal Sum(IEnumerable<CfeArithmeticLineResult> lines, VatRateKind rateKind) =>
        lines.Where(line => line.VatRateKind == rateKind).Sum(line => line.ItemAmount);

    private static decimal? ResolveBucketRate(
        IReadOnlyCollection<CfeArithmeticLineResult> lines,
        VatRateKind rateKind)
    {
        var rates = lines
            .Where(line => line.VatRateKind == rateKind)
            .Select(line => line.AppliedRatePercent)
            .Distinct()
            .ToArray();

        if (rates.Length > 1)
        {
            throw new DomainRuleException(
                "fiscal.arithmetic.inconsistent_bucket_rate",
                "A fiscal VAT bucket cannot contain multiple applied rates under the same rate kind.");
        }

        return rates.Length == 0 ? null : rates[0];
    }

    private static string NormalizeCurrency(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainRuleException("fiscal.arithmetic.currency_required", "Fiscal calculation currency is required.");

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch < 'A' || ch > 'Z'))
            throw new DomainRuleException("fiscal.arithmetic.currency_invalid", "Fiscal calculation currency must use ISO alpha-3 form.");
        return normalized;
    }

    private static void EnsureEvidenceCovers(RegulatoryRuleEvidence evidence, DateOnly effectiveOn)
    {
        if (!evidence.Covers(effectiveOn))
        {
            throw new DomainRuleException(
                "fiscal.arithmetic.rule_not_effective",
                $"Regulatory rule evidence '{evidence.RuleId}' does not cover the requested fiscal date.");
        }
    }
}