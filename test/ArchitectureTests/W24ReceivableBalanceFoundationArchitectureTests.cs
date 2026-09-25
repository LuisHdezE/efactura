using Xunit;

namespace ArchitectureTests;

public sealed class W24ReceivableBalanceFoundationArchitectureTests
{
    [Fact]
    public void Ar_foundation_is_append_only_and_application_owned()
    {
        var domain = Read("src/Domain/Receivables/ReceivableBalanceEffect.cs");
        var application = Read("src/Application/Receivables/ReceivableAccountBalance.cs");
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfReceivableBalanceRepository.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260923043000_V1ReceivableBalanceLedger.cs");

        Assert.Contains("CollectionAllocation", domain, StringComparison.Ordinal);
        Assert.Contains("CollectionReversal", domain, StringComparison.Ordinal);
        Assert.Contains("AdjustmentIncrease", domain, StringComparison.Ordinal);
        Assert.Contains("AdjustmentDecrease", domain, StringComparison.Ordinal);
        Assert.Contains("SignedDelta", domain, StringComparison.Ordinal);

        Assert.Contains("IPartyReceivableAccountReadModel", application, StringComparison.Ordinal);
        Assert.Contains("IReceivableBalanceSourceReader", application, StringComparison.Ordinal);
        Assert.Contains("negative_balance", application, StringComparison.Ordinal);
        Assert.Contains("reversal_amount_mismatch", application, StringComparison.Ordinal);
        Assert.Contains("Days91Plus", application, StringComparison.Ordinal);

        Assert.Contains("IReceivableBalanceEffectRepository", repository, StringComparison.Ordinal);
        Assert.Contains("OrganizationId == organizationId", repository, StringComparison.Ordinal);
        Assert.Contains("CustomerPartyId == partyId", repository, StringComparison.Ordinal);
        Assert.Contains("v1_receivable_balance_effects", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_ar_effect_source", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_ar_effect_reversal", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void Ar_foundation_remains_internal_after_runtime_closure()
    {
        var wave = Read("documentation/api-completion-matrix/WAVE_2.md");
        var controller = Read("src/WebApi/Controllers/V1/PartyAccountSummaryController.cs");

        Assert.Contains("IPartyAccountSummaryReadModel", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("IPartyReceivableAccountReadModel", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("EfReceivableBalanceRepository", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", controller, StringComparison.Ordinal);
        Assert.Contains("**26 implemented HTTP surfaces**, **0 non-implemented**", wave, StringComparison.Ordinal);
        Assert.Contains("`API-PTY-008`", wave, StringComparison.Ordinal);
        Assert.Contains("IMPLEMENTED", wave, StringComparison.Ordinal);
        Assert.Contains("RUNTIME_ACCEPTED / regression", wave, StringComparison.Ordinal);
        Assert.DoesNotContain("MISSING_HTTP", wave, StringComparison.Ordinal);
        Assert.DoesNotContain("PREREQUISITE_REQUIRED", wave, StringComparison.Ordinal);
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
