using Xunit;

namespace ArchitectureTests;

public sealed class W24PartyAccountSummaryHttpContractArchitectureTests
{
    [Fact]
    public void Http_implementation_matches_the_locked_contract_without_advancing_runtime_counts()
    {
        var contract = Read("documentation/api-completion-matrix/W2_4_PARTY_ACCOUNT_SUMMARY_HTTP_CONTRACT.md");
        var controller = Read("src/WebApi/Controllers/V1/PartyAccountSummaryController.cs");
        var partiesController = Read("src/WebApi/Controllers/V1/PartiesController.cs");
        var contracts = Read("src/WebApi/Controllers/V1/Contracts/PartyAccountSummaryContracts.cs");
        var program = Read("src/WebApi/Program.cs");
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

        Assert.Contains("[Route(\"api/v1/parties\")]", controller, StringComparison.Ordinal);
        Assert.Contains(
            "[HttpGet(\"{partyId:guid}/account-summary\", Name = \"getPartyAccountSummary\")]",
            controller,
            StringComparison.Ordinal);
        Assert.Contains("[RequirePermission(Permissions.PartiesRead)]", controller, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status200OK", controller, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status401Unauthorized", controller, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status403Forbidden", controller, StringComparison.Ordinal);
        Assert.Contains("StatusCodes.Status404NotFound", controller, StringComparison.Ordinal);
        Assert.Contains("var organizationId = _organization.Resolve(Request);", controller, StringComparison.Ordinal);
        Assert.Contains("var asOfUtc = DateTimeOffset.UtcNow;", controller, StringComparison.Ordinal);
        Assert.Contains("_accountSummary.GetAsync(", controller, StringComparison.Ordinal);
        Assert.Contains("Response.Headers[\"Cache-Control\"] = \"private, no-store\";", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("V1RequestContract.RequireIdempotencyKey", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("ETag", controller, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DbContext", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("EfReceivable", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("EfPayable", controller, StringComparison.Ordinal);

        Assert.Contains("record PartyAccountSummaryDto", contracts, StringComparison.Ordinal);
        Assert.Contains("bool ReceivablesApplicable", contracts, StringComparison.Ordinal);
        Assert.Contains("IReadOnlyList<PartyAccountCurrencyDto> Receivables", contracts, StringComparison.Ordinal);
        Assert.Contains("bool PayablesApplicable", contracts, StringComparison.Ordinal);
        Assert.Contains("IReadOnlyList<PartyAccountCurrencyDto> Payables", contracts, StringComparison.Ordinal);
        Assert.Contains("record PartyAccountCurrencyDto", contracts, StringComparison.Ordinal);
        Assert.Contains("record PartyAccountAgingDto", contracts, StringComparison.Ordinal);
        Assert.Contains("decimal Days91Plus", contracts, StringComparison.Ordinal);

        Assert.Contains("builder.Services.AddW24PartyAccountSummaryComposition();", program, StringComparison.Ordinal);
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
