using EFactura.Application.Receivables;
using EFactura.Domain.Common;
using EFactura.Domain.Receivables;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfReceivableAccountStore : IReceivableBalanceFactStore, IReceivableAccountReadModel
{
    private const int MonetaryScale = 6;
    private readonly V1PersistenceDbContext _dbContext;

    public EfReceivableAccountStore(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public async Task AppendAsync(
        ReceivableBalanceFact fact,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fact);

        var receivable = await _dbContext.Set<V1ReceivableRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == fact.OrganizationId && x.Id == fact.ReceivableId,
                cancellationToken);

        if (receivable is null)
            throw Rule("receivables.not_found", "Receivable was not found in the requested organization.");

        var existingFacts = await _dbContext.Set<V1ReceivableBalanceFactRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == fact.OrganizationId && x.ReceivableId == fact.ReceivableId)
            .ToListAsync(cancellationToken);

        if (existingFacts.Any(x => x.Id == fact.Id))
            throw Rule("receivables.balance_fact_duplicate", "Receivable balance fact already exists.");

        if (fact.Kind == ReceivableBalanceFactKind.CollectionReversal)
            ValidateCollectionReversal(fact, existingFacts);

        EnsureNonNegativeTimeline(
            receivable.OriginalAmount,
            existingFacts.Select(Map).Append(fact));

        _dbContext.Set<V1ReceivableBalanceFactRecord>().Add(ToRecord(fact));
    }

    public async Task<IReadOnlyList<ReceivableAccountCurrencySummary>> GetPartySummaryAsync(
        string organizationId,
        Guid partyId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
            throw Rule("receivables.organization_required", "Organization is required.");
        if (partyId == Guid.Empty)
            throw Rule("receivables.party_required", "Party id is required.");

        var normalizedOrganizationId = organizationId.Trim();

        var partyExists = await _dbContext.Set<V1PartyRecord>()
            .AsNoTracking()
            .AnyAsync(
                x => x.OrganizationId == normalizedOrganizationId && x.Id == partyId,
                cancellationToken);

        if (!partyExists)
            return Array.Empty<ReceivableAccountCurrencySummary>();

        var receivables = await _dbContext.Set<V1ReceivableRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == normalizedOrganizationId && x.CustomerPartyId == partyId)
            .Select(x => new ReceivableProjectionRow(
                x.Id,
                x.OriginalAmount,
                x.CurrencyCode,
                x.DueDate))
            .ToListAsync(cancellationToken);

        if (receivables.Count == 0)
            return Array.Empty<ReceivableAccountCurrencySummary>();

        var receivableIds = receivables.Select(x => x.Id).ToArray();
        var asOfUtcDate = asOfDate.ToDateTime(TimeOnly.MinValue);

        var facts = await _dbContext.Set<V1ReceivableBalanceFactRecord>()
            .AsNoTracking()
            .Where(x =>
                x.OrganizationId == normalizedOrganizationId
                && receivableIds.Contains(x.ReceivableId)
                && x.EffectiveOn <= asOfUtcDate)
            .Select(x => new FactProjectionRow(
                x.ReceivableId,
                x.Kind,
                x.Amount))
            .ToListAsync(cancellationToken);

        var effectsByReceivable = facts
            .GroupBy(x => x.ReceivableId)
            .ToDictionary(
                x => x.Key,
                x => x.Sum(Effect));

        var buckets = new Dictionary<string, MutableCurrencyBucket>(StringComparer.Ordinal);

        foreach (var receivable in receivables)
        {
            var outstanding = Round(receivable.OriginalAmount
                + effectsByReceivable.GetValueOrDefault(receivable.Id));

            if (outstanding < 0m)
                throw Rule(
                    "receivables.authoritative_balance_invalid",
                    "Authoritative receivable facts produced a negative outstanding balance.");

            if (outstanding == 0m)
                continue;

            var currencyCode = NormalizeCurrency(receivable.CurrencyCode);
            if (!buckets.TryGetValue(currencyCode, out var bucket))
            {
                bucket = new MutableCurrencyBucket();
                buckets.Add(currencyCode, bucket);
            }

            bucket.Outstanding = Round(bucket.Outstanding + outstanding);

            var dueDate = DateOnly.FromDateTime(receivable.DueDate);
            if (dueDate >= asOfDate)
            {
                bucket.Current = Round(bucket.Current + outstanding);
                continue;
            }

            bucket.Overdue = Round(bucket.Overdue + outstanding);
            var daysOverdue = asOfDate.DayNumber - dueDate.DayNumber;

            if (daysOverdue <= 30)
                bucket.Days1To30 = Round(bucket.Days1To30 + outstanding);
            else if (daysOverdue <= 60)
                bucket.Days31To60 = Round(bucket.Days31To60 + outstanding);
            else if (daysOverdue <= 90)
                bucket.Days61To90 = Round(bucket.Days61To90 + outstanding);
            else
                bucket.Days91Plus = Round(bucket.Days91Plus + outstanding);
        }

        return buckets
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => new ReceivableAccountCurrencySummary(
                x.Key,
                x.Value.Outstanding,
                x.Value.Overdue,
                new ReceivableAgingSummary(
                    x.Value.Current,
                    x.Value.Days1To30,
                    x.Value.Days31To60,
                    x.Value.Days61To90,
                    x.Value.Days91Plus)))
            .ToArray();
    }

    private static void ValidateCollectionReversal(
        ReceivableBalanceFact reversal,
        IReadOnlyCollection<V1ReceivableBalanceFactRecord> existingFacts)
    {
        var target = existingFacts.SingleOrDefault(x => x.Id == reversal.ReversalOfFactId);
        if (target is null
            || target.Kind != (int)ReceivableBalanceFactKind.CollectionAllocation)
            throw Rule(
                "receivables.collection_reversal_target_invalid",
                "Collection reversal must reference an existing collection allocation for the same receivable.");

        if (target.Amount != reversal.Amount)
            throw Rule(
                "receivables.collection_reversal_amount_mismatch",
                "Collection reversal amount must equal the referenced allocation amount.");

        var targetEffectiveOn = DateOnly.FromDateTime(target.EffectiveOn);
        if (reversal.EffectiveOn < targetEffectiveOn)
            throw Rule(
                "receivables.collection_reversal_date_invalid",
                "Collection reversal cannot precede the allocation it reverses.");

        if (existingFacts.Any(x => x.ReversalOfFactId == target.Id))
            throw Rule(
                "receivables.collection_reversal_duplicate",
                "Collection allocation has already been reversed.");
    }

    private static void EnsureNonNegativeTimeline(
        decimal originalAmount,
        IEnumerable<ReceivableBalanceFact> facts)
    {
        var balance = Round(originalAmount);
        if (balance <= 0m)
            throw Rule(
                "receivables.original_amount_invalid",
                "Receivable original amount must be greater than zero.");

        foreach (var day in facts
                     .GroupBy(x => x.EffectiveOn)
                     .OrderBy(x => x.Key))
        {
            balance = Round(balance + day.Sum(x => x.SignedEffect));
            if (balance < 0m)
                throw Rule(
                    "receivables.balance_negative",
                    "Receivable balance facts cannot reduce outstanding balance below zero.");
        }
    }

    private static ReceivableBalanceFact Map(V1ReceivableBalanceFactRecord record) =>
        ReceivableBalanceFact.Rehydrate(
            record.Id,
            record.OrganizationId,
            record.ReceivableId,
            (ReceivableBalanceFactKind)record.Kind,
            record.Amount,
            DateOnly.FromDateTime(record.EffectiveOn),
            record.ReversalOfFactId,
            record.RecordedAtUtc);

    private static V1ReceivableBalanceFactRecord ToRecord(ReceivableBalanceFact fact) => new()
    {
        Id = fact.Id,
        OrganizationId = fact.OrganizationId,
        ReceivableId = fact.ReceivableId,
        Kind = (int)fact.Kind,
        Amount = Round(fact.Amount),
        EffectiveOn = fact.EffectiveOn.ToDateTime(TimeOnly.MinValue),
        ReversalOfFactId = fact.ReversalOfFactId,
        RecordedAtUtc = fact.RecordedAtUtc
    };

    private static decimal Effect(FactProjectionRow fact) =>
        ((ReceivableBalanceFactKind)fact.Kind) switch
        {
            ReceivableBalanceFactKind.AdjustmentIncrease => fact.Amount,
            ReceivableBalanceFactKind.AdjustmentDecrease => -fact.Amount,
            ReceivableBalanceFactKind.CollectionAllocation => -fact.Amount,
            ReceivableBalanceFactKind.CollectionReversal => fact.Amount,
            _ => throw Rule(
                "receivables.balance_fact_kind_invalid",
                "Stored receivable balance fact kind is invalid.")
        };

    private static decimal Round(decimal value) =>
        decimal.Round(value, MonetaryScale, MidpointRounding.ToEven);

    private static string NormalizeCurrency(string currencyCode)
    {
        var normalized = currencyCode.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw Rule("receivables.currency_invalid", "Receivable currency must use ISO alpha-3 form.");
        return normalized;
    }

    private static DomainRuleException Rule(string code, string message) => new(code, message);

    private sealed record ReceivableProjectionRow(
        Guid Id,
        decimal OriginalAmount,
        string CurrencyCode,
        DateTime DueDate);

    private sealed record FactProjectionRow(
        Guid ReceivableId,
        int Kind,
        decimal Amount);

    private sealed class MutableCurrencyBucket
    {
        public decimal Outstanding { get; set; }
        public decimal Overdue { get; set; }
        public decimal Current { get; set; }
        public decimal Days1To30 { get; set; }
        public decimal Days31To60 { get; set; }
        public decimal Days61To90 { get; set; }
        public decimal Days91Plus { get; set; }
    }
}
