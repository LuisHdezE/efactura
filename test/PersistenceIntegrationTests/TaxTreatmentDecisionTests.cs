using EFactura.Domain.Taxation;
using Xunit;

namespace PersistenceIntegrationTests;

public sealed class TaxTreatmentDecisionTests
{
    private static readonly DateOnly EffectiveOn = new(2026, 8, 29);

    [Fact]
    public void Foreign_receiver_local_goods_remains_domestic_without_using_nationality_shortcut()
    {
        var engine = new TaxTreatmentDecisionEngine();
        var receiver = ForeignReceiverWithoutUruguayanRuc();
        var facts = new TaxTransactionFacts(
            "company-1",
            EffectiveOn,
            TaxOperationKind.Goods,
            receiver,
            goodsMovementScope: GoodsMovementScope.DomesticDelivery,
            deliveryCountry: "UY");

        var decision = engine.Resolve(facts, RulePack());

        Assert.Equal(TaxDecisionStatus.Resolved, decision.Status);
        Assert.Equal(TaxTreatmentClassification.Domestic, decision.Classification);
        Assert.Equal("DOMESTIC", decision.TreatmentCode);
        Assert.False(receiver.HasUruguayanRuc);
    }

    [Fact]
    public void Presence_of_uruguayan_ruc_does_not_change_tax_classification_for_same_local_transaction()
    {
        var engine = new TaxTreatmentDecisionEngine();
        var withoutRuc = new TaxTransactionFacts(
            "company-1",
            EffectiveOn,
            TaxOperationKind.Goods,
            ForeignReceiverWithoutUruguayanRuc(),
            goodsMovementScope: GoodsMovementScope.DomesticDelivery,
            deliveryCountry: "UY");
        var withRuc = new TaxTransactionFacts(
            "company-1",
            EffectiveOn,
            TaxOperationKind.Goods,
            ForeignReceiverWithUruguayanRuc(),
            goodsMovementScope: GoodsMovementScope.DomesticDelivery,
            deliveryCountry: "UY");

        var first = engine.Resolve(withoutRuc, RulePack());
        var second = engine.Resolve(withRuc, RulePack());

        Assert.Equal(TaxTreatmentClassification.Domestic, first.Classification);
        Assert.Equal(first.Classification, second.Classification);
        Assert.True(withRuc.Receiver.HasUruguayanRuc);
    }

    [Fact]
    public void Foreign_receiver_without_confirmed_goods_export_evidence_does_not_become_export()
    {
        var engine = new TaxTreatmentDecisionEngine();
        var facts = new TaxTransactionFacts(
            "company-1",
            EffectiveOn,
            TaxOperationKind.Goods,
            ForeignReceiverWithoutUruguayanRuc(),
            goodsMovementScope: GoodsMovementScope.Unknown,
            deliveryCountry: "AR");

        var decision = engine.Resolve(facts, RulePack());

        Assert.Equal(TaxDecisionStatus.RequiresReview, decision.Status);
        Assert.Equal(TaxTreatmentClassification.RequiresReview, decision.Classification);
        Assert.Contains("goods_movement_scope", decision.MissingFacts);
        Assert.DoesNotContain(decision.Reasons, reason => reason.Contains("foreign", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Confirmed_goods_export_is_resolved_as_export_goods_with_rule_evidence()
    {
        var engine = new TaxTreatmentDecisionEngine();
        var facts = new TaxTransactionFacts(
            "company-1",
            EffectiveOn,
            TaxOperationKind.Goods,
            ForeignReceiverWithoutUruguayanRuc(),
            goodsMovementScope: GoodsMovementScope.ExportConfirmed,
            deliveryCountry: "AR",
            evidenceReferences: new[] { "shipment:EXP-2026-0001" });

        var decision = engine.Resolve(facts, RulePack());

        Assert.Equal(TaxTreatmentClassification.ExportGoods, decision.Classification);
        Assert.Equal("EXPORT_GOODS", decision.TreatmentCode);
        Assert.Contains(decision.RuleEvidence, rule => rule.RuleId == "UY-IVA-T10-ART5-EXPORT-GOODS");
    }

    [Fact]
    public void Service_performed_entirely_outside_uruguay_is_outside_vat_territorial_scope()
    {
        var engine = new TaxTreatmentDecisionEngine();
        var facts = new TaxTransactionFacts(
            "company-1",
            EffectiveOn,
            TaxOperationKind.Services,
            ForeignReceiverWithoutUruguayanRuc(),
            servicePerformanceScope: ServicePerformanceScope.EntirelyOutsideUruguay,
            serviceUseCountry: "AR");

        var decision = engine.Resolve(facts, RulePack());

        Assert.Equal(TaxTreatmentClassification.OutsideVatTerritorialScope, decision.Classification);
        Assert.Equal("OUTSIDE_VAT_SCOPE", decision.TreatmentCode);
        Assert.Contains(decision.RuleEvidence, rule => rule.RuleId == "UY-IVA-T10-ART5-TERRITORIALITY");
    }

    [Fact]
    public void Service_in_uruguay_qualifying_under_article_34_resolves_export_services_and_preserves_provenance()
    {
        var engine = new TaxTreatmentDecisionEngine();
        var facts = new TaxTransactionFacts(
            "company-1",
            EffectiveOn,
            TaxOperationKind.Services,
            ForeignReceiverWithoutUruguayanRuc(),
            servicePerformanceScope: ServicePerformanceScope.EntirelyInUruguay,
            serviceUseCountry: "AR",
            evidenceReferences: new[] { "contract:EXT-001", "evidence:exclusive-use-abroad" });
        var evaluation = new ExportServiceEligibilityEvaluation(
            ExportServiceEligibilityStatus.Qualified,
            new[] { Article34Numeral11Rule() },
            new[] { "tax.reason.article34_11_qualified" });

        var decision = engine.Resolve(facts, RulePack(), evaluation);

        Assert.Equal(TaxDecisionStatus.Resolved, decision.Status);
        Assert.Equal(TaxTreatmentClassification.ExportServices, decision.Classification);
        Assert.Equal("EXPORT_SERVICES", decision.TreatmentCode);
        Assert.Contains(decision.RuleEvidence, rule => rule.RuleId == "UY-IVA-D220-ART34-11");
        Assert.Equal("uy-iva-2026.08", decision.RulePackVersion);
    }

    [Fact]
    public void Service_to_foreign_receiver_that_does_not_qualify_under_article_34_is_domestic_classification()
    {
        var engine = new TaxTreatmentDecisionEngine();
        var facts = new TaxTransactionFacts(
            "company-1",
            EffectiveOn,
            TaxOperationKind.Services,
            ForeignReceiverWithoutUruguayanRuc(),
            servicePerformanceScope: ServicePerformanceScope.EntirelyInUruguay,
            serviceUseCountry: "UY");
        var evaluation = new ExportServiceEligibilityEvaluation(
            ExportServiceEligibilityStatus.NotQualified,
            new[] { Article34Numeral11Rule() },
            new[] { "tax.reason.article34_exclusive_use_abroad_not_met" });

        var decision = engine.Resolve(facts, RulePack(), evaluation);

        Assert.Equal(TaxTreatmentClassification.Domestic, decision.Classification);
        Assert.Equal("DOMESTIC", decision.TreatmentCode);
        Assert.Contains("tax.reason.article34_exclusive_use_abroad_not_met", decision.Reasons);
    }

    [Fact]
    public void Foreign_service_with_incomplete_article_34_evidence_requires_review_instead_of_zero_tax_assumption()
    {
        var engine = new TaxTreatmentDecisionEngine();
        var facts = new TaxTransactionFacts(
            "company-1",
            EffectiveOn,
            TaxOperationKind.Services,
            ForeignReceiverWithoutUruguayanRuc(),
            servicePerformanceScope: ServicePerformanceScope.EntirelyInUruguay,
            serviceUseCountry: "AR");
        var evaluation = new ExportServiceEligibilityEvaluation(
            ExportServiceEligibilityStatus.InsufficientEvidence,
            new[] { Article34Numeral11Rule() },
            new[] { "tax.reason.article34_use_abroad_not_proven" },
            new[] { "exclusive_use_abroad_evidence" });

        var decision = engine.Resolve(facts, RulePack(), evaluation);

        Assert.Equal(TaxDecisionStatus.RequiresReview, decision.Status);
        Assert.Equal("REQUIRES_REVIEW", decision.TreatmentCode);
        Assert.Contains("exclusive_use_abroad_evidence", decision.MissingFacts);
    }

    [Fact]
    public void Mixed_operation_requires_line_level_resolution()
    {
        var engine = new TaxTreatmentDecisionEngine();
        var facts = new TaxTransactionFacts(
            "company-1",
            EffectiveOn,
            TaxOperationKind.Mixed,
            ForeignReceiverWithUruguayanRuc());

        var decision = engine.Resolve(facts, RulePack());

        Assert.Equal(TaxDecisionStatus.RequiresReview, decision.Status);
        Assert.Contains("line_level_operation_kind", decision.MissingFacts);
    }

    [Fact]
    public void Rule_pack_outside_effective_range_is_rejected()
    {
        var engine = new TaxTreatmentDecisionEngine();
        var facts = new TaxTransactionFacts(
            "company-1",
            new DateOnly(2023, 12, 31),
            TaxOperationKind.Goods,
            ForeignReceiverWithoutUruguayanRuc(),
            goodsMovementScope: GoodsMovementScope.DomesticDelivery);

        var exception = Assert.Throws<EFactura.Domain.Common.DomainRuleException>(() => engine.Resolve(facts, RulePack()));

        Assert.Equal("tax.rule_not_effective", exception.Code);
    }

    private static ReceiverTaxFacts ForeignReceiverWithoutUruguayanRuc() =>
        new(
            "AR",
            "AR",
            new[] { new ReceiverFiscalIdentityFact("6", "AR") });

    private static ReceiverTaxFacts ForeignReceiverWithUruguayanRuc() =>
        new(
            "AR",
            "AR",
            new[]
            {
                new ReceiverFiscalIdentityFact("6", "AR"),
                new ReceiverFiscalIdentityFact("2", "UY")
            });

    private static TaxTreatmentRulePack RulePack() =>
        new(
            "uy-iva-2026.08",
            new RegulatoryRuleEvidence(
                "UY-IVA-T10-ART5-TERRITORIALITY",
                "IMPO - T.O. 2023 Título 10",
                "https://www.impo.com.uy/bases/todgi2023/101-2024/5_T10",
                "T.O. 2023 current rule reviewed 2026-08-29",
                new DateOnly(2024, 5, 16),
                clause: "Artículo 5 - Territorialidad"),
            new RegulatoryRuleEvidence(
                "UY-IVA-T10-ART5-EXPORT-GOODS",
                "IMPO - T.O. 2023 Título 10",
                "https://www.impo.com.uy/bases/todgi2023/101-2024/5_T10",
                "T.O. 2023 current rule reviewed 2026-08-29",
                new DateOnly(2024, 5, 16),
                clause: "Artículo 5 - exportaciones de bienes no gravadas"));

    private static RegulatoryRuleEvidence Article34Numeral11Rule() =>
        new(
            "UY-IVA-D220-ART34-11",
            "IMPO - Decreto 220/998 actualizado",
            "https://www.impo.com.uy/bases/decretos/220-1998/34",
            "Artículo 34 actualizado, reviewed 2026-08-29",
            new DateOnly(2024, 5, 16),
            clause: "Artículo 34 numeral 11");
}
