using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Idempotency;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Application.Common.Security;
using EFactura.Application.Fiscal;
using EFactura.Application.Parties;
using EFactura.Application.Taxation;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Sales;
using EFactura.Domain.Taxation;

namespace EFactura.Application.Sales;

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
        SalesAuthorization.Ensure(_actors, organizationId, Permissions.SalesRead);

        var sale = await _sales.GetAsync(organizationId, saleId, cancellationToken)
            ?? throw new ApplicationProblemException(ApplicationProblemKind.NotFound, "sales.not_found", "Sale was not found.");
        var receiver = await BuildReceiverAsync(sale, cancellationToken);

        var linePreviews = new List<SaleFiscalPreviewLineView>();
        var taxDecisions = new List<TaxTreatmentDecision>();
        decimal totalPreviewTax = 0m;
        var allAmountsResolved = true;

        foreach (var line in sale.Lines)
        {
            var treatment = await _taxTreatment.ExecuteAsync(
                BuildTreatmentRequest(sale, line, receiver),
                cancellationToken);
            taxDecisions.Add(treatment);

            var rate = await _taxRate.ExecuteAsync(
                new ResolveTaxRateRequest(
                    sale.OrganizationId,
                    sale.EffectiveOn,
                    treatment,
                    line.TaxProfileId),
                cancellationToken);

            decimal? previewTaxAmount = null;
            if (rate.Status == TaxRateResolutionStatus.Resolved && rate.AppliedRatePercent.HasValue)
            {
                // Preview-only arithmetic. Final CFE arithmetic/rounding authority remains
                // blocked until the official XML/arithmetic rule slice is accepted.
                previewTaxAmount = decimal.Round(
                    line.NetAmount * rate.AppliedRatePercent.Value / 100m,
                    2,
                    MidpointRounding.AwayFromZero);
                totalPreviewTax += previewTaxAmount.Value;
            }
            else
            {
                allAmountsResolved = false;
            }

            linePreviews.Add(new SaleFiscalPreviewLineView(
                line.Id,
                line.ItemCode,
                line.NetAmount,
                treatment.Status,
                treatment.Classification,
                treatment.TreatmentCode,
                rate.Status,
                rate.Liability,
                rate.RateKind,
                rate.AppliedRatePercent,
                previewTaxAmount,
                treatment.Reasons.Concat(rate.Reasons).Distinct(StringComparer.Ordinal).ToArray(),
                treatment.MissingFacts.Concat(rate.MissingFacts).Distinct(StringComparer.Ordinal).ToArray(),
                treatment.RuleEvidence.Concat(rate.RuleEvidence).DistinctBy(x => x.RuleId).ToArray()));
        }

        var overallTreatment = CombineTaxTreatments(taxDecisions);
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

        var findings = linePreviews.SelectMany(x => x.MissingFacts)
            .Concat(eligibility.MissingFacts)
            .Concat(selection.MissingFacts)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var readyForConfirmation = linePreviews.All(line =>
                                       line.TaxTreatmentStatus == TaxDecisionStatus.Resolved
                                       && line.TaxRateStatus == TaxRateResolutionStatus.Resolved)
                                   && selection.Status == CfeSelectionStatus.Selected;

        var taxAmount = allAmountsResolved ? totalPreviewTax : null;
        var totalAmount = taxAmount.HasValue
            ? decimal.Round(sale.NetAmount + taxAmount.Value, 2, MidpointRounding.AwayFromZero)
            : null;
        var fingerprint = BuildFingerprint(sale, linePreviews, overallTreatment, selection);

        return new SaleFiscalPreviewView(
            sale.Id,
            sale.Version,
            sale.CurrencyCode,
            sale.NetAmount,
            taxAmount,
            totalAmount,
            linePreviews,
            overallTreatment,
            eligibility,
            selection,
            readyForConfirmation,
            fingerprint,
            findings);
    }

    private async Task<ReceiverTaxFacts> BuildReceiverAsync(
        Sale sale,
        CancellationToken cancellationToken)
    {
        if (!sale.CustomerPartyId.HasValue)
        {
            return new ReceiverTaxFacts("UY", "UY");
        }

        var party = await _parties.GetAsync(
            sale.OrganizationId,
            sale.CustomerPartyId.Value,
            cancellationToken)
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

        return new ReceiverTaxFacts(
            party.ResidenceCountry,
            party.TaxResidenceCountry,
            identities);
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

    private static TaxTreatmentDecision CombineTaxTreatments(
        IReadOnlyCollection<TaxTreatmentDecision> decisions)
    {
        if (decisions.Count == 0)
        {
            return Review(
                new[] { "sales.preview.no_tax_decisions" },
                new[] { "sale_lines" },
                Array.Empty<RegulatoryRuleEvidence>(),
                "MULTI");
        }

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
                new[] { "sales.preview.mixed_tax_treatments_require_fiscal_policy" },
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

    private static string BuildFingerprint(
        Sale sale,
        IEnumerable<SaleFiscalPreviewLineView> lines,
        TaxTreatmentDecision treatment,
        CfeSelectionResult selection)
    {
        var material = new StringBuilder()
            .Append(sale.Id).Append('|')
            .Append(sale.Version).Append('|')
            .Append(sale.EffectiveOn).Append('|')
            .Append(treatment.Status).Append('|')
            .Append(treatment.Classification).Append('|')
            .Append(selection.Status).Append('|')
            .Append(selection.SelectedFamily);

        foreach (var line in lines.OrderBy(x => x.LineId))
        {
            material.Append('|')
                .Append(line.LineId).Append(':')
                .Append(line.NetAmount).Append(':')
                .Append(line.TaxTreatment).Append(':')
                .Append(line.AppliedRatePercent);
        }

        return Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(material.ToString())))
            .ToLowerInvariant();
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

    public async Task<SaleValidationResult> ExecuteAsync(
        ValidateSaleCommand command,
        CancellationToken cancellationToken = default)
    {
        SalesAuthorization.Ensure(_actors, command.OrganizationId, Permissions.SalesCreate);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.IdempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(command.RequestHash);

        var preview = await _preview.ExecuteAsync(
            command.OrganizationId,
            command.SaleId,
            cancellationToken);
        var current = await _sales.GetAsync(command.OrganizationId, command.SaleId, cancellationToken)
            ?? throw new ApplicationProblemException(ApplicationProblemKind.NotFound, "sales.not_found", "Sale was not found.");

        if (!preview.ReadyForConfirmation)
        {
            return new SaleValidationResult(false, SaleView.FromDomain(current), preview, false);
        }

        if (preview.SaleVersion != command.ExpectedVersion)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency.stale_version",
                "The sale changed after the preview was calculated.",
                conflictType: "stale_version");
        }

        return await _transactions.ExecuteAsync(async ct =>
        {
            var now = DateTimeOffset.UtcNow;
            var scope = $"sales.validate:{command.OrganizationId}:{command.SaleId}";
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
            {
                var replayed = await _sales.GetAsync(command.OrganizationId, command.SaleId, ct)
                    ?? throw new ApplicationProblemException(
                        ApplicationProblemKind.Conflict,
                        "idempotency.missing_completed_resource",
                        "The validated sale no longer exists.");
                return new SaleValidationResult(true, SaleView.FromDomain(replayed), preview, true);
            }

            if (reservation.Status != IdempotencyReservationStatus.Acquired)
            {
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Conflict,
                    reservation.Status == IdempotencyReservationStatus.PayloadMismatch
                        ? "idempotency_key_reused"
                        : "idempotency_in_progress",
                    "The validation idempotency reservation could not be acquired.");
            }

            await _unitOfWork.SaveChangesAsync(ct);

            var sale = await _sales.GetAsync(command.OrganizationId, command.SaleId, ct)
                ?? throw new ApplicationProblemException(ApplicationProblemKind.NotFound, "sales.not_found", "Sale was not found.");

            try
            {
                sale.MarkValidated(preview.ValidationFingerprint, now, command.ExpectedVersion);
            }
            catch (DomainRuleException ex)
            {
                throw new ApplicationProblemException(
                    ApplicationProblemKind.Conflict,
                    ex.Code,
                    ex.Message,
                    conflictType: "stale_version");
            }

            await _sales.SaveAsync(sale, ct);
            await _audit.AppendAsync(
                new AuditEvent(
                    Guid.NewGuid(),
                    now,
                    "SALE_VALIDATED",
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
                        ["validationFingerprint"] = preview.ValidationFingerprint,
                        ["version"] = sale.Version.ToString()
                    }),
                ct);
            await _outbox.EnqueueAsync(
                new SaleValidatedIntegrationEvent(
                    Guid.NewGuid(),
                    now,
                    sale.Id,
                    sale.OrganizationId,
                    preview.ValidationFingerprint),
                new OutboxContext(
                    _correlations.Current.CorrelationId,
                    organizationId: sale.OrganizationId,
                    actorId: _actors.Current.ActorId),
                ct);
            await _idempotency.CompleteAsync(
                new IdempotencyCompletion(
                    scope,
                    command.IdempotencyKey,
                    command.RequestHash,
                    "sale_validated",
                    "Sale",
                    sale.Id.ToString(),
                    _correlations.Current.CorrelationId,
                    now),
                ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return new SaleValidationResult(true, SaleView.FromDomain(sale), preview, false);
        }, cancellationToken);
    }
}
