using EFactura.Application.Fiscal;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Taxation;
using Xunit;

namespace CrossCuttingTests;

public sealed class CfeArithmeticTests
{
    private static readonly DateOnly EffectiveOn = new(2026, 9, 3);
    private readonly CfeArithmeticCalculator _calculator = new();

    [Fact]
    public void Item_amount_uses_quantity_price_discount_surcharge_and_mathematical_two_decimal_rounding()
    {
        var result = Calculate(
            new CfeArithmeticLineInput(
                Guid.NewGuid(),
                Quantity: 1m,
                UnitPrice: 10.005m,
                DiscountAmount: 1m,
                SurchargeAmount: 0m,
                TaxRate: ResolvedVat(VatRateKind.Basic, 22m)));

        Assert.Equal(9.01m, result.Lines.Single().ItemAmount);
        Assert.Equal(9.01m, result.Totals.BasicTaxableAmount);
    }

    [Fact]
    public void Vat_is_rounded_from_the_header_taxable_bucket_not_from_each_line_tax()
    {
        var result = Calculate(
            BasicLine(0.02m),
            BasicLine(0.02m));

        Assert.Equal(0.04m, result.Totals.BasicTaxableAmount);
        Assert.Equal(0.01m, result.Totals.BasicVatAmount);
        Assert.Equal(0.01m, result.Totals.VatAmount);
        Assert.Equal(0.05m, result.Totals.TotalAmount);
    }

    [Fact]
    public void Minimum_vat_midpoint_uses_mathematical_two_decimal_rounding()
    {
        var result = Calculate(
            new CfeArithmeticLineInput(
                Guid.NewGuid(),
                1m,
                0.25m,
                0m,
                0m,
                ResolvedVat(VatRateKind.Minimum, 10m)));

        Assert.Equal(0.25m, result.Totals.MinimumTaxableAmount);
        Assert.Equal(0.03m, result.Totals.MinimumVatAmount);
        Assert.Equal(0.28m, result.Totals.TotalAmount);
    }

    [Fact]
    public void Basic_and_minimum_vat_are_calculated_in_separate_header_buckets()
    {
        var result = Calculate(
            BasicLine(100m),
            new CfeArithmeticLineInput(
                Guid.NewGuid(),
                1m,
                50m,
                0m,
                0m,
                ResolvedVat(VatRateKind.Minimum, 10m)));

        Assert.Equal(100m, result.Totals.BasicTaxableAmount);
        Assert.Equal(50m, result.Totals.MinimumTaxableAmount);
        Assert.Equal(22m, result.Totals.BasicVatAmount);
        Assert.Equal(5m, result.Totals.MinimumVatAmount);
        Assert.Equal(177m, result.Totals.TotalAmount);
    }

    [Fact]
    public void Export_no_vat_due_is_preserved_as_export_amount_with_zero_tax()
    {
        var result = Calculate(
            new CfeArithmeticLineInput(
                Guid.NewGuid(),
                2m,
                15m,
                0m,
                0m,
                ResolvedExport()));

        Assert.Equal(30m, result.Totals.ExportAmount);
        Assert.Equal(30m, result.Totals.NetAmount);
        Assert.Equal(0m, result.Totals.VatAmount);
        Assert.Equal(30m, result.Totals.TotalAmount);
        Assert.Equal(VatLiabilityKind.NoVatDue, result.Lines.Single().VatLiability);
        Assert.Equal(VatRateKind.Export, result.Lines.Single().VatRateKind);
    }

    [Fact]
    public void Unresolved_tax_rate_is_rejected_before_arithmetic()
    {
        var unresolved = new TaxRateResolution(
            TaxRateResolutionStatus.RequiresReview,
            VatLiabilityKind.RequiresReview,
            VatRateKind.Unsupported,
            null,
            null,
            null,
            "REQUIRES_REVIEW",
            new[] { "tax.rate.review" },
            new[] { "resolved_rate" },
            new[] { Evidence("TEST-RULE") },
            "TEST-RATE-PACK");

        var exception = Assert.Throws<DomainRuleException>(() => Calculate(
            new CfeArithmeticLineInput(Guid.NewGuid(), 1m, 10m, 0m, 0m, unresolved)));

        Assert.Equal("fiscal.arithmetic.tax_rate_unresolved", exception.Code);
    }

    [Fact]
    public void Release1_does_not_silently_enable_exemption_arithmetic_without_an_accepted_exemption_rule_slice()
    {
        var exempt = new TaxRateResolution(
            TaxRateResolutionStatus.Resolved,
            VatLiabilityKind.NoVatDue,
            VatRateKind.Exempt,
            0m,
            Guid.NewGuid(),
            "EXEMPT",
            "VAT_EXEMPT",
            new[] { "test" },
            Array.Empty<string>(),
            new[] { Evidence("TEST-EXEMPT") },
            "TEST-RATE-PACK");

        var exception = Assert.Throws<DomainRuleException>(() => Calculate(
            new CfeArithmeticLineInput(Guid.NewGuid(), 1m, 10m, 0m, 0m, exempt)));

        Assert.Equal("fiscal.arithmetic.tax_treatment_not_supported", exception.Code);
    }

    [Fact]
    public void Rule_pack_rejects_dates_before_CFE_25_2_production_support_boundary()
    {
        var request = new CfeArithmeticRequest(
            new DateOnly(2026, 6, 29),
            "UYU",
            new[] { BasicLine(10m) });

        var exception = Assert.Throws<DomainRuleException>(() =>
            _calculator.Calculate(request, UruguayCfe25_2ArithmeticCatalog.Current));

        Assert.Equal("fiscal.arithmetic.rule_pack_date_unsupported", exception.Code);
    }

    private CfeArithmeticResult Calculate(params CfeArithmeticLineInput[] lines) =>
        _calculator.Calculate(
            new CfeArithmeticRequest(EffectiveOn, "UYU", lines),
            UruguayCfe25_2ArithmeticCatalog.Current);

    private static CfeArithmeticLineInput BasicLine(decimal unitPrice) =>
        new(Guid.NewGuid(), 1m, unitPrice, 0m, 0m, ResolvedVat(VatRateKind.Basic, 22m));

    private static TaxRateResolution ResolvedVat(VatRateKind kind, decimal rate) =>
        new(
            TaxRateResolutionStatus.Resolved,
            VatLiabilityKind.VatDue,
            kind,
            rate,
            Guid.NewGuid(),
            kind == VatRateKind.Basic ? "VAT_BASIC" : "VAT_MINIMUM",
            kind == VatRateKind.Basic ? "VAT_BASIC" : "VAT_MINIMUM",
            new[] { "tax.rate.authoritative_profile_and_rate_match" },
            Array.Empty<string>(),
            new[] { Evidence($"TEST-{kind.ToString().ToUpperInvariant()}") },
            "UY-IVA-RATE-R1-TEST");

    private static TaxRateResolution ResolvedExport() =>
        new(
            TaxRateResolutionStatus.Resolved,
            VatLiabilityKind.NoVatDue,
            VatRateKind.Export,
            0m,
            null,
            null,
            "EXPORT_GOODS",
            new[] { "tax.rate.no_vat_due_export_goods" },
            Array.Empty<string>(),
            new[] { Evidence("TEST-EXPORT") },
            "UY-IVA-RATE-R1-TEST");

    private static RegulatoryRuleEvidence Evidence(string id) =>
        new(
            id,
            "Test regulatory source",
            "https://example.invalid/regulatory-evidence",
            "test-version",
            UruguayCfe25_2ArithmeticCatalog.SupportedFrom);
}
