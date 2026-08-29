using EFactura.Application.Common.Persistence;

namespace Infrastructure.Persistence.V1.Transactions;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly Write.V1PersistenceDbContext _dbContext;

    public EfUnitOfWork(Write.V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _dbContext.SaveChangesAsync(cancellationToken);
}
