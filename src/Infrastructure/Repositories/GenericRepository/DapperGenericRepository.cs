using ApplicationCore.Entities;
using ApplicationCore.Interfaces.Repositories.GenericRepository;
using ApplicationCore.Utilities.Helpers.ApplicationCore.Common.Helpers;
using ApplicationCore.ValueObjects.Country;
using AutoMapper;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;
using static Dapper.SqlMapper;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Infrastructure.Repositories.GenericRepository
{
    public class DapperGenericRepository<TEntity> : IDapperGenericRepository<TEntity> where TEntity : class
    {
        private readonly IMapper _mapper;
        private readonly IDbConnection _connection;
        
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DapperGenericRepository(IConfiguration configuration, IMapper mapper)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("PostgresConnection");
    
            _connection = new NpgsqlConnection(_connectionString);
            _mapper = mapper;
        }

        public void Open()
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
           // _transaction?.Dispose();
            _connection.Dispose();
        }

        public async Task<IEnumerable<TEntity>> GetAll(string tableName)
        {
            var query = $"SELECT * FROM \"{tableName}\"";
            var entities = await _connection.QueryAsync<TEntity>(query);

            return entities;
        }

        public async Task<TEntity> GetByIdTracked(string tableName, string idColumn, object id)
        {
            var query = $"SELECT * FROM \"{tableName}\" WHERE \"{idColumn}\" = @Id";
            var entity = await _connection.QuerySingleOrDefaultAsync<TEntity>(query, new { Id = id });

            return entity;
        }

        public async Task<IEnumerable<TEntity>> GetIQueryable(string[] columns, string tableName, string whereClause = null)
        {
            // Si no se proporcionan columnas, selecciona todas
            var selectedColumns = columns?.Length > 0 ? string.Join(", ", columns) : "*";

            var query = $"SELECT {selectedColumns} FROM \"{tableName}\"";

            // Agregar un WHERE si hay un filtro
            if (!string.IsNullOrEmpty(whereClause))
            {
                query += " WHERE " + whereClause;
            }

            return await _connection.QueryAsync<TEntity>(query);
        }

        public async Task Create(string tableName, TEntity entity)
        {
            // Obtiene las propiedades de la entidad
            var entityName = typeof(TEntity).Name;

            // Obtener las propiedades no nulas de la entidad
            var properties = typeof(TEntity).GetProperties()
                .Where(p => p.GetValue(entity) != null &&
                           !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) &&
                           !p.Name.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase) &&
                           !p.Name.Equals("UpdatedAt", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Create the columns and values placeholders
            var columns = string.Join(", ", properties.Select(p => $"\"{p.Name}\"")); // Get the property names as column names
            var paramNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            // Crear el diccionario de parámetros
            var parameters = new DynamicParameters();
            foreach (var prop in properties)
            {
                parameters.Add($"@{prop.Name}", prop.GetValue(entity));
            }

            // Construir la consulta SQL
            string query = $@"
                INSERT INTO ""{tableName}"" ({columns}) 
                VALUES ({paramNames}) 
                RETURNING ""Id""";

            // Ejecutar la consulta
            var id = await _connection.ExecuteScalarAsync<int>(query, parameters);

            // Asignar el ID generado a la entidad
            var idProperty = typeof(TEntity).GetProperty("Id");
            if (idProperty != null)
            {
                idProperty.SetValue(entity, id);
            }

        }

        public async Task Delete(string tableName, string idColumn, object id)
        {
            var query = $"DELETE FROM \"{tableName}\" WHERE \"{idColumn}\" = @Id";
            await _connection.ExecuteAsync(query, new { Id = id });
        }

        
        public async Task Update(string tableName, string idColumn, TEntity entity, object id)
        {
            // Obtiene las propiedades de la entidad
            var entityName = typeof(TEntity).Name;

            // Obtener las propiedades no nulas de la entidad
            var properties = typeof(TEntity).GetProperties()
                .Where(p => p.GetValue(entity) != null &&
                           !p.Name.Equals("CreatedAt", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Create the columns and values placeholders
            var columns = string.Join(", ", properties.Select(p => $"\"\"{p.Name}\"\" = @{p.Name}")); // Get the property names as column names
            var objectColumns = string.Join(", ", properties.Select(p => $"\"{p.Name}\""));
            object paramNames = StringHelper.AnonimousObject(objectColumns, "contactTypeVO");

            
            string query = $@"UPDATE ""{tableName}"" SET {columns} WHERE ""{id}"" = @Id";

            int rowsAffected = await _connection.ExecuteAsync(query, paramNames);
        }

        
    }
}
