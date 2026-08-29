namespace EFactura.Application.Common.Results;

public sealed record PageResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long Total);
