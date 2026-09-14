using System.Collections;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;
using EFactura.Application.Fiscal;

namespace Infrastructure.Fiscal;

/// <summary>
/// Verifies XMLDSig signature mathematics for a durable ACKCFE using the public X.509 certificate
/// embedded in KeyInfo. Certificate-chain trust and DGI signer identity are deliberately separate.
/// </summary>
public sealed class DgiFiscalCfeEnvelopeDocumentResponseSignatureVerifier :
    IFiscalCfeEnvelopeDocumentResponseSignatureVerifier
{
    public const string ProfileId = "dgi-ackcfe-xmldsig-math-v1";

    private const string DgiNamespace = "http://cfe.dgi.gub.uy";
    private const string XmlDsigNamespace = "http://www.w3.org/2000/09/xmldsig#";
    private const string RsaSha256 = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
    private const string RsaSha384 = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha384";
    private const string RsaSha512 = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha512";
    private const string Sha256 = "http://www.w3.org/2001/04/xmlenc#sha256";
    private const string Sha384 = "http://www.w3.org/2001/04/xmldsig-more#sha384";
    private const string Sha512 = "http://www.w3.org/2001/04/xmlenc#sha512";

    private static readonly HashSet<string> AllowedSignatureMethods = new(StringComparer.Ordinal)
    {
        SignedXml.XmlDsigRSASHA1Url,
        RsaSha256,
        RsaSha384,
        RsaSha512
    };

    private static readonly HashSet<string> AllowedDigestMethods = new(StringComparer.Ordinal)
    {
        SignedXml.XmlDsigSHA1Url,
        Sha256,
        Sha384,
        Sha512
    };

    private static readonly HashSet<string> AllowedCanonicalizationMethods = new(StringComparer.Ordinal)
    {
        SignedXml.XmlDsigC14NTransformUrl,
        SignedXml.XmlDsigC14NWithCommentsTransformUrl,
        SignedXml.XmlDsigExcC14NTransformUrl,
        SignedXml.XmlDsigExcC14NWithCommentsTransformUrl
    };

    private static readonly HashSet<string> AllowedReferenceTransforms = new(StringComparer.Ordinal)
    {
        SignedXml.XmlDsigEnvelopedSignatureTransformUrl,
        SignedXml.XmlDsigC14NTransformUrl,
        SignedXml.XmlDsigC14NWithCommentsTransformUrl,
        SignedXml.XmlDsigExcC14NTransformUrl,
        SignedXml.XmlDsigExcC14NWithCommentsTransformUrl
    };

    public FiscalCfeEnvelopeDocumentResponseSignatureVerificationEvidence Verify(string responseXml)
    {
        if (string.IsNullOrWhiteSpace(responseXml))
            return Invalid("fiscal.envelope.document_response.signature.empty");

        try
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            };
            using var reader = XmlReader.Create(new StringReader(responseXml), settings);
            var document = new XmlDocument
            {
                PreserveWhitespace = true,
                XmlResolver = null
            };
            document.Load(reader);

            var root = document.DocumentElement;
            if (root is null
                || !string.Equals(root.LocalName, "ACKCFE", StringComparison.Ordinal)
                || !string.Equals(root.NamespaceURI, DgiNamespace, StringComparison.Ordinal))
            {
                return Invalid("fiscal.envelope.document_response.signature.root_invalid");
            }

            var signatures = root.ChildNodes
                .OfType<XmlElement>()
                .Where(x => string.Equals(x.LocalName, "Signature", StringComparison.Ordinal)
                    && string.Equals(x.NamespaceURI, XmlDsigNamespace, StringComparison.Ordinal))
                .ToArray();
            if (signatures.Length != 1)
                return Invalid("fiscal.envelope.document_response.signature.structure_invalid");

            var certificateNodes = signatures[0].GetElementsByTagName("X509Certificate", XmlDsigNamespace);
            if (certificateNodes.Count != 1 || certificateNodes[0] is not XmlElement certificateElement)
                return Invalid("fiscal.envelope.document_response.signature.certificate_required");

            var certificateText = new string(certificateElement.InnerText.Where(c => !char.IsWhiteSpace(c)).ToArray());
            if (string.IsNullOrWhiteSpace(certificateText))
                return Invalid("fiscal.envelope.document_response.signature.certificate_required");

            var signedXml = new SignedXml(document);
            signedXml.LoadXml(signatures[0]);

            var signedInfo = signedXml.SignedInfo;
            if (signedInfo is null
                || string.IsNullOrWhiteSpace(signedInfo.CanonicalizationMethod)
                || !AllowedCanonicalizationMethods.Contains(signedInfo.CanonicalizationMethod)
                || string.IsNullOrWhiteSpace(signedInfo.SignatureMethod)
                || !AllowedSignatureMethods.Contains(signedInfo.SignatureMethod))
            {
                return Invalid("fiscal.envelope.document_response.signature.algorithm_unsupported");
            }

            if (signedInfo.References.Count != 1 || signedInfo.References[0] is not Reference reference)
                return Invalid("fiscal.envelope.document_response.signature.reference_invalid");
            if (!string.Equals(reference.Uri, string.Empty, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(reference.DigestMethod)
                || !AllowedDigestMethods.Contains(reference.DigestMethod))
            {
                return Invalid("fiscal.envelope.document_response.signature.reference_invalid");
            }

            var transforms = new List<string>();
            foreach (Transform transform in reference.TransformChain)
            {
                if (string.IsNullOrWhiteSpace(transform.Algorithm)
                    || !AllowedReferenceTransforms.Contains(transform.Algorithm))
                {
                    return Invalid("fiscal.envelope.document_response.signature.transform_unsupported");
                }
                transforms.Add(transform.Algorithm);
            }
            if (transforms.Count == 0
                || !transforms.Contains(SignedXml.XmlDsigEnvelopedSignatureTransformUrl, StringComparer.Ordinal))
            {
                return Invalid("fiscal.envelope.document_response.signature.enveloped_transform_required");
            }

            var rawCertificate = Convert.FromBase64String(certificateText);
            using var certificate = X509CertificateLoader.LoadCertificate(rawCertificate);
            using var rsa = certificate.GetRSAPublicKey();
            if (rsa is null)
                return Invalid("fiscal.envelope.document_response.signature.rsa_certificate_required");

            if (!signedXml.CheckSignature(certificate, verifySignatureOnly: true))
                return Invalid("fiscal.envelope.document_response.signature.check_failed");

            return new FiscalCfeEnvelopeDocumentResponseSignatureVerificationEvidence(
                true,
                ProfileId,
                Convert.ToHexString(SHA256.HashData(certificate.RawData)).ToLowerInvariant(),
                Normalize(certificate.Thumbprint),
                Normalize(certificate.SerialNumber),
                certificate.Subject,
                certificate.Issuer,
                signedInfo.CanonicalizationMethod,
                signedInfo.SignatureMethod,
                reference.DigestMethod,
                reference.Uri ?? string.Empty,
                transforms,
                CertificateTrustValidated: false,
                FailureCode: null);
        }
        catch (Exception ex) when (ex is XmlException or CryptographicException or FormatException or InvalidOperationException)
        {
            return Invalid("fiscal.envelope.document_response.signature.verification_failed");
        }
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();

    private static FiscalCfeEnvelopeDocumentResponseSignatureVerificationEvidence Invalid(string code) =>
        new(
            false,
            ProfileId,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            Array.Empty<string>(),
            CertificateTrustValidated: false,
            FailureCode: code);
}
