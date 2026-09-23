using Xunit;

namespace ArchitectureTests;

public sealed class W24PayableBalanceFoundationArchitectureTests
{
    [Fact]
    public void Ap_foundation_is_source_governed_append_only_and_application_owned()
    {
        var payable = Read("src/Domain/Payables/Payables.cs");
        var effects = Read("src/Domain/Payables/PayableBalanceEffect.cs");
        var application = Read("src/Application/Payables/PayableAccountBalance.cs");
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfPayableBalanceRepository.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260923143000_V1PayableBalanceFoundation.cs");

        Assert.Contains("PurchaseReceipt", payable, StringComparison.Ordinal);
        Assert.Contains("ReceivedFiscalDocument", payable, StringComparison.Ordinal);
        Assert.DoesNotContain("SupplierRole", payable, StringComparison.Ordinal);

        Assert.Contains("SupplierPaymentAllocation", effects, StringComparison.Ordinal);
        Assert.Contains("SupplierPaymentReversal", effects, StringComparison.Ordinal);
        Assert.Contains("AdjustmentIncrease", effects, StringComparison.Ordinal);
        Assert.Contains("AdjustmentDecrease", effects, StringComparison.Ordinal);
        Assert.Contains("SignedDelta", effects, StringComparison.Ordinal);

        Assert.Contains("IPartyPayableAccountReadModel", application, StringComparison.Ordinal);
        Assert.Contains("IPayableBalanceSourceReader", application, StringComparison.Ordinal);
        Assert.Contains("negative_balance", application, StringComparison.Ordinal);
        Assert.Contains("reversal_amount_mismatch", application, StringComparison.Ordinal);
        Assert.Contains("Days91Plus", application, StringComparison.Ordinal);

        Assert.Contains("IPayableBalanceEffectRepository", repository, StringComparison.Ordinal);
        Assert.Contains("OrganizationId == organizationId", repository, StringComparison.Ordinal);
        Assert.Contains("SupplierPartyId == partyId", repository, StringComparison.Ordinal);

        Assert.Contains("v1_payables", migration, StringComparison.Ordinal);
        Assert.Contains("v1_payable_balance_effects", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_ap_org_source", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_ap_effect_source", migration, StringComparison.Ordinal);
        Assert.Contains("UX_v1_ap_effect_reversal", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void Ap_foundation_does_not_prematurely_expose_w24_http_surface_or_advance_counts()
    {
        var root = FindRepositoryRoot();
        var wave = Read("documentation/api-completion-matrix/WAVE_2.md");
        var controllerPath = Path.Combine(root, "src", "WebApi", "Controllers", "V1", "PartyAccountSummaryController.cs");

        Assert.False(File.Exists(controllerPath));
        Assert.Contains("**25 implemented HTTP surfaces**, **1 non-implemented**", wave, StringComparison.Ordinal);
        Assert.Contains("`API-PTY-008`", wave, StringComparison.Ordinal);
        Assert.Contains("MISSING_HTTP", wave, StringComparison.Ordinal);
        Assert.Contains("PREREQUISITE_REQUIRED", wave, StringComparison.Ordinal);
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
