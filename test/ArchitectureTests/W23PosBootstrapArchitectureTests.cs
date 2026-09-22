using Xunit;

namespace ArchitectureTests;

public sealed class W23PosBootstrapArchitectureTests
{
    [Fact]
    public void Pos_bootstrap_http_surface_matches_locked_contract()
    {
        var controller = Read("src/WebApi/Controllers/V1/PosBootstrapController.cs");
        var contracts = Read("src/WebApi/Controllers/V1/Contracts/PosBootstrapContracts.cs");

        Assert.Contains("api/v1/pos/bootstrap", controller, StringComparison.Ordinal);
        Assert.Contains("getPosBootstrap", controller, StringComparison.Ordinal);
        Assert.Contains("Permissions.SalesRead", controller, StringComparison.Ordinal);
        Assert.Contains("private, no-cache", controller, StringComparison.Ordinal);
        Assert.Contains("If-None-Match", controller, StringComparison.Ordinal);
        Assert.Contains("Status304NotModified", controller, StringComparison.Ordinal);
        Assert.Contains("Response.Headers.ETag", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpPost", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpPatch", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpPut", controller, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpDelete", controller, StringComparison.Ordinal);

        Assert.Contains("OrganizationId", contracts, StringComparison.Ordinal);
        Assert.Contains("GeneratedAtUtc", contracts, StringComparison.Ordinal);
        Assert.Contains("Contexts", contracts, StringComparison.Ordinal);
        Assert.Contains("LocationId", contracts, StringComparison.Ordinal);
        Assert.Contains("LocationName", contracts, StringComparison.Ordinal);
        Assert.Contains("DgiBranchCode", contracts, StringComparison.Ordinal);
        Assert.Contains("LocationVersion", contracts, StringComparison.Ordinal);
        Assert.Contains("TerminalId", contracts, StringComparison.Ordinal);
        Assert.Contains("Code", contracts, StringComparison.Ordinal);
        Assert.Contains("Name", contracts, StringComparison.Ordinal);
        Assert.Contains("TerminalVersion", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("Payment", contracts, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Price", contracts, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DefaultLocation", contracts, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DefaultTerminal", contracts, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Pos_bootstrap_application_path_is_read_only_scoped_and_deterministic()
    {
        var source = Read("src/Application/Sales/PosBootstrap.cs");

        Assert.Contains("IFiscalLocationRepository", source, StringComparison.Ordinal);
        Assert.Contains("ITerminalRepository", source, StringComparison.Ordinal);
        Assert.Contains("IActorContextAccessor", source, StringComparison.Ordinal);
        Assert.Contains("SalesAuthorization.Ensure", source, StringComparison.Ordinal);
        Assert.Contains("Permissions.SalesRead", source, StringComparison.Ordinal);
        Assert.Contains("actor.LocationScopes.Contains", source, StringComparison.Ordinal);
        Assert.Contains("actor.TerminalScopes.Count == 0", source, StringComparison.Ordinal);
        Assert.Contains("StringComparer.OrdinalIgnoreCase", source, StringComparison.Ordinal);
        Assert.Contains("SHA256.HashData", source, StringComparison.Ordinal);

        Assert.DoesNotContain("ITransactionManager", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IUnitOfWork", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IIdempotencyStore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IAuditWriter", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IOutboxWriter", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IPayment", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IParty", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ICommercialItem", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalCfe", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IInventory", source, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", source, StringComparison.Ordinal);
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
