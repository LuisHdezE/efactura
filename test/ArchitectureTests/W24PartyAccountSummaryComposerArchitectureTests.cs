using Xunit;

namespace ArchitectureTests;

public sealed class W24PartyAccountSummaryComposerArchitectureTests
{
    [Fact]
    public void Composer_is_application_owned_role_aware_and_composes_only_authoritative_ar_ap_ports()
    {
        var composer = Read("src/Application/Parties/PartyAccountSummary.cs");
        var registration = Read("src/Infrastructure/Persistence/V1/W24PartyAccountSummaryServiceCollectionExtensions.cs");

        Assert.Contains("IPartyAccountSummaryReadModel", composer, StringComparison.Ordinal);
        Assert.Contains("IPartyReceivableAccountReadModel", composer, StringComparison.Ordinal);
        Assert.Contains("IPartyPayableAccountReadModel", composer, StringComparison.Ordinal);
        Assert.Contains("PartyRole.Customer", composer, StringComparison.Ordinal);
        Assert.Contains("PartyRole.Supplier", composer, StringComparison.Ordinal);
        Assert.Contains("ReceivablesApplicable", composer, StringComparison.Ordinal);
        Assert.Contains("PayablesApplicable", composer, StringComparison.Ordinal);
        Assert.Contains("party.account_summary.party_scope_mismatch", composer, StringComparison.Ordinal);
        Assert.Contains("receivable_currency_duplicate", composer, StringComparison.Ordinal);
        Assert.Contains("payable_currency_duplicate", composer, StringComparison.Ordinal);

        Assert.Contains("IReceivableBalanceSourceReader", registration, StringComparison.Ordinal);
        Assert.Contains("IPayableBalanceSourceReader", registration, StringComparison.Ordinal);
        Assert.Contains("IPartyReceivableAccountReadModel", registration, StringComparison.Ordinal);
        Assert.Contains("IPartyPayableAccountReadModel", registration, StringComparison.Ordinal);
        Assert.Contains("IPartyAccountSummaryReadModel", registration, StringComparison.Ordinal);
    }

    [Fact]
    public void Http_candidate_delegates_only_to_composer_without_advancing_wave_counts()
    {
        var wave = Read("documentation/api-completion-matrix/WAVE_2.md");
        var controller = Read("src/WebApi/Controllers/V1/PartyAccountSummaryController.cs");

        Assert.Contains("IPartyAccountSummaryReadModel", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("IPartyReceivableAccountReadModel", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("IPartyPayableAccountReadModel", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("EfReceivableBalanceRepository", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("EfPayableBalanceRepository", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", controller, StringComparison.Ordinal);
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
