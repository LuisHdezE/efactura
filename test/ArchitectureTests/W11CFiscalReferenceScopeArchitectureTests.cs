using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace ArchitectureTests;

public sealed class W11CFiscalReferenceScopeArchitectureTests
{
    [Fact]
    public void Reference_data_controller_exposes_the_two_W1_1C_routes_with_exact_fiscal_read_permission()
    {
        var controller = Read("src/WebApi/Controllers/V1/ReferenceDataController.cs");

        var documentTypes = ActionSlice(
            controller,
            "[HttpGet(\"fiscal-document-types\", Name = \"listFiscalDocumentTypes\")]");
        var indicators = ActionSlice(
            controller,
            "[HttpGet(\"invoice-indicators\", Name = \"listInvoiceIndicators\")]");

        Assert.Contains("[RequirePermission(Permissions.FiscalRead)]", documentTypes, StringComparison.Ordinal);
        Assert.Contains("[RequirePermission(Permissions.FiscalRead)]", indicators, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status403Forbidden", documentTypes, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status403Forbidden", indicators, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplicationCore", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Fiscal_document_catalog_is_the_fail_closed_release1_domestic_emit_capability_subset()
    {
        var catalog = Read("src/Infrastructure/ReferenceData/Release1FiscalReferenceCatalog.cs");
        var documentBlock = catalog[..catalog.IndexOf("InvoiceIndicators", StringComparison.Ordinal)];
        var codes = Regex.Matches(documentBlock, @"new\((?<code>\d{3}),")
            .Select(match => int.Parse(match.Groups["code"].Value))
            .ToArray();
        var contingencyCodes = Regex.Matches(documentBlock, @", (?<code>2\d{2}), true\)")
            .Select(match => int.Parse(match.Groups["code"].Value))
            .ToArray();

        Assert.Equal(new[] { 101, 102, 103, 111, 112, 113 }, codes);
        Assert.Equal(new[] { 201, 202, 203, 211, 212, 213 }, contingencyCodes);
        Assert.DoesNotContain(121, codes);
        Assert.DoesNotContain(181, codes);
        Assert.Equal(6, Regex.Matches(documentBlock, @", true\)").Count);

        Assert.Contains("\"ETICKET\"", documentBlock, StringComparison.Ordinal);
        Assert.Contains("\"EFACTURA\"", documentBlock, StringComparison.Ordinal);
        Assert.Contains("\"CREDIT_NOTE\"", documentBlock, StringComparison.Ordinal);
        Assert.Contains("\"DEBIT_NOTE\"", documentBlock, StringComparison.Ordinal);
    }

    [Fact]
    public void Invoice_indicator_catalog_is_the_exact_subset_the_release1_builder_can_map()
    {
        var catalog = Read("src/Infrastructure/ReferenceData/Release1FiscalReferenceCatalog.cs");
        var indicatorBlock = catalog[catalog.IndexOf("InvoiceIndicators", StringComparison.Ordinal)..];
        var codes = Regex.Matches(indicatorBlock, @"new\((?<code>\d+),")
            .Select(match => int.Parse(match.Groups["code"].Value))
            .ToArray();

        Assert.Equal(new[] { 1, 2, 3, 10 }, codes);
        Assert.Contains("new(1, \"Exento de IVA\", \"EXEMPT\")", indicatorBlock, StringComparison.Ordinal);
        Assert.Contains("new(2, \"Gravado a Tasa Mínima\", \"MINIMUM\")", indicatorBlock, StringComparison.Ordinal);
        Assert.Contains("new(3, \"Gravado a Tasa Básica\", \"BASIC\")", indicatorBlock, StringComparison.Ordinal);
        Assert.Contains("new(10, \"Exportación y asimiladas\", \"EXPORT\")", indicatorBlock, StringComparison.Ordinal);
        Assert.DoesNotContain(4, codes);
        Assert.DoesNotContain(5, codes);
        Assert.DoesNotContain(6, codes);
        Assert.DoesNotContain(7, codes);
        Assert.DoesNotContain(11, codes);
        Assert.DoesNotContain(12, codes);
    }

    [Fact]
    public void W1_1C_catalog_scope_is_tied_to_current_executable_builder_capabilities()
    {
        var builder = Read("src/Application/Fiscal/UnsignedCfeBuilder.cs");
        var catalog = Read("src/Infrastructure/ReferenceData/Release1FiscalReferenceCatalog.cs");

        Assert.Contains("CfeFamily.EFacturaExportacion => throw Rule(", builder, StringComparison.Ordinal);
        Assert.Contains("fiscal.cfe_builder.export_not_supported", builder, StringComparison.Ordinal);
        Assert.Contains("VatRateKind.Exempt => 1", builder, StringComparison.Ordinal);
        Assert.Contains("VatRateKind.Minimum => 2", builder, StringComparison.Ordinal);
        Assert.Contains("VatRateKind.Basic => 3", builder, StringComparison.Ordinal);
        Assert.Contains("VatRateKind.Export => 10", builder, StringComparison.Ordinal);
        Assert.Contains("indicator_not_supported", builder, StringComparison.Ordinal);

        Assert.DoesNotContain("new(121,", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("new(4,", catalog, StringComparison.Ordinal);
    }

    [Fact]
    public void Application_provider_and_contracts_publish_versioned_W1_1C_metadata_without_legacy_or_database_dependencies()
    {
        var application = Read("src/Application/ReferenceData/ReferenceDataApplication.cs");
        var provider = Read("src/Infrastructure/ReferenceData/Release1ReferenceDataCatalog.cs");
        var catalog = Read("src/Infrastructure/ReferenceData/Release1FiscalReferenceCatalog.cs");
        var contracts = Read("src/WebApi/Controllers/V1/Contracts/ReferenceDataContracts.cs");

        Assert.Contains("FiscalDocumentTypeReference", application, StringComparison.Ordinal);
        Assert.Contains("InvoiceIndicatorReference", application, StringComparison.Ordinal);
        Assert.Contains("ListFiscalDocumentTypesAsync", application, StringComparison.Ordinal);
        Assert.Contains("ListInvoiceIndicatorsAsync", application, StringComparison.Ordinal);
        Assert.Contains("ListFiscalDocumentTypesUseCase", application, StringComparison.Ordinal);
        Assert.Contains("ListInvoiceIndicatorsUseCase", application, StringComparison.Ordinal);
        Assert.Contains("actor.IsAuthenticated", application, StringComparison.Ordinal);

        Assert.Contains("\"DGI Formato_CFE / eFactura Release-1 enabled document policy\"", provider, StringComparison.Ordinal);
        Assert.Contains("\"25-2/release-1-domestic\"", provider, StringComparison.Ordinal);
        Assert.Contains("\"DGI Formato_CFE / eFactura Release-1 supported indicator policy\"", provider, StringComparison.Ordinal);
        Assert.Contains("\"25-2/release-1\"", provider, StringComparison.Ordinal);
        Assert.Contains("services.AddScoped<ListFiscalDocumentTypesUseCase>();", provider, StringComparison.Ordinal);
        Assert.Contains("services.AddScoped<ListInvoiceIndicatorsUseCase>();", provider, StringComparison.Ordinal);

        Assert.Contains("FiscalDocumentTypeDto", contracts, StringComparison.Ordinal);
        Assert.Contains("InvoiceIndicatorDto", contracts, StringComparison.Ordinal);
        Assert.Contains("RequiresApplicabilityValidation", contracts, StringComparison.Ordinal);

        foreach (var source in new[] { application, provider, catalog })
        {
            Assert.DoesNotContain("Npgsql", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Dapper", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ApplicationCore", source, StringComparison.Ordinal);
            Assert.DoesNotContain("DbContext", source, StringComparison.Ordinal);
        }
    }

    private static string ActionSlice(string source, string routeMarker)
    {
        var start = source.IndexOf(routeMarker, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Missing route marker {routeMarker}.");
        var next = source.IndexOf("[HttpGet(", start + routeMarker.Length, StringComparison.Ordinal);
        return next < 0 ? source[start..] : source[start..next];
    }

    private static string Read(string path) => File.ReadAllText(Full(path), Encoding.UTF8);

    private static string Full(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return Path.GetFullPath(full);
    }
}
