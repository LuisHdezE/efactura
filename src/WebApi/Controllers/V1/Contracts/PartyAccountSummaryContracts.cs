namespace WebApi.Controllers.V1.Contracts;

public sealed record PartyAccountAgingDto(
    decimal Current,
    decimal Days1To30,
    decimal Days31To60,
    decimal Days61To90,
    decimal Days91Plus);

public sealed record PartyAccountCurrencyDto(
    string CurrencyCode,
    decimal Outstanding,
    decimal Overdue,
    PartyAccountAgingDto Aging);

public sealed record PartyAccountSummaryDto(
    string OrganizationId,
    Guid PartyId,
    DateTimeOffset AsOfUtc,
    bool ReceivablesApplicable,
    IReadOnlyList<PartyAccountCurrencyDto> Receivables,
    bool PayablesApplicable,
    IReadOnlyList<PartyAccountCurrencyDto> Payables);
