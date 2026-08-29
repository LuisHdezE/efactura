using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Common.Security;
using EFactura.Domain.Catalog;
using EFactura.Domain.Common;

namespace EFactura.Application.Catalog;

public sealed record CreateCommercialItemCommand(
    string OrganizationId,
    string Code,
    string Name,
    string? Description,
    CommercialItemKind Kind,
    string Unit,
    bool TrackInventory,
    Guid? TaxProfileId,
    Guid? CategoryId,
    string IdempotencyKey,
    string RequestHash);

public sealed record CommercialItemCreatedResult(Guid ItemId, long Version, bool Replayed);

public interface ICommercialItemRepository
{
    Task AddAsync(CommercialItem item, CancellationToken cancellationToken = default);
    Task<CommercialItem?> GetAsync(string organizationId, Guid itemId, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(
        string organizationId,
        string code,
        Guid? excludingItemId = null,
        CancellationToken cancellationToken = default);
}

public sealed record CommercialItemCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid ItemId,
    string OrganizationId) : IIntegrationEvent;

public sealed class CreateCommercialItemUseCase
{
    private readonly ICommercialItemRepository _items;
    private readonly IItemCategoryRepository? _categories;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actorContext;
    private readonly ICorrelationContextAccessor _correlationContext;

    public CreateCommercialItemUseCase(
        ICommercialItemRepository items,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actorContext,
        ICorrelationContextAccessor correlationContext,
        IItemCategoryRepository? categories = null)
    {
        _items = items;
        _categories = categories;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actorContext = actorContext;
        _correlationContext = correlationContext;
    }

    public Task<CommercialItemCreatedResult> ExecuteAsync(
        CreateCommercialItemCommand command,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthorized(command.OrganizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RequestHash);

        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var actor = _actorContext.Current;
            var correlation = _correlationContext.Current;
            var scope = $"catalog.item.create:{command.OrganizationId}";

            var reservation = await _idempotency.TryReserveAsync(
                new IdempotencyReservation(
                    scope,
                    command.IdempotencyKey,
                    command.RequestHash,
                    actor.ActorId,
                    correlation.CorrelationId,
                    now.AddMinutes(10)),
                ct);

            if (reservation.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                if (!Guid.TryParse(reservation.ResourceId, out var replayId))
                {
                    throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict,
                        "idempotency.invalid_completed_resource",
                        "The prior completed operation cannot be reconstructed safely.");
                }

                var replayed = await _items.GetAsync(command.OrganizationId, replayId, ct)
                    ?? throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict,
                        "idempotency.missing_completed_resource",
                        "The prior completed item no longer exists in the authoritative store.");

                return new CommercialItemCreatedResult(replayed.Id, replayed.Version, true);
            }

            if (reservation.Status == IdempotencyReservationStatus.PayloadMismatch)
            {
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Conflict,
                    "idempotency_key_reused",
                    "The idempotency key was already used with a different request.",
                    conflictType: "payload_mismatch");
            }

            if (reservation.Status == IdempotencyReservationStatus.ExistingInProgress)
            {
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Conflict,
                    "idempotency_in_progress",
                    "An operation with this idempotency key is still in progress.",
                    conflictType: "in_progress",
                    retryAfterSeconds: 2);
            }

            await _unitOfWork.SaveChangesAsync(ct);

            if (await _items.CodeExistsAsync(command.OrganizationId, command.Code, null, ct))
            {
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Conflict,
                    "catalog.code_exists",
                    "An item with the same code already exists in this organization.",
                    conflictType: "duplicate_code");
            }

            if (command.CategoryId.HasValue)
            {
                if (_categories is null)
                {
                    throw new ApplicationProblemException(
                        ApplicationProblemKind.Validation,
                        "catalog.category_validation_unavailable",
                        "Category assignment is unavailable in this application composition.");
                }

                var category = await _categories.GetAsync(command.OrganizationId, command.CategoryId.Value, ct)
                    ?? throw new ApplicationProblemException(
                        ApplicationProblemKind.Validation,
                        "catalog.category_not_found",
                        "The selected category does not exist in this organization.");

                if (!category.Active)
                {
                    throw new ApplicationProblemException(
                        ApplicationProblemKind.Validation,
                        "catalog.category_inactive",
                        "The selected category is inactive.");
                }
            }

            CommercialItem item;
            try
            {
                item = CommercialItem.Create(
                    Guid.NewGuid(),
                    command.OrganizationId,
                    command.Code,
                    command.Name,
                    command.Description,
                    command.Kind,
                    command.Unit,
                    command.TrackInventory,
                    command.TaxProfileId,
                    command.CategoryId);
            }
            catch (DomainRuleException ex)
            {
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Validation,
                    ex.Code,
                    ex.Message);
            }

            await _items.AddAsync(item, ct);

            await _audit.AppendAsync(
                new AuditEvent(
                    Guid.NewGuid(),
                    now,
                    "catalog.item.created",
                    actor.ActorId,
                    command.OrganizationId,
                    null,
                    null,
                    "CommercialItem",
                    item.Id.ToString(),
                    AuditOutcome.Succeeded,
                    correlation.CorrelationId,
                    correlation.CausationId,
                    new Dictionary<string, string?>
                    {
                        ["code"] = item.Code,
                        ["kind"] = item.Kind.ToString(),
                        ["trackInventory"] = item.TrackInventory.ToString()
                    }),
                ct);

            await _outbox.EnqueueAsync(
                new CommercialItemCreatedIntegrationEvent(Guid.NewGuid(), now, item.Id, command.OrganizationId),
                new OutboxContext(
                    correlation.CorrelationId,
                    correlation.CausationId,
                    command.OrganizationId,
                    actor.ActorId),
                ct);

            await _idempotency.CompleteAsync(
                new IdempotencyCompletion(
                    scope,
                    command.IdempotencyKey,
                    command.RequestHash,
                    "created",
                    "CommercialItem",
                    item.Id.ToString(),
                    correlation.CorrelationId,
                    now),
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return new CommercialItemCreatedResult(item.Id, item.Version, false);
        }, cancellationToken);
    }

    private void EnsureAuthorized(string organizationId)
    {
        var actor = _actorContext.Current;
        if (!actor.IsAuthenticated || !actor.HasPermission(Permissions.CatalogManage))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "permission_denied",
                "The actor is not allowed to manage catalog items.");
        }

        if (!actor.CompanyScopes.Contains(organizationId))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "organization_scope_denied",
                "The actor is not allowed to manage catalog items in this organization.");
        }
    }
}
