using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using EFactura.Application.Fiscal;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Taxation;
using Infrastructure.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class DomesticCorrectionNoteFoundationTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string SettlementFingerprint = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private static readonly DateTimeOffset SigningTimestamp =
        new(2026, 9, 10, 1, 30, 45, TimeSpan.FromHours(-3));

    [Fact]
    public void Domestic_note_CFE_codes_are_pinned()
    {
        Assert.Equal(102, (int)CfeFamily.ETicketCreditNote);
        Assert.Equal(103, (int)CfeFamily.ETicketDebitNote);
        Assert.Equal(112, (int)CfeFamily.EFacturaCreditNote);
        Assert.Equal(113, (int)CfeFamily.EFacturaDebitNote);
    }

    [Theory]
    [InlineData(CfeFamily.ETicketCreditNote)]
    [InlineData(CfeFamily.ETicketDebitNote)]
    [InlineData(CfeFamily.EFacturaCreditNote)]
    [InlineData(CfeFamily.EFacturaDebitNote)]
    public void Domestic_note_snapshot_fails_closed_without_reference(CfeFamily family)
    {
        var error = Assert.Throws<DomainRuleException>(() => Fixture(family, includeReference: false));

        Assert.Equal("fiscal.snapshot.reference_required", error.Code);
    }

    [Fact]
    public void Reference_content_changes_snapshot_fingerprint_and_survives_JSON_roundtrip()
    {
        var (_, first) = Fixture(
            CfeFamily.EFacturaCreditNote,
            referenceReason: "Corrección de precio");
        var (_, second) = Fixture(
            CfeFamily.EFacturaCreditNote,
            referenceReason: "Corrección de cantidad");

        Assert.NotEqual(first.References!.Single().EvidenceFingerprint, second.References!.Single().EvidenceFingerprint);
        Assert.NotEqual(first.ContentFingerprint, second.ContentFingerprint);

        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var json = JsonSerializer.Serialize(first, options);
        var roundTrip = JsonSerializer.Deserialize<FiscalContentSnapshot>(json, options);

        Assert.NotNull(roundTrip);
        roundTrip!.EnsureIntegrity();
        Assert.Equal(first.ContentFingerprint, roundTrip.ContentFingerprint);
        Assert.Equal(first.References!.Single(), roundTrip.References!.Single());
    }

    [Fact]
    public void Pinned_DGI_numeric_prefix_series_form_is_accepted()
    {
        var reference = FiscalDocumentReferenceEvidence.Capture(
            1,
            "company-1",
            CfeFamily.EFactura,
            "1A",
            77,
            new DateOnly(2026, 9, 9),
            122m,
            "UYU",
            reason: "Corrección");

        Assert.Equal("1A", reference.Series);
        reference.EnsureIntegrity();
    }

    [Fact]
    public void Foreign_currency_reference_requires_exchange_rate()
    {
        var error = Assert.Throws<DomainRuleException>(() => FiscalDocumentReferenceEvidence.Capture(
            1,
            "company-1",
            CfeFamily.EFactura,
            "B",
            77,
            new DateOnly(2026, 9, 9),
            122m,
            "USD"));

        Assert.Equal("fiscal.snapshot.reference_exchange_rate_required", error.Code);
    }

    [Fact]
    public void Note_reference_must_belong_to_same_organization()
    {
        var reference = FiscalDocumentReferenceEvidence.Capture(
            1,
            "company-2",
            CfeFamily.EFactura,
            "B",
            77,
            new DateOnly(2026, 9, 9),
            122m,
            "UYU",
            reason: "Corrección");

        var error = Assert.Throws<DomainRuleException>(() => Fixture(
            CfeFamily.EFacturaCreditNote,
            references: [reference]));

        Assert.Equal("fiscal.snapshot.reference_organization_mismatch", error.Code);
    }

    [Fact]
    public void Note_reference_must_stay_inside_compatible_domestic_family()
    {
        var reference = FiscalDocumentReferenceEvidence.Capture(
            1,
            "company-1",
            CfeFamily.EFactura,
            "B",
            77,
            new DateOnly(2026, 9, 9),
            122m,
            "UYU",
            reason: "Corrección");

        var error = Assert.Throws<DomainRuleException>(() => Fixture(
            CfeFamily.ETicketCreditNote,
            references: [reference]));

        Assert.Equal("fiscal.snapshot.reference_family_incompatible", error.Code);
    }

    [Theory]
    [InlineData(CfeFamily.ETicketCreditNote, "eTck", "102")]
    [InlineData(CfeFamily.ETicketDebitNote, "eTck", "103")]
    [InlineData(CfeFamily.EFacturaCreditNote, "eFact", "112")]
    [InlineData(CfeFamily.EFacturaDebitNote, "eFact", "113")]
    public void Unsigned_builder_emits_domestic_note_with_reference_in_XSD_order(
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
        Assert.Contains("<Referencia><Referencia><NroLinRef>1</NroLinRef>", first.Xml);
        Assert.Contains("<TpoDocRef>", first.Xml);
        Assert.Contains("<Serie>B</Serie><NroCFERef>77</NroCFERef>", first.Xml);
        Assert.Contains("<RazonRef>Corrección de comprobante original</RazonRef>", first.Xml);
        Assert.Contains("<FechaCFEref>2026-09-09</FechaCFEref>", first.Xml);
        Assert.Contains("<MntCFEref>122</MntCFEref><TpoMonedaRef>UYU</TpoMonedaRef>", first.Xml);
        Assert.True(first.Xml.IndexOf("<Referencia>", StringComparison.Ordinal)
            < first.Xml.IndexOf("<CAEData>", StringComparison.Ordinal));
        Assert.DoesNotContain("IndGlobal", first.Xml);
        Assert.DoesNotContain("TmstFirma", first.Xml);
        Assert.DoesNotContain("Signature", first.Xml);
    }

    [Fact]
    public void Unsigned_builder_emits_reference_exchange_rate_when_evidence_requires_it()
    {
        var reference = FiscalDocumentReferenceEvidence.Capture(
            1,
            "company-1",
            CfeFamily.EFactura,
            "B",
            77,
            new DateOnly(2026, 9, 9),
            3.25m,
            "USD",
            39.50m,
            "Corrección en moneda extranjera");
        var (document, snapshot) = Fixture(
            CfeFamily.EFacturaDebitNote,
            references: [reference]);

        var artifact = new DeterministicUnsignedCfeBuilder().Build(document, snapshot);

        Assert.Contains("<TpoMonedaRef>USD</TpoMonedaRef><TpoCambioRef>39.5</TpoCambioRef>", artifact.Xml);
    }

    [Fact]
    public void Unsigned_builder_rejects_self_reference()
    {
        var self = FiscalDocumentReferenceEvidence.Capture(
            1,
            "company-1",
            CfeFamily.ETicketCreditNote,
            "A",
            100,
            new DateOnly(2026, 9, 10),
            122m,
            "UYU",
            reason: "Referencia inválida a sí misma");
        var (document, snapshot) = Fixture(
            CfeFamily.ETicketCreditNote,
            references: [self]);

        var error = Assert.Throws<DomainRuleException>(() =>
            new DeterministicUnsignedCfeBuilder().Build(document, snapshot));

        Assert.Equal("fiscal.cfe_builder.reference_self_forbidden", error.Code);
    }

    [Theory]
    [InlineData(CfeFamily.ETicketCreditNote)]
    [InlineData(CfeFamily.ETicketDebitNote)]
    [InlineData(CfeFamily.EFacturaCreditNote)]
    [InlineData(CfeFamily.EFacturaDebitNote)]
    public async Task Domestic_note_signs_and_validates_against_pinned_DGI_XSD(CfeFamily family)
    {
        var (document, snapshot) = Fixture(family);
        var unsigned = new DeterministicUnsignedCfeBuilder().Build(document, snapshot);
        var evidence = FiscalSigningEvidence.Establish(
            Guid.Parse("73000000-0000-0000-0000-000000000001"),
            document.OrganizationId,
            document.Id,
            unsigned.ContentFingerprint,
            Sha256(unsigned.Xml),
            SigningTimestamp);
        var payload = new DeterministicFiscalSigningPayloadBuilder().Build(document.Id, unsigned, evidence);

        using var certificate = CreateCertificate(SigningTimestamp.AddDays(-1), SigningTimestamp.AddDays(30));
        var signer = new XmlDsigFiscalSignatureProvider(new StaticCertificateSource(certificate));
        var signed = await signer.SignAsync(new FiscalSignatureRequest(
            document.Id,
            payload.Family,
            payload.FormatVersion,
            payload.FiscalContentFingerprint,
            payload.UnsignedContentHash,
            payload.Xml,
            payload.ContentHash,
            payload.SigningTimestamp));

        var validation = new DgiFeV1_44_2SignedCfeSchemaValidator().Validate(signed.SignedXml);

        Assert.True(
            validation.IsValid,
            string.Join(Environment.NewLine, validation.Errors.Select(error => $"{error.Code}: {error.Message}")));
        Assert.Equal(FiscalSignedCfeSchemaValidationStatus.Valid, validation.Status);
        Assert.Contains("TmstFirma", signed.SignedXml);
        Assert.Contains("Signature", signed.SignedXml);
        Assert.Contains("Referencia", signed.SignedXml);
    }

    [Fact]
    public void Existing_101_and_111_snapshots_still_do_not_require_references()
    {
        var (ticket, ticketSnapshot) = Fixture(CfeFamily.ETicket, includeReference: false);
        var (invoice, invoiceSnapshot) = Fixture(CfeFamily.EFactura, includeReference: false);

        Assert.Null(ticketSnapshot.References);
        Assert.Null(invoiceSnapshot.References);
        Assert.Contains("<TipoCFE>101</TipoCFE>", new DeterministicUnsignedCfeBuilder().Build(ticket, ticketSnapshot).Xml);
        Assert.Contains("<TipoCFE>111</TipoCFE>", new DeterministicUnsignedCfeBuilder().Build(invoice, invoiceSnapshot).Xml);
    }

    private static (FiscalDocument Document, FiscalContentSnapshot Snapshot) Fixture(
        CfeFamily family,
        bool includeReference = true,
        string referenceReason = "Corrección de comprobante original",
        IReadOnlyCollection<FiscalDocumentReferenceEvidence>? references = null)
    {
        var saleId = Guid.Parse("70000000-0000-0000-0000-000000000001");
        var lineId = Guid.Parse("70000000-0000-0000-0000-000000000002");
        var rule = new RegulatoryRuleEvidence(
            "TEST-CFE-RULE",
            "DGI test evidence",
            "https://example.invalid/dgi-rule",
            "25.2-test",
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            "test clause");
        var calculation = new CfeArithmeticResult(
            "UYU",
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
        var settlement = new FiscalSettlementEvidence(
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
            settlement);
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
        var receiver = ticketFamily ? null : new FiscalReceiverContentSnapshot(
            Guid.Parse("70000000-0000-0000-0000-000000000004"),
            "Cliente Snapshot S.A.",
            "UY",
            "UY",
            5,
            new FiscalReceiverIdentitySnapshot("2", "219999990017", "UY"),
            new FiscalReceiverAddressSnapshot(
                Guid.Parse("70000000-0000-0000-0000-000000000005"),
                FiscalReceiverAddressKind.Fiscal,
                "18 de Julio 2000",
                "Montevideo",
                "Montevideo",
                "UY",
                "11200"));

        if (references is null && includeReference && IsDomesticNote(family))
        {
            var referencedType = ticketFamily ? CfeFamily.ETicket : CfeFamily.EFactura;
            references = [FiscalDocumentReferenceEvidence.Capture(
                1,
                "company-1",
                referencedType,
                "B",
                77,
                new DateOnly(2026, 9, 9),
                122m,
                "UYU",
                reason: referenceReason)];
        }

        var snapshot = FiscalContentSnapshot.Create(
            "company-1",
            saleId,
            family,
            receiverRequirement,
            "25.2",
            new DateOnly(2026, 9, 10),
            Confirmation,
            SettlementFingerprint,
            issuer,
            receiver,
            [new FiscalContentLineSnapshot(
                1,
                lineId,
                Guid.Parse("70000000-0000-0000-0000-000000000003"),
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
            Guid.Parse("71000000-0000-0000-0000-000000000001"),
            "company-1",
            Guid.Parse("71000000-0000-0000-0000-000000000002"),
            saleId,
            Guid.Parse("71000000-0000-0000-0000-000000000003"),
            Guid.Parse("71000000-0000-0000-0000-000000000004"),
            null,
            family,
            "A",
            100,
            "90260000001",
            1,
            1000,
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            new DateOnly(2026, 9, 10),
            "loc-1",
            "terminal-1",
            receiverRequirement,
            "25.2",
            Confirmation,
            SettlementFingerprint,
            "UYU",
            100m,
            22m,
            122m,
            new DateTimeOffset(2026, 9, 10, 1, 0, 0, TimeSpan.FromHours(-3)));
        return (document, snapshot);
    }

    private static bool IsDomesticNote(CfeFamily family) => family is
        CfeFamily.ETicketCreditNote or
        CfeFamily.ETicketDebitNote or
        CfeFamily.EFacturaCreditNote or
        CfeFamily.EFacturaDebitNote;

    private static X509Certificate2 CreateCertificate(DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=eFactura Domestic Note Test, O=EFactura Tests, C=UY",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation,
            critical: true));
        return request.CreateSelfSigned(notBefore, notAfter);
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class StaticCertificateSource(X509Certificate2 certificate) : IFiscalSigningCertificateSource
    {
        public ValueTask<X509Certificate2> GetSigningCertificateAsync(
            FiscalSignatureRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(certificate);
        }
    }
}
