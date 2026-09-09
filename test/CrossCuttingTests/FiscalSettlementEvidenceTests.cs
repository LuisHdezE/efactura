using System.Text.Json;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Taxation;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalSettlementEvidenceTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public void Immediate_payment_is_frozen_as_cash_and_changes_the_evidence_fingerprint()
    {
        var lineId = Guid.Parse("31000000-0000-0000-0000-000000000001");
        var legacy = Capture(lineId);
        var current = Capture(
            lineId,
            new FiscalSettlementEvidence(
                FiscalSettlementKind.ImmediatePayment,
                FiscalPaymentForm.Cash,
                null));

        Assert.Null(legacy.Settlement);
        Assert.NotNull(current.Settlement);
        Assert.Equal(FiscalSettlementKind.ImmediatePayment, current.Settlement!.Kind);
        Assert.Equal(FiscalPaymentForm.Cash, current.Settlement.PaymentForm);
        Assert.Null(current.Settlement.DueDate);
        Assert.NotEqual(legacy.EvidenceFingerprint, current.EvidenceFingerprint);
        current.EnsureIntegrity();
    }

    [Fact]
    public void Credit_settlement_requires_credit_form_and_authoritative_due_date()
    {
        var dueDate = new DateOnly(2026, 10, 15);
        var evidence = Capture(
            Guid.Parse("31000000-0000-0000-0000-000000000002"),
            new FiscalSettlementEvidence(
                FiscalSettlementKind.CreditReceivable,
                FiscalPaymentForm.Credit,
                dueDate));

        Assert.Equal(FiscalPaymentForm.Credit, evidence.Settlement!.PaymentForm);
        Assert.Equal(dueDate, evidence.Settlement.DueDate);
        evidence.EnsureIntegrity();

        var invalid = new FiscalSettlementEvidence(
            FiscalSettlementKind.CreditReceivable,
            FiscalPaymentForm.Credit,
            null);
        var error = Assert.Throws<DomainRuleException>(invalid.EnsureValid);
        Assert.Equal("fiscal.snapshot.credit_due_date_required", error.Code);
    }

    [Fact]
    public void Mixed_settlement_preserves_due_date_without_inventing_DGI_payment_form()
    {
        var dueDate = new DateOnly(2026, 11, 1);
        var settlement = new FiscalSettlementEvidence(
            FiscalSettlementKind.Mixed,
            null,
            dueDate);

        settlement.EnsureValid();
        Assert.Null(settlement.PaymentForm);
        Assert.Equal(dueDate, settlement.DueDate);
    }

    [Fact]
    public void Ambiguous_or_no_charge_settlement_shapes_fail_closed()
    {
        var dueDate = new DateOnly(2026, 11, 1);

        var mixedWithInventedForm = new FiscalSettlementEvidence(
            FiscalSettlementKind.Mixed,
            FiscalPaymentForm.Cash,
            dueDate);
        var mixedError = Assert.Throws<DomainRuleException>(mixedWithInventedForm.EnsureValid);
        Assert.Equal("fiscal.snapshot.payment_form_ambiguous", mixedError.Code);

        var noChargeWithInventedForm = new FiscalSettlementEvidence(
            FiscalSettlementKind.NoCharge,
            FiscalPaymentForm.Credit,
            null);
        var noChargeError = Assert.Throws<DomainRuleException>(noChargeWithInventedForm.EnsureValid);
        Assert.Equal("fiscal.snapshot.no_charge_payment_form_forbidden", noChargeError.Code);

        var cashWithDueDate = new FiscalSettlementEvidence(
            FiscalSettlementKind.ImmediatePayment,
            FiscalPaymentForm.Cash,
            dueDate);
        var cashError = Assert.Throws<DomainRuleException>(cashWithDueDate.EnsureValid);
        Assert.Equal("fiscal.snapshot.payment_form_mismatch", cashError.Code);
    }

    [Fact]
    public void Historical_JSON_without_settlement_property_keeps_the_legacy_fingerprint_valid()
    {
        var legacy = Capture(Guid.Parse("31000000-0000-0000-0000-000000000003"));
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var currentJson = JsonSerializer.Serialize(legacy, options);
        Assert.Contains("\"settlement\":null", currentJson, StringComparison.Ordinal);

        var historicalJson = currentJson.Replace(",\"settlement\":null}", "}", StringComparison.Ordinal);
        var restored = JsonSerializer.Deserialize<FiscalConfirmationEvidence>(historicalJson, options);

        Assert.NotNull(restored);
        Assert.Null(restored!.Settlement);
        Assert.Equal(legacy.EvidenceFingerprint, restored.EvidenceFingerprint);
        restored.EnsureIntegrity();
    }

    private static FiscalConfirmationEvidence Capture(
        Guid lineId,
        FiscalSettlementEvidence? settlement = null)
    {
        var rule = RuleEvidence();
        return FiscalConfirmationEvidence.Capture(
            CfeFamily.EFactura,
            ReceiverIdentificationRequirement.Required,
            "25.2",
            Confirmation,
            new CfeArithmeticResult(
                "UYU",
                "25.2",
                "UY-CFE-25.2-ARITH-R1",
                new[]
                {
                    new CfeArithmeticLineResult(
                        lineId,
                        100m,
                        VatLiabilityKind.VatDue,
                        VatRateKind.Basic,
                        22m,
                        new[] { rule },
                        "UY-VAT-RATE-R1")
                },
                new CfeArithmeticTotals(
                    100m,
                    0m,
                    100m,
                    0m,
                    0m,
                    22m,
                    22m,
                    122m),
                new[] { rule }),
            new[] { rule },
            settlement);
    }

    private static RegulatoryRuleEvidence RuleEvidence() =>
        new(
            "TEST-CFE-SETTLEMENT-RULE",
            "DGI test evidence",
            "https://example.invalid/dgi-rule",
            "25.2-test",
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            "test clause");
}
