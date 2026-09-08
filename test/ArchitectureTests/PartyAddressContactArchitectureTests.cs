using Xunit;

namespace ArchitectureTests;

public sealed class PartyAddressContactArchitectureTests
{
    [Fact]
    public void Party_address_and_contact_domain_is_framework_and_fiscal_transport_free()
    {
        var source = Read("src/Domain/Parties/Party.cs");

        Assert.Contains("PartyAddress", source, StringComparison.Ordinal);
        Assert.Contains("PartyContact", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Microsoft.AspNetCore", source, StringComparison.Ordinal);
        Assert.DoesNotContain("XDocument", source, StringComparison.Ordinal);
        Assert.DoesNotContain("XmlDocument", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalSigner", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IFiscalTransportGateway", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Party_contract_completes_addresses_and_contacts_without_new_child_routes()
    {
        var contracts = Read("src/WebApi/Controllers/V1/Contracts/PartyCatalogContracts.cs");
        var controller = Read("src/WebApi/Controllers/V1/PartiesController.cs");

        Assert.Contains("PartyAddressRequest", contracts, StringComparison.Ordinal);
        Assert.Contains("PartyContactRequest", contracts, StringComparison.Ordinal);
        Assert.Contains("IReadOnlyCollection<PartyAddressDto> Addresses", contracts, StringComparison.Ordinal);
        Assert.Contains("IReadOnlyCollection<PartyContactDto> Contacts", contracts, StringComparison.Ordinal);
        Assert.DoesNotContain("/addresses", controller, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/contacts", controller, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[HttpPost]", controller, StringComparison.Ordinal);
        Assert.Contains("[HttpPatch(\"{partyId:guid}\")]", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void Party_channel_persistence_has_owned_tables_and_cascade_back_to_party()
    {
        var records = Read("src/Infrastructure/Persistence/V1/Write/Models/PartyChannelRecords.cs");
        var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260908190000_V1PartyAddressContact.cs");

        Assert.Contains("v1_party_addresses", records, StringComparison.Ordinal);
        Assert.Contains("v1_party_contacts", records, StringComparison.Ordinal);
        Assert.Contains("v1_party_addresses", migration, StringComparison.Ordinal);
        Assert.Contains("v1_party_contacts", migration, StringComparison.Ordinal);
        Assert.Contains("ReferentialAction.Cascade", migration, StringComparison.Ordinal);
    }

    [Fact]
    public void Party_repository_does_not_own_transaction_or_flush()
    {
        var repository = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfPartyRepository.cs");

        Assert.DoesNotContain("BeginTransaction", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveChanges", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("ITransactionManager", repository, StringComparison.Ordinal);
        Assert.Contains("Include(x => x.Addresses)", repository, StringComparison.Ordinal);
        Assert.Contains("Include(x => x.Contacts)", repository, StringComparison.Ordinal);
    }

    private static string Read(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "src"))
                && Directory.Exists(Path.Combine(current.FullName, "test")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
