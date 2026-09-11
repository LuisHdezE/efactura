using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Taxation;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalDailyReportHighValueAutomationTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Settlement = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string SignedContentHash = "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd";
    private const string DgiResponseHash = "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee";
    private const string ThirdPartySourceHash = "1212121212121212121212121212121212121212121212121212121212121212";
    private const string FxSourceHash = "3434343434343434343434343434343434343434343434343434343434343434";
    private const string BcuSourceHash = "7878787878787878787878787878787878787878787878787878787878787878";
    private const string BcuCotizaciones = "https://www.bcu.gub.uy/Estadisticas-e-Indicadores/Paginas/Cotizaciones.aspx";
    private static readonly DateOnly ReportDate = new(2026, 9, 10);
    private static readonly DateTimeOffset SigningTimestamp =
        new(2026, 9, 10, 10, 30, 45, TimeSpan.FromHours(-3));

    [Fact]
    public void Annual_UI_quote_requires_December_31_of_previous_year()
    {
        var error = Assert.Throws<DomainRuleException>(() =>
            FiscalDailyReportAnnualUiQuoteEvidence.Capture(
                ReportDate,
                new DateOnly(2025, 12, 30),
                6m,
                BcuCotizaciones,
                BcuSourceHash));

        Assert.Equal("fiscal.daily_report.wire.ui_quote_date_invalid", error.Code);

        var quote = Quote();
        Assert.Equal(2026, quote.ApplicableYear);
        Assert.Equal(new DateOnly(2025, 12, 31), quote.QuoteDate);
        Assert.Equal(5_000m, quote.ThresholdUi);
        Assert.Equal(30_000m, quote.ThresholdUyu);
        quote.EnsureAppliesTo(ReportDate);
    }

    [Fact]
    public void Automatic_B_C27_uses_strictly_greater_than_5000_UI()
    {
        var exact = ReportDocument(CfeFamily.ETicket, 10, 30_000m);
        var above = ReportDocument(CfeFamily.ETicket, 11, 30_006m);
        var report = FiscalDailyReportSnapshot.Create(
            "company-1",
            "214748364700",
            ReportDate,
            1,
            [exact.DocumentEvidence, above.DocumentEvidence]);

        var first = FiscalDailyReportHighValueCounterComposer.Capture(report, Quote());
        var second = FiscalDailyReportHighValueCounterComposer.Capture(report, Quote());

        var counter = Assert.Single(first);
        Assert.Equal(CfeFamily.ETicket, counter.CfeType);
        Assert.Equal(1, counter.Count);
        Assert.Equal(counter.EvidenceFingerprint, Assert.Single(second).EvidenceFingerprint);
    }

    [Fact]
    public void Automatic_B_C27_is_partitioned_by_eTicket_family()
    {
        var ticket = ReportDocument(CfeFamily.ETicket, 10, 30_006m);
        var credit = ReportDocument(CfeFamily.ETicketCreditNote, 20, 30_012m);
        var debit = ReportDocument(CfeFamily.ETicketDebitNote, 30, 29_994m);
        var invoice = ReportDocument(CfeFamily.EFactura, 40, 90_000m);
        var report = FiscalDailyReportSnapshot.Create(
            "company-1",
            "214748364700",
            ReportDate,
            1,
            [ticket.DocumentEvidence, credit.DocumentEvidence, debit.DocumentEvidence, invoice.DocumentEvidence]);

        var counters = FiscalDailyReportHighValueCounterComposer.Capture(report, Quote())
            .OrderBy(item => (int)item.CfeType)
            .ToArray();

        Assert.Equal(3, counters.Length);
        Assert.Equal(1, counters.Single(item => item.CfeType == CfeFamily.ETicket).Count);
        Assert.Equal(1, counters.Single(item => item.CfeType == CfeFamily.ETicketCreditNote).Count);
        Assert.Equal(0, counters.Single(item => item.CfeType == CfeFamily.ETicketDebitNote).Count);
        Assert.DoesNotContain(counters, item => item.CfeType == CfeFamily.EFactura);
    }

    [Fact]
    public void Automatic_B_C27_feeds_wire_projection_without_manual_counter()
    {
        var source = ReportDocument(CfeFamily.ETicket, 10, 30_006m);
        var report = FiscalDailyReportSnapshot.Create(
            "company-1",
            "214748364700",
            ReportDate,
            1,
            [source.DocumentEvidence]);
        var rates = FiscalDailyReportVatRateEvidence.Capture(source.Identity, source.ContentSnapshot);
        var highValue = FiscalDailyReportHighValueCounterComposer.Capture(report, Quote());

        var projection = FiscalDailyReportWireReadinessProjector.Project(report, [rates], highValue);

        Assert.Equal(1, Assert.Single(projection.Counters).HighValueCount);
    }

    [Fact]
    public void Foreign_currency_eTicket_remains_fail_closed_for_B_C27_conversion_semantics()
    {
        var (document, snapshot) = Fixture(CfeFamily.ETicket, 10, 1_000m, "USD");
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
            40m,
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

        var error = Assert.Throws<DomainRuleException>(() =>
            FiscalDailyReportHighValueCounterComposer.Capture(report, Quote()));

        Assert.Equal("fiscal.daily_report.wire.high_value_foreign_currency_policy_required", error.Code);
    }

    private static FiscalDailyReportAnnualUiQuoteEvidence Quote() =>
        FiscalDailyReportAnnualUiQuoteEvidence.Capture(
            ReportDate,
            new DateOnly(2025, 12, 31),
            6m,
            BcuCotizaciones,
            BcuSourceHash);

    private static ReportSource ReportDocument(CfeFamily family, long number, decimal netAmount)
    {
        var (document, snapshot) = Fixture(family, number, netAmount);
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
        return new ReportSource(identity, snapshot, evidence);
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
        decimal netAmount,
        string currency = "UYU")
    {
        var saleId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var vatAmount = netAmount * 22m / 100m;
        var totalAmount = netAmount + vatAmount;
        var rule = new RegulatoryRuleEvidence(
            "TEST-DAILY-REPORT-HIGH-VALUE",
            "DGI test evidence",
            "https://example.invalid/daily-report-high-value",
            "13.2-high-value-test",
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            "test clause");

        var calculation = new CfeArithmeticResult(
            currency,
            "25.2",
            "UY-CFE-25.2-ARITH-R1",
            [new CfeArithmeticLineResult(
                lineId,
                netAmount,
                VatLiabilityKind.VatDue,
                VatRateKind.Basic,
                22m,
                [rule],
                "UY-VAT-RATE-R1")],
            new CfeArithmeticTotals(netAmount, 0m, netAmount, 0m, 0m, vatAmount, vatAmount, totalAmount),
            [rule]);

        var ticketFamily = family is CfeFamily.ETicket or CfeFamily.ETicketCreditNote or CfeFamily.ETicketDebitNote;
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
            "loc-0001",
            "0001",
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

        IReadOnlyCollection<FiscalDocumentReferenceEvidence>? references = null;
        if (family is CfeFamily.ETicketCreditNote or CfeFamily.ETicketDebitNote)
        {
            references =
            [
                FiscalDocumentReferenceEvidence.Capture(
                    1,
                    "company-1",
                    CfeFamily.ETicket,
                    "B",
                    77,
                    new DateOnly(2026, 9, 9),
                    totalAmount,
                    currency,
                    exchangeRate: currency == "UYU" ? null : 40m,
                    reason: "Corrección")
            ];
        }

        var contentSnapshot = FiscalContentSnapshot.Create(
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
                netAmount,
                0m,
                0m,
                fiscal.Lines.Single(),
                "UNI")],
            fiscal,
            references);

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
            "loc-0001",
            "terminal-1",
            receiverRequirement,
            "25.2",
            Confirmation,
            Settlement,
            currency,
            netAmount,
            vatAmount,
            totalAmount,
            SigningTimestamp.AddMinutes(-10));

        return (document, contentSnapshot);
    }

    private sealed record ReportSource(
        FiscalDailyReportCfeIdentityEvidence Identity,
        FiscalContentSnapshot ContentSnapshot,
        FiscalDailyReportDocumentEvidence DocumentEvidence);
}
