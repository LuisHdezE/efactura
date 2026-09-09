using Xunit;

namespace ArchitectureTests;

public sealed class FiscalXmlDsigArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Concrete_XMLDSig_signer_stays_in_Infrastructure_and_keeps_transport_out()
    {
        var provider = Read("src/Infrastructure/Fiscal/XmlDsigFiscalSignatureProvider.cs");

        Assert.Contains("namespace Infrastructure.Fiscal", provider, StringComparison.Ordinal);
        Assert.Contains("IFiscalSignatureProvider", provider, StringComparison.Ordinal);
        Assert.Contains("IFiscalSigningCertificateSource", provider, StringComparison.Ordinal);
        Assert.Contains("SignedXml", provider, StringComparison.Ordinal);
        Assert.Contains("GetRSAPrivateKey", provider, StringComparison.Ordinal);
        Assert.Contains("CheckSignature", provider, StringComparison.Ordinal);
        Assert.Contains("TmstFirma", provider, StringComparison.Ordinal);
        Assert.Contains("Adenda", provider, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", provider, StringComparison.Ordinal);
        Assert.DoesNotContain("EFACRECEPCION", provider, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("V1Fiscal", provider, StringComparison.Ordinal);
    }

    [Fact]
    public void XMLDSig_profile_is_explicit_SHA256_and_does_not_embed_historical_SHA1_uris()
    {
        var provider = Read("src/Infrastructure/Fiscal/XmlDsigFiscalSignatureProvider.cs");
        var project = Read("src/Infrastructure/Infrastructure.csproj");

        Assert.Contains("dgi-cfe-sha256-evidence-backed-v1", provider, StringComparison.Ordinal);
        Assert.Contains("xmldsig-more#rsa-sha256", provider, StringComparison.Ordinal);
        Assert.Contains("xmlenc#sha256", provider, StringComparison.Ordinal);
        Assert.Contains("REC-xml-c14n-20010315", provider, StringComparison.Ordinal);
        Assert.Contains("enveloped-signature", provider, StringComparison.Ordinal);
        Assert.DoesNotContain("http://www.w3.org/2000/09/xmldsig#rsa-sha1", provider, StringComparison.Ordinal);
        Assert.DoesNotContain("http://www.w3.org/2000/09/xmldsig#sha1", provider, StringComparison.Ordinal);
        Assert.Contains("System.Security.Cryptography.Xml\" Version=\"10.0.12\"", project, StringComparison.Ordinal);
    }

    [Fact]
    public void Application_and_Domain_remain_certificate_private_key_agnostic()
    {
        var workflow = Read("src/Application/Fiscal/FiscalSigningEvidenceWorkflow.cs");
        var domain = Read("src/Domain/Fiscal/FiscalSigningEvidence.cs");

        Assert.DoesNotContain("X509Certificate", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("GetRSAPrivateKey", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("CertificatePath", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("KeyVault", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("_signatureProvider.SignAsync", workflow, StringComparison.Ordinal);

        Assert.DoesNotContain("X509Certificate", domain, StringComparison.Ordinal);
        Assert.DoesNotContain("PrivateKey", domain, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CertificateThumbprint", domain, StringComparison.OrdinalIgnoreCase);
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
