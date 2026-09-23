using Xunit;

namespace ArchitectureTests;

public sealed class W24ReceivableAccountFoundationArchitectureTests
{
    [Fact]
    public void W24_first_increment_stays_internal_and_does_not_expose_wave3_or_party_http()
    {
        var application = Read("src/Application/Receivables/ReceivableAccountReadModel.cs");
        var infrastructure = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfReceivableAccountStore.cs");
        var partiesController = Read("src/WebApi/Controllers/V1/PartiesController.cs");

        Assert.Contains("IReceivableBalanceFactStore", application, StringComparison.Ordinal);
        Assert.Contains("IReceivableAccountReadModel", application, StringComparison.Ordinal);
        Assert.Contains("GetPartySummaryAsync", application, StringComparison.Ordinal);
        Assert.Contains("EfReceivableAccountStore", infrastructure, StringComparison.Ordinal);
        Assert.Contains("OrganizationId == normalizedOrganizationId", infrastructure, StringComparison.Ordinal);
        Assert.Contains("CustomerPartyId == partyId", infrastructure, StringComparison.Ordinal);
        Assert.Contains("OrderBy(x => x.Key, StringComparer.Ordinal)", infrastructure, StringComparison.Ordinal);

        Assert.DoesNotContain("account-summary", partiesController, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("api/v1/receivables", infrastructure, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("api/v1/collections", infrastructure, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Receivable_balance_facts_are_durable_and_provider_neutral()
    {
        var domain = Read("src/Domain/Receivables/ReceivableBalanceFacts.cs");
        var records = Read("src/Infrastructure/Persistence/V1/Write/Models/FinanceRecords.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260923033000_V1ReceivableBalanceFacts.cs");

        Assert.Contains("AdjustmentIncrease", domain, StringComparison.Ordinal);
        Assert.Contains("AdjustmentDecrease", domain, StringComparison.Ordinal);
        Assert.Contains("CollectionAllocation", domain, StringComparison.Ordinal);
        Assert.Contains("CollectionReversal", domain, StringComparison.Ordinal);
        Assert.Contains("v1_receivable_balance_facts", records, StringComparison.Ordinal);
        Assert.Contains("[Precision(18, 6)]", records, StringComparison.Ordinal);
        Assert.Contains("precision: 18, scale: 6", migration, StringComparison.Ordinal);
        Assert.Contains("ReferentialAction.Restrict", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("Npgsql", migration, StringComparison.Ordinal);
        Assert.DoesNotContain("MySql", migration, StringComparison.Ordinal);
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
