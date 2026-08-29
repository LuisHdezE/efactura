using System.Data.Common;
using EFactura.Application.Catalog;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Security;
using EFactura.Application.Parties;
using EFactura.Domain.Catalog;
using EFactura.Domain.Parties;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class PartyCatalogMaintenancePersistenceTests
{
    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Maintenance_use_cases_commit_business_audit_idempotency_and_outbox_together(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
        {
            return;
        }

        var actor = CreateActor();
        var correlation = new FixedCorrelationContextAccessor(new CorrelationContext("corr-maintenance", "trace-maintenance"));

        Guid partyId;
        Guid categoryId;
        Guid itemId;

        await using (var context = database.CreateContext())
        {
            var parties = new EfPartyRepository(context);
            var items = new EfCommercialItemRepository(context);
            var categories = new EfItemCategoryRepository(context);
            var transactions = new EfTransactionManager(context);
            var unitOfWork = new EfUnitOfWork(context);
            var idempotency = new EfIdempotencyStore(context);
            var audit = new EfAuditWriter(context);
            var outbox = new EfOutboxWriter(context);

            var createParty = new CreatePartyUseCase(
                parties,
                transactions,
                unitOfWork,
                idempotency,
                audit,
                outbox,
                actor,
                correlation);

            var createdParty = await createParty.ExecuteAsync(new CreatePartyCommand(
                "company-1",
                PartyKind.Organization,
                "Cliente Inicial SA",
                "UY",
                "UY",
                new[] { PartyRole.Customer },
                Array.Empty<PartyFiscalIdentityInput>(),
                "party-create-maintenance",
                "hash-party-create-maintenance"));
            partyId = createdParty.PartyId;

            var partyWorkflow = new PartyMutationWorkflow(
                parties,
                parties,
                transactions,
                unitOfWork,
                idempotency,
                audit,
                outbox,
                actor,
                correlation);

            var updateParty = new UpdatePartyUseCase(partyWorkflow);
            var updatedParty = await updateParty.ExecuteAsync(new UpdatePartyCommand(
                "company-1",
                partyId,
                null,
                "Cliente Actualizado SA",
                "AR",
                "UY",
                1,
                "party-update-maintenance",
                "hash-party-update-maintenance"));
            Assert.Equal(2, updatedParty.Version);

            var addIdentity = new AddPartyFiscalIdentityUseCase(partyWorkflow, parties);
            var identityAdded = await addIdentity.ExecuteAsync(new AddPartyFiscalIdentityCommand(
                "company-1",
                partyId,
                new PartyFiscalIdentityInput("7", "AR-30-11111111-1", "AR"),
                2,
                "party-identity-add-maintenance",
                "hash-party-identity-add-maintenance"));
            Assert.Equal(3, identityAdded.Version);

            var setRoles = new SetPartyRolesUseCase(partyWorkflow);
            var rolesChanged = await setRoles.ExecuteAsync(new SetPartyRolesCommand(
                "company-1",
                partyId,
                new[] { PartyRole.Customer, PartyRole.Supplier },
                3,
                "party-roles-maintenance",
                "hash-party-roles-maintenance"));
            Assert.Equal(4, rolesChanged.Version);

            var partyAfterIdentity = await parties.GetAsync("company-1", partyId)
                ?? throw new InvalidOperationException("Party missing during test.");
            var identityId = partyAfterIdentity.FiscalIdentities.Single().Id;

            var updateIdentity = new UpdatePartyFiscalIdentityUseCase(partyWorkflow, parties);
            var identityUpdated = await updateIdentity.ExecuteAsync(new UpdatePartyFiscalIdentityCommand(
                "company-1",
                partyId,
                identityId,
                new PartyFiscalIdentityInput("7", "AR-30-22222222-2", "AR"),
                true,
                4,
                "party-identity-update-maintenance",
                "hash-party-identity-update-maintenance"));
            Assert.Equal(5, identityUpdated.Version);

            var createCategory = new CreateItemCategoryUseCase(
                categories,
                transactions,
                unitOfWork,
                idempotency,
                audit,
                outbox,
                actor,
                correlation);
            var category = await createCategory.ExecuteAsync(new CreateItemCategoryCommand(
                "company-1",
                "SERV",
                "Servicios",
                "category-create-maintenance",
                "hash-category-create-maintenance"));
            categoryId = category.ResourceId;

            var updateCategory = new UpdateItemCategoryUseCase(
                categories,
                transactions,
                unitOfWork,
                idempotency,
                audit,
                outbox,
                actor,
                correlation);
            var categoryUpdated = await updateCategory.ExecuteAsync(new UpdateItemCategoryCommand(
                "company-1",
                categoryId,
                null,
                "Servicios profesionales",
                true,
                1,
                "category-update-maintenance",
                "hash-category-update-maintenance"));
            Assert.Equal(2, categoryUpdated.Version);

            var createItem = new CreateCommercialItemUseCase(
                items,
                transactions,
                unitOfWork,
                idempotency,
                audit,
                outbox,
                actor,
                correlation,
                categories);
            var item = await createItem.ExecuteAsync(new CreateCommercialItemCommand(
                "company-1",
                "SERV-100",
                "Servicio inicial",
                null,
                CommercialItemKind.Service,
                "UNIT",
                false,
                null,
                categoryId,
                "item-create-maintenance",
                "hash-item-create-maintenance"));
            itemId = item.ItemId;

            var itemWorkflow = new CatalogItemMutationWorkflow(
                items,
                items,
                transactions,
                unitOfWork,
                idempotency,
                audit,
                outbox,
                actor,
                correlation);
            var updateItem = new TaxSafeUpdateCommercialItemUseCase(
                new UpdateCommercialItemUseCase(itemWorkflow, items, categories));
            var itemUpdated = await updateItem.ExecuteAsync(new UpdateCommercialItemCommand(
                "company-1",
                itemId,
                "SERV-101",
                "Servicio actualizado",
                "Descripción actualizada",
                CommercialItemKind.Service,
                "UNIT",
                false,
                null,
                false,
                categoryId,
                true,
                1,
                "item-update-maintenance",
                "hash-item-update-maintenance"));
            Assert.Equal(2, itemUpdated.Version);

            var deactivateItem = new DeactivateCommercialItemUseCase(itemWorkflow);
            var deactivated = await deactivateItem.ExecuteAsync(new DeactivateCommercialItemCommand(
                "company-1",
                itemId,
                2,
                "item-deactivate-maintenance",
                "hash-item-deactivate-maintenance"));
            Assert.Equal(3, deactivated.Version);
        }

        await using var verification = database.CreateContext();
        var partyRow = await verification.Parties.SingleAsync(x => x.Id == partyId);
        Assert.Equal("Cliente Actualizado SA", partyRow.Name);
        Assert.Equal("AR", partyRow.ResidenceCountry);
        Assert.Equal("UY", partyRow.TaxResidenceCountry);
        Assert.Equal(5, partyRow.Version);
        Assert.Equal(2, await verification.PartyRoles.CountAsync(x => x.PartyId == partyId));
        Assert.Equal("AR-30-22222222-2", (await verification.PartyFiscalIdentities.SingleAsync(x => x.PartyId == partyId)).Number);

        var categoryRow = await verification.ItemCategories.SingleAsync(x => x.Id == categoryId);
        Assert.Equal("Servicios profesionales", categoryRow.Name);
        Assert.Equal(2, categoryRow.Version);

        var itemRow = await verification.CommercialItems.SingleAsync(x => x.Id == itemId);
        Assert.Equal("SERV-101", itemRow.Code);
        Assert.False(itemRow.Active);
        Assert.Equal(3, itemRow.Version);
        Assert.Equal(categoryId, itemRow.CategoryId);

        Assert.Equal(10, await verification.AuditEvents.CountAsync());
        Assert.Equal(10, await verification.OutboxMessages.CountAsync());
        Assert.Equal(10, await verification.IdempotencyRecords.CountAsync());
        Assert.All(await verification.IdempotencyRecords.ToListAsync(), row => Assert.Equal(1, row.State));
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Stale_party_update_rolls_back_idempotency_and_does_not_emit_success_evidence(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
        {
            return;
        }

        var actor = CreateActor();
        var correlation = new FixedCorrelationContextAccessor(new CorrelationContext("corr-stale", "trace-stale"));
        Guid partyId;

        await using (var context = database.CreateContext())
        {
            var parties = new EfPartyRepository(context);
            var create = new CreatePartyUseCase(
                parties,
                new EfTransactionManager(context),
                new EfUnitOfWork(context),
                new EfIdempotencyStore(context),
                new EfAuditWriter(context),
                new EfOutboxWriter(context),
                actor,
                correlation);

            partyId = (await create.ExecuteAsync(new CreatePartyCommand(
                "company-1",
                PartyKind.Person,
                "Cliente concurrente",
                "UY",
                "UY",
                new[] { PartyRole.Customer },
                Array.Empty<PartyFiscalIdentityInput>(),
                "stale-seed",
                "stale-seed-hash"))).PartyId;
        }

        await using (var context = database.CreateContext())
        {
            var parties = new EfPartyRepository(context);
            var workflow = new PartyMutationWorkflow(
                parties,
                parties,
                new EfTransactionManager(context),
                new EfUnitOfWork(context),
                new EfIdempotencyStore(context),
                new EfAuditWriter(context),
                new EfOutboxWriter(context),
                actor,
                correlation);
            var update = new UpdatePartyUseCase(workflow);

            var exception = await Assert.ThrowsAsync<ApplicationProblemException>(() => update.ExecuteAsync(new UpdatePartyCommand(
                "company-1",
                partyId,
                null,
                "No debe persistir",
                null,
                null,
                0,
                "stale-attempt",
                "stale-attempt-hash")));

            Assert.Equal("concurrency_conflict", exception.Code);
        }

        await using var verification = database.CreateContext();
        Assert.Equal("Cliente concurrente", (await verification.Parties.SingleAsync(x => x.Id == partyId)).Name);
        Assert.Equal(1, await verification.AuditEvents.CountAsync());
        Assert.Equal(1, await verification.OutboxMessages.CountAsync());
        Assert.Equal(1, await verification.IdempotencyRecords.CountAsync());
        Assert.DoesNotContain(await verification.IdempotencyRecords.ToListAsync(), x => x.Scope.StartsWith("party.update", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Failure_after_updated_party_flush_rolls_back_business_row_and_companion_evidence(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
        {
            return;
        }

        Guid partyId;
        await using (var seed = database.CreateContext())
        {
            var party = Party.Create(
                Guid.NewGuid(),
                "company-1",
                PartyKind.Organization,
                "Original",
                "UY",
                "UY",
                new[] { PartyRole.Customer },
                Array.Empty<PartyFiscalIdentity>());
            partyId = party.Id;
            await new EfPartyRepository(seed).AddAsync(party);
            await seed.SaveChangesAsync();
        }

        await using (var context = database.CreateContext())
        {
            var transactions = new EfTransactionManager(context);
            var unitOfWork = new EfUnitOfWork(context);
            var parties = new EfPartyRepository(context);
            var audit = new EfAuditWriter(context);
            var outbox = new EfOutboxWriter(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => transactions.ExecuteAsync(async ct =>
            {
                var party = await parties.GetAsync("company-1", partyId, ct)
                    ?? throw new InvalidOperationException("Seed party missing.");
                party.UpdateMasterData(party.Kind, "Changed inside rolled back transaction", "AR", "AR", 1);
                await parties.SaveAsync(party, ct);
                await audit.AppendAsync(
                    new AuditEvent(
                        Guid.NewGuid(),
                        DateTimeOffset.UtcNow,
                        "party.updated",
                        "actor-1",
                        "company-1",
                        null,
                        null,
                        "Party",
                        partyId.ToString(),
                        AuditOutcome.Succeeded,
                        "corr-rollback-update",
                        null,
                        new Dictionary<string, string?>()),
                    ct);
                await outbox.EnqueueAsync(
                    new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, partyId),
                    new OutboxContext("corr-rollback-update", OrganizationId: "company-1", ActorId: "actor-1"),
                    ct);
                await unitOfWork.SaveChangesAsync(ct);
                throw new InvalidOperationException("Injected failure after updated business row flush.");
            }));
        }

        await using var verification = database.CreateContext();
        var row = await verification.Parties.SingleAsync(x => x.Id == partyId);
        Assert.Equal("Original", row.Name);
        Assert.Equal("UY", row.ResidenceCountry);
        Assert.Equal(1, row.Version);
        Assert.Equal(0, await verification.AuditEvents.CountAsync());
        Assert.Equal(0, await verification.OutboxMessages.CountAsync());
    }

    private static FixedActorContextAccessor CreateActor() =>
        new(new ActorContext(
            "actor-maintenance",
            "Maintenance Tester",
            true,
            new HashSet<string>(new[]
            {
                Permissions.PartiesRead,
                Permissions.PartiesManage,
                Permissions.PartiesFiscalManage,
                Permissions.CatalogRead,
                Permissions.CatalogManage
            }, StringComparer.Ordinal),
            new HashSet<string>(new[] { "company-1" }, StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            new HashSet<string>(StringComparer.Ordinal),
            null));

    private sealed record TestEvent(Guid EventId, DateTimeOffset OccurredAt, Guid PartyId) : IIntegrationEvent;

    private sealed class FixedActorContextAccessor : IActorContextAccessor
    {
        public FixedActorContextAccessor(ActorContext current) => Current = current;
        public ActorContext Current { get; }
    }

    private sealed class FixedCorrelationContextAccessor : ICorrelationContextAccessor
    {
        public FixedCorrelationContextAccessor(CorrelationContext current) => Current = current;
        public CorrelationContext Current { get; }
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly DbContextOptions<V1PersistenceDbContext> _options;

        private TestDatabase(DbContextOptions<V1PersistenceDbContext> options)
        {
            _options = options;
        }

        public static async Task<TestDatabase?> CreateAsync(V1DatabaseProvider provider)
        {
            var variable = provider == V1DatabaseProvider.PostgreSql
                ? "POSTGRES_TEST_CONNECTION"
                : "MYSQL_TEST_CONNECTION";

            var baseConnectionString = Environment.GetEnvironmentVariable(variable);
            if (string.IsNullOrWhiteSpace(baseConnectionString))
            {
                if (string.Equals(
                    Environment.GetEnvironmentVariable("PERSISTENCE_INTEGRATION_REQUIRED"),
                    "true",
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Required integration test connection variable {variable} is missing.");
                }

                return null;
            }

            var connectionBuilder = new DbConnectionStringBuilder { ConnectionString = baseConnectionString };
            connectionBuilder["Database"] = $"efactura_pc_maint_{provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";

            var optionsBuilder = new DbContextOptionsBuilder<V1PersistenceDbContext>();
            V1PersistenceDatabaseConfigurator.Configure(optionsBuilder, provider, connectionBuilder.ConnectionString);

            var database = new TestDatabase(optionsBuilder.Options);
            await using var context = database.CreateContext();
            await context.Database.MigrateAsync();
            return database;
        }

        public V1PersistenceDbContext CreateContext() => new(_options);

        public async ValueTask DisposeAsync()
        {
            await using var context = CreateContext();
            await context.Database.EnsureDeletedAsync();
        }
    }
}
