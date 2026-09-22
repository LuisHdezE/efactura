namespace WebApi.Controllers.V1.Contracts;

public sealed record SaleCancelRequest(
    long ExpectedVersion,
    string OperatorReason,
    string? OperatorContext = null);

public sealed record SaleCancellationDto(
    string SaleId,
    long Version,
    string Status,
    bool Replayed);
