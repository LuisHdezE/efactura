using EFactura.Application.Common.Errors;
using EFactura.Application.Taxation;
using EFactura.Domain.Taxation;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class Article34Numeral11RulePackTests
{
    private static readonly DateOnly EffectiveOn = new(2026, 8, 29);

    [Fact]
    public async Task Release1_rule_pack_returns_traceable_current_sources_without_tax_rates()
    {
        var provider = new UruguayRelease1TaxTreatmentRulePackProvider();

        var pack = await provider.GetAsync("company-1", EffectiveOn);

        Assert.Equal(UruguayRelease1TaxTreatmentRulePackProvider.PackVersion, pack.Version);
        Assert.Equal("UY-IVA-T10-ART5-TERRITORIALITY", pack.TerritorialityRule.RuleId);
        Assert.Equal("UY-IVA-T10-ART5-EXPORT-GOODS", pack.ExportGoodsRule.RuleId);
        Assert.Contains("impo.com.uy", pack.TerritorialityRule.SourceReference, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Release1_rule_pack_rejects_dates_before_verified_support_boundary()
    {
        var provider = new UruguayRelease1TaxTreatmentRulePackProvider();

        var exception = await Assert.ThrowsAsync<ApplicationProblemException>(() =>
            provider.GetAsync("company-1", new DateOnly(2024, 5, 15)));

        Assert.Equal("tax.rule_pack_date_unsupported", exception.Code);
    }

    [Fact]
    public async Task Article34_11_a_requires_person_abroad_foreign_relation_and_exclusive_use_abroad()
    {
        var evaluator = new Article34Numeral11ExportServiceEligibilityEvaluator();
        var context = Context(new Article34Numeral11Facts(
            Article34Numeral11ServiceKind.AdvisoryOrTechnical,
            RegulatoryFactStatus.Confirmed,
            RegulatoryFactStatus.Confirmed,
            RegulatoryFactStatus.Confirmed));

        var result = await evaluator.EvaluateAsync(ServiceFacts(), context);

        Assert.Equal(ExportServiceEligibilityStatus.Qualified, result.Status);
        Assert.Contains(result.RuleEvidence, rule => rule.RuleId == "UY-IVA-D220-ART34-11-A");
    }

    [Fact]
    public async Task Article34_11_a_foreign_customer_does_not_qualify_when_exclusive_use_abroad_is_not_met()
    {
        var evaluator = new Article34Numeral11ExportServiceEligibilityEvaluator();
        var context = Context(new Article34Numeral11Facts(
            Article34Numeral11ServiceKind.AdvisoryOrTechnical,
            RegulatoryFactStatus.Confirmed,
            RegulatoryFactStatus.NotMet,
            RegulatoryFactStatus.Confirmed));

        var result = await evaluator.EvaluateAsync(ServiceFacts(), context);

        Assert.Equal(ExportServiceEligibilityStatus.NotQualified, result.Status);
        Assert.Contains("tax.reason.article34_11_a_exclusive_use_abroad_not_met", result.Reasons);
    }

    [Fact]
    public async Task Article34_11_b_custom_software_qualifies_on_exterior_path_only_with_explicit_facts()
    {
        var evaluator = new Article34Numeral11ExportServiceEligibilityEvaluator();
        var context = Context(new Article34Numeral11Facts(
            Article34Numeral11ServiceKind.CustomSoftware,
            RegulatoryFactStatus.Confirmed,
            RegulatoryFactStatus.Confirmed,
            RecipientInstalledInFreeZone: RegulatoryFactStatus.Unknown));

        var result = await evaluator.EvaluateAsync(ServiceFacts(), context);

        Assert.Equal(ExportServiceEligibilityStatus.Qualified, result.Status);
        Assert.Contains(result.RuleEvidence, rule => rule.RuleId == "UY-IVA-D220-ART34-11-B");
    }

    [Fact]
    public async Task Article34_11_software_free_zone_path_qualifies_independently_when_provider_origin_is_confirmed()
    {
        var evaluator = new Article34Numeral11ExportServiceEligibilityEvaluator();
        var context = Context(new Article34Numeral11Facts(
            Article34Numeral11ServiceKind.SoftwareLicense,
            RegulatoryFactStatus.NotMet,
            RegulatoryFactStatus.NotMet,
            RecipientInstalledInFreeZone: RegulatoryFactStatus.Confirmed,
            ProviderFromNonFreeNationalTerritory: RegulatoryFactStatus.Confirmed));

        var result = await evaluator.EvaluateAsync(ServiceFacts(), context);

        Assert.Equal(ExportServiceEligibilityStatus.Qualified, result.Status);
        Assert.Contains("tax.reason.article34_11_free_zone_path_qualified", result.Reasons);
        Assert.Contains(result.RuleEvidence, rule => rule.RuleId == "UY-IVA-D220-ART34-11-C");
    }

    [Fact]
    public async Task Article34_11_free_zone_path_requires_provider_origin_evidence()
    {
        var evaluator = new Article34Numeral11ExportServiceEligibilityEvaluator();
        var context = Context(new Article34Numeral11Facts(
            Article34Numeral11ServiceKind.SoftwareRightsAssignment,
            RegulatoryFactStatus.NotMet,
            RegulatoryFactStatus.NotMet,
            RecipientInstalledInFreeZone: RegulatoryFactStatus.Confirmed,
            ProviderFromNonFreeNationalTerritory: RegulatoryFactStatus.Unknown));

        var result = await evaluator.EvaluateAsync(ServiceFacts(), context);

        Assert.Equal(ExportServiceEligibilityStatus.InsufficientEvidence, result.Status);
        Assert.Contains("provider_from_non_free_national_territory", result.MissingEvidence);
    }

    [Fact]
    public async Task Other_article34_families_fail_closed_until_explicitly_implemented()
    {
        var evaluator = new Article34Numeral11ExportServiceEligibilityEvaluator();
        var context = new ExportServiceEvaluationContext(ExportServiceRuleFamily.OtherArticle34);

        var result = await evaluator.EvaluateAsync(ServiceFacts(), context);

        Assert.Equal(ExportServiceEligibilityStatus.UnsupportedScenario, result.Status);
        Assert.Contains("tax.reason.article34_rule_family_not_supported_in_release1", result.Reasons);
    }

    [Fact]
    public async Task Missing_rule_family_requires_review_instead_of_inferring_from_customer_country()
    {
        var evaluator = new Article34Numeral11ExportServiceEligibilityEvaluator();

        var result = await evaluator.EvaluateAsync(ServiceFacts(), null);

        Assert.Equal(ExportServiceEligibilityStatus.InsufficientEvidence, result.Status);
        Assert.Contains("export_service_rule_family", result.MissingEvidence);
    }

    [Fact]
    public async Task Production_rule_pack_and_numeral11_evaluator_resolve_export_services_end_to_end()
    {
        var useCase = new ResolveTaxTreatmentUseCase(
            new UruguayRelease1TaxTreatmentRulePackProvider(),
            new Article34Numeral11ExportServiceEligibilityEvaluator(),
            new TaxTreatmentDecisionEngine());
        var context = Context(new Article34Numeral11Facts(
            Article34Numeral11ServiceKind.CustomSoftware,
            RegulatoryFactStatus.Confirmed,
            RegulatoryFactStatus.Confirmed));

        var decision = await useCase.ExecuteAsync(new ResolveTaxTreatmentRequest(
            "company-1",
            EffectiveOn,
            TaxOperationKind.Services,
            ForeignReceiver(),
            ServicePerformanceScope: ServicePerformanceScope.EntirelyInUruguay,
            ServiceUseCountry: "AR",
            EvidenceReferences: new[] { "contract:software-001", "evidence:exclusive-use-abroad" },
            ExportServiceContext: context));

        Assert.Equal(TaxDecisionStatus.Resolved, decision.Status);
        Assert.Equal(TaxTreatmentClassification.ExportServices, decision.Classification);
        Assert.Equal("EXPORT_SERVICES", decision.TreatmentCode);
        Assert.Equal(UruguayRelease1TaxTreatmentRulePackProvider.PackVersion, decision.RulePackVersion);
        Assert.Contains(decision.RuleEvidence, rule => rule.RuleId == "UY-IVA-D220-ART34-11-B");
    }

    private static ExportServiceEvaluationContext Context(Article34Numeral11Facts facts) =>
        new(ExportServiceRuleFamily.Article34Numeral11, facts);

    private static TaxTransactionFacts ServiceFacts() =>
        new(
            "company-1",
            EffectiveOn,
            TaxOperationKind.Services,
            ForeignReceiver(),
            servicePerformanceScope: ServicePerformanceScope.EntirelyInUruguay,
            serviceUseCountry: "AR",
            evidenceReferences: new[] { "evidence:regulatory-facts" });

    private static ReceiverTaxFacts ForeignReceiver() =>
        new("AR", "AR", new[] { new ReceiverFiscalIdentityFact("6", "AR") });
}
