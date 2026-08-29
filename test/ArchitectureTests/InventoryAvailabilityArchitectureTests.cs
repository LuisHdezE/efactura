using Xunit;

namespace ArchitectureTests;

public sealed class InventoryAvailabilityArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void Inventory_controller_exposes_only_API_INV_001_to_004_in_this_slice()
    {
        var content = Read("src/WebApi/Controllers/V1/InventoryController.cs");

        Assert.Contains("[Route(\"api/v1/inventory\")]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"positions\")]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"positions/{positionId:guid}\")]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"movements\")]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpPost(\"adjustments\")]", content, StringComparison.Ordinal);

        Assert.DoesNotContain("stock-transfers", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("replenishment", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("purchase-orders", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Inventory_domain_is_framework_and_persistence_free()
    {
        var content = Read("src/Domain/Inventory/Inventory.cs");

        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.AspNetCore", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Dapper", content, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Inventory_repository_does_not_own_transaction_or_SaveChanges_and_has_no_ad_hoc_sql_write()
    {
        var content = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfInventoryRepository.cs");

        Assert.DoesNotContain("SaveChanges", content, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTransaction", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Commit", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Rollback", content, StringComparison.Ordinal);
        Assert.DoesNotContain("using Dapper", content, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecuteSql", content, StringComparison.Ordinal);
        Assert.DoesNotContain("INSERT ", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE ", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE ", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Stock_adjustment_is_idempotent_audited_and_transactionally_orchestrated_in_Application()
    {
        var content = Read("src/Application/Inventory/InventoryApplication.cs");

        Assert.Contains("ITransactionManager", content, StringComparison.Ordinal);
        Assert.Contains("IUnitOfWork", content, StringComparison.Ordinal);
        Assert.Contains("IIdempotencyStore", content, StringComparison.Ordinal);
        Assert.Contains("IAuditWriter", content, StringComparison.Ordinal);
        Assert.Contains("IOutboxWriter", content, StringComparison.Ordinal);
        Assert.Contains("inventory.adjustment.posted", content, StringComparison.Ordinal);
        Assert.Contains("concurrency_conflict", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Inventory_position_is_unique_by_organization_item_and_location_and_movement_is_append_oriented()
    {
        var context = Read("src/Infrastructure/Persistence/V1/Write/V1PersistenceDbContext.cs");
        var domain = Read("src/Domain/Inventory/Inventory.cs");

        Assert.Contains("new { x.OrganizationId, x.ItemId, x.LocationId }).IsUnique()", context, StringComparison.Ordinal);
        Assert.Contains("public sealed class StockMovement", domain, StringComparison.Ordinal);
        Assert.DoesNotContain("void Update(", domain, StringComparison.Ordinal);
        Assert.DoesNotContain("void Delete(", domain, StringComparison.Ordinal);
    }

    [Fact]
    public void Availability_checker_respects_Catalog_TrackInventory_instead_of_assuming_every_product_tracks_stock()
    {
        var content = Read("src/Application/Inventory/InventoryApplication.cs");

        Assert.Contains("!item.TrackInventory", content, StringComparison.Ordinal);
        Assert.Contains("ICommercialItemRepository", content, StringComparison.Ordinal);
        Assert.Contains("available >= requirement.Quantity", content, StringComparison.Ordinal);
    }

    [Fact]
    public void Adjustment_API_requires_approved_permission_and_idempotency_contract()
    {
        var controller = Read("src/WebApi/Controllers/V1/InventoryController.cs");

        Assert.Contains("RequirePermission(Permissions.InventoryAdjust)", controller, StringComparison.Ordinal);
        Assert.Contains("V1RequestContract.RequireIdempotencyKey", controller, StringComparison.Ordinal);
        Assert.Contains("ExpectedVersion", Read("src/WebApi/Controllers/V1/Contracts/InventoryContracts.cs"), StringComparison.Ordinal);
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "api-accounting.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
