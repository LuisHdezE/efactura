using System.Text.Json;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Taxation;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalDailyReportSourceFactsTests
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
    public void Signed_CFE_identity_is_bound_to_document_snapshot_and_artifact()
    {
        var (document, snapshot) = Fixture(CfeFamily.EFactura, 10);
        var artifactId = Guid.NewGuid();
        var signingId = Guid.NewGuid();

        var first = FiscalDailyReportCfeIdentityEvidence.Capture(
            document,
            snapshot,
            artifactId,
            signingId,
            SigningTimestamp,
            SignedContentHash);
        var second = FiscalDailyReportCfeIdentityEvidence.Capture(
            document,
            snapshot,
            artifactId,
            signingId,
            SigningTimestamp,
            SignedContentHash);

        Assert.Equal(first.EvidenceFingerprint, second.EvidenceFingerprint);
        Assert.Equal(snapshot.ContentFingerprint, first.FiscalContentFingerprint);
        Assert.Equal("UYU", first.CurrencyCode);
        first.EnsureIntegrity();
    }

    [Theory]
    [InlineData(FiscalDailyReportDgiCfeOutcome.Received, "AE")]
    [InlineData(FiscalDailyReportDgiCfeOutcome.Rejected, "BE")]
    public void Dgi_outcome_freezes_current_v19_status_code(
        FiscalDailyReportDgiCfeOutcome outcome,
        string expectedCode)
    {
        var identity = Identity(CfeFamily.ETicket, 10);

        var evidence = FiscalDailyReportDgiOutcomeEvidence.Capture(
            identity,
            outcome,
            SigningTimestamp.AddMinutes(2),
            DgiResponseHash);

        Assert.Equal(expectedCode, evidence.DgiStatusCode);
        Assert.Equal(outcome == FiscalDailyReportDgiCfeOutcome.Received, evidence.IsEligibleForEmittedPopulation);
        Assert.Equal(outcome == FiscalDailyReportDgiCfeOutcome.Rejected, evidence.IsDgiRejection);
        evidence.EnsureIntegrity();
    }

    [Fact]
    public void Later_Dgi_outcome_can_preserve_immutable_supersession_chain()
    {
        var identity = Identity(CfeFamily.ETicket, 10);
        var received = FiscalDailyReportDgiOutcomeEvidence.Capture(
            identity,
            FiscalDailyReportDgiCfeOutcome.Received,
            SigningTimestamp.AddMinutes(1),
            DgiResponseHash);

        var rejected = FiscalDailyReportDgiOutcomeEvidence.Capture(
            identity,
            FiscalDailyReportDgiCfeOutcome.Rejected,
            SigningTimestamp.AddMinutes(5),
            "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff",
            received.EvidenceFingerprint);

        Assert.Equal(received.EvidenceFingerprint, rejected.SupersedesEvidenceFingerprint);
        Assert.Equal("BE", rejected.DgiStatusCode);
        rejected.EnsureIntegrity();
    }

    [Fact]
    public void A19_source_preserves_absence_and_exact_value_one_without_boolean_coercion()
    {
        var identity = Identity(CfeFamily.EFactura, 10);

        var absent = FiscalDailyReportThirdPartyPaymentEvidence.Capture(
            identity,
            null,
            ThirdPartySourceHash);
        var present = FiscalDailyReportThirdPartyPaymentEvidence.Capture(
            identity,
            1,
            ThirdPartySourceHash);

        Assert.False(absent.PaymentOnBehalfOfThirdParty);
        Assert.True(present.PaymentOnBehalfOfThirdParty);
        Assert.NotEqual(absent.EvidenceFingerprint, present.EvidenceFingerprint);

        var error = Assert.Throws<DomainRuleException>(() =>
            FiscalDailyReportThirdPartyPaymentEvidence.Capture(
                identity,
                2,
                ThirdPartySourceHash));
        Assert.Equal("fiscal.daily_report.third_party.a19_invalid", error.Code);
    }

    [Fact]
    public void Typed_received_and_A19_evidence_compose_current_UYU_document_evidence()
    {
        var (document, snapshot) = Fixture(CfeFamily.EFactura, 10);
        var identity = Identity(document, snapshot);
        var outcome = FiscalDailyReportDgiOutcomeEvidence.Capture(
            identity,
            FiscalDailyReportDgiCfeOutcome.Received,
            SigningTimestamp.AddMinutes(2),
            DgiResponseHash);
        var thirdParty = FiscalDailyReportThirdPartyPaymentEvidence.Capture(
            identity,
            1,
            ThirdPartySourceHash);

        var evidence = FiscalDailyReportSourceFactComposer.CaptureEmittedDocument(
            document,
            snapshot,
            identity,
            outcome,
            thirdParty);

        Assert.True(evidence.PaymentOnBehalfOfThirdParty);
        Assert.Equal(outcome.EvidenceFingerprint, evidence.ReportingStatusEvidenceFingerprint);
        Assert.Equal(thirdParty.EvidenceFingerprint, evidence.ThirdPartyPaymentEvidenceFingerprint);
        evidence.EnsureIntegrity();
    }

    [Fact]
    public void Rejected_Dgi_outcome_cannot_enter_emitted_population_and_can_create_explicit_annulment()
    {
        var (document, snapshot) = Fixture(CfeFamily.ETicket, 10);
        var identity = Identity(document, snapshot);
        var rejected = FiscalDailyReportDgiOutcomeEvidence.Capture(
            identity,
            FiscalDailyReportDgiCfeOutcome.Rejected,
            SigningTimestamp.AddMinutes(3),
            DgiResponseHash);
        var thirdParty = FiscalDailyReportThirdPartyPaymentEvidence.Capture(
            identity,
            null,
            ThirdPartySourceHash);

        var emittedError = Assert.Throws<DomainRuleException>(() =>
            FiscalDailyReportSourceFactComposer.CaptureEmittedDocument(
                document,
                snapshot,
                identity,
                rejected,
                thirdParty));
        Assert.Equal("fiscal.daily_report.source_facts.received_outcome_required", emittedError.Code);

        var annulment = FiscalDailyReportSourceFactComposer.CaptureDgiRejectedAnnulment(
            identity,
            rejected);

        Assert.Equal(FiscalDailyReportAnnulmentKind.DgiRejection, annulment.Kind);
        Assert.Equal(document.Id, annulment.FiscalDocumentId);
        Assert.Equal(identity.SignedArtifactId, annulment.SignedArtifactId);
        Assert.Equal(rejected.EvidenceFingerprint, annulment.SourceEvidenceFingerprint);
        annulment.EnsureIntegrity();
    }

    [Fact]
    public void Bcu_fiscal_rate_preserves_all_decimal_digits_for_deterministic_conversion()
    {
        var identity = Identity(CfeFamily.EFactura, 10, currency: "USD");

        var fx = FiscalDailyReportCurrencyConversionEvidence.Capture(
            identity,
            FiscalDailyReportFxRateSource.BcuFiscalQuotation,
            39.1234567m,
            new DateOnly(2026, 9, 9),
            FxSourceHash);

        Assert.Equal(3912.3456700m, fx.ConvertToUyu(100m));
        Assert.False(fx.RequiresReliquidation);
        fx.EnsureIntegrity();
    }

    [Fact]
    public void Domestic_correction_note_requires_exchange_rate_from_correction_CFE()
    {
        var identity = Identity(CfeFamily.EFacturaCreditNote, 10, currency: "USD");

        var error = Assert.Throws<DomainRuleException>(() =>
            FiscalDailyReportCurrencyConversionEvidence.Capture(
                identity,
                FiscalDailyReportFxRateSource.BcuFiscalQuotation,
                39.5m,
                new DateOnly(2026, 9, 9),
                FxSourceHash));

        Assert.Equal("fiscal.daily_report.fx.correction_rate_source_required", error.Code);

        var accepted = FiscalDailyReportCurrencyConversionEvidence.Capture(
            identity,
            FiscalDailyReportFxRateSource.CorrectionCfeRate,
            39.5m,
            ReportDate,
            FxSourceHash);
        accepted.EnsureIntegrity();
    }

    [Fact]
    public void Future_date_CFE_rate_fallback_requires_reliquidation_marker()
    {
        var identity = Identity(CfeFamily.EFactura, 10, currency: "USD");

        var error = Assert.Throws<DomainRuleException>(() =>
            FiscalDailyReportCurrencyConversionEvidence.Capture(
                identity,
                FiscalDailyReportFxRateSource.FutureDateCfeRate,
                39.5m,
                ReportDate,
                FxSourceHash));

        Assert.Equal("fiscal.daily_report.fx.reliquidation_marker_required", error.Code);

        var accepted = FiscalDailyReportCurrencyConversionEvidence.Capture(
            identity,
            FiscalDailyReportFxRateSource.FutureDateCfeRate,
            39.5m,
            ReportDate,
            FxSourceHash,
            requiresReliquidation: true);
        Assert.True(accepted.RequiresReliquidation);
    }

    [Fact]
    public void Foreign_currency_source_facts_are_frozen_but_composition_remains_fail_closed()
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

        var error = Assert.Throws<DomainRuleException>(() =>
            FiscalDailyReportSourceFactComposer.CaptureEmittedDocument(
                document,
                snapshot,
                identity,
                outcome,
                thirdParty,
                fx));

        Assert.Equal("fiscal.daily_report.source_facts.foreign_currency_integration_required", error.Code);
    }

    [Fact]
    public void Source_fact_records_survive_JSON_roundtrip_and_reject_tampering()
    {
        var identity = Identity(CfeFamily.EFactura, 10, currency: "USD");
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

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        foreach (var value in new object[] { identity, outcome, thirdParty, fx })
        {
            var json = JsonSerializer.Serialize(value, value.GetType(), options);
            Assert.False(string.IsNullOrWhiteSpace(json));
        }

        var roundTrip = JsonSerializer.Deserialize<FiscalDailyReportDgiOutcomeEvidence>(
            JsonSerializer.Serialize(outcome, options),
            options);
        Assert.NotNull(roundTrip);
        roundTrip!.EnsureIntegrity();

        var tampered = outcome with { DgiStatusCode = "BE" };
        var error = Assert.Throws<DomainRuleException>(tampered.EnsureIntegrity);
        Assert.Equal("fiscal.daily_report.dgi_outcome.status_code_mismatch", error.Code);
    }

    private static FiscalDailyReportCfeIdentityEvidence Identity(
        CfeFamily family,
        long number,
        string currency = "UYU")
    {
        var (document, snapshot) = Fixture(family, number, currency: currency);
        return Identity(document, snapshot);
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
            "TEST-DAILY-REPORT-SOURCE-FACTS",
            "DGI test evidence",
            "https://example.invalid/daily-report-source-facts",
            "13.2-source-facts-test",
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
