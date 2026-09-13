using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using Infrastructure.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalCfeEnvelopeAckSignatureVerifierTests
{
    private const string RsaSha256 = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
    private const string Sha256 = "http://www.w3.org/2001/04/xmlenc#sha256";

    private readonly DgiFiscalCfeEnvelopeAckSignatureVerifier _verifier = new();

    [Fact]
    public void Valid_whole_document_ACKSobre_XMLDSig_is_verified_without_claiming_certificate_trust()
    {
        var xml = SignedAck();

        var result = _verifier.Verify(xml);

        Assert.True(result.IsValid);
        Assert.Equal(DgiFiscalCfeEnvelopeAckSignatureVerifier.ProfileId, result.VerificationProfileId);
        Assert.Equal(64, result.CertificateSha256.Length);
        Assert.NotEmpty(result.CertificateThumbprint);
        Assert.NotEmpty(result.CertificateSerialNumber);
        Assert.NotEmpty(result.CertificateSubject);
        Assert.NotEmpty(result.CertificateIssuer);
        Assert.Equal(SignedXml.XmlDsigC14NTransformUrl, result.CanonicalizationMethod);
        Assert.Equal(RsaSha256, result.SignatureMethod);
        Assert.Equal(Sha256, result.DigestMethod);
        Assert.Equal(string.Empty, result.ReferenceUri);
        Assert.Contains(SignedXml.XmlDsigEnvelopedSignatureTransformUrl, result.ReferenceTransforms);
        Assert.False(result.CertificateTrustValidated);
        Assert.Null(result.FailureCode);
    }

    [Fact]
    public void Tampering_signed_ACKSobre_bytes_fails_cryptographic_verification()
    {
        var xml = SignedAck().Replace("<Estado>AS</Estado>", "<Estado>BS</Estado>", StringComparison.Ordinal);

        var result = _verifier.Verify(xml);

        Assert.False(result.IsValid);
        Assert.Equal("fiscal.envelope.ack.signature.check_failed", result.FailureCode);
        Assert.False(result.CertificateTrustValidated);
    }

    [Fact]
    public void External_reference_is_rejected_before_signature_evaluation()
    {
        var xml = SignedAck().Replace("Reference URI=\"\"", "Reference URI=\"https://example.invalid/ack.xml\"", StringComparison.Ordinal);

        var result = _verifier.Verify(xml);

        Assert.False(result.IsValid);
        Assert.Equal("fiscal.envelope.ack.signature.reference_invalid", result.FailureCode);
    }

    [Fact]
    public void Missing_embedded_X509_certificate_is_rejected()
    {
        var document = Load(SignedAck());
        var certificates = document.GetElementsByTagName("X509Certificate", SignedXml.XmlDsigNamespaceUrl);
        Assert.Equal(1, certificates.Count);
        certificates[0]!.ParentNode!.RemoveChild(certificates[0]!);

        var result = _verifier.Verify(document.OuterXml);

        Assert.False(result.IsValid);
        Assert.Equal("fiscal.envelope.ack.signature.certificate_required", result.FailureCode);
    }

    [Fact]
    public void DTD_input_fails_closed()
    {
        const string xml = "<!DOCTYPE x [<!ENTITY e SYSTEM 'file:///etc/passwd'>]><ACKSobre xmlns=\"http://cfe.dgi.gub.uy\">&e;</ACKSobre>";

        var result = _verifier.Verify(xml);

        Assert.False(result.IsValid);
        Assert.Equal("fiscal.envelope.ack.signature.verification_failed", result.FailureCode);
    }

    private static string SignedAck()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=DGI ACKSobre Signature Test, O=EFactura Tests, C=UY",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30));

        var document = Load(
            "<ACKSobre xmlns=\"http://cfe.dgi.gub.uy\"><Caratula><RUCReceptor>219999830019</RUCReceptor><RUCEmisor>214748364700</RUCEmisor><IDRespuesta>5001</IDRespuesta><NomArch/><FecHRecibido>2026-09-13T03:15:00</FecHRecibido><IdEmisor>6101</IdEmisor><IDReceptor>9001</IDReceptor><CantidadCFE>1</CantidadCFE><Tmst>2026-09-13T03:15:01</Tmst></Caratula><Detalle><Estado>AS</Estado></Detalle></ACKSobre>");

        using var privateKey = certificate.GetRSAPrivateKey();
        Assert.NotNull(privateKey);
        var signedXml = new SignedXml(document)
        {
            SigningKey = privateKey
        };
        signedXml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigC14NTransformUrl;
        signedXml.SignedInfo.SignatureMethod = RsaSha256;
        var reference = new Reference
        {
            Uri = string.Empty,
            DigestMethod = Sha256
        };
        reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        reference.AddTransform(new XmlDsigC14NTransform());
        signedXml.AddReference(reference);
        var x509Data = new KeyInfoX509Data();
        x509Data.AddCertificate(certificate);
        var keyInfo = new KeyInfo();
        keyInfo.AddClause(x509Data);
        signedXml.KeyInfo = keyInfo;
        signedXml.ComputeSignature();
        document.DocumentElement!.AppendChild(document.ImportNode(signedXml.GetXml(), deep: true));
        return document.OuterXml;
    }

    private static XmlDocument Load(string xml)
    {
        var document = new XmlDocument
        {
            PreserveWhitespace = true,
            XmlResolver = null
        };
        document.LoadXml(xml);
        return document;
    }
}
