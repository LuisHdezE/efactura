using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalDailyReportWireContractTests
{
    [Fact]
    public void V13_2_functional_contract_pins_authoritative_limits_without_drifting_from_snapshot_version()
    {
        Assert.Equal("13.2", FiscalDailyReportV13_2WireContract.Version);
        Assert.Equal(
            FiscalDailyReportV13_2WireContract.Version,
            FiscalDailyReportSnapshot.CurrentFormatVersion);
        Assert.Equal("UYU", FiscalDailyReportV13_2WireContract.ReportingCurrencyCode);
        Assert.Equal(1_000, FiscalDailyReportV13_2WireContract.MaximumSummaryZoneOccurrences);
        Assert.Equal(1_000, FiscalDailyReportV13_2WireContract.MaximumAmountRowsPerSummary);
        Assert.Equal(50_000, FiscalDailyReportV13_2WireContract.MaximumNumberRangeRepetitions);
        Assert.Equal(15, FiscalDailyReportV13_2WireContract.MonetaryIntegerDigits);
        Assert.Equal(2, FiscalDailyReportV13_2WireContract.MonetaryDecimalDigits);
        Assert.Equal(3, FiscalDailyReportV13_2WireContract.VatRateIntegerDigits);
        Assert.Equal(3, FiscalDailyReportV13_2WireContract.VatRateDecimalDigits);
        Assert.Equal(4, FiscalDailyReportV13_2WireContract.BranchCodeDigits);
        Assert.Equal(2, FiscalDailyReportV13_2WireContract.SeriesMaximumLength);
        Assert.Equal(1L, FiscalDailyReportV13_2WireContract.MinimumDocumentNumber);
        Assert.Equal(9_999_999L, FiscalDailyReportV13_2WireContract.MaximumDocumentNumber);
        Assert.Equal(1, FiscalDailyReportV13_2WireContract.ThirdPartyPaymentIndicator);
        Assert.Equal(0, FiscalDailyReportV13_2WireContract.HighValueTicketCounterMinimum);
        Assert.Equal("XML Digital Signature", FiscalDailyReportV13_2WireContract.XmlDigitalSignatureStandard);
    }

    [Fact]
    public void Snapshot_rejects_more_than_1000_amount_rows_for_one_CFE_type()
    {
        var summaryDate = new DateOnly(2026, 9, 10);
        var snapshot = FiscalDailyReportSnapshot.Create(
            "org-wire-contract",
            "219999830019",
            summaryDate,
            sequence: 1);
        var bucket = new FiscalDailyReportSummaryBucket(
            CfeFamily.ETicket,
            summaryDate,
            "0001",
            false,
            1,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m,
            0m);

        var tampered = snapshot with
        {
            Summaries = Enumerable
                .Repeat(bucket, FiscalDailyReportV13_2WireContract.MaximumAmountRowsPerSummary + 1)
                .ToArray()
        };

        var exception = Assert.Throws<DomainRuleException>(tampered.EnsureIntegrity);
        Assert.Equal("fiscal.daily_report.summary_repetition_limit_exceeded", exception.Code);
    }

    [Fact]
    public void V13_2_contract_document_keeps_unresolved_serializer_rules_fail_closed()
    {
        var root = FindRepositoryRoot();
        var contract = File.ReadAllText(
            Path.Combine(
                root,
                "documentation",
                "blueprint-api-implementation",
                "41_FISCAL_DAILY_REPORT_V13_2_WIRE_CONTRACT.md"));

        foreach (var required in new[]
        {
            "current v13.2 B-C27 validation is only `C27 >= 0`",
            "1000 non-duplicated grouping rows",
            "B-C14",
            "B-C15",
            "B-C18",
            "B-C21",
            "B-C22",
            "B-C23",
            "B-C25",
            "B-C25.1",
            "B-C27 remains an explicit serializer/product gap",
            "Wire rounding/quantization therefore remains **UNRESOLVED / FAIL-CLOSED**",
            "does **not** claim to have vendored or hashed the current report example/XSD bytes",
            "Historical/generated public implementations",
            "Formal traditional DGI Testing readiness remains exactly:",
            "**BLOCKED BY MISSING PRODUCT CAPABILITIES**"
        })
        {
            Assert.Contains(required, contract, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("rsa-sha1", contract, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sha1", contract, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("serializer is ready", contract, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void V13_2_contract_points_only_to_current_official_DGI_source_endpoints()
    {
        Assert.StartsWith(
            "https://www.efactura.dgi.gub.uy/",
            FiscalDailyReportV13_2WireContract.OfficialRegistryUrl,
            StringComparison.Ordinal);
        Assert.StartsWith(
            "https://www.efactura.dgi.gub.uy/",
            FiscalDailyReportV13_2WireContract.OfficialFormatUrl,
            StringComparison.Ordinal);
        Assert.StartsWith(
            "https://www.efactura.dgi.gub.uy/",
            FiscalDailyReportV13_2WireContract.OfficialExampleUrl,
            StringComparison.Ordinal);
        Assert.StartsWith(
            "https://www.efactura.dgi.gub.uy/",
            FiscalDailyReportV13_2WireContract.OfficialSchemaArchiveUrl,
            StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "api-accounting.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Repository root containing api-accounting.sln was not found.");
    }
}
