using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using Infrastructure.Fiscal;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalCfeEnvelopeDocumentResponseSignatureVerifierTests
{
    private const string RsaSha256 = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
    private const string Sha256 = "http://www.w3.org/2001/04/xmlenc#sha256";

    private readonly DgiFiscalCfeEnvelopeDocumentResponseSignatureVerifier _verifier = new();

    [Fact]
    public void Valid_whole_document_ACKCFE_XMLDSig_is_verified_without_claiming_certificate_trust()
    {
        var result = _verifier.Verify(SignedAckCfe());

        Assert.True(result.IsValid);
        Assert.Equal(DgiFiscalCfeEnvelopeDocumentResponseSignatureVerifier.ProfileId, result.VerificationProfileId);
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
    public void Tampering_signed_ACKCFE_bytes_fails_cryptographic_verification()
    {
        var xml = SignedAckCfe().Replace("<Estado>A</Estado>", "<Estado>R</Estado>", StringComparison.Ordinal);

        var result = _verifier.Verify(xml);

        Assert.False(result.IsValid);
        Assert.Equal("fiscal.envelope.document_response.signature.check_failed", result.FailureCode);
        Assert.False(result.CertificateTrustValidated);
    }

    [Fact]
    public void External_reference_is_rejected_before_signature_evaluation()
    {
        var xml = SignedAckCfe().Replace(
            "Reference URI=\"\"",
            "Reference URI=\"https://example.invalid/ackcfe.xml\"",
            StringComparison.Ordinal);

        var result = _verifier.Verify(xml);

        Assert.False(result.IsValid);
        Assert.Equal("fiscal.envelope.document_response.signature.reference_invalid", result.FailureCode);
    }

    [Fact]
    public void Missing_embedded_X509_certificate_is_rejected()
    {
        var document = Load(SignedAckCfe());
        var certificates = document.GetElementsByTagName("X509Certificate", SignedXml.XmlDsigNamespaceUrl);
        Assert.Equal(1, certificates.Count);
        certificates[0]!.ParentNode!.RemoveChild(certificates[0]!);

        var result = _verifier.Verify(document.OuterXml);

        Assert.False(result.IsValid);
        Assert.Equal("fiscal.envelope.document_response.signature.certificate_required", result.FailureCode);
    }

    [Fact]
    public void DTD_input_fails_closed()
    {
        const string xml = "<!DOCTYPE x [<!ENTITY e SYSTEM 'file:///etc/passwd'>]><ACKCFE xmlns=\"http://cfe.dgi.gub.uy\">&e;</ACKCFE>";

        var result = _verifier.Verify(xml);

        Assert.False(result.IsValid);
        Assert.Equal("fiscal.envelope.document_response.signature.verification_failed", result.FailureCode);
    }

    private static string SignedAckCfe()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            "CN=DGI ACKCFE Signature Test, O=EFactura Tests, C=UY",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30));

        var document = Load(
            "<ACKCFE xmlns=\"http://cfe.dgi.gub.uy\"><Caratula><RUCReceptor>219999830019</RUCReceptor><RUCEmisor>214748364700</RUCEmisor><IDRespuesta>5001</IDRespuesta><IDEmisor>6101</IDEmisor><IDReceptor>9001</IDReceptor><CantenSobre>1</CantenSobre><CantResponden>1</CantResponden><CantCFEAceptados>1</CantCFEAceptados><CantCFERechazados>0</CantCFERechazados><CantCFEObservados>0</CantCFEObservados><CantOtrosRechazados>0</CantOtrosRechazados></Caratula><ACKCFE_det ordinal=\"1\"><TipoCFE>101</TipoCFE><Serie>A</Serie><NroCFE>1</NroCFE><Estado>A</Estado></ACKCFE_det></ACKCFE>");

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
