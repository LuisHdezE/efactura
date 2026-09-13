using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Infrastructure.Fiscal;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CrossCuttingTests;

public sealed class FiscalCfeEnvelopeAckCertificateTrustValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 13, 17, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Configuration_fails_closed_when_no_trust_root_is_supplied()
    {
        var error = Assert.Throws<FiscalCfeEnvelopeAckCertificateTrustException>(() =>
            new DgiFiscalCfeEnvelopeAckCertificateTrustValidator(Configuration()));

        Assert.Equal("fiscal.envelope.ack.trust.root_required", error.Code);
    }

    [Fact]
    public void Configuration_fails_closed_when_root_sha256_pin_does_not_match()
    {
        using var root = CreateRoot("Pinned Root");
        var path = WriteCertificate(root);
        try
        {
            var error = Assert.Throws<FiscalCfeEnvelopeAckCertificateTrustException>(() =>
                new DgiFiscalCfeEnvelopeAckCertificateTrustValidator(Configuration(
                    RootEntry(path, new string('0', 64)))));

            Assert.Equal("fiscal.envelope.ack.trust.certificate_hash_mismatch", error.Code);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Configuration_rejects_non_CA_certificate_as_trust_root()
    {
        using var certificate = CreateSelfSignedLeaf("Not A CA");
        var path = WriteCertificate(certificate);
        try
        {
            var error = Assert.Throws<FiscalCfeEnvelopeAckCertificateTrustException>(() =>
                new DgiFiscalCfeEnvelopeAckCertificateTrustValidator(Configuration(
                    RootEntry(path, Sha256(certificate)))));

            Assert.Equal("fiscal.envelope.ack.trust.ca_certificate_required", error.Code);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Validator_rejects_embedded_certificate_that_does_not_match_prior_signature_evidence()
    {
        using var root = CreateRoot("Test PKI Uruguay Root");
        using var leaf = CreateSelfSignedLeaf("Unexpected ACK Certificate");
        var path = WriteCertificate(root);
        try
        {
            using var validator = new DgiFiscalCfeEnvelopeAckCertificateTrustValidator(Configuration(
                RootEntry(path, Sha256(root))));

            var result = validator.Validate(AckWithCertificate(leaf), new string('a', 64), Now);

            Assert.False(result.IsTrusted);
            Assert.Equal("fiscal.envelope.ack.trust.certificate_source_mismatch", result.FailureCode);
            Assert.Equal(Sha256(leaf), result.CertificateSha256);
            Assert.False(result.ChainBuilt);
            Assert.False(result.RevocationChecked);
            Assert.False(result.DgiIdentityValidated);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void DTD_input_fails_closed_without_overclaiming_trust_or_DGI_identity()
    {
        using var root = CreateRoot("DTD Root");
        var path = WriteCertificate(root);
        try
        {
            using var validator = new DgiFiscalCfeEnvelopeAckCertificateTrustValidator(Configuration(
                RootEntry(path, Sha256(root))));
            const string xml = "<!DOCTYPE x [<!ENTITY e SYSTEM 'file:///etc/passwd'>]><ACKSobre xmlns=\"http://cfe.dgi.gub.uy\">&e;</ACKSobre>";

            var result = validator.Validate(xml, new string('b', 64), Now);

            Assert.False(result.IsTrusted);
            Assert.Equal("fiscal.envelope.ack.trust.validation_failed", result.FailureCode);
            Assert.False(result.DgiIdentityValidated);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static IConfiguration Configuration(params IReadOnlyDictionary<string, string?>[] entries)
    {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var entry in entries)
        foreach (var pair in entry)
            values.Add(pair.Key, pair.Value);

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private static IReadOnlyDictionary<string, string?> RootEntry(string path, string sha256) =>
        new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["FiscalTransport:AckCertificateTrust:RootCertificates:acrn:Path"] = path,
            ["FiscalTransport:AckCertificateTrust:RootCertificates:acrn:ExpectedSha256"] = sha256,
            ["FiscalTransport:AckCertificateTrust:UrlRetrievalTimeoutSeconds"] = "2"
        };

    private static X509Certificate2 CreateRoot(string commonName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={commonName}, O=EFactura Tests, C=UY",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign,
            true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        return request.CreateSelfSigned(Now.AddDays(-1), Now.AddYears(10));
    }

    private static X509Certificate2 CreateSelfSignedLeaf(string commonName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={commonName}, O=EFactura Tests, C=UY",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        return request.CreateSelfSigned(Now.AddDays(-1), Now.AddDays(30));
    }

    private static string AckWithCertificate(X509Certificate2 certificate) =>
        $"<ACKSobre xmlns=\"http://cfe.dgi.gub.uy\"><Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><KeyInfo><X509Data><X509Certificate>{Convert.ToBase64String(certificate.RawData)}</X509Certificate></X509Data></KeyInfo></Signature></ACKSobre>";

    private static string WriteCertificate(X509Certificate2 certificate)
    {
        var path = Path.Combine(Path.GetTempPath(), $"efactura-ack-trust-{Guid.NewGuid():N}.cer");
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Cert));
        return Path.GetFullPath(path);
    }

    private static string Sha256(X509Certificate2 certificate) =>
        Convert.ToHexString(SHA256.HashData(certificate.RawData)).ToLowerInvariant();
}
