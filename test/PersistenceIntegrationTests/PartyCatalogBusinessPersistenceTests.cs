using System.Data.Common;
using EFactura.Application.Catalog;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Security;
using EFactura.Application.Parties;
using EFactura.Domain.Catalog;
using EFactura.Domain.Common;
using EFactura.Domain.Parties;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class PartyCatalogBusinessPersistenceTests
{
    [Fact]
    public void Service_cannot_track_inventory()
    {
        var exception = Assert.Throws<DomainRuleException>(() => CommercialItem.Create(
            Guid.NewGuid(),
            "company-1",
            "SERV-01",
            "Technical service",
            null,
            CommercialItemKind.Service,
            "UNIT",
            true,
            null,
            null));

        Assert.Equal("catalog.service_inventory_forbidden", exception.Code);
    }

    [Fact]
    public void Party_keeps_residence_tax_residence_and_fiscal_identity_as_separate_facts()
    {
        var identity = PartyFiscalIdentity.Create(Guid.NewGuid(), "7", "AR-20-12345678-9", "AR");
        var party = Party.Create(
            Guid.NewGuid(),
            "company-1",
            PartyKind.Organization,
            "Foreign customer",
            "AR",
            "AR",
            new[] { PartyRole.Customer },
            new[] { identity });

        Assert.Equal("AR", party.ResidenceCountry);
        Assert.Equal("AR", party.TaxResidenceCountry);
        Assert.Equal("AR", party.FiscalIdentities.Single().IssuingCountry);
        Assert.DoesNotContain(party.GetType().GetProperties(), property =>
            string.Equals(property.Name, "IsForeign", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Failure_after_business_flush_rolls_back_party_and_all_atomic_companions(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
        {
            return;
        }

        await using (var context = database.CreateContext())
        {
            var transactions = new EfTransactionManager(context);
            var unitOfWork = new EfUnitOfWork(context);
            var parties = new EfPartyRepository(context);
            var audit = new EfAuditWriter(context);
            var idempotency = new EfIdempotencyStore(context);
            var outbox = new EfOutboxWriter(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => transactions.ExecuteAsync(async ct =>
            {
                var reservation = new IdempotencyReservation(
                    "party.create:company-1",
                    "rollback-party-key",
                    "rollback-party-hash",
                    "actor-1",
                    "corr-business-rollback",
                    DateTimeOffset.UtcNow.AddMinutes(10));

                Assert.Equal(
                    IdempotencyReservationStatus.Acquired,
                    (await idempotency.TryReserveAsync(reservation, ct)).Status);

                await unitOfWork.SaveChangesAsync(ct);

                var identity = PartyFiscalIdentity.Create(Guid.NewGuid(), "7", "AR-20123456789", "AR");
                var party = Party.Create(
                    Guid.NewGuid(),
                    "company-1",
                    PartyKind.Organization,
                    "Rollback customer",
                    "AR",
                    "AR",
                    new[] { PartyRole.Customer },
                    new[] { identity });

                await parties.AddAsync(party, ct);
                await audit.AppendAsync(
                    new AuditEvent(
                        Guid.NewGuid(),
                        DateTimeOffset.UtcNow,
                        "party.created",
                        "actor-1",
                        "company-1",
                        null,
                        null,
                        "Party",
                        party.Id.ToString(),
                        AuditOutcome.Succeeded,
                        "corr-business-rollback",
                        null,
                        new Dictionary<string, string?>()),
                    ct);

                await outbox.EnqueueAsync(
                    new TestBusinessEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, party.Id),
                    new OutboxContext("corr-business-rollback", OrganizationId: "company-1", ActorId: "actor-1"),
                    ct);

                await unitOfWork.SaveChangesAsync(ct);
                throw new InvalidOperationException("Injected failure after business rows were flushed.");
            }));
        }

        await using var verification = database.CreateContext();
        Assert.Equal(0, await verification.Parties.CountAsync());
        Assert.Equal(0, await verification.PartyRoles.CountAsync());
        Assert.Equal(0, await verification.PartyFiscalIdentities.CountAsync());
        Assert.Equal(0, await verification.AuditEvents.CountAsync());
        Assert.Equal(0, await verification.OutboxMessages.CountAsync());
        Assert.Equal(0, await verification.IdempotencyRecords.CountAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Create_use_cases_commit_business_audit_idempotency_and_outbox_together(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
        {
            return;
        }

        Guid partyId;
        await using (var context = database.CreateContext())
        {
            var actor = new FixedActorContextAccessor(new ActorContext(
                "actor-1",
                "Integration Tester",
                true,
                new HashSet<string>(new[] { Permissions.PartiesManage, Permissions.CatalogManage }, StringComparer.Ordinal),
                new HashSet<string>(new[] { "company-1" }, StringComparer.Ordinal),
                new HashSet<string>(StringComparer.Ordinal),
                new HashSet<string>(StringComparer.Ordinal),
                null));
            var correlation = new FixedCorrelationContextAccessor(new CorrelationContext("corr-party-create", "trace-1"));

            var partyUseCase = new CreatePartyUseCase(
                new EfPartyRepository(context),
                new EfTransactionManager(context),
                new EfUnitOfWork(context),
                new EfIdempotencyStore(context),
                new EfAuditWriter(context),
                new EfOutboxWriter(context),
                actor,
                correlation);

            var command = new CreatePartyCommand(
                "company-1",
                PartyKind.Organization,
                "Cliente Exterior SA",
                "AR",
                "AR",
                new[] { PartyRole.Customer },
                new[] { new PartyFiscalIdentityInput("7", "AR-30-12345678-9", "AR") },
                "party-create-key-1",
                "party-request-hash-1");

            var created = await partyUseCase.ExecuteAsync(command);
            Assert.False(created.Replayed);
            partyId = created.PartyId;
        }

        await using (var replayContext = database.CreateContext())
        {
            var actor = new FixedActorContextAccessor(new ActorContext(
                "actor-1",
                "Integration Tester",
                true,
                new HashSet<string>(new[] { Permissions.PartiesManage }, StringComparer.Ordinal),
                new HashSet<string>(new[] { "company-1" }, StringComparer.Ordinal),
                new HashSet<string>(StringComparer.Ordinal),
                new HashSet<string>(StringComparer.Ordinal),
                null));
            var correlation = new FixedCorrelationContextAccessor(new CorrelationContext("corr-party-replay", "trace-2"));
            var useCase = new CreatePartyUseCase(
                new EfPartyRepository(replayContext),
                new EfTransactionManager(replayContext),
                new EfUnitOfWork(replayContext),
                new EfIdempotencyStore(replayContext),
                new EfAuditWriter(replayContext),
                new EfOutboxWriter(replayContext),
                actor,
                correlation);

            var replay = await useCase.ExecuteAsync(new CreatePartyCommand(
                "company-1",
                PartyKind.Organization,
                "Cliente Exterior SA",
                "AR",
                "AR",
                new[] { PartyRole.Customer },
                new[] { new PartyFiscalIdentityInput("7", "AR-30-12345678-9", "AR") },
                "party-create-key-1",
                "party-request-hash-1"));

            Assert.True(replay.Replayed);
            Assert.Equal(partyId, replay.PartyId);
        }

        await using (var itemContext = database.CreateContext())
        {
            var actor = new FixedActorContextAccessor(new ActorContext(
                "actor-1",
                "Integration Tester",
                true,
                new HashSet<string>(new[] { Permissions.CatalogManage }, StringComparer.Ordinal),
                new HashSet<string>(new[] { "company-1" }, StringComparer.Ordinal),
                new HashSet<string>(StringComparer.Ordinal),
                new HashSet<string>(StringComparer.Ordinal),
                null));
            var correlation = new FixedCorrelationContextAccessor(new CorrelationContext("corr-item-create", "trace-3"));

            var itemUseCase = new CreateCommercialItemUseCase(
                new EfCommercialItemRepository(itemContext),
                new EfTransactionManager(itemContext),
                new EfUnitOfWork(itemContext),
                new EfIdempotencyStore(itemContext),
                new EfAuditWriter(itemContext),
                new EfOutboxWriter(itemContext),
                actor,
                correlation);

            var createdItem = await itemUseCase.ExecuteAsync(new CreateCommercialItemCommand(
                "company-1",
                "SERV-001",
                "Asesoramiento técnico",
                "Servicio no inventariable",
                CommercialItemKind.Service,
                "UNIT",
                false,
                null,
                null,
                "item-create-key-1",
                "item-request-hash-1"));

            Assert.False(createdItem.Replayed);
        }

        await using var verification = database.CreateContext();
        Assert.Equal(1, await verification.Parties.CountAsync());
        Assert.Equal(1, await verification.PartyRoles.CountAsync());
        Assert.Equal(1, await verification.PartyFiscalIdentities.CountAsync());
        Assert.Equal(1, await verification.CommercialItems.CountAsync());
        Assert.Equal(2, await verification.AuditEvents.CountAsync());
        Assert.Equal(2, await verification.OutboxMessages.CountAsync());
        Assert.Equal(2, await verification.IdempotencyRecords.CountAsync());
        Assert.All(await verification.IdempotencyRecords.ToListAsync(), row => Assert.Equal(1, row.State));
    }

    private sealed record TestBusinessEvent(
        Guid EventId,
        DateTimeOffset OccurredAt,
        Guid PartyId) : IIntegrationEvent;

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
            connectionBuilder["Database"] = $"efactura_pc_{provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";

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
