using System.Text.Json;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Taxation;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalDailyReportFoundationTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Settlement = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private const string ReportingStatus = "cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc";
    private const string SignedContentHash = "dddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddddd";
    private const string ThirdPartyEvidence = "1212121212121212121212121212121212121212121212121212121212121212";
    private const string AnnulmentSource = "eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee";
    private static readonly DateOnly ReportDate = new(2026, 9, 10);
    private static readonly DateTimeOffset SigningTimestamp =
        new(2026, 9, 10, 10, 30, 45, TimeSpan.FromHours(-3));

    [Fact]
    public void Empty_calendar_day_is_a_valid_reconciliation_snapshot()
    {
        var report = FiscalDailyReportSnapshot.Create(
            "company-1",
            "214748364700",
            ReportDate,
            sequence: 1);

        Assert.Equal("13.2", report.FormatVersion);
        Assert.Equal(0, report.UsedCfeCount);
        Assert.Empty(report.Documents);
        Assert.Empty(report.Annulments);
        Assert.Empty(report.Summaries);
        Assert.Empty(report.Consumptions);
        report.EnsureIntegrity();
    }

    [Theory]
    [InlineData(CfeFamily.ETicket)]
    [InlineData(CfeFamily.ETicketCreditNote)]
    [InlineData(CfeFamily.ETicketDebitNote)]
    [InlineData(CfeFamily.EFactura)]
    [InlineData(CfeFamily.EFacturaCreditNote)]
    [InlineData(CfeFamily.EFacturaDebitNote)]
    public void Accepted_domestic_families_can_be_frozen_for_daily_report(CfeFamily family)
    {
        var evidence = Evidence(family, number: 10);

        Assert.Equal(family, evidence.CfeType);
        evidence.EnsureIntegrity();
    }

    [Fact]
    public void Monetary_summaries_are_grouped_by_type_document_date_branch_and_third_party_indicator()
    {
        var first = Evidence(CfeFamily.ETicket, number: 10, branch: "0001");
        var second = Evidence(CfeFamily.ETicket, number: 11, branch: "0001");
        var thirdParty = Evidence(
            CfeFamily.ETicket,
            number: 12,
            branch: "0001",
            paymentOnBehalfOfThirdParty: true);
        var otherBranch = Evidence(CfeFamily.ETicket, number: 13, branch: "0002");

        var report = FiscalDailyReportSnapshot.Create(
            "company-1",
            "214748364700",
            ReportDate,
            1,
            [otherBranch, thirdParty, second, first]);

        Assert.Equal(3, report.Summaries.Count);

        var ordinary = report.Summaries.Single(summary =>
            summary.DgiBranchCode == "0001" && !summary.PaymentOnBehalfOfThirdParty);
        Assert.Equal(2, ordinary.DocumentCount);
        Assert.Equal(200m, ordinary.NetAmount);
        Assert.Equal(200m, ordinary.BasicTaxableAmount);
        Assert.Equal(44m, ordinary.VatAmount);
        Assert.Equal(244m, ordinary.TotalAmount);

        Assert.Single(report.Summaries.Where(summary =>
            summary.DgiBranchCode == "0001" && summary.PaymentOnBehalfOfThirdParty));
        Assert.Single(report.Summaries.Where(summary => summary.DgiBranchCode == "0002"));
    }

    [Fact]
    public void Used_ranges_include_emitted_and_explicit_annulled_numbers_without_inference()
    {
        var emitted10 = Evidence(CfeFamily.ETicket, number: 10);
        var emitted12 = Evidence(CfeFamily.ETicket, number: 12);
        var annulled11 = FiscalDailyReportAnnulmentEvidence.Capture(
            "company-1",
            "214748364700",
            CfeFamily.ETicket,
            "A",
            11,
            ReportDate,
            FiscalDailyReportAnnulmentKind.CompanyAnnulment,
            AnnulmentSource);

        var report = FiscalDailyReportSnapshot.Create(
            "company-1",
            "214748364700",
            ReportDate,
            1,
            [emitted12, emitted10],
            [annulled11]);

        var consumption = Assert.Single(report.Consumptions);
        Assert.Equal(3, consumption.UsedCount);
        Assert.Equal(2, consumption.EmittedCount);
        Assert.Equal(1, consumption.AnnulledCount);

        var used = Assert.Single(consumption.UsedRanges);
        Assert.Equal("A", used.Series);
        Assert.Equal(10, used.From);
        Assert.Equal(12, used.To);

        var annulled = Assert.Single(consumption.AnnulledRanges);
        Assert.Equal(11, annulled.From);
        Assert.Equal(11, annulled.To);
    }

    [Fact]
    public void Numbering_gaps_are_not_synthesized_as_annulments()
    {
        var emitted10 = Evidence(CfeFamily.ETicket, number: 10);
        var emitted12 = Evidence(CfeFamily.ETicket, number: 12);

        var report = FiscalDailyReportSnapshot.Create(
            "company-1",
            "214748364700",
            ReportDate,
            1,
            [emitted10, emitted12]);

        var consumption = Assert.Single(report.Consumptions);
        Assert.Equal(2, consumption.UsedCount);
        Assert.Equal(0, consumption.AnnulledCount);
        Assert.Equal(2, consumption.UsedRanges.Count);
        Assert.Empty(consumption.AnnulledRanges);
    }

    [Fact]
    public void Dgi_rejection_annulment_requires_rejected_document_and_signed_artifact_identity()
    {
        var error = Assert.Throws<DomainRuleException>(() =>
            FiscalDailyReportAnnulmentEvidence.Capture(
                "company-1",
                "214748364700",
                CfeFamily.EFactura,
                "A",
                77,
                ReportDate,
                FiscalDailyReportAnnulmentKind.DgiRejection,
                AnnulmentSource));

        Assert.Equal("fiscal.daily_report.dgi_rejection_artifact_required", error.Code);
    }

    [Fact]
    public void Same_identity_cannot_be_both_emitted_and_annulled()
    {
        var emitted = Evidence(CfeFamily.ETicket, number: 10);
        var annulled = FiscalDailyReportAnnulmentEvidence.Capture(
            "company-1",
            "214748364700",
            CfeFamily.ETicket,
            "A",
            10,
            ReportDate,
            FiscalDailyReportAnnulmentKind.CompanyAnnulment,
            AnnulmentSource);

        var error = Assert.Throws<DomainRuleException>(() =>
            FiscalDailyReportSnapshot.Create(
                "company-1",
                "214748364700",
                ReportDate,
                1,
                [emitted],
                [annulled]));

        Assert.Equal("fiscal.daily_report.emitted_annulled_overlap", error.Code);
    }

    [Fact]
    public void Foreign_currency_fails_closed_until_fiscal_conversion_evidence_is_frozen()
    {
        var (document, snapshot) = Fixture(
            CfeFamily.EFactura,
            number: 10,
            currency: "USD");

        var error = Assert.Throws<DomainRuleException>(() =>
            FiscalDailyReportDocumentEvidence.Capture(
                document,
                snapshot,
                Guid.NewGuid(),
                Guid.NewGuid(),
                SigningTimestamp,
                snapshot.ContentFingerprint,
                SignedContentHash,
                ReportingStatus,
                paymentOnBehalfOfThirdParty: false,
                thirdPartyPaymentEvidenceFingerprint: ThirdPartyEvidence));

        Assert.Equal("fiscal.daily_report.foreign_currency_conversion_evidence_required", error.Code);
    }

    [Fact]
    public void Report_requires_the_CFE_advanced_signature_date_to_match_summary_date()
    {
        var evidence = Evidence(
            CfeFamily.ETicket,
            number: 10,
            signingTimestamp: SigningTimestamp.AddDays(-1));

        var error = Assert.Throws<DomainRuleException>(() =>
            FiscalDailyReportSnapshot.Create(
                "company-1",
                "214748364700",
                ReportDate,
                1,
                [evidence]));

        Assert.Equal("fiscal.daily_report.signing_date_mismatch", error.Code);
    }

    [Fact]
    public void Signed_artifact_fingerprint_must_match_frozen_fiscal_content()
    {
        var (document, snapshot) = Fixture(CfeFamily.EFactura, number: 10);

        var error = Assert.Throws<DomainRuleException>(() =>
            FiscalDailyReportDocumentEvidence.Capture(
                document,
                snapshot,
                Guid.NewGuid(),
                Guid.NewGuid(),
                SigningTimestamp,
                "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff",
                SignedContentHash,
                ReportingStatus,
                false,
                ThirdPartyEvidence));

        Assert.Equal("fiscal.daily_report.artifact_snapshot_fingerprint_mismatch", error.Code);
    }

    [Fact]
    public void Export_family_remains_fail_closed()
    {
        var (document, snapshot) = Fixture(CfeFamily.EFacturaExportacion, number: 10);

        var error = Assert.Throws<DomainRuleException>(() =>
            FiscalDailyReportDocumentEvidence.Capture(
                document,
                snapshot,
                Guid.NewGuid(),
                Guid.NewGuid(),
                SigningTimestamp,
                snapshot.ContentFingerprint,
                SignedContentHash,
                ReportingStatus,
                false,
                ThirdPartyEvidence));

        Assert.Equal("fiscal.daily_report.cfe_family_not_supported", error.Code);
    }

    [Fact]
    public void Snapshot_is_deterministic_and_survives_JSON_roundtrip()
    {
        var doc11 = Evidence(CfeFamily.ETicket, 11);
        var doc10 = Evidence(CfeFamily.ETicket, 10);
        var annulled12 = FiscalDailyReportAnnulmentEvidence.Capture(
            "company-1",
            "214748364700",
            CfeFamily.ETicket,
            "A",
            12,
            ReportDate,
            FiscalDailyReportAnnulmentKind.CompanyAnnulment,
            AnnulmentSource);

        var first = FiscalDailyReportSnapshot.Create(
            "company-1",
            "214748364700",
            ReportDate,
            2,
            [doc11, doc10],
            [annulled12]);
        var second = FiscalDailyReportSnapshot.Create(
            "company-1",
            "214748364700",
            ReportDate,
            2,
            [doc10, doc11],
            [annulled12]);

        Assert.Equal(first.ReconciliationFingerprint, second.ReconciliationFingerprint);
        Assert.True(first.Summaries.SequenceEqual(second.Summaries));
        Assert.True(first.Consumptions.Single().UsedRanges.SequenceEqual(second.Consumptions.Single().UsedRanges));

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var json = JsonSerializer.Serialize(first, options);
        var roundTrip = JsonSerializer.Deserialize<FiscalDailyReportSnapshot>(json, options);

        Assert.NotNull(roundTrip);
        roundTrip!.EnsureIntegrity();
        Assert.Equal(first.ReconciliationFingerprint, roundTrip.ReconciliationFingerprint);
    }

    [Fact]
    public void Tampered_reconciliation_fingerprint_is_rejected()
    {
        var report = FiscalDailyReportSnapshot.Create(
            "company-1",
            "214748364700",
            ReportDate,
            1,
            [Evidence(CfeFamily.ETicket, 10)]);

        var tampered = report with
        {
            ReconciliationFingerprint =
                "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"
        };

        var error = Assert.Throws<DomainRuleException>(tampered.EnsureIntegrity);
        Assert.Equal("fiscal.daily_report.reconciliation_fingerprint_mismatch", error.Code);
    }

    private static FiscalDailyReportDocumentEvidence Evidence(
        CfeFamily family,
        long number,
        string branch = "0001",
        bool paymentOnBehalfOfThirdParty = false,
        DateTimeOffset? signingTimestamp = null)
    {
        var (document, snapshot) = Fixture(family, number, branch);
        return FiscalDailyReportDocumentEvidence.Capture(
            document,
            snapshot,
            Guid.NewGuid(),
            Guid.NewGuid(),
            signingTimestamp ?? SigningTimestamp,
            snapshot.ContentFingerprint,
            SignedContentHash,
            ReportingStatus,
            paymentOnBehalfOfThirdParty,
            ThirdPartyEvidence);
    }

    private static (FiscalDocument Document, FiscalContentSnapshot Snapshot) Fixture(
        CfeFamily family,
        long number,
        string branch = "0001",
        string currency = "UYU")
    {
        var saleId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var rule = new RegulatoryRuleEvidence(
            "TEST-DAILY-REPORT",
            "DGI test evidence",
            "https://example.invalid/daily-report-rule",
            "13.2-test",
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
        var settlementEvidence = new FiscalSettlementEvidence(
            FiscalSettlementKind.ImmediatePayment,
            FiscalPaymentForm.Cash,
            null);
        var fiscal = FiscalConfirmationEvidence.Capture(
            family,
            receiverRequirement,
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

        IReadOnlyCollection<FiscalDocumentReferenceEvidence>? references = null;
        if (family is CfeFamily.ETicketCreditNote
            or CfeFamily.ETicketDebitNote
            or CfeFamily.EFacturaCreditNote
            or CfeFamily.EFacturaDebitNote)
        {
            references =
            [
                FiscalDocumentReferenceEvidence.Capture(
                    1,
                    "company-1",
                    ticketFamily ? CfeFamily.ETicket : CfeFamily.EFactura,
                    "B",
                    77,
                    new DateOnly(2026, 9, 9),
                    122m,
                    currency,
                    exchangeRate: currency == "UYU" ? null : 39.5m,
                    reason: "Corrección")
            ];
        }

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
