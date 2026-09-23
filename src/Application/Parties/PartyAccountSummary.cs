using EFactura.Application.Common.Errors;
using EFactura.Application.Payables;
using EFactura.Application.Receivables;
using EFactura.Domain.Parties;

namespace EFactura.Application.Parties;

public sealed record PartyAccountSummary(
    string OrganizationId,
    Guid PartyId,
    DateTimeOffset AsOfUtc,
    bool ReceivablesApplicable,
    IReadOnlyList<ReceivableCurrencyAccountSummary> Receivables,
    bool PayablesApplicable,
    IReadOnlyList<PayableCurrencyAccountSummary> Payables);

public interface IPartyAccountSummaryReadModel
{
    Task<PartyAccountSummary> GetAsync(
        string organizationId,
        Guid partyId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default);
}

public sealed class PartyAccountSummaryReadModel : IPartyAccountSummaryReadModel
{
    private readonly IPartyRepository _parties;
    private readonly IPartyReceivableAccountReadModel _receivables;
    private readonly IPartyPayableAccountReadModel _payables;

    public PartyAccountSummaryReadModel(
        IPartyRepository parties,
        IPartyReceivableAccountReadModel receivables,
        IPartyPayableAccountReadModel payables)
    {
        _parties = parties;
        _receivables = receivables;
        _payables = payables;
    }

    public async Task<PartyAccountSummary> GetAsync(
        string organizationId,
        Guid partyId,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken = default)
    {
        var normalizedOrganizationId = RequiredOrganization(organizationId);
        if (partyId == Guid.Empty)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                "party.account_summary.party_required",
                "Party id is required for account-summary projection.");
        }

        var normalizedAsOfUtc = asOfUtc.ToUniversalTime();
        var party = await _parties.GetAsync(normalizedOrganizationId, partyId, cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound,
                "party.not_found",
                "The requested party was not found.");

        if (!string.Equals(party.OrganizationId, normalizedOrganizationId, StringComparison.Ordinal)
            || party.Id != partyId)
        {
            throw Invariant("party.account_summary.party_scope_mismatch");
        }

        if (party.Roles.Any(role => !Enum.IsDefined(typeof(PartyRole), role)))
            throw Invariant("party.account_summary.party_role_invalid");

        var receivablesApplicable = party.Roles.Contains(PartyRole.Customer);
        var payablesApplicable = party.Roles.Contains(PartyRole.Supplier);

        IReadOnlyList<ReceivableCurrencyAccountSummary> receivableCurrencies =
            Array.Empty<ReceivableCurrencyAccountSummary>();
        IReadOnlyList<PayableCurrencyAccountSummary> payableCurrencies =
            Array.Empty<PayableCurrencyAccountSummary>();

        if (receivablesApplicable)
        {
            var receivableSummary = await _receivables.GetAsync(
                normalizedOrganizationId,
                partyId,
                normalizedAsOfUtc,
                cancellationToken);

            EnsureProjectionScope(
                "receivable",
                receivableSummary.OrganizationId,
                receivableSummary.PartyId,
                receivableSummary.AsOfUtc,
                normalizedOrganizationId,
                partyId,
                normalizedAsOfUtc);

            receivableCurrencies = NormalizeReceivables(receivableSummary.Currencies);
        }

        if (payablesApplicable)
        {
            var payableSummary = await _payables.GetAsync(
                normalizedOrganizationId,
                partyId,
                normalizedAsOfUtc,
                cancellationToken);

            EnsureProjectionScope(
                "payable",
                payableSummary.OrganizationId,
                payableSummary.PartyId,
                payableSummary.AsOfUtc,
                normalizedOrganizationId,
                partyId,
                normalizedAsOfUtc);

            payableCurrencies = NormalizePayables(payableSummary.Currencies);
        }

        return new PartyAccountSummary(
            normalizedOrganizationId,
            partyId,
            normalizedAsOfUtc,
            receivablesApplicable,
            receivableCurrencies,
            payablesApplicable,
            payableCurrencies);
    }

    private static IReadOnlyList<ReceivableCurrencyAccountSummary> NormalizeReceivables(
        IReadOnlyList<ReceivableCurrencyAccountSummary> currencies)
    {
        ArgumentNullException.ThrowIfNull(currencies);
        EnsureUniqueCurrencies(
            currencies.Select(x => x.CurrencyCode),
            "party.account_summary.receivable_currency_duplicate");

        foreach (var currency in currencies)
        {
            EnsureAmounts(
                currency.CurrencyCode,
                currency.Outstanding,
                currency.Overdue,
                currency.Aging.Current,
                currency.Aging.Days1To30,
                currency.Aging.Days31To60,
                currency.Aging.Days61To90,
                currency.Aging.Days91Plus,
                "receivable");
        }

        return currencies
            .OrderBy(x => x.CurrencyCode, StringComparer.Ordinal)
            .ToArray();
    }

    private static IReadOnlyList<PayableCurrencyAccountSummary> NormalizePayables(
        IReadOnlyList<PayableCurrencyAccountSummary> currencies)
    {
        ArgumentNullException.ThrowIfNull(currencies);
        EnsureUniqueCurrencies(
            currencies.Select(x => x.CurrencyCode),
            "party.account_summary.payable_currency_duplicate");

        foreach (var currency in currencies)
        {
            EnsureAmounts(
                currency.CurrencyCode,
                currency.Outstanding,
                currency.Overdue,
                currency.Aging.Current,
                currency.Aging.Days1To30,
                currency.Aging.Days31To60,
                currency.Aging.Days61To90,
                currency.Aging.Days91Plus,
                "payable");
        }

        return currencies
            .OrderBy(x => x.CurrencyCode, StringComparer.Ordinal)
            .ToArray();
    }

    private static void EnsureProjectionScope(
        string side,
        string projectionOrganizationId,
        Guid projectionPartyId,
        DateTimeOffset projectionAsOfUtc,
        string organizationId,
        Guid partyId,
        DateTimeOffset asOfUtc)
    {
        if (!string.Equals(projectionOrganizationId, organizationId, StringComparison.Ordinal)
            || projectionPartyId != partyId
            || projectionAsOfUtc.ToUniversalTime() != asOfUtc)
        {
            throw Invariant($"party.account_summary.{side}_scope_mismatch");
        }
    }

    private static void EnsureUniqueCurrencies(IEnumerable<string> currencies, string code)
    {
        var normalized = currencies.Select(NormalizeCurrency).ToArray();
        if (normalized.Distinct(StringComparer.Ordinal).Count() != normalized.Length)
            throw Invariant(code);
    }

    private static void EnsureAmounts(
        string currencyCode,
        decimal outstanding,
        decimal overdue,
        decimal current,
        decimal days1To30,
        decimal days31To60,
        decimal days61To90,
        decimal days91Plus,
        string side)
    {
        _ = NormalizeCurrency(currencyCode);
        if (outstanding < 0m
            || overdue < 0m
            || overdue > outstanding
            || current < 0m
            || days1To30 < 0m
            || days31To60 < 0m
            || days61To90 < 0m
            || days91Plus < 0m)
        {
            throw Invariant($"party.account_summary.{side}_amount_invalid");
        }
    }

    private static string RequiredOrganization(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                "party.account_summary.organization_required",
                "Organization id is required for account-summary projection.");
        }

        return value.Trim();
    }

    private static string NormalizeCurrency(string value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        var normalized = trimmed.ToUpperInvariant();
        if (normalized.Length != 3
            || normalized.Any(ch => ch is < 'A' or > 'Z')
            || !string.Equals(trimmed, normalized, StringComparison.Ordinal))
        {
            throw Invariant("party.account_summary.currency_invalid");
        }

        return normalized;
    }

    private static InvalidOperationException Invariant(string code) =>
        new($"Authoritative Party account-summary invariant failed: {code}.");
}
