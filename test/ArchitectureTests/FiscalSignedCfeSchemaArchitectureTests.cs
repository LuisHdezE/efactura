using System.Security.Cryptography;
using Xunit;

namespace ArchitectureTests;

public sealed class FiscalSignedCfeSchemaArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private const string SchemaDirectory = "src/Infrastructure/Fiscal/Schemas/DgiFeV1_44_2";

    [Fact]
    public void Pinned_DGI_schema_closure_has_exact_expected_bytes()
    {
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["CFEDGI.xsd"] = "e08d9cd95f9128d065fdbfdcccd2d3eaef99604561c8711fdb5587362681af21",
            ["CFEType.xsd"] = "81d718fa26bba9908d45cf05af47fa6d41bdedc0906f995628820f051f5f9ce5",
            ["DGITypes.xsd"] = "5bbc462de995acc26c572e5392c5604539b76b6645e4677d06aba84347205769",
            ["xmldsig-core-schema.xsd"] = "35cf8197da812c85e40d57891b35c94187569ed474a2dac813ce5090dafcd35c",
            ["xenc-schema.xsd"] = "a7401e4126ea13975a444f418a173c1ed16785d85b70135728c39f2d74a3aaf9",
            ["version.txt"] = "8386e71531b561bd75a2ec62c0b2d8d24fae42fd1cc318790a2f2585d5591d27"
        };

        foreach (var pair in expected)
        {
            var bytes = File.ReadAllBytes(Path.Combine(RepositoryRoot, SchemaDirectory, pair.Key));
            var actual = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            Assert.Equal(pair.Value, actual);
        }

        Assert.Equal(
            "version: 1.44.2",
            File.ReadAllText(Path.Combine(RepositoryRoot, SchemaDirectory, "version.txt")));
    }

    [Fact]
    public void Schema_provenance_does_not_claim_empty_DGI_archive_supplied_the_bytes()
    {
        var manifest = Read($"{SchemaDirectory}/schema-manifest.json");
        var provenance = Read($"{SchemaDirectory}/PROVENANCE.md");

        Assert.Contains("dgi-fe-xsd-v1.44.2", manifest, StringComparison.Ordinal);
        Assert.Contains("\"contentLength\": 0", manifest, StringComparison.Ordinal);
        Assert.Contains("olagopirez/factible", manifest, StringComparison.Ordinal);
        Assert.Contains("does **not** claim that the committed bytes were downloaded from DGI", provenance, StringComparison.Ordinal);
        Assert.Contains("mirror is **not** the regulatory authority", provenance, StringComparison.Ordinal);
    }

    [Fact]
    public void Runtime_validator_is_local_only_and_blocks_external_schema_resolution()
    {
        var validator = Read("src/Infrastructure/Fiscal/DgiFeV1_44_2SignedCfeSchemaValidator.cs");
        var project = Read("src/Infrastructure/Infrastructure.csproj");
        var boundary = Read("src/Application/Fiscal/FiscalSignedCfeSchemaValidation.cs");

        Assert.Contains("DtdProcessing = DtdProcessing.Prohibit", validator, StringComparison.Ordinal);
        Assert.Contains("EmbeddedSchemaResolver", validator, StringComparison.Ordinal);
        Assert.Contains("External schema resolution is forbidden", validator, StringComparison.Ordinal);
        Assert.Contains("SHA256.HashData", validator, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", validator, StringComparison.Ordinal);
        Assert.DoesNotContain("XmlUrlResolver", validator, StringComparison.Ordinal);
        Assert.DoesNotContain("WebRequest", validator, StringComparison.Ordinal);
        Assert.Contains("Fiscal\\Schemas\\DgiFeV1_44_2\\*.xsd", project, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Xml.Schema", boundary, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", boundary, StringComparison.Ordinal);
    }

    [Fact]
    public void XSD_gate_runs_after_signature_structure_check_and_before_durable_persistence()
    {
        var workflow = Read("src/Application/Fiscal/FiscalSignedArtifactWorkflow.cs");

        var signatureValidation = workflow.IndexOf("ValidateSignatureResult(signature, payload)", StringComparison.Ordinal);
        var schemaValidation = workflow.IndexOf("EnsureSchemaValid(signature.SignedXml)", StringComparison.Ordinal);
        var persist = workflow.IndexOf("_signedArtifacts.AddAsync(stored", StringComparison.Ordinal);
        var replaySchemaValidation = workflow.IndexOf("EnsureSchemaValid(existing.SignedXml)", StringComparison.Ordinal);
        var replayReturn = workflow.IndexOf("return Result(existing, true)", StringComparison.Ordinal);

        Assert.True(signatureValidation >= 0);
        Assert.True(schemaValidation > signatureValidation);
        Assert.True(persist > schemaValidation);
        Assert.True(replaySchemaValidation >= 0);
        Assert.True(replayReturn > replaySchemaValidation);
        Assert.Contains("SchemaSetFingerprint", workflow, StringComparison.Ordinal);
    }

    [Fact]
    public void Schema_evidence_migration_is_backward_compatible_but_replay_requires_evidence()
    {
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260909222000_V1FiscalSignedArtifactSchemaEvidence.cs");
        var records = Read("src/Infrastructure/Persistence/V1/Write/Models/FiscalizationRecords.cs");
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalSignedArtifactRepository.cs");

        Assert.Contains("SchemaSetId", migration, StringComparison.Ordinal);
        Assert.Contains("SchemaVersion", migration, StringComparison.Ordinal);
        Assert.Contains("SchemaSetFingerprint", migration, StringComparison.Ordinal);
        Assert.Equal(3, migration.Split("nullable: true", StringSplitOptions.None).Length - 1);
        Assert.Contains("string? SchemaSetId", records, StringComparison.Ordinal);
        Assert.Contains("SchemaSetFingerprint = artifact.SchemaSetFingerprint", repository, StringComparison.Ordinal);
    }

    [Fact]
    public void One_shot_import_workflow_is_not_part_of_the_candidate_runtime_tree()
    {
        Assert.False(File.Exists(Path.Combine(
            RepositoryRoot,
            ".github",
            "workflows",
            "import-dgi-xsd-v1-44-2.yml")));
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
