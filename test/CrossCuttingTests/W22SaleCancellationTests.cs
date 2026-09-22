using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Common.Results;
using EFactura.Application.Common.Security;
using EFactura.Application.Sales;
using EFactura.Domain.Common;
using EFactura.Domain.Sales;
using Xunit;

namespace CrossCuttingTests;

public sealed class W22SaleCancellationTests
{
    private const string OrganizationId = "org-a";
    private const string ValidationFingerprint = "validated-evidence";
    private const string ConfirmationFingerprint = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string SettlementFingerprint = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void Draft_to_cancelled_is_terminal_and_increments_version_once()
    {
        var sale = SaleOf(SaleStatus.Draft);

        sale.MarkCancelled(1);

        Assert.Equal(SaleStatus.Cancelled, sale.Status);
        Assert.Equal(2, sale.Version);
        var error = Assert.Throws<DomainRuleException>(() => sale.MarkCancelled(2));
        Assert.Equal("sales.already_cancelled", error.Code);
    }

    [Fact]
    public void Validated_to_cancelled_preserves_validation_evidence()
    {
        var sale = SaleOf(SaleStatus.Validated);
        var fingerprint = sale.ValidationFingerprint;
        var validatedAt = sale.ValidatedAtUtc;

        sale.MarkCancelled(2);

        Assert.Equal(SaleStatus.Cancelled, sale.Status);
        Assert.Equal(3, sale.Version);
        Assert.Equal(fingerprint, sale.ValidationFingerprint);
        Assert.Equal(validatedAt, sale.ValidatedAtUtc);
        Assert.Null(sale.ConfirmationFingerprint);
        Assert.Null(sale.SettlementFingerprint);
        Assert.Null(sale.ConfirmedAtUtc);
    }

    [Fact]
    public void Confirmed_rejects_cancellation_before_stale_version_hint()
    {
        var sale = SaleOf(SaleStatus.Confirmed);

        var error = Assert.Throws<DomainRuleException>(() => sale.MarkCancelled(999));

        Assert.Equal("sales.cancellation.irreversible_boundary_crossed", error.Code);
        Assert.Equal(SaleStatus.Confirmed, sale.Status);
    }

    [Fact]
    public void Cancelled_rejects_edit_and_validation()
    {
        var sale = SaleOf(SaleStatus.Draft);
        sale.MarkCancelled(1);

        var validationError = Assert.Throws<DomainRuleException>(
            () => sale.MarkValidated("new-evidence", DateTimeOffset.UtcNow, 2));
        Assert.Equal("sales.cancelled_terminal", validationError.Code);
    }

    [Fact]
    public async Task Successful_command_persists_exact_cancellation_evidence()
    {
        var harness = Harness(SaleOf(SaleStatus.Validated));
        var command = Command(harness.Repository.Sale, expectedVersion: 2);

        var result = await harness.UseCase.ExecuteAsync(command);

        Assert.False(result.Replayed);
        Assert.Equal(SaleStatus.Cancelled, result.Status);
        Assert.Equal(3, result.Version);
        Assert.Equal(1, harness.Repository.SaveCalls);
        var audit = Assert.Single(harness.Audit.Events);
        Assert.Equal("SALE_CANCELLED", audit.EventName);
        Assert.Equal("Validated", audit.Metadata["previousStatus"]);
        Assert.Equal("Cancelled", audit.Metadata["newStatus"]);
        Assert.Equal(command.OperatorReason.Trim(), audit.Metadata["operatorReason"]);
        var outbound = Assert.IsType<SaleCancelledIntegrationEvent>(Assert.Single(harness.Outbox.Events));
        Assert.Equal(SaleStatus.Validated, outbound.PreviousStatus);
        Assert.Equal(3, outbound.Version);
        Assert.NotNull(harness.Idempotency.Completion);
        Assert.Equal("sale_cancelled", harness.Idempotency.Completion!.OutcomeCode);
        Assert.Equal("Sale", harness.Idempotency.Completion.ResourceType);
        Assert.Equal(harness.Repository.Sale.Id.ToString(), harness.Idempotency.Completion.ResourceId);
    }

    [Fact]
    public async Task Completed_same_request_replays_without_duplicate_evidence_or_version_increment()
    {
        var harness = Harness(SaleOf(SaleStatus.Draft));
        var command = Command(harness.Repository.Sale, expectedVersion: 1);

        var first = await harness.UseCase.ExecuteAsync(command);
        var replay = await harness.UseCase.ExecuteAsync(command);

        Assert.False(first.Replayed);
        Assert.True(replay.Replayed);
        Assert.Equal(first.Version, replay.Version);
        Assert.Equal(2, harness.Repository.Sale.Version);
        Assert.Single(harness.Audit.Events);
        Assert.Single(harness.Outbox.Events);
        Assert.Equal(1, harness.Repository.SaveCalls);
    }

    [Fact]
    public async Task Confirmed_state_precedes_stale_version_for_new_command()
    {
        var harness = Harness(SaleOf(SaleStatus.Confirmed));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => harness.UseCase.ExecuteAsync(Command(harness.Repository.Sale, expectedVersion: 1)));

        Assert.Equal("sales.cancellation.irreversible_boundary_crossed", error.Code);
        Assert.Equal("irreversible_boundary", error.ConflictType);
        Assert.Equal(harness.Repository.Sale.Version.ToString(), error.CurrentVersion);
        Assert.Empty(harness.Audit.Events);
        Assert.Empty(harness.Outbox.Events);
    }

    [Fact]
    public async Task Already_cancelled_state_precedes_stale_version_for_new_command()
    {
        var sale = SaleOf(SaleStatus.Draft);
        sale.MarkCancelled(1);
        var harness = Harness(sale);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => harness.UseCase.ExecuteAsync(Command(sale, expectedVersion: 99)));

        Assert.Equal("sales.already_cancelled", error.Code);
        Assert.Equal("invalid_state", error.ConflictType);
    }

    [Fact]
    public async Task Draft_with_stale_version_returns_locked_conflict_shape()
    {
        var harness = Harness(SaleOf(SaleStatus.Draft));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => harness.UseCase.ExecuteAsync(Command(harness.Repository.Sale, expectedVersion: 2)));

        Assert.Equal("concurrency.stale_version", error.Code);
        Assert.Equal("stale_version", error.ConflictType);
        Assert.Equal("1", error.CurrentVersion);
    }

    [Fact]
    public async Task Missing_sales_cancel_permission_is_forbidden()
    {
        var sale = SaleOf(SaleStatus.Draft);
        var harness = Harness(sale, actor: Actor(OrganizationId));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => harness.UseCase.ExecuteAsync(Command(sale, 1)));

        Assert.Equal("permission_denied", error.Code);
    }

    [Fact]
    public async Task Organization_scope_escape_is_forbidden()
    {
        var sale = SaleOf(SaleStatus.Draft);
        var harness = Harness(sale, actor: Actor("org-b", Permissions.SalesCancel));

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => harness.UseCase.ExecuteAsync(Command(sale, 1)));

        Assert.Equal("organization_scope_denied", error.Code);
    }

    [Fact]
    public async Task Cross_organization_sale_is_masked_as_not_found()
    {
        var sale = SaleOf(SaleStatus.Draft);
        var harness = Harness(sale, actor: Actor("org-b", Permissions.SalesCancel));
        var command = Command(sale, 1) with { OrganizationId = "org-b" };

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => harness.UseCase.ExecuteAsync(command));

        Assert.Equal("sales.not_found", error.Code);
    }

    [Fact]
    public async Task Payload_mismatch_and_in_progress_preserve_sale_and_evidence()
    {
        foreach (var status in new[]
                 {
                     IdempotencyReservationStatus.PayloadMismatch,
                     IdempotencyReservationStatus.ExistingInProgress
                 })
        {
            var harness = Harness(SaleOf(SaleStatus.Draft), idempotencyStatus: status);
            var error = await Assert.ThrowsAsync<ApplicationProblemException>(
                () => harness.UseCase.ExecuteAsync(Command(harness.Repository.Sale, 1)));

            Assert.Equal(
                status == IdempotencyReservationStatus.PayloadMismatch
                    ? "idempotency_key_reused"
                    : "idempotency_in_progress",
                error.Code);
            Assert.Equal(SaleStatus.Draft, harness.Repository.Sale.Status);
            Assert.Empty(harness.Audit.Events);
            Assert.Empty(harness.Outbox.Events);
        }
    }

    [Fact]
    public async Task Reason_and_context_validation_follow_locked_bounds()
    {
        var sale = SaleOf(SaleStatus.Draft);
        var harness = Harness(sale);

        var missing = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => harness.UseCase.ExecuteAsync(Command(sale, 1) with { OperatorReason = "  " }));
        Assert.Equal("sales.cancellation.operator_reason_required", missing.Code);

        var tooLong = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => harness.UseCase.ExecuteAsync(Command(sale, 1) with { OperatorContext = new string('x', 1001) }));
        Assert.Equal("sales.cancellation.operator_context_too_long", tooLong.Code);
    }

    private static HarnessState Harness(
        Sale sale,
        ActorContext? actor = null,
        IdempotencyReservationStatus idempotencyStatus = IdempotencyReservationStatus.Acquired)
    {
        var repository = new SaleRepository(sale);
        var idempotency = new IdempotencyStore(idempotencyStatus);
        var audit = new AuditWriter();
        var outbox = new OutboxWriter();
        var useCase = new CancelSaleUseCase(
            repository,
            new DirectTransactionManager(),
            new UnitOfWork(),
            idempotency,
            audit,
            outbox,
            new ActorAccessor(actor ?? Actor(OrganizationId, Permissions.SalesCancel)),
            new CorrelationAccessor(new CorrelationContext("corr-w22", "trace-w22")));
        return new HarnessState(useCase, repository, idempotency, audit, outbox);
    }

    private static CancelSaleCommand Command(Sale sale, long expectedVersion) => new(
        sale.OrganizationId,
        sale.Id,
        expectedVersion,
        " Customer requested cancellation before confirmation ",
        "operator-context",
        "cancel-key",
        "cancel-hash");

    private static ActorContext Actor(string organizationId, params string[] permissions) => new(
        "actor-w22",
        "W2.2 Operator",
        true,
        new HashSet<string>(permissions, StringComparer.Ordinal),
        new HashSet<string>(new[] { organizationId }, StringComparer.Ordinal),
        new HashSet<string>(new[] { "loc-1" }, StringComparer.Ordinal),
        new HashSet<string>(new[] { "term-1" }, StringComparer.Ordinal),
        null);

    private static Sale SaleOf(SaleStatus status)
    {
        var now = DateTimeOffset.UtcNow;
        var line = SaleLine.Create(
            Guid.NewGuid(), Guid.NewGuid(), "SKU-1", "Item", SaleLineKind.Product, 1m, 100m, null);

        return Sale.Rehydrate(
            Guid.NewGuid(),
            OrganizationId,
            "loc-1",
            "term-1",
            null,
            SaleCommercialIntent.ConsumerFinal,
            "UYU",
            DateOnly.FromDateTime(DateTime.UtcNow),
            null,
            false,
            new[] { line },
            status,
            status is SaleStatus.Validated or SaleStatus.Confirmed or SaleStatus.Cancelled ? ValidationFingerprint : null,
            status is SaleStatus.Validated or SaleStatus.Confirmed or SaleStatus.Cancelled ? now.AddMinutes(-2) : null,
            status switch
            {
                SaleStatus.Draft => 1,
                SaleStatus.Validated => 2,
                SaleStatus.Confirmed => 3,
                SaleStatus.Cancelled => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(status))
            },
            status == SaleStatus.Confirmed ? ConfirmationFingerprint : null,
            status == SaleStatus.Confirmed ? SettlementFingerprint : null,
            status == SaleStatus.Confirmed ? now.AddMinutes(-1) : null);
    }

    private sealed record HarnessState(
        CancelSaleUseCase UseCase,
        SaleRepository Repository,
        IdempotencyStore Idempotency,
        AuditWriter Audit,
        OutboxWriter Outbox);

    private sealed class SaleRepository : ISaleRepository
    {
        public SaleRepository(Sale sale) => Sale = sale;
        public Sale Sale { get; }
        public int SaveCalls { get; private set; }

        public Task<Sale?> GetAsync(string organizationId, Guid saleId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Sale?>(
                string.Equals(organizationId, Sale.OrganizationId, StringComparison.Ordinal) && saleId == Sale.Id
                    ? Sale
                    : null);
        public Task SaveAsync(Sale sale, CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.CompletedTask;
        }
        public Task AddAsync(Sale sale, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<PageResult<Sale>> SearchAsync(SaleSearchRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class DirectTransactionManager : ITransactionManager
    {
        public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) =>
            await operation(cancellationToken);
        public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            await operation(cancellationToken);
    }

    private sealed class UnitOfWork : IUnitOfWork
    {
        public int Calls { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(1);
        }
    }

    private sealed class IdempotencyStore : IIdempotencyStore
    {
        private IdempotencyReservationStatus _status;
        public IdempotencyStore(IdempotencyReservationStatus status) => _status = status;
        public IdempotencyCompletion? Completion { get; private set; }

        public Task<IdempotencyReservationResult> TryReserveAsync(
            IdempotencyReservation reservation,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new IdempotencyReservationResult(_status));

        public Task CompleteAsync(IdempotencyCompletion completion, CancellationToken cancellationToken = default)
        {
            Completion = completion;
            _status = IdempotencyReservationStatus.ExistingCompleted;
            return Task.CompletedTask;
        }

        public Task AbandonAsync(
            string scope,
            string key,
            string requestHash,
            string correlationId,
            CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class AuditWriter : IAuditWriter
    {
        public List<AuditEvent> Events { get; } = new();
        public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class OutboxWriter : IOutboxWriter
    {
        public List<IIntegrationEvent> Events { get; } = new();
        public Task EnqueueAsync<TEvent>(
            TEvent integrationEvent,
            OutboxContext context,
            CancellationToken cancellationToken = default)
            where TEvent : IIntegrationEvent
        {
            Events.Add(integrationEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class ActorAccessor : IActorContextAccessor
    {
        public ActorAccessor(ActorContext current) => Current = current;
        public ActorContext Current { get; }
    }

    private sealed class CorrelationAccessor : ICorrelationContextAccessor
    {
        public CorrelationAccessor(CorrelationContext current) => Current = current;
        public CorrelationContext Current { get; }
    }
}
