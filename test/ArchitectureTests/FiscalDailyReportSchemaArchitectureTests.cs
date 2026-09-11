using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Xunit;

namespace ArchitectureTests;

public sealed class FiscalDailyReportSchemaArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();
    private const string SchemaDirectory = "src/Infrastructure/Fiscal/Schemas/DgiFeV1_44_2";
    private const string ManifestPath = $"{SchemaDirectory}/daily-report-schema-manifest.json";

    [Fact]
    public void Daily_report_schema_and_example_match_the_pinned_byte_manifest()
    {
        using var manifest = JsonDocument.Parse(Read(ManifestPath));
        var root = manifest.RootElement;

        Assert.Equal("dgi-daily-report-v13.2-byte-pin", root.GetProperty("artifactSetId").GetString());
        Assert.Equal("13.2", root.GetProperty("functionalFormatVersion").GetString());
        Assert.Equal("1.44.2", root.GetProperty("schemaArchiveVersion").GetString());
        Assert.Equal(
            "9a24a598e9470c4a1c5c34cb0cb253d08ffa3db3",
            root.GetProperty("byteRecoverySource").GetProperty("commit").GetString());

        var artifacts = root.GetProperty("artifacts");
        AssertArtifact(artifacts.GetProperty("reportSchema"));
        AssertArtifact(artifacts.GetProperty("officialExample"));
        foreach (var dependency in artifacts.GetProperty("schemaClosure").EnumerateArray())
            AssertArtifact(dependency);
    }

    [Fact]
    public void Daily_report_schema_has_exact_local_closure_and_wire_shape()
    {
        XNamespace xs = "http://www.w3.org/2001/XMLSchema";
        var schema = XDocument.Load(Absolute($"{SchemaDirectory}/ReporteDiarioCFE.xsd"), LoadOptions.PreserveWhitespace).Root
            ?? throw new InvalidDataException("ReporteDiarioCFE.xsd has no root element.");

        Assert.Equal("http://cfe.dgi.gub.uy", schema.Attribute("targetNamespace")?.Value);

        Assert.Equal(
            ["DGITypes.xsd"],
            schema.Elements(xs + "include").Select(element => element.Attribute("schemaLocation")?.Value).ToArray());
        Assert.Equal(
            ["xmldsig-core-schema.xsd"],
            schema.Elements(xs + "import").Select(element => element.Attribute("schemaLocation")?.Value).ToArray());

        Assert.DoesNotContain("schemaLocation=", Read($"{SchemaDirectory}/DGITypes.xsd"), StringComparison.Ordinal);
        Assert.DoesNotContain("schemaLocation=", Read($"{SchemaDirectory}/xmldsig-core-schema.xsd"), StringComparison.Ordinal);

        var report = schema.Elements(xs + "element")
            .Single(element => string.Equals(element.Attribute("name")?.Value, "Reporte", StringComparison.Ordinal));
        Assert.Equal("ns1:ReporteDefType", report.Attribute("type")?.Value);

        var reportType = schema.Elements(xs + "complexType")
            .Single(element => string.Equals(element.Attribute("name")?.Value, "ReporteDefType", StringComparison.Ordinal));
        var children = reportType.Element(xs + "sequence")?.Elements(xs + "element").ToArray()
            ?? throw new InvalidDataException("ReporteDefType sequence is missing.");

        Assert.Equal("Caratula", children[0].Attribute("name")?.Value);
        Assert.Equal("ds:Signature", children[^1].Attribute("ref")?.Value);
        Assert.Equal("1", children[^1].Attribute("minOccurs")?.Value);

        var caratulaVersion = children[0]
            .Descendants(xs + "attribute")
            .Single(attribute => string.Equals(attribute.Attribute("name")?.Value, "version", StringComparison.Ordinal));
        Assert.Equal("required", caratulaVersion.Attribute("use")?.Value);
        Assert.Equal("1.0", caratulaVersion.Attribute("fixed")?.Value);
    }

    [Fact]
    public void Functional_v13_2_limits_are_not_silently_replaced_by_more_permissive_xsd_limits()
    {
        XNamespace xs = "http://www.w3.org/2001/XMLSchema";
        var schema = XDocument.Load(Absolute($"{SchemaDirectory}/ReporteDiarioCFE.xsd")).Root
            ?? throw new InvalidDataException("ReporteDiarioCFE.xsd has no root element.");

        var montos = schema.Elements(xs + "complexType")
            .Single(element => string.Equals(element.Attribute("name")?.Value, "Montos_FyT", StringComparison.Ordinal));
        var amountItem = montos.Descendants(xs + "element")
            .Single(element => string.Equals(element.Attribute("name")?.Value, "Mnts_FyT_Item", StringComparison.Ordinal));
        Assert.Equal("2000", amountItem.Attribute("maxOccurs")?.Value);

        var ranges = schema.Elements(xs + "complexType")
            .Single(element => string.Equals(element.Attribute("name")?.Value, "RngDocsUtil", StringComparison.Ordinal));
        var rangeItem = ranges.Descendants(xs + "element")
            .Single(element => string.Equals(element.Attribute("name")?.Value, "RDU_Item", StringComparison.Ordinal));
        Assert.Equal("10000", rangeItem.Attribute("maxOccurs")?.Value);

        var functionalContract = Read("src/Domain/Fiscal/FiscalDailyReportV13_2WireContract.cs");
        Assert.Contains("MaximumAmountRowsPerSummary = 1_000", functionalContract, StringComparison.Ordinal);
        Assert.Contains("MaximumNumberRangeRepetitions = 50_000", functionalContract, StringComparison.Ordinal);

        using var manifest = JsonDocument.Parse(Read(ManifestPath));
        Assert.True(manifest.RootElement
            .GetProperty("safetyBoundary")
            .GetProperty("functionalFormatAndXsdAreIndependentGates")
            .GetBoolean());
    }

    [Fact]
    public void Historical_example_signature_does_not_become_current_crypto_policy()
    {
        const string example =
            "src/Infrastructure/Fiscal/Schemas/DgiFeV1_44_2/Examples/Rep_219000090011_20120511_01_Ej_Mod_27032014.xml";
        var xml = Read(example);
        var contract = Read("src/Domain/Fiscal/FiscalDailyReportV13_2WireContract.cs");

        Assert.Contains("ReporteDiarioCFE_v1.15.xsd", xml, StringComparison.Ordinal);
        Assert.Contains("http://www.w3.org/2000/09/xmldsig#rsa-sha1", xml, StringComparison.Ordinal);
        Assert.Contains("http://www.w3.org/2000/09/xmldsig#sha1", xml, StringComparison.Ordinal);
        Assert.DoesNotContain("rsa-sha1", contract, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("xmldsig#sha1", contract, StringComparison.OrdinalIgnoreCase);

        using var manifest = JsonDocument.Parse(Read(ManifestPath));
        Assert.True(manifest.RootElement
            .GetProperty("safetyBoundary")
            .GetProperty("exampleSignatureAlgorithmsAreHistoricalEvidenceOnly")
            .GetBoolean());
    }

    [Fact]
    public void Report_schema_identity_is_exposed_only_after_byte_pin_and_one_shot_workflow_is_absent()
    {
        var contract = Read("src/Domain/Fiscal/FiscalDailyReportV13_2WireContract.cs");
        var project = Read("src/Infrastructure/Infrastructure.csproj");

        Assert.Contains("SchemaArchiveVersion = \"1.44.2\"", contract, StringComparison.Ordinal);
        Assert.Contains("ReportSchemaFileName = \"ReporteDiarioCFE.xsd\"", contract, StringComparison.Ordinal);
        Assert.Contains("XmlNamespace = \"http://cfe.dgi.gub.uy\"", contract, StringComparison.Ordinal);
        Assert.Contains("XmlRootElementName = \"Reporte\"", contract, StringComparison.Ordinal);
        Assert.Contains("XmlDigitalSignatureIsRequiredFinalChild = true", contract, StringComparison.Ordinal);
        Assert.Contains("daily-report-schema-manifest.json", project, StringComparison.Ordinal);

        Assert.False(File.Exists(Absolute(".github/workflows/vendor-daily-report-bytes.yml")));
    }

    private static void AssertArtifact(JsonElement artifact)
    {
        var relativePath = artifact.GetProperty("path").GetString()
            ?? throw new InvalidDataException("Pinned artifact path is missing.");
        var bytes = File.ReadAllBytes(Absolute(relativePath));

        Assert.Equal(artifact.GetProperty("byteLength").GetInt32(), bytes.Length);
        Assert.Equal(
            artifact.GetProperty("sha256").GetString(),
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
        Assert.Equal(
            artifact.GetProperty("gitBlobSha1").GetString(),
            GitBlobSha1(bytes));
    }

    private static string GitBlobSha1(byte[] bytes)
    {
        var header = Encoding.UTF8.GetBytes($"blob {bytes.Length}\0");
        var payload = new byte[header.Length + bytes.Length];
        Buffer.BlockCopy(header, 0, payload, 0, header.Length);
        Buffer.BlockCopy(bytes, 0, payload, header.Length, bytes.Length);
        return Convert.ToHexString(SHA1.HashData(payload)).ToLowerInvariant();
    }

    private static string Read(string relativePath) => File.ReadAllText(Absolute(relativePath));

    private static string Absolute(string relativePath) =>
        Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

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
