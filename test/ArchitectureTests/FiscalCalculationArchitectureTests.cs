using Xunit;

namespace ArchitectureTests;

public sealed class FiscalCalculationArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Cfe_arithmetic_is_domain_owned_framework_free_and_independent_from_Sales()
    {
        var content = Read("src/Domain/Fiscal/CfeArithmetic.cs");

        Assert.Contains("public sealed class CfeArithmeticCalculator", content, StringComparison.Ordinal);
        Assert.Contains("EFactura.Domain.Taxation", content, StringComparison.Ordinal);
        Assert.DoesNotContain("EFactura.Domain.Sales", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.AspNetCore", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure", content, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", content, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpClient", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Cfe_25_2_arithmetic_catalog_keeps_official_DGI_provenance_in_source_control()
    {
        var content = Read("src/Application/Fiscal/UruguayCfe25_2ArithmeticCatalog.cs");

        Assert.Contains("FormatVersion = \"25.2\"", content, StringComparison.Ordinal);
        Assert.Contains("new(2026, 6, 30)", content, StringComparison.Ordinal);
        Assert.Contains("https://www.efactura.dgi.gub.uy/files/formato_cfe_v25-2-pdf?es=", content, StringComparison.Ordinal);
        Assert.Contains("B-C24", content, StringComparison.Ordinal);
        Assert.Contains("A-C121/A-C122", content, StringComparison.Ordinal);
        Assert.Contains("redondeo matematico con 2 decimales", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sales_preview_remains_non_authoritative_until_the_confirm_sale_slice_consumes_Cfe_arithmetic()
    {
        var preview = Read("src/Application/Sales/SaleFiscalPreviewUseCases.cs");
        var controller = Read("src/WebApi/Controllers/V1/SalesController.cs");

        Assert.Contains("Preview-only arithmetic", preview, StringComparison.Ordinal);
        Assert.DoesNotContain("CfeArithmeticCalculator", preview, StringComparison.Ordinal);
        Assert.DoesNotContain("/confirm", controller, StringComparison.OrdinalIgnoreCase);
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

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
