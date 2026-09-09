using EFactura.Application.Fiscal;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Taxation;
using Xunit;

namespace CrossCuttingTests;

public sealed class UnsignedCfeBuilderTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string SettlementFingerprint = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Theory]
    [InlineData(CfeFamily.ETicket, "eTck", "101")]
    [InlineData(CfeFamily.EFactura, "eFact", "111")]
    public void Build_is_deterministic_and_contains_only_unsigned_business_content(
        CfeFamily family,
        string familyElement,
        string typeCode)
    {
        var (document, snapshot) = Fixture(family);
        var sut = new DeterministicUnsignedCfeBuilder();

        var first = sut.Build(document, snapshot);
        var second = sut.Build(document, snapshot);

        Assert.Equal(first, second);
        Assert.Contains($"<{familyElement}>", first.Xml);
        Assert.Contains($"<TipoCFE>{typeCode}</TipoCFE>", first.Xml);
        Assert.Contains("<FmaPago>1</FmaPago>", first.Xml);
        Assert.Contains("<CdgDGISucur>1</CdgDGISucur>", first.Xml);
        Assert.Contains("<IndFact>3</IndFact>", first.Xml);
        Assert.Contains("<UniMed>UNI</UniMed>", first.Xml);
        Assert.Contains("<CAE_ID>90260000001</CAE_ID>", first.Xml);
        Assert.DoesNotContain("TmstFirma", first.Xml);
        Assert.DoesNotContain("Signature", first.Xml);
    }

    [Fact]
    public void Build_fails_closed_for_export_family()
    {
        var (document, snapshot) = Fixture(CfeFamily.EFacturaExportacion);
        var sut = new DeterministicUnsignedCfeBuilder();

        var error = Assert.Throws<DomainRuleException>(() => sut.Build(document, snapshot));

        Assert.Equal("fiscal.cfe_builder.export_not_supported", error.Code);
    }

    [Fact]
    public void Build_fails_closed_when_unit_evidence_is_missing()
    {
        var (document, snapshot) = Fixture(CfeFamily.EFactura);
        var line = snapshot.Lines.Single();
        var tampered = snapshot with
        {
            Lines = new[] { line with { UnitOfMeasure = null } }
        };
        // Recompute a valid immutable snapshot whose accepted source still lacks a unit.
        tampered = FiscalContentSnapshot.Create(
            tampered.OrganizationId,
            tampered.SaleId,
            tampered.CfeFamily,
            tampered.ReceiverIdentification,
            tampered.FormatVersion,
            tampered.EffectiveOn,
            tampered.ConfirmationFingerprint,
            tampered.SettlementFingerprint,
            tampered.Issuer,
            tampered.Receiver,
            tampered.Lines,
            tampered.FiscalEvidence);
        var sut = new DeterministicUnsignedCfeBuilder();

        var error = Assert.Throws<DomainRuleException>(() => sut.Build(document, tampered));

        Assert.Equal("fiscal.cfe_builder.unit_required", error.Code);
    }

    private static (FiscalDocument Document, FiscalContentSnapshot Snapshot) Fixture(CfeFamily family)
    {
        var saleId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var lineId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var rule = new RegulatoryRuleEvidence(
            "TEST-CFE-RULE", "DGI test evidence", "https://example.invalid/dgi-rule", "25.2-test",
            new DateOnly(2026, 1, 1), new DateOnly(2027, 12, 31), "test clause");
        var calculation = new CfeArithmeticResult(
            "UYU", "25.2", "UY-CFE-25.2-ARITH-R1",
            new[] { new CfeArithmeticLineResult(lineId, 100m, VatLiabilityKind.VatDue, VatRateKind.Basic, 22m, new[] { rule }, "UY-VAT-RATE-R1") },
            new CfeArithmeticTotals(100m, 0m, 100m, 0m, 0m, 22m, 22m, 122m),
            new[] { rule });
        var receiverRequirement = family == CfeFamily.ETicket
            ? ReceiverIdentificationRequirement.Optional
            : ReceiverIdentificationRequirement.Required;
        var settlement = new FiscalSettlementEvidence(FiscalSettlementKind.ImmediatePayment, FiscalPaymentForm.Cash, null);
        var fiscal = FiscalConfirmationEvidence.Capture(
            family, receiverRequirement, "25.2", Confirmation, calculation, new[] { rule }, settlement);
        var issuer = new FiscalIssuerContentSnapshot(
            "214748364700", "Empresa Snapshot S.A.", "Empresa Snapshot", 3, "loc-1", "0001",
            "Av. Italia 1234", "Montevideo", "Montevideo", 4);
        var receiver = family == CfeFamily.ETicket ? null : new FiscalReceiverContentSnapshot(
            Guid.Parse("20000000-0000-0000-0000-000000000004"), "Cliente Snapshot S.A.", "UY", "UY", 5,
            new FiscalReceiverIdentitySnapshot("2", "219999990017", "UY"),
            new FiscalReceiverAddressSnapshot(
                Guid.Parse("20000000-0000-0000-0000-000000000005"), FiscalReceiverAddressKind.Fiscal,
                "18 de Julio 2000", "Montevideo", "Montevideo", "UY", "11200"));
        var snapshot = FiscalContentSnapshot.Create(
            "company-1", saleId, family, receiverRequirement, "25.2", new DateOnly(2026, 9, 9),
            Confirmation, SettlementFingerprint, issuer, receiver,
            new[]
            {
                new FiscalContentLineSnapshot(
                    1, lineId, Guid.Parse("20000000-0000-0000-0000-000000000003"), "ITEM-001",
                    "Producto confirmado", FiscalContentLineKind.Goods, 1m, 100m, 0m, 0m,
                    fiscal.Lines.Single(), "UNI")
            },
            fiscal);
        var document = FiscalDocument.CreateIdentity(
            Guid.Parse("30000000-0000-0000-0000-000000000001"), "company-1",
            Guid.Parse("30000000-0000-0000-0000-000000000002"), saleId,
            Guid.Parse("30000000-0000-0000-0000-000000000003"),
            Guid.Parse("30000000-0000-0000-0000-000000000004"), null,
            family, "A", 100, "90260000001", 1, 1000,
            new DateOnly(2026, 1, 1), new DateOnly(2027, 12, 31), new DateOnly(2026, 9, 9),
            "loc-1", "terminal-1", receiverRequirement, "25.2", Confirmation, SettlementFingerprint,
            "UYU", 100m, 22m, 122m, new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.FromHours(-3)));
        return (document, snapshot);
    }
}
