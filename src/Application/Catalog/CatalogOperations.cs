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

namespace EFactura.Application.Catalog;

public sealed record CommercialItemView(
    Guid Id,
    long Version,
    bool Active,
    string Code,
    string Name,
    string? Description,
    CommercialItemKind Kind,
    string Unit,
    bool TrackInventory,
    Guid? TaxProfileId,
    Guid? CategoryId)
{
    public static CommercialItemView FromDomain(CommercialItem item) =>
        new(
            item.Id,
            item.Version,
            item.Active,
            item.Code,
            item.Name,
            item.Description,
            item.Kind,
            item.Unit,
            item.TrackInventory,
            item.TaxProfileId,
            item.CategoryId);
}

public sealed record ItemCategoryView(
    Guid Id,
    long Version,
    bool Active,
    string Code,
    string Name)
{
    public static ItemCategoryView FromDomain(ItemCategory category) =>
        new(category.Id, category.Version, category.Active, category.Code, category.Name);
}

public sealed record CommercialItemSearchRequest(
    string OrganizationId,
    string? Search,
    CommercialItemKind? Kind,
    bool? Active,
    bool? TrackInventory,
    Guid? CategoryId,
    int Page = 1,
    int PageSize = 50);

public sealed record ItemCategorySearchRequest(
    string OrganizationId,
    string? Search,
    bool? Active,
    int Page = 1,
    int PageSize = 100);

public interface ICommercialItemMaintenanceRepository
{
    Task<PageResult<CommercialItem>> SearchAsync(
        CommercialItemSearchRequest request,
        CancellationToken cancellationToken = default);

    Task SaveAsync(CommercialItem item, CancellationToken cancellationToken = default);
}

public interface IItemCategoryRepository
{
    Task AddAsync(ItemCategory category, CancellationToken cancellationToken = default);
    Task<ItemCategory?> GetAsync(string organizationId, Guid categoryId, CancellationToken cancellationToken = default);
    Task<PageResult<ItemCategory>> SearchAsync(ItemCategorySearchRequest request, CancellationToken cancellationToken = default);
    Task SaveAsync(ItemCategory category, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(
        string organizationId,
        string code,
        Guid? excludingCategoryId = null,
        CancellationToken cancellationToken = default);
}

public sealed class ListCommercialItemsUseCase
{
    private readonly ICommercialItemMaintenanceRepository _items;
    private readonly IActorContextAccessor _actorContext;

    public ListCommercialItemsUseCase(ICommercialItemMaintenanceRepository items, IActorContextAccessor actorContext)
    {
        _items = items;
        _actorContext = actorContext;
    }

    public async Task<PageResult<CommercialItemView>> ExecuteAsync(
        CommercialItemSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCatalogReadAuthorized(_actorContext.Current, request.OrganizationId);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var result = await _items.SearchAsync(request with { Page = page, PageSize = pageSize }, cancellationToken);
        return new PageResult<CommercialItemView>(
            result.Items.Select(CommercialItemView.FromDomain).ToArray(),
            page,
            pageSize,
            result.Total);
    }

    internal static void EnsureCatalogReadAuthorized(ActorContext actor, string organizationId)
    {
        if (!actor.IsAuthenticated || !actor.HasPermission(Permissions.CatalogRead))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "permission_denied",
                "The actor is not allowed to read the catalog.");
        }

        if (!actor.CompanyScopes.Contains(organizationId))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "organization_scope_denied",
                "The actor is not allowed to read the catalog in this organization.");
        }
    }
}

public sealed class GetCommercialItemUseCase
{
    private readonly ICommercialItemRepository _items;
    private readonly IActorContextAccessor _actorContext;

    public GetCommercialItemUseCase(ICommercialItemRepository items, IActorContextAccessor actorContext)
    {
        _items = items;
        _actorContext = actorContext;
    }

    public async Task<CommercialItemView> ExecuteAsync(
        string organizationId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        ListCommercialItemsUseCase.EnsureCatalogReadAuthorized(_actorContext.Current, organizationId);
        var item = await _items.GetAsync(organizationId, itemId, cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound,
                "catalog.item_not_found",
                "The requested catalog item was not found.");

        return CommercialItemView.FromDomain(item);
    }
}

public sealed class ListItemCategoriesUseCase
{
    private readonly IItemCategoryRepository _categories;
    private readonly IActorContextAccessor _actorContext;

    public ListItemCategoriesUseCase(IItemCategoryRepository categories, IActorContextAccessor actorContext)
    {
        _categories = categories;
        _actorContext = actorContext;
    }

    public async Task<PageResult<ItemCategoryView>> ExecuteAsync(
        ItemCategorySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        ListCommercialItemsUseCase.EnsureCatalogReadAuthorized(_actorContext.Current, request.OrganizationId);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var result = await _categories.SearchAsync(request with { Page = page, PageSize = pageSize }, cancellationToken);
        return new PageResult<ItemCategoryView>(
            result.Items.Select(ItemCategoryView.FromDomain).ToArray(),
            page,
            pageSize,
            result.Total);
    }
}

public sealed record CatalogMutationResult(Guid ResourceId, long Version, bool Replayed);

public sealed record CatalogChangedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid ResourceId,
    string OrganizationId,
    string ResourceType,
    string ChangeType) : IIntegrationEvent;

public sealed class CatalogItemMutationWorkflow
{
    private readonly ICommercialItemRepository _items;
    private readonly ICommercialItemMaintenanceRepository _maintenance;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actorContext;
    private readonly ICorrelationContextAccessor _correlationContext;

    public CatalogItemMutationWorkflow(
        ICommercialItemRepository items,
        ICommercialItemMaintenanceRepository maintenance,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actorContext,
        ICorrelationContextAccessor correlationContext)
    {
        _items = items;
        _maintenance = maintenance;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actorContext = actorContext;
        _correlationContext = correlationContext;
    }

    public Task<CatalogMutationResult> ExecuteAsync(
        string organizationId,
        Guid itemId,
        string operationScope,
        string idempotencyKey,
        string requestHash,
        string auditEventName,
        string changeType,
        Func<CommercialItem, CancellationToken, Task> mutate,
        Func<CommercialItem, IReadOnlyDictionary<string, string?>> metadata,
        CancellationToken cancellationToken = default)
    {
        EnsureCatalogManageAuthorized(_actorContext.Current, organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestHash);

        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var actor = _actorContext.Current;
            var correlation = _correlationContext.Current;
            var scope = $"{operationScope}:{organizationId}:{itemId:N}";

            var reservation = await _idempotency.TryReserveAsync(
                new IdempotencyReservation(
                    scope,
                    idempotencyKey,
                    requestHash,
                    actor.ActorId,
                    correlation.CorrelationId,
                    now.AddMinutes(10)),
                ct);

            if (reservation.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                var replayed = await _items.GetAsync(organizationId, itemId, ct)
                    ?? throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict,
                        "idempotency.missing_completed_resource",
                        "The prior completed catalog item no longer exists in the authoritative store.");
                return new CatalogMutationResult(replayed.Id, replayed.Version, true);
            }

            HandleReservationConflict(reservation);
            await _unitOfWork.SaveChangesAsync(ct);

            var item = await _items.GetAsync(organizationId, itemId, ct)
                ?? throw new ApplicationProblemException(
                    ApplicationProblemKind.NotFound,
                    "catalog.item_not_found",
                    "The requested catalog item was not found.");

            try
            {
                await mutate(item, ct);
            }
            catch (DomainRuleException ex)
            {
                throw MapDomainRule(ex, item.Version);
            }

            await _maintenance.SaveAsync(item, ct);
            await _audit.AppendAsync(
                new AuditEvent(
                    Guid.NewGuid(),
                    now,
                    auditEventName,
                    actor.ActorId,
                    organizationId,
                    null,
                    null,
                    "CommercialItem",
                    item.Id.ToString(),
                    AuditOutcome.Succeeded,
                    correlation.CorrelationId,
                    correlation.CausationId,
                    metadata(item)),
                ct);

            await _outbox.EnqueueAsync(
                new CatalogChangedIntegrationEvent(
                    Guid.NewGuid(),
                    now,
                    item.Id,
                    organizationId,
                    "CommercialItem",
                    changeType),
                new OutboxContext(correlation.CorrelationId, correlation.CausationId, organizationId, actor.ActorId),
                ct);

            await _idempotency.CompleteAsync(
                new IdempotencyCompletion(
                    scope,
                    idempotencyKey,
                    requestHash,
                    auditEventName,
                    "CommercialItem",
                    item.Id.ToString(),
                    correlation.CorrelationId,
                    now),
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return new CatalogMutationResult(item.Id, item.Version, false);
        }, cancellationToken);
    }

    internal static void EnsureCatalogManageAuthorized(ActorContext actor, string organizationId)
    {
        if (!actor.IsAuthenticated || !actor.HasPermission(Permissions.CatalogManage))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "permission_denied",
                "The actor is not allowed to manage the catalog.");
        }

        if (!actor.CompanyScopes.Contains(organizationId))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "organization_scope_denied",
                "The actor is not allowed to manage the catalog in this organization.");
        }
    }

    internal static void HandleReservationConflict(IdempotencyReservationResult reservation)
    {
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
    }

    internal static ApplicationProblemException MapDomainRule(DomainRuleException ex, long currentVersion)
    {
        if (string.Equals(ex.Code, "concurrency.stale_version", StringComparison.Ordinal))
        {
            return new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                "The catalog resource changed before this operation could be applied.",
                conflictType: "stale_version",
                currentVersion: currentVersion.ToString());
        }

        return new ApplicationProblemException(ApplicationProblemKind.Validation, ex.Code, ex.Message);
    }
}

public sealed record UpdateCommercialItemCommand(
    string OrganizationId,
    Guid ItemId,
    string? Code,
    string? Name,
    string? Description,
    CommercialItemKind? Kind,
    string? Unit,
    bool? TrackInventory,
    Guid? TaxProfileId,
    bool ReplaceTaxProfile,
    Guid? CategoryId,
    bool ReplaceCategory,
    long ExpectedVersion,
    string IdempotencyKey,
    string RequestHash);

public sealed class UpdateCommercialItemUseCase
{
    private readonly CatalogItemMutationWorkflow _workflow;
    private readonly ICommercialItemRepository _items;
    private readonly IItemCategoryRepository _categories;

    public UpdateCommercialItemUseCase(
        CatalogItemMutationWorkflow workflow,
        ICommercialItemRepository items,
        IItemCategoryRepository categories)
    {
        _workflow = workflow;
        _items = items;
        _categories = categories;
    }

    public Task<CatalogMutationResult> ExecuteAsync(
        UpdateCommercialItemCommand command,
        CancellationToken cancellationToken = default) =>
        _workflow.ExecuteAsync(
            command.OrganizationId,
            command.ItemId,
            "catalog.item.update",
            command.IdempotencyKey,
            command.RequestHash,
            "catalog.item.updated",
            "updated",
            async (item, ct) =>
            {
                var code = string.IsNullOrWhiteSpace(command.Code) ? item.Code : command.Code;
                if (!string.Equals(code.Trim(), item.Code, StringComparison.OrdinalIgnoreCase)
                    && await _items.CodeExistsAsync(command.OrganizationId, code, command.ItemId, ct))
                {
                    throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict,
                        "catalog.code_exists",
                        "An item with the same code already exists in this organization.",
                        conflictType: "duplicate_code");
                }

                var categoryId = command.ReplaceCategory ? command.CategoryId : item.CategoryId;
                if (categoryId.HasValue)
                {
                    var category = await _categories.GetAsync(command.OrganizationId, categoryId.Value, ct)
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

                item.Update(
                    code,
                    string.IsNullOrWhiteSpace(command.Name) ? item.Name : command.Name,
                    command.Description ?? item.Description,
                    command.Kind ?? item.Kind,
                    string.IsNullOrWhiteSpace(command.Unit) ? item.Unit : command.Unit,
                    command.TrackInventory ?? item.TrackInventory,
                    command.ReplaceTaxProfile ? command.TaxProfileId : item.TaxProfileId,
                    categoryId,
                    command.ExpectedVersion);
            },
            item => new Dictionary<string, string?>
            {
                ["version"] = item.Version.ToString(),
                ["code"] = item.Code,
                ["kind"] = item.Kind.ToString(),
                ["active"] = item.Active.ToString()
            },
            cancellationToken);
}

public sealed record DeactivateCommercialItemCommand(
    string OrganizationId,
    Guid ItemId,
    long ExpectedVersion,
    string IdempotencyKey,
    string RequestHash);

public sealed class DeactivateCommercialItemUseCase
{
    private readonly CatalogItemMutationWorkflow _workflow;

    public DeactivateCommercialItemUseCase(CatalogItemMutationWorkflow workflow)
    {
        _workflow = workflow;
    }

    public Task<CatalogMutationResult> ExecuteAsync(
        DeactivateCommercialItemCommand command,
        CancellationToken cancellationToken = default) =>
        _workflow.ExecuteAsync(
            command.OrganizationId,
            command.ItemId,
            "catalog.item.deactivate",
            command.IdempotencyKey,
            command.RequestHash,
            "catalog.item.deactivated",
            "deactivated",
            (item, _) =>
            {
                item.Deactivate(command.ExpectedVersion);
                return Task.CompletedTask;
            },
            item => new Dictionary<string, string?>
            {
                ["version"] = item.Version.ToString(),
                ["code"] = item.Code,
                ["active"] = item.Active.ToString()
            },
            cancellationToken);
}

public sealed record CreateItemCategoryCommand(
    string OrganizationId,
    string Code,
    string Name,
    string IdempotencyKey,
    string RequestHash);

public sealed record UpdateItemCategoryCommand(
    string OrganizationId,
    Guid CategoryId,
    string? Code,
    string? Name,
    bool? Active,
    long ExpectedVersion,
    string IdempotencyKey,
    string RequestHash);

public sealed class CreateItemCategoryUseCase
{
    private readonly IItemCategoryRepository _categories;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actorContext;
    private readonly ICorrelationContextAccessor _correlationContext;

    public CreateItemCategoryUseCase(
        IItemCategoryRepository categories,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actorContext,
        ICorrelationContextAccessor correlationContext)
    {
        _categories = categories;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actorContext = actorContext;
        _correlationContext = correlationContext;
    }

    public Task<CatalogMutationResult> ExecuteAsync(
        CreateItemCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        CatalogItemMutationWorkflow.EnsureCatalogManageAuthorized(_actorContext.Current, command.OrganizationId);
        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var actor = _actorContext.Current;
            var correlation = _correlationContext.Current;
            var scope = $"catalog.category.create:{command.OrganizationId}";
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
                        "The prior completed category cannot be reconstructed safely.");
                }

                var replay = await _categories.GetAsync(command.OrganizationId, replayId, ct)
                    ?? throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict,
                        "idempotency.missing_completed_resource",
                        "The prior completed category no longer exists.");
                return new CatalogMutationResult(replay.Id, replay.Version, true);
            }

            CatalogItemMutationWorkflow.HandleReservationConflict(reservation);
            await _unitOfWork.SaveChangesAsync(ct);

            if (await _categories.CodeExistsAsync(command.OrganizationId, command.Code, null, ct))
            {
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Conflict,
                    "catalog.category_code_exists",
                    "A category with the same code already exists.",
                    conflictType: "duplicate_code");
            }

            ItemCategory category;
            try
            {
                category = ItemCategory.Create(Guid.NewGuid(), command.OrganizationId, command.Code, command.Name);
            }
            catch (DomainRuleException ex)
            {
                throw new ApplicationProblemException(ApplicationProblemKind.Validation, ex.Code, ex.Message);
            }

            await _categories.AddAsync(category, ct);
            await AppendCategoryEvidenceAsync(
                category,
                "catalog.category.created",
                "created",
                actor,
                correlation,
                now,
                ct);

            await _idempotency.CompleteAsync(
                new IdempotencyCompletion(
                    scope,
                    command.IdempotencyKey,
                    command.RequestHash,
                    "catalog.category.created",
                    "ItemCategory",
                    category.Id.ToString(),
                    correlation.CorrelationId,
                    now),
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return new CatalogMutationResult(category.Id, category.Version, false);
        }, cancellationToken);
    }

    private async Task AppendCategoryEvidenceAsync(
        ItemCategory category,
        string auditEventName,
        string changeType,
        ActorContext actor,
        CorrelationContext correlation,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await _audit.AppendAsync(
            new AuditEvent(
                Guid.NewGuid(),
                now,
                auditEventName,
                actor.ActorId,
                category.OrganizationId,
                null,
                null,
                "ItemCategory",
                category.Id.ToString(),
                AuditOutcome.Succeeded,
                correlation.CorrelationId,
                correlation.CausationId,
                new Dictionary<string, string?>
                {
                    ["code"] = category.Code,
                    ["version"] = category.Version.ToString(),
                    ["active"] = category.Active.ToString()
                }),
            cancellationToken);

        await _outbox.EnqueueAsync(
            new CatalogChangedIntegrationEvent(
                Guid.NewGuid(),
                now,
                category.Id,
                category.OrganizationId,
                "ItemCategory",
                changeType),
            new OutboxContext(correlation.CorrelationId, correlation.CausationId, category.OrganizationId, actor.ActorId),
            cancellationToken);
    }
}

public sealed class UpdateItemCategoryUseCase
{
    private readonly IItemCategoryRepository _categories;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actorContext;
    private readonly ICorrelationContextAccessor _correlationContext;

    public UpdateItemCategoryUseCase(
        IItemCategoryRepository categories,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actorContext,
        ICorrelationContextAccessor correlationContext)
    {
        _categories = categories;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actorContext = actorContext;
        _correlationContext = correlationContext;
    }

    public Task<CatalogMutationResult> ExecuteAsync(
        UpdateItemCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        CatalogItemMutationWorkflow.EnsureCatalogManageAuthorized(_actorContext.Current, command.OrganizationId);
        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var actor = _actorContext.Current;
            var correlation = _correlationContext.Current;
            var scope = $"catalog.category.update:{command.OrganizationId}:{command.CategoryId:N}";
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
                var replay = await _categories.GetAsync(command.OrganizationId, command.CategoryId, ct)
                    ?? throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict,
                        "idempotency.missing_completed_resource",
                        "The prior completed category no longer exists.");
                return new CatalogMutationResult(replay.Id, replay.Version, true);
            }

            CatalogItemMutationWorkflow.HandleReservationConflict(reservation);
            await _unitOfWork.SaveChangesAsync(ct);

            var category = await _categories.GetAsync(command.OrganizationId, command.CategoryId, ct)
                ?? throw new ApplicationProblemException(
                    ApplicationProblemKind.NotFound,
                    "catalog.category_not_found",
                    "The requested category was not found.");

            var code = string.IsNullOrWhiteSpace(command.Code) ? category.Code : command.Code;
            if (!string.Equals(code.Trim(), category.Code, StringComparison.OrdinalIgnoreCase)
                && await _categories.CodeExistsAsync(command.OrganizationId, code, category.Id, ct))
            {
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Conflict,
                    "catalog.category_code_exists",
                    "A category with the same code already exists.",
                    conflictType: "duplicate_code");
            }

            try
            {
                category.Update(
                    code,
                    string.IsNullOrWhiteSpace(command.Name) ? category.Name : command.Name,
                    command.Active ?? category.Active,
                    command.ExpectedVersion);
            }
            catch (DomainRuleException ex)
            {
                throw CatalogItemMutationWorkflow.MapDomainRule(ex, category.Version);
            }

            await _categories.SaveAsync(category, ct);
            await _audit.AppendAsync(
                new AuditEvent(
                    Guid.NewGuid(),
                    now,
                    "catalog.category.updated",
                    actor.ActorId,
                    command.OrganizationId,
                    null,
                    null,
                    "ItemCategory",
                    category.Id.ToString(),
                    AuditOutcome.Succeeded,
                    correlation.CorrelationId,
                    correlation.CausationId,
                    new Dictionary<string, string?>
                    {
                        ["code"] = category.Code,
                        ["version"] = category.Version.ToString(),
                        ["active"] = category.Active.ToString()
                    }),
                ct);

            await _outbox.EnqueueAsync(
                new CatalogChangedIntegrationEvent(
                    Guid.NewGuid(),
                    now,
                    category.Id,
                    command.OrganizationId,
                    "ItemCategory",
                    "updated"),
                new OutboxContext(correlation.CorrelationId, correlation.CausationId, command.OrganizationId, actor.ActorId),
                ct);

            await _idempotency.CompleteAsync(
                new IdempotencyCompletion(
                    scope,
                    command.IdempotencyKey,
                    command.RequestHash,
                    "catalog.category.updated",
                    "ItemCategory",
                    category.Id.ToString(),
                    correlation.CorrelationId,
                    now),
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return new CatalogMutationResult(category.Id, category.Version, false);
        }, cancellationToken);
    }
}
