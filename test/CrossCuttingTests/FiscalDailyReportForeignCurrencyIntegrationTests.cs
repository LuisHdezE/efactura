using System.Text.Json;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Taxation;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalDailyReportForeignCurrencyIntegrationTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Settlement = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string SignedContentHash = "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd";
    private const string DgiResponseHash = "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee";
    private const string ThirdPartySourceHash = "1212121212121212121212121212121212121212121212121212121212121212";
    private const string FxSourceHash = "3434343434343434343434343434343434343434343434343434343434343434";
    private static readonly DateOnly ReportDate = new(2026, 9, 10);
    private static readonly DateTimeOffset SigningTimestamp =
        new(2026, 9, 10, 10, 30, 45, TimeSpan.FromHours(-3));

    [Fact]
    public void Foreign_currency_composition_preserves_provenance_and_converts_all_partitions_to_UYU()
    {
        var evidence = ComposeForeign(
            number: 10,
            rateSource: FiscalDailyReportFxRateSource.BcuFiscalQuotation,
            rate: 39.1234567m,
            rateSourceDate: new DateOnly(2026, 9, 9));

        Assert.Equal("USD", evidence.OriginalCurrencyCode);
        Assert.Equal("UYU", evidence.ReportingCurrencyCode);
        Assert.NotNull(evidence.CurrencyConversionEvidenceFingerprint);
        Assert.False(evidence.CurrencyConversionRequiresReliquidation);
        Assert.Equal(3912.3456700m, evidence.NetAmount);
        Assert.Equal(0m, evidence.NonTaxedAmount);
        Assert.Equal(0m, evidence.MinimumTaxableAmount);
        Assert.Equal(3912.3456700m, evidence.BasicTaxableAmount);
        Assert.Equal(0m, evidence.ExportAmount);
        Assert.Equal(0m, evidence.MinimumVatAmount);
        Assert.Equal(860.7160474m, evidence.BasicVatAmount);
        Assert.Equal(860.7160474m, evidence.VatAmount);
        Assert.Equal(4773.0617174m, evidence.TotalAmount);
        evidence.EnsureIntegrity();
    }

    [Fact]
    public void Mixed_UYU_and_foreign_currency_documents_aggregate_only_UYU_reporting_amounts()
    {
        var uyu = ComposeUyu(number: 10);
        var usd = ComposeForeign(
            number: 11,
            rateSource: FiscalDailyReportFxRateSource.BcuFiscalQuotation,
            rate: 39.1234567m,
            rateSourceDate: new DateOnly(2026, 9, 9));

        var report = FiscalDailyReportSnapshot.Create(
            "company-1",
            "214748364700",
            ReportDate,
            1,
            [usd, uyu]);

        var summary = Assert.Single(report.Summaries);
        Assert.Equal(2, summary.DocumentCount);
        Assert.Equal(4012.3456700m, summary.NetAmount);
        Assert.Equal(4012.3456700m, summary.BasicTaxableAmount);
        Assert.Equal(882.7160474m, summary.VatAmount);
        Assert.Equal(4895.0617174m, summary.TotalAmount);
        Assert.All(report.Documents, document => Assert.Equal("UYU", document.ReportingCurrencyCode));
        report.EnsureIntegrity();
    }

    [Fact]
    public void Future_date_fallback_propagates_reliquidation_marker_into_document_evidence()
    {
        var evidence = ComposeForeign(
            number: 12,
            rateSource: FiscalDailyReportFxRateSource.FutureDateCfeRate,
            rate: 39.5m,
            rateSourceDate: ReportDate,
            requiresReliquidation: true);

        Assert.Equal("USD", evidence.OriginalCurrencyCode);
        Assert.True(evidence.CurrencyConversionRequiresReliquidation);
        Assert.NotNull(evidence.CurrencyConversionEvidenceFingerprint);
        evidence.EnsureIntegrity();
    }

    [Fact]
    public void Foreign_currency_evidence_rejects_loss_of_conversion_provenance_even_with_recomputed_fingerprint()
    {
        var evidence = ComposeForeign(
            number: 13,
            rateSource: FiscalDailyReportFxRateSource.BcuFiscalQuotation,
            rate: 39.5m,
            rateSourceDate: new DateOnly(2026, 9, 9));

        var withoutFx = evidence with { CurrencyConversionEvidenceFingerprint = null };
        var tampered = withoutFx with { EvidenceFingerprint = withoutFx.ComputeFingerprint() };

        var error = Assert.Throws<DomainRuleException>(tampered.EnsureIntegrity);
        Assert.Equal("fiscal.daily_report.fx_evidence_required", error.Code);
    }

    [Fact]
    public void Foreign_currency_document_evidence_survives_JSON_roundtrip()
    {
        var evidence = ComposeForeign(
            number: 14,
            rateSource: FiscalDailyReportFxRateSource.BcuFiscalQuotation,
            rate: 39.1234567m,
            rateSourceDate: new DateOnly(2026, 9, 9));

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var json = JsonSerializer.Serialize(evidence, options);
        var roundTrip = JsonSerializer.Deserialize<FiscalDailyReportDocumentEvidence>(json, options);

        Assert.NotNull(roundTrip);
        roundTrip!.EnsureIntegrity();
        Assert.Equal(evidence.EvidenceFingerprint, roundTrip.EvidenceFingerprint);
        Assert.Equal("USD", roundTrip.OriginalCurrencyCode);
        Assert.Equal("UYU", roundTrip.ReportingCurrencyCode);
        Assert.Equal(evidence.CurrencyConversionEvidenceFingerprint, roundTrip.CurrencyConversionEvidenceFingerprint);
    }

    private static FiscalDailyReportDocumentEvidence ComposeUyu(long number)
    {
        var (document, snapshot) = Fixture(number, "UYU");
        var identity = Identity(document, snapshot);
        var outcome = FiscalDailyReportDgiOutcomeEvidence.Capture(
            identity,
            FiscalDailyReportDgiCfeOutcome.Received,
            SigningTimestamp.AddMinutes(2),
            DgiResponseHash);
        var thirdParty = FiscalDailyReportThirdPartyPaymentEvidence.Capture(
            identity,
            null,
            ThirdPartySourceHash);

        var evidence = FiscalDailyReportSourceFactComposer.CaptureEmittedDocument(
            document,
            snapshot,
            identity,
            outcome,
            thirdParty);

        Assert.Equal("UYU", evidence.OriginalCurrencyCode);
        Assert.Equal("UYU", evidence.ReportingCurrencyCode);
        Assert.Null(evidence.CurrencyConversionEvidenceFingerprint);
        Assert.False(evidence.CurrencyConversionRequiresReliquidation);
        return evidence;
    }

    private static FiscalDailyReportDocumentEvidence ComposeForeign(
        long number,
        FiscalDailyReportFxRateSource rateSource,
        decimal rate,
        DateOnly rateSourceDate,
        bool requiresReliquidation = false)
    {
        var (document, snapshot) = Fixture(number, "USD");
        var identity = Identity(document, snapshot);
        var outcome = FiscalDailyReportDgiOutcomeEvidence.Capture(
            identity,
            FiscalDailyReportDgiCfeOutcome.Received,
            SigningTimestamp.AddMinutes(2),
            DgiResponseHash);
        var thirdParty = FiscalDailyReportThirdPartyPaymentEvidence.Capture(
            identity,
            null,
            ThirdPartySourceHash);
        var fx = FiscalDailyReportCurrencyConversionEvidence.Capture(
            identity,
            rateSource,
            rate,
            rateSourceDate,
            FxSourceHash,
            requiresReliquidation);

        var evidence = FiscalDailyReportForeignCurrencyComposer.CaptureEmittedDocument(
            document,
            snapshot,
            identity,
            outcome,
            thirdParty,
            fx);

        Assert.Equal(fx.EvidenceFingerprint, evidence.CurrencyConversionEvidenceFingerprint);
        return evidence;
    }

    private static FiscalDailyReportCfeIdentityEvidence Identity(
        FiscalDocument document,
        FiscalContentSnapshot snapshot) =>
        FiscalDailyReportCfeIdentityEvidence.Capture(
            document,
            snapshot,
            Guid.NewGuid(),
            Guid.NewGuid(),
            SigningTimestamp,
            snapshot.ContentFingerprint,
            SignedContentHash);

    private static (FiscalDocument Document, FiscalContentSnapshot Snapshot) Fixture(
        long number,
        string currency)
    {
        var saleId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var rule = new RegulatoryRuleEvidence(
            "TEST-DAILY-REPORT-FX-INTEGRATION",
            "DGI test evidence",
            "https://example.invalid/daily-report-fx-integration",
            "13.2-fx-integration-test",
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            "test clause");

        var calculation = new CfeArithmeticResult(
            currency,
            "25.2",
            "UY-CFE-25.2-ARITH-R1",
            [new CfeArithmeticLineResult(
                lineId,
                100m,
                VatLiabilityKind.VatDue,
                VatRateKind.Basic,
                22m,
                [rule],
                "UY-VAT-RATE-R1")],
            new CfeArithmeticTotals(100m, 0m, 100m, 0m, 0m, 22m, 22m, 122m),
            [rule]);

        var settlementEvidence = new FiscalSettlementEvidence(
            FiscalSettlementKind.ImmediatePayment,
            FiscalPaymentForm.Cash,
            null);
        var fiscal = FiscalConfirmationEvidence.Capture(
            CfeFamily.EFactura,
            ReceiverIdentificationRequirement.Required,
            "25.2",
            Confirmation,
            calculation,
            [rule],
            settlementEvidence);
        var issuer = new FiscalIssuerContentSnapshot(
            "214748364700",
            "Empresa Snapshot S.A.",
            "Empresa Snapshot",
            3,
            "loc-0001",
            "0001",
            "Av. Italia 1234",
            "Montevideo",
            "Montevideo",
            4);
        var receiver = new FiscalReceiverContentSnapshot(
            Guid.NewGuid(),
            "Cliente Snapshot S.A.",
            "UY",
            "UY",
            5,
            new FiscalReceiverIdentitySnapshot("2", "219999990017", "UY"),
            new FiscalReceiverAddressSnapshot(
                Guid.NewGuid(),
                FiscalReceiverAddressKind.Fiscal,
                "18 de Julio 2000",
                "Montevideo",
                "Montevideo",
                "UY",
                "11200"));

        var snapshot = FiscalContentSnapshot.Create(
            "company-1",
            saleId,
            CfeFamily.EFactura,
            ReceiverIdentificationRequirement.Required,
            "25.2",
            ReportDate,
            Confirmation,
            Settlement,
            issuer,
            receiver,
            [new FiscalContentLineSnapshot(
                1,
                lineId,
                Guid.NewGuid(),
                "ITEM-001",
                "Producto confirmado",
                FiscalContentLineKind.Goods,
                1m,
                100m,
                0m,
                0m,
                fiscal.Lines.Single(),
                "UNI")],
            fiscal);

        var document = FiscalDocument.CreateIdentity(
            Guid.NewGuid(),
            "company-1",
            Guid.NewGuid(),
            saleId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            CfeFamily.EFactura,
            "A",
            number,
            "90260000001",
            1,
            1000,
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            ReportDate,
            "loc-0001",
            "terminal-1",
            ReceiverIdentificationRequirement.Required,
            "25.2",
            Confirmation,
            Settlement,
            currency,
            100m,
            22m,
            122m,
            SigningTimestamp.AddMinutes(-10));

        return (document, snapshot);
    }
}
