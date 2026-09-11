using System.Xml.Linq;
using Xunit;

namespace ArchitectureTests;

public sealed class FiscalDailyReportUnsignedXmlArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Unsigned_report_builder_consumes_semantic_projection_without_infrastructure_or_signing_dependencies()
    {
        var source = Read("src/Application/Fiscal/UnsignedDailyReportBuilder.cs");

        Assert.Contains("FiscalDailyReportWireProjection", source, StringComparison.Ordinal);
        Assert.Contains("Rsmn_Tck", source, StringComparison.Ordinal);
        Assert.Contains("Rsmn_Tck_Nota_Credito", source, StringComparison.Ordinal);
        Assert.Contains("Rsmn_Tck_Nota_Debito", source, StringComparison.Ordinal);
        Assert.Contains("Rsmn_Fac", source, StringComparison.Ordinal);
        Assert.Contains("Rsmn_Fac_Nota_Credito", source, StringComparison.Ordinal);
        Assert.Contains("Rsmn_Fac_Nota_Debito", source, StringComparison.Ordinal);
        Assert.Contains("yyyy-MM-dd'T'HH:mm:sszzz", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SignedXml", source, StringComparison.Ordinal);
        Assert.DoesNotContain("X509Certificate", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Unsigned_structural_validator_verifies_pinned_closure_and_relaxes_only_signature_cardinality_in_memory()
    {
        var validator = Read("src/Infrastructure/Fiscal/DgiFeV1_44_2UnsignedDailyReportSchemaValidator.cs");
        var manifest = Read("src/Infrastructure/Fiscal/Schemas/DgiFeV1_44_2/daily-report-schema-manifest.json");

        Assert.Contains("ReporteDiarioCFE.xsd", validator, StringComparison.Ordinal);
        Assert.Contains("daily-report-schema-manifest.json", validator, StringComparison.Ordinal);
        Assert.Contains("<xs:element ref=\"ds:Signature\" minOccurs=\"1\"/>", validator, StringComparison.Ordinal);
        Assert.Contains("<xs:element ref=\"ds:Signature\" minOccurs=\"0\"/>", validator, StringComparison.Ordinal);
        Assert.Contains("Count(text, SignatureDeclaration) != 1", validator, StringComparison.Ordinal);
        Assert.Contains("DtdProcessing = DtdProcessing.Prohibit", validator, StringComparison.Ordinal);
        Assert.Contains("External schema resolution is forbidden", validator, StringComparison.Ordinal);
        Assert.Contains("officialByteEqualityDirectlyObserved\": false", manifest, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", validator, StringComparison.Ordinal);
    }

    [Fact]
    public void Pinned_schema_still_requires_signature_as_final_child_and_is_not_modified_on_disk()
    {
        XNamespace xs = "http://www.w3.org/2001/XMLSchema";
        var schema = XDocument.Load(Absolute("src/Infrastructure/Fiscal/Schemas/DgiFeV1_44_2/ReporteDiarioCFE.xsd")).Root
            ?? throw new InvalidDataException("ReporteDiarioCFE.xsd has no root element.");
        var reportType = schema.Elements(xs + "complexType")
            .Single(element => string.Equals(element.Attribute("name")?.Value, "ReporteDefType", StringComparison.Ordinal));
        var children = reportType.Element(xs + "sequence")!.Elements(xs + "element").ToArray();

        Assert.Equal("ds:Signature", children[^1].Attribute("ref")?.Value);
        Assert.Equal("1", children[^1].Attribute("minOccurs")?.Value);
    }

    [Fact]
    public void Implementation_record_keeps_signed_validation_and_DGI_testing_as_later_gates()
    {
        var document = Read("documentation/blueprint-api-implementation/46_FISCAL_DAILY_REPORT_UNSIGNED_XML.md");

        Assert.Contains("pre-signature", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Signature", document, StringComparison.Ordinal);
        Assert.Contains("XSD", document, StringComparison.Ordinal);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", document, StringComparison.Ordinal);
        Assert.DoesNotContain("READY FOR DGI TESTING", document, StringComparison.OrdinalIgnoreCase);
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
