namespace ApplicationCore.Interfaces.Repositories.GenericRepository
{
    public interface IDapperGenericRepository<TEntity> where TEntity : class
    {
        Task<IEnumerable<TEntity>> GetAll(string tableName);

        Task<TEntity> GetByIdTracked(string tableName, string idColumn, object id);
        
        Task<IEnumerable<TEntity>> GetIQueryable(string[] columns, string tableName, string whereClause = null);
        
        Task Create(string tableName, TEntity entity);
        
        Task Update(string tableName, string idColumn, TEntity vo, object id);
        
        Task Delete(string tableName, string idColumn, object id);

        

    }
}
