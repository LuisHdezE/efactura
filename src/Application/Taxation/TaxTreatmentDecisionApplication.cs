using EFactura.Domain.Taxation;

namespace EFactura.Application.Taxation;

public sealed record ResolveTaxTreatmentRequest(
    string OrganizationId,
    DateOnly EffectiveOn,
    TaxOperationKind OperationKind,
    ReceiverTaxFacts Receiver,
    GoodsMovementScope GoodsMovementScope = GoodsMovementScope.Unknown,
    ServicePerformanceScope ServicePerformanceScope = ServicePerformanceScope.UnknownOrMixed,
    string? DeliveryCountry = null,
    string? ServiceUseCountry = null,
    IReadOnlyCollection<string>? EvidenceReferences = null,
    ExportServiceEvaluationContext? ExportServiceContext = null);

public interface ITaxTreatmentRulePackProvider
{
    Task<TaxTreatmentRulePack> GetAsync(
        string organizationId,
        DateOnly effectiveOn,
        CancellationToken cancellationToken = default);
}

public interface IExportServiceEligibilityEvaluator
{
    Task<ExportServiceEligibilityEvaluation> EvaluateAsync(
        TaxTransactionFacts facts,
        ExportServiceEvaluationContext? context,
        CancellationToken cancellationToken = default);
}

public sealed class ResolveTaxTreatmentUseCase
{
    private readonly ITaxTreatmentRulePackProvider _rulePacks;
    private readonly IExportServiceEligibilityEvaluator _exportServices;
    private readonly TaxTreatmentDecisionEngine _engine;

    public ResolveTaxTreatmentUseCase(
        ITaxTreatmentRulePackProvider rulePacks,
        IExportServiceEligibilityEvaluator exportServices,
        TaxTreatmentDecisionEngine engine)
    {
        _rulePacks = rulePacks;
        _exportServices = exportServices;
        _engine = engine;
    }

    public async Task<TaxTreatmentDecision> ExecuteAsync(
        ResolveTaxTreatmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var facts = new TaxTransactionFacts(
            request.OrganizationId,
            request.EffectiveOn,
            request.OperationKind,
            request.Receiver,
            request.GoodsMovementScope,
            request.ServicePerformanceScope,
            request.DeliveryCountry,
            request.ServiceUseCountry,
            request.EvidenceReferences);

        var rulePack = await _rulePacks.GetAsync(
            request.OrganizationId,
            request.EffectiveOn,
            cancellationToken);

        var exportEvaluation = request.OperationKind == TaxOperationKind.Services
            && request.ServicePerformanceScope == ServicePerformanceScope.EntirelyInUruguay
            ? await _exportServices.EvaluateAsync(facts, request.ExportServiceContext, cancellationToken)
            : ExportServiceEligibilityEvaluation.NotEvaluated();

        return _engine.Resolve(facts, rulePack, exportEvaluation);
    }
}
