using EFactura.Application.Catalog;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Common.Results;
using EFactura.Application.Common.Security;
using EFactura.Application.Parties;
using EFactura.Domain.Catalog;
using EFactura.Domain.Common;
using EFactura.Domain.Parties;
using EFactura.Domain.Sales;

namespace EFactura.Application.Sales;

public sealed class SaleDraftBuilder
{
    private readonly ICommercialItemRepository _items;
    private readonly IPartyRepository _parties;

    public SaleDraftBuilder(ICommercialItemRepository items, IPartyRepository parties)
    {
        _items = items;
        _parties = parties;
    }

    public async Task ValidateCustomerAsync(
        string organizationId,
        Guid? customerPartyId,
        SaleCommercialIntent intent,
        DateOnly effectiveOn,
        CancellationToken cancellationToken)
    {
        if (!customerPartyId.HasValue)
        {
            if (intent != SaleCommercialIntent.ConsumerFinal)
            {
                throw Validation("sales.customer_required", "The selected sale intent requires a customer.");
            }
            return;
        }

        var party = await _parties.GetAsync(organizationId, customerPartyId.Value, cancellationToken)
            ?? throw Validation("sales.customer_not_found", "The selected customer does not exist in this organization.");
        if (!party.Active || !party.Roles.Contains(PartyRole.Customer))
        {
            throw Validation("sales.party_not_active_customer", "The selected party is not an active customer.");
        }
    }

    public async Task<IReadOnlyCollection<SaleLine>> BuildLinesAsync(
        string organizationId,
        IReadOnlyCollection<SaleLineInput> inputs,
        CancellationToken cancellationToken)
    {
        if (inputs.Count == 0)
            throw Validation("sales.lines_required", "At least one sale line is required.");

        var result = new List<SaleLine>(inputs.Count);
        foreach (var input in inputs)
        {
            var item = await _items.GetAsync(organizationId, input.ItemId, cancellationToken)
                ?? throw Validation("sales.item_not_found", "A selected item does not exist in this organization.");
            if (!item.Active)
                throw Validation("sales.item_inactive", $"Item {item.Code} is inactive.");

            try
            {
                result.Add(SaleLine.Create(
                    Guid.NewGuid(), item.Id, item.Code, item.Name,
                    item.Kind == CommercialItemKind.Product ? SaleLineKind.Product : SaleLineKind.Service,
                    input.Quantity, input.UnitPrice, item.TaxProfileId,
                    input.ServicePerformanceScope, input.ServiceUseCountry, input.ExportServiceKind,
                    input.RecipientIsPersonAbroad, input.ExclusiveUseAbroad,
                    input.ForeignEconomicRelation, input.RecipientInstalledInFreeZone,
                    input.ProviderFromNonFreeNationalTerritory));
            }
            catch (DomainRuleException ex)
            {
                throw Validation(ex.Code, ex.Message);
            }
        }
        return result;
    }

    private static ApplicationProblemException Validation(string code, string detail) =>
        new(ApplicationProblemKind.Validation, code, detail);
}

internal static class SalesAuthorization
{
    public static void Ensure(IActorContextAccessor actors, string organizationId, string permission)
    {
        var actor = actors.Current;
        if (!actor.IsAuthenticated || !actor.HasPermission(permission))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden, "permission_denied",
                "The actor is not allowed to perform this sales operation.");
        }
        if (!actor.CompanyScopes.Contains(organizationId))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden, "organization_scope_denied",
                "The actor is outside the requested organization scope.");
        }
    }
}

public sealed class CreateSaleUseCase
{
    private readonly ISaleRepository _sales;
    private readonly SaleDraftBuilder _builder;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public CreateSaleUseCase(
        ISaleRepository sales,
        SaleDraftBuilder builder,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _sales = sales;
        _builder = builder;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<SaleMutationResult> ExecuteAsync(CreateSaleCommand command, CancellationToken cancellationToken = default)
    {
        SalesAuthorization.Ensure(_actors, command.OrganizationId, Permissions.SalesCreate);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RequestHash);

        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var scope = $"sales.create:{command.OrganizationId}";
            var reservation = await _idempotency.TryReserveAsync(
                new IdempotencyReservation(
                    scope, command.IdempotencyKey, command.RequestHash,
                    _actors.Current.ActorId, _correlations.Current.CorrelationId, now.AddMinutes(10)), ct);

            if (reservation.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                var replayId = ParseReplayId(reservation.ResourceId);
                var replayed = await _sales.GetAsync(command.OrganizationId, replayId, ct)
                    ?? throw Conflict("idempotency.missing_completed_resource", "The prior completed sale no longer exists.");
                return new SaleMutationResult(replayed.Id, replayed.Version, true);
            }
            EnsureAcquired(reservation);
            await _unitOfWork.SaveChangesAsync(ct);

            await _builder.ValidateCustomerAsync(
                command.OrganizationId, command.CustomerPartyId, command.Intent, command.EffectiveOn, ct);
            var lines = await _builder.BuildLinesAsync(command.OrganizationId, command.Lines, ct);

            Sale sale;
            try
            {
                sale = Sale.Create(
                    Guid.NewGuid(), command.OrganizationId, command.LocationId, command.TerminalId,
                    command.CustomerPartyId, command.Intent, command.CurrencyCode, command.EffectiveOn,
                    command.DeliveryCountry, command.GoodsExportConfirmed, lines);
            }
            catch (DomainRuleException ex)
            {
                throw Validation(ex.Code, ex.Message);
            }

            await _sales.AddAsync(sale, ct);
            await AppendAuditAsync("SALE_DRAFT_CREATED", sale, now, ct);
            await _outbox.EnqueueAsync(
                new SaleDraftCreatedIntegrationEvent(Guid.NewGuid(), now, sale.Id, sale.OrganizationId),
                new OutboxContext(
                    _correlations.Current.CorrelationId,
                    OrganizationId: sale.OrganizationId,
                    ActorId: _actors.Current.ActorId), ct);
            await _idempotency.CompleteAsync(
                new IdempotencyCompletion(
                    scope, command.IdempotencyKey, command.RequestHash, "sale_draft_created",
                    "Sale", sale.Id.ToString(), _correlations.Current.CorrelationId, now), ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return new SaleMutationResult(sale.Id, sale.Version, false);
        }, cancellationToken);
    }

    private static void EnsureAcquired(IdempotencyReservationResult reservation)
    {
        if (reservation.Status == IdempotencyReservationStatus.PayloadMismatch)
            throw Conflict("idempotency_key_reused", "The idempotency key was already used with a different request.");
        if (reservation.Status == IdempotencyReservationStatus.ExistingInProgress)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict, "idempotency_in_progress",
                "An operation with this idempotency key is still in progress.",
                conflictType: "in_progress", retryAfterSeconds: 2);
        }
    }

    private static Guid ParseReplayId(string? value) =>
        Guid.TryParse(value, out var id)
            ? id
            : throw Conflict("idempotency.invalid_completed_resource", "The prior completed sale cannot be reconstructed safely.");

    private Task AppendAuditAsync(string eventName, Sale sale, DateTimeOffset now, CancellationToken ct) =>
        _audit.AppendAsync(new AuditEvent(
            Guid.NewGuid(), now, eventName, _actors.Current.ActorId, sale.OrganizationId,
            sale.LocationId, sale.TerminalId, "Sale", sale.Id.ToString(), AuditOutcome.Succeeded,
            _correlations.Current.CorrelationId, null,
            new Dictionary<string, string?>
            {
                ["status"] = sale.Status.ToString(),
                ["version"] = sale.Version.ToString()
            }), ct);

    private static ApplicationProblemException Validation(string code, string detail) =>
        new(ApplicationProblemKind.Validation, code, detail);
    private static ApplicationProblemException Conflict(string code, string detail) =>
        new(ApplicationProblemKind.Conflict, code, detail);
}

public sealed class UpdateSaleDraftUseCase
{
    private readonly ISaleRepository _sales;
    private readonly SaleDraftBuilder _builder;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public UpdateSaleDraftUseCase(
        ISaleRepository sales,
        SaleDraftBuilder builder,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _sales = sales;
        _builder = builder;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<SaleMutationResult> ExecuteAsync(UpdateSaleDraftCommand command, CancellationToken cancellationToken = default)
    {
        SalesAuthorization.Ensure(_actors, command.OrganizationId, Permissions.SalesCreate);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RequestHash);

        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var scope = $"sales.update:{command.OrganizationId}:{command.SaleId}";
            var reservation = await _idempotency.TryReserveAsync(
                new IdempotencyReservation(
                    scope, command.IdempotencyKey, command.RequestHash,
                    _actors.Current.ActorId, _correlations.Current.CorrelationId, now.AddMinutes(10)), ct);

            if (reservation.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                var replayed = await _sales.GetAsync(command.OrganizationId, command.SaleId, ct)
                    ?? throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict, "idempotency.missing_completed_resource",
                        "The prior completed sale no longer exists.");
                return new SaleMutationResult(replayed.Id, replayed.Version, true);
            }
            if (reservation.Status != IdempotencyReservationStatus.Acquired)
            {
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Conflict,
                    reservation.Status == IdempotencyReservationStatus.PayloadMismatch
                        ? "idempotency_key_reused" : "idempotency_in_progress",
                    "The update idempotency reservation could not be acquired.");
            }

            await _unitOfWork.SaveChangesAsync(ct);
            var sale = await _sales.GetAsync(command.OrganizationId, command.SaleId, ct)
                ?? throw new ApplicationProblemException(ApplicationProblemKind.NotFound, "sales.not_found", "Sale was not found.");

            await _builder.ValidateCustomerAsync(
                command.OrganizationId, command.CustomerPartyId, command.Intent, command.EffectiveOn, ct);
            var lines = await _builder.BuildLinesAsync(command.OrganizationId, command.Lines, ct);

            try
            {
                sale.ReplaceDraft(
                    command.CustomerPartyId, command.Intent, command.CurrencyCode, command.EffectiveOn,
                    command.DeliveryCountry, command.GoodsExportConfirmed, lines, command.ExpectedVersion);
            }
            catch (DomainRuleException ex)
            {
                var kind = ex.Code == "concurrency.stale_version"
                    ? ApplicationProblemKind.Conflict : ApplicationProblemKind.Validation;
                throw new ApplicationProblemException(
                    kind, ex.Code, ex.Message,
                    conflictType: kind == ApplicationProblemKind.Conflict ? "stale_version" : null);
            }

            await _sales.SaveAsync(sale, ct);
            await _audit.AppendAsync(new AuditEvent(
                Guid.NewGuid(), now, "SALE_DRAFT_UPDATED", _actors.Current.ActorId,
                sale.OrganizationId, sale.LocationId, sale.TerminalId, "Sale", sale.Id.ToString(),
                AuditOutcome.Succeeded, _correlations.Current.CorrelationId, null,
                new Dictionary<string, string?> { ["version"] = sale.Version.ToString() }), ct);
            await _outbox.EnqueueAsync(
                new SaleDraftUpdatedIntegrationEvent(Guid.NewGuid(), now, sale.Id, sale.OrganizationId),
                new OutboxContext(
                    _correlations.Current.CorrelationId,
                    OrganizationId: sale.OrganizationId,
                    ActorId: _actors.Current.ActorId), ct);
            await _idempotency.CompleteAsync(new IdempotencyCompletion(
                scope, command.IdempotencyKey, command.RequestHash, "sale_draft_updated",
                "Sale", sale.Id.ToString(), _correlations.Current.CorrelationId, now), ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return new SaleMutationResult(sale.Id, sale.Version, false);
        }, cancellationToken);
    }
}

public sealed class GetSaleUseCase
{
    private readonly ISaleRepository _sales;
    private readonly IActorContextAccessor _actors;

    public GetSaleUseCase(ISaleRepository sales, IActorContextAccessor actors)
    {
        _sales = sales;
        _actors = actors;
    }

    public async Task<SaleView> ExecuteAsync(
        string organizationId, Guid saleId, CancellationToken cancellationToken = default)
    {
        SalesAuthorization.Ensure(_actors, organizationId, Permissions.SalesRead);
        var sale = await _sales.GetAsync(organizationId, saleId, cancellationToken)
            ?? throw new ApplicationProblemException(ApplicationProblemKind.NotFound, "sales.not_found", "Sale was not found.");
        return SaleView.FromDomain(sale);
    }
}

public sealed class ListSalesUseCase
{
    private readonly ISaleRepository _sales;
    private readonly IActorContextAccessor _actors;

    public ListSalesUseCase(ISaleRepository sales, IActorContextAccessor actors)
    {
        _sales = sales;
        _actors = actors;
    }

    public async Task<PageResult<SaleView>> ExecuteAsync(
        SaleSearchRequest request, CancellationToken cancellationToken = default)
    {
        SalesAuthorization.Ensure(_actors, request.OrganizationId, Permissions.SalesRead);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);
        var result = await _sales.SearchAsync(
            request with { Page = page, PageSize = pageSize }, cancellationToken);
        return new PageResult<SaleView>(
            result.Items.Select(SaleView.FromDomain).ToArray(), page, pageSize, result.Total);
    }
}
