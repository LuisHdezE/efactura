using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using EFactura.Application.Fiscal;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Fiscal;

/// <summary>
/// Production-oriented PKCS#12 certificate source. Configuration is external to source control and
/// scoped by organization. Configured certificates are loaded with EphemeralKeySet, pinned by a
/// SHA-256 certificate fingerprint, cached for the process lifetime, and disposed with the source.
/// </summary>
public sealed class PfxFiscalSigningCertificateSource : IFiscalSigningCertificateSource, IDisposable
{
    public const string ConfigurationSection = "FiscalSigning:Certificates";

    private readonly IReadOnlyDictionary<string, X509Certificate2> _certificates;
    private bool _disposed;

    public PfxFiscalSigningCertificateSource(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _certificates = LoadConfiguredCertificates(configuration);
    }

    public ValueTask<X509Certificate2> GetSigningCertificateAsync(
        FiscalSignatureRequest request,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var organizationId = request.OrganizationId?.Trim();
        if (string.IsNullOrWhiteSpace(organizationId))
        {
            throw Error(
                "fiscal.signature.organization_required",
                "Organization id is required to resolve a fiscal signing certificate.");
        }

        if (!_certificates.TryGetValue(organizationId, out var certificate))
        {
            throw Error(
                "fiscal.signature.certificate_not_configured",
                $"No fiscal signing certificate is configured for organization '{organizationId}'.");
        }

        return ValueTask.FromResult(certificate);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        foreach (var certificate in _certificates.Values)
            certificate.Dispose();

        _disposed = true;
    }

    private static IReadOnlyDictionary<string, X509Certificate2> LoadConfiguredCertificates(
        IConfiguration configuration)
    {
        var certificates = new Dictionary<string, X509Certificate2>(StringComparer.Ordinal);
        try
        {
            foreach (var entry in configuration.GetSection(ConfigurationSection).GetChildren())
            {
                var organizationId = Required(entry.Key, "organization id");
                if (certificates.ContainsKey(organizationId))
                {
                    throw Error(
                        "fiscal.signature.certificate_configuration_duplicate",
                        $"More than one fiscal signing certificate is configured for organization '{organizationId}'.");
                }

                var path = Required(entry["Path"], $"PFX path for organization '{organizationId}'");
                var password = Required(entry["Password"], $"PFX password for organization '{organizationId}'");
                var expectedSha256 = NormalizeSha256Thumbprint(
                    Required(
                        entry["ExpectedSha256Thumbprint"],
                        $"expected SHA-256 certificate thumbprint for organization '{organizationId}'"),
                    organizationId);

                if (!Path.IsPathFullyQualified(path))
                {
                    throw Error(
                        "fiscal.signature.certificate_path_not_absolute",
                        $"Fiscal signing certificate path must be absolute for organization '{organizationId}'.");
                }

                if (!File.Exists(path))
                {
                    throw Error(
                        "fiscal.signature.certificate_file_not_found",
                        $"Fiscal signing certificate file was not found for organization '{organizationId}'.");
                }

                X509Certificate2 certificate;
                try
                {
                    certificate = X509CertificateLoader.LoadPkcs12FromFile(
                        path,
                        password,
                        X509KeyStorageFlags.EphemeralKeySet);
                }
                catch (Exception ex) when (ex is CryptographicException
                    or IOException
                    or UnauthorizedAccessException
                    or ArgumentException)
                {
                    throw Error(
                        "fiscal.signature.certificate_load_failed",
                        $"Fiscal signing certificate could not be loaded for organization '{organizationId}'.",
                        ex);
                }

                try
                {
                    if (!certificate.HasPrivateKey)
                    {
                        throw Error(
                            "fiscal.signature.private_key_required",
                            $"Fiscal signing certificate for organization '{organizationId}' does not expose a private key.");
                    }

                    var actualSha256 = NormalizeSha256Thumbprint(
                        certificate.GetCertHashString(HashAlgorithmName.SHA256),
                        organizationId);
                    if (!string.Equals(actualSha256, expectedSha256, StringComparison.Ordinal))
                    {
                        throw Error(
                            "fiscal.signature.certificate_thumbprint_mismatch",
                            $"Fiscal signing certificate SHA-256 thumbprint does not match configuration for organization '{organizationId}'.");
                    }

                    certificates.Add(organizationId, certificate);
                }
                catch
                {
                    certificate.Dispose();
                    throw;
                }
            }

            return certificates;
        }
        catch
        {
            foreach (var certificate in certificates.Values)
                certificate.Dispose();
            throw;
        }
    }

    private static string Required(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Error(
                "fiscal.signature.certificate_configuration_invalid",
                $"Fiscal signing certificate configuration requires {field}.");
        }

        return value.Trim();
    }

    private static string NormalizeSha256Thumbprint(string value, string organizationId)
    {
        var compact = value
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace(":", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .ToUpperInvariant();

        if (compact.Length != 64 || compact.Any(character => !Uri.IsHexDigit(character)))
        {
            throw Error(
                "fiscal.signature.certificate_thumbprint_invalid",
                $"Expected SHA-256 certificate thumbprint is invalid for organization '{organizationId}'.");
        }

        return compact;
    }

    private static FiscalSignatureProviderException Error(
        string code,
        string message,
        Exception? innerException = null) =>
        new(code, message, innerException);
}

/// <summary>
/// Supplies Uruguay-local signing time from the system UTC clock. The Application layer persists the
/// returned instant before any private-key boundary is crossed, so retries reuse the same TmstFirma.
/// </summary>
public sealed class UruguayFiscalSigningTimeSource : IFiscalSigningTimeSource
{
    private static readonly TimeZoneInfo UruguayTimeZone = ResolveUruguayTimeZone();

    public DateTimeOffset GetSigningTimestamp() =>
        TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, UruguayTimeZone);

    private static TimeZoneInfo ResolveUruguayTimeZone()
    {
        foreach (var id in new[] { "America/Montevideo", "Montevideo Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        throw new TimeZoneNotFoundException(
            "Uruguay time zone could not be resolved from the host operating system.");
    }
}
