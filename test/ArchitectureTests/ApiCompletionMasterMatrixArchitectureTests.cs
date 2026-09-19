using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace ArchitectureTests;

public sealed class ApiCompletionMasterMatrixArchitectureTests
{
    private const int CurrentInventoryCount = 194;
    private const int CurrentImplementedCount = 41;
    private const int CurrentMissingCount = 153;

    [Fact]
    public void Public_v1_inventory_has_194_unique_operations_and_preserves_the_193_to_194_chronology()
    {
        var inventoryA = InventoryIds(Read("documentation/blueprint-api-contract/02A_ENDPOINT_INVENTORY_CORE.md"));
        var inventoryB = InventoryIds(Read("documentation/blueprint-api-contract/02B_ENDPOINT_INVENTORY_OPERATIONS.md"));
        var inventoryC = InventoryIds(Read("documentation/blueprint-api-contract/02C_ENDPOINT_INVENTORY_TECHNICAL_OPERATIONS.md"));
        var all = inventoryA.Concat(inventoryB).Concat(inventoryC).ToArray();

        Assert.Equal(79, inventoryA.Length);
        Assert.Equal(93, inventoryB.Length);
        Assert.Equal(22, inventoryC.Length);
        Assert.Equal(CurrentInventoryCount, all.Length);
        Assert.Equal(CurrentInventoryCount, all.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("API-FIS-010", all);

        var acceptance = Read("documentation/blueprint-api-contract/11_API_CONTRACT_ACCEPTANCE_DRAFT.md");
        var matrix = Read("documentation/API_COMPLETION_MASTER_MATRIX.md");

        Assert.Contains("193 unique public v1 operations", acceptance, StringComparison.Ordinal);
        Assert.Contains("original commercial/fiscal/administrative design: **171** operations", acceptance, StringComparison.Ordinal);
        Assert.Contains("Technical Operations Console amendment: **22** operations", acceptance, StringComparison.Ordinal);

        Assert.Contains("current inventory total: **194 operations**", matrix, StringComparison.Ordinal);
        Assert.Contains("API-FIS-010", matrix, StringComparison.Ordinal);
        Assert.Contains("193", matrix, StringComparison.Ordinal);
        Assert.Contains("No existing API ID is renumbered to hide this chronology.", matrix, StringComparison.Ordinal);
    }

    [Fact]
    public void Wave_ledger_assigns_every_inventory_operation_exactly_once()
    {
        var inventoryIds = new[]
            {
                Read("documentation/blueprint-api-contract/02A_ENDPOINT_INVENTORY_CORE.md"),
                Read("documentation/blueprint-api-contract/02B_ENDPOINT_INVENTORY_OPERATIONS.md"),
                Read("documentation/blueprint-api-contract/02C_ENDPOINT_INVENTORY_TECHNICAL_OPERATIONS.md")
            }
            .SelectMany(InventoryIds)
            .ToHashSet(StringComparer.Ordinal);

        var ledger = Read("documentation/API_COMPLETION_MASTER_MATRIX_WAVE_COUNTS.md");
        var rows = Regex.Matches(
            ledger,
            @"(?m)^\|\s*(?<wave>[1-7])\s*\|\s*(?<families>(?:`[^`]+`(?:,\s*)?)+)\s*\|\s*(?<count>\d+)\s*\|\s*(?<implemented>.*?)\s*\|\s*$");

        Assert.Equal(7, rows.Count);

        var assigned = new List<string>();
        foreach (Match row in rows)
        {
            var tokens = Regex.Matches(row.Groups["families"].Value, @"`(?<token>[^`]+)`")
                .Select(match => match.Groups["token"].Value)
                .ToArray();
            var expanded = tokens.SelectMany(ExpandFamilyToken).ToArray();
            var declaredCount = int.Parse(row.Groups["count"].Value);

            Assert.Equal(declaredCount, expanded.Length);
            assigned.AddRange(expanded);
        }

        Assert.Equal(CurrentInventoryCount, assigned.Count);
        Assert.Equal(CurrentInventoryCount, assigned.Distinct(StringComparer.Ordinal).Count());
        Assert.True(inventoryIds.SetEquals(assigned), "Wave ledger must cover the exact current public-v1 inventory.");
    }

    [Fact]
    public void Wave_ledger_arithmetic_reconciles_194_41_153_and_wave1_30_7_23()
    {
        var ledger = Read("documentation/API_COMPLETION_MASTER_MATRIX_WAVE_COUNTS.md");
        var rows = Regex.Matches(
            ledger,
            @"(?m)^\|\s*(?<wave>[1-7])\s*\|\s*[^|]+\|\s*(?<total>\d+)\s*\|\s*(?<implemented>\d+)\s*\|\s*(?<missing>\d+)\s*\|\s*[^|]+\|\s*$");

        Assert.Equal(7, rows.Count);

        var total = 0;
        var implemented = 0;
        var missing = 0;

        foreach (Match row in rows)
        {
            var rowTotal = int.Parse(row.Groups["total"].Value);
            var rowImplemented = int.Parse(row.Groups["implemented"].Value);
            var rowMissing = int.Parse(row.Groups["missing"].Value);

            Assert.Equal(rowTotal, rowImplemented + rowMissing);
            total += rowTotal;
            implemented += rowImplemented;
            missing += rowMissing;

            if (row.Groups["wave"].Value == "1")
            {
                Assert.Equal(30, rowTotal);
                Assert.Equal(7, rowImplemented);
                Assert.Equal(23, rowMissing);
            }
        }

        Assert.Equal(CurrentInventoryCount, total);
        Assert.Equal(CurrentImplementedCount, implemented);
        Assert.Equal(CurrentMissingCount, missing);
        Assert.Equal(CurrentInventoryCount, implemented + missing);

        Assert.Contains("**194**", ledger, StringComparison.Ordinal);
        Assert.Contains("**41**", ledger, StringComparison.Ordinal);
        Assert.Contains("**153**", ledger, StringComparison.Ordinal);
    }

    [Fact]
    public void Clean_architecture_guard_tracks_both_completion_matrix_documents()
    {
        var workflow = Read(".github/workflows/clean-architecture.yml");

        Assert.Equal(2, Count(workflow, "documentation/API_COMPLETION_MASTER_MATRIX.md"));
        Assert.Equal(2, Count(workflow, "documentation/API_COMPLETION_MASTER_MATRIX_WAVE_COUNTS.md"));
    }

    private static string[] InventoryIds(string text) =>
        Regex.Matches(text, @"(?m)^\|\s*`(?<id>API-[^`]+)`\s*\|")
            .Select(match => match.Groups["id"].Value)
            .ToArray();

    private static IReadOnlyCollection<string> ExpandFamilyToken(string token)
    {
        var technicalRange = Regex.Match(token, @"^API-(?<start>\d{3})\.\.API-(?<end>\d{3})$");
        if (technicalRange.Success)
        {
            return Range(
                string.Empty,
                int.Parse(technicalRange.Groups["start"].Value),
                int.Parse(technicalRange.Groups["end"].Value),
                technical: true);
        }

        var familyRange = Regex.Match(token, @"^(?<prefix>[A-Z]+)-(?<start>\d{3})\.\.(?<end>\d{3})$");
        if (familyRange.Success)
        {
            return Range(
                familyRange.Groups["prefix"].Value,
                int.Parse(familyRange.Groups["start"].Value),
                int.Parse(familyRange.Groups["end"].Value),
                technical: false);
        }

        var single = Regex.Match(token, @"^(?<prefix>[A-Z]+)-(?<number>\d{3})$");
        if (single.Success)
        {
            return new[] { $"API-{single.Groups["prefix"].Value}-{single.Groups["number"].Value}" };
        }

        throw new InvalidOperationException($"Unsupported API family token '{token}'.");
    }

    private static IReadOnlyCollection<string> Range(string prefix, int start, int end, bool technical)
    {
        Assert.True(end >= start, $"Invalid API range {start}..{end}.");
        return Enumerable.Range(start, end - start + 1)
            .Select(number => technical ? $"API-{number:D3}" : $"API-{prefix}-{number:D3}")
            .ToArray();
    }

    private static int Count(string source, string value) =>
        Regex.Matches(source, Regex.Escape(value), RegexOptions.CultureInvariant).Count;

    private static string Read(string path) => File.ReadAllText(Full(path), Encoding.UTF8);

    private static string Full(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return Path.GetFullPath(full);
    }
}
