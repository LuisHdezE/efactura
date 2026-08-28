
using ApplicationCore.Interfaces.Repositories.VoucherType;
using ApplicationCore.Utilities.Helpers.ApplicationCore.Common.Helpers;
using ApplicationCore.ValueObjects.VoucherType;
using AutoMapper;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace Infrastructure.Repositories.VoucherType
{
    public class VoucherTypeRepository:IVoucherTypeRepository
    {
        private readonly IMapper _mapper;
        private readonly IDbConnection _connection;

        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        private const string _tableName = "VoucherTypes";
        private const string _idColumn = "Id";

        public VoucherTypeRepository(IConfiguration configuration, IMapper mapper)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("PostgresConnection");

            _connection = new NpgsqlConnection(_connectionString);
            _mapper = mapper;
        }

        public async Task Create(CreateVoucherTypeVO voucherTypeVO)
        {
            // Get the properties of the entity
            var properties = typeof(CreateVoucherTypeVO).GetProperties().ToList();

            // Create the columns and paramNames
            var columns = string.Join(", ", properties.Select(p => $"\"{p.Name}\""));
            var paramNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            //Build the SQL query
            string query = StringHelper.BuildInsertQuery(_tableName, voucherTypeVO);

            var id = await _connection.ExecuteScalarAsync<int>(query, new { 
                voucherTypeVO.Id, 
                voucherTypeVO.Name 
            });
        }

        public async Task Delete(int id)
        {
            var query = $"DELETE FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            await _connection.ExecuteAsync(query, new { Id = id });
        }

        public async Task<IEnumerable<ListVoucherTypeVO>> GetAll()
        {
            var query = $"SELECT * FROM \"{_tableName}\"";
            var vouchers = await _connection.QueryAsync<ListVoucherTypeVO>(query);

            return vouchers;
        }

        public async Task<GetVoucherTypeVO> GetById(int id)
        {
            var query = $"SELECT * FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            var voucher = await _connection.QuerySingleOrDefaultAsync<GetVoucherTypeVO>(query, new { Id = id });

            return voucher;
        }

        public async Task Update(UpdateVaucherTypeVO voucherTypeVO)
        {
            string query = $@"UPDATE ""{_tableName}"" SET ""Name"" = @Name WHERE ""Id"" = @Id";

            int rowsAffected = await _connection.ExecuteAsync(query, new
            {
                voucherTypeVO.Id,
                voucherTypeVO.Name
            });
        }
    }
}
