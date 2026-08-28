
using ApplicationCore.Entities;
using ApplicationCore.Interfaces.Repositories.SupplierType;
using ApplicationCore.Utilities.Helpers.ApplicationCore.Common.Helpers;
using ApplicationCore.ValueObjects.ContactType;
using ApplicationCore.ValueObjects.SupplierType;
using AutoMapper;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace Infrastructure.Repositories.SupplierType
{
    public class SupplierTypeRepository : ISupplierTypeRepository
    {
        private readonly IMapper _mapper;
        private readonly IDbConnection _connection;

        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        private const string _tableName = "SupplierTypes";
        private const string _idColumn = "Id";

        public SupplierTypeRepository(IConfiguration configuration, IMapper mapper)
        {
            _configuration = configuration;
            _mapper = mapper;  
        }
        public async Task Create(CreateSupplierTypeVO supplierType)
        {
            // Get the properties of the entity
            var properties = typeof(CreateSupplierTypeVO).GetProperties().ToList();

            // Create the columns and paramNames
            var columns = string.Join(", ", properties.Select(p => $"\"{p.Name}\""));
            var paramNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            //Build the SQL query
            string query = StringHelper.BuildInsertQuery(_tableName, supplierType);

            var id = await _connection.ExecuteScalarAsync<int>(query, new { supplierType.Name });
        }

        public async Task Delete(int id)
        {
            var query = $"DELETE FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            await _connection.ExecuteAsync(query, new { Id = id });
        }

        public async Task<IEnumerable<ListSupplierTypeVO>> GetAll()
        {
            var query = $"SELECT * FROM \"{_tableName}\"";
            var entities = await _connection.QueryAsync<ListSupplierTypeVO>(query);

            return entities;
        }

        public async Task<GetSupplierTypeVO> GetById(int id)
        {
            var query = $"SELECT * FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            var entity = await _connection.QuerySingleOrDefaultAsync<GetSupplierTypeVO>(query, new { Id = id });

            return entity;
        }

        public async Task Update(UpdateSupplierTypeVO supplierType)
        {
            string query = $@"UPDATE ""{_tableName}"" SET ""Name"" = @Name WHERE ""Id"" = @Id";

            int rowsAffected = await _connection.ExecuteAsync(query, new
            {
                supplierType.Id,
                supplierType.Name
            });
        }
    }
}
