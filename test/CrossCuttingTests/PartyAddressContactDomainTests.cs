using EFactura.Domain.Common;
using EFactura.Domain.Parties;
using Xunit;

namespace CrossCuttingTests;

public sealed class PartyAddressContactDomainTests
{
    [Fact]
    public void Create_preserves_and_normalizes_address_and_contact_master_data()
    {
        var party = Party.Create(
            Guid.NewGuid(),
            "company-1",
            PartyKind.Organization,
            "Cliente SA",
            "UY",
            "UY",
            new[] { PartyRole.Customer },
            addresses: new[]
            {
                PartyAddress.Create(
                    Guid.NewGuid(),
                    PartyAddressKind.Fiscal,
                    "  Av. Italia 1234  ",
                    " Montevideo ",
                    " Montevideo ",
                    "uy",
                    "11600",
                    true)
            },
            contacts: new[]
            {
                PartyContact.Create(Guid.NewGuid(), " EMAIL ", " facturas@example.com ", true)
            });

        var address = Assert.Single(party.Addresses);
        Assert.Equal("Av. Italia 1234", address.AddressLine);
        Assert.Equal("Montevideo", address.City);
        Assert.Equal("Montevideo", address.Region);
        Assert.Equal("UY", address.CountryCode);
        Assert.Equal("11600", address.PostalCode);
        Assert.True(address.Primary);

        var contact = Assert.Single(party.Contacts);
        Assert.Equal("EMAIL", contact.TypeCode);
        Assert.Equal("facturas@example.com", contact.Value);
        Assert.True(contact.Primary);
    }

    [Fact]
    public void Two_primary_addresses_of_same_kind_are_rejected()
    {
        var ex = Assert.Throws<DomainRuleException>(() => Party.Create(
            Guid.NewGuid(),
            "company-1",
            PartyKind.Organization,
            "Cliente SA",
            "UY",
            "UY",
            new[] { PartyRole.Customer },
            addresses: new[]
            {
                PartyAddress.Create(Guid.NewGuid(), PartyAddressKind.Fiscal, "Calle 1", "Montevideo", null, "UY", null, true),
                PartyAddress.Create(Guid.NewGuid(), PartyAddressKind.Fiscal, "Calle 2", "Montevideo", null, "UY", null, true)
            }));

        Assert.Equal("party.address.multiple_primary", ex.Code);
    }

    [Fact]
    public void Duplicate_contacts_are_rejected_by_normalized_type_and_value()
    {
        var ex = Assert.Throws<DomainRuleException>(() => Party.Create(
            Guid.NewGuid(),
            "company-1",
            PartyKind.Organization,
            "Cliente SA",
            "UY",
            "UY",
            new[] { PartyRole.Customer },
            contacts: new[]
            {
                PartyContact.Create(Guid.NewGuid(), "EMAIL", "FACTURAS@example.com", false),
                PartyContact.Create(Guid.NewGuid(), "email", "facturas@EXAMPLE.com", false)
            }));

        Assert.Equal("party.contact.duplicate", ex.Code);
    }

    [Fact]
    public void Combined_master_update_replaces_channels_and_increments_version_once()
    {
        var party = Party.Create(
            Guid.NewGuid(),
            "company-1",
            PartyKind.Organization,
            "Cliente SA",
            "UY",
            "UY",
            new[] { PartyRole.Customer },
            addresses: new[]
            {
                PartyAddress.Create(Guid.NewGuid(), PartyAddressKind.Fiscal, "Vieja 1", "Montevideo", null, "UY", null, true)
            },
            contacts: new[]
            {
                PartyContact.Create(Guid.NewGuid(), "PHONE", "111", true)
            });

        party.UpdateMasterData(
            PartyKind.Organization,
            "Cliente Nuevo SA",
            "UY",
            "UY",
            new[]
            {
                PartyAddress.Create(Guid.NewGuid(), PartyAddressKind.Fiscal, "Nueva 2", "Montevideo", "Montevideo", "UY", "11000", true),
                PartyAddress.Create(Guid.NewGuid(), PartyAddressKind.Delivery, "Depósito 3", "Canelones", "Canelones", "UY", null, true)
            },
            new[]
            {
                PartyContact.Create(Guid.NewGuid(), "EMAIL", "nuevo@example.com", true)
            },
            1);

        Assert.Equal(2, party.Version);
        Assert.Equal("Cliente Nuevo SA", party.Name);
        Assert.Equal(2, party.Addresses.Count);
        Assert.DoesNotContain(party.Addresses, x => x.AddressLine == "Vieja 1");
        Assert.Single(party.Contacts);
        Assert.Equal("nuevo@example.com", party.Contacts.Single().Value);
    }
}
