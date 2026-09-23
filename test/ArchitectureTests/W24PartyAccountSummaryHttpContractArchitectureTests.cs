using Xunit;

namespace ArchitectureTests;

public sealed class W24PartyAccountSummaryHttpContractArchitectureTests
{
    [Fact]
    public void Http_contract_locks_surface_without_exposing_endpoint_or_advancing_counts()
    {
        var contract = Read("documentation/api-completion-matrix/W2_4_PARTY_ACCOUNT_SUMMARY_HTTP_CONTRACT.md");
        var partiesController = Read("src/WebApi/Controllers/V1/PartiesController.cs");
        var wave = Read("documentation/api-completion-matrix/WAVE_2.md");

        Assert.Contains("API-PTY-008", contract, StringComparison.Ordinal);
        Assert.Contains("getPartyAccountSummary", contract, StringComparison.Ordinal);
        Assert.Contains("GET /api/v1/parties/{partyId}/account-summary", contract, StringComparison.Ordinal);
        Assert.Contains("permission: parties.read", contract, StringComparison.Ordinal);
        Assert.Contains("idempotency: NO", contract, StringComparison.Ordinal);
        Assert.Contains("receivablesApplicable", contract, StringComparison.Ordinal);
        Assert.Contains("payablesApplicable", contract, StringComparison.Ordinal);
        Assert.Contains("Cache-Control: private, no-store", contract, StringComparison.Ordinal);
        Assert.Contains("no client-controlled `asOf`", contract, StringComparison.Ordinal);
        Assert.Contains("20260923043000_V1ReceivableBalanceLedger", contract, StringComparison.Ordinal);
        Assert.Contains("20260923143000_V1PayableBalanceFoundation", contract, StringComparison.Ordinal);

        Assert.DoesNotContain("account-summary", partiesController, StringComparison.Ordinal);
        Assert.Contains("**25 implemented HTTP surfaces**, **1 non-implemented**", wave, StringComparison.Ordinal);
        Assert.Contains("`API-PTY-008`", wave, StringComparison.Ordinal);
        Assert.Contains("MISSING_HTTP", wave, StringComparison.Ordinal);
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src")) && Directory.Exists(Path.Combine(current.FullName, "test")))
                return current.FullName;
            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
