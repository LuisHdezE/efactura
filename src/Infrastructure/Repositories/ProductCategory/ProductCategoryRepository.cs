using ApplicationCore.Entities;
using ApplicationCore.Interfaces.Repositories.ProductCategory;
using ApplicationCore.Utilities.Helpers.ApplicationCore.Common.Helpers;
using ApplicationCore.ValueObjects.ContactType;
using ApplicationCore.ValueObjects.ProductCategory;
using AutoMapper;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.ProductCategory
{
    public class ProductCategoryRepository : IProductCategoryRepository
    {
        private readonly IMapper _mapper;
        private readonly IDbConnection _connection;

        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        private const string _tableName = "ProductCategories";
        private const string _idColumn = "Id"; 
        
        public ProductCategoryRepository(IConfiguration configuration, IMapper mapper)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("PostgresConnection");

            _connection = new NpgsqlConnection(_connectionString);
            _mapper = mapper;
        }

        public async Task Create(CreateProductCategoryVO productCategoryVO)
        {
            // Get the properties of the entity
            var properties = typeof(CreateProductCategoryVO).GetProperties().ToList();

            // Create the columns and paramNames
            var columns = string.Join(", ", properties.Select(p => $"\"{p.Name}\""));
            var paramNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            //Build the SQL query
            string query = StringHelper.BuildInsertQuery(_tableName, productCategoryVO);

            var id = await _connection.ExecuteScalarAsync<int>(query, new { productCategoryVO.Name });
        }

        public async Task Delete(int id)
        {
            var query = $"DELETE FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            await _connection.ExecuteAsync(query, new { Id = id });
        }

        public async Task<IEnumerable<ListProductCategoryVO>> GetAll()
        {
            var query = $"SELECT * FROM \"{_tableName}\"";
            var entities = await _connection.QueryAsync<ListProductCategoryVO>(query);

            return entities;
        }

        public async Task<GetProductCategoryVO> GetById(int id)
        {
            var query = $"SELECT * FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            var entity = await _connection.QuerySingleOrDefaultAsync<GetProductCategoryVO>(query, new { Id = id });

            return entity;
        }

        public async Task Update(UpdateProductCategoryVO productCategoryVO)
        {
            string query = $@"UPDATE ""{_tableName}"" SET ""Name"" = @Name WHERE ""Id"" = @Id";

            int rowsAffected = await _connection.ExecuteAsync(query, new
            {
                productCategoryVO.Id,
                productCategoryVO.Name
            });
        }
    }
}
