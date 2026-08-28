using ApplicationCore.Interfaces.Repositories.InvoiceIndicator;
using ApplicationCore.Utilities.Helpers.ApplicationCore.Common.Helpers;
using ApplicationCore.ValueObjects.InvoiceIndicator;
using AutoMapper;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace Infrastructure.Repositories.InvoiceIndicator
{
    public class InvoiceIndicatorRepository : IInvoiceIndicatorRepository
    {
        private readonly IMapper _mapper;
        private readonly IDbConnection _connection;

        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
         
        private const string _tableName = "InvoiceIndicators";
        private const string _idColumn = "Id";

        public InvoiceIndicatorRepository(IConfiguration configuration, IMapper mapper)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("PostgresConnection");

            _connection = new NpgsqlConnection(_connectionString);
            _mapper = mapper;
        }

        public async Task Create(CreateInvoiceIndicatorVO invoiceIndicatorVO)
        {
            // Get the properties of the entity
            var properties = typeof(CreateInvoiceIndicatorVO).GetProperties().ToList();

            // Create the columns and paramNames
            var columns = string.Join(", ", properties.Select(p => $"\"{p.Name}\""));
            var paramNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            //Build the SQL query
            string query = StringHelper.BuildInsertQuery(_tableName, invoiceIndicatorVO);

            var id = await _connection.ExecuteScalarAsync<int>(query, new
            {
                invoiceIndicatorVO.Id,
                invoiceIndicatorVO.Name
            });
        }

        public async Task Delete(int id)
        {
            var query = $"DELETE FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            await _connection.ExecuteAsync(query, new { Id = id });
        }

        public async Task<IEnumerable<ListInvoiceIndicatorVO>> GetAll()
        {
            var query = $"SELECT * FROM \"{_tableName}\"";
            var entities = await _connection.QueryAsync<ListInvoiceIndicatorVO>(query);

            return entities;
        }

        public async Task<GetInvoiceIndicatorVO> GetById(int id)
        {
            var query = $"SELECT * FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            var entity = await _connection.QuerySingleOrDefaultAsync<GetInvoiceIndicatorVO>(query, new { Id = id });

            return entity;
        }

        public async Task Update(UpdateInvoiceIndicatorVO invoiceIndicatorVO)
        {
            string query = $@"UPDATE ""{_tableName}"" SET ""Name"" = @Name WHERE ""Id"" = @Id";

            int rowsAffected = await _connection.ExecuteAsync(query, new
            {
                invoiceIndicatorVO.Id,
                invoiceIndicatorVO.Name
            });
        }
    }
}
