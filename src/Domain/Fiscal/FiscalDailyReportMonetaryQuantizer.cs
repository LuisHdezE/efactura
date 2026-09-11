using EFactura.Domain.Common;

namespace EFactura.Domain.Fiscal;

/// <summary>
/// Monetary wire quantization for Reporte Diario v13.2. Reconciliation and FX conversion retain
/// full decimal precision; numerical modification happens only when materializing NUM 17 monetary
/// concepts for the wire projection.
/// </summary>
public static class FiscalDailyReportMonetaryQuantizer
{
    public const int MonetaryScale = FiscalDailyReportV13_2WireContract.MonetaryDecimalDigits;
    public const decimal MaximumWireAmount = 999_999_999_999_999.99m;

    public static decimal QuantizeNonNegative(decimal value)
    {
        if (value < 0m)
        {
            throw new DomainRuleException(
                "fiscal.daily_report.wire.monetary_negative",
                "The accepted Reporte Diario Release-1 monetary concepts cannot be negative.");
        }

        var quantized = decimal.Round(value, MonetaryScale, MidpointRounding.AwayFromZero);
        EnsureFitsWire(quantized);
        return quantized;
    }

    public static void EnsureFitsWire(decimal value)
    {
        if (value < 0m
            || value > MaximumWireAmount
            || value != decimal.Round(value, MonetaryScale, MidpointRounding.ToEven))
        {
            throw new DomainRuleException(
                "fiscal.daily_report.wire.monetary_out_of_range",
                "Reporte Diario monetary value must fit NUM 17 with 15 integer and 2 decimal digits.");
        }
    }
}
