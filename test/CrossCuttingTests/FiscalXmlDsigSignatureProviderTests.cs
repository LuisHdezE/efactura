using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using Infrastructure.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalXmlDsigSignatureProviderTests
{
    private static readonly Guid DocumentId = Guid.Parse("62000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset SigningTimestamp =
        new(2026, 9, 9, 15, 30, 45, TimeSpan.FromHours(-3));
    private const string XmlDsigNamespace = "http://www.w3.org/2000/09/xmldsig#";

    [Fact]
    public async Task Sign_produces_verifiable_sha256_enveloped_signature_with_certificate_evidence()
    {
        using var certificate = CreateCertificate(
            SigningTimestamp.AddDays(-1),
            SigningTimestamp.AddDays(30));
        var source = new StaticCertificateSource(certificate);
        var sut = new XmlDsigFiscalSignatureProvider(source);
        var request = Request(BasePayload());

        var result = await sut.SignAsync(request);

        var document = Load(result.SignedXml);
        var signatures = document.GetElementsByTagName("Signature", XmlDsigNamespace);
        Assert.Equal(1, signatures.Count);
        var signatureElement = Assert.IsType<XmlElement>(signatures[0]);
        Assert.Same(signatureElement, document.DocumentElement!.LastChild);

        var signedInfo = signatureElement["SignedInfo", XmlDsigNamespace]!;
        Assert.Equal(
            FiscalXmlSignatureProfile.InclusiveC14N,
            signedInfo["CanonicalizationMethod", XmlDsigNamespace]!.GetAttribute("Algorithm"));
        Assert.Equal(
            FiscalXmlSignatureProfile.RsaSha256,
            signedInfo["SignatureMethod", XmlDsigNamespace]!.GetAttribute("Algorithm"));

        var reference = signedInfo["Reference", XmlDsigNamespace]!;
        Assert.Equal(string.Empty, reference.GetAttribute("URI"));
        Assert.Equal(
            FiscalXmlSignatureProfile.Sha256,
            reference["DigestMethod", XmlDsigNamespace]!.GetAttribute("Algorithm"));
        var transforms = reference["Transforms", XmlDsigNamespace]!;
        Assert.Equal(
            FiscalXmlSignatureProfile.EnvelopedSignature,
            transforms["Transform", XmlDsigNamespace]!.GetAttribute("Algorithm"));

        Assert.NotNull(signatureElement.SelectSingleNode(
            "*[local-name()='KeyInfo']/*[local-name()='X509Data']/*[local-name()='X509Certificate']"));
        Assert.NotNull(signatureElement.SelectSingleNode(
            "*[local-name()='KeyInfo']/*[local-name()='X509Data']/*[local-name()='X509IssuerSerial']"));

        var verifier = new SignedXml(document);
        verifier.LoadXml(signatureElement);
        Assert.True(verifier.CheckSignature(certificate, verifySignatureOnly: true));
    }

    [Fact]
    public async Task Sign_fails_closed_before_certificate_access_when_payload_hash_changed()
    {
        using var certificate = CreateCertificate(
            SigningTimestamp.AddDays(-1),
            SigningTimestamp.AddDays(30));
        var source = new StaticCertificateSource(certificate);
        var sut = new XmlDsigFiscalSignatureProvider(source);
        var request = Request(BasePayload()) with
        {
            SigningPayloadHash = new string('0', 64)
        };

        var error = await Assert.ThrowsAsync<FiscalSignatureProviderException>(() => sut.SignAsync(request));

        Assert.Equal("fiscal.signature.payload_hash_mismatch", error.Code);
        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public async Task Sign_fails_closed_when_Adenda_enters_the_signature_payload()
    {
        using var certificate = CreateCertificate(
            SigningTimestamp.AddDays(-1),
            SigningTimestamp.AddDays(30));
        var source = new StaticCertificateSource(certificate);
        var sut = new XmlDsigFiscalSignatureProvider(source);
        var payload = BasePayload().Replace(
            "</CFE>",
            "<Adenda><Texto>commercial-only</Texto></Adenda></CFE>",
            StringComparison.Ordinal);

        var error = await Assert.ThrowsAsync<FiscalSignatureProviderException>(
            () => sut.SignAsync(Request(payload)));

        Assert.Equal("fiscal.signature.adenda_not_signable", error.Code);
        Assert.Equal(0, source.Calls);
    }

    [Fact]
    public async Task Sign_fails_closed_when_certificate_is_expired_at_durable_TmstFirma()
    {
        using var certificate = CreateCertificate(
            SigningTimestamp.AddDays(-10),
            SigningTimestamp.AddDays(-1));
        var source = new StaticCertificateSource(certificate);
        var sut = new XmlDsigFiscalSignatureProvider(source);

        var error = await Assert.ThrowsAsync<FiscalSignatureProviderException>(
            () => sut.SignAsync(Request(BasePayload())));

        Assert.Equal("fiscal.signature.certificate_expired", error.Code);
        Assert.Equal(1, source.Calls);
    }

    [Fact]
    public void Constructor_rejects_sha1_profile_instead_of_silently_using_historical_DGI_material()
    {
        using var certificate = CreateCertificate(
            SigningTimestamp.AddDays(-1),
            SigningTimestamp.AddDays(30));
        var source = new StaticCertificateSource(certificate);
        var sha1 = FiscalXmlSignatureProfile.DgiCfeSha256EvidenceBackedV1 with
        {
            ProfileId = "legacy-sha1-not-allowed",
            SignatureMethod = "http://www.w3.org/2000/09/xmldsig#rsa-sha1",
            DigestMethod = "http://www.w3.org/2000/09/xmldsig#sha1"
        };

        var error = Assert.Throws<FiscalSignatureProviderException>(
            () => new XmlDsigFiscalSignatureProvider(source, sha1));

        Assert.Equal("fiscal.signature.profile_sha1_forbidden", error.Code);
    }

    private static FiscalSignatureRequest Request(string payload) => new(
        DocumentId,
        CfeFamily.EFactura,
        "25.2",
        "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
        "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
        payload,
        Sha256(payload),
        SigningTimestamp);

    private static string BasePayload() =>
        "<?xml version=\"1.0\" encoding=\"utf-8\"?><CFE version=\"1.0\" xmlns=\"http://cfe.dgi.gub.uy\"><eFact><TmstFirma>2026-09-09T15:30:45-03:00</TmstFirma><Encabezado /><Detalle /><CAEData /></eFact></CFE>";

    private static X509Certificate2 CreateCertificate(DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=eFactura XMLDSig Test, O=EFactura Tests, C=UY",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation,
            critical: true));
        return request.CreateSelfSigned(notBefore, notAfter);
    }

    private static XmlDocument Load(string xml)
    {
        var document = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
        document.LoadXml(xml);
        return document;
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed class StaticCertificateSource(X509Certificate2 certificate) : IFiscalSigningCertificateSource
    {
        public int Calls { get; private set; }

        public ValueTask<X509Certificate2> GetSigningCertificateAsync(
            FiscalSignatureRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            return ValueTask.FromResult(certificate);
        }
    }
}
