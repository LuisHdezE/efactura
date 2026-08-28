using ApplicationCore.Interfaces.Repositories.PaymentMethod;
using ApplicationCore.Utilities.Helpers.ApplicationCore.Common.Helpers;
using ApplicationCore.ValueObjects.PaymentMethod;
using AutoMapper;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace Infrastructure.Repositories.PaymentMethod
{
    public class PaymentMethodRepository: IPaymentMethodRepository
    {
        private readonly IMapper _mapper;
        private readonly IDbConnection _connection;

        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        private const string _tableName = "PaymentMethods";
        private const string _idColumn = "Id";

        public PaymentMethodRepository(IConfiguration configuration, IMapper mapper) 
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("PostgresConnection");

            _connection = new NpgsqlConnection(_connectionString);
            _mapper = mapper;
        }

        public async Task<IEnumerable<ListPaymentMethodVO>> GetAll()
        {
            var query = $"SELECT * FROM \"{_tableName}\"";
            var entities = await _connection.QueryAsync<ListPaymentMethodVO>(query);

            return entities;
        }

        public async Task<GetPaymentMethodVO> GetById(long id)
        {
            var query = $"SELECT * FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            var entity = await _connection.QuerySingleOrDefaultAsync<GetPaymentMethodVO>(query, new { Id = id });

            return entity;
        }

        public async Task Create(CreatePaymentMethodVO paymentMethodVO)
        {
            // Get the properties of the entity
            var properties = typeof(CreatePaymentMethodVO).GetProperties().ToList();

            // Create the columns and paramNames
            var columns = string.Join(", ", properties.Select(p => $"\"{p.Name}\""));
            var paramNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            //Build the SQL query
            string query = StringHelper.BuildInsertQuery(_tableName, paymentMethodVO);

            var id = await _connection.ExecuteScalarAsync<int>(query, new
            {
                paymentMethodVO.Name
            });
        }

        public async Task Update(UpdatePaymentMethodVO paymentMethodVO)
        {
            string query = $@"UPDATE ""{_tableName}"" SET ""Name"" = @Name
                    WHERE ""Id"" = @Id";

            int rowsAffected = await _connection.ExecuteAsync(query, new
            {
                paymentMethodVO.Id,
                paymentMethodVO.Name
            });
        }

        public async Task Delete(long id)
        {
            var query = $"DELETE FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            await _connection.ExecuteAsync(query, new { Id = id });
        }
    }
}
