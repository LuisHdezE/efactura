using EFactura.Domain.Common;
using EFactura.Domain.Payables;

namespace EFactura.Application.Payables;

public sealed record PayableBalanceSource(
    Guid PayableId,
    string OrganizationId,
    Guid PartyId,
    decimal OriginalAmount,
    string CurrencyCode,
    DateOnly DueDate,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyList<PayableBalanceEffect> Effects);

public sealed record PayableAgingBuckets(
    decimal Current,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Days91Plus);

public sealed record PayableCurrencyAccountSummary(
    string CurrencyCode,
    decimal Outstanding,
    decimal Overdue,
    PayableAgingBuckets Aging);

public sealed record PartyPayableAccountSummary(
    string OrganizationId,
    Guid PartyId,
    DateTimeOffset AsOfUtc,
    IReadOnlyList<PayableCurrencyAccountSummary> Currencies);

public interface IPayableBalanceSourceReader
{
    Task<IReadOnlyList<PayableBalanceSource>> ListByPartyAsync(
        string organizationId,
        Guid partyId,
        CancellationToken cancellationToken = default);
}

public interface IPayableBalanceEffectRepository
{
    Task AddAsync(
        PayableBalanceEffect effect,
        CancellationToken cancellationToken = default);

    Task<PayableBalanceEffect?> GetAsync(
        string organizationId,
        Guid effectId,
        CancellationToken cancellationToken = default);
}

public interface IPartyPayableAccountReadModel
{
    Task<PartyPayableAccountSummary> GetAsync(
        string organizationId,
        Guid partyId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);
}

public sealed class PartyPayableAccountReadModel : IPartyPayableAccountReadModel
{
    private readonly IPayableBalanceSourceReader _sourceReader;

    public PartyPayableAccountReadModel(IPayableBalanceSourceReader sourceReader) =>
        _sourceReader = sourceReader;

    public async Task<PartyPayableAccountSummary> GetAsync(
        string organizationId,
        Guid partyId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var normalizedOrganizationId = RequiredOrganization(organizationId);
        if (partyId == Guid.Empty)
            throw Rule("payables.account.party_required", "Party id is required for payable account projection.");

        var sources = await _sourceReader.ListByPartyAsync(
            normalizedOrganizationId,
            partyId,
            cancellationToken);

        var buckets = new SortedDictionary<string, MutableCurrencyBucket>(StringComparer.Ordinal);
        var asOfDate = DateOnly.FromDateTime(asOfUtc.UtcDateTime);

        foreach (var source in sources
                     .OrderBy(x => x.CurrencyCode, StringComparer.Ordinal)
                     .ThenBy(x => x.DueDate)
                     .ThenBy(x => x.PayableId))
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
            .Select(x => new PayableCurrencyAccountSummary(
                x.Key,
                Money(x.Value.Outstanding),
                Money(x.Value.Overdue),
                new PayableAgingBuckets(
                    Money(x.Value.Current),
                    Money(x.Value.Days1To30),
                    Money(x.Value.Days31To60),
                    Money(x.Value.Days61To90),
                    Money(x.Value.Days91Plus))))
            .ToArray();

        return new PartyPayableAccountSummary(
            normalizedOrganizationId,
            partyId,
            asOfUtc,
            currencies);
    }

    private static decimal ComputeOutstanding(PayableBalanceSource source, DateTimeOffset asOfUtc)
    {
        if (source.OriginalAmount <= 0m)
            throw Rule("payables.account.original_amount_invalid", "Payable source original amount must be positive.");

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
                    "payables.account.negative_balance",
                    "Payable balance effects cannot reduce outstanding amount below zero.");
            }
        }

        return outstanding;
    }

    private static void ValidateEffects(
        PayableBalanceSource source,
        IReadOnlyList<PayableBalanceEffect> effects)
    {
        if (effects.Select(x => x.Id).Distinct().Count() != effects.Count)
            throw Rule("payables.account.duplicate_effect", "Payable balance source contains duplicate effect ids.");

        if (effects
            .Select(x => (x.Kind, x.SourceId, x.SourceSequence))
            .Distinct()
            .Count() != effects.Count)
        {
            throw Rule(
                "payables.account.duplicate_source_effect",
                "Payable balance source contains duplicate lifecycle source effects.");
        }

        var byId = effects.ToDictionary(x => x.Id);
        foreach (var effect in effects)
        {
            if (!string.Equals(effect.OrganizationId, source.OrganizationId, StringComparison.Ordinal)
                || effect.PayableId != source.PayableId)
            {
                throw Rule(
                    "payables.account.effect_scope_mismatch",
                    "Payable balance effect is outside the source organization or payable scope.");
            }

            if (effect.Kind != PayableBalanceEffectKind.SupplierPaymentReversal)
                continue;

            if (!effect.ReversesEffectId.HasValue
                || !byId.TryGetValue(effect.ReversesEffectId.Value, out var allocation)
                || allocation.Kind != PayableBalanceEffectKind.SupplierPaymentAllocation)
            {
                throw Rule(
                    "payables.account.reversal_target_invalid",
                    "Supplier payment reversal must reference an existing supplier payment allocation effect.");
            }

            if (allocation.OccurredAtUtc > effect.OccurredAtUtc)
            {
                throw Rule(
                    "payables.account.reversal_order_invalid",
                    "Supplier payment reversal cannot precede the allocation it reverses.");
            }

            if (Money(allocation.Amount) != Money(effect.Amount))
            {
                throw Rule(
                    "payables.account.reversal_amount_mismatch",
                    "Supplier payment reversal amount must exactly match the allocation being reversed.");
            }
        }
    }

    private static void ValidateSource(
        PayableBalanceSource source,
        string organizationId,
        Guid partyId)
    {
        if (!string.Equals(source.OrganizationId, organizationId, StringComparison.Ordinal)
            || source.PartyId != partyId)
        {
            throw Rule(
                "payables.account.source_scope_mismatch",
                "Payable balance source is outside the requested organization or Party scope.");
        }

        if (source.PayableId == Guid.Empty)
            throw Rule("payables.account.payable_required", "Payable source id is required.");
    }

    private static string RequiredOrganization(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule("payables.account.organization_required", "Organization id is required.");
        return value.Trim();
    }

    private static string NormalizeCurrency(string value)
    {
        var normalized = value?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length != 3 || normalized.Any(ch => ch is < 'A' or > 'Z'))
            throw Rule("payables.account.currency_invalid", "Payable currency must use ISO alpha-3 form.");
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
