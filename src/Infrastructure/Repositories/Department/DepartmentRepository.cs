using ApplicationCore.Entities;
using ApplicationCore.Interfaces.Repositories.Department;
using ApplicationCore.Utilities.Helpers.ApplicationCore.Common.Helpers;
using ApplicationCore.ValueObjects.ContactType;
using ApplicationCore.ValueObjects.Department;
using ApplicationCore.ValueObjects.Result;
using AutoMapper;
using Dapper;
using Infrastructure.Repositories.GenericRepository;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace Infrastructure.Repositories.DepartmentRepository
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly IMapper _mapper;
        private readonly IDbConnection _connection;

        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        private const string _tableName = "Departments";
        private const string _idColumn = "Id";
        
        public DepartmentRepository(IConfiguration configuration, IMapper mapper)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("PostgresConnection");

            _connection = new NpgsqlConnection(_connectionString);
            _mapper = mapper;
        }

        public async Task Create(CreateDepartmentVO departmentVO)
        {
            // Get the properties of the entity
            var properties = typeof(CreateDepartmentVO).GetProperties().ToList();

            // Create the columns and paramNames
            var columns = string.Join(", ", properties.Select(p => $"\"{p.Name}\""));
            var paramNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            //Build the SQL query
            string query = StringHelper.BuildInsertQuery(_tableName, departmentVO);

            var id = await _connection.ExecuteScalarAsync<int>(query, new { 
                departmentVO.Name,
                departmentVO.CountryId
            });
        }

        public async Task Delete(int id)
        {
            var query = $"DELETE FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            await _connection.ExecuteAsync(query, new { Id = id });
        }

        public async Task<IEnumerable<ListDepartmentVO>> GetAll()
        {
            var query = $"SELECT * FROM \"{_tableName}\"";
            var entities = await _connection.QueryAsync<ListDepartmentVO>(query);

            return entities;
        }

        public async Task<GetDepartmentVO> GetById(int id)
        {
            var query = $"SELECT * FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            var entity = await _connection.QuerySingleOrDefaultAsync<GetDepartmentVO>(query, new { Id = id });

            return entity;
        }

        public async Task Update(UpdateDepartmentVO departmentVO)
        {
            string query = $@"UPDATE ""{_tableName}"" 
                    SET 
                        ""Name"" = @Name, 
                        ""CountryId"" = @CountryId 
                    WHERE ""Id"" = @Id";

            int rowsAffected = await _connection.ExecuteAsync(query, new
            {
                departmentVO.Id,
                departmentVO.Name,
                departmentVO.CountryId
            });
        }
    }
}
