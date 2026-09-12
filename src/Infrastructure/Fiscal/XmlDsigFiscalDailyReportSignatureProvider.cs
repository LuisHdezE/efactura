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
/// Infrastructure-only certificate boundary for Reporte Diario signing. Implementations may reuse
/// the same organization-scoped fiscal certificate material as CFE signing without collapsing the
/// two Application contracts into one artifact type.
/// </summary>
public interface IFiscalDailyReportSigningCertificateSource
{
    ValueTask<X509Certificate2> GetSigningCertificateAsync(
        FiscalDailyReportSignatureRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Adds the mandatory final XMLDSig to a deterministic Reporte Diario v13.2 payload. The default
/// profile deliberately reuses the accepted SHA-256 compatibility tuple while carrying a distinct
/// Reporte profile id; this remains evidence-backed compatibility policy, not a claim that DGI
/// publishes one unique current XMLDSig algorithm tuple.
/// </summary>
public sealed class XmlDsigFiscalDailyReportSignatureProvider : IFiscalDailyReportSignatureProvider
{
    public const string DailyReportProfileId = "dgi-daily-report-sha256-evidence-backed-v1";

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

    public static FiscalXmlSignatureProfile DailyReportSha256EvidenceBackedV1 { get; } =
        FiscalXmlSignatureProfile.DgiCfeSha256EvidenceBackedV1 with
        {
            ProfileId = DailyReportProfileId
        };

    private readonly IFiscalDailyReportSigningCertificateSource _certificateSource;
    private readonly FiscalXmlSignatureProfile _profile;

    public XmlDsigFiscalDailyReportSignatureProvider(
        IFiscalDailyReportSigningCertificateSource certificateSource,
        FiscalXmlSignatureProfile? profile = null)
    {
        _certificateSource = certificateSource
            ?? throw new ArgumentNullException(nameof(certificateSource));
        _profile = profile ?? DailyReportSha256EvidenceBackedV1;
        ValidateProfile(_profile);
    }

    public async Task<FiscalDailyReportSignatureResult> SignAsync(
        FiscalDailyReportSignatureRequest request,
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
                "fiscal.daily_report.signature.certificate_source_failed",
                "The Reporte Diario signing certificate source failed.",
                ex);
        }

        if (certificate is null)
        {
            throw Error(
                "fiscal.daily_report.signature.certificate_required",
                "A fiscal signing certificate is required for Reporte Diario.");
        }

        EnsureCertificateUsable(certificate, request.SigningTimestamp);

        try
        {
            using var privateKey = certificate.GetRSAPrivateKey();
            if (privateKey is null)
            {
                throw Error(
                    "fiscal.daily_report.signature.rsa_private_key_required",
                    "The Reporte Diario signing certificate must expose an RSA private key.");
            }

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
                ?? throw Error(
                    "fiscal.daily_report.signature.root_required",
                    "Reporte Diario XML requires a root element.");
            root.AppendChild(document.ImportNode(signedXml.GetXml(), deep: true));

            VerifyProducedSignature(document, certificate);
            var signedContent = document.OuterXml;
            return new FiscalDailyReportSignatureResult(
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
                "fiscal.daily_report.signature.compute_failed",
                "Reporte Diario XMLDSig signature generation failed.",
                ex);
        }
    }

    private static void ValidateProfile(FiscalXmlSignatureProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.ProfileId)
            || string.IsNullOrWhiteSpace(profile.CanonicalizationMethod)
            || string.IsNullOrWhiteSpace(profile.SignatureMethod)
            || string.IsNullOrWhiteSpace(profile.DigestMethod))
        {
            throw Error(
                "fiscal.daily_report.signature.profile_invalid",
                "Reporte Diario XMLDSig profile identifiers and algorithms are required.");
        }

        if (profile.SignatureMethod.Contains("sha1", StringComparison.OrdinalIgnoreCase)
            || profile.DigestMethod.Contains("sha1", StringComparison.OrdinalIgnoreCase))
        {
            throw Error(
                "fiscal.daily_report.signature.profile_sha1_forbidden",
                "SHA-1 Reporte Diario XMLDSig profiles are not enabled.");
        }

        if (!string.Equals(profile.SignatureMethod, FiscalXmlSignatureProfile.RsaSha256, StringComparison.Ordinal)
            || !string.Equals(profile.DigestMethod, FiscalXmlSignatureProfile.Sha256, StringComparison.Ordinal))
        {
            throw Error(
                "fiscal.daily_report.signature.profile_algorithm_unsupported",
                "Reporte Diario signing currently supports the evidence-backed RSA-SHA256/SHA256 profile only.");
        }

        if (profile.CanonicalizationMethod is not FiscalXmlSignatureProfile.InclusiveC14N
            and not FiscalXmlSignatureProfile.ExclusiveC14N)
        {
            throw Error(
                "fiscal.daily_report.signature.profile_c14n_unsupported",
                "Unsupported Reporte Diario XMLDSig canonicalization method.");
        }

        if (!string.Equals(profile.ReferenceUri, string.Empty, StringComparison.Ordinal))
        {
            throw Error(
                "fiscal.daily_report.signature.profile_reference_unsupported",
                "Reporte Diario signing currently supports the whole-document empty Reference URI only.");
        }

        if (profile.ReferenceTransforms is null
            || profile.ReferenceTransforms.Count == 0
            || !profile.ReferenceTransforms.Contains(FiscalXmlSignatureProfile.EnvelopedSignature, StringComparer.Ordinal))
        {
            throw Error(
                "fiscal.daily_report.signature.profile_enveloped_required",
                "Reporte Diario signing requires the enveloped-signature transform.");
        }

        foreach (var transform in profile.ReferenceTransforms)
        {
            if (transform is not FiscalXmlSignatureProfile.EnvelopedSignature
                and not FiscalXmlSignatureProfile.InclusiveC14N
                and not FiscalXmlSignatureProfile.ExclusiveC14N)
            {
                throw Error(
                    "fiscal.daily_report.signature.profile_transform_unsupported",
                    $"Unsupported Reporte Diario XMLDSig reference transform: {transform}");
            }
        }

        if (!profile.IncludeIssuerSerial)
        {
            throw Error(
                "fiscal.daily_report.signature.profile_issuer_serial_required",
                "The Reporte Diario compatibility profile must include X509 issuer and serial evidence.");
        }
    }

    private static void ValidateRequest(FiscalDailyReportSignatureRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.OrganizationId))
            throw Error("fiscal.daily_report.signature.organization_required", "Organization id is required.");
        if (!string.Equals(
                request.FunctionalFormatVersion,
                FiscalDailyReportV13_2WireContract.Version,
                StringComparison.Ordinal))
        {
            throw Error(
                "fiscal.daily_report.signature.format_version_unsupported",
                "Reporte Diario signature request must use the pinned v13.2 functional format.");
        }
        if (string.IsNullOrWhiteSpace(request.ProjectionFingerprint))
            throw Error("fiscal.daily_report.signature.projection_fingerprint_required", "Projection fingerprint is required.");
        if (string.IsNullOrWhiteSpace(request.UnsignedXml))
            throw Error("fiscal.daily_report.signature.unsigned_xml_required", "Unsigned Reporte Diario XML is required.");
        if (string.IsNullOrWhiteSpace(request.UnsignedContentHash))
            throw Error("fiscal.daily_report.signature.unsigned_hash_required", "Unsigned Reporte Diario content hash is required.");
        if (request.SigningTimestamp.Ticks % TimeSpan.TicksPerSecond != 0)
        {
            throw Error(
                "fiscal.daily_report.signature.signing_timestamp_precision_invalid",
                "Reporte Diario signing timestamp must be frozen at whole-second precision.");
        }

        var actualHash = Sha256(request.UnsignedXml);
        if (!string.Equals(actualHash, request.UnsignedContentHash, StringComparison.Ordinal))
        {
            throw Error(
                "fiscal.daily_report.signature.unsigned_hash_mismatch",
                "Unsigned Reporte Diario XML no longer matches its supplied content hash.");
        }
    }

    private static XmlDocument LoadAndValidatePayload(FiscalDailyReportSignatureRequest request)
    {
        var document = new XmlDocument
        {
            PreserveWhitespace = true,
            XmlResolver = null
        };

        try
        {
            document.LoadXml(request.UnsignedXml);
        }
        catch (XmlException ex)
        {
            throw Error(
                "fiscal.daily_report.signature.xml_invalid",
                "Unsigned Reporte Diario XML is not well formed.",
                ex);
        }

        var root = document.DocumentElement
            ?? throw Error(
                "fiscal.daily_report.signature.root_required",
                "Reporte Diario XML requires a root element.");
        if (!string.Equals(root.LocalName, FiscalDailyReportV13_2WireContract.XmlRootElementName, StringComparison.Ordinal)
            || !string.Equals(root.NamespaceURI, FiscalDailyReportV13_2WireContract.XmlNamespace, StringComparison.Ordinal))
        {
            throw Error(
                "fiscal.daily_report.signature.root_invalid",
                "Unsigned Reporte Diario must use the pinned DGI Reporte root and namespace.");
        }

        if (document.GetElementsByTagName(
                FiscalDailyReportV13_2WireContract.XmlDigitalSignatureElementName,
                XmlDsigNamespace).Count != 0)
        {
            throw Error(
                "fiscal.daily_report.signature.signature_already_present",
                "Unsigned Reporte Diario must not contain a pre-existing ds:Signature.");
        }

        var firstChild = root.ChildNodes.OfType<XmlElement>().FirstOrDefault();
        if (firstChild is null
            || !string.Equals(firstChild.LocalName, FiscalDailyReportV13_2WireContract.XmlCaratulaElementName, StringComparison.Ordinal)
            || !string.Equals(firstChild.NamespaceURI, FiscalDailyReportV13_2WireContract.XmlNamespace, StringComparison.Ordinal)
            || !string.Equals(firstChild.GetAttribute("version"), FiscalDailyReportV13_2WireContract.XmlCaratulaVersion, StringComparison.Ordinal))
        {
            throw Error(
                "fiscal.daily_report.signature.caratula_invalid",
                "Reporte Diario signing payload must begin with the pinned Caratula v1.0.");
        }

        var timestampElements = firstChild.ChildNodes
            .OfType<XmlElement>()
            .Where(element =>
                string.Equals(element.LocalName, "TmstFirmaEnv", StringComparison.Ordinal)
                && string.Equals(element.NamespaceURI, FiscalDailyReportV13_2WireContract.XmlNamespace, StringComparison.Ordinal))
            .ToArray();
        if (timestampElements.Length != 1)
        {
            throw Error(
                "fiscal.daily_report.signature.tmstfirmaenv_required",
                "Reporte Diario Caratula must contain exactly one TmstFirmaEnv.");
        }

        var expectedTimestamp = request.SigningTimestamp.ToString(
            "yyyy-MM-dd'T'HH:mm:sszzz",
            CultureInfo.InvariantCulture);
        if (!string.Equals(timestampElements[0].InnerText, expectedTimestamp, StringComparison.Ordinal))
        {
            throw Error(
                "fiscal.daily_report.signature.tmstfirmaenv_mismatch",
                "TmstFirmaEnv must match the frozen Reporte Diario signing timestamp exactly.");
        }

        return document;
    }

    private static void EnsureCertificateUsable(
        X509Certificate2 certificate,
        DateTimeOffset signingTimestamp)
    {
        if (!certificate.HasPrivateKey)
        {
            throw Error(
                "fiscal.daily_report.signature.private_key_required",
                "The fiscal signing certificate does not expose a private key.");
        }

        var signingUtc = signingTimestamp.ToUniversalTime();
        if (signingUtc < certificate.NotBefore.ToUniversalTime())
        {
            throw Error(
                "fiscal.daily_report.signature.certificate_not_yet_valid",
                "The fiscal signing certificate is not yet valid at TmstFirmaEnv.");
        }
        if (signingUtc > certificate.NotAfter.ToUniversalTime())
        {
            throw Error(
                "fiscal.daily_report.signature.certificate_expired",
                "The fiscal signing certificate is expired at TmstFirmaEnv.");
        }

        var signatureOid = certificate.SignatureAlgorithm?.Value;
        if (string.IsNullOrWhiteSpace(signatureOid) || !Sha2CertificateSignatureOids.Contains(signatureOid))
        {
            throw Error(
                "fiscal.daily_report.signature.certificate_sha2_required",
                "The fiscal signing certificate must use a recognized SHA-2 certificate signature algorithm.");
        }
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
            "fiscal.daily_report.signature.profile_transform_unsupported",
            $"Unsupported Reporte Diario XMLDSig reference transform: {algorithm}")
    };

    private static void VerifyProducedSignature(
        XmlDocument document,
        X509Certificate2 certificate)
    {
        var signatures = document.GetElementsByTagName(
            FiscalDailyReportV13_2WireContract.XmlDigitalSignatureElementName,
            XmlDsigNamespace);
        if (signatures.Count != 1 || signatures[0] is not XmlElement signatureElement)
        {
            throw Error(
                "fiscal.daily_report.signature.verification_structure_invalid",
                "Exactly one Reporte Diario XMLDSig Signature must be produced.");
        }

        if (!ReferenceEquals(signatureElement, document.DocumentElement?.LastChild))
        {
            throw Error(
                "fiscal.daily_report.signature.signature_position_invalid",
                "Reporte Diario ds:Signature must be the final child of Reporte.");
        }

        var verifier = new SignedXml(document);
        verifier.LoadXml(signatureElement);
        if (!verifier.CheckSignature(certificate, verifySignatureOnly: true))
        {
            throw Error(
                "fiscal.daily_report.signature.verification_failed",
                "The produced Reporte Diario XMLDSig could not be verified with the signing certificate.");
        }
    }

    private static string DecimalSerial(X509Certificate2 certificate)
    {
        var serialBytes = certificate.GetSerialNumber();
        var value = new BigInteger(serialBytes, isUnsigned: true, isBigEndian: false);
        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string NormalizeIdentifier(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace(" ", string.Empty, StringComparison.Ordinal).Trim().ToLowerInvariant();

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static FiscalSignatureProviderException Error(
        string code,
        string message,
        Exception? innerException = null) => new(code, message, innerException);
}
