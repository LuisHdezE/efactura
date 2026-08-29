using EFactura.Application.Catalog;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Common.Results;
using EFactura.Application.Common.Security;
using EFactura.Domain.Catalog;
using EFactura.Domain.Common;
using EFactura.Domain.Inventory;

namespace EFactura.Application.Inventory;

public sealed record InventoryPositionSearchRequest(
    string OrganizationId,
    Guid? ItemId,
    string? LocationId,
    IReadOnlyCollection<string> AllowedLocationIds,
    int Page = 1,
    int PageSize = 50);

public sealed record StockMovementSearchRequest(
    string OrganizationId,
    Guid? ItemId,
    string? LocationId,
    Guid? PositionId,
    IReadOnlyCollection<string> AllowedLocationIds,
    int Page = 1,
    int PageSize = 50);

public sealed record InventoryPositionView(
    Guid Id,
    long Version,
    string OrganizationId,
    Guid ItemId,
    string LocationId,
    decimal Quantity)
{
    public static InventoryPositionView FromDomain(InventoryPosition position) =>
        new(position.Id, position.Version, position.OrganizationId, position.ItemId, position.LocationId, position.Quantity);
}

public sealed record StockMovementView(
    Guid Id,
    Guid PositionId,
    string OrganizationId,
    Guid ItemId,
    string LocationId,
    StockMovementKind Kind,
    decimal QuantityBefore,
    decimal QuantityDelta,
    decimal QuantityAfter,
    string ReasonCode,
    string? Explanation,
    DateTimeOffset OccurredAtUtc)
{
    public static StockMovementView FromDomain(StockMovement movement) =>
        new(movement.Id, movement.PositionId, movement.OrganizationId, movement.ItemId, movement.LocationId,
            movement.Kind, movement.QuantityBefore, movement.QuantityDelta, movement.QuantityAfter,
            movement.ReasonCode, movement.Explanation, movement.OccurredAtUtc);
}

public sealed record CreateStockAdjustmentCommand(
    string OrganizationId,
    Guid ItemId,
    string LocationId,
    decimal QuantityDelta,
    string ReasonCode,
    string? Explanation,
    long ExpectedVersion,
    string IdempotencyKey,
    string RequestHash);

public sealed record StockAdjustmentResult(
    Guid PositionId,
    Guid MovementId,
    long Version,
    decimal Quantity,
    bool Replayed);

public sealed record InventoryAvailabilityRequirement(Guid ItemId, decimal Quantity);

public sealed record InventoryAvailabilityLineResult(
    Guid ItemId,
    bool TracksInventory,
    decimal RequiredQuantity,
    decimal? AvailableQuantity,
    long? PositionVersion,
    bool Sufficient,
    string? Finding);

public sealed record InventoryAvailabilityResult(
    bool Ready,
    IReadOnlyCollection<InventoryAvailabilityLineResult> Lines,
    IReadOnlyCollection<string> Findings);

public interface IInventoryRepository
{
    Task<InventoryPosition?> GetPositionAsync(
        string organizationId,
        Guid positionId,
        CancellationToken cancellationToken = default);

    Task<InventoryPosition?> GetPositionAsync(
        string organizationId,
        Guid itemId,
        string locationId,
        CancellationToken cancellationToken = default);

    Task<PageResult<InventoryPosition>> SearchPositionsAsync(
        InventoryPositionSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<PageResult<StockMovement>> SearchMovementsAsync(
        StockMovementSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<StockMovement?> GetMovementAsync(
        string organizationId,
        Guid movementId,
        CancellationToken cancellationToken = default);

    Task AddPositionAsync(InventoryPosition position, CancellationToken cancellationToken = default);
    Task SavePositionAsync(InventoryPosition position, CancellationToken cancellationToken = default);
    Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default);
}

public interface IInventoryAvailabilityChecker
{
    Task<InventoryAvailabilityResult> CheckAsync(
        string organizationId,
        string? locationId,
        IReadOnlyCollection<InventoryAvailabilityRequirement> requirements,
        CancellationToken cancellationToken = default);
}

public sealed class InventoryAvailabilityChecker : IInventoryAvailabilityChecker
{
    private readonly ICommercialItemRepository _items;
    private readonly IInventoryRepository _inventory;

    public InventoryAvailabilityChecker(ICommercialItemRepository items, IInventoryRepository inventory)
    {
        _items = items;
        _inventory = inventory;
    }

    public async Task<InventoryAvailabilityResult> CheckAsync(
        string organizationId,
        string? locationId,
        IReadOnlyCollection<InventoryAvailabilityRequirement> requirements,
        CancellationToken cancellationToken = default)
    {
        var grouped = requirements
            .GroupBy(x => x.ItemId)
            .Select(group => new InventoryAvailabilityRequirement(group.Key, group.Sum(x => x.Quantity)))
            .ToArray();
        var results = new List<InventoryAvailabilityLineResult>(grouped.Length);
        var findings = new HashSet<string>(StringComparer.Ordinal);

        foreach (var requirement in grouped)
        {
            var item = await _items.GetAsync(organizationId, requirement.ItemId, cancellationToken);
            if (item is null || !item.Active)
            {
                findings.Add("inventory.item_unavailable");
                results.Add(new InventoryAvailabilityLineResult(
                    requirement.ItemId, false, requirement.Quantity, null, null, false, "inventory.item_unavailable"));
                continue;
            }

            if (item.Kind != CommercialItemKind.Product || !item.TrackInventory)
            {
                results.Add(new InventoryAvailabilityLineResult(
                    requirement.ItemId, false, requirement.Quantity, null, null, true, null));
                continue;
            }

            if (string.IsNullOrWhiteSpace(locationId))
            {
                findings.Add("inventory.location_required");
                results.Add(new InventoryAvailabilityLineResult(
                    requirement.ItemId, true, requirement.Quantity, null, null, false, "inventory.location_required"));
                continue;
            }

            var position = await _inventory.GetPositionAsync(
                organizationId, requirement.ItemId, locationId.Trim(), cancellationToken);
            var available = position?.Quantity ?? 0m;
            var sufficient = available >= requirement.Quantity;
            var finding = sufficient ? null : "inventory.insufficient_stock";
            if (finding is not null)
                findings.Add(finding);

            results.Add(new InventoryAvailabilityLineResult(
                requirement.ItemId,
                true,
                requirement.Quantity,
                available,
                position?.Version,
                sufficient,
                finding));
        }

        if (results.Any(x => !x.Sufficient))
            findings.Add("inventory_availability_check");

        return new InventoryAvailabilityResult(
            results.All(x => x.Sufficient),
            results,
            findings.ToArray());
    }
}

internal static class InventoryAuthorization
{
    public static ActorContext Ensure(
        IActorContextAccessor actors,
        string organizationId,
        string permission,
        string? locationId = null)
    {
        var actor = actors.Current;
        if (!actor.IsAuthenticated || !actor.HasPermission(permission))
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden, "permission_denied",
                "The actor is not allowed to perform this inventory operation.");
        if (!actor.CompanyScopes.Contains(organizationId))
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden, "organization_scope_denied",
                "The actor is outside the requested organization scope.");
        if (actor.LocationScopes.Count == 0)
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden, "location_scope_required",
                "An explicit inventory location scope is required.");
        if (!string.IsNullOrWhiteSpace(locationId) && !actor.LocationScopes.Contains(locationId.Trim()))
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden, "location_scope_denied",
                "The actor is outside the requested inventory location scope.");
        return actor;
    }
}

public sealed class ListInventoryPositionsUseCase
{
    private readonly IInventoryRepository _inventory;
    private readonly IActorContextAccessor _actors;

    public ListInventoryPositionsUseCase(IInventoryRepository inventory, IActorContextAccessor actors)
    {
        _inventory = inventory;
        _actors = actors;
    }

    public Task<PageResult<InventoryPosition>> ExecuteAsync(
        string organizationId,
        Guid? itemId,
        string? locationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var actor = InventoryAuthorization.Ensure(_actors, organizationId, Permissions.InventoryRead, locationId);
        return _inventory.SearchPositionsAsync(
            new InventoryPositionSearchRequest(
                organizationId, itemId, locationId, actor.LocationScopes.ToArray(), page, pageSize),
            cancellationToken);
    }
}

public sealed class GetInventoryPositionUseCase
{
    private readonly IInventoryRepository _inventory;
    private readonly IActorContextAccessor _actors;

    public GetInventoryPositionUseCase(IInventoryRepository inventory, IActorContextAccessor actors)
    {
        _inventory = inventory;
        _actors = actors;
    }

    public async Task<InventoryPosition> ExecuteAsync(
        string organizationId,
        Guid positionId,
        CancellationToken cancellationToken = default)
    {
        var actor = InventoryAuthorization.Ensure(_actors, organizationId, Permissions.InventoryRead);
        var position = await _inventory.GetPositionAsync(organizationId, positionId, cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound, "inventory.position_not_found", "Inventory position was not found.");
        if (!actor.LocationScopes.Contains(position.LocationId))
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden, "location_scope_denied",
                "The actor is outside the inventory position location scope.");
        return position;
    }
}

public sealed class ListStockMovementsUseCase
{
    private readonly IInventoryRepository _inventory;
    private readonly IActorContextAccessor _actors;

    public ListStockMovementsUseCase(IInventoryRepository inventory, IActorContextAccessor actors)
    {
        _inventory = inventory;
        _actors = actors;
    }

    public Task<PageResult<StockMovement>> ExecuteAsync(
        string organizationId,
        Guid? itemId,
        string? locationId,
        Guid? positionId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var actor = InventoryAuthorization.Ensure(_actors, organizationId, Permissions.InventoryRead, locationId);
        return _inventory.SearchMovementsAsync(
            new StockMovementSearchRequest(
                organizationId, itemId, locationId, positionId,
                actor.LocationScopes.ToArray(), page, pageSize),
            cancellationToken);
    }
}

public sealed record StockAdjustedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid PositionId,
    Guid MovementId,
    Guid ItemId,
    string OrganizationId,
    string LocationId) : IIntegrationEvent;

public sealed class CreateStockAdjustmentUseCase
{
    private readonly IInventoryRepository _inventory;
    private readonly ICommercialItemRepository _items;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public CreateStockAdjustmentUseCase(
        IInventoryRepository inventory,
        ICommercialItemRepository items,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _inventory = inventory;
        _items = items;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<StockAdjustmentResult> ExecuteAsync(
        CreateStockAdjustmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var actor = InventoryAuthorization.Ensure(
            _actors, command.OrganizationId, Permissions.InventoryAdjust, command.LocationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RequestHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.ReasonCode);

        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var correlation = _correlations.Current;
            var scope = $"inventory.adjust:{command.OrganizationId}:{command.LocationId}:{command.ItemId}";
            var reservation = await _idempotency.TryReserveAsync(
                new IdempotencyReservation(
                    scope, command.IdempotencyKey, command.RequestHash,
                    actor.ActorId, correlation.CorrelationId, now.AddMinutes(10)), ct);

            if (reservation.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                if (!Guid.TryParse(reservation.ResourceId, out var movementId))
                    throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict, "idempotency.invalid_completed_resource",
                        "The prior stock adjustment cannot be reconstructed safely.");
                var replayedMovement = await _inventory.GetMovementAsync(command.OrganizationId, movementId, ct)
                    ?? throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict, "idempotency.missing_completed_resource",
                        "The prior stock adjustment no longer exists in the authoritative store.");
                return new StockAdjustmentResult(
                    replayedMovement.PositionId,
                    replayedMovement.Id,
                    replayedMovement.PositionVersionAfter,
                    replayedMovement.QuantityAfter,
                    true);
            }

            if (reservation.Status == IdempotencyReservationStatus.PayloadMismatch)
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Conflict, "idempotency_key_reused",
                    "The idempotency key was already used with a different stock adjustment.",
                    conflictType: "payload_mismatch");
            if (reservation.Status == IdempotencyReservationStatus.ExistingInProgress)
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Conflict, "idempotency_in_progress",
                    "A stock adjustment with this idempotency key is still in progress.",
                    conflictType: "in_progress", retryAfterSeconds: 2);

            await _unitOfWork.SaveChangesAsync(ct);

            var item = await _items.GetAsync(command.OrganizationId, command.ItemId, ct)
                ?? throw new ApplicationProblemException(
                    ApplicationProblemKind.Validation, "inventory.item_not_found",
                    "The selected item does not exist in this organization.");
            if (!item.Active || item.Kind != CommercialItemKind.Product || !item.TrackInventory)
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Validation, "inventory.item_not_stock_tracked",
                    "Stock adjustments require an active inventory-tracked product.");

            var position = await _inventory.GetPositionAsync(
                command.OrganizationId, command.ItemId, command.LocationId, ct);
            if (position is null)
            {
                if (command.ExpectedVersion != 0)
                    throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict, "concurrency_conflict",
                        "The inventory position does not yet exist; expectedVersion must be 0 for its first adjustment.",
                        conflictType: "stale_version", currentVersion: "0");
                position = InventoryPosition.Create(
                    Guid.NewGuid(), command.OrganizationId, command.ItemId, command.LocationId);
                await _inventory.AddPositionAsync(position, ct);
            }
            else if (position.Version != command.ExpectedVersion)
            {
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Conflict, "concurrency_conflict",
                    "The inventory position changed before this adjustment was applied.",
                    conflictType: "stale_version", currentVersion: position.Version.ToString());
            }

            StockMovement movement;
            try
            {
                movement = position.ApplyAdjustment(
                    command.QuantityDelta, command.ReasonCode, command.Explanation,
                    now, position.Version);
            }
            catch (DomainRuleException ex)
            {
                throw new ApplicationProblemException(
                    ex.Code == "concurrency.stale_version" ? ApplicationProblemKind.Conflict : ApplicationProblemKind.Validation,
                    ex.Code == "concurrency.stale_version" ? "concurrency_conflict" : ex.Code,
                    ex.Message,
                    conflictType: ex.Code == "concurrency.stale_version" ? "stale_version" : null);
            }

            await _inventory.SavePositionAsync(position, ct);
            await _inventory.AddMovementAsync(movement, ct);
            await _audit.AppendAsync(new AuditEvent(
                Guid.NewGuid(), now, "inventory.adjustment.posted", actor.ActorId,
                command.OrganizationId, command.LocationId, null,
                "InventoryPosition", position.Id.ToString(), AuditOutcome.Succeeded,
                correlation.CorrelationId, null,
                new Dictionary<string, string?>
                {
                    ["itemId"] = command.ItemId.ToString(),
                    ["movementId"] = movement.Id.ToString(),
                    ["quantityBefore"] = movement.QuantityBefore.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["quantityDelta"] = movement.QuantityDelta.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["quantityAfter"] = movement.QuantityAfter.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["reasonCode"] = movement.ReasonCode,
                    ["explanation"] = movement.Explanation,
                    ["version"] = position.Version.ToString()
                }), ct);
            await _outbox.EnqueueAsync(
                new StockAdjustedIntegrationEvent(
                    Guid.NewGuid(), now, position.Id, movement.Id, command.ItemId,
                    command.OrganizationId, command.LocationId),
                new OutboxContext(
                    correlation.CorrelationId,
                    OrganizationId: command.OrganizationId,
                    ActorId: actor.ActorId), ct);
            await _idempotency.CompleteAsync(new IdempotencyCompletion(
                scope, command.IdempotencyKey, command.RequestHash, "inventory_adjustment_posted",
                "StockMovement", movement.Id.ToString(), correlation.CorrelationId, now), ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return new StockAdjustmentResult(position.Id, movement.Id, position.Version, position.Quantity, false);
        }, cancellationToken);
    }
}
