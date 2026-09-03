using EFactura.Application.Inventory;
using EFactura.Application.Sales;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Sales;
using EFactura.Domain.Taxation;
using Xunit;

namespace CrossCuttingTests;

public sealed class SaleConfirmationPlanningTests
{
    private static readonly DateOnly FiscalDate = new(2026, 7, 1);

    [Fact]
    public void Validated_plan_uses_authoritative_header_bucket_arithmetic_and_preserves_inventory_version()
    {
        var itemId = Guid.NewGuid();
        var lines = new[]
        {
            Line(Guid.NewGuid(), itemId, 0.03m, BasicRate()),
            Line(Guid.NewGuid(), itemId, 0.03m, BasicRate())
        };
        var inventory = Inventory(itemId, required: 2m, available: 10m, version: 7);

        var plan = new SaleConfirmationPlanner().Prepare(Request(lines, inventory));

        Assert.Equal(CfeFamily.ETicket, plan.Selection.SelectedFamily);
        Assert.Equal("25.2", plan.Selection.FormatVersion);
        Assert.NotEmpty(plan.Selection.RuleEvidence);
        Assert.Equal(0.06m, plan.FiscalCalculation.Totals.NetAmount);
        Assert.Equal(0.01m, plan.FiscalCalculation.Totals.BasicVatAmount);
        Assert.Equal(0.01m, plan.FiscalCalculation.Totals.VatAmount);
        Assert.Equal(0.07m, plan.FiscalCalculation.Totals.TotalAmount);
        Assert.Equal(7L, Assert.Single(plan.Inventory.Lines).PositionVersion);
        Assert.Equal(64, plan.ConfirmationFingerprint.Length);
        Assert.Equal("validated-fingerprint", plan.ValidationFingerprint);
    }

    [Fact]
    public void Draft_sale_cannot_produce_confirmation_plan()
    {
        var itemId = Guid.NewGuid();
        var request = Request(
            new[] { Line(Guid.NewGuid(), itemId, 10m, BasicRate()) },
            Inventory(itemId, 1m, 5m, 2),
            status: SaleStatus.Draft);

        var exception = Assert.Throws<DomainRuleException>(() => new SaleConfirmationPlanner().Prepare(request));

        Assert.Equal("sales.confirmation.validation_required", exception.Code);
    }

    [Fact]
    public void Inventory_quantity_must_match_product_requirements_exactly()
    {
        var itemId = Guid.NewGuid();
        var request = Request(
            new[] { Line(Guid.NewGuid(), itemId, 10m, BasicRate()) },
            Inventory(itemId, required: 2m, available: 5m, version: 2));

        var exception = Assert.Throws<DomainRuleException>(() => new SaleConfirmationPlanner().Prepare(request));

        Assert.Equal("sales.confirmation.inventory_quantity_mismatch", exception.Code);
    }

    [Fact]
    public void Tracked_inventory_requires_authoritative_position_version()
    {
        var itemId = Guid.NewGuid();
        var inventory = new InventoryAvailabilityResult(
            true,
            new[]
            {
                new InventoryAvailabilityLineResult(itemId, true, 1m, 5m, null, true, null)
            },
            Array.Empty<string>());
        var request = Request(
            new[] { Line(Guid.NewGuid(), itemId, 10m, BasicRate()) },
            inventory);

        var exception = Assert.Throws<DomainRuleException>(() => new SaleConfirmationPlanner().Prepare(request));

        Assert.Equal("sales.confirmation.inventory_position_unresolved", exception.Code);
    }

    [Fact]
    public void Confirmation_fingerprint_changes_when_inventory_version_changes()
    {
        var itemId = Guid.NewGuid();
        var line = Line(Guid.NewGuid(), itemId, 10m, BasicRate());
        var planner = new SaleConfirmationPlanner();

        var first = planner.Prepare(Request(
            new[] { line },
            Inventory(itemId, 1m, 5m, 11)));
        var second = planner.Prepare(Request(
            new[] { line },
            Inventory(itemId, 1m, 5m, 12)));

        Assert.NotEqual(first.ConfirmationFingerprint, second.ConfirmationFingerprint);
    }

    [Fact]
    public void Unresolved_tax_rate_fails_closed_before_confirmation_effects_exist()
    {
        var itemId = Guid.NewGuid();
        var unresolved = new TaxRateResolution(
            TaxRateResolutionStatus.RequiresReview,
            VatLiabilityKind.RequiresReview,
            VatRateKind.Unsupported,
            null,
            null,
            null,
            "REQUIRES_REVIEW",
            new[] { "tax.rate.unresolved" },
            new[] { "tax_profile" },
            new[] { Evidence("TEST-UNRESOLVED") },
            "TEST-RATE-PACK");
        var request = Request(
            new[] { Line(Guid.NewGuid(), itemId, 10m, unresolved) },
            Inventory(itemId, 1m, 5m, 3));

        var exception = Assert.Throws<DomainRuleException>(() => new SaleConfirmationPlanner().Prepare(request));

        Assert.Equal("fiscal.arithmetic.tax_rate_unresolved", exception.Code);
    }

    [Fact]
    public void Cfe_selection_without_regulatory_provenance_fails_closed()
    {
        var itemId = Guid.NewGuid();
        var selection = new CfeSelectionResult(
            CfeSelectionStatus.Selected,
            CfeFamily.ETicket,
            ReceiverIdentificationRequirement.Optional,
            new[] { new CfeCandidate(CfeFamily.ETicket, ReceiverIdentificationRequirement.Optional, new[] { "test.candidate" }) },
            new[] { "fiscal.selection.single_candidate_selected" },
            Array.Empty<string>(),
            Array.Empty<RegulatoryRuleEvidence>(),
            "25.2");
        var request = Request(
            new[] { Line(Guid.NewGuid(), itemId, 10m, BasicRate()) },
            Inventory(itemId, 1m, 5m, 3),
            selection: selection);

        var exception = Assert.Throws<DomainRuleException>(() => new SaleConfirmationPlanner().Prepare(request));

        Assert.Equal("sales.confirmation.selection_rule_evidence_required", exception.Code);
    }

    private static SaleConfirmationPlanningRequest Request(
        IReadOnlyCollection<SaleConfirmationPlanningLine> lines,
        InventoryAvailabilityResult inventory,
        SaleStatus status = SaleStatus.Validated,
        CfeSelectionResult? selection = null) =>
        new(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            4,
            status,
            FiscalDate,
            "UYU",
            "validated-fingerprint",
            selection ?? Selection(),
            lines,
            inventory);

    private static SaleConfirmationPlanningLine Line(
        Guid lineId,
        Guid itemId,
        decimal unitPrice,
        TaxRateResolution rate) =>
        new(
            lineId,
            itemId,
            SaleLineKind.Product,
            1m,
            unitPrice,
            rate);

    private static InventoryAvailabilityResult Inventory(
        Guid itemId,
        decimal required,
        decimal available,
        long version) =>
        new(
            true,
            new[]
            {
                new InventoryAvailabilityLineResult(
                    itemId,
                    true,
                    required,
                    available,
                    version,
                    available >= required,
                    available >= required ? null : "inventory.insufficient_stock")
            },
            Array.Empty<string>());

    private static CfeSelectionResult Selection() =>
        new(
            CfeSelectionStatus.Selected,
            CfeFamily.ETicket,
            ReceiverIdentificationRequirement.Optional,
            new[]
            {
                new CfeCandidate(
                    CfeFamily.ETicket,
                    ReceiverIdentificationRequirement.Optional,
                    new[] { "test.candidate" })
            },
            new[] { "fiscal.selection.single_candidate_selected" },
            Array.Empty<string>(),
            new[] { Evidence("TEST-CFE-SELECTION") },
            "25.2");

    private static TaxRateResolution BasicRate() =>
        new(
            TaxRateResolutionStatus.Resolved,
            VatLiabilityKind.VatDue,
            VatRateKind.Basic,
            22m,
            null,
            null,
            "VAT_BASIC",
            Array.Empty<string>(),
            Array.Empty<string>(),
            new[] { Evidence("TEST-VAT-BASIC-22") },
            "TEST-UY-VAT-R1");

    private static RegulatoryRuleEvidence Evidence(string id) =>
        new(
            id,
            "Test regulatory source",
            "https://example.test/regulatory-source",
            "test-v1",
            new DateOnly(2026, 6, 30),
            clause: "test evidence covering the CFE 25.2 planning date");
}
