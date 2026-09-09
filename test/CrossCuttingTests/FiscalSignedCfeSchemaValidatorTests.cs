using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Taxation;
using Infrastructure.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalSignedCfeSchemaValidatorTests
{
    private const string Confirmation = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string SettlementFingerprint = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    private static readonly DateTimeOffset SigningTimestamp =
        new(2026, 9, 9, 15, 30, 45, TimeSpan.FromHours(-3));

    [Theory]
    [InlineData(CfeFamily.ETicket)]
    [InlineData(CfeFamily.EFactura)]
    public async Task Validate_accepts_builder_payload_after_real_XMLDSig(CfeFamily family)
    {
        var signed = await BuildSignedArtifact(family);
        var sut = new DgiFeV1_44_2SignedCfeSchemaValidator();

        var result = sut.Validate(signed);

        Assert.Equal(FiscalSignedCfeSchemaValidationStatus.Valid, result.Status);
        Assert.True(result.IsValid);
        Assert.Equal(DgiFeV1_44_2SignedCfeSchemaValidator.SchemaSetId, result.SchemaSetId);
        Assert.Equal(DgiFeV1_44_2SignedCfeSchemaValidator.SchemaVersion, result.SchemaVersion);
        Assert.Matches("^[0-9a-f]{64}$", result.SchemaSetFingerprint);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_rejects_unsigned_payload_because_DGI_root_requires_signature()
    {
        var (document, snapshot) = Fixture(CfeFamily.EFactura);
        var unsigned = new DeterministicUnsignedCfeBuilder().Build(document, snapshot);
        var evidence = FiscalSigningEvidence.Establish(
            Guid.Parse("63000000-0000-0000-0000-000000000001"),
            document.OrganizationId,
            document.Id,
            unsigned.ContentFingerprint,
            Sha256(unsigned.Xml),
            SigningTimestamp);
        var payload = new DeterministicFiscalSigningPayloadBuilder().Build(document.Id, unsigned, evidence);
        var sut = new DgiFeV1_44_2SignedCfeSchemaValidator();

        var result = sut.Validate(payload.Xml);

        Assert.Equal(FiscalSignedCfeSchemaValidationStatus.DocumentInvalid, result.Status);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_rejects_signature_when_it_is_not_the_final_CFE_child()
    {
        var signed = await BuildSignedArtifact(CfeFamily.ETicket);
        var document = XDocument.Parse(signed, LoadOptions.PreserveWhitespace);
        XNamespace ds = "http://www.w3.org/2000/09/xmldsig#";
        var signature = document.Root!.Element(ds + "Signature")!;
        signature.Remove();
        document.Root!.AddFirst(signature);
        var sut = new DgiFeV1_44_2SignedCfeSchemaValidator();

        var result = sut.Validate(document.ToString(SaveOptions.DisableFormatting));

        Assert.Equal(FiscalSignedCfeSchemaValidationStatus.DocumentInvalid, result.Status);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task Validate_rejects_malformed_TmstFirma()
    {
        var signed = await BuildSignedArtifact(CfeFamily.EFactura);
        var document = XDocument.Parse(signed, LoadOptions.PreserveWhitespace);
        XNamespace cfe = "http://cfe.dgi.gub.uy";
        document.Descendants(cfe + "TmstFirma").Single().Value = "not-a-dgi-timestamp";
        var sut = new DgiFeV1_44_2SignedCfeSchemaValidator();

        var result = sut.Validate(document.ToString(SaveOptions.DisableFormatting));

        Assert.Equal(FiscalSignedCfeSchemaValidationStatus.DocumentInvalid, result.Status);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    private static async Task<string> BuildSignedArtifact(CfeFamily family)
    {
        var (document, snapshot) = Fixture(family);
        var unsigned = new DeterministicUnsignedCfeBuilder().Build(document, snapshot);
        var evidence = FiscalSigningEvidence.Establish(
            Guid.Parse("63000000-0000-0000-0000-000000000001"),
            document.OrganizationId,
            document.Id,
            unsigned.ContentFingerprint,
            Sha256(unsigned.Xml),
            SigningTimestamp);
        var payload = new DeterministicFiscalSigningPayloadBuilder().Build(document.Id, unsigned, evidence);

        using var certificate = CreateCertificate(SigningTimestamp.AddDays(-1), SigningTimestamp.AddDays(30));
        var signer = new XmlDsigFiscalSignatureProvider(new StaticCertificateSource(certificate));
        var result = await signer.SignAsync(new FiscalSignatureRequest(
            document.Id,
            payload.Family,
            payload.FormatVersion,
            payload.FiscalContentFingerprint,
            payload.UnsignedContentHash,
            payload.Xml,
            payload.ContentHash,
            payload.SigningTimestamp));
        return result.SignedXml;
    }

    private static X509Certificate2 CreateCertificate(DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=eFactura XSD Test, O=EFactura Tests, C=UY",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation,
            critical: true));
        return request.CreateSelfSigned(notBefore, notAfter);
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
            [new CfeArithmeticLineResult(lineId, 100m, VatLiabilityKind.VatDue, VatRateKind.Basic, 22m, [rule], "UY-VAT-RATE-R1")],
            new CfeArithmeticTotals(100m, 0m, 100m, 0m, 0m, 22m, 22m, 122m),
            [rule]);
        var receiverRequirement = family == CfeFamily.ETicket
            ? ReceiverIdentificationRequirement.Optional
            : ReceiverIdentificationRequirement.Required;
        var settlement = new FiscalSettlementEvidence(FiscalSettlementKind.ImmediatePayment, FiscalPaymentForm.Cash, null);
        var fiscal = FiscalConfirmationEvidence.Capture(
            family, receiverRequirement, "25.2", Confirmation, calculation, [rule], settlement);
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
            [new FiscalContentLineSnapshot(
                1, lineId, Guid.Parse("20000000-0000-0000-0000-000000000003"), "ITEM-001",
                "Producto confirmado", FiscalContentLineKind.Goods, 1m, 100m, 0m, 0m,
                fiscal.Lines.Single(), "UNI")],
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
