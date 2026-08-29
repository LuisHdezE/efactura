using EFactura.Application.Taxation;
using EFactura.Domain.Taxation;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class TaxTreatmentApplicationTests
{
    private static readonly DateOnly EffectiveOn = new(2026, 8, 29);

    [Fact]
    public async Task Service_entirely_outside_uruguay_skips_article_34_evaluator()
    {
        var evaluator = new CountingExportServiceEvaluator(QualifiedEvaluation());
        var useCase = new ResolveTaxTreatmentUseCase(
            new FixedRulePackProvider(RulePack()),
            evaluator,
            new TaxTreatmentDecisionEngine());

        var decision = await useCase.ExecuteAsync(new ResolveTaxTreatmentRequest(
            "company-1",
            EffectiveOn,
            TaxOperationKind.Services,
            ForeignReceiver(),
            ServicePerformanceScope: ServicePerformanceScope.EntirelyOutsideUruguay,
            ServiceUseCountry: "AR"));

        Assert.Equal(TaxTreatmentClassification.OutsideVatTerritorialScope, decision.Classification);
        Assert.Equal(0, evaluator.CallCount);
    }

    [Fact]
    public async Task Unknown_service_performance_scope_requires_review_before_article_34_evaluator()
    {
        var evaluator = new CountingExportServiceEvaluator(QualifiedEvaluation());
        var useCase = new ResolveTaxTreatmentUseCase(
            new FixedRulePackProvider(RulePack()),
            evaluator,
            new TaxTreatmentDecisionEngine());

        var decision = await useCase.ExecuteAsync(new ResolveTaxTreatmentRequest(
            "company-1",
            EffectiveOn,
            TaxOperationKind.Services,
            ForeignReceiver(),
            ServicePerformanceScope: ServicePerformanceScope.UnknownOrMixed,
            ServiceUseCountry: "AR"));

        Assert.Equal(TaxDecisionStatus.RequiresReview, decision.Status);
        Assert.Contains("service_performance_scope", decision.MissingFacts);
        Assert.Equal(0, evaluator.CallCount);
    }

    [Fact]
    public async Task Service_in_uruguay_invokes_article_34_evaluator_once_and_passes_context()
    {
        var evaluator = new CountingExportServiceEvaluator(QualifiedEvaluation());
        var useCase = new ResolveTaxTreatmentUseCase(
            new FixedRulePackProvider(RulePack()),
            evaluator,
            new TaxTreatmentDecisionEngine());
        var context = new ExportServiceEvaluationContext(
            ExportServiceRuleFamily.Article34Numeral11,
            new Article34Numeral11Facts(
                Article34Numeral11ServiceKind.AdvisoryOrTechnical,
                RegulatoryFactStatus.Confirmed,
                RegulatoryFactStatus.Confirmed,
                RegulatoryFactStatus.Confirmed));

        var decision = await useCase.ExecuteAsync(new ResolveTaxTreatmentRequest(
            "company-1",
            EffectiveOn,
            TaxOperationKind.Services,
            ForeignReceiver(),
            ServicePerformanceScope: ServicePerformanceScope.EntirelyInUruguay,
            ServiceUseCountry: "AR",
            EvidenceReferences: new[] { "evidence:exclusive-use-abroad" },
            ExportServiceContext: context));

        Assert.Equal(TaxTreatmentClassification.ExportServices, decision.Classification);
        Assert.Equal(1, evaluator.CallCount);
        Assert.Same(context, evaluator.LastContext);
    }

    private static ReceiverTaxFacts ForeignReceiver() =>
        new("AR", "AR", new[] { new ReceiverFiscalIdentityFact("6", "AR") });

    private static TaxTreatmentRulePack RulePack() =>
        new(
            "uy-iva-2026.08",
            new RegulatoryRuleEvidence(
                "UY-IVA-T10-ART5-TERRITORIALITY",
                "IMPO - T.O. 2023 Título 10",
                "https://www.impo.com.uy/bases/todgi2023/101-2024/5_T10",
                "reviewed 2026-08-29",
                new DateOnly(2024, 5, 16)),
            new RegulatoryRuleEvidence(
                "UY-IVA-T10-ART5-EXPORT-GOODS",
                "IMPO - T.O. 2023 Título 10",
                "https://www.impo.com.uy/bases/todgi2023/101-2024/5_T10",
                "reviewed 2026-08-29",
                new DateOnly(2024, 5, 16)));

    private static ExportServiceEligibilityEvaluation QualifiedEvaluation() =>
        new(
            ExportServiceEligibilityStatus.Qualified,
            new[]
            {
                new RegulatoryRuleEvidence(
                    "UY-IVA-D220-ART34-11",
                    "IMPO - Decreto 220/998 actualizado",
                    "https://www.impo.com.uy/bases/decretos/220-1998/34",
                    "reviewed 2026-08-29",
                    new DateOnly(2024, 5, 16))
            },
            new[] { "tax.reason.article34_11_qualified" });

    private sealed class FixedRulePackProvider : ITaxTreatmentRulePackProvider
    {
        private readonly TaxTreatmentRulePack _rulePack;

        public FixedRulePackProvider(TaxTreatmentRulePack rulePack) => _rulePack = rulePack;

        public Task<TaxTreatmentRulePack> GetAsync(
            string organizationId,
            DateOnly effectiveOn,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_rulePack);
    }

    private sealed class CountingExportServiceEvaluator : IExportServiceEligibilityEvaluator
    {
        private readonly ExportServiceEligibilityEvaluation _result;

        public CountingExportServiceEvaluator(ExportServiceEligibilityEvaluation result) => _result = result;

        public int CallCount { get; private set; }
        public ExportServiceEvaluationContext? LastContext { get; private set; }

        public Task<ExportServiceEligibilityEvaluation> EvaluateAsync(
            TaxTransactionFacts facts,
            ExportServiceEvaluationContext? context,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastContext = context;
            return Task.FromResult(_result);
        }
    }
}
