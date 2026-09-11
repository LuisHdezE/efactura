using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalDailyReportCurrencyCanonicalizationTests
{
    [Theory]
    [InlineData("usd", "UYU")]
    [InlineData("USD", "uyu")]
    public void Recomputed_fingerprint_cannot_hide_non_canonical_currency_codes(
        string originalCurrency,
        string reportingCurrency)
    {
        var provisional = new FiscalDailyReportDocumentEvidence(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "company-1",
            "214748364700",
            CfeFamily.EFactura,
            "A",
            1,
            new DateOnly(2026, 9, 10),
            "0001",
            false,
            Fingerprint('1'),
            new DateTimeOffset(2026, 9, 10, 10, 30, 0, TimeSpan.FromHours(-3)),
            Fingerprint('2'),
            Fingerprint('3'),
            Fingerprint('4'),
            originalCurrency,
            reportingCurrency,
            Fingerprint('5'),
            false,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            Fingerprint('0'));

        var recomputed = provisional with { EvidenceFingerprint = provisional.ComputeFingerprint() };

        var error = Assert.Throws<DomainRuleException>(recomputed.EnsureIntegrity);
        Assert.Equal("fiscal.daily_report.currency_not_canonical", error.Code);
    }

    private static string Fingerprint(char value) => new(value, 64);
}
