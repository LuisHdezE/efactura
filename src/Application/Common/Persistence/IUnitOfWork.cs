namespace EFactura.Application.Common.Persistence;

/// <summary>
/// Persists the state staged by Application repositories participating in the current
/// authoritative write unit. The concrete implementation belongs to Infrastructure.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Flushes all staged changes that belong to the current transaction.
    /// Repositories must not call SaveChanges independently.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
