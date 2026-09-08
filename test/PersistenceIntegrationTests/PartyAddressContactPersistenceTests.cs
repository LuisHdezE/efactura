using EFactura.Domain.Parties;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Write.Repositories;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class PartyAddressContactPersistenceTests
{
    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Address_and_contact_round_trip_and_atomic_replacement_are_portable(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
        {
            return;
        }

        var partyId = Guid.NewGuid();
        var originalAddressId = Guid.NewGuid();
        var originalContactId = Guid.NewGuid();

        await using (var context = database.CreateContext())
        {
            var party = Party.Create(
                partyId,
                "company-1",
                PartyKind.Organization,
                "Cliente Persistido SA",
                "UY",
                "UY",
                new[] { PartyRole.Customer },
                addresses: new[]
                {
                    PartyAddress.Create(
                        originalAddressId,
                        PartyAddressKind.Fiscal,
                        "Av. Italia 1234",
                        "Montevideo",
                        "Montevideo",
                        "UY",
                        "11600",
                        true)
                },
                contacts: new[]
                {
                    PartyContact.Create(originalContactId, "EMAIL", "facturas@example.com", true)
                });

            await new EfPartyRepository(context).AddAsync(party);
            await context.SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var loaded = await new EfPartyRepository(context).GetAsync("company-1", partyId)
                ?? throw new InvalidOperationException("Party was not persisted.");

            Assert.Equal(1, loaded.Version);
            var address = Assert.Single(loaded.Addresses);
            Assert.Equal(originalAddressId, address.Id);
            Assert.Equal(PartyAddressKind.Fiscal, address.Kind);
            Assert.Equal("11600", address.PostalCode);
            var contact = Assert.Single(loaded.Contacts);
            Assert.Equal(originalContactId, contact.Id);
            Assert.Equal("facturas@example.com", contact.Value);
        }

        var replacementAddressId = Guid.NewGuid();
        var replacementContactId = Guid.NewGuid();

        await using (var context = database.CreateContext())
        {
            var repository = new EfPartyRepository(context);
            var loaded = await repository.GetAsync("company-1", partyId)
                ?? throw new InvalidOperationException("Party missing before update.");

            loaded.UpdateMasterData(
                PartyKind.Organization,
                "Cliente Persistido Actualizado SA",
                "UY",
                "UY",
                new[]
                {
                    PartyAddress.Create(
                        replacementAddressId,
                        PartyAddressKind.Fiscal,
                        "18 de Julio 2000",
                        "Montevideo",
                        "Montevideo",
                        "UY",
                        "11200",
                        true)
                },
                new[]
                {
                    PartyContact.Create(replacementContactId, "PHONE", "+59829000000", true)
                },
                1);

            await repository.SaveAsync(loaded);
            await context.SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var loaded = await new EfPartyRepository(context).GetAsync("company-1", partyId)
                ?? throw new InvalidOperationException("Party missing after update.");

            Assert.Equal(2, loaded.Version);
            Assert.Equal("Cliente Persistido Actualizado SA", loaded.Name);

            var address = Assert.Single(loaded.Addresses);
            Assert.Equal(replacementAddressId, address.Id);
            Assert.Equal("18 de Julio 2000", address.AddressLine);
            Assert.DoesNotContain(loaded.Addresses, x => x.Id == originalAddressId);

            var contact = Assert.Single(loaded.Contacts);
            Assert.Equal(replacementContactId, contact.Id);
            Assert.Equal("PHONE", contact.TypeCode);
            Assert.DoesNotContain(loaded.Contacts, x => x.Id == originalContactId);
        }
    }
}
