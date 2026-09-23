using EFactura.Domain.Receivables;

namespace EFactura.Application.Receivables;

public interface IReceivableBalanceFactStore
{
    Task AppendAsync(
        ReceivableBalanceFact fact,
        CancellationToken cancellationToken = default);
}

public interface IReceivableAccountReadModel
{
    Task<IReadOnlyList<ReceivableAccountCurrencySummary>> GetPartySummaryAsync(
        string organizationId,
        Guid partyId,
        DateOnly asOfDate,
        CancellationToken cancellationToken = default);
}

public sealed record ReceivableAgingSummary(
    decimal Current,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Days91Plus);

public sealed record ReceivableAccountCurrencySummary(
    string CurrencyCode,
    decimal Outstanding,
    decimal Overdue,
    ReceivableAgingSummary Aging);
