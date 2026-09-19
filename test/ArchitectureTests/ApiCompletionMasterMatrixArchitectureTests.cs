using System.Text.RegularExpressions;
using Xunit;

namespace ArchitectureTests;

public sealed class ApiCompletionMasterMatrixArchitectureTests
{
    [Fact]
    public void Canonical_inventory_has_194_unique_ids_and_operation_ids_but_193_unique_http_slots()
    {
        var rows = ParseInventory();

        Assert.Equal(194, rows.Count);
        Assert.Equal(194, rows.Select(row => row.ApiId).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(194, rows.Select(row => row.OperationId).Distinct(StringComparer.Ordinal).Count());

        var slots = rows
            .GroupBy(row => $"{row.Method} {row.Route}", StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(193, slots.Length);

        var collisions = slots.Where(group => group.Count() > 1).ToArray();
        var collision = Assert.Single(collisions);

        Assert.Equal("GET /api/v1/operations/integrations", collision.Key);
        Assert.Equal(
            new[] { "API-180", "API-MON-001" },
            collision.Select(row => row.ApiId).OrderBy(value => value, StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void Governed_v1_implementation_baseline_has_41_http_actions_across_10_controllers()
    {
        var expected = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["CaeAuthorizationsController.cs"] = 7,
            ["CompanyController.cs"] = 2,
            ["FiscalCfeEnvelopesController.cs"] = 1,
            ["InventoryController.cs"] = 4,
            ["ItemCategoriesController.cs"] = 3,
            ["ItemsController.cs"] = 5,
            ["LocationsController.cs"] = 4,
            ["PartiesController.cs"] = 7,
            ["SalesController.cs"] = 7,
            ["TaxProfilesController.cs"] = 1
        };

        var controllersDirectory = Path.Combine(
            FindRepositoryRoot(),
            "src",
            "WebApi",
            "Controllers",
            "V1");

        var actualFiles = Directory
            .GetFiles(controllersDirectory, "*Controller.cs", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(file => file is not null)
            .Cast<string>()
            .OrderBy(file => file, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected.Keys.OrderBy(file => file, StringComparer.Ordinal).ToArray(), actualFiles);

        var total = 0;
        foreach (var (file, expectedCount) in expected)
        {
            var source = File.ReadAllText(Path.Combine(controllersDirectory, file));
            var actualCount = Regex.Matches(
                source,
                @"\[Http(?:Get|Post|Patch|Put|Delete)(?:\([^\]]*\))?\]",
                RegexOptions.CultureInvariant).Count;

            Assert.True(
                actualCount == expectedCount,
                $"{file} exposes {actualCount} governed HTTP actions; matrix baseline expects {expectedCount}.");

            total += actualCount;
        }

        Assert.Equal(41, total);
    }

    [Fact]
    public void Master_matrix_records_reconciliation_coverage_and_wave_partition()
    {
        var matrix = Read("documentation/api-completion/API_COMPLETION_MASTER_MATRIX.md");

        Assert.Contains("raw contract rows: `194`", matrix, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("unique API IDs: `194`", matrix, StringComparison.Ordinal);
        Assert.Contains("unique `operationId` values: `194`", matrix, StringComparison.Ordinal);
        Assert.Contains("unique HTTP method + route slots: `193`", matrix, StringComparison.Ordinal);
        Assert.Contains("`API-MON-001`", matrix, StringComparison.Ordinal);
        Assert.Contains("`API-180`", matrix, StringComparison.Ordinal);
        Assert.Contains("GET `/api/v1/operations/integrations`", matrix, StringComparison.Ordinal);
        Assert.Contains("`CONTRACT_COLLISION`", matrix, StringComparison.Ordinal);
        Assert.Contains("`41` contract-mapped HTTP actions", matrix, StringComparison.Ordinal);
        Assert.Contains("Raw-record completion: `41 / 194 = 21.13%`", matrix, StringComparison.Ordinal);
        Assert.Contains("Unique-slot completion: `41 / 193 = 21.24%`", matrix, StringComparison.Ordinal);
        Assert.Contains("| 1 | Identity + Organization + Reference Data | 33 | 7 | 26 |", matrix, StringComparison.Ordinal);
        Assert.Contains("| 2 | Parties + Catalog + Sales Completion | 26 | 22 | 4 |", matrix, StringComparison.Ordinal);
        Assert.Contains("| 3 | Payments + Cash + AR/AP | 21 | 0 | 21 |", matrix, StringComparison.Ordinal);
        Assert.Contains("| 4 | Inventory + Transfers + Procurement + Receiving | 23 | 4 | 19 |", matrix, StringComparison.Ordinal);
        Assert.Contains("| 5 | Fiscal Completion + CAE + CFE Lifecycle | 39 | 8 | 31 |", matrix, StringComparison.Ordinal);
        Assert.Contains("| 6 | Reporting + Audit + Sync | 24 | 0 | 24 |", matrix, StringComparison.Ordinal);
        Assert.Contains("| 7 | Technical Operations Console | 28 | 0 | 28 |", matrix, StringComparison.Ordinal);
        Assert.Contains("| **Total** |  | **194** | **41** | **153** |", matrix, StringComparison.Ordinal);
    }

    [Fact]
    public void Wave_1_matrix_contains_33_records_with_7_implemented_and_26_pending()
    {
        var matrix = Read("documentation/api-completion/API_COMPLETION_MASTER_MATRIX.md");
        var waveOneSection = Section(
            matrix,
            "## 6. Wave 1 detailed execution matrix",
            "## 7. Completion rules");

        var rows = Regex.Matches(
            waveOneSection,
            @"^\| `API-(?:IAM|ORG|PMT|CAT|REF)-[^`]+` \|",
            RegexOptions.Multiline | RegexOptions.CultureInvariant);
        var implemented = Regex.Matches(
            waveOneSection,
            @"^\| `API-(?:IAM|ORG|PMT|CAT|REF)-[^`]+` \|.*\| IMPLEMENTED \|",
            RegexOptions.Multiline | RegexOptions.CultureInvariant);
        var pending = Regex.Matches(
            waveOneSection,
            @"^\| `API-(?:IAM|ORG|PMT|CAT|REF)-[^`]+` \|.*\| PENDING \|",
            RegexOptions.Multiline | RegexOptions.CultureInvariant);

        Assert.Equal(33, rows.Count);
        Assert.Equal(7, implemented.Count);
        Assert.Equal(26, pending.Count);
    }

    private static IReadOnlyList<ApiContractRow> ParseInventory()
    {
        var rows = new List<ApiContractRow>();
        rows.AddRange(ParseCoreStyle(Read("documentation/blueprint-api-contract/02A_ENDPOINT_INVENTORY_CORE.md")));
        rows.AddRange(ParseCoreStyle(Read("documentation/blueprint-api-contract/02B_ENDPOINT_INVENTORY_OPERATIONS.md")));
        rows.AddRange(ParseTechnicalStyle(Read("documentation/blueprint-api-contract/02C_ENDPOINT_INVENTORY_TECHNICAL_OPERATIONS.md")));
        return rows;
    }

    private static IEnumerable<ApiContractRow> ParseCoreStyle(string content)
    {
        const string pattern = @"^\| `(?<api>API-[^`]+)` \| `(?<operation>[^`]+)` \| (?<method>GET|POST|PATCH|PUT|DELETE) `(?<route>[^`]+)` \|";
        return Regex.Matches(content, pattern, RegexOptions.Multiline | RegexOptions.CultureInvariant)
            .Select(match => new ApiContractRow(
                match.Groups["api"].Value,
                match.Groups["operation"].Value,
                match.Groups["method"].Value,
                match.Groups["route"].Value));
    }

    private static IEnumerable<ApiContractRow> ParseTechnicalStyle(string content)
    {
        const string pattern = @"^\| `(?<api>API-\d+)` \| (?<method>GET|POST|PATCH|PUT|DELETE) \| `(?<route>[^`]+)` \| `(?<operation>[^`]+)` \|";
        return Regex.Matches(content, pattern, RegexOptions.Multiline | RegexOptions.CultureInvariant)
            .Select(match => new ApiContractRow(
                match.Groups["api"].Value,
                match.Groups["operation"].Value,
                match.Groups["method"].Value,
                match.Groups["route"].Value));
    }

    private static string Section(string content, string startMarker, string endMarker)
    {
        var start = content.IndexOf(startMarker, StringComparison.Ordinal);
        var end = content.IndexOf(endMarker, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start, "Expected matrix section markers were not found in order.");
        return content[start..end];
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "api-accounting.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root containing api-accounting.sln was not found.");
    }

    private sealed record ApiContractRow(string ApiId, string OperationId, string Method, string Route);
}
