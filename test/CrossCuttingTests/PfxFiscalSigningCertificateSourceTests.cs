using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using Infrastructure.Fiscal;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CrossCuttingTests;

public sealed class PfxFiscalSigningCertificateSourceTests
{
    private static readonly DateTimeOffset SigningTimestamp =
        new(2026, 9, 9, 20, 30, 0, TimeSpan.FromHours(-3));

    [Fact]
    public async Task Source_routes_by_organization_and_returns_pinned_private_key_certificate()
    {
        const string organizationA = "org-a";
        const string organizationB = "org-b";
        const string passwordA = "test-password-a";
        const string passwordB = "test-password-b";

        using var certificateA = CreateCertificate("Org A");
        using var certificateB = CreateCertificate("Org B");
        var pathA = WritePfx(certificateA, passwordA);
        var pathB = WritePfx(certificateB, passwordB);

        try
        {
            using var source = new PfxFiscalSigningCertificateSource(Configuration(
                Entry(organizationA, pathA, passwordA, Sha256Thumbprint(certificateA)),
                Entry(organizationB, pathB, passwordB, Sha256Thumbprint(certificateB))));

            var resolvedA = await source.GetSigningCertificateAsync(Request(organizationA));
            var resolvedB = await source.GetSigningCertificateAsync(Request(organizationB));

            Assert.True(resolvedA.HasPrivateKey);
            Assert.True(resolvedB.HasPrivateKey);
            Assert.Equal(Sha256Thumbprint(certificateA), Sha256Thumbprint(resolvedA));
            Assert.Equal(Sha256Thumbprint(certificateB), Sha256Thumbprint(resolvedB));
            Assert.NotEqual(Sha256Thumbprint(resolvedA), Sha256Thumbprint(resolvedB));
        }
        finally
        {
            File.Delete(pathA);
            File.Delete(pathB);
        }
    }

    [Fact]
    public async Task Source_fails_closed_when_organization_has_no_certificate_configuration()
    {
        using var source = new PfxFiscalSigningCertificateSource(Configuration());

        var error = await Assert.ThrowsAsync<FiscalSignatureProviderException>(async () =>
            await source.GetSigningCertificateAsync(Request("missing-org")));

        Assert.Equal("fiscal.signature.certificate_not_configured", error.Code);
    }

    [Fact]
    public void Source_fails_closed_when_configured_sha256_thumbprint_does_not_match_file()
    {
        const string password = "test-password";
        using var certificate = CreateCertificate("Thumbprint Mismatch");
        var path = WritePfx(certificate, password);

        try
        {
            var error = Assert.Throws<FiscalSignatureProviderException>(() =>
                new PfxFiscalSigningCertificateSource(Configuration(
                    Entry("org-a", path, password, new string('0', 64)))));

            Assert.Equal("fiscal.signature.certificate_thumbprint_mismatch", error.Code);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Source_fails_closed_when_PFX_password_is_wrong()
    {
        using var certificate = CreateCertificate("Wrong Password");
        var path = WritePfx(certificate, "correct-password");

        try
        {
            var error = Assert.Throws<FiscalSignatureProviderException>(() =>
                new PfxFiscalSigningCertificateSource(Configuration(
                    Entry("org-a", path, "wrong-password", Sha256Thumbprint(certificate)))));

            Assert.Equal("fiscal.signature.certificate_load_failed", error.Code);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Source_rejects_relative_certificate_paths()
    {
        var error = Assert.Throws<FiscalSignatureProviderException>(() =>
            new PfxFiscalSigningCertificateSource(Configuration(
                Entry("org-a", "relative/signing.pfx", "secret", new string('A', 64)))));

        Assert.Equal("fiscal.signature.certificate_path_not_absolute", error.Code);
    }

    [Fact]
    public void Uruguay_clock_returns_an_offset_aware_current_instant()
    {
        var sut = new UruguayFiscalSigningTimeSource();
        var before = DateTimeOffset.UtcNow.AddSeconds(-2);

        var value = sut.GetSigningTimestamp();

        var after = DateTimeOffset.UtcNow.AddSeconds(2);
        Assert.InRange(value.ToUniversalTime(), before, after);
        Assert.Equal(TimeSpan.FromHours(-3), value.Offset);
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

    private static IReadOnlyDictionary<string, string?> Entry(
        string organizationId,
        string path,
        string password,
        string sha256Thumbprint) =>
        new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            [$"FiscalSigning:Certificates:{organizationId}:Path"] = path,
            [$"FiscalSigning:Certificates:{organizationId}:Password"] = password,
            [$"FiscalSigning:Certificates:{organizationId}:ExpectedSha256Thumbprint"] = sha256Thumbprint
        };

    private static FiscalSignatureRequest Request(string organizationId) => new(
        Guid.Parse("64000000-0000-0000-0000-000000000001"),
        CfeFamily.EFactura,
        "25.2",
        new string('a', 64),
        new string('b', 64),
        "<CFE />",
        new string('c', 64),
        SigningTimestamp,
        organizationId);

    private static X509Certificate2 CreateCertificate(string commonName)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={commonName}, O=EFactura Tests, C=UY",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation,
            critical: true));
        return request.CreateSelfSigned(SigningTimestamp.AddDays(-1), SigningTimestamp.AddDays(30));
    }

    private static string WritePfx(X509Certificate2 certificate, string password)
    {
        var path = Path.Combine(Path.GetTempPath(), $"efactura-signing-{Guid.NewGuid():N}.pfx");
        File.WriteAllBytes(path, certificate.Export(X509ContentType.Pkcs12, password));
        return Path.GetFullPath(path);
    }

    private static string Sha256Thumbprint(X509Certificate2 certificate) =>
        certificate.GetCertHashString(HashAlgorithmName.SHA256).ToUpperInvariant();
}
