using Xunit;

namespace ArchitectureTests;

public sealed class FiscalSignedArtifactArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Signed_artifact_workflow_replays_before_crossing_private_key_boundary()
    {
        var workflow = Read("src/Application/Fiscal/FiscalSignedArtifactWorkflow.cs");

        var replayLookup = workflow.IndexOf("_signedArtifacts.GetByFiscalDocumentAsync", StringComparison.Ordinal);
        var signerCall = workflow.IndexOf("_signatureProvider.SignAsync", StringComparison.Ordinal);

        Assert.True(replayLookup >= 0);
        Assert.True(signerCall > replayLookup);
        Assert.Contains("return Result(existing, true)", workflow, StringComparison.Ordinal);
        Assert.Contains("SigningPayloadHash", workflow, StringComparison.Ordinal);
        Assert.Contains("SignedContentHash", workflow, StringComparison.Ordinal);
        Assert.Contains("SignatureProfileId", workflow, StringComparison.Ordinal);
        Assert.Contains("CertificateThumbprint", workflow, StringComparison.Ordinal);
        Assert.Contains("CertificateSerialNumber", workflow, StringComparison.Ordinal);
        Assert.Contains("FISCAL_SIGNED_ARTIFACT_CREATED", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("EFACRECEPCION", workflow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("X509Certificate", workflow, StringComparison.Ordinal);
        Assert.DoesNotContain("PrivateKey", workflow, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Signed_artifact_is_unique_per_document_and_signing_evidence()
    {
        var records = Read("src/Infrastructure/Persistence/V1/Write/Models/FiscalizationRecords.cs");
        var model = Read("src/Infrastructure/Persistence/V1/V1PersistenceModelCustomizer.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260909195000_V1FiscalSignedArtifact.cs");
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalSignedArtifactRepository.cs");

        Assert.Contains("V1FiscalSignedArtifactRecord", records, StringComparison.Ordinal);
        Assert.Contains("v1_fiscal_signed_artifacts", model, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fsa_org_document", model, StringComparison.Ordinal);
        Assert.Contains("UX_v1_fsa_evidence", model, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fsa_document", migration, StringComparison.Ordinal);
        Assert.Contains("FK_v1_fsa_evidence", migration, StringComparison.Ordinal);
        Assert.Contains("unique: true", migration, StringComparison.Ordinal);
        Assert.Contains("AsNoTracking", repository, StringComparison.Ordinal);
    }

    [Fact]
    public void Composition_registers_storage_payload_builder_and_explicit_secret_source_boundary()
    {
        var services = Read("src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs");

        Assert.Contains("IFiscalSigningPayloadBuilder, DeterministicFiscalSigningPayloadBuilder", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalSignedArtifactRepository, EfFiscalSignedArtifactRepository", services, StringComparison.Ordinal);
        Assert.Contains("PrepareFiscalSigningEvidenceUseCase", services, StringComparison.Ordinal);
        Assert.Contains("SignFiscalDocumentUseCase", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalSigningCertificateSource, PfxFiscalSigningCertificateSource", services, StringComparison.Ordinal);
        Assert.Contains("IFiscalSignatureProvider, XmlDsigFiscalSignatureProvider", services, StringComparison.Ordinal);
        Assert.DoesNotContain("KeyVault", services, StringComparison.OrdinalIgnoreCase);
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
