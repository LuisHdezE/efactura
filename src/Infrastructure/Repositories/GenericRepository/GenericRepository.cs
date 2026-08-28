using ApplicationCore.Interfaces.Repositories.GenericRepository;
using AutoMapper;
using Dapper;
using Infrastructure.DataBase.Context;
using Npgsql;
using System.Data;

namespace Infrastructure.Repositories.GenericRepository
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        protected readonly DBContext _dbContext;
        private readonly IMapper _mapper;
       
        private readonly IDbConnection _connection;
        private IDbTransaction _transaction;

        public GenericRepository(DBContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        /*public GenericRepository(string connectionString, IMapper mapper)
        {
            _connection = new NpgsqlConnection(connectionString);
            _mapper = mapper;
        }*/

       /* public void Open()
        {
            if (_connection.State == ConnectionState.Closed)
                _connection.Open();
        }

        public void Close()
        {
            if (_connection.State == ConnectionState.Open)
                _connection.Close();
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _connection.Dispose();
        }*/

        //Analizar, posible eliminacion
       public async Task<IEnumerable<TEntity>> GetAll(string tableName)
        {
            var query = $"SELECT * FROM {tableName}";
            var entities = await _connection.QueryAsync<TEntity>(query);

            // Map entities to ValueObjects
            return _mapper.Map<IEnumerable<TEntity>>(entities);
        }

        /// <summary>
        /// Retorna la entidad trackeada recibida filtrando por el campo Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<TEntity> GetByIdTracked(int id)
        {
            return await _dbContext.Set<TEntity>().FindAsync(id);
        }

      /*  public async Task<TEntity> GetByIdTracked(string tableName, string idColumn, object id)
        {
            var query = $"SELECT * FROM {tableName} WHERE {idColumn} = @Id";
            var entity = await _connection.QuerySingleOrDefaultAsync<TEntity>(query, new { Id = id });

            // Map entity to DTO
            return _mapper.Map<TEntity>(entity);
        }*/


        /// <summary>
        /// Retorna la entidad mapeada al value object recibido, se debe tener en cuenta que debe existir el mapeo de la entidad con el VO en AutomapperProfiles.
        /// </summary>
        /// <typeparam name="U"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<U> GetByIdAndMap<U>(int id)
        {
            var entity = await _dbContext.Set<TEntity>().FindAsync(id);
            return _mapper.Map<TEntity, U>(entity);
        }

        /// <summary>
        /// Retorna un objeto Iqueriable para realizar diferentes operaciones de consulta
        /// </summary>
        /// <returns></returns>
        public IQueryable<TEntity> GetIQueryable()
        {
            return _dbContext.Set<TEntity>();
        }

        /*public async Task<IEnumerable<TEntity>> GetIQueryable(string[] columns, string tableName, string whereClause = null)
        {
            // Si no se proporcionan columnas, selecciona todas
            var selectedColumns = columns?.Length > 0 ? string.Join(", ", columns) : "*";

            var query = $"SELECT {selectedColumns} FROM {tableName}";

            // Agregar un WHERE si hay un filtro
            if (!string.IsNullOrEmpty(whereClause))
            {
                query += " WHERE " + whereClause;
            }

            return await _connection.QueryAsync<TEntity>(query);
        }*/

        public async Task Create(TEntity entity)
        {
            await _dbContext.Set<TEntity>().AddAsync(entity);
            await _dbContext.SaveChangesAsync();
        }

        /*public async Task Create(string tableName, TEntity vo)
        {
            var entity = _mapper.Map<TEntity>(vo);

            // Get the properties of the entity that we want to insert
            var properties = entity.GetType().GetProperties()
                                   .Where(p => p.CanRead && p.GetValue(entity) != null)  // Only include readable properties with values
                                   .ToArray();

            // Create the columns and values placeholders
            var columns = string.Join(", ", properties.Select(p => p.Name)); // Get the property names as column names
            var values = string.Join(", ", properties.Select(p => $"@{p.Name}")); // Create parameters for each property

            // Create the query string
            var query = $"INSERT INTO {tableName} ({columns}) VALUES ({values}) RETURNING id";

            // Execute the query
            var parameters = properties.ToDictionary(p => p.Name, p => p.GetValue(entity)); // Create a dictionary of parameters
            await _connection.QuerySingleAsync<int>(query, parameters); // Use parameters safely
        }*/


        public async Task Update(int id, TEntity entity)
        {
            _dbContext.Set<TEntity>().Update(entity);
            await _dbContext.SaveChangesAsync();
        }

       /* public async Task Update(string tableName, string idColumn, TEntity vo, object id)
        {
            // Map DTO to entity
            var entity = _mapper.Map<TEntity>(vo);

            var setClause = string.Join(", ", entity.GetType().GetProperties().Select(p => $"{p.Name} = @{p.Name}"));
            var query = $"UPDATE {tableName} SET {setClause} WHERE {idColumn} = @Id";

            await _connection.ExecuteAsync(query, new { Id = id });
        }*/

        public async Task BeginTransaction()
        {
            await _dbContext.Database.BeginTransactionAsync();
        }

        public async Task CommitTransaction()
        {
            await _dbContext.Database.CommitTransactionAsync();
        }

        public async Task RollbackTransaction()
        {
            await _dbContext.Database.RollbackTransactionAsync();
        }

        public async Task Delete(int id)
        {
            var entity = await GetByIdTracked(id);
            _dbContext.Set<TEntity>().Remove(entity);
            await _dbContext.SaveChangesAsync();
        }

       /* public async Task Delete(string tableName, string idColumn, object id)
        {
            var query = $"DELETE FROM {tableName} WHERE {idColumn} = @Id";
            await _connection.ExecuteAsync(query, new { Id = id });
        }*/
    }
}