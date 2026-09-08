using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Taxation;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalContentSnapshotDomainTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string Settlement = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void Fiscal_confirmation_evidence_has_deterministic_content_fingerprint()
    {
        var lineId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var calculation = Calculation(lineId);
        var rule = RuleEvidence();

        var first = FiscalConfirmationEvidence.Capture(
            CfeFamily.EFactura,
            ReceiverIdentificationRequirement.Required,
            "25.2",
            Confirmation,
            calculation,
            new[] { rule });
        var second = FiscalConfirmationEvidence.Capture(
            CfeFamily.EFactura,
            ReceiverIdentificationRequirement.Required,
            "25.2",
            Confirmation,
            calculation,
            new[] { rule });

        Assert.Equal(first.EvidenceFingerprint, second.EvidenceFingerprint);
        Assert.Equal(64, first.EvidenceFingerprint.Length);
        Assert.Equal(100m, first.Totals.NetAmount);
        Assert.Equal(22m, first.Totals.VatAmount);
        first.EnsureIntegrity();
    }

    [Fact]
    public void Fiscal_content_snapshot_is_deterministic_and_tamper_evident()
    {
        var saleId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var lineId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var itemId = Guid.Parse("20000000-0000-0000-0000-000000000003");
        var partyId = Guid.Parse("20000000-0000-0000-0000-000000000004");
        var addressId = Guid.Parse("20000000-0000-0000-0000-000000000005");
        var fiscal = FiscalConfirmationEvidence.Capture(
            CfeFamily.EFactura,
            ReceiverIdentificationRequirement.Required,
            "25.2",
            Confirmation,
            Calculation(lineId),
            new[] { RuleEvidence() });

        var issuer = new FiscalIssuerContentSnapshot(
            "214748364700",
            "Empresa Snapshot S.A.",
            "Empresa Snapshot",
            3,
            "loc-1",
            "0001",
            "Av. Italia 1234",
            "Montevideo",
            "Montevideo",
            4);
        var receiver = new FiscalReceiverContentSnapshot(
            partyId,
            "Cliente Snapshot S.A.",
            "UY",
            "UY",
            5,
            new FiscalReceiverIdentitySnapshot("2", "219999990017", "UY"),
            new FiscalReceiverAddressSnapshot(
                addressId,
                FiscalReceiverAddressKind.Fiscal,
                "18 de Julio 2000",
                "Montevideo",
                "Montevideo",
                "UY",
                "11200"));
        var lines = new[]
        {
            new FiscalContentLineSnapshot(
                1,
                lineId,
                itemId,
                "ITEM-001",
                "Producto confirmado",
                FiscalContentLineKind.Goods,
                1m,
                100m,
                0m,
                0m,
                fiscal.Lines.Single())
        };

        var first = FiscalContentSnapshot.Create(
            "company-1",
            saleId,
            CfeFamily.EFactura,
            ReceiverIdentificationRequirement.Required,
            "25.2",
            new DateOnly(2026, 9, 8),
            Confirmation,
            Settlement,
            issuer,
            receiver,
            lines,
            fiscal);
        var second = FiscalContentSnapshot.Create(
            "company-1",
            saleId,
            CfeFamily.EFactura,
            ReceiverIdentificationRequirement.Required,
            "25.2",
            new DateOnly(2026, 9, 8),
            Confirmation,
            Settlement,
            issuer,
            receiver,
            lines,
            fiscal);

        Assert.Equal(first.ContentFingerprint, second.ContentFingerprint);
        Assert.Equal(64, first.ContentFingerprint.Length);
        first.EnsureIntegrity();

        var tampered = first with
        {
            Issuer = first.Issuer with { LegalName = "Empresa cambiada después" }
        };
        var error = Assert.Throws<DomainRuleException>(tampered.EnsureIntegrity);
        Assert.Equal("fiscal.snapshot.content_fingerprint_mismatch", error.Code);
    }

    [Fact]
    public void Fiscal_content_snapshot_rejects_line_tax_evidence_not_owned_by_confirmation()
    {
        var saleId = Guid.NewGuid();
        var lineId = Guid.NewGuid();
        var fiscal = FiscalConfirmationEvidence.Capture(
            CfeFamily.ETicket,
            ReceiverIdentificationRequirement.Optional,
            "25.2",
            Confirmation,
            Calculation(lineId),
            new[] { RuleEvidence() });
        var alienFiscal = fiscal.Lines.Single() with { LineId = Guid.NewGuid() };

        var error = Assert.Throws<DomainRuleException>(() => FiscalContentSnapshot.Create(
            "company-1",
            saleId,
            CfeFamily.ETicket,
            ReceiverIdentificationRequirement.Optional,
            "25.2",
            new DateOnly(2026, 9, 8),
            Confirmation,
            Settlement,
            new FiscalIssuerContentSnapshot(
                "214748364700",
                "Empresa Snapshot S.A.",
                null,
                1,
                "loc-1",
                "0001",
                "Av. Italia 1234",
                "Montevideo",
                "Montevideo",
                1),
            null,
            new[]
            {
                new FiscalContentLineSnapshot(
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
                    alienFiscal)
            },
            fiscal));

        Assert.Equal("fiscal.snapshot.line_fiscal_evidence_mismatch", error.Code);
    }

    private static CfeArithmeticResult Calculation(Guid lineId)
    {
        var rule = RuleEvidence();
        return new CfeArithmeticResult(
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
            new[] { rule });
    }

    private static RegulatoryRuleEvidence RuleEvidence() =>
        new(
            "TEST-CFE-RULE",
            "DGI test evidence",
            "https://example.invalid/dgi-rule",
            "25.2-test",
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            "test clause");
}
