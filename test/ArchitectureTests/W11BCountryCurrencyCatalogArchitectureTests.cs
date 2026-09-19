using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace ArchitectureTests;

public sealed class W11BCountryCurrencyCatalogArchitectureTests
{
    [Fact]
    public void Reference_data_controller_exposes_the_two_W1_1B_authenticated_routes()
    {
        var controller = Read("src/WebApi/Controllers/V1/ReferenceDataController.cs");

        Assert.Contains("[Authorize]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"countries\", Name = \"listCountries\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"currencies\", Name = \"listCurrencies\")]", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("RequirePermission", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplicationCore", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure.", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Country_catalog_is_complete_provider_neutral_and_uses_alpha2_codes()
    {
        var catalog = Read("src/Infrastructure/ReferenceData/Release1CountryCatalog.cs");
        var rows = Regex.Matches(
                catalog,
                "new\\(\"(?<code>[A-Z]{2})\",\\s*\"(?<name>[^\"]+)\"\\)")
            .Select(match => (
                Code: match.Groups["code"].Value,
                Name: match.Groups["name"].Value))
            .ToArray();

        Assert.Equal(249, rows.Length);
        Assert.Equal(249, rows.Select(row => row.Code).Distinct(StringComparer.Ordinal).Count());
        Assert.All(rows, row => Assert.False(string.IsNullOrWhiteSpace(row.Name)));
        Assert.Contains(rows, row => row.Code == "UY" && row.Name == "Uruguay");
        Assert.Contains(rows, row => row.Code == "AR");
        Assert.Contains(rows, row => row.Code == "BR");
        Assert.Contains(rows, row => row.Code == "CU");
        Assert.Contains(rows, row => row.Code == "US");

        Assert.DoesNotContain("Npgsql", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("Dapper", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplicationCore", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", catalog, StringComparison.Ordinal);
    }

    [Fact]
    public void Currency_catalog_is_an_explicit_fail_closed_release1_supported_subset()
    {
        var catalog = Read("src/Infrastructure/ReferenceData/Release1CurrencyCatalog.cs");
        var codes = Regex.Matches(catalog, "new\\(\"(?<code>[A-Z]{3})\",")
            .Select(match => match.Groups["code"].Value)
            .ToArray();

        Assert.Equal(new[] { "USD", "UYI", "UYU" }, codes);
        Assert.DoesNotContain("EUR", codes);
        Assert.DoesNotContain("BGN", codes);

        Assert.DoesNotContain("Npgsql", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("Dapper", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplicationCore", catalog, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", catalog, StringComparison.Ordinal);
    }

    [Fact]
    public void Application_and_provider_publish_versioned_W1_1B_catalogs_without_legacy_dependencies()
    {
        var application = Read("src/Application/ReferenceData/ReferenceDataApplication.cs");
        var provider = Read("src/Infrastructure/ReferenceData/Release1ReferenceDataCatalog.cs");
        var contracts = Read("src/WebApi/Controllers/V1/Contracts/ReferenceDataContracts.cs");

        Assert.Contains("CountryReference", application, StringComparison.Ordinal);
        Assert.Contains("CurrencyReference", application, StringComparison.Ordinal);
        Assert.Contains("ListCountriesAsync", application, StringComparison.Ordinal);
        Assert.Contains("ListCurrenciesAsync", application, StringComparison.Ordinal);
        Assert.Contains("ListCountriesUseCase", application, StringComparison.Ordinal);
        Assert.Contains("ListCurrenciesUseCase", application, StringComparison.Ordinal);
        Assert.Contains("actor.IsAuthenticated", application, StringComparison.Ordinal);

        Assert.Contains("\"ISO 3166-1 current country codes\"", provider, StringComparison.Ordinal);
        Assert.Contains("\"snapshot-2026-09-19\"", provider, StringComparison.Ordinal);
        Assert.Contains("\"ISO 4217 / eFactura Release-1 supported currency policy\"", provider, StringComparison.Ordinal);
        Assert.Contains("\"release-1/amendment-180\"", provider, StringComparison.Ordinal);
        Assert.Contains("services.AddScoped<ListCountriesUseCase>();", provider, StringComparison.Ordinal);
        Assert.Contains("services.AddScoped<ListCurrenciesUseCase>();", provider, StringComparison.Ordinal);

        Assert.Contains("CountryDto", contracts, StringComparison.Ordinal);
        Assert.Contains("CurrencyDto", contracts, StringComparison.Ordinal);

        Assert.DoesNotContain("Npgsql", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Dapper", application, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplicationCore", application, StringComparison.Ordinal);
        Assert.DoesNotContain("Npgsql", provider, StringComparison.Ordinal);
        Assert.DoesNotContain("Dapper", provider, StringComparison.Ordinal);
        Assert.DoesNotContain("ApplicationCore", provider, StringComparison.Ordinal);
    }

    private static string Read(string path) => File.ReadAllText(Full(path), Encoding.UTF8);

    private static string Full(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return Path.GetFullPath(full);
    }
}
