using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Security;
using EFactura.Application.Organizations;
using EFactura.Domain.Organizations;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Models;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class W15TerminalUseCasePersistenceTests
{
    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Register_update_replay_and_location_dependency_are_provider_equivalent(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider); if (database is null) return;
        await SeedAsync(database);

        var actor = new FixedActorContextAccessor(new ActorContext(
            "w15-admin",
            "W1.5 Persistence Tester",
            true,
            new HashSet<string>(new[] { Permissions.OrganizationRead, Permissions.OrganizationManage }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "company-1" }, StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            null));
        var correlation = new FixedCorrelationContextAccessor(new CorrelationContext("corr-w15", "trace-w15"));

        string terminalId;
        await using (var context = database.CreateContext())
        {
            var terminals = new EfTerminalRepository(context);
            var locations = new EfOrganizationRepository(context);
            var create = RegisterUseCase(context, terminals, locations, actor, correlation);

            var created = await create.ExecuteAsync(new RegisterTerminalCommand(
                "company-1", " pos-01 ", "Caja Uno", "loc-1", "w15-create", "hash-create"));

            Assert.False(created.Replayed);
            Assert.Equal(1, created.Version);
            terminalId = created.ResourceId;

            var persisted = await terminals.GetAsync("company-1", terminalId);
            Assert.NotNull(persisted);
            Assert.Equal("POS-01", persisted!.Code);
            Assert.True(persisted.Active);
        }

        await using (var context = database.CreateContext())
        {
            var terminals = new EfTerminalRepository(context);
            var locations = new EfOrganizationRepository(context);
            var create = RegisterUseCase(context, terminals, locations, actor, correlation);

            var replayed = await create.ExecuteAsync(new RegisterTerminalCommand(
                "company-1", "pos-01", "Caja Uno", "loc-1", "w15-create", "hash-create"));

            Assert.True(replayed.Replayed);
            Assert.Equal(terminalId, replayed.ResourceId);
            Assert.Equal(1, replayed.Version);
        }

        await using (var context = database.CreateContext())
        {
            var terminals = new EfTerminalRepository(context);
            var locations = new EfOrganizationRepository(context);
            var update = UpdateUseCase(context, terminals, locations, actor, correlation);

            var updated = await update.ExecuteAsync(new UpdateTerminalCommand(
                "company-1", terminalId, "Caja Principal", "loc-2", true, 1, "w15-update", "hash-update"));

            Assert.False(updated.Replayed);
            Assert.Equal(2, updated.Version);

            var moved = await terminals.GetAsync("company-1", terminalId);
            Assert.NotNull(moved);
            Assert.Equal("loc-2", moved!.LocationId);
            Assert.True(moved.Active);
            Assert.Equal("POS-01", moved.Code);
        }

        await using (var context = database.CreateContext())
        {
            var terminals = new EfTerminalRepository(context);
            var locations = new EfOrganizationRepository(context);
            var locationUpdate = LocationUpdateUseCase(context, terminals, locations, actor, correlation);
            var location = (await locations.GetAsync("company-1", "loc-2"))!;

            var blocked = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
                locationUpdate.ExecuteAsync(new UpdateFiscalLocationCommand(
                    "company-1", "loc-2", location.Name, location.DgiBranchCode, location.FiscalAddress,
                    location.City, location.Department, false, location.Version, "w15-location-off", "hash-location-off")));

            Assert.Equal("organization.location.active_terminals_exist", blocked.Code);
            Assert.Equal("active_terminal_dependency", blocked.ConflictType);
        }

        await using (var context = database.CreateContext())
        {
            var terminals = new EfTerminalRepository(context);
            var locations = new EfOrganizationRepository(context);
            var update = UpdateUseCase(context, terminals, locations, actor, correlation);

            var deactivated = await update.ExecuteAsync(new UpdateTerminalCommand(
                "company-1", terminalId, "Caja Principal", "loc-2", false, 2, "w15-terminal-off", "hash-terminal-off"));
            Assert.Equal(3, deactivated.Version);
        }

        await using (var context = database.CreateContext())
        {
            var terminals = new EfTerminalRepository(context);
            var locations = new EfOrganizationRepository(context);
            var locationUpdate = LocationUpdateUseCase(context, terminals, locations, actor, correlation);
            var location = (await locations.GetAsync("company-1", "loc-2"))!;

            var deactivated = await locationUpdate.ExecuteAsync(new UpdateFiscalLocationCommand(
                "company-1", "loc-2", location.Name, location.DgiBranchCode, location.FiscalAddress,
                location.City, location.Department, false, location.Version, "w15-location-off-2", "hash-location-off-2"));

            Assert.False(deactivated.Replayed);
        }

        await using (var context = database.CreateContext())
        {
            var terminals = new EfTerminalRepository(context);
            var locations = new EfOrganizationRepository(context);
            var create = RegisterUseCase(context, terminals, locations, actor, correlation);

            var inactiveLocation = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
                create.ExecuteAsync(new RegisterTerminalCommand(
                    "company-1", "POS-02", "Caja Dos", "loc-inactive", "w15-create-inactive", "hash-create-inactive")));

            Assert.Equal("organization.terminal.location_inactive", inactiveLocation.Code);
            Assert.Equal("inactive_location", inactiveLocation.ConflictType);
        }

        await using (var context = database.CreateContext())
        {
            var terminalAudits = await context.Set<V1AuditEventRecord>()
                .AsNoTracking()
                .Where(x => x.OrganizationId == "company-1" && x.TargetType == "Terminal" && x.Outcome == 1)
                .ToArrayAsync();
            var terminalOutbox = await context.Set<V1OutboxMessageRecord>()
                .AsNoTracking()
                .Where(x => x.OrganizationId == "company-1" && x.EventType.Contains("TerminalChangedIntegrationEvent"))
                .ToArrayAsync();
            var terminalIdempotency = await context.Set<V1IdempotencyRecord>()
                .AsNoTracking()
                .Where(x => x.Scope.StartsWith("organization.terminal."))
                .ToArrayAsync();

            Assert.Equal(3, terminalAudits.Length);
            Assert.Equal(3, terminalOutbox.Length);
            Assert.Equal(3, terminalIdempotency.Count(x => x.CompletedAtUtc != null));
        }
    }

    private static async Task SeedAsync(TestDatabase database)
    {
        await using var context = database.CreateContext();
        var organizations = new EfOrganizationRepository(context);
        await organizations.AddAsync(CompanyFiscalProfile.Create("company-1", "211234560019", "Empresa de Prueba S.A.", null));
        await organizations.AddAsync(Location("loc-1", "0001"));
        await organizations.AddAsync(Location("loc-2", "0002"));
        var inactive = Location("loc-inactive", "0003");
        inactive.Update(inactive.Name, inactive.DgiBranchCode, inactive.FiscalAddress, inactive.City, inactive.Department, false, 1);
        await organizations.AddAsync(inactive);
        await new EfUnitOfWork(context).SaveChangesAsync();
    }

    private static FiscalLocation Location(string id, string branch) =>
        FiscalLocation.Create(id, "company-1", id, branch, "Test 1", "Montevideo", "Montevideo");

    private static RegisterTerminalUseCase RegisterUseCase(
        V1PersistenceDbContext context,
        EfTerminalRepository terminals,
        EfOrganizationRepository locations,
        IActorContextAccessor actor,
        ICorrelationContextAccessor correlation) =>
        new(terminals, locations, new EfTransactionManager(context), new EfUnitOfWork(context),
            new EfIdempotencyStore(context), new EfAuditWriter(context), new EfOutboxWriter(context), actor, correlation);

    private static UpdateTerminalUseCase UpdateUseCase(
        V1PersistenceDbContext context,
        EfTerminalRepository terminals,
        EfOrganizationRepository locations,
        IActorContextAccessor actor,
        ICorrelationContextAccessor correlation) =>
        new(terminals, locations, new EfTransactionManager(context), new EfUnitOfWork(context),
            new EfIdempotencyStore(context), new EfAuditWriter(context), new EfOutboxWriter(context), actor, correlation);

    private static UpdateFiscalLocationUseCase LocationUpdateUseCase(
        V1PersistenceDbContext context,
        EfTerminalRepository terminals,
        EfOrganizationRepository locations,
        IActorContextAccessor actor,
        ICorrelationContextAccessor correlation) =>
        new(locations, terminals, new EfTransactionManager(context), new EfUnitOfWork(context),
            new EfIdempotencyStore(context), new EfAuditWriter(context), new EfOutboxWriter(context), actor, correlation);

    private sealed class FixedActorContextAccessor : IActorContextAccessor
    {
        public FixedActorContextAccessor(ActorContext current) => Current = current;
        public ActorContext Current { get; }
    }
}
