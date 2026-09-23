using EFactura.Application.Receivables;
using EFactura.Domain.Common;
using EFactura.Domain.Receivables;
using Xunit;

namespace CrossCuttingTests;

public sealed class W24ReceivableAccountBalanceTests
{
    private const string Organization = "company-1";
    private static readonly Guid PartyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset AsOfUtc = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Projection_applies_adjustments_allocations_reversals_and_keeps_currencies_isolated()
    {
        var current = Source(100m, "UYU", new DateOnly(2026, 9, 23));
        var overdue1To30 = Source(
            300m,
            "UYU",
            new DateOnly(2026, 9, 22),
            ReceivableBalanceEffect.CreateCollectionAllocation(
                Guid.NewGuid(), Organization, Guid.Empty, 100m, "collection-1", 0, AsOfUtc.AddDays(-1)));
        overdue1To30 = RebindEffects(overdue1To30);

        var overdue31To60 = Source(200m, "UYU", new DateOnly(2026, 8, 23));
        var allocation = ReceivableBalanceEffect.CreateCollectionAllocation(
            Guid.NewGuid(), Organization, overdue31To60.ReceivableId, 80m, "collection-2", 0, AsOfUtc.AddDays(-5));
        var reversal = ReceivableBalanceEffect.CreateCollectionReversal(
            Guid.NewGuid(), Organization, overdue31To60.ReceivableId, 80m, "reversal-2", 0, allocation.Id, AsOfUtc.AddDays(-4));
        var adjustment = ReceivableBalanceEffect.CreateAdjustment(
            Guid.NewGuid(), Organization, overdue31To60.ReceivableId, 20m, false, "adjustment-2", 0, AsOfUtc.AddDays(-3));
        overdue31To60 = overdue31To60 with { Effects = new[] { allocation, reversal, adjustment } };

        var settledUsd = Source(50m, "USD", new DateOnly(2026, 6, 24));
        var settleUsd = ReceivableBalanceEffect.CreateCollectionAllocation(
            Guid.NewGuid(), Organization, settledUsd.ReceivableId, 50m, "collection-usd", 0, AsOfUtc.AddDays(-1));
        settledUsd = settledUsd with { Effects = new[] { settleUsd } };

        var model = new PartyReceivableAccountReadModel(
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
        Assert.Equal(0m, usd.Aging.Days91Plus);
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
        var model = new PartyReceivableAccountReadModel(new FakeSourceReader(source));

        var result = await model.GetAsync(Organization, PartyId, AsOfUtc);
        var currency = Assert.Single(result.Currencies);

        Assert.Equal(expectedBucket == "current" ? 125m : 0m, currency.Aging.Current);
        Assert.Equal(expectedBucket == "1-30" ? 125m : 0m, currency.Aging.Days1To30);
        Assert.Equal(expectedBucket == "31-60" ? 125m : 0m, currency.Aging.Days31To60);
        Assert.Equal(expectedBucket == "61-90" ? 125m : 0m, currency.Aging.Days61To90);
        Assert.Equal(expectedBucket == "91+" ? 125m : 0m, currency.Aging.Days91Plus);
    }

    [Fact]
    public async Task Projection_fails_closed_when_effects_over_collect_receivable()
    {
        var source = Source(100m, "UYU", new DateOnly(2026, 9, 20));
        var allocation = ReceivableBalanceEffect.CreateCollectionAllocation(
            Guid.NewGuid(), Organization, source.ReceivableId, 100.000001m, "collection-over", 0, AsOfUtc.AddDays(-1));
        source = source with { Effects = new[] { allocation } };
        var model = new PartyReceivableAccountReadModel(new FakeSourceReader(source));

        var error = await Assert.ThrowsAsync<DomainRuleException>(
            () => model.GetAsync(Organization, PartyId, AsOfUtc));

        Assert.Equal("receivables.account.negative_balance", error.Code);
    }

    [Fact]
    public async Task Projection_fails_closed_when_reversal_does_not_match_allocation_amount()
    {
        var source = Source(100m, "UYU", new DateOnly(2026, 9, 20));
        var allocation = ReceivableBalanceEffect.CreateCollectionAllocation(
            Guid.NewGuid(), Organization, source.ReceivableId, 40m, "collection-3", 0, AsOfUtc.AddDays(-2));
        var reversal = ReceivableBalanceEffect.CreateCollectionReversal(
            Guid.NewGuid(), Organization, source.ReceivableId, 30m, "reversal-3", 0, allocation.Id, AsOfUtc.AddDays(-1));
        source = source with { Effects = new[] { allocation, reversal } };
        var model = new PartyReceivableAccountReadModel(new FakeSourceReader(source));

        var error = await Assert.ThrowsAsync<DomainRuleException>(
            () => model.GetAsync(Organization, PartyId, AsOfUtc));

        Assert.Equal("receivables.account.reversal_amount_mismatch", error.Code);
    }

    [Fact]
    public async Task Projection_ignores_future_effects_for_as_of_snapshot()
    {
        var source = Source(100m, "UYU", new DateOnly(2026, 9, 23));
        var futureAllocation = ReceivableBalanceEffect.CreateCollectionAllocation(
            Guid.NewGuid(), Organization, source.ReceivableId, 75m, "collection-future", 0, AsOfUtc.AddMinutes(1));
        source = source with { Effects = new[] { futureAllocation } };
        var model = new PartyReceivableAccountReadModel(new FakeSourceReader(source));

        var result = await model.GetAsync(Organization, PartyId, AsOfUtc);

        Assert.Equal(100m, Assert.Single(result.Currencies).Outstanding);
    }

    [Fact]
    public async Task Projection_rejects_source_outside_requested_party_scope()
    {
        var source = Source(100m, "UYU", new DateOnly(2026, 9, 23)) with
        {
            PartyId = Guid.Parse("22222222-2222-2222-2222-222222222222")
        };
        var model = new PartyReceivableAccountReadModel(new FakeSourceReader(source));

        var error = await Assert.ThrowsAsync<DomainRuleException>(
            () => model.GetAsync(Organization, PartyId, AsOfUtc));

        Assert.Equal("receivables.account.source_scope_mismatch", error.Code);
    }

    private static ReceivableBalanceSource Source(
        decimal originalAmount,
        string currency,
        DateOnly dueDate,
        params ReceivableBalanceEffect[] effects)
    {
        var receivableId = Guid.NewGuid();
        return new ReceivableBalanceSource(
            receivableId,
            Organization,
            PartyId,
            originalAmount,
            currency,
            dueDate,
            AsOfUtc.AddMonths(-2),
            effects);
    }

    private static ReceivableBalanceSource RebindEffects(ReceivableBalanceSource source)
    {
        var rebound = source.Effects.Select(effect =>
            ReceivableBalanceEffect.Rehydrate(
                effect.Id,
                effect.OrganizationId,
                source.ReceivableId,
                effect.Kind,
                effect.Amount,
                effect.SourceId,
                effect.SourceSequence,
                effect.ReversesEffectId,
                effect.OccurredAtUtc)).ToArray();
        return source with { Effects = rebound };
    }

    private sealed class FakeSourceReader : IReceivableBalanceSourceReader
    {
        private readonly IReadOnlyList<ReceivableBalanceSource> _sources;

        public FakeSourceReader(params ReceivableBalanceSource[] sources) => _sources = sources;

        public Task<IReadOnlyList<ReceivableBalanceSource>> ListByPartyAsync(
            string organizationId,
            Guid partyId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_sources);
    }
}
