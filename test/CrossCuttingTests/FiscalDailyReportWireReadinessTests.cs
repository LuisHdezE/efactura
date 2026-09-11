using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Taxation;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalDailyReportWireReadinessTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Settlement = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string SignedContentHash = "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd";
    private const string DgiResponseHash = "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee";
    private const string ThirdPartySourceHash = "1212121212121212121212121212121212121212121212121212121212121212";
    private const string FxSourceHash = "3434343434343434343434343434343434343434343434343434343434343434";
    private const string HighValueSourceHash = "5656565656565656565656565656565656565656565656565656565656565656";
    private static readonly DateOnly ReportDate = new(2026, 9, 10);
    private static readonly DateTimeOffset SigningTimestamp =
        new(2026, 9, 10, 10, 30, 45, TimeSpan.FromHours(-3));

    [Fact]
    public void Vat_rate_evidence_is_bound_to_signed_CFE_and_preserves_basic_rate()
    {
        var (document, snapshot) = Fixture(CfeFamily.EFactura, 10);
        var identity = Identity(document, snapshot);

        var rates = FiscalDailyReportVatRateEvidence.Capture(identity, snapshot);

        Assert.Null(rates.MinimumVatRatePercent);
        Assert.Equal(22m, rates.BasicVatRatePercent);
        Assert.Equal(document.Id, rates.FiscalDocumentId);
        Assert.Equal(identity.EvidenceFingerprint, rates.CfeIdentityFingerprint);
        rates.EnsureIntegrity();
    }

    [Fact]
    public void Uyu_efactura_projects_complete_release1_wire_semantics_without_rounding()
    {
        var (document, snapshot) = Fixture(CfeFamily.EFactura, 10);
        var identity = Identity(document, snapshot);
        var reportDocument = Emitted(document, snapshot, identity);
        var report = FiscalDailyReportSnapshot.Create(
            "company-1",
            "214748364700",
            ReportDate,
            1,
            [reportDocument]);
        var rates = FiscalDailyReportVatRateEvidence.Capture(identity, snapshot);

        var first = FiscalDailyReportWireReadinessProjector.Project(report, [rates], []);
        var second = FiscalDailyReportWireReadinessProjector.Project(report, [rates], []);

        var row = Assert.Single(first.AmountRows);
        Assert.Equal(0m, row.PerceivedTaxAmount);
        Assert.Equal(0m, row.VatInSuspenseAmount);
        Assert.Equal(0m, row.OtherVatTaxableAmount);
        Assert.Equal(0m, row.OtherVatAmount);
        Assert.Equal(0m, row.RetainedOrPerceivedAmount);
        Assert.Equal(0m, row.FiscalCreditAmount);
        Assert.Null(row.MinimumVatRatePercent);
        Assert.Equal(22m, row.BasicVatRatePercent);
        Assert.Equal(100m, row.BasicTaxableAmount);
        Assert.Equal(22m, row.BasicVatAmount);
        Assert.Equal(122m, row.TotalAmount);

        var counter = Assert.Single(first.Counters);
        Assert.Equal(CfeFamily.EFactura, counter.CfeType);
        Assert.Equal(1, counter.UsedCount);
        Assert.Equal(1, counter.EmittedCount);
        Assert.Equal(0, counter.AnnulledCount);
        Assert.Equal(0, counter.HighValueCount);
        Assert.Equal(first.ProjectionFingerprint, second.ProjectionFingerprint);
    }

    [Fact]
    public void Eticket_family_requires_explicit_B_C27_source_evidence()
    {
        var (document, snapshot) = Fixture(CfeFamily.ETicket, 10);
        var identity = Identity(document, snapshot);
        var reportDocument = Emitted(document, snapshot, identity);
        var report = FiscalDailyReportSnapshot.Create(
            "company-1",
            "214748364700",
            ReportDate,
            1,
            [reportDocument]);
        var rates = FiscalDailyReportVatRateEvidence.Capture(identity, snapshot);

        var missing = Assert.Throws<DomainRuleException>(() =>
            FiscalDailyReportWireReadinessProjector.Project(report, [rates], []));
        Assert.Equal("fiscal.daily_report.wire.high_value_evidence_required", missing.Code);

        var highValue = FiscalDailyReportHighValueCounterEvidence.Capture(
            CfeFamily.ETicket,
            1,
            HighValueSourceHash);
        var projected = FiscalDailyReportWireReadinessProjector.Project(report, [rates], [highValue]);

        Assert.Equal(1, Assert.Single(projected.Counters).HighValueCount);
    }

    [Fact]
    public void B_C27_cannot_exceed_emitted_population()
    {
        var (document, snapshot) = Fixture(CfeFamily.ETicket, 10);
        var identity = Identity(document, snapshot);
        var report = FiscalDailyReportSnapshot.Create(
            "company-1",
            "214748364700",
            ReportDate,
            1,
            [Emitted(document, snapshot, identity)]);
        var rates = FiscalDailyReportVatRateEvidence.Capture(identity, snapshot);
        var highValue = FiscalDailyReportHighValueCounterEvidence.Capture(
            CfeFamily.ETicket,
            2,
            HighValueSourceHash);

        var error = Assert.Throws<DomainRuleException>(() =>
            FiscalDailyReportWireReadinessProjector.Project(report, [rates], [highValue]));

        Assert.Equal("fiscal.daily_report.wire.high_value_count_exceeds_emitted", error.Code);
    }

    [Fact]
    public void Foreign_currency_projection_fails_closed_when_exact_UYU_values_need_quantization()
    {
        var (document, snapshot) = Fixture(CfeFamily.EFactura, 10, currency: "USD");
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
            FiscalDailyReportFxRateSource.BcuFiscalQuotation,
            39.1234567m,
            new DateOnly(2026, 9, 9),
            FxSourceHash);
        var reportDocument = FiscalDailyReportForeignCurrencyComposer.CaptureEmittedDocument(
            document,
            snapshot,
            identity,
            outcome,
            thirdParty,
            fx);
        var report = FiscalDailyReportSnapshot.Create(
            "company-1",
            "214748364700",
            ReportDate,
            1,
            [reportDocument]);
        var rates = FiscalDailyReportVatRateEvidence.Capture(identity, snapshot);

        var error = Assert.Throws<DomainRuleException>(() =>
            FiscalDailyReportWireReadinessProjector.Project(report, [rates], []));

        Assert.Equal("fiscal.daily_report.wire.quantization_required", error.Code);
    }

    [Fact]
    public void Vat_rate_evidence_tampering_is_detected_even_with_valid_shape()
    {
        var (document, snapshot) = Fixture(CfeFamily.EFactura, 10);
        var identity = Identity(document, snapshot);
        var rates = FiscalDailyReportVatRateEvidence.Capture(identity, snapshot);
        var tampered = rates with { BasicVatRatePercent = 10m };

        var error = Assert.Throws<DomainRuleException>(tampered.EnsureIntegrity);

        Assert.Equal("fiscal.daily_report.wire.vat_rate_evidence_fingerprint_mismatch", error.Code);
    }

    private static FiscalDailyReportDocumentEvidence Emitted(
        FiscalDocument document,
        FiscalContentSnapshot snapshot,
        FiscalDailyReportCfeIdentityEvidence identity)
    {
        var outcome = FiscalDailyReportDgiOutcomeEvidence.Capture(
            identity,
            FiscalDailyReportDgiCfeOutcome.Received,
            SigningTimestamp.AddMinutes(2),
            DgiResponseHash);
        var thirdParty = FiscalDailyReportThirdPartyPaymentEvidence.Capture(
            identity,
            null,
            ThirdPartySourceHash);
        return FiscalDailyReportSourceFactComposer.CaptureEmittedDocument(
            document,
            snapshot,
            identity,
            outcome,
            thirdParty);
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
        CfeFamily family,
        long number,
        string branch = "0001",
        string currency = "UYU")
    {
        var saleId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var rule = new RegulatoryRuleEvidence(
            "TEST-DAILY-REPORT-WIRE-READINESS",
            "DGI test evidence",
            "https://example.invalid/daily-report-wire-readiness",
            "13.2-wire-readiness-test",
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

        var ticketFamily = family is
            CfeFamily.ETicket or
            CfeFamily.ETicketCreditNote or
            CfeFamily.ETicketDebitNote;
        var receiverRequirement = ticketFamily
            ? ReceiverIdentificationRequirement.Optional
            : ReceiverIdentificationRequirement.Required;
        var fiscal = FiscalConfirmationEvidence.Capture(
            family,
            receiverRequirement,
            "25.2",
            Confirmation,
            calculation,
            [rule],
            new FiscalSettlementEvidence(FiscalSettlementKind.ImmediatePayment, FiscalPaymentForm.Cash, null));
        var issuer = new FiscalIssuerContentSnapshot(
            "214748364700",
            "Empresa Snapshot S.A.",
            "Empresa Snapshot",
            3,
            $"loc-{branch}",
            branch,
            "Av. Italia 1234",
            "Montevideo",
            "Montevideo",
            4);
        var receiver = ticketFamily ? null : new FiscalReceiverContentSnapshot(
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
            family,
            receiverRequirement,
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
            family,
            "A",
            number,
            "90260000001",
            1,
            1000,
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            ReportDate,
            $"loc-{branch}",
            "terminal-1",
            receiverRequirement,
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
