using System.Data.Common;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using Infrastructure.Persistence.V1;
using Infrastructure.Persistence.V1.Transactions;
using Infrastructure.Persistence.V1.Write;
using Infrastructure.Persistence.V1.Write.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class TransactionalPersistenceTests
{
    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Failure_after_flush_rolls_back_all_cross_cutting_state(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
        {
            return;
        }

        await using (var context = database.CreateContext())
        {
            var transactionManager = new EfTransactionManager(context);
            var unitOfWork = new EfUnitOfWork(context);
            var audit = new EfAuditWriter(context);
            var idempotency = new EfIdempotencyStore(context);
            var outbox = new EfOutboxWriter(context);
            var inbox = new EfInboxStore(context);

            await Assert.ThrowsAsync<InvalidOperationException>(() => transactionManager.ExecuteAsync(async ct =>
            {
                var reservation = CreateIdempotencyReservation();
                Assert.Equal(
                    IdempotencyReservationStatus.Acquired,
                    (await idempotency.TryReserveAsync(reservation, ct)).Status);

                Assert.Equal(
                    InboxReservationStatus.Acquired,
                    (await inbox.TryReserveAsync(CreateInboxReservation(), ct)).Status);

                await audit.AppendAsync(CreateAuditEvent(), ct);
                await outbox.EnqueueAsync(CreateIntegrationEvent(), CreateOutboxContext(), ct);

                await unitOfWork.SaveChangesAsync(ct);

                throw new InvalidOperationException("Injected failure after relational flush and before commit.");
            }));
        }

        await using var verification = database.CreateContext();
        Assert.Equal(0, await verification.AuditEvents.CountAsync());
        Assert.Equal(0, await verification.IdempotencyRecords.CountAsync());
        Assert.Equal(0, await verification.OutboxMessages.CountAsync());
        Assert.Equal(0, await verification.InboxMessages.CountAsync());
    }

    [Theory]
    [InlineData(V1DatabaseProvider.PostgreSql)]
    [InlineData(V1DatabaseProvider.MySql)]
    public async Task Successful_transaction_commits_audit_idempotency_outbox_and_inbox_together(V1DatabaseProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        if (database is null)
        {
            return;
        }

        var reservation = CreateIdempotencyReservation();
        var inboxReservation = CreateInboxReservation();

        await using (var context = database.CreateContext())
        {
            var transactionManager = new EfTransactionManager(context);
            var unitOfWork = new EfUnitOfWork(context);
            var audit = new EfAuditWriter(context);
            var idempotency = new EfIdempotencyStore(context);
            var outbox = new EfOutboxWriter(context);
            var inbox = new EfInboxStore(context);

            await transactionManager.ExecuteAsync(async ct =>
            {
                Assert.Equal(
                    IdempotencyReservationStatus.Acquired,
                    (await idempotency.TryReserveAsync(reservation, ct)).Status);

                Assert.Equal(
                    InboxReservationStatus.Acquired,
                    (await inbox.TryReserveAsync(inboxReservation, ct)).Status);

                await audit.AppendAsync(CreateAuditEvent(), ct);
                await outbox.EnqueueAsync(CreateIntegrationEvent(), CreateOutboxContext(), ct);
                await unitOfWork.SaveChangesAsync(ct);

                await idempotency.CompleteAsync(
                    new IdempotencyCompletion(
                        reservation.Scope,
                        reservation.Key,
                        reservation.RequestHash,
                        "sale.confirmed",
                        "Sale",
                        "sale-1001",
                        reservation.CorrelationId,
                        DateTimeOffset.UtcNow),
                    ct);

                await inbox.CompleteAsync(
                    new InboxCompletion(
                        inboxReservation.Consumer,
                        inboxReservation.MessageId,
                        inboxReservation.PayloadHash,
                        "processed",
                        inboxReservation.CorrelationId,
                        DateTimeOffset.UtcNow),
                    ct);

                await unitOfWork.SaveChangesAsync(ct);
            });
        }

        await using (var verification = database.CreateContext())
        {
            Assert.Equal(1, await verification.AuditEvents.CountAsync());
            Assert.Equal(1, await verification.IdempotencyRecords.CountAsync());
            Assert.Equal(1, await verification.OutboxMessages.CountAsync());
            Assert.Equal(1, await verification.InboxMessages.CountAsync());

            var idempotencyRow = await verification.IdempotencyRecords.SingleAsync();
            Assert.Equal(1, idempotencyRow.State);
            Assert.Equal("sale.confirmed", idempotencyRow.OutcomeCode);

            var inboxRow = await verification.InboxMessages.SingleAsync();
            Assert.Equal(1, inboxRow.State);
            Assert.Equal("processed", inboxRow.OutcomeCode);
        }

        await using (var replayContext = database.CreateContext())
        {
            var replayStore = new EfIdempotencyStore(replayContext);
            var replay = await replayStore.TryReserveAsync(reservation);
            Assert.Equal(IdempotencyReservationStatus.ExistingCompleted, replay.Status);
            Assert.Equal("sale.confirmed", replay.OutcomeCode);

            var mismatch = await replayStore.TryReserveAsync(reservation with { RequestHash = "different-request-hash" });
            Assert.Equal(IdempotencyReservationStatus.PayloadMismatch, mismatch.Status);
        }
    }

    private static IdempotencyReservation CreateIdempotencyReservation() =>
        new(
            "sales.confirm",
            "idem-key-1001",
            "request-hash-1001",
            "actor-1",
            "corr-1001",
            DateTimeOffset.UtcNow.AddMinutes(10));

    private static InboxReservation CreateInboxReservation() =>
        new(
            "fiscal-response-consumer",
            "provider-message-1001",
            "payload-hash-1001",
            "corr-1001",
            DateTimeOffset.UtcNow.AddMinutes(10));

    private static AuditEvent CreateAuditEvent() =>
        new(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            "sale.confirmed",
            "actor-1",
            "company-1",
            "location-1",
            "terminal-1",
            "Sale",
            "sale-1001",
            AuditOutcome.Succeeded,
            "corr-1001",
            null,
            new Dictionary<string, string?> { ["source"] = "integration-test" });

    private static TestIntegrationEvent CreateIntegrationEvent() =>
        new(Guid.NewGuid(), DateTimeOffset.UtcNow, "sale-1001");

    private static OutboxContext CreateOutboxContext() =>
        new("corr-1001", OrganizationId: "company-1", ActorId: "actor-1");

    private sealed record TestIntegrationEvent(
        Guid EventId,
        DateTimeOffset OccurredAt,
        string SaleId) : IIntegrationEvent;

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

            var connectionBuilder = new DbConnectionStringBuilder
            {
                ConnectionString = baseConnectionString
            };
            connectionBuilder["Database"] = $"efactura_v1_{provider.ToString().ToLowerInvariant()}_{Guid.NewGuid():N}";

            var optionsBuilder = new DbContextOptionsBuilder<V1PersistenceDbContext>();
            V1PersistenceDatabaseConfigurator.Configure(
                optionsBuilder,
                provider,
                connectionBuilder.ConnectionString);

            var database = new TestDatabase(optionsBuilder.Options);
            await using var context = database.CreateContext();
            await context.Database.EnsureCreatedAsync();
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
