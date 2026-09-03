using EFactura.Application.Inventory;
using EFactura.Application.Sales;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;
using EFactura.Domain.Sales;
using EFactura.Domain.Taxation;
using Xunit;

namespace CrossCuttingTests;

public sealed class SaleSettlementPlanningTests
{
    private static readonly DateOnly SaleDate = new(2026, 7, 1);
    private static readonly Guid SaleId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid CustomerId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid MethodId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void Full_immediate_payment_covers_authoritative_confirmation_total_without_receivable()
    {
        var sale = Sale(customerId: null);
        var plan = Planner().Prepare(Request(
            sale,
            total: 122m,
            payments: new[] { Payment(122m) },
            methods: new[] { Method() }));

        Assert.Equal(SaleSettlementKind.ImmediatePayment, plan.Kind);
        Assert.Equal(122m, plan.SaleTotal);
        Assert.Equal(122m, Assert.Single(plan.ImmediatePayments).Amount);
        Assert.Null(plan.Receivable);
        Assert.Equal(64, plan.SettlementFingerprint.Length);
    }

    [Fact]
    public void Full_credit_derives_receivable_from_authoritative_total()
    {
        var sale = Sale(CustomerId);
        var dueDate = SaleDate.AddDays(30);
        var plan = Planner().Prepare(Request(
            sale,
            total: 250m,
            credit: new SaleCreditTerms(dueDate)));

        Assert.Equal(SaleSettlementKind.CreditReceivable, plan.Kind);
        Assert.Empty(plan.ImmediatePayments);
        Assert.NotNull(plan.Receivable);
        Assert.Equal(CustomerId, plan.Receivable!.CustomerPartyId);
        Assert.Equal(250m, plan.Receivable.OriginalAmount);
        Assert.Equal("UYU", plan.Receivable.CurrencyCode);
        Assert.Equal(dueDate, plan.Receivable.DueDate);
    }

    [Fact]
    public void Mixed_payment_and_credit_derives_only_residual_receivable()
    {
        var sale = Sale(CustomerId);
        var plan = Planner().Prepare(Request(
            sale,
            total: 100m,
            payments: new[] { Payment(40m) },
            methods: new[] { Method() },
            credit: new SaleCreditTerms(SaleDate.AddDays(15))));

        Assert.Equal(SaleSettlementKind.Mixed, plan.Kind);
        Assert.Equal(40m, Assert.Single(plan.ImmediatePayments).Amount);
        Assert.Equal(60m, plan.Receivable!.OriginalAmount);
    }

    [Fact]
    public void Overpayment_fails_closed_until_advance_policy_is_accepted()
    {
        var exception = Assert.Throws<DomainRuleException>(() => Planner().Prepare(Request(
            Sale(CustomerId),
            total: 100m,
            payments: new[] { Payment(101m) },
            methods: new[] { Method() })));

        Assert.Equal("sales.settlement.overpayment_not_supported", exception.Code);
    }

    [Fact]
    public void Partial_payment_without_credit_terms_cannot_leave_uncovered_balance()
    {
        var exception = Assert.Throws<DomainRuleException>(() => Planner().Prepare(Request(
            Sale(CustomerId),
            total: 100m,
            payments: new[] { Payment(99m) },
            methods: new[] { Method() })));

        Assert.Equal("sales.settlement.uncovered_balance", exception.Code);
    }

    [Fact]
    public void Credit_requires_identified_customer()
    {
        var exception = Assert.Throws<DomainRuleException>(() => Planner().Prepare(Request(
            Sale(customerId: null),
            total: 100m,
            credit: new SaleCreditTerms(SaleDate.AddDays(10)))));

        Assert.Equal("sales.settlement.credit_customer_required", exception.Code);
    }

    [Fact]
    public void Credit_due_date_cannot_precede_sale_business_date()
    {
        var exception = Assert.Throws<DomainRuleException>(() => Planner().Prepare(Request(
            Sale(CustomerId),
            total: 100m,
            credit: new SaleCreditTerms(SaleDate.AddDays(-1)))));

        Assert.Equal("sales.settlement.credit_due_date_invalid", exception.Code);
    }

    [Fact]
    public void Disabled_payment_method_cannot_be_planned()
    {
        var exception = Assert.Throws<DomainRuleException>(() => Planner().Prepare(Request(
            Sale(customerId: null),
            total: 100m,
            payments: new[] { Payment(100m) },
            methods: new[] { Method(enabled: false) })));

        Assert.Equal("sales.settlement.payment_method_disabled", exception.Code);
    }

    [Fact]
    public void Payment_method_requires_authoritative_evidence()
    {
        var exception = Assert.Throws<DomainRuleException>(() => Planner().Prepare(Request(
            Sale(customerId: null),
            total: 100m,
            payments: new[] { Payment(100m) })));

        Assert.Equal("sales.settlement.payment_method_evidence_missing", exception.Code);
    }

    [Fact]
    public void Cross_currency_payment_fails_closed_until_fx_settlement_policy_exists()
    {
        var exception = Assert.Throws<DomainRuleException>(() => Planner().Prepare(Request(
            Sale(customerId: null),
            total: 100m,
            payments: new[] { Payment(100m, currency: "USD") },
            methods: new[] { Method() })));

        Assert.Equal("sales.settlement.cross_currency_not_supported", exception.Code);
    }

    [Fact]
    public void Settlement_fingerprint_changes_when_payment_method_version_changes()
    {
        var sale = Sale(customerId: null);
        var planner = Planner();
        var first = planner.Prepare(Request(
            sale,
            100m,
            new[] { Payment(100m) },
            new[] { Method(version: 7) }));
        var second = planner.Prepare(Request(
            sale,
            100m,
            new[] { Payment(100m) },
            new[] { Method(version: 8) }));

        Assert.NotEqual(first.SettlementFingerprint, second.SettlementFingerprint);
    }

    [Fact]
    public void Zero_total_sale_produces_no_charge_plan_and_no_money_effects()
    {
        var plan = Planner().Prepare(Request(Sale(customerId: null), total: 0m));

        Assert.Equal(SaleSettlementKind.NoCharge, plan.Kind);
        Assert.Empty(plan.ImmediatePayments);
        Assert.Null(plan.Receivable);
    }

    private static SaleSettlementPlanner Planner() => new();

    private static SaleSettlementPlanningRequest Request(
        Sale sale,
        decimal total,
        IReadOnlyCollection<SaleImmediatePaymentIntent>? payments = null,
        IReadOnlyCollection<SalePaymentMethodEvidence>? methods = null,
        SaleCreditTerms? credit = null) =>
        new(
            sale,
            Confirmation(sale, total),
            payments ?? Array.Empty<SaleImmediatePaymentIntent>(),
            methods ?? Array.Empty<SalePaymentMethodEvidence>(),
            credit);

    private static Sale Sale(Guid? customerId, string currency = "UYU")
    {
        var line = SaleLine.Create(
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            "ITEM-1",
            "Item",
            SaleLineKind.Product,
            1m,
            100m,
            null);

        return EFactura.Domain.Sales.Sale.Rehydrate(
            SaleId,
            "org-1",
            "loc-1",
            "term-1",
            customerId,
            SaleCommercialIntent.ConsumerFinal,
            currency,
            SaleDate,
            "UY",
            false,
            new[] { line },
            SaleStatus.Validated,
            "validated-fingerprint",
            new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero),
            4);
    }

    private static SaleConfirmationPlan Confirmation(Sale sale, decimal total) =>
        new(
            sale.Id,
            sale.Version,
            "validated-fingerprint",
            new string('a', 64),
            new CfeSelectionResult(
                CfeSelectionStatus.Selected,
                CfeFamily.ETicket,
                ReceiverIdentificationRequirement.Optional,
                Array.Empty<CfeCandidate>(),
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<RegulatoryRuleEvidence>(),
                "25.2"),
            new CfeArithmeticResult(
                sale.CurrencyCode,
                "25.2",
                "TEST-ARITHMETIC",
                Array.Empty<CfeArithmeticLineResult>(),
                new CfeArithmeticTotals(total, 0m, 0m, 0m, 0m, 0m, 0m, total),
                Array.Empty<RegulatoryRuleEvidence>()),
            new InventoryAvailabilityResult(
                true,
                Array.Empty<InventoryAvailabilityLineResult>(),
                Array.Empty<string>()));

    private static SaleImmediatePaymentIntent Payment(decimal amount, string currency = "UYU") =>
        new(MethodId, amount, currency, "ref-1");

    private static SalePaymentMethodEvidence Method(long version = 7, bool enabled = true) =>
        new(MethodId, "org-1", version, enabled);
}
