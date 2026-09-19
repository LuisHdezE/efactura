using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace ArchitectureTests;

public sealed class W11ReferenceDataReadinessArchitectureTests
{
    private const string WavePath = "documentation/api-completion-matrix/WAVE_1.md";
    private const string AuditPath = "documentation/api-completion-matrix/W1_1_REFERENCE_DATA_READINESS.md";

    private static readonly string[] ReferenceIds =
    {
        "API-REF-001",
        "API-REF-002",
        "API-REF-003",
        "API-REF-004",
        "API-REF-005",
        "API-REF-006",
        "API-REF-007",
        "API-REF-008"
    };

    [Fact]
    public void W1_1_reference_data_rows_are_audited_without_claiming_runtime_implementation()
    {
        var rows = ReferenceRows(Read(WavePath));

        Assert.Equal(8, rows.Length);
        Assert.Equal(ReferenceIds, rows.Select(row => row.Id).ToArray());
        Assert.All(rows, row => Assert.Equal("MISSING_HTTP", row.Implementation));
        Assert.DoesNotContain(rows, row => row.Readiness == "NOT_YET_AUDITED");

        Assert.Equal(
            new[] { "API-REF-002", "API-REF-003" },
            rows.Where(row => row.Readiness == "READY_FOR_FOUNDATION")
                .Select(row => row.Id)
                .ToArray());

        Assert.Equal(
            new[]
            {
                "API-REF-001",
                "API-REF-004",
                "API-REF-005",
                "API-REF-006",
                "API-REF-007",
                "API-REF-008"
            },
            rows.Where(row => row.Readiness == "PREREQUISITE_REQUIRED")
                .Select(row => row.Id)
                .ToArray());
    }

    [Fact]
    public void W1_1_readiness_audit_covers_the_exact_reference_data_contract_set()
    {
        var audit = Read(AuditPath);
        var contractedRows = Regex.Matches(
                audit,
                @"(?m)^\|\s*`(?<id>API-REF-\d{3})`\s*\|\s*`(?<operation>[^`]+)`\s*\|\s*GET\s+`(?<path>/api/v1/reference-data/[^`]+)`\s*\|\s*`(?<permission>[^`]+)`\s*\|\s*`(?<readiness>[^`]+)`\s*\|")
            .Select(match => match.Groups["id"].Value)
            .ToArray();

        Assert.Equal(ReferenceIds, contractedRows);
        Assert.Contains("`W1.1 READINESS AUDIT: CLOSED`", audit, StringComparison.Ordinal);
        Assert.Contains("global implementation baseline therefore remains **41 / 194**", audit, StringComparison.Ordinal);
        Assert.Contains("Wave 1 remains **7 / 30**", audit, StringComparison.Ordinal);
    }

    [Fact]
    public void W1_1_audit_forbids_direct_legacy_reference_repository_dependency()
    {
        var audit = Read(AuditPath);

        Assert.Contains(
            "must not depend directly on legacy `ApplicationCore` services or Npgsql/Dapper repositories",
            audit,
            StringComparison.Ordinal);
        Assert.Contains("W1.1A - Reference Data Foundation", audit, StringComparison.Ordinal);
        Assert.Contains("API-REF-002 listUruguayDepartments", audit, StringComparison.Ordinal);
        Assert.Contains("API-REF-003 listFiscalIdentityTypes", audit, StringComparison.Ordinal);
    }

    private static ReferenceRow[] ReferenceRows(string text) =>
        Regex.Matches(
                text,
                @"(?m)^\|\s*`(?<id>API-REF-\d{3})`\s*\|\s*`[^`]+`\s*\|\s*GET\s+`/api/v1/reference-data/[^`]+`\s*\|\s*`[^`]+`\s*\|\s*ACCEPTED\s*\|\s*(?<implementation>[^|]+?)\s*\|\s*[^|]+\|\s*(?<readiness>[^|]+?)\s*\|\s*1\s*\|")
            .Select(match => new ReferenceRow(
                match.Groups["id"].Value,
                match.Groups["implementation"].Value.Trim(),
                match.Groups["readiness"].Value.Trim()))
            .ToArray();

    private static string Read(string path) => File.ReadAllText(Full(path), Encoding.UTF8);

    private static string Full(string path)
    {
        var full = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", path);
        return Path.GetFullPath(full);
    }

    private sealed record ReferenceRow(string Id, string Implementation, string Readiness);
}
