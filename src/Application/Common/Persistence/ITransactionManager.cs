namespace EFactura.Application.Common.Persistence;

/// <summary>
/// Executes one authoritative Application write workflow inside a single local database
/// transaction. The concrete provider-specific implementation belongs to Infrastructure.
/// </summary>
public interface ITransactionManager
{
    /// <summary>
    /// Executes the callback inside a transaction. Implementations must commit only after the
    /// callback completes successfully and must roll back on exception or cancellation.
    /// </summary>
    Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the callback inside a transaction and returns its result. Implementations must
    /// commit only after the callback completes successfully and must roll back on exception or cancellation.
    /// </summary>
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
