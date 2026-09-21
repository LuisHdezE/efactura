using EFactura.Application.Common.Errors;
using EFactura.Domain.Organizations;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write.Repositories;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class TerminalPersistenceTests
{
    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Terminal_round_trips_with_normalized_code_and_location_filter(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider); if (database is null) return;
        await SeedLocationAsync(database);

        await using (var context = database.CreateContext())
        {
            var terminals = new EfTerminalRepository(context);
            await terminals.AddAsync(Terminal.Create("terminal-1", "company-1", " pos-01 ", "Caja 1", "loc-1"));
            await new EfUnitOfWork(context).SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var terminals = new EfTerminalRepository(context);
            var terminal = await terminals.GetAsync("company-1", "terminal-1");
            Assert.NotNull(terminal);
            Assert.Equal("POS-01", terminal!.Code);
            Assert.Equal("Caja 1", terminal.Name);
            Assert.Equal("loc-1", terminal.LocationId);
            Assert.True(terminal.Active);
            Assert.Equal(1, terminal.Version);

            var list = await terminals.ListAsync("company-1", true, "loc-1");
            Assert.Single(list);
            Assert.Equal("terminal-1", list.Single().Id);
            Assert.True(await terminals.HasActiveAtLocationAsync("company-1", "loc-1"));
        }
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Duplicate_normalized_terminal_code_is_rejected_portably(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider); if (database is null) return;
        await SeedLocationAsync(database);

        await using (var seedContext = database.CreateContext())
        {
            var terminals = new EfTerminalRepository(seedContext);
            await terminals.AddAsync(Terminal.Create("terminal-1", "company-1", "pos-01", "Caja 1", "loc-1"));
            await new EfUnitOfWork(seedContext).SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var terminals = new EfTerminalRepository(context);
            await terminals.AddAsync(Terminal.Create("terminal-2", "company-1", "POS-01", "Caja 2", "loc-1"));
            var error = await Assert.ThrowsAsync<ApplicationProblemException>(() => new EfUnitOfWork(context).SaveChangesAsync());
            Assert.Equal(ApplicationProblemKind.Conflict, error.Kind);
            Assert.Equal("organization.terminal.code_duplicate", error.Code);
            Assert.Equal("duplicate_terminal_code", error.ConflictType);
        }
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Stale_terminal_update_is_rejected_without_overwriting_newer_state(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider); if (database is null) return;
        await SeedLocationAsync(database);

        await using (var seedContext = database.CreateContext())
        {
            var terminals = new EfTerminalRepository(seedContext);
            await terminals.AddAsync(Terminal.Create("terminal-1", "company-1", "POS-01", "Caja 1", "loc-1"));
            await new EfUnitOfWork(seedContext).SaveChangesAsync();
        }

        Terminal first;
        Terminal second;
        await using (var firstRead = database.CreateContext())
        await using (var secondRead = database.CreateContext())
        {
            first = (await new EfTerminalRepository(firstRead).GetAsync("company-1", "terminal-1"))!;
            second = (await new EfTerminalRepository(secondRead).GetAsync("company-1", "terminal-1"))!;
        }

        first.Update("Caja Uno", "loc-1", true, 1);
        second.Update("Caja Dos", "loc-1", true, 1);

        await using (var firstWrite = database.CreateContext())
        {
            var terminals = new EfTerminalRepository(firstWrite);
            await terminals.SaveAsync(first);
            await new EfUnitOfWork(firstWrite).SaveChangesAsync();
        }

        await using (var secondWrite = database.CreateContext())
        {
            var terminals = new EfTerminalRepository(secondWrite);
            var error = await Assert.ThrowsAsync<ApplicationProblemException>(() => terminals.SaveAsync(second));
            Assert.Equal("concurrency_conflict", error.Code);
            Assert.Equal("stale_version", error.ConflictType);
            Assert.Equal("2", error.CurrentVersion);
        }

        await using (var verify = database.CreateContext())
        {
            var terminal = await new EfTerminalRepository(verify).GetAsync("company-1", "terminal-1");
            Assert.NotNull(terminal);
            Assert.Equal("Caja Uno", terminal!.Name);
            Assert.Equal(2, terminal.Version);
        }
    }

    private static async Task SeedLocationAsync(TestDatabase database)
    {
        await using var context = database.CreateContext();
        var organizations = new EfOrganizationRepository(context);
        await organizations.AddAsync(CompanyFiscalProfile.Create("company-1", "211234560019", "Empresa de Prueba S.A.", null));
        await organizations.AddAsync(FiscalLocation.Create("loc-1", "company-1", "Casa Central", "0001", "Av. 18 de Julio 1234", "Montevideo", "Montevideo"));
        await new EfUnitOfWork(context).SaveChangesAsync();
    }
}
