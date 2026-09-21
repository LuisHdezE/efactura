using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace ArchitectureTests;

public sealed class ApiCompletionMasterMatrixArchitectureTests
{
    private const int CurrentInventoryCount = 194;
    private const int CurrentImplementedCount = 64;
    private const int CurrentMissingHttpCount = 128;
    private const int CurrentContractCollisionCount = 2;
    private const int CurrentNonImplementedCount = 130;
    private const int CurrentDistinctRouteSignatureCount = 193;

    private static readonly string[] MatrixShardPaths =
    {
        "documentation/api-completion-matrix/WAVE_1.md",
        "documentation/api-completion-matrix/WAVE_2.md",
        "documentation/api-completion-matrix/WAVE_3.md",
        "documentation/api-completion-matrix/WAVE_4.md",
        "documentation/api-completion-matrix/WAVE_5.md",
        "documentation/api-completion-matrix/WAVE_6.md",
        "documentation/api-completion-matrix/WAVE_7.md"
    };

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
        Assert.Contains("historical accepted total: `193` operations", matrix, StringComparison.Ordinal);
        Assert.Contains("No existing API ID is renumbered to hide this chronology.", matrix, StringComparison.Ordinal);
    }

    [Fact]
    public void Wave_ledger_assigns_every_inventory_operation_exactly_once()
    {
        var inventoryIds = InventoryContractRows().Select(row => row.Id).ToHashSet(StringComparer.Ordinal);
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
    public void Wave_ledger_arithmetic_reconciles_current_194_operation_baseline()
    {
        var ledger = Read("documentation/API_COMPLETION_MASTER_MATRIX_WAVE_COUNTS.md");
        var rows = Regex.Matches(
            ledger,
            @"(?m)^\|\s*(?<wave>[1-7])\s*\|\s*[^|]+\|\s*(?<total>\d+)\s*\|\s*(?<implemented>\d+)\s*\|\s*(?<missing>\d+)\s*\|\s*(?<collision>\d+)\s*\|\s*(?<nonimplemented>\d+)\s*\|\s*[^|]+\|\s*$");

        Assert.Equal(7, rows.Count);

        var total = 0;
        var implemented = 0;
        var missing = 0;
        var collisions = 0;
        var nonImplemented = 0;

        foreach (Match row in rows)
        {
            var rowTotal = int.Parse(row.Groups["total"].Value);
            var rowImplemented = int.Parse(row.Groups["implemented"].Value);
            var rowMissing = int.Parse(row.Groups["missing"].Value);
            var rowCollisions = int.Parse(row.Groups["collision"].Value);
            var rowNonImplemented = int.Parse(row.Groups["nonimplemented"].Value);

            Assert.Equal(rowTotal, rowImplemented + rowNonImplemented);
            Assert.Equal(rowNonImplemented, rowMissing + rowCollisions);

            total += rowTotal;
            implemented += rowImplemented;
            missing += rowMissing;
            collisions += rowCollisions;
            nonImplemented += rowNonImplemented;
        }

        Assert.Equal(CurrentInventoryCount, total);
        Assert.Equal(CurrentImplementedCount, implemented);
        Assert.Equal(CurrentMissingHttpCount, missing);
        Assert.Equal(CurrentContractCollisionCount, collisions);
        Assert.Equal(CurrentNonImplementedCount, nonImplemented);
        Assert.Equal(CurrentInventoryCount, implemented + nonImplemented);
    }

    [Fact]
    public void Master_matrix_contains_every_inventory_operation_exactly_once_and_reconciles_status_totals()
    {
        var inventoryIds = InventoryContractRows().Select(row => row.Id).ToHashSet(StringComparer.Ordinal);
        var rows = MatrixOperationRows();

        Assert.Equal(CurrentInventoryCount, rows.Length);
        Assert.Equal(CurrentInventoryCount, rows.Select(row => row.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.True(inventoryIds.SetEquals(rows.Select(row => row.Id)), "Matrix shards must cover the exact public-v1 inventory.");
        Assert.Equal(CurrentImplementedCount, rows.Count(row => row.Implementation == "IMPLEMENTED"));
        Assert.Equal(CurrentMissingHttpCount, rows.Count(row => row.Implementation == "MISSING_HTTP"));
        Assert.Equal(CurrentContractCollisionCount, rows.Count(row => row.Implementation == "CONTRACT_COLLISION"));

        var expectedWaveCounts = new[] { 30, 26, 24, 23, 40, 21, 30 };
        for (var index = 0; index < MatrixShardPaths.Length; index++)
        {
            var shardRows = MatrixOperationRows(Read(MatrixShardPaths[index]));
            Assert.Equal(expectedWaveCounts[index], shardRows.Length);
            Assert.All(shardRows, row => Assert.Equal(index + 1, row.Wave));
        }
    }

    [Fact]
    public void Master_matrix_contract_columns_match_inventory_exactly()
    {
        var inventory = InventoryContractRows().ToDictionary(row => row.Id, StringComparer.Ordinal);
        var matrix = MatrixOperationRows().ToDictionary(row => row.Id, StringComparer.Ordinal);

        Assert.Equal(CurrentInventoryCount, inventory.Count);
        Assert.Equal(CurrentInventoryCount, matrix.Count);

        foreach (var expected in inventory.Values)
        {
            Assert.True(matrix.TryGetValue(expected.Id, out var actual), $"Missing matrix row for {expected.Id}.");
            Assert.Equal(expected.OperationId, actual!.OperationId);
            Assert.Equal(expected.Method, actual.Method);
            Assert.Equal(expected.Path, actual.Path);
            Assert.Equal(expected.Permission, actual.Permission);
        }
    }

    [Fact]
    public void Inventory_route_signatures_expose_only_the_known_MON_001_API_180_collision()
    {
        var rows = InventoryContractRows();
        var duplicateGroups = rows
            .GroupBy(row => row.Signature, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .ToArray();

        Assert.Equal(CurrentDistinctRouteSignatureCount, rows.Select(row => row.Signature).Distinct(StringComparer.Ordinal).Count());
        Assert.Single(duplicateGroups);

        var collision = duplicateGroups[0];
        Assert.Equal("GET /api/v1/operations/integrations", collision.Key);
        Assert.Equal(
            new[] { "API-180", "API-MON-001" },
            collision.Select(row => row.Id).OrderBy(id => id, StringComparer.Ordinal).ToArray());

        var wave7 = MatrixOperationRows(Read("documentation/api-completion-matrix/WAVE_7.md"))
            .Where(row => row.Id is "API-MON-001" or "API-180")
            .ToArray();

        Assert.Equal(2, wave7.Length);
        Assert.All(wave7, row => Assert.Equal("CONTRACT_COLLISION", row.Implementation));
        Assert.All(wave7, row => Assert.Equal("BLOCKED_BY_CONTRACT", row.DeepReadiness));

        var master = Read("documentation/API_COMPLETION_MASTER_MATRIX.md");
        Assert.Contains("194 unique operation IDs but 193 distinct HTTP method/path signatures", master, StringComparison.Ordinal);
        Assert.Contains("API-MON-001", master, StringComparison.Ordinal);
        Assert.Contains("API-180", master, StringComparison.Ordinal);
    }

    [Fact]
    public void Clean_architecture_guard_tracks_completion_matrix_documents_and_shards()
    {
        var workflow = Read(".github/workflows/clean-architecture.yml");

        Assert.Equal(2, Count(workflow, "documentation/API_COMPLETION_MASTER_MATRIX.md"));
        Assert.Equal(2, Count(workflow, "documentation/API_COMPLETION_MASTER_MATRIX_WAVE_COUNTS.md"));
        Assert.Equal(2, Count(workflow, "documentation/api-completion-matrix/**"));
    }

    private static ApiMatrixRow[] MatrixOperationRows() =>
        MatrixShardPaths.SelectMany(path => MatrixOperationRows(Read(path))).ToArray();

    private static ApiMatrixRow[] MatrixOperationRows(string text) =>
        Regex.Matches(
                text,
                @"(?m)^\|\s*`(?<id>API-[^`]+)`\s*\|\s*`(?<operation>[^`]+)`\s*\|\s*(?<method>GET|POST|PUT|PATCH|DELETE)\s+`(?<path>[^`]+)`\s*\|\s*`(?<permission>[^`]+)`\s*\|\s*(?<contract>[^|]+?)\s*\|\s*(?<implementation>[^|]+?)\s*\|\s*(?<evidence>[^|]+?)\s*\|\s*(?<readiness>[^|]+?)\s*\|\s*(?<wave>[1-7])\s*\|")
            .Select(match => new ApiMatrixRow(
                match.Groups["id"].Value,
                match.Groups["operation"].Value,
                match.Groups["method"].Value,
                match.Groups["path"].Value,
                match.Groups["permission"].Value,
                match.Groups["implementation"].Value.Trim(),
                match.Groups["readiness"].Value.Trim(),
                int.Parse(match.Groups["wave"].Value)))
            .ToArray();

    private static ApiContractRow[] InventoryContractRows()
    {
        var standard = new[]
            {
                Read("documentation/blueprint-api-contract/02A_ENDPOINT_INVENTORY_CORE.md"),
                Read("documentation/blueprint-api-contract/02B_ENDPOINT_INVENTORY_OPERATIONS.md")
            }
            .SelectMany(InventoryContractRowsStandard);
        var technical = InventoryContractRowsTechnical(
            Read("documentation/blueprint-api-contract/02C_ENDPOINT_INVENTORY_TECHNICAL_OPERATIONS.md"));

        return standard.Concat(technical).ToArray();
    }

    private static ApiContractRow[] InventoryContractRowsStandard(string text) =>
        Regex.Matches(
                text,
                @"(?m)^\|\s*`(?<id>API-[^`]+)`\s*\|\s*`(?<operation>[^`]+)`\s*\|\s*(?<method>GET|POST|PUT|PATCH|DELETE)\s+`(?<path>[^`]+)`\s*\|\s*`(?<permission>[^`]+)`\s*\|")
            .Select(ToContractRow)
            .ToArray();

    private static ApiContractRow[] InventoryContractRowsTechnical(string text) =>
        Regex.Matches(
                text,
                @"(?m)^\|\s*`(?<id>API-\d{3})`\s*\|\s*(?<method>GET|POST|PUT|PATCH|DELETE)\s*\|\s*`(?<path>[^`]+)`\s*\|\s*`(?<operation>[^`]+)`\s*\|\s*`(?<permission>[^`]+)`\s*\|")
            .Select(ToContractRow)
            .ToArray();

    private static ApiContractRow ToContractRow(Match match) =>
        new(
            match.Groups["id"].Value,
            match.Groups["operation"].Value,
            match.Groups["method"].Value,
            match.Groups["path"].Value,
            match.Groups["permission"].Value);

    private static string[] InventoryIds(string text) =>
        Regex.Matches(text, @"(?m)^\|\s*`(?<id>API-[^`]+)`\s*\|")
            .Select(match => match.Groups["id"].Value)
            .ToArray();

    private static IReadOnlyCollection<string> ExpandFamilyToken(string token)
    {
        var technicalRange = Regex.Match(token, @"^API-(?<start>\d{3})\.\.API-(?<end>\d{3})$");
        if (technicalRange.Success)
        {
            return Range(string.Empty, int.Parse(technicalRange.Groups["start"].Value), int.Parse(technicalRange.Groups["end"].Value), technical: true);
        }

        var familyRange = Regex.Match(token, @"^(?<prefix>[A-Z]+)-(?<start>\d{3})\.\.(?<end>\d{3})$");
        if (familyRange.Success)
        {
            return Range(familyRange.Groups["prefix"].Value, int.Parse(familyRange.Groups["start"].Value), int.Parse(familyRange.Groups["end"].Value), technical: false);
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

    private sealed record ApiContractRow(string Id, string OperationId, string Method, string Path, string Permission)
    {
        public string Signature => $"{Method} {Path}";
    }

    private sealed record ApiMatrixRow(
        string Id,
        string OperationId,
        string Method,
        string Path,
        string Permission,
        string Implementation,
        string DeepReadiness,
        int Wave);
}
