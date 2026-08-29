using Xunit;

namespace ArchitectureTests;

public sealed class CaeNumberingArchitectureTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Fact]
    public void CAE_controller_exposes_only_contracted_API_CAE_001_to_007_and_no_next_number_endpoint()
    {
        var content = Read("src/WebApi/Controllers/V1/CaeAuthorizationsController.cs");

        Assert.Contains("[Route(\"api/v1/cae-authorizations\")]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpGet]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"{caeId:guid}\")]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpPost(\"import\")]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpPost(\"{caeId:guid}/activate\")]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpGet(\"{caeId:guid}/allocations\")]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpPost(\"{caeId:guid}/allocations\")]", content, StringComparison.Ordinal);
        Assert.Contains("[HttpPost(\"{caeId:guid}/allocations/{allocationId:guid}/close\")]", content, StringComparison.Ordinal);
        Assert.DoesNotContain("next-number", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("nextNumber", Read("src/WebApi/Controllers/V1/Contracts/CaeContracts.cs"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CAE_domain_is_framework_and_provider_free()
    {
        var content = Read("src/Domain/Fiscal/CaeAuthorization.cs");

        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.AspNetCore", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Infrastructure", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Npgsql", content, StringComparison.Ordinal);
        Assert.DoesNotContain("MySql", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Dapper", content, StringComparison.Ordinal);
        Assert.DoesNotContain("DbContext", content, StringComparison.Ordinal);
    }

    [Fact]
    public void CAE_repository_does_not_own_transaction_SaveChanges_or_ad_hoc_SQL_write()
    {
        var content = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfCaeRepository.cs");

        Assert.DoesNotContain("SaveChanges", content, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTransaction", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Commit", content, StringComparison.Ordinal);
        Assert.DoesNotContain("Rollback", content, StringComparison.Ordinal);
        Assert.DoesNotContain("using Dapper", content, StringComparison.Ordinal);
        Assert.DoesNotContain("ExecuteSql", content, StringComparison.Ordinal);
        Assert.DoesNotContain("INSERT ", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE ", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE ", content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MERGE ", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Fiscal_number_allocator_is_an_internal_Application_port_and_does_not_commit_its_own_transaction()
    {
        var contracts = Read("src/Application/Fiscal/CaeContracts.cs");
        var allocator = Read("src/Application/Fiscal/FiscalNumberAllocator.cs");

        Assert.Contains("interface IFiscalNumberAllocator", contracts, StringComparison.Ordinal);
        Assert.Contains("class FiscalNumberAllocator", allocator, StringComparison.Ordinal);
        Assert.DoesNotContain("ITransactionManager", allocator, StringComparison.Ordinal);
        Assert.DoesNotContain("IUnitOfWork", allocator, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", allocator, StringComparison.Ordinal);
        Assert.DoesNotContain("BeginTransaction", allocator, StringComparison.Ordinal);
        Assert.Contains("fiscal.number.reserved", allocator, StringComparison.Ordinal);
    }

    [Fact]
    public void CAE_commands_are_idempotent_audited_outboxed_and_transactionally_orchestrated_in_Application()
    {
        var commands = Read("src/Application/Fiscal/CaeCommands.cs");

        Assert.Contains("ITransactionManager", commands, StringComparison.Ordinal);
        Assert.Contains("IUnitOfWork", commands, StringComparison.Ordinal);
        Assert.Contains("IIdempotencyStore", commands, StringComparison.Ordinal);
        Assert.Contains("IAuditWriter", commands, StringComparison.Ordinal);
        Assert.Contains("IOutboxWriter", commands, StringComparison.Ordinal);
        Assert.Contains("cae.imported", commands, StringComparison.Ordinal);
        Assert.Contains("cae.verified", commands, StringComparison.Ordinal);
        Assert.Contains("cae.activated", commands, StringComparison.Ordinal);
        Assert.Contains("cae.allocation.created", commands, StringComparison.Ordinal);
        Assert.Contains("cae.allocation.closed", commands, StringComparison.Ordinal);
    }

    [Fact]
    public void Fiscal_identity_has_independent_database_uniqueness_guard()
    {
        var context = Read("src/Infrastructure/Persistence/V1/Write/V1PersistenceDbContext.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260829160000_V1CaeNumbering.cs");

        Assert.Contains("new { x.OrganizationId, x.CfeType, x.Series, x.Number }).IsUnique()", context, StringComparison.Ordinal);
        Assert.Contains("IX_v1_fiscal_number_reservations_OrganizationId_CfeType_Series_Number", migration, StringComparison.Ordinal);
        Assert.Contains("unique: true", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void CAE_mutation_endpoints_require_fiscal_manage_cae_and_idempotency()
    {
        var controller = Read("src/WebApi/Controllers/V1/CaeAuthorizationsController.cs");

        Assert.Contains("RequirePermission(Permissions.FiscalManageCae)", controller, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(Permissions.FiscalRead)", controller, StringComparison.Ordinal);
        Assert.Equal(4, Count(controller, "V1RequestContract.RequireIdempotencyKey"));
    }

    [Fact]
    public void CAE_slice_does_not_cross_into_XML_signing_transport_or_sale_confirmation()
    {
        var domain = Read("src/Domain/Fiscal/CaeAuthorization.cs");
        var application = Read("src/Application/Fiscal/FiscalNumberAllocator.cs")
                          + Read("src/Application/Fiscal/CaeCommands.cs");
        var controller = Read("src/WebApi/Controllers/V1/CaeAuthorizationsController.cs");
        var combined = domain + application + controller;

        Assert.DoesNotContain("XmlDocument", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("XmlSigner", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("DgiTransport", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("ProviderSdk", combined, StringComparison.Ordinal);
        Assert.DoesNotContain("confirmSale", combined, StringComparison.OrdinalIgnoreCase);
    }

    private static int Count(string content, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = content.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }
        return count;
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
