using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Transactions;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly Write.V1PersistenceDbContext _dbContext;

    public EfUnitOfWork(Write.V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                "The resource changed before this operation could be committed.",
                conflictType: "stale_version");
        }
    }
}
