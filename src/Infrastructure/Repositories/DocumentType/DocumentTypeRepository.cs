using ApplicationCore.Interfaces.Repositories.DocumentType;
using ApplicationCore.Utilities.Helpers.ApplicationCore.Common.Helpers;
using ApplicationCore.ValueObjects.DocumentType;
using AutoMapper;
using Dapper;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Data;
using static Dapper.SqlMapper;

namespace Infrastructure.Repositories.DocumentType
{
    public class DocumentTypeRepository : IDocumentTypeRepository
    {
        private readonly IMapper _mapper;
        private readonly IDbConnection _connection;

        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        private const string _tableName = "DocumentTypes";
        private const string _idColumn = "Id";

        public DocumentTypeRepository(IConfiguration configuration, IMapper mapper)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("PostgresConnection");

            _connection = new NpgsqlConnection(_connectionString);
            _mapper = mapper;
        }

        public async Task Create(CreateDocumentTypeVO documentTypeVO)
        {
            // Get the properties of the entity
            var properties = typeof(CreateDocumentTypeVO).GetProperties().ToList();

            // Create the columns and paramNames
            var columns = string.Join(", ", properties.Select(p => $"\"{p.Name}\"")); 
            var paramNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));
            
            //Build the SQL query
            string query = StringHelper.BuildInsertQuery(_tableName, documentTypeVO);

            var id = await _connection.ExecuteScalarAsync<int>(query, new { documentTypeVO.Name});
        }

        public async Task Delete(int id)
        {
            var query = $"DELETE FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            await _connection.ExecuteAsync(query, new { Id = id });
        }

        public async Task<IEnumerable<ListDocumentTypeVO>> GetAll()
        {
            var query = $"SELECT * FROM \"{_tableName}\"";
            var entities = await _connection.QueryAsync<ListDocumentTypeVO>(query);

            return entities;
        }

        public async Task<GetDocumentTypeVO> GetById(int id)
        {
            var query = $"SELECT * FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            var entity = await _connection.QuerySingleOrDefaultAsync<GetDocumentTypeVO>(query, new { Id = id });

            return entity;
        }

        public async Task Update(UpdateDocumentTypeVO documentTypeVO)
        {
            string query = @"UPDATE ""DocumentTypes"" SET ""Name"" = @Name WHERE ""Id"" = @Id";

            int rowsAffected = await _connection.ExecuteAsync(query, new {
                documentTypeVO.Id,
                documentTypeVO.Name
            });
        }
    }
}
