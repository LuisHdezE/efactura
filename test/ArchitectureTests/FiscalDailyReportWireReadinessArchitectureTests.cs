using Xunit;

namespace ArchitectureTests;

public sealed class FiscalDailyReportWireReadinessArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Wire_readiness_domain_boundary_does_not_generate_xml_or_depend_on_infrastructure()
    {
        var source = Read("src/Domain/Fiscal/FiscalDailyReportWireReadiness.cs");

        Assert.DoesNotContain("System.Xml", source, StringComparison.Ordinal);
        Assert.DoesNotContain("XElement", source, StringComparison.Ordinal);
        Assert.DoesNotContain("XmlWriter", source, StringComparison.Ordinal);
        Assert.DoesNotContain("XmlDocument", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", source, StringComparison.Ordinal);
        Assert.Contains("fiscal.daily_report.wire.quantization_required", source, StringComparison.Ordinal);
        Assert.Contains("fiscal.daily_report.wire.high_value_evidence_required", source, StringComparison.Ordinal);
        Assert.Contains("release1-minimum-basic-export-only", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Wire_readiness_documentation_preserves_serializer_and_DGI_readiness_gates()
    {
        var document = Read("documentation/blueprint-api-implementation/43_FISCAL_DAILY_REPORT_WIRE_READINESS.md");

        Assert.Contains("does not generate XML", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("B-C27", document, StringComparison.Ordinal);
        Assert.Contains("quantization", document, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", document, StringComparison.Ordinal);
        Assert.DoesNotContain("READY FOR DGI TESTING", document, StringComparison.OrdinalIgnoreCase);
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
