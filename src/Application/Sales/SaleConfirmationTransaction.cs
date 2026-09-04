using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Common.Security;
using EFactura.Application.Fiscal;
using EFactura.Application.Inventory;
using EFactura.Application.Parties;
using EFactura.Application.Payments;
using EFactura.Application.Receivables;
using EFactura.Application.Taxation;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Payments;
using EFactura.Domain.Receivables;
using EFactura.Domain.Sales;
using EFactura.Domain.Taxation;

namespace EFactura.Application.Sales;

public sealed record ConfirmSaleCommand(
    string OrganizationId,
    Guid SaleId,
    long ExpectedVersion,
    IReadOnlyCollection<SaleImmediatePaymentIntent> PaymentIntents,
    SaleCreditTerms? CreditTerms,
    string OperatorReason,
    string? OperatorContext,
    string IdempotencyKey,
    string RequestHash);

public sealed record SaleConfirmationResult(
    Guid SaleId,
    long Version,
    string ConfirmationFingerprint,
    string SettlementFingerprint,
    Guid FiscalizationRequestId,
    int PaymentCount,
    Guid? ReceivableId,
    DateTimeOffset ConfirmedAtUtc,
    bool Replayed);

public sealed record SaleConfirmedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid SaleId,
    string OrganizationId,
    string ConfirmationFingerprint,
    string SettlementFingerprint,
    Guid FiscalizationRequestId) : IIntegrationEvent;

public sealed record FiscalizationRequestedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid FiscalizationRequestId,
    Guid SaleId,
    string OrganizationId,
    string ConfirmationFingerprint,
    string SettlementFingerprint) : IIntegrationEvent;

public interface ISaleConfirmationEvidenceResolver
{
    Task<SaleConfirmationPlan> PrepareAsync(
        Sale sale,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Recomputes authoritative fiscal, CFE and inventory evidence from server-owned sources and
/// converts it directly into the deterministic confirmation plan. This is intentionally distinct
/// from the public fiscal-preview DTO so confirmation never reconstructs regulatory provenance
/// from a client-visible projection.
/// </summary>
public sealed class SaleConfirmationEvidenceResolver : ISaleConfirmationEvidenceResolver
{
    private readonly IPartyRepository _parties;
    private readonly ResolveTaxTreatmentUseCase _taxTreatment;
    private readonly ResolveTaxRateUseCase _taxRate;
    private readonly PrepareCfeEligibilityUseCase _eligibility;
    private readonly SelectCfeUseCase _selector;
    private readonly IUiAmountConverter _uiAmount;
    private readonly IInventoryAvailabilityChecker _inventory;
    private readonly SaleConfirmationPlanner _planner;

    public SaleConfirmationEvidenceResolver(
        IPartyRepository parties,
        ResolveTaxTreatmentUseCase taxTreatment,
        ResolveTaxRateUseCase taxRate,
        PrepareCfeEligibilityUseCase eligibility,
        SelectCfeUseCase selector,
        IUiAmountConverter uiAmount,
        IInventoryAvailabilityChecker inventory,
        SaleConfirmationPlanner planner)
    {
        _parties = parties;
        _taxTreatment = taxTreatment;
        _taxRate = taxRate;
        _eligibility = eligibility;
        _selector = selector;
        _uiAmount = uiAmount;
        _inventory = inventory;
        _planner = planner;
    }

    public async Task<SaleConfirmationPlan> PrepareAsync(
        Sale sale,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sale);
        if (sale.Status != SaleStatus.Validated || string.IsNullOrWhiteSpace(sale.ValidationFingerprint))
            throw Rule("sales.confirmation.validation_required", "Only a validated sale with validation evidence can be prepared for confirmation.");

        var receiver = await BuildReceiverAsync(sale, cancellationToken);
        var linePlans = new List<SaleConfirmationPlanningLine>(sale.Lines.Count);
        var treatments = new List<TaxTreatmentDecision>(sale.Lines.Count);

        foreach (var line in sale.Lines)
        {
            var treatment = await _taxTreatment.ExecuteAsync(
                BuildTreatmentRequest(sale, line, receiver), cancellationToken);
            treatments.Add(treatment);

            var rate = await _taxRate.ExecuteAsync(
                new ResolveTaxRateRequest(
                    sale.OrganizationId,
                    sale.EffectiveOn,
                    treatment,
                    line.TaxProfileId),
                cancellationToken);

            linePlans.Add(new SaleConfirmationPlanningLine(
                line.Id,
                line.ItemId,
                line.Kind,
                line.Quantity,
                line.UnitPrice,
                rate));
        }

        var overallTreatment = CombineTaxTreatments(treatments);
        var netAmountUi = await _uiAmount.TryConvertToUiAsync(
            sale.CurrencyCode,
            sale.NetAmount,
            sale.EffectiveOn,
            cancellationToken);
        var eligibility = await _eligibility.ExecuteAsync(
            new PrepareCfeEligibilityRequest(
                sale.EffectiveOn,
                overallTreatment,
                receiver,
                MapIntent(sale.Intent),
                netAmountUi,
                HasRetentionsOrPerceptions: false),
            cancellationToken);
        var selection = await _selector.ExecuteAsync(
            new SelectCfeRequest(
                sale.OrganizationId,
                sale.EffectiveOn,
                overallTreatment,
                eligibility),
            cancellationToken);

        var inventory = await _inventory.CheckAsync(
            sale.OrganizationId,
            sale.LocationId,
            sale.Lines
                .Where(line => line.Kind == SaleLineKind.Product)
                .Select(line => new InventoryAvailabilityRequirement(line.ItemId, line.Quantity))
                .ToArray(),
            cancellationToken);

        return _planner.Prepare(new SaleConfirmationPlanningRequest(
            sale.Id,
            sale.Version,
            sale.Status,
            sale.EffectiveOn,
            sale.CurrencyCode,
            sale.ValidationFingerprint,
            selection,
            linePlans,
            inventory));
    }

    private async Task<ReceiverTaxFacts> BuildReceiverAsync(Sale sale, CancellationToken cancellationToken)
    {
        if (!sale.CustomerPartyId.HasValue)
            return new ReceiverTaxFacts("UY", "UY");

        var party = await _parties.GetAsync(sale.OrganizationId, sale.CustomerPartyId.Value, cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                "sales.customer_not_found",
                "Sale customer no longer exists.");
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
        var operationKind = line.Kind == SaleLineKind.Product
            ? TaxOperationKind.Goods
            : TaxOperationKind.Services;
        var goodsScope = line.Kind == SaleLineKind.Product
            ? sale.GoodsExportConfirmed
                ? GoodsMovementScope.ExportConfirmed
                : sale.Intent == SaleCommercialIntent.Export
                  || (sale.DeliveryCountry is not null && sale.DeliveryCountry != "UY")
                    ? GoodsMovementScope.Unknown
                    : GoodsMovementScope.DomesticDelivery
            : GoodsMovementScope.Unknown;
        var serviceScope = line.ServicePerformanceScope switch
        {
            SaleServicePerformanceScope.EntirelyInUruguay => ServicePerformanceScope.EntirelyInUruguay,
            SaleServicePerformanceScope.EntirelyOutsideUruguay => ServicePerformanceScope.EntirelyOutsideUruguay,
            _ => ServicePerformanceScope.UnknownOrMixed
        };

        ExportServiceEvaluationContext? exportServiceContext = null;
        if (line.Kind == SaleLineKind.Service && line.ExportServiceKind != SaleExportServiceKind.None)
        {
            exportServiceContext = new ExportServiceEvaluationContext(
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
            operationKind,
            receiver,
            goodsScope,
            serviceScope,
            sale.DeliveryCountry,
            line.ServiceUseCountry,
            ExportServiceContext: exportServiceContext);
    }

    private static TaxTreatmentDecision CombineTaxTreatments(IReadOnlyCollection<TaxTreatmentDecision> decisions)
    {
        if (decisions.Count == 0)
            return Review(new[] { "sales.confirmation.no_tax_decisions" }, new[] { "sale_lines" }, Array.Empty<RegulatoryRuleEvidence>(), "MULTI");

        if (decisions.Any(x => x.Status == TaxDecisionStatus.RequiresReview))
        {
            return Review(
                decisions.SelectMany(x => x.Reasons).Distinct(StringComparer.Ordinal).ToArray(),
                decisions.SelectMany(x => x.MissingFacts).Distinct(StringComparer.Ordinal).ToArray(),
                decisions.SelectMany(x => x.RuleEvidence).DistinctBy(x => x.RuleId).ToArray(),
                string.Join("+", decisions.Select(x => x.RulePackVersion).Distinct(StringComparer.Ordinal)));
        }

        var classifications = decisions.Select(x => x.Classification).Distinct().ToArray();
        if (classifications.Length != 1)
        {
            return Review(
                new[] { "sales.confirmation.mixed_tax_treatments_require_fiscal_policy" },
                new[] { "uniform_sale_tax_treatment_or_supported_mixed_policy" },
                decisions.SelectMany(x => x.RuleEvidence).DistinctBy(x => x.RuleId).ToArray(),
                string.Join("+", decisions.Select(x => x.RulePackVersion).Distinct(StringComparer.Ordinal)));
        }

        var first = decisions.First();
        return new TaxTreatmentDecision(
            TaxDecisionStatus.Resolved,
            first.Classification,
            first.TreatmentCode,
            decisions.SelectMany(x => x.Reasons).Distinct(StringComparer.Ordinal).ToArray(),
            Array.Empty<string>(),
            decisions.SelectMany(x => x.RuleEvidence).DistinctBy(x => x.RuleId).ToArray(),
            string.Join("+", decisions.Select(x => x.RulePackVersion).Distinct(StringComparer.Ordinal)));
    }

    private static TaxTreatmentDecision Review(
        IReadOnlyCollection<string> reasons,
        IReadOnlyCollection<string> missingFacts,
        IReadOnlyCollection<RegulatoryRuleEvidence> evidence,
        string rulePackVersion) =>
        new(
            TaxDecisionStatus.RequiresReview,
            TaxTreatmentClassification.RequiresReview,
            "REQUIRES_REVIEW",
            reasons,
            missingFacts,
            evidence,
            rulePackVersion);

    private static FiscalOperationIntent MapIntent(SaleCommercialIntent intent) => intent switch
    {
        SaleCommercialIntent.ConsumerFinal => FiscalOperationIntent.ConsumerFinal,
        SaleCommercialIntent.TaxpayerInvoice => FiscalOperationIntent.TaxpayerInvoice,
        SaleCommercialIntent.Export => FiscalOperationIntent.Export,
        _ => throw new ArgumentOutOfRangeException(nameof(intent))
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

    private static DomainRuleException Rule(string code, string message) => new(code, message);
}

/// <summary>
/// Owns the complete local atomic boundary for confirming a validated Sale. It deliberately stops
/// at durable FiscalizationRequested state. CAE allocation, FiscalDocument identity, XML generation,
/// signing and DGI transport are later workflow steps and never execute inside this transaction.
/// </summary>
public sealed class ConfirmSaleUseCase
{
    private readonly ISaleRepository _sales;
    private readonly ISaleConfirmationEvidenceResolver _confirmationEvidence;
    private readonly SaleSettlementPlanner _settlementPlanner;
    private readonly IPaymentMethodRepository _paymentMethods;
    private readonly IPaymentRepository _payments;
    private readonly IReceivableRepository _receivables;
    private readonly SaleStockConsumer _stock;
    private readonly IFiscalizationRequestRepository _fiscalization;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdempotencyStore _idempotency;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public ConfirmSaleUseCase(
        ISaleRepository sales,
        ISaleConfirmationEvidenceResolver confirmationEvidence,
        SaleSettlementPlanner settlementPlanner,
        IPaymentMethodRepository paymentMethods,
        IPaymentRepository payments,
        IReceivableRepository receivables,
        SaleStockConsumer stock,
        IFiscalizationRequestRepository fiscalization,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IIdempotencyStore idempotency,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _sales = sales;
        _confirmationEvidence = confirmationEvidence;
        _settlementPlanner = settlementPlanner;
        _paymentMethods = paymentMethods;
        _payments = payments;
        _receivables = receivables;
        _stock = stock;
        _fiscalization = fiscalization;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _idempotency = idempotency;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public async Task<SaleConfirmationResult> ExecuteAsync(
        ConfirmSaleCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        SalesAuthorization.Ensure(_actors, command.OrganizationId, Permissions.SalesConfirm);
        ValidateCommand(command);

        return await _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var scope = $"sales.confirm:{command.OrganizationId}:{command.SaleId}";
            var reservation = await _idempotency.TryReserveAsync(
                new IdempotencyReservation(
                    scope,
                    command.IdempotencyKey,
                    command.RequestHash,
                    _actors.Current.ActorId,
                    _correlations.Current.CorrelationId,
                    now.AddMinutes(10)),
                ct);

            if (reservation.Status == IdempotencyReservationStatus.ExistingCompleted)
                return await ReplayAsync(command.OrganizationId, command.SaleId, ct);

            if (reservation.Status != IdempotencyReservationStatus.Acquired)
            {
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Conflict,
                    reservation.Status == IdempotencyReservationStatus.PayloadMismatch
                        ? "idempotency_key_reused"
                        : "idempotency_in_progress",
                    "The sale-confirmation idempotency reservation could not be acquired.");
            }

            await _unitOfWork.SaveChangesAsync(ct);

            var sale = await _sales.GetAsync(command.OrganizationId, command.SaleId, ct)
                ?? throw new ApplicationProblemException(
                    ApplicationProblemKind.NotFound,
                    "sales.not_found",
                    "Sale was not found.");
            EnsureSaleScope(_actors.Current, sale);
            if (sale.Status != SaleStatus.Validated)
            {
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Conflict,
                    sale.Status == SaleStatus.Confirmed
                        ? "sales.already_confirmed"
                        : "sales.confirmation.validation_required",
                    "Only a validated, unconfirmed sale can be confirmed.",
                    conflictType: "invalid_state",
                    currentVersion: sale.Version.ToString());
            }
            if (sale.Version != command.ExpectedVersion)
            {
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Conflict,
                    "concurrency.stale_version",
                    "The sale changed before confirmation could start.",
                    conflictType: "stale_version",
                    currentVersion: sale.Version.ToString());
            }

            SaleConfirmationPlan confirmation;
            SaleSettlementPlan settlement;
            try
            {
                confirmation = await _confirmationEvidence.PrepareAsync(sale, ct);
                var methodEvidence = await LoadPaymentMethodEvidenceAsync(
                    command.OrganizationId,
                    command.PaymentIntents,
                    ct);
                settlement = _settlementPlanner.Prepare(new SaleSettlementPlanningRequest(
                    sale,
                    confirmation,
                    command.PaymentIntents,
                    methodEvidence,
                    command.CreditTerms));
            }
            catch (DomainRuleException ex)
            {
                throw DomainProblem(ex);
            }

            var paymentCount = 0;
            foreach (var planned in settlement.ImmediatePayments)
            {
                var payment = Payment.CreateFromSale(
                    Guid.NewGuid(),
                    sale.OrganizationId,
                    sale.Id,
                    ++paymentCount,
                    planned.PaymentMethodId,
                    planned.PaymentMethodVersion,
                    planned.Amount,
                    planned.CurrencyCode,
                    planned.ExternalReference,
                    confirmation.ConfirmationFingerprint,
                    settlement.SettlementFingerprint,
                    now);
                await _payments.AddAsync(payment, ct);
            }

            Guid? receivableId = null;
            if (settlement.Receivable is not null)
            {
                var receivable = Receivable.CreateFromSale(
                    Guid.NewGuid(),
                    sale.OrganizationId,
                    settlement.Receivable.CustomerPartyId,
                    sale.Id,
                    settlement.Receivable.OriginalAmount,
                    settlement.Receivable.CurrencyCode,
                    sale.EffectiveOn,
                    settlement.Receivable.DueDate,
                    confirmation.ConfirmationFingerprint,
                    settlement.SettlementFingerprint,
                    now);
                receivableId = receivable.Id;
                await _receivables.AddAsync(receivable, ct);
            }

            var trackedInventory = confirmation.Inventory.Lines.Any(line => line.TracksInventory);
            if (trackedInventory)
            {
                if (string.IsNullOrWhiteSpace(sale.LocationId))
                {
                    throw new ApplicationProblemException(
                        ApplicationProblemKind.Validation,
                        "sales.confirmation.location_required_for_stock",
                        "A sale that consumes tracked inventory requires an authoritative location.");
                }

                await _stock.StageAsync(new SaleStockConsumptionRequest(
                    sale.OrganizationId,
                    sale.LocationId,
                    sale.Id,
                    confirmation.ConfirmationFingerprint,
                    settlement.SettlementFingerprint,
                    confirmation.Inventory.Lines), ct);
            }

            var fiscalization = FiscalizationRequest.CreateFromSale(
                Guid.NewGuid(),
                sale.OrganizationId,
                sale.Id,
                sale.LocationId,
                sale.TerminalId,
                confirmation.Selection.SelectedFamily!.Value,
                confirmation.Selection.ReceiverIdentification!.Value,
                confirmation.Selection.FormatVersion,
                confirmation.ConfirmationFingerprint,
                settlement.SettlementFingerprint,
                confirmation.FiscalCalculation.CurrencyCode,
                confirmation.FiscalCalculation.Totals.NetAmount,
                confirmation.FiscalCalculation.Totals.VatAmount,
                confirmation.FiscalCalculation.Totals.TotalAmount,
                now);
            await _fiscalization.AddAsync(fiscalization, ct);

            try
            {
                sale.MarkConfirmed(
                    confirmation.ConfirmationFingerprint,
                    settlement.SettlementFingerprint,
                    now,
                    command.ExpectedVersion);
            }
            catch (DomainRuleException ex)
            {
                throw DomainProblem(ex);
            }
            await _sales.SaveAsync(sale, ct);

            var reason = command.OperatorReason.Trim();
            var operatorContext = Optional(command.OperatorContext, 1000);
            await _audit.AppendAsync(new AuditEvent(
                Guid.NewGuid(),
                now,
                "SALE_CONFIRMED",
                _actors.Current.ActorId,
                sale.OrganizationId,
                sale.LocationId,
                sale.TerminalId,
                "Sale",
                sale.Id.ToString(),
                AuditOutcome.Succeeded,
                _correlations.Current.CorrelationId,
                null,
                new Dictionary<string, string?>
                {
                    ["confirmationFingerprint"] = confirmation.ConfirmationFingerprint,
                    ["settlementFingerprint"] = settlement.SettlementFingerprint,
                    ["settlementKind"] = settlement.Kind.ToString(),
                    ["paymentCount"] = paymentCount.ToString(),
                    ["receivableId"] = receivableId?.ToString(),
                    ["fiscalizationRequestId"] = fiscalization.Id.ToString(),
                    ["operatorReason"] = reason,
                    ["operatorContext"] = operatorContext,
                    ["version"] = sale.Version.ToString()
                }), ct);

            var outboxContext = new OutboxContext(
                _correlations.Current.CorrelationId,
                OrganizationId: sale.OrganizationId,
                ActorId: _actors.Current.ActorId);
            await _outbox.EnqueueAsync(new SaleConfirmedIntegrationEvent(
                Guid.NewGuid(),
                now,
                sale.Id,
                sale.OrganizationId,
                confirmation.ConfirmationFingerprint,
                settlement.SettlementFingerprint,
                fiscalization.Id), outboxContext, ct);
            await _outbox.EnqueueAsync(new FiscalizationRequestedIntegrationEvent(
                Guid.NewGuid(),
                now,
                fiscalization.Id,
                sale.Id,
                sale.OrganizationId,
                confirmation.ConfirmationFingerprint,
                settlement.SettlementFingerprint), outboxContext, ct);

            await _idempotency.CompleteAsync(new IdempotencyCompletion(
                scope,
                command.IdempotencyKey,
                command.RequestHash,
                "sale_confirmed",
                "Sale",
                sale.Id.ToString(),
                _correlations.Current.CorrelationId,
                now), ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return new SaleConfirmationResult(
                sale.Id,
                sale.Version,
                confirmation.ConfirmationFingerprint,
                settlement.SettlementFingerprint,
                fiscalization.Id,
                paymentCount,
                receivableId,
                now,
                false);
        }, cancellationToken);
    }

    private async Task<IReadOnlyCollection<SalePaymentMethodEvidence>> LoadPaymentMethodEvidenceAsync(
        string organizationId,
        IReadOnlyCollection<SaleImmediatePaymentIntent> intents,
        CancellationToken cancellationToken)
    {
        var result = new List<SalePaymentMethodEvidence>();
        foreach (var methodId in intents.Select(x => x.PaymentMethodId).Distinct())
        {
            var method = await _paymentMethods.GetAsync(organizationId, methodId, cancellationToken)
                ?? throw new ApplicationProblemException(
                    ApplicationProblemKind.Validation,
                    "sales.settlement.payment_method_not_found",
                    "An immediate-payment method no longer exists.");
            result.Add(new SalePaymentMethodEvidence(
                method.Id,
                method.OrganizationId,
                method.Version,
                method.Enabled));
        }

        return result;
    }

    private async Task<SaleConfirmationResult> ReplayAsync(
        string organizationId,
        Guid saleId,
        CancellationToken cancellationToken)
    {
        var sale = await _sales.GetAsync(organizationId, saleId, cancellationToken)
            ?? throw ReplayConflict("The completed confirmation sale no longer exists.");
        EnsureSaleScope(_actors.Current, sale);
        var fiscalization = await _fiscalization.GetBySaleAsync(organizationId, saleId, cancellationToken)
            ?? throw ReplayConflict("The completed confirmation fiscalization work item no longer exists.");
        var payments = await _payments.ListBySaleAsync(organizationId, saleId, cancellationToken);
        var receivable = await _receivables.GetBySaleAsync(organizationId, saleId, cancellationToken);

        if (sale.Status != SaleStatus.Confirmed
            || string.IsNullOrWhiteSpace(sale.ConfirmationFingerprint)
            || string.IsNullOrWhiteSpace(sale.SettlementFingerprint)
            || !sale.ConfirmedAtUtc.HasValue)
        {
            throw ReplayConflict("The completed idempotency record no longer matches a confirmed sale snapshot.");
        }
        if (!string.Equals(sale.ConfirmationFingerprint, fiscalization.ConfirmationFingerprint, StringComparison.Ordinal)
            || !string.Equals(sale.SettlementFingerprint, fiscalization.SettlementFingerprint, StringComparison.Ordinal))
        {
            throw ReplayConflict("The completed confirmation evidence no longer matches its fiscalization work item.");
        }

        return new SaleConfirmationResult(
            sale.Id,
            sale.Version,
            sale.ConfirmationFingerprint,
            sale.SettlementFingerprint,
            fiscalization.Id,
            payments.Count,
            receivable?.Id,
            sale.ConfirmedAtUtc.Value,
            true);
    }

    private static void ValidateCommand(ConfirmSaleCommand command)
    {
        if (command.SaleId == Guid.Empty)
            throw Validation("sales.sale_id_required", "Sale id is required.");
        if (command.ExpectedVersion <= 0)
            throw Validation("sales.expected_version_invalid", "Expected sale version must be positive.");
        if (command.PaymentIntents is null)
            throw Validation("sales.settlement.payment_intents_required", "Payment intents collection is required, even when empty.");
        if (command.PaymentIntents.Any(intent => intent is null))
            throw Validation("sales.settlement.payment_intent_required", "Payment intents cannot contain null entries.");
        if (string.IsNullOrWhiteSpace(command.OperatorReason))
            throw Validation("sales.confirmation.operator_reason_required", "Operator reason/context is required for confirmation auditability.");
        if (command.OperatorReason.Trim().Length > 500)
            throw Validation("sales.confirmation.operator_reason_too_long", "Operator reason cannot exceed 500 characters.");
        _ = Optional(command.OperatorContext, 1000);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RequestHash);
    }

    private static void EnsureSaleScope(ActorContext actor, Sale sale)
    {
        if (!string.IsNullOrWhiteSpace(sale.LocationId)
            && !actor.LocationScopes.Contains(sale.LocationId))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "location_scope_denied",
                "The actor is outside the sale location scope.");
        }

        if (!string.IsNullOrWhiteSpace(sale.TerminalId)
            && actor.TerminalScopes.Count > 0
            && !actor.TerminalScopes.Contains(sale.TerminalId))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Forbidden,
                "terminal_scope_denied",
                "The actor is outside the sale terminal scope.");
        }
    }

    private static ApplicationProblemException DomainProblem(DomainRuleException ex) =>
        ex.Code == "concurrency.stale_version"
            ? new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                ex.Code,
                ex.Message,
                conflictType: "stale_version")
            : new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                ex.Code,
                ex.Message);

    private static ApplicationProblemException Validation(string code, string message) =>
        new(ApplicationProblemKind.Validation, code, message);

    private static ApplicationProblemException ReplayConflict(string message) =>
        new(
            ApplicationProblemKind.Conflict,
            "idempotency.missing_completed_resource",
            message,
            conflictType: "inconsistent_replay");

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Validation("sales.confirmation.operator_context_too_long", $"Operator context cannot exceed {max} characters.");
        return normalized;
    }
}
