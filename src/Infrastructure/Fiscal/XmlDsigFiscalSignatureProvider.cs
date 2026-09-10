using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;

namespace Infrastructure.Fiscal;

/// <summary>
/// Explicit XMLDSig profile consumed by the Infrastructure signer. The built-in profile is an
/// evidence-backed SHA-256 compatibility candidate, not a claim that DGI currently publishes one
/// unique normative XMLDSig algorithm tuple. Production enablement still requires DGI test proof.
/// </summary>
public sealed record FiscalXmlSignatureProfile(
    string ProfileId,
    string CanonicalizationMethod,
    string SignatureMethod,
    string DigestMethod,
    string ReferenceUri,
    IReadOnlyList<string> ReferenceTransforms,
    bool IncludeCertificate,
    bool IncludeIssuerSerial)
{
    public const string InclusiveC14N = "http://www.w3.org/TR/2001/REC-xml-c14n-20010315";
    public const string ExclusiveC14N = "http://www.w3.org/2001/10/xml-exc-c14n#";
    public const string RsaSha256 = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
    public const string Sha256 = "http://www.w3.org/2001/04/xmlenc#sha256";
    public const string EnvelopedSignature = "http://www.w3.org/2000/09/xmldsig#enveloped-signature";

    public static FiscalXmlSignatureProfile DgiCfeSha256EvidenceBackedV1 { get; } = new(
        "dgi-cfe-sha256-evidence-backed-v1",
        InclusiveC14N,
        RsaSha256,
        Sha256,
        string.Empty,
        new[] { EnvelopedSignature },
        IncludeCertificate: true,
        IncludeIssuerSerial: true);
}

/// <summary>
/// Infrastructure-only certificate/private-key boundary. The source owns certificate lifetime and
/// may later resolve a platform store, PFX, HSM or managed key provider without leaking those details
/// into Application or Domain.
/// </summary>
public interface IFiscalSigningCertificateSource
{
    ValueTask<X509Certificate2> GetSigningCertificateAsync(
        FiscalSignatureRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class FiscalSignatureProviderException : CryptographicException
{
    public FiscalSignatureProviderException(string code, string message, Exception innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}

/// <summary>
/// Creates an enveloped XML Digital Signature over the deterministic CFE signing payload. The
/// payload must already contain durable TmstFirma and must not contain Adenda or a prior Signature.
/// The adapter validates payload integrity, certificate suitability and its own produced signature
/// before returning signed XML plus non-secret signature/certificate evidence. It does not persist
/// artifacts or contact DGI.
/// </summary>
public sealed class XmlDsigFiscalSignatureProvider : IFiscalSignatureProvider
{
    private const string CfeNamespace = "http://cfe.dgi.gub.uy";
    private const string XmlDsigNamespace = "http://www.w3.org/2000/09/xmldsig#";

    private static readonly HashSet<string> Sha2CertificateSignatureOids = new(StringComparer.Ordinal)
    {
        "1.2.840.113549.1.1.11",
        "1.2.840.113549.1.1.12",
        "1.2.840.113549.1.1.13",
        "1.2.840.10045.4.3.2",
        "1.2.840.10045.4.3.3",
        "1.2.840.10045.4.3.4"
    };

    private readonly IFiscalSigningCertificateSource _certificateSource;
    private readonly FiscalXmlSignatureProfile _profile;

    public XmlDsigFiscalSignatureProvider(
        IFiscalSigningCertificateSource certificateSource,
        FiscalXmlSignatureProfile profile = null)
    {
        _certificateSource = certificateSource
            ?? throw new ArgumentNullException(nameof(certificateSource));
        _profile = profile ?? FiscalXmlSignatureProfile.DgiCfeSha256EvidenceBackedV1;
        ValidateProfile(_profile);
    }

    public async Task<FiscalSignatureResult> SignAsync(
        FiscalSignatureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateRequest(request);
        var document = LoadAndValidatePayload(request);

        X509Certificate2 certificate;
        try
        {
            certificate = await _certificateSource.GetSigningCertificateAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (FiscalSignatureProviderException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw Error(
                "fiscal.signature.certificate_source_failed",
                "The fiscal signing certificate source failed.",
                ex);
        }

        if (certificate is null)
            throw Error(
                "fiscal.signature.certificate_required",
                "A fiscal signing certificate is required.");

        EnsureCertificateUsable(certificate, request.SigningTimestamp);

        try
        {
            using var privateKey = certificate.GetRSAPrivateKey();
            if (privateKey is null)
                throw Error(
                    "fiscal.signature.rsa_private_key_required",
                    "The fiscal signing certificate must expose an RSA private key.");

            var signedXml = new SignedXml(document)
            {
                SigningKey = privateKey
            };

            signedXml.SignedInfo.CanonicalizationMethod = _profile.CanonicalizationMethod;
            signedXml.SignedInfo.SignatureMethod = _profile.SignatureMethod;

            var reference = new Reference
            {
                Uri = _profile.ReferenceUri,
                DigestMethod = _profile.DigestMethod
            };

            foreach (var transform in _profile.ReferenceTransforms)
                reference.AddTransform(CreateTransform(transform));

            signedXml.AddReference(reference);
            signedXml.KeyInfo = BuildKeyInfo(certificate);
            signedXml.ComputeSignature();

            var root = document.DocumentElement
                ?? throw Error("fiscal.signature.root_required", "CFE XML requires a root element.");
            root.AppendChild(document.ImportNode(signedXml.GetXml(), deep: true));

            VerifyProducedSignature(document, certificate);
            var signedContent = document.OuterXml;
            return new FiscalSignatureResult(
                signedContent,
                Sha256(signedContent),
                _profile.ProfileId,
                NormalizeIdentifier(certificate.Thumbprint),
                NormalizeIdentifier(certificate.SerialNumber));
        }
        catch (FiscalSignatureProviderException)
        {
            throw;
        }
        catch (CryptographicException ex)
        {
            throw Error(
                "fiscal.signature.compute_failed",
                "XMLDSig signature generation failed.",
                ex);
        }
    }

    private static void ValidateProfile(FiscalXmlSignatureProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (string.IsNullOrWhiteSpace(profile.ProfileId)
            || string.IsNullOrWhiteSpace(profile.CanonicalizationMethod)
            || string.IsNullOrWhiteSpace(profile.SignatureMethod)
            || string.IsNullOrWhiteSpace(profile.DigestMethod))
        {
            throw Error(
                "fiscal.signature.profile_invalid",
                "XMLDSig profile identifiers and algorithms are required.");
        }

        if (profile.SignatureMethod.Contains("sha1", StringComparison.OrdinalIgnoreCase)
            || profile.DigestMethod.Contains("sha1", StringComparison.OrdinalIgnoreCase))
        {
            throw Error(
                "fiscal.signature.profile_sha1_forbidden",
                "SHA-1 XMLDSig profiles are not enabled by this adapter.");
        }

        if (!string.Equals(profile.SignatureMethod, FiscalXmlSignatureProfile.RsaSha256, StringComparison.Ordinal)
            || !string.Equals(profile.DigestMethod, FiscalXmlSignatureProfile.Sha256, StringComparison.Ordinal))
        {
            throw Error(
                "fiscal.signature.profile_algorithm_unsupported",
                "This adapter currently supports the evidence-backed RSA-SHA256/SHA256 profile only.");
        }

        if (profile.CanonicalizationMethod is not FiscalXmlSignatureProfile.InclusiveC14N
            and not FiscalXmlSignatureProfile.ExclusiveC14N)
        {
            throw Error(
                "fiscal.signature.profile_c14n_unsupported",
                "Unsupported XMLDSig canonicalization method.");
        }

        if (!string.Equals(profile.ReferenceUri, string.Empty, StringComparison.Ordinal))
            throw Error(
                "fiscal.signature.profile_reference_unsupported",
                "This adapter currently supports the whole-document empty Reference URI only.");

        if (profile.ReferenceTransforms is null || profile.ReferenceTransforms.Count == 0)
            throw Error(
                "fiscal.signature.profile_transform_required",
                "At least the enveloped-signature transform is required.");

        if (!profile.ReferenceTransforms.Contains(FiscalXmlSignatureProfile.EnvelopedSignature, StringComparer.Ordinal))
            throw Error(
                "fiscal.signature.profile_enveloped_required",
                "The enveloped-signature transform is required.");

        foreach (var transform in profile.ReferenceTransforms)
        {
            if (transform is not FiscalXmlSignatureProfile.EnvelopedSignature
                and not FiscalXmlSignatureProfile.InclusiveC14N
                and not FiscalXmlSignatureProfile.ExclusiveC14N)
            {
                throw Error(
                    "fiscal.signature.profile_transform_unsupported",
                    $"Unsupported XMLDSig reference transform: {transform}");
            }
        }

        if (!profile.IncludeIssuerSerial)
            throw Error(
                "fiscal.signature.profile_issuer_serial_required",
                "The DGI compatibility profile must include X509 issuer and serial evidence.");
    }

    private static void ValidateRequest(FiscalSignatureRequest request)
    {
        if (request.FiscalDocumentId == Guid.Empty)
            throw Error("fiscal.signature.document_id_required", "Fiscal document id is required.");
        if (string.IsNullOrWhiteSpace(request.SigningPayloadXml))
            throw Error("fiscal.signature.payload_required", "Signing payload XML is required.");
        if (string.IsNullOrWhiteSpace(request.SigningPayloadHash))
            throw Error("fiscal.signature.payload_hash_required", "Signing payload hash is required.");

        var actualHash = Sha256(request.SigningPayloadXml);
        if (!string.Equals(actualHash, request.SigningPayloadHash, StringComparison.Ordinal))
            throw Error(
                "fiscal.signature.payload_hash_mismatch",
                "Signing payload XML no longer matches its durable payload hash.");
    }

    private static XmlDocument LoadAndValidatePayload(FiscalSignatureRequest request)
    {
        var document = new XmlDocument
        {
            PreserveWhitespace = true,
            XmlResolver = null
        };

        try
        {
            document.LoadXml(request.SigningPayloadXml);
        }
        catch (XmlException ex)
        {
            throw Error("fiscal.signature.xml_invalid", "Signing payload XML is not well formed.", ex);
        }

        var root = document.DocumentElement
            ?? throw Error("fiscal.signature.root_required", "CFE XML requires a root element.");
        if (!string.Equals(root.LocalName, "CFE", StringComparison.Ordinal)
            || !string.Equals(root.NamespaceURI, CfeNamespace, StringComparison.Ordinal))
        {
            throw Error(
                "fiscal.signature.root_invalid",
                "Signing payload must use the official DGI CFE root namespace.");
        }

        if (document.GetElementsByTagName("Signature", XmlDsigNamespace).Count != 0)
            throw Error(
                "fiscal.signature.signature_already_present",
                "Signing payload must not contain a pre-existing XMLDSig Signature.");

        if (ContainsElement(document, "Adenda"))
            throw Error(
                "fiscal.signature.adenda_not_signable",
                "Zone J Adenda is outside the fiscal signature scope and must not be in the signing payload.");

        var expectedFamily = request.CfeFamily switch
        {
            CfeFamily.ETicket or
            CfeFamily.ETicketCreditNote or
            CfeFamily.ETicketDebitNote => "eTck",
            CfeFamily.EFactura or
            CfeFamily.EFacturaCreditNote or
            CfeFamily.EFacturaDebitNote => "eFact",
            CfeFamily.EFacturaExportacion => throw Error(
                "fiscal.signature.export_not_supported",
                "Release-1 XMLDSig signing does not enable export CFE."),
            _ => throw Error(
                "fiscal.signature.family_not_supported",
                "CFE family is not supported by the XMLDSig signer.")
        };

        var familyElements = root.ChildNodes
            .OfType<XmlElement>()
            .Where(element => string.Equals(element.NamespaceURI, CfeNamespace, StringComparison.Ordinal))
            .ToArray();
        if (familyElements.Length != 1
            || !string.Equals(familyElements[0].LocalName, expectedFamily, StringComparison.Ordinal))
        {
            throw Error(
                "fiscal.signature.family_invalid",
                "Signing payload does not contain exactly the expected DGI CFE family element.");
        }

        var family = familyElements[0];
        var firstElement = family.ChildNodes.OfType<XmlElement>().FirstOrDefault();
        if (firstElement is null
            || !string.Equals(firstElement.LocalName, "TmstFirma", StringComparison.Ordinal)
            || !string.Equals(firstElement.NamespaceURI, CfeNamespace, StringComparison.Ordinal))
        {
            throw Error(
                "fiscal.signature.tmstfirma_required",
                "Signing payload must begin the CFE family with durable TmstFirma.");
        }

        var expectedTimestamp = request.SigningTimestamp.ToString(
            "yyyy-MM-dd'T'HH:mm:sszzz",
            CultureInfo.InvariantCulture);
        if (!string.Equals(firstElement.InnerText, expectedTimestamp, StringComparison.Ordinal))
            throw Error(
                "fiscal.signature.tmstfirma_mismatch",
                "TmstFirma must match the durable signing timestamp exactly.");

        return document;
    }

    private static void EnsureCertificateUsable(X509Certificate2 certificate, DateTimeOffset signingTimestamp)
    {
        if (!certificate.HasPrivateKey)
            throw Error(
                "fiscal.signature.private_key_required",
                "The fiscal signing certificate does not expose a private key.");

        var signingUtc = signingTimestamp.ToUniversalTime();
        var notBeforeUtc = certificate.NotBefore.ToUniversalTime();
        var notAfterUtc = certificate.NotAfter.ToUniversalTime();
        if (signingUtc < notBeforeUtc)
            throw Error(
                "fiscal.signature.certificate_not_yet_valid",
                "The fiscal signing certificate is not yet valid at TmstFirma.");
        if (signingUtc > notAfterUtc)
            throw Error(
                "fiscal.signature.certificate_expired",
                "The fiscal signing certificate is expired at TmstFirma.");

        var signatureOid = certificate.SignatureAlgorithm?.Value;
        if (string.IsNullOrWhiteSpace(signatureOid) || !Sha2CertificateSignatureOids.Contains(signatureOid))
            throw Error(
                "fiscal.signature.certificate_sha2_required",
                "The fiscal signing certificate must use a recognized SHA-2 certificate signature algorithm.");
    }

    private KeyInfo BuildKeyInfo(X509Certificate2 certificate)
    {
        var x509Data = new KeyInfoX509Data();
        if (_profile.IncludeCertificate)
            x509Data.AddCertificate(certificate);
        if (_profile.IncludeIssuerSerial)
            x509Data.AddIssuerSerial(certificate.Issuer, DecimalSerial(certificate));

        var keyInfo = new KeyInfo();
        keyInfo.AddClause(x509Data);
        return keyInfo;
    }

    private static Transform CreateTransform(string algorithm) => algorithm switch
    {
        FiscalXmlSignatureProfile.EnvelopedSignature => new XmlDsigEnvelopedSignatureTransform(),
        FiscalXmlSignatureProfile.InclusiveC14N => new XmlDsigC14NTransform(),
        FiscalXmlSignatureProfile.ExclusiveC14N => new XmlDsigExcC14NTransform(),
        _ => throw Error(
            "fiscal.signature.profile_transform_unsupported",
            $"Unsupported XMLDSig reference transform: {algorithm}")
    };

    private static void VerifyProducedSignature(XmlDocument document, X509Certificate2 certificate)
    {
        var signatures = document.GetElementsByTagName("Signature", XmlDsigNamespace);
        if (signatures.Count != 1 || signatures[0] is not XmlElement signatureElement)
            throw Error(
                "fiscal.signature.verification_structure_invalid",
                "Exactly one XMLDSig Signature must be produced.");

        var verifier = new SignedXml(document);
        verifier.LoadXml(signatureElement);
        if (!verifier.CheckSignature(certificate, verifySignatureOnly: true))
            throw Error(
                "fiscal.signature.verification_failed",
                "The produced XMLDSig signature could not be verified with the signing certificate.");
    }

    private static bool ContainsElement(XmlDocument document, string localName)
    {
        foreach (XmlNode node in document.GetElementsByTagName("*"))
        {
            if (node is XmlElement element
                && string.Equals(element.LocalName, localName, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    private static string DecimalSerial(X509Certificate2 certificate)
    {
        var serialBytes = certificate.GetSerialNumber();
        var value = new BigInteger(serialBytes, isUnsigned: true, isBigEndian: false);
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string NormalizeIdentifier(string value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace(" ", string.Empty, StringComparison.Ordinal).Trim().ToLowerInvariant();

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static FiscalSignatureProviderException Error(
        string code,
        string message,
        Exception innerException = null) => new(code, message, innerException);
}
