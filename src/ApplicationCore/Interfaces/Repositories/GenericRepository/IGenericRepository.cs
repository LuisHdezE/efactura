namespace ApplicationCore.Interfaces.Repositories.GenericRepository
{
    public interface IGenericRepository<TEntity> where TEntity : class
    {
        Task<TEntity> GetByIdTracked(int id);

        Task<U> GetByIdAndMap<U>(int id);

        IQueryable<TEntity> GetIQueryable();
        
        Task<IEnumerable<TEntity>> GetAll(string tableName);

        Task BeginTransaction();

        Task CommitTransaction();

        Task RollbackTransaction();

        Task Create(TEntity entity);

        Task Update(int id, TEntity entity);

        Task Delete(int id);
    }
}