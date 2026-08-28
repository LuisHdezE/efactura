using ApplicationCore.Entites;
using ApplicationCore.Interfaces.Repositories.TaxType;
using ApplicationCore.Utilities.Helpers.ApplicationCore.Common.Helpers;
using ApplicationCore.ValueObjects.SupplierType;
using ApplicationCore.ValueObjects.TaxType;
using AutoMapper;
using Dapper;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.TaxType
{
    public class TaxTypeRepository: ITaxTypeRepository
    {
        private readonly IMapper _mapper;
        private readonly IDbConnection _connection;

        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        private const string _tableName = "TaxTypes";
        private const string _idColumn = "Id";

        public TaxTypeRepository(IConfiguration configuration, IMapper mapper)
        {
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task<GetTaxTypeVO> GetById(int id)
        {
            var query = $"SELECT * FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            var entity = await _connection.QuerySingleOrDefaultAsync<GetTaxTypeVO>(query, new { Id = id });

            return entity;
        }

        public async Task<IEnumerable<ListTaxTypeVO>> GetAll()
        {
            var query = $"SELECT * FROM \"{_tableName}\"";
            var entities = await _connection.QueryAsync<ListTaxTypeVO>(query);

            return entities;
        }

        public async Task Create(CreateTaxTypeVO taxType)
        {
            // Get the properties of the entity
            var properties = typeof(CreateTaxTypeVO).GetProperties().ToList();

            // Create the columns and paramNames
            var columns = string.Join(", ", properties.Select(p => $"\"{p.Name}\""));
            var paramNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            //Build the SQL query
            string query = StringHelper.BuildInsertQuery(_tableName, taxType);

            var id = await _connection.ExecuteScalarAsync<int>(query, new { taxType.Name });
        }

        public async Task Update(UpdateTaxTypeVO taxType)
        {
            string query = $@"UPDATE ""{_tableName}"" SET ""Name"" = @Name WHERE ""Id"" = @Id";

            int rowsAffected = await _connection.ExecuteAsync(query, new
            {
                taxType.Id,
                taxType.Name
            });
        }

        public async Task Delete(int id)
        {
            var query = $"DELETE FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            await _connection.ExecuteAsync(query, new { Id = id });
        }
    }
}
