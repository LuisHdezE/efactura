using ApplicationCore.Interfaces.Repositories.ContactTypeRepository;
using ApplicationCore.Utilities.Helpers.ApplicationCore.Common.Helpers;
using ApplicationCore.ValueObjects.ContactType;
using AutoMapper;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace Infrastructure.Repositories.ContactTypeRepository
{
    public class ContactTypeRepository : IContactTypeRepository
    { 
        private readonly IMapper _mapper;
        private readonly IDbConnection _connection;

        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        private const string _tableName = "ContactTypes";
        private const string _idColumn = "Id";

        public ContactTypeRepository(IConfiguration configuration, IMapper mapper)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("PostgresConnection");

            _connection = new NpgsqlConnection(_connectionString);
            _mapper = mapper;
        }

        public async Task Create(CreateContactTypeVO contactTypeVO)
        {
            // Get the properties of the entity
            var properties = typeof(CreateContactTypeVO).GetProperties().ToList();

            // Create the columns and paramNames
            var columns = string.Join(", ", properties.Select(p => $"\"{p.Name}\""));
            var paramNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            //Build the SQL query
            string query = StringHelper.BuildInsertQuery(_tableName, contactTypeVO);

            var id = await _connection.ExecuteScalarAsync<int>(query, new { contactTypeVO.Name });
        }

        public async Task Delete(int id)
        {
            var query = $"DELETE FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            await _connection.ExecuteAsync(query, new { Id = id });
        }

        public async Task<IEnumerable<ListContactTypeVO>> GetAll()
        {
            var query = $"SELECT * FROM \"{_tableName}\"";
            var entities = await _connection.QueryAsync<ListContactTypeVO>(query);

            return entities;
        }

        public async Task<GetContactTypeVO> GetById(int id)
        {
            var query = $"SELECT * FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            var entity = await _connection.QuerySingleOrDefaultAsync<GetContactTypeVO>(query, new { Id = id });

            return entity;
        }

        public async Task Update(UpdateContactTypeVO contactTypeVO)
        {
            string query = $@"UPDATE ""{_tableName}"" SET ""Name"" = @Name WHERE ""Id"" = @Id";

            int rowsAffected = await _connection.ExecuteAsync(query, new
            {
                contactTypeVO.Id,
                contactTypeVO.Name
            });
        }
    }
}
