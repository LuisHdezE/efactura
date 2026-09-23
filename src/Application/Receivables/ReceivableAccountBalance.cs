using EFactura.Domain.Common;
using EFactura.Domain.Receivables;

namespace EFactura.Application.Receivables;

public sealed record ReceivableBalanceSource(
    Guid ReceivableId,
    string OrganizationId,
    Guid PartyId,
    decimal OriginalAmount,
    string CurrencyCode,
    DateOnly DueDate,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<ReceivableBalanceEffect> Effects);

public sealed record ReceivableAgingBuckets(
    decimal Current,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Days91Plus);

public sealed record ReceivableCurrencyAccountSummary(
    string CurrencyCode,
    decimal Outstanding,
    decimal Overdue,
    ReceivableAgingBuckets Aging);

public sealed record PartyReceivableAccountSummary(
    string OrganizationId,
    Guid PartyId,
    DateTimeOffset AsOfUtc,
    IReadOnlyList<ReceivableCurrencyAccountSummary> Currencies);

public interface IReceivableBalanceSourceReader
{
    Task<IReadOnlyList<ReceivableBalanceSource>> ListByPartyAsync(
        string organizationId,
        Guid partyId,
        CancellationToken cancellationToken = default);
}

public interface IReceivableBalanceEffectRepository
{
    Task AddAsync(
        ReceivableBalanceEffect effect,
        CancellationToken cancellationToken = default);

    Task<ReceivableBalanceEffect?> GetAsync(
        string organizationId,
        Guid effectId,
        CancellationToken cancellationToken = default);
}

public interface IPartyReceivableAccountReadModel
{
    Task<PartyReceivableAccountSummary> GetAsync(
        string organizationId,
        Guid partyId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);
}

public sealed class PartyReceivableAccountReadModel : IPartyReceivableAccountReadModel
{
    private readonly IReceivableBalanceSourceReader _sourceReader;

    public PartyReceivableAccountReadModel(IReceivableBalanceSourceReader sourceReader) =>
        _sourceReader = sourceReader;

    public async Task<PartyReceivableAccountSummary> GetAsync(
        string organizationId,
        Guid partyId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var normalizedOrganizationId = RequiredOrganization(organizationId);
        if (partyId == Guid.Empty)
            throw Rule("receivables.account.party_required", "Party id is required for receivable account projection.");

        var sources = await _sourceReader.ListByPartyAsync(
            normalizedOrganizationId,
            partyId,
            cancellationToken);

        var buckets = new SortedDictionary<string, MutableCurrencyBucket>(StringComparer.Ordinal);
        var asOfDate = DateOnly.FromDateTime(asOfUtc.UtcDateTime);

        foreach (var source in sources
                     .OrderBy(x => x.CurrencyCode, StringComparer.Ordinal)
                     .ThenBy(x => x.DueDate)
                     .ThenBy(x => x.ReceivableId))
        {
            ValidateSource(source, normalizedOrganizationId, partyId);
            if (source.CreatedAtUtc > asOfUtc)
                continue;

            var currency = NormalizeCurrency(source.CurrencyCode);
            if (!buckets.TryGetValue(currency, out var bucket))
            {
                bucket = new MutableCurrencyBucket();
                buckets.Add(currency, bucket);
            }

            var outstanding = ComputeOutstanding(source, asOfUtc);
            if (outstanding == 0m)
                continue;

            bucket.Outstanding += outstanding;
            var overdueDays = asOfDate.DayNumber - source.DueDate.DayNumber;
            if (overdueDays > 0)
                bucket.Overdue += outstanding;

            if (overdueDays <= 0)
                bucket.Current += outstanding;
            else if (overdueDays <= 30)
                bucket.Days1To30 += outstanding;
            else if (overdueDays <= 60)
                bucket.Days31To60 += outstanding;
            else if (overdueDays <= 90)
                bucket.Days61To90 += outstanding;
            else
                bucket.Days91Plus += outstanding;
        }

        var currencies = buckets
            .Select(x => new ReceivableCurrencyAccountSummary(
                x.Key,
                Money(x.Value.Outstanding),
                Money(x.Value.Overdue),
                new ReceivableAgingBuckets(
                    Money(x.Value.Current),
                    Money(x.Value.Days1To30),
                    Money(x.Value.Days31To60),
                    Money(x.Value.Days61To90),
                    Money(x.Value.Days91Plus))))
            .ToArray();

        return new PartyReceivableAccountSummary(
            normalizedOrganizationId,
            partyId,
            asOfUtc,
            currencies);
    }

    private static decimal ComputeOutstanding(ReceivableBalanceSource source, DateTimeOffset asOfUtc)
    {
        if (source.OriginalAmount <= 0m)
            throw Rule("receivables.account.original_amount_invalid", "Receivable source original amount must be positive.");

        var outstanding = Money(source.OriginalAmount);
        var effects = source.Effects
            .Where(x => x.OccurredAtUtc <= asOfUtc)
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.Id)
            .ToArray();

        ValidateEffects(source, effects);

        foreach (var effect in effects)
        {
            outstanding = Money(outstanding + effect.SignedDelta);
            if (outstanding < 0m)
            {
                throw Rule(
                    "receivables.account.negative_balance",
                    "Receivable balance effects cannot reduce outstanding amount below zero.");
            }
        }

        return outstanding;
    }

    private static void ValidateEffects(
        ReceivableBalanceSource source,
        IReadOnlyList<ReceivableBalanceEffect> effects)
    {
        if (effects.Select(x => x.Id).Distinct().Count() != effects.Count)
            throw Rule("receivables.account.duplicate_effect", "Receivable balance source contains duplicate effect ids.");

        if (effects
            .Select(x => (x.Kind, x.SourceId, x.SourceSequence))
            .Distinct()
            .Count() != effects.Count)
        {
            throw Rule(
                "receivables.account.duplicate_source_effect",
                "Receivable balance source contains duplicate lifecycle source effects.");
        }

        var byId = effects.ToDictionary(x => x.Id);
        foreach (var effect in effects)
        {
            if (!string.Equals(effect.OrganizationId, source.OrganizationId, StringComparison.Ordinal)
                || effect.ReceivableId != source.ReceivableId)
            {
                throw Rule(
                    "receivables.account.effect_scope_mismatch",
                    "Receivable balance effect is outside the source organization or receivable scope.");
            }

            if (effect.Kind != ReceivableBalanceEffectKind.CollectionReversal)
                continue;

            if (!effect.ReversesEffectId.HasValue
                || !byId.TryGetValue(effect.ReversesEffectId.Value, out var allocation)
                || allocation.Kind != ReceivableBalanceEffectKind.CollectionAllocation)
            {
                throw Rule(
                    "receivables.account.reversal_target_invalid",
                    "Collection reversal must reference an existing collection allocation effect.");
            }

            if (allocation.OccurredAtUtc > effect.OccurredAtUtc)
            {
                throw Rule(
                    "receivables.account.reversal_order_invalid",
                    "Collection reversal cannot precede the allocation it reverses.");
            }

            if (Money(allocation.Amount) != Money(effect.Amount))
            {
                throw Rule(
                    "receivables.account.reversal_amount_mismatch",
                    "Collection reversal amount must exactly match the allocation being reversed.");
            }
        }
    }

    private static void ValidateSource(
        ReceivableBalanceSource source,
        string organizationId,
        Guid partyId)
    {
        if (!string.Equals(source.OrganizationId, organizationId, StringComparison.Ordinal)
            || source.PartyId != partyId)
        {
            throw Rule(
                "receivables.account.source_scope_mismatch",
                "Receivable balance source is outside the requested organization or Party scope.");
        }

        if (source.ReceivableId == Guid.Empty)
            throw Rule("receivables.account.receivable_required", "Receivable source id is required.");
    }

    private static string RequiredOrganization(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule("receivables.account.organization_required", "Organization id is required.");
        return value.Trim();
    }

    private static string NormalizeCurrency(string value)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw Rule("receivables.account.currency_invalid", "Receivable currency must use ISO alpha-3 form.");
        return normalized;
    }

    private static decimal Money(decimal value) =>
        decimal.Round(value, 6, MidpointRounding.ToEven);

    private static DomainRuleException Rule(string code, string message) => new(code, message);

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
