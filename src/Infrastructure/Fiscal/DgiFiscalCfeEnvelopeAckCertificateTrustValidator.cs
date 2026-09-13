using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using EFactura.Application.Fiscal;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Fiscal;

/// <summary>
/// Validates the certificate embedded in a cryptographically verified ACKSobre against externally
/// configured, SHA-256-pinned PKI Uruguay trust roots. The chain is built with CustomRootTrust and
/// online revocation checking for the entire chain. Successful PKI validation deliberately does not
/// assert that the end-entity certificate belongs to DGI; that identity boundary requires separate
/// authoritative evidence.
/// </summary>
public sealed class DgiFiscalCfeEnvelopeAckCertificateTrustValidator :
    IFiscalCfeEnvelopeAckCertificateTrustValidator,
    IDisposable
{
    public const string ProfileId = "pki-uruguay-custom-root-online-revocation-v1";
    public const string ConfigurationSection = "FiscalTransport:AckCertificateTrust";

    private const string XmlDsigNamespace = "http://www.w3.org/2000/09/xmldsig#";

    private readonly X509Certificate2Collection _trustedRoots;
    private readonly X509Certificate2Collection _intermediates;
    private readonly TimeSpan _urlRetrievalTimeout;
    private bool _disposed;

    public DgiFiscalCfeEnvelopeAckCertificateTrustValidator(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(ConfigurationSection);
        _trustedRoots = LoadCertificates(section.GetSection("RootCertificates"), required: true, "root");
        _intermediates = LoadCertificates(section.GetSection("IntermediateCertificates"), required: false, "intermediate");

        var timeoutSeconds = section.GetValue<int?>("UrlRetrievalTimeoutSeconds") ?? 15;
        if (timeoutSeconds is < 1 or > 60)
        {
            DisposeCertificates(_trustedRoots);
            DisposeCertificates(_intermediates);
            throw Error(
                "fiscal.envelope.ack.trust.timeout_invalid",
                "ACK certificate trust URL retrieval timeout must be between 1 and 60 seconds.");
        }
        _urlRetrievalTimeout = TimeSpan.FromSeconds(timeoutSeconds);
    }

    public FiscalCfeEnvelopeAckCertificateTrustEvidence Validate(
        string responseXml,
        string expectedCertificateSha256,
        DateTimeOffset validationTimeUtc)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(responseXml))
            return Invalid("fiscal.envelope.ack.trust.response_empty");
        if (!Sha256Value(expectedCertificateSha256))
            return Invalid("fiscal.envelope.ack.trust.expected_certificate_hash_invalid");
        if (validationTimeUtc == default)
            return Invalid("fiscal.envelope.ack.trust.validation_time_invalid");

        try
        {
            using var certificate = ExtractCertificate(responseXml);
            var certificateSha256 = Sha256(certificate);
            if (!string.Equals(certificateSha256, expectedCertificateSha256, StringComparison.OrdinalIgnoreCase))
            {
                return Invalid(
                    "fiscal.envelope.ack.trust.certificate_source_mismatch",
                    certificateSha256);
            }

            var keyUsage = certificate.Extensions.OfType<X509KeyUsageExtension>().SingleOrDefault();
            if (keyUsage is null || !keyUsage.KeyUsages.HasFlag(X509KeyUsageFlags.DigitalSignature))
            {
                return Invalid(
                    "fiscal.envelope.ack.trust.digital_signature_usage_required",
                    certificateSha256);
            }

            using var chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.AddRange(_trustedRoots);
            chain.ChainPolicy.ExtraStore.AddRange(_intermediates);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            chain.ChainPolicy.VerificationTime = validationTimeUtc.UtcDateTime;
            chain.ChainPolicy.UrlRetrievalTimeout = _urlRetrievalTimeout;
            chain.ChainPolicy.DisableCertificateDownloads = false;

            var built = chain.Build(certificate);
            var chainHashes = chain.ChainElements
                .Cast<X509ChainElement>()
                .Select(element => Sha256(element.Certificate))
                .ToArray();

            if (!built || chain.ChainElements.Count < 2)
            {
                var failure = chain.ChainStatus.Any(status =>
                        status.Status.HasFlag(X509ChainStatusFlags.RevocationStatusUnknown)
                        || status.Status.HasFlag(X509ChainStatusFlags.OfflineRevocation))
                    ? "fiscal.envelope.ack.trust.revocation_unavailable"
                    : "fiscal.envelope.ack.trust.chain_invalid";
                return Invalid(failure, certificateSha256, chainHashes);
            }

            var rootCertificate = chain.ChainElements[^1].Certificate;
            var rootSha256 = Sha256(rootCertificate);
            var configuredRoot = _trustedRoots.Cast<X509Certificate2>()
                .Any(root => string.Equals(Sha256(root), rootSha256, StringComparison.OrdinalIgnoreCase));
            if (!configuredRoot)
            {
                return Invalid(
                    "fiscal.envelope.ack.trust.root_not_configured",
                    certificateSha256,
                    chainHashes);
            }

            if (chain.ChainStatus.Any(status => status.Status != X509ChainStatusFlags.NoError))
            {
                return Invalid(
                    "fiscal.envelope.ack.trust.chain_status_invalid",
                    certificateSha256,
                    chainHashes);
            }

            return new FiscalCfeEnvelopeAckCertificateTrustEvidence(
                IsTrusted: true,
                ValidationProfileId: ProfileId,
                CertificateSha256: certificateSha256,
                TrustedRootSha256: rootSha256,
                ChainCertificateSha256: chainHashes,
                ChainBuilt: true,
                RevocationChecked: true,
                RevocationMode: "Online",
                DgiIdentityValidated: false,
                FailureCode: null);
        }
        catch (Exception ex) when (ex is XmlException
            or CryptographicException
            or FormatException
            or InvalidOperationException)
        {
            return Invalid("fiscal.envelope.ack.trust.validation_failed");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        DisposeCertificates(_trustedRoots);
        DisposeCertificates(_intermediates);
        _disposed = true;
    }

    private static X509Certificate2 ExtractCertificate(string responseXml)
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

        var certificateNodes = document.GetElementsByTagName("X509Certificate", XmlDsigNamespace);
        if (certificateNodes.Count != 1 || certificateNodes[0] is not XmlElement certificateElement)
            throw new CryptographicException("Exactly one embedded X509Certificate is required.");

        var certificateText = new string(certificateElement.InnerText.Where(c => !char.IsWhiteSpace(c)).ToArray());
        if (string.IsNullOrWhiteSpace(certificateText))
            throw new CryptographicException("Embedded X509Certificate is empty.");

        return X509CertificateLoader.LoadCertificate(Convert.FromBase64String(certificateText));
    }

    private static X509Certificate2Collection LoadCertificates(
        IConfigurationSection section,
        bool required,
        string role)
    {
        var certificates = new X509Certificate2Collection();
        try
        {
            foreach (var entry in section.GetChildren())
            {
                var path = Required(entry["Path"], $"{role} certificate path");
                var expectedSha256 = NormalizeSha256(
                    Required(entry["ExpectedSha256"], $"expected SHA-256 for {role} certificate"));
                if (!Path.IsPathFullyQualified(path))
                {
                    throw Error(
                        "fiscal.envelope.ack.trust.certificate_path_not_absolute",
                        $"Configured ACK {role} certificate path must be absolute.");
                }
                if (!File.Exists(path))
                {
                    throw Error(
                        "fiscal.envelope.ack.trust.certificate_file_not_found",
                        $"Configured ACK {role} certificate file was not found.");
                }

                X509Certificate2 certificate;
                try
                {
                    certificate = X509CertificateLoader.LoadCertificateFromFile(path);
                }
                catch (Exception ex) when (ex is CryptographicException
                    or IOException
                    or UnauthorizedAccessException
                    or ArgumentException)
                {
                    throw Error(
                        "fiscal.envelope.ack.trust.certificate_load_failed",
                        $"Configured ACK {role} certificate could not be loaded.",
                        ex);
                }

                try
                {
                    var actualSha256 = Sha256(certificate);
                    if (!string.Equals(actualSha256, expectedSha256, StringComparison.OrdinalIgnoreCase))
                    {
                        throw Error(
                            "fiscal.envelope.ack.trust.certificate_hash_mismatch",
                            $"Configured ACK {role} certificate SHA-256 does not match its pin.");
                    }

                    var basicConstraints = certificate.Extensions.OfType<X509BasicConstraintsExtension>().SingleOrDefault();
                    if (basicConstraints is null || !basicConstraints.CertificateAuthority)
                    {
                        throw Error(
                            "fiscal.envelope.ack.trust.ca_certificate_required",
                            $"Configured ACK {role} certificate must be a CA certificate.");
                    }

                    certificates.Add(certificate);
                }
                catch
                {
                    certificate.Dispose();
                    throw;
                }
            }

            if (required && certificates.Count == 0)
            {
                throw Error(
                    "fiscal.envelope.ack.trust.root_required",
                    "At least one SHA-256-pinned PKI Uruguay root certificate must be configured.");
            }

            return certificates;
        }
        catch
        {
            DisposeCertificates(certificates);
            throw;
        }
    }

    private static string Required(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Error(
                "fiscal.envelope.ack.trust.configuration_invalid",
                $"ACK certificate trust configuration requires {field}.");
        }
        return value.Trim();
    }

    private static string NormalizeSha256(string value)
    {
        var compact = value
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(":", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
        if (!Sha256Value(compact))
        {
            throw Error(
                "fiscal.envelope.ack.trust.certificate_hash_invalid",
                "Configured ACK trust certificate SHA-256 pin is invalid.");
        }
        return compact;
    }

    private static string Sha256(X509Certificate2 certificate) =>
        Convert.ToHexString(SHA256.HashData(certificate.RawData)).ToLowerInvariant();

    private static bool Sha256Value(string? value) =>
        value is not null && value.Length == 64 && value.All(Uri.IsHexDigit);

    private static FiscalCfeEnvelopeAckCertificateTrustEvidence Invalid(
        string code,
        string certificateSha256 = "",
        IReadOnlyList<string>? chainCertificateSha256 = null) =>
        new(
            IsTrusted: false,
            ValidationProfileId: ProfileId,
            CertificateSha256: certificateSha256,
            TrustedRootSha256: string.Empty,
            ChainCertificateSha256: chainCertificateSha256 ?? Array.Empty<string>(),
            ChainBuilt: false,
            RevocationChecked: false,
            RevocationMode: "Online",
            DgiIdentityValidated: false,
            FailureCode: code);

    private static void DisposeCertificates(X509Certificate2Collection certificates)
    {
        foreach (var certificate in certificates.Cast<X509Certificate2>())
            certificate.Dispose();
        certificates.Clear();
    }

    private static FiscalCfeEnvelopeAckCertificateTrustException Error(
        string code,
        string message,
        Exception? innerException = null) =>
        new(code, message, innerException);
}

public sealed class FiscalCfeEnvelopeAckCertificateTrustException : Exception
{
    public FiscalCfeEnvelopeAckCertificateTrustException(
        string code,
        string message,
        Exception? innerException = null)
        : base(message, innerException) => Code = code;

    public string Code { get; }
}
