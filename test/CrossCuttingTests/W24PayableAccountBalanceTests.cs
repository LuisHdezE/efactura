using EFactura.Application.Payables;
using EFactura.Domain.Common;
using EFactura.Domain.Payables;
using Xunit;

namespace CrossCuttingTests;

public sealed class W24PayableAccountBalanceTests
{
    private const string Organization = "company-1";
    private static readonly Guid PartyId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTimeOffset AsOfUtc = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Payable_requires_an_explicit_accepted_business_source()
    {
        var fromReceipt = Payable.CreateFromPurchaseReceipt(
            Guid.NewGuid(), Organization, PartyId, "receipt-42", 100m, "UYU",
            new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1), AsOfUtc);
        var fromReceivedCfe = Payable.CreateFromReceivedFiscalDocument(
            Guid.NewGuid(), Organization, PartyId, "received-cfe-42", 50m, "USD",
            new DateOnly(2026, 9, 2), new DateOnly(2026, 10, 2), AsOfUtc);

        Assert.Equal(PayableSourceKind.PurchaseReceipt, fromReceipt.SourceKind);
        Assert.Equal("receipt-42", fromReceipt.SourceId);
        Assert.Equal(PayableSourceKind.ReceivedFiscalDocument, fromReceivedCfe.SourceKind);
        Assert.Equal("received-cfe-42", fromReceivedCfe.SourceId);
    }

    [Fact]
    public async Task Projection_applies_adjustments_supplier_payments_reversals_and_keeps_currencies_isolated()
    {
        var current = Source(100m, "UYU", new DateOnly(2026, 9, 23));

        var overdue1To30 = Source(300m, "UYU", new DateOnly(2026, 9, 22));
        var partialAllocation = PayableBalanceEffect.CreateSupplierPaymentAllocation(
            Guid.NewGuid(), Organization, overdue1To30.PayableId, 100m, "supplier-payment-1", 0, AsOfUtc.AddDays(-1));
        overdue1To30 = overdue1To30 with { Effects = new[] { partialAllocation } };

        var overdue31To60 = Source(200m, "UYU", new DateOnly(2026, 8, 23));
        var allocation = PayableBalanceEffect.CreateSupplierPaymentAllocation(
            Guid.NewGuid(), Organization, overdue31To60.PayableId, 80m, "supplier-payment-2", 0, AsOfUtc.AddDays(-5));
        var reversal = PayableBalanceEffect.CreateSupplierPaymentReversal(
            Guid.NewGuid(), Organization, overdue31To60.PayableId, 80m, "supplier-payment-reversal-2", 0, allocation.Id, AsOfUtc.AddDays(-4));
        var adjustment = PayableBalanceEffect.CreateAdjustment(
            Guid.NewGuid(), Organization, overdue31To60.PayableId, 20m, false, "adjustment-2", 0, AsOfUtc.AddDays(-3));
        overdue31To60 = overdue31To60 with { Effects = new[] { allocation, reversal, adjustment } };

        var settledUsd = Source(50m, "USD", new DateOnly(2026, 6, 24));
        var settleUsd = PayableBalanceEffect.CreateSupplierPaymentAllocation(
            Guid.NewGuid(), Organization, settledUsd.PayableId, 50m, "supplier-payment-usd", 0, AsOfUtc.AddDays(-1));
        settledUsd = settledUsd with { Effects = new[] { settleUsd } };

        var model = new PartyPayableAccountReadModel(
            new FakeSourceReader(current, overdue1To30, overdue31To60, settledUsd));

        var result = await model.GetAsync(Organization, PartyId, AsOfUtc);

        Assert.Equal(2, result.Currencies.Count);

        var uyu = Assert.Single(result.Currencies, x => x.CurrencyCode == "UYU");
        Assert.Equal(480m, uyu.Outstanding);
        Assert.Equal(380m, uyu.Overdue);
        Assert.Equal(100m, uyu.Aging.Current);
        Assert.Equal(200m, uyu.Aging.Days1To30);
        Assert.Equal(180m, uyu.Aging.Days31To60);
        Assert.Equal(0m, uyu.Aging.Days61To90);
        Assert.Equal(0m, uyu.Aging.Days91Plus);

        var usd = Assert.Single(result.Currencies, x => x.CurrencyCode == "USD");
        Assert.Equal(0m, usd.Outstanding);
        Assert.Equal(0m, usd.Overdue);
    }

    [Theory]
    [InlineData(0, "current")]
    [InlineData(1, "1-30")]
    [InlineData(30, "1-30")]
    [InlineData(31, "31-60")]
    [InlineData(60, "31-60")]
    [InlineData(61, "61-90")]
    [InlineData(90, "61-90")]
    [InlineData(91, "91+")]
    public async Task Aging_boundaries_follow_governed_calendar_day_buckets(int daysOverdue, string expectedBucket)
    {
        var asOfDate = DateOnly.FromDateTime(AsOfUtc.UtcDateTime);
        var source = Source(125m, "UYU", asOfDate.AddDays(-daysOverdue));
        var model = new PartyPayableAccountReadModel(new FakeSourceReader(source));

        var result = await model.GetAsync(Organization, PartyId, AsOfUtc);
        var currency = Assert.Single(result.Currencies);

        Assert.Equal(expectedBucket == "current" ? 125m : 0m, currency.Aging.Current);
        Assert.Equal(expectedBucket == "1-30" ? 125m : 0m, currency.Aging.Days1To30);
        Assert.Equal(expectedBucket == "31-60" ? 125m : 0m, currency.Aging.Days31To60);
        Assert.Equal(expectedBucket == "61-90" ? 125m : 0m, currency.Aging.Days61To90);
        Assert.Equal(expectedBucket == "91+" ? 125m : 0m, currency.Aging.Days91Plus);
    }

    [Fact]
    public async Task Projection_fails_closed_when_supplier_payment_over_allocates_payable()
    {
        var source = Source(100m, "UYU", new DateOnly(2026, 9, 20));
        var allocation = PayableBalanceEffect.CreateSupplierPaymentAllocation(
            Guid.NewGuid(), Organization, source.PayableId, 100.000001m, "supplier-payment-over", 0, AsOfUtc.AddDays(-1));
        source = source with { Effects = new[] { allocation } };
        var model = new PartyPayableAccountReadModel(new FakeSourceReader(source));

        var error = await Assert.ThrowsAsync<DomainRuleException>(
            () => model.GetAsync(Organization, PartyId, AsOfUtc));

        Assert.Equal("payables.account.negative_balance", error.Code);
    }

    [Fact]
    public async Task Projection_fails_closed_when_reversal_does_not_match_allocation_amount()
    {
        var source = Source(100m, "UYU", new DateOnly(2026, 9, 20));
        var allocation = PayableBalanceEffect.CreateSupplierPaymentAllocation(
            Guid.NewGuid(), Organization, source.PayableId, 40m, "supplier-payment-3", 0, AsOfUtc.AddDays(-2));
        var reversal = PayableBalanceEffect.CreateSupplierPaymentReversal(
            Guid.NewGuid(), Organization, source.PayableId, 30m, "supplier-payment-reversal-3", 0, allocation.Id, AsOfUtc.AddDays(-1));
        source = source with { Effects = new[] { allocation, reversal } };
        var model = new PartyPayableAccountReadModel(new FakeSourceReader(source));

        var error = await Assert.ThrowsAsync<DomainRuleException>(
            () => model.GetAsync(Organization, PartyId, AsOfUtc));

        Assert.Equal("payables.account.reversal_amount_mismatch", error.Code);
    }

    [Fact]
    public async Task Projection_ignores_future_effects_for_as_of_snapshot()
    {
        var source = Source(100m, "UYU", new DateOnly(2026, 9, 23));
        var futureAllocation = PayableBalanceEffect.CreateSupplierPaymentAllocation(
            Guid.NewGuid(), Organization, source.PayableId, 75m, "supplier-payment-future", 0, AsOfUtc.AddMinutes(1));
        source = source with { Effects = new[] { futureAllocation } };
        var model = new PartyPayableAccountReadModel(new FakeSourceReader(source));

        var result = await model.GetAsync(Organization, PartyId, AsOfUtc);

        Assert.Equal(100m, Assert.Single(result.Currencies).Outstanding);
    }

    [Fact]
    public async Task Projection_rejects_source_outside_requested_party_scope()
    {
        var source = Source(100m, "UYU", new DateOnly(2026, 9, 23)) with
        {
            PartyId = Guid.Parse("44444444-4444-4444-4444-444444444444")
        };
        var model = new PartyPayableAccountReadModel(new FakeSourceReader(source));

        var error = await Assert.ThrowsAsync<DomainRuleException>(
            () => model.GetAsync(Organization, PartyId, AsOfUtc));

        Assert.Equal("payables.account.source_scope_mismatch", error.Code);
    }

    private static PayableBalanceSource Source(
        decimal originalAmount,
        string currency,
        DateOnly dueDate) =>
        new(
            Guid.NewGuid(),
            Organization,
            PartyId,
            originalAmount,
            currency,
            dueDate,
            AsOfUtc.AddMonths(-2),
            Array.Empty<PayableBalanceEffect>());

    private sealed class FakeSourceReader : IPayableBalanceSourceReader
    {
        private readonly IReadOnlyList<PayableBalanceSource> _sources;

        public FakeSourceReader(params PayableBalanceSource[] sources) => _sources = sources;

        public Task<IReadOnlyList<PayableBalanceSource>> ListByPartyAsync(
            string organizationId,
            Guid partyId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_sources);
    }
}
