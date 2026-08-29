using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Catalog;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Common.Results;
using EFactura.Application.Common.Security;
using EFactura.Application.Fiscal;
using EFactura.Application.Parties;
using EFactura.Application.Taxation;
using EFactura.Domain.Catalog;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Parties;
using EFactura.Domain.Sales;
using EFactura.Domain.Taxation;

namespace EFactura.Application.Sales;

public sealed record SaleLineInput(
    Guid ItemId,
    decimal Quantity,
    decimal UnitPrice,
    SaleServicePerformanceScope ServicePerformanceScope = SaleServicePerformanceScope.UnknownOrMixed,
    string? ServiceUseCountry = null,
    SaleExportServiceKind ExportServiceKind = SaleExportServiceKind.None,
    SaleRegulatoryFactStatus RecipientIsPersonAbroad = SaleRegulatoryFactStatus.Unknown,
    SaleRegulatoryFactStatus ExclusiveUseAbroad = SaleRegulatoryFactStatus.Unknown,
    SaleRegulatoryFactStatus ForeignEconomicRelation = SaleRegulatoryFactStatus.Unknown,
    SaleRegulatoryFactStatus RecipientInstalledInFreeZone = SaleRegulatoryFactStatus.Unknown,
    SaleRegulatoryFactStatus ProviderFromNonFreeNationalTerritory = SaleRegulatoryFactStatus.Unknown);

public sealed record CreateSaleCommand(
    string OrganizationId,
    string? LocationId,
    string? TerminalId,
    Guid? CustomerPartyId,
    SaleCommercialIntent Intent,
    string CurrencyCode,
    DateOnly EffectiveOn,
    string? DeliveryCountry,
    bool GoodsExportConfirmed,
    IReadOnlyCollection<SaleLineInput> Lines,
    string IdempotencyKey,
    string RequestHash);

public sealed record UpdateSaleDraftCommand(
    string OrganizationId,
    Guid SaleId,
    long ExpectedVersion,
    Guid? CustomerPartyId,
    SaleCommercialIntent Intent,
    string CurrencyCode,
    DateOnly EffectiveOn,
    string? DeliveryCountry,
    bool GoodsExportConfirmed,
    IReadOnlyCollection<SaleLineInput> Lines,
    string IdempotencyKey,
    string RequestHash);

public sealed record ValidateSaleCommand(
    string OrganizationId,
    Guid SaleId,
    long ExpectedVersion,
    string IdempotencyKey,
    string RequestHash);

public sealed record SaleSearchRequest(
    string OrganizationId,
    DateOnly? From,
    DateOnly? To,
    Guid? CustomerPartyId,
    SaleStatus? Status,
    int Page = 1,
    int PageSize = 50);

public sealed record SaleLineView(
    Guid Id,
    Guid ItemId,
    string ItemCode,
    string ItemName,
    SaleLineKind Kind,
    decimal Quantity,
    decimal UnitPrice,
    decimal NetAmount,
    Guid? TaxProfileId);

public sealed record SaleView(
    Guid Id,
    long Version,
    SaleStatus Status,
    string OrganizationId,
    string? LocationId,
    string? TerminalId,
    Guid? CustomerPartyId,
    SaleCommercialIntent Intent,
    string CurrencyCode,
    DateOnly EffectiveOn,
    string? DeliveryCountry,
    bool GoodsExportConfirmed,
    decimal NetAmount,
    string? ValidationFingerprint,
    DateTimeOffset? ValidatedAtUtc,
    IReadOnlyCollection<SaleLineView> Lines)
{
    public static SaleView FromDomain(Sale sale) => new(
        sale.Id,
        sale.Version,
        sale.Status,
        sale.OrganizationId,
        sale.LocationId,
        sale.TerminalId,
        sale.CustomerPartyId,
        sale.Intent,
        sale.CurrencyCode,
        sale.EffectiveOn,
        sale.DeliveryCountry,
        sale.GoodsExportConfirmed,
        sale.NetAmount,
        sale.ValidationFingerprint,
        sale.ValidatedAtUtc,
        sale.Lines.Select(line => new SaleLineView(
            line.Id,
            line.ItemId,
            line.ItemCode,
            line.ItemName,
            line.Kind,
            line.Quantity,
            line.UnitPrice,
            line.NetAmount,
            line.TaxProfileId)).ToArray());
}

public sealed record SaleFiscalPreviewLineView(
    Guid LineId,
    string ItemCode,
    decimal NetAmount,
    TaxDecisionStatus TaxTreatmentStatus,
    TaxTreatmentClassification TaxTreatment,
    string TreatmentCode,
    TaxRateResolutionStatus TaxRateStatus,
    VatLiabilityKind VatLiability,
    VatRateKind VatRateKind,
    decimal? AppliedRatePercent,
    decimal? TaxAmount,
    IReadOnlyCollection<string> Reasons,
    IReadOnlyCollection<string> MissingFacts,
    IReadOnlyCollection<RegulatoryRuleEvidence> RuleEvidence);

public sealed record SaleFiscalPreviewView(
    Guid SaleId,
    long SaleVersion,
    string CurrencyCode,
    decimal NetAmount,
    decimal? TaxAmount,
    decimal? TotalAmount,
    IReadOnlyCollection<SaleFiscalPreviewLineView> Lines,
    TaxTreatmentDecision OverallTaxTreatment,
    CfeEligibilityResult CfeEligibility,
    CfeSelectionResult CfeSelection,
    bool ReadyForConfirmation,
    string ValidationFingerprint,
    IReadOnlyCollection<string> Findings);

public sealed record SaleMutationResult(Guid SaleId, long Version, bool Replayed);
public sealed record SaleValidationResult(bool Valid, SaleView Sale, SaleFiscalPreviewView Preview, bool Replayed);

public interface ISaleRepository
{
    Task AddAsync(Sale sale, CancellationToken cancellationToken = default);
    Task<Sale?> GetAsync(string organizationId, Guid saleId, CancellationToken cancellationToken = default);
    Task<PageResult<Sale>> SearchAsync(SaleSearchRequest request, CancellationToken cancellationToken = default);
}

public interface IUiAmountConverter
{
    Task<decimal?> TryConvertToUiAsync(
        string currencyCode,
        decimal amount,
        DateOnly effectiveOn,
        CancellationToken cancellationToken = default);
}

public sealed class Release1UiAmountConverter : IUiAmountConverter
{
    public Task<decimal?> TryConvertToUiAsync(
        string currencyCode,
        decimal amount,
        DateOnly effectiveOn,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<decimal?>(
            string.Equals(currencyCode, "UI", StringComparison.OrdinalIgnoreCase) ? amount : null);
    }
}

public sealed record SaleDraftCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid SaleId,
    string OrganizationId) : IIntegrationEvent;

public sealed record SaleDraftUpdatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid SaleId,
    string OrganizationId) : IIntegrationEvent;

public sealed record SaleValidatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid SaleId,
    string OrganizationId,
    string ValidationFingerprint) : IIntegrationEvent;

public sealed class CreateSaleUseCase
{
    private readonly ISaleRepository _sales;
    private readonly ICommercialItemRepository _items;
    private readonly IPartyRepository _parties;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public CreateSaleUseCase(
        ISaleRepository sales,
        ICommercialItemRepository items,
        IPartyRepository parties,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _sales = sales;
        _items = items;
        _parties = parties;
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
        EnsureAuthorized(command.OrganizationId, Permissions.SalesCreate);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RequestHash);

        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var scope = $"sales.create:{command.OrganizationId}";
            var reservation = await ReserveAsync(scope, command.IdempotencyKey, command.RequestHash, now, ct);
            if (reservation.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                var replayId = ParseReplayId(reservation.ResourceId);
                var replayed = await _sales.GetAsync(command.OrganizationId, replayId, ct)
                    ?? throw Conflict("idempotency.missing_completed_resource", "The prior completed sale no longer exists.");
                return new SaleMutationResult(replayed.Id, replayed.Version, true);
            }
            EnsureReservationAcquired(reservation);
            await _unitOfWork.SaveChangesAsync(ct);

            await ValidateCustomerAsync(command.OrganizationId, command.CustomerPartyId, command.Intent, ct);
            var lines = await BuildLinesAsync(command.OrganizationId, command.Lines, ct);

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
                throw Validation(ex);
            }

            await _sales.AddAsync(sale, ct);
            await AppendAuditAsync("SALE_DRAFT_CREATED", sale, now, ct);
            await _outbox.EnqueueAsync(
                new SaleDraftCreatedIntegrationEvent(Guid.NewGuid(), now, sale.Id, sale.OrganizationId),
                OutboxContext(), ct);
            await _idempotency.CompleteAsync(
                new IdempotencyCompletion(scope, command.IdempotencyKey, command.RequestHash,
                    "sale_draft_created", "Sale", sale.Id.ToString(), _correlations.Current.CorrelationId, now), ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return new SaleMutationResult(sale.Id, sale.Version, false);
        }, cancellationToken);
    }

    private async Task<IReadOnlyCollection<SaleLine>> BuildLinesAsync(
        string organizationId,
        IReadOnlyCollection<SaleLineInput> inputs,
        CancellationToken cancellationToken)
    {
        if (inputs.Count == 0)
        {
            throw new ApplicationProblemException(ApplicationProblemKind.Validation, "sales.lines_required", "At least one sale line is required.");
        }

        var result = new List<SaleLine>(inputs.Count);
        foreach (var input in inputs)
        {
            var item = await _items.GetAsync(organizationId, input.ItemId, cancellationToken)
                ?? throw new ApplicationProblemException(ApplicationProblemKind.Validation, "sales.item_not_found", "A selected item does not exist in this organization.");
            if (!item.Active)
            {
                throw new ApplicationProblemException(ApplicationProblemKind.Validation, "sales.item_inactive", $"Item {item.Code} is inactive.");
            }

            try
            {
                result.Add(SaleLine.Create(
                    Guid.NewGuid(), item.Id, item.Code, item.Name,
                    item.Kind == CommercialItemKind.Product ? SaleLineKind.Product : SaleLineKind.Service,
                    input.Quantity, input.UnitPrice, item.TaxProfileId,
                    input.ServicePerformanceScope, input.ServiceUseCountry, input.ExportServiceKind,
                    input.RecipientIsPersonAbroad, input.ExclusiveUseAbroad, input.ForeignEconomicRelation,
                    input.RecipientInstalledInFreeZone, input.ProviderFromNonFreeNationalTerritory));
            }
            catch (DomainRuleException ex)
            {
                throw Validation(ex);
            }
        }
        return result;
    }

    private async Task ValidateCustomerAsync(
        string organizationId,
        Guid? customerPartyId,
        SaleCommercialIntent intent,
        CancellationToken cancellationToken)
    {
        if (!customerPartyId.HasValue)
        {
            if (intent != SaleCommercialIntent.ConsumerFinal)
            {
                throw new ApplicationProblemException(ApplicationProblemKind.Validation, "sales.customer_required", "The selected sale intent requires a customer.");
            }
            return;
        }

        var party = await _parties.GetAsync(organizationId, customerPartyId.Value, cancellationToken)
            ?? throw new ApplicationProblemException(ApplicationProblemKind.Validation, "sales.customer_not_found", "The selected customer does not exist in this organization.");
        if (!party.Active || !party.Roles.Contains(PartyRole.Customer))
        {
            throw new ApplicationProblemException(ApplicationProblemKind.Validation, "sales.party_not_active_customer", "The selected party is not an active customer.");
        }
    }

    private Task<IdempotencyReservationResult> ReserveAsync(
        string scope, string key, string requestHash, DateTimeOffset now, CancellationToken cancellationToken) =>
        _idempotency.TryReserveAsync(new IdempotencyReservation(
            scope, key, requestHash, _actors.Current.ActorId, _correlations.Current.CorrelationId, now.AddMinutes(10)), cancellationToken);

    private static void EnsureReservationAcquired(IdempotencyReservationResult reservation)
    {
        if (reservation.Status == IdempotencyReservationStatus.PayloadMismatch)
            throw Conflict("idempotency_key_reused", "The idempotency key was already used with a different request.");
        if (reservation.Status == IdempotencyReservationStatus.ExistingInProgress)
            throw new ApplicationProblemException(ApplicationProblemKind.Conflict, "idempotency_in_progress", "An operation with this idempotency key is still in progress.", conflictType: "in_progress", retryAfterSeconds: 2);
    }

    private static Guid ParseReplayId(string? value) =>
        Guid.TryParse(value, out var id) ? id : throw Conflict("idempotency.invalid_completed_resource", "The prior completed sale cannot be reconstructed safely.");

    private async Task AppendAuditAsync(string eventName, Sale sale, DateTimeOffset now, CancellationToken ct) =>
        await _audit.AppendAsync(new AuditEvent(
            Guid.NewGuid(), now, eventName, _actors.Current.ActorId, sale.OrganizationId,
            sale.LocationId, sale.TerminalId, "Sale", sale.Id.ToString(), AuditOutcome.Succeeded,
            _correlations.Current.CorrelationId, null,
            new Dictionary<string, string?> { ["status"] = sale.Status.ToString(), ["version"] = sale.Version.ToString() }), ct);

    private OutboxContext OutboxContext() => new(
        _correlations.Current.CorrelationId, organizationId: _actors.Current.CompanyScopes.Count == 1 ? _actors.Current.CompanyScopes.Single() : null,
        actorId: _actors.Current.ActorId);

    private void EnsureAuthorized(string organizationId, string permission)
    {
        var actor = _actors.Current;
        if (!actor.IsAuthenticated || !actor.HasPermission(permission))
            throw new ApplicationProblemException(ApplicationProblemKind.Forbidden, "permission_denied", "The actor is not allowed to perform this sales operation.");
        if (!actor.CompanyScopes.Contains(organizationId))
            throw new ApplicationProblemException(ApplicationProblemKind.Forbidden, "organization_scope_denied", "The actor is outside the requested organization scope.");
    }

    private static ApplicationProblemException Validation(DomainRuleException ex) =>
        new(ApplicationProblemKind.Validation, ex.Code, ex.Message);
    private static ApplicationProblemException Conflict(string code, string detail) =>
        new(ApplicationProblemKind.Conflict, code, detail);
}

public sealed class UpdateSaleDraftUseCase
{
    private readonly ISaleRepository _sales;
    private readonly CreateSaleUseCase _createHelper;
    private readonly ICommercialItemRepository _items;
    private readonly IPartyRepository _parties;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public UpdateSaleDraftUseCase(
        ISaleRepository sales,
        CreateSaleUseCase createHelper,
        ICommercialItemRepository items,
        IPartyRepository parties,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _sales = sales;
        _createHelper = createHelper;
        _items = items;
        _parties = parties;
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
        EnsureAuthorized(command.OrganizationId);
        return _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var scope = $"sales.update:{command.OrganizationId}:{command.SaleId}";
            var reservation = await _idempotency.TryReserveAsync(new IdempotencyReservation(
                scope, command.IdempotencyKey, command.RequestHash, _actors.Current.ActorId,
                _correlations.Current.CorrelationId, now.AddMinutes(10)), ct);
            if (reservation.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                var replayed = await _sales.GetAsync(command.OrganizationId, command.SaleId, ct)
                    ?? throw new ApplicationProblemException(ApplicationProblemKind.Conflict, "idempotency.missing_completed_resource", "The prior completed sale no longer exists.");
                return new SaleMutationResult(replayed.Id, replayed.Version, true);
            }
            if (reservation.Status != IdempotencyReservationStatus.Acquired)
                throw new ApplicationProblemException(ApplicationProblemKind.Conflict, "idempotency_conflict", "The update idempotency reservation could not be acquired.");
            await _unitOfWork.SaveChangesAsync(ct);

            var sale = await _sales.GetAsync(command.OrganizationId, command.SaleId, ct)
                ?? throw new ApplicationProblemException(ApplicationProblemKind.NotFound, "sales.not_found", "Sale was not found.");

            if (command.CustomerPartyId.HasValue)
            {
                var party = await _parties.GetAsync(command.OrganizationId, command.CustomerPartyId.Value, ct)
                    ?? throw new ApplicationProblemException(ApplicationProblemKind.Validation, "sales.customer_not_found", "The selected customer does not exist.");
                if (!party.Active || !party.Roles.Contains(PartyRole.Customer))
                    throw new ApplicationProblemException(ApplicationProblemKind.Validation, "sales.party_not_active_customer", "The selected party is not an active customer.");
            }
            else if (command.Intent != SaleCommercialIntent.ConsumerFinal)
            {
                throw new ApplicationProblemException(ApplicationProblemKind.Validation, "sales.customer_required", "The selected sale intent requires a customer.");
            }

            var lines = new List<SaleLine>();
            foreach (var input in command.Lines)
            {
                var item = await _items.GetAsync(command.OrganizationId, input.ItemId, ct)
                    ?? throw new ApplicationProblemException(ApplicationProblemKind.Validation, "sales.item_not_found", "A selected item does not exist.");
                if (!item.Active)
                    throw new ApplicationProblemException(ApplicationProblemKind.Validation, "sales.item_inactive", $"Item {item.Code} is inactive.");
                try
                {
                    lines.Add(SaleLine.Create(
                        Guid.NewGuid(), item.Id, item.Code, item.Name,
                        item.Kind == CommercialItemKind.Product ? SaleLineKind.Product : SaleLineKind.Service,
                        input.Quantity, input.UnitPrice, item.TaxProfileId,
                        input.ServicePerformanceScope, input.ServiceUseCountry, input.ExportServiceKind,
                        input.RecipientIsPersonAbroad, input.ExclusiveUseAbroad, input.ForeignEconomicRelation,
                        input.RecipientInstalledInFreeZone, input.ProviderFromNonFreeNationalTerritory));
                }
                catch (DomainRuleException ex)
                {
                    throw new ApplicationProblemException(ApplicationProblemKind.Validation, ex.Code, ex.Message);
                }
            }

            try
            {
                sale.ReplaceDraft(
                    command.CustomerPartyId, command.Intent, command.CurrencyCode, command.EffectiveOn,
                    command.DeliveryCountry, command.GoodsExportConfirmed, lines, command.ExpectedVersion);
            }
            catch (DomainRuleException ex)
            {
                var kind = ex.Code == "concurrency.stale_version" ? ApplicationProblemKind.Conflict : ApplicationProblemKind.Validation;
                throw new ApplicationProblemException(kind, ex.Code, ex.Message, conflictType: kind == ApplicationProblemKind.Conflict ? "stale_version" : null);
            }

            await _audit.AppendAsync(new AuditEvent(
                Guid.NewGuid(), now, "SALE_DRAFT_UPDATED", _actors.Current.ActorId, sale.OrganizationId,
                sale.LocationId, sale.TerminalId, "Sale", sale.Id.ToString(), AuditOutcome.Succeeded,
                _correlations.Current.CorrelationId, null,
                new Dictionary<string, string?> { ["version"] = sale.Version.ToString() }), ct);
            await _outbox.EnqueueAsync(
                new SaleDraftUpdatedIntegrationEvent(Guid.NewGuid(), now, sale.Id, sale.OrganizationId),
                new OutboxContext(_correlations.Current.CorrelationId, organizationId: sale.OrganizationId, actorId: _actors.Current.ActorId), ct);
            await _idempotency.CompleteAsync(new IdempotencyCompletion(
                scope, command.IdempotencyKey, command.RequestHash, "sale_draft_updated", "Sale",
                sale.Id.ToString(), _correlations.Current.CorrelationId, now), ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return new SaleMutationResult(sale.Id, sale.Version, false);
        }, cancellationToken);
    }

    private void EnsureAuthorized(string organizationId)
    {
        var actor = _actors.Current;
        if (!actor.IsAuthenticated || !actor.HasPermission(Permissions.SalesCreate))
            throw new ApplicationProblemException(ApplicationProblemKind.Forbidden, "permission_denied", "The actor cannot update sale drafts.");
        if (!actor.CompanyScopes.Contains(organizationId))
            throw new ApplicationProblemException(ApplicationProblemKind.Forbidden, "organization_scope_denied", "The actor is outside the requested organization scope.");
    }
}

public sealed class GetSaleFiscalPreviewUseCase
{
    private readonly ISaleRepository _sales;
    private readonly IPartyRepository _parties;
    private readonly ResolveTaxTreatmentUseCase _taxTreatment;
    private readonly ResolveTaxRateUseCase _taxRate;
    private readonly PrepareCfeEligibilityUseCase _eligibility;
    private readonly SelectCfeUseCase _selector;
    private readonly IUiAmountConverter _uiAmount;
    private readonly IActorContextAccessor _actors;

    public GetSaleFiscalPreviewUseCase(
        ISaleRepository sales,
        IPartyRepository parties,
        ResolveTaxTreatmentUseCase taxTreatment,
        ResolveTaxRateUseCase taxRate,
        PrepareCfeEligibilityUseCase eligibility,
        SelectCfeUseCase selector,
        IUiAmountConverter uiAmount,
        IActorContextAccessor actors)
    {
        _sales = sales;
        _parties = parties;
        _taxTreatment = taxTreatment;
        _taxRate = taxRate;
        _eligibility = eligibility;
        _selector = selector;
        _uiAmount = uiAmount;
        _actors = actors;
    }

    public async Task<SaleFiscalPreviewView> ExecuteAsync(
        string organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        EnsureReadAuthorized(organizationId);
        var sale = await _sales.GetAsync(organizationId, saleId, cancellationToken)
            ?? throw new ApplicationProblemException(ApplicationProblemKind.NotFound, "sales.not_found", "Sale was not found.");
        var receiver = await BuildReceiverAsync(sale, cancellationToken);
        var previews = new List<SaleFiscalPreviewLineView>();
        var decisions = new List<TaxTreatmentDecision>();
        decimal totalTax = 0m;
        var allTaxAmountsResolved = true;

        foreach (var line in sale.Lines)
        {
            var decision = await _taxTreatment.ExecuteAsync(BuildTreatmentRequest(sale, line, receiver), cancellationToken);
            decisions.Add(decision);
            var rate = await _taxRate.ExecuteAsync(
                new ResolveTaxRateRequest(sale.OrganizationId, sale.EffectiveOn, decision, line.TaxProfileId),
                cancellationToken);

            decimal? taxAmount = null;
            if (rate.Status == TaxRateResolutionStatus.Resolved && rate.AppliedRatePercent.HasValue)
            {
                taxAmount = decimal.Round(
                    line.NetAmount * rate.AppliedRatePercent.Value / 100m,
                    2,
                    MidpointRounding.AwayFromZero);
                totalTax += taxAmount.Value;
            }
            else
            {
                allTaxAmountsResolved = false;
            }

            previews.Add(new SaleFiscalPreviewLineView(
                line.Id, line.ItemCode, line.NetAmount,
                decision.Status, decision.Classification, decision.TreatmentCode,
                rate.Status, rate.Liability, rate.RateKind, rate.AppliedRatePercent, taxAmount,
                decision.Reasons.Concat(rate.Reasons).Distinct().ToArray(),
                decision.MissingFacts.Concat(rate.MissingFacts).Distinct().ToArray(),
                decision.RuleEvidence.Concat(rate.RuleEvidence).DistinctBy(x => x.RuleId).ToArray()));
        }

        var overall = Combine(decisions);
        var netUi = await _uiAmount.TryConvertToUiAsync(sale.CurrencyCode, sale.NetAmount, sale.EffectiveOn, cancellationToken);
        var eligibility = await _eligibility.ExecuteAsync(new PrepareCfeEligibilityRequest(
            sale.EffectiveOn,
            overall,
            receiver,
            MapIntent(sale.Intent),
            netUi,
            HasRetentionsOrPerceptions: false), cancellationToken);
        var selection = await _selector.ExecuteAsync(new SelectCfeRequest(
            sale.OrganizationId, sale.EffectiveOn, overall, eligibility), cancellationToken);

        var findings = previews.SelectMany(x => x.MissingFacts)
            .Concat(eligibility.MissingFacts)
            .Concat(selection.MissingFacts)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var ready = previews.All(x => x.TaxTreatmentStatus == TaxDecisionStatus.Resolved
                                      && x.TaxRateStatus == TaxRateResolutionStatus.Resolved)
                    && selection.Status == CfeSelectionStatus.Selected;
        var tax = allTaxAmountsResolved ? totalTax : null;
        var total = tax.HasValue ? decimal.Round(sale.NetAmount + tax.Value, 2, MidpointRounding.AwayFromZero) : null;
        var fingerprint = Fingerprint(sale, previews, overall, selection);

        return new SaleFiscalPreviewView(
            sale.Id, sale.Version, sale.CurrencyCode, sale.NetAmount, tax, total,
            previews, overall, eligibility, selection, ready, fingerprint, findings);
    }

    private async Task<ReceiverTaxFacts> BuildReceiverAsync(Sale sale, CancellationToken cancellationToken)
    {
        if (!sale.CustomerPartyId.HasValue)
        {
            return new ReceiverTaxFacts("UY", "UY");
        }

        var party = await _parties.GetAsync(sale.OrganizationId, sale.CustomerPartyId.Value, cancellationToken)
            ?? throw new ApplicationProblemException(ApplicationProblemKind.Validation, "sales.customer_not_found", "Sale customer no longer exists.");
        var identities = party.FiscalIdentities
            .Where(identity => identity.Active
                               && (!identity.ValidFrom.HasValue || identity.ValidFrom.Value <= sale.EffectiveOn)
                               && (!identity.ValidTo.HasValue || sale.EffectiveOn <= identity.ValidTo.Value))
            .Select(identity => new ReceiverFiscalIdentityFact(identity.TypeCode, identity.IssuingCountry))
            .ToArray();
        return new ReceiverTaxFacts(party.ResidenceCountry, party.TaxResidenceCountry, identities);
    }

    private static ResolveTaxTreatmentRequest BuildTreatmentRequest(
        Sale sale,
        SaleLine line,
        ReceiverTaxFacts receiver)
    {
        var kind = line.Kind == SaleLineKind.Product ? TaxOperationKind.Goods : TaxOperationKind.Services;
        var goodsScope = line.Kind == SaleLineKind.Product
            ? sale.GoodsExportConfirmed
                ? GoodsMovementScope.ExportConfirmed
                : sale.Intent == SaleCommercialIntent.Export || (sale.DeliveryCountry is not null && sale.DeliveryCountry != "UY")
                    ? GoodsMovementScope.Unknown
                    : GoodsMovementScope.DomesticDelivery
            : GoodsMovementScope.Unknown;
        var serviceScope = line.ServicePerformanceScope switch
        {
            SaleServicePerformanceScope.EntirelyInUruguay => ServicePerformanceScope.EntirelyInUruguay,
            SaleServicePerformanceScope.EntirelyOutsideUruguay => ServicePerformanceScope.EntirelyOutsideUruguay,
            _ => ServicePerformanceScope.UnknownOrMixed
        };

        ExportServiceEvaluationContext? exportContext = null;
        if (line.Kind == SaleLineKind.Service && line.ExportServiceKind != SaleExportServiceKind.None)
        {
            exportContext = new ExportServiceEvaluationContext(
                ExportServiceRuleFamily.Article34Numeral11,
                new Article34Numeral11Facts(
                    MapServiceKind(line.ExportServiceKind),
                    MapFact(line.RecipientIsPersonAbroad),
                    MapFact(line.ExclusiveUseAbroad),
                    MapFact(line.ForeignEconomicRelation),
                    MapFact(line.RecipientInstalledInFreeZone),
                    MapFact(line.ProviderFromNonFreeNationalTerritory)));
        }

        return new ResolveTaxTreatmentRequest(
            sale.OrganizationId,
            sale.EffectiveOn,
            kind,
            receiver,
            goodsScope,
            serviceScope,
            sale.DeliveryCountry,
            line.ServiceUseCountry,
            ExportServiceContext: exportContext);
    }

    private static TaxTreatmentDecision Combine(IReadOnlyCollection<TaxTreatmentDecision> decisions)
    {
        if (decisions.Count == 0)
        {
            return new TaxTreatmentDecision(
                TaxDecisionStatus.RequiresReview,
                TaxTreatmentClassification.RequiresReview,
                "REQUIRES_REVIEW",
                new[] { "sales.preview.no_tax_decisions" },
                new[] { "sale_lines" },
                Array.Empty<RegulatoryRuleEvidence>(),
                "MULTI");
        }

        if (decisions.Any(x => x.Status == TaxDecisionStatus.RequiresReview))
        {
            return new TaxTreatmentDecision(
                TaxDecisionStatus.RequiresReview,
                TaxTreatmentClassification.RequiresReview,
                "REQUIRES_REVIEW",
                decisions.SelectMany(x => x.Reasons).Distinct().ToArray(),
                decisions.SelectMany(x => x.MissingFacts).Distinct().ToArray(),
                decisions.SelectMany(x => x.RuleEvidence).DistinctBy(x => x.RuleId).ToArray(),
                string.Join("+", decisions.Select(x => x.RulePackVersion).Distinct()));
        }

        var classifications = decisions.Select(x => x.Classification).Distinct().ToArray();
        if (classifications.Length != 1)
        {
            return new TaxTreatmentDecision(
                TaxDecisionStatus.RequiresReview,
                TaxTreatmentClassification.RequiresReview,
                "REQUIRES_REVIEW",
                new[] { "sales.preview.mixed_tax_treatments_require_fiscal_policy" },
                new[] { "uniform_sale_tax_treatment_or_supported_mixed_policy" },
                decisions.SelectMany(x => x.RuleEvidence).DistinctBy(x => x.RuleId).ToArray(),
                string.Join("+", decisions.Select(x => x.RulePackVersion).Distinct()));
        }

        var first = decisions.First();
        return new TaxTreatmentDecision(
            TaxDecisionStatus.Resolved,
            first.Classification,
            first.TreatmentCode,
            decisions.SelectMany(x => x.Reasons).Distinct().ToArray(),
            Array.Empty<string>(),
            decisions.SelectMany(x => x.RuleEvidence).DistinctBy(x => x.RuleId).ToArray(),
            string.Join("+", decisions.Select(x => x.RulePackVersion).Distinct()));
    }

    private static FiscalOperationIntent MapIntent(SaleCommercialIntent intent) => intent switch
    {
        SaleCommercialIntent.ConsumerFinal => FiscalOperationIntent.ConsumerFinal,
        SaleCommercialIntent.TaxpayerInvoice => FiscalOperationIntent.TaxpayerInvoice,
        SaleCommercialIntent.Export => FiscalOperationIntent.Export,
        _ => FiscalOperationIntent.ConsumerFinal
    };

    private static Article34Numeral11ServiceKind MapServiceKind(SaleExportServiceKind kind) => kind switch
    {
        SaleExportServiceKind.AdvisoryOrTechnical => Article34Numeral11ServiceKind.AdvisoryOrTechnical,
        SaleExportServiceKind.CustomSoftware => Article34Numeral11ServiceKind.CustomSoftware,
        SaleExportServiceKind.SoftwareLicense => Article34Numeral11ServiceKind.SoftwareLicense,
        SaleExportServiceKind.SoftwareRightsAssignment => Article34Numeral11ServiceKind.SoftwareRightsAssignment,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static RegulatoryFactStatus MapFact(SaleRegulatoryFactStatus status) => status switch
    {
        SaleRegulatoryFactStatus.Confirmed => RegulatoryFactStatus.Confirmed,
        SaleRegulatoryFactStatus.NotMet => RegulatoryFactStatus.NotMet,
        _ => RegulatoryFactStatus.Unknown
    };

    private void EnsureReadAuthorized(string organizationId)
    {
        var actor = _actors.Current;
        if (!actor.IsAuthenticated || !actor.HasPermission(Permissions.SalesRead))
            throw new ApplicationProblemException(ApplicationProblemKind.Forbidden, "permission_denied", "The actor cannot read sales.");
        if (!actor.CompanyScopes.Contains(organizationId))
            throw new ApplicationProblemException(ApplicationProblemKind.Forbidden, "organization_scope_denied", "The actor is outside the requested organization scope.");
    }

    private static string Fingerprint(
        Sale sale,
        IEnumerable<SaleFiscalPreviewLineView> lines,
        TaxTreatmentDecision overall,
        CfeSelectionResult selection)
    {
        var material = new StringBuilder()
            .Append(sale.Id).Append('|').Append(sale.Version).Append('|').Append(sale.EffectiveOn).Append('|')
            .Append(overall.Status).Append('|').Append(overall.Classification).Append('|')
            .Append(selection.Status).Append('|').Append(selection.SelectedFamily);
        foreach (var line in lines.OrderBy(x => x.LineId))
        {
            material.Append('|').Append(line.LineId).Append(':').Append(line.NetAmount)
                .Append(':').Append(line.TaxTreatment).Append(':').Append(line.AppliedRatePercent);
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(material.ToString()))).ToLowerInvariant();
    }
}

public sealed class ValidateSaleUseCase
{
    private readonly ISaleRepository _sales;
    private readonly GetSaleFiscalPreviewUseCase _preview;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public ValidateSaleUseCase(
        ISaleRepository sales,
        GetSaleFiscalPreviewUseCase preview,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _sales = sales;
        _preview = preview;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public async Task<SaleValidationResult> ExecuteAsync(ValidateSaleCommand command, CancellationToken cancellationToken = default)
    {
        var preview = await _preview.ExecuteAsync(command.OrganizationId, command.SaleId, cancellationToken);
        var current = await _sales.GetAsync(command.OrganizationId, command.SaleId, cancellationToken)
            ?? throw new ApplicationProblemException(ApplicationProblemKind.NotFound, "sales.not_found", "Sale was not found.");

        if (!preview.ReadyForConfirmation)
        {
            return new SaleValidationResult(false, SaleView.FromDomain(current), preview, false);
        }

        if (preview.SaleVersion != command.ExpectedVersion)
        {
            throw new ApplicationProblemException(ApplicationProblemKind.Conflict, "concurrency.stale_version", "The sale changed after the preview was calculated.", conflictType: "stale_version");
        }

        return await _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var scope = $"sales.validate:{command.OrganizationId}:{command.SaleId}";
            var reservation = await _idempotency.TryReserveAsync(new IdempotencyReservation(
                scope, command.IdempotencyKey, command.RequestHash, _actors.Current.ActorId,
                _correlations.Current.CorrelationId, now.AddMinutes(10)), ct);
            if (reservation.Status == IdempotencyReservationStatus.ExistingCompleted)
            {
                var replayed = await _sales.GetAsync(command.OrganizationId, command.SaleId, ct)
                    ?? throw new ApplicationProblemException(ApplicationProblemKind.Conflict, "idempotency.missing_completed_resource", "The validated sale no longer exists.");
                return new SaleValidationResult(true, SaleView.FromDomain(replayed), preview, true);
            }
            if (reservation.Status != IdempotencyReservationStatus.Acquired)
                throw new ApplicationProblemException(ApplicationProblemKind.Conflict, "idempotency_conflict", "The validation idempotency reservation could not be acquired.");
            await _unitOfWork.SaveChangesAsync(ct);

            var sale = await _sales.GetAsync(command.OrganizationId, command.SaleId, ct)
                ?? throw new ApplicationProblemException(ApplicationProblemKind.NotFound, "sales.not_found", "Sale was not found.");
            try
            {
                sale.MarkValidated(preview.ValidationFingerprint, now, command.ExpectedVersion);
            }
            catch (DomainRuleException ex)
            {
                throw new ApplicationProblemException(ApplicationProblemKind.Conflict, ex.Code, ex.Message, conflictType: "stale_version");
            }

            await _audit.AppendAsync(new AuditEvent(
                Guid.NewGuid(), now, "SALE_VALIDATED", _actors.Current.ActorId, sale.OrganizationId,
                sale.LocationId, sale.TerminalId, "Sale", sale.Id.ToString(), AuditOutcome.Succeeded,
                _correlations.Current.CorrelationId, null,
                new Dictionary<string, string?> { ["validationFingerprint"] = preview.ValidationFingerprint, ["version"] = sale.Version.ToString() }), ct);
            await _outbox.EnqueueAsync(
                new SaleValidatedIntegrationEvent(Guid.NewGuid(), now, sale.Id, sale.OrganizationId, preview.ValidationFingerprint),
                new OutboxContext(_correlations.Current.CorrelationId, organizationId: sale.OrganizationId, actorId: _actors.Current.ActorId), ct);
            await _idempotency.CompleteAsync(new IdempotencyCompletion(
                scope, command.IdempotencyKey, command.RequestHash, "sale_validated", "Sale",
                sale.Id.ToString(), _correlations.Current.CorrelationId, now), ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return new SaleValidationResult(true, SaleView.FromDomain(sale), preview, false);
        }, cancellationToken);
    }
}

public sealed class GetSaleUseCase
{
    private readonly ISaleRepository _sales;
    private readonly IActorContextAccessor _actors;
    public GetSaleUseCase(ISaleRepository sales, IActorContextAccessor actors) { _sales = sales; _actors = actors; }
    public async Task<SaleView> ExecuteAsync(string organizationId, Guid saleId, CancellationToken cancellationToken = default)
    {
        Ensure(organizationId);
        var sale = await _sales.GetAsync(organizationId, saleId, cancellationToken)
            ?? throw new ApplicationProblemException(ApplicationProblemKind.NotFound, "sales.not_found", "Sale was not found.");
        return SaleView.FromDomain(sale);
    }
    private void Ensure(string organizationId)
    {
        var actor = _actors.Current;
        if (!actor.IsAuthenticated || !actor.HasPermission(Permissions.SalesRead) || !actor.CompanyScopes.Contains(organizationId))
            throw new ApplicationProblemException(ApplicationProblemKind.Forbidden, "permission_denied", "The actor cannot read this sale.");
    }
}

public sealed class ListSalesUseCase
{
    private readonly ISaleRepository _sales;
    private readonly IActorContextAccessor _actors;
    public ListSalesUseCase(ISaleRepository sales, IActorContextAccessor actors) { _sales = sales; _actors = actors; }
    public async Task<PageResult<SaleView>> ExecuteAsync(SaleSearchRequest request, CancellationToken cancellationToken = default)
    {
        var actor = _actors.Current;
        if (!actor.IsAuthenticated || !actor.HasPermission(Permissions.SalesRead) || !actor.CompanyScopes.Contains(request.OrganizationId))
            throw new ApplicationProblemException(ApplicationProblemKind.Forbidden, "permission_denied", "The actor cannot list sales.");
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, 200);
        var result = await _sales.SearchAsync(request with { Page = page, PageSize = size }, cancellationToken);
        return new PageResult<SaleView>(result.Items.Select(SaleView.FromDomain).ToArray(), page, size, result.Total);
    }
}
