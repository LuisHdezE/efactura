using ApplicationCore.Interfaces.Repositories.Country;
using ApplicationCore.Utilities.Helpers.ApplicationCore.Common.Helpers;
using ApplicationCore.ValueObjects.Country;
using AutoMapper;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace Infrastructure.Repositories.Country
{
    public class CountryRepository : ICountryRepository
    {
        private readonly IMapper _mapper;
        private readonly IDbConnection _connection;

        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        private const string _tableName = "Countries";
        private const string _idColumn = "Id";

        public CountryRepository(IConfiguration configuration, IMapper mapper)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("PostgresConnection");

            _connection = new NpgsqlConnection(_connectionString);
            _mapper = mapper;
        }

        public async Task Create(CreateCountryVO countryVO)
        {
            // Get the properties of the entity
            var properties = typeof(CreateCountryVO).GetProperties().ToList();

            // Create the columns and paramNames
            var columns = string.Join(", ", properties.Select(p => $"\"{p.Name}\""));
            var paramNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            //Build the SQL query
            string query = StringHelper.BuildInsertQuery(_tableName, countryVO);

            var id = await _connection.ExecuteScalarAsync<int>(query, new { countryVO.Name, countryVO.Code });
        }

        public async Task Delete(int id)
        {
            var query = $"DELETE FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            await _connection.ExecuteAsync(query, new { Id = id });
        }

        public async Task<IEnumerable<ListCountryVO>> GetAll()
        {
            var query = $"SELECT * FROM \"{_tableName}\"";
            var entities = await _connection.QueryAsync<ListCountryVO>(query);

            return entities;
        }

        public async Task<GetCountryByIdVO> GetById(int id)
        {
            var query = $"SELECT * FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            var entity = await _connection.QuerySingleOrDefaultAsync<GetCountryByIdVO>(query, new { Id = id });

            return entity;
        }

        public async Task Update(UpdateCountryVO countryVO)
        {
            string query = $@"UPDATE ""{_tableName}"" SET ""Name"" = @Name WHERE ""Id"" = @Id";

            int rowsAffected = await _connection.ExecuteAsync(query, new
            {
                countryVO.Id,
                countryVO.Name,
                countryVO.Code
            });
        }
    }

}
