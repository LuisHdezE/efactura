using System.Data;
using EFactura.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Transactions;

public sealed class EfTransactionManager : ITransactionManager
{
    private readonly Write.V1PersistenceDbContext _dbContext;

    public EfTransactionManager(Write.V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreAsync(async ct =>
        {
            await operation(ct);
            return true;
        }, cancellationToken);

    public Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreAsync(operation, cancellationToken);

    private async Task<T> ExecuteCoreAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            return await operation(cancellationToken);
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);

        try
        {
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            finally
            {
                _dbContext.ChangeTracker.Clear();
            }

            throw;
        }
    }
}
