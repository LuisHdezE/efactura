using Xunit;

namespace ArchitectureTests;

public sealed class FiscalSigningArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Signing_evidence_domain_remains_framework_and_secret_agnostic()
    {
        var domain = Read("src/Domain/Fiscal/FiscalSigningEvidence.cs");

        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", domain, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", domain, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Security.Cryptography", domain, StringComparison.Ordinal);
        Assert.DoesNotContain("X509Certificate", domain, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CertificateThumbprint", domain, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CertificatePath", domain, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PrivateKey", domain, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("KeyVault", domain, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HttpClient", domain, StringComparison.Ordinal);
    }

    [Fact]
    public void Signing_evidence_workflow_owns_clock_through_port_and_does_not_sign_yet()
    {
        var workflow = Read("src/Application/Fiscal/FiscalSigningEvidenceWorkflow.cs");

        Assert.Contains("interface IFiscalSigningTimeSource", workflow, StringComparison.Ordinal);
        Assert.Contains("interface IFiscalSignatureProvider", workflow, StringComparison.Ordinal);
        Assert.Contains("_signingTime.GetSigningTimestamp()", workflow, StringComparison.Ordinal);
        Assert.Contains("SigningPayloadXml", workflow, StringComparison.Ordinal);
        Assert.Contains("SigningPayloadHash", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTimeOffset.UtcNow", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTime.Now", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("X509Certificate", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("PrivateKey", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("_signatureProvider.SignAsync", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void Signing_payload_builder_inserts_durable_TmstFirma_without_secret_or_transport_dependencies()
    {
        var builder = Read("src/Application/Fiscal/FiscalSigningPayloadBuilder.cs");

        Assert.Contains("TmstFirma", builder, StringComparison.Ordinal);
        Assert.Contains("yyyy-MM-dd'T'HH:mm:sszzz", builder, StringComparison.Ordinal);
        Assert.Contains("family.AddFirst", builder, StringComparison.Ordinal);
        Assert.Contains("UnsignedContentHash", builder, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTimeOffset.UtcNow", builder, StringComparison.Ordinal);
        Assert.DoesNotContain("DateTime.Now", builder, StringComparison.Ordinal);
        Assert.DoesNotContain("X509Certificate", builder, StringComparison.Ordinal);
        Assert.DoesNotContain("PrivateKey", builder, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyVault", builder, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("HttpClient", builder, StringComparison.Ordinal);
    }

    [Fact]
    public void Durable_signing_evidence_is_separate_and_unique_per_fiscal_document()
    {
        var records = Read("src/Infrastructure/Persistence/V1/Write/Models/FiscalizationRecords.cs");
        var model = Read("src/Infrastructure/Persistence/V1/V1PersistenceModelCustomizer.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260909130000_V1FiscalSigningEvidence.cs");

        Assert.Contains("V1FiscalSigningEvidenceRecord", records, StringComparison.Ordinal);
        Assert.Contains("FiscalContentFingerprint", records, StringComparison.Ordinal);
        Assert.Contains("UnsignedContentHash", records, StringComparison.Ordinal);
        Assert.Contains("SigningTimestamp", records, StringComparison.Ordinal);
        Assert.Contains("v1_fiscal_signing_evidence", model, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fse_org_document", model, StringComparison.Ordinal);
        Assert.Contains("unique: true", migration, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fse_document", migration, StringComparison.Ordinal);
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
