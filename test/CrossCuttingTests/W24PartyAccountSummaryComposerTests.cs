using EFactura.Application.Common.Errors;
using EFactura.Application.Parties;
using EFactura.Application.Payables;
using EFactura.Application.Receivables;
using EFactura.Domain.Parties;
using Xunit;

namespace CrossCuttingTests;

public sealed class W24PartyAccountSummaryComposerTests
{
    private const string OrganizationId = "company-1";
    private static readonly Guid PartyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset AsOf = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Customer_only_calls_only_receivable_projection_and_marks_payables_not_applicable()
    {
        var parties = new StubPartyRepository(PartyWith(PartyRole.Customer));
        var receivables = new StubReceivables(ReceivableSummary("UYU", 100m, 25m));
        var payables = new StubPayables(PayableSummary("UYU", 999m, 999m));
        var sut = new PartyAccountSummaryReadModel(parties, receivables, payables);

        var result = await sut.GetAsync(OrganizationId, PartyId, AsOf);

        Assert.True(result.ReceivablesApplicable);
        Assert.False(result.PayablesApplicable);
        Assert.Single(result.Receivables);
        Assert.Empty(result.Payables);
        Assert.Equal(1, receivables.Calls);
        Assert.Equal(0, payables.Calls);
    }

    [Fact]
    public async Task Supplier_only_calls_only_payable_projection_and_marks_receivables_not_applicable()
    {
        var parties = new StubPartyRepository(PartyWith(PartyRole.Supplier));
        var receivables = new StubReceivables(ReceivableSummary("UYU", 999m, 999m));
        var payables = new StubPayables(PayableSummary("USD", 80m, 20m));
        var sut = new PartyAccountSummaryReadModel(parties, receivables, payables);

        var result = await sut.GetAsync(OrganizationId, PartyId, AsOf);

        Assert.False(result.ReceivablesApplicable);
        Assert.True(result.PayablesApplicable);
        Assert.Empty(result.Receivables);
        Assert.Single(result.Payables);
        Assert.Equal(0, receivables.Calls);
        Assert.Equal(1, payables.Calls);
    }

    [Fact]
    public async Task Dual_role_composes_both_authoritative_sides_and_sorts_currency_buckets()
    {
        var parties = new StubPartyRepository(PartyWith(PartyRole.Customer, PartyRole.Supplier));
        var receivables = new StubReceivables(new PartyReceivableAccountSummary(
            OrganizationId,
            PartyId,
            AsOf,
            new[]
            {
                CurrencyReceivable("UYU", 100m, 25m),
                CurrencyReceivable("USD", 50m, 0m)
            }));
        var payables = new StubPayables(new PartyPayableAccountSummary(
            OrganizationId,
            PartyId,
            AsOf,
            new[]
            {
                CurrencyPayable("UYU", 70m, 10m),
                CurrencyPayable("EUR", 20m, 0m)
            }));
        var sut = new PartyAccountSummaryReadModel(parties, receivables, payables);

        var result = await sut.GetAsync(OrganizationId, PartyId, AsOf.ToOffset(TimeSpan.FromHours(-3)));

        Assert.True(result.ReceivablesApplicable);
        Assert.True(result.PayablesApplicable);
        Assert.Equal(TimeSpan.Zero, result.AsOfUtc.Offset);
        Assert.Equal(new[] { "USD", "UYU" }, result.Receivables.Select(x => x.CurrencyCode));
        Assert.Equal(new[] { "EUR", "UYU" }, result.Payables.Select(x => x.CurrencyCode));
        Assert.Equal(1, receivables.Calls);
        Assert.Equal(1, payables.Calls);
    }

    [Fact]
    public async Task Missing_or_cross_organization_party_fails_closed_before_financial_projection()
    {
        var parties = new StubPartyRepository(null);
        var receivables = new StubReceivables(ReceivableSummary("UYU", 100m, 0m));
        var payables = new StubPayables(PayableSummary("UYU", 100m, 0m));
        var sut = new PartyAccountSummaryReadModel(parties, receivables, payables);

        var error = await Assert.ThrowsAsync<ApplicationProblemException>(
            () => sut.GetAsync("company-2", PartyId, AsOf));

        Assert.Equal(ApplicationProblemKind.NotFound, error.Kind);
        Assert.Equal("party.not_found", error.Code);
        Assert.Equal(0, receivables.Calls);
        Assert.Equal(0, payables.Calls);
    }

    [Fact]
    public async Task Unknown_party_role_fails_closed_before_financial_projection()
    {
        var parties = new StubPartyRepository(PartyWith((PartyRole)999));
        var receivables = new StubReceivables(ReceivableSummary("UYU", 100m, 0m));
        var payables = new StubPayables(PayableSummary("UYU", 100m, 0m));
        var sut = new PartyAccountSummaryReadModel(parties, receivables, payables);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.GetAsync(OrganizationId, PartyId, AsOf));

        Assert.Equal(0, receivables.Calls);
        Assert.Equal(0, payables.Calls);
    }

    [Fact]
    public async Task Downstream_scope_mismatch_fails_closed()
    {
        var parties = new StubPartyRepository(PartyWith(PartyRole.Customer));
        var receivables = new StubReceivables(new PartyReceivableAccountSummary(
            "company-2",
            PartyId,
            AsOf,
            new[] { CurrencyReceivable("UYU", 100m, 0m) }));
        var payables = new StubPayables(PayableSummary("UYU", 0m, 0m));
        var sut = new PartyAccountSummaryReadModel(parties, receivables, payables);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetAsync(OrganizationId, PartyId, AsOf));
    }

    [Fact]
    public async Task Duplicate_currency_or_invalid_amount_fails_closed()
    {
        var parties = new StubPartyRepository(PartyWith(PartyRole.Supplier));
        var receivables = new StubReceivables(ReceivableSummary("UYU", 0m, 0m));
        var payables = new StubPayables(new PartyPayableAccountSummary(
            OrganizationId,
            PartyId,
            AsOf,
            new[]
            {
                CurrencyPayable("USD", 20m, 0m),
                CurrencyPayable("USD", 10m, 0m)
            }));
        var sut = new PartyAccountSummaryReadModel(parties, receivables, payables);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetAsync(OrganizationId, PartyId, AsOf));
    }

    [Fact]
    public async Task Non_canonical_currency_fails_closed()
    {
        var parties = new StubPartyRepository(PartyWith(PartyRole.Customer));
        var receivables = new StubReceivables(ReceivableSummary("uyu", 10m, 0m));
        var payables = new StubPayables(PayableSummary("UYU", 0m, 0m));
        var sut = new PartyAccountSummaryReadModel(parties, receivables, payables);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.GetAsync(OrganizationId, PartyId, AsOf));
    }

    private static Party PartyWith(params PartyRole[] roles) =>
        Party.Create(
            PartyId,
            OrganizationId,
            PartyKind.Organization,
            "Party account summary test",
            "UY",
            "UY",
            roles);

    private static PartyReceivableAccountSummary ReceivableSummary(string currency, decimal outstanding, decimal overdue) =>
        new(OrganizationId, PartyId, AsOf, new[] { CurrencyReceivable(currency, outstanding, overdue) });

    private static PartyPayableAccountSummary PayableSummary(string currency, decimal outstanding, decimal overdue) =>
        new(OrganizationId, PartyId, AsOf, new[] { CurrencyPayable(currency, outstanding, overdue) });

    private static ReceivableCurrencyAccountSummary CurrencyReceivable(
        string currency,
        decimal outstanding,
        decimal overdue) =>
        new(
            currency,
            outstanding,
            overdue,
            new ReceivableAgingBuckets(
                outstanding - overdue,
                overdue,
                0m,
                0m,
                0m));

    private static PayableCurrencyAccountSummary CurrencyPayable(
        string currency,
        decimal outstanding,
        decimal overdue) =>
        new(
            currency,
            outstanding,
            overdue,
            new PayableAgingBuckets(
                outstanding - overdue,
                overdue,
                0m,
                0m,
                0m));

    private sealed class StubPartyRepository : IPartyRepository
    {
        private readonly Party? _party;

        public StubPartyRepository(Party? party) => _party = party;

        public Task AddAsync(Party party, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<Party?> GetAsync(
            string organizationId,
            Guid partyId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _party is not null
                && string.Equals(_party.OrganizationId, organizationId, StringComparison.Ordinal)
                && _party.Id == partyId
                    ? _party
                    : null);

        public Task<bool> FiscalIdentityExistsAsync(
            string organizationId,
            string typeCode,
            string number,
            string issuingCountry,
            Guid? excludingPartyId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);
    }

    private sealed class StubReceivables : IPartyReceivableAccountReadModel
    {
        private readonly PartyReceivableAccountSummary _summary;

        public StubReceivables(PartyReceivableAccountSummary summary) => _summary = summary;

        public int Calls { get; private set; }

        public Task<PartyReceivableAccountSummary> GetAsync(
            string organizationId,
            Guid partyId,
            DateTimeOffset asOfUtc,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(_summary);
        }
    }

    private sealed class StubPayables : IPartyPayableAccountReadModel
    {
        private readonly PartyPayableAccountSummary _summary;

        public StubPayables(PartyPayableAccountSummary summary) => _summary = summary;

        public int Calls { get; private set; }

        public Task<PartyPayableAccountSummary> GetAsync(
            string organizationId,
            Guid partyId,
            DateTimeOffset asOfUtc,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult(_summary);
        }
    }
}
