using Xunit;

namespace ArchitectureTests;

public sealed class FiscalCertificateCompositionArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void PFX_certificate_source_stays_in_Infrastructure_and_keeps_private_key_ephemeral()
    {
        var source = Read("src/Infrastructure/Fiscal/PfxFiscalSigningCertificateSource.cs");

        Assert.Contains("namespace Infrastructure.Fiscal", source, StringComparison.Ordinal);
        Assert.Contains("IFiscalSigningCertificateSource", source, StringComparison.Ordinal);
        Assert.Contains("X509CertificateLoader.LoadPkcs12FromFile", source, StringComparison.Ordinal);
        Assert.Contains("X509KeyStorageFlags.EphemeralKeySet", source, StringComparison.Ordinal);
        Assert.Contains("GetCertHashString(HashAlgorithmName.SHA256)", source, StringComparison.Ordinal);
        Assert.Contains("FiscalSigning:Certificates", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PersistKeySet", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Exportable", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("EFACRECEPCION", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Signing_request_carries_organization_without_leaking_certificate_details_into_Application()
    {
        var application = Read("src/Application/Fiscal/FiscalSigningEvidenceWorkflow.cs");
        var signedArtifact = Read("src/Application/Fiscal/FiscalSignedArtifactWorkflow.cs");

        Assert.Contains("string OrganizationId", application, StringComparison.Ordinal);
        Assert.Contains("organizationId", signedArtifact, StringComparison.Ordinal);
        Assert.DoesNotContain("Pfx", application, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("X509Certificate", application, StringComparison.Ordinal);
        Assert.DoesNotContain("ExpectedSha256Thumbprint", application, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_signing_dependencies_are_explicitly_composed()
    {
        var services = Read("src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs");

        Assert.Contains("IFiscalSigningTimeSource, UruguayFiscalSigningTimeSource", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalSigningCertificateSource, PfxFiscalSigningCertificateSource", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalSignatureProvider, XmlDsigFiscalSignatureProvider", services, StringComparison.Ordinal);
    }

    [Fact]
    public void Repository_never_tracks_PKCS12_private_key_files()
    {
        var gitignore = Read(".gitignore");

        Assert.Contains("*.pfx", gitignore, StringComparison.OrdinalIgnoreCase);
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "api-accounting.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root containing api-accounting.sln was not found.");
    }
}
