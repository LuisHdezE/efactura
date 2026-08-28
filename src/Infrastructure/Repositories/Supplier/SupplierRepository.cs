using ApplicationCore.Entities;
using ApplicationCore.Interfaces.Repositories.Supplier;
using ApplicationCore.Utilities.Helpers.ApplicationCore.Common.Helpers;
using ApplicationCore.Utilities.Paginado;
using ApplicationCore.ValueObjects.ContactType;
using ApplicationCore.ValueObjects.Customer;
using ApplicationCore.ValueObjects.Result;
using ApplicationCore.ValueObjects.Supplier;
using AutoMapper;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Data;

namespace Infrastructure.Repositories.Supplier
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly IMapper _mapper;
        private readonly IDbConnection _connection;

        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        private const string _tableName = "Suppliers";
        private const string _idColumn = "Id";

        public SupplierRepository(IConfiguration configuration, IMapper mapper)
        {
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task Create(CreateSupplierVO supplierVO)
        {
            // Get the properties of the entity
            var properties = typeof(CreateSupplierVO).GetProperties().ToList();

            // Create the columns and paramNames
            var columns = string.Join(", ", properties.Select(p => $"\"{p.Name}\""));
            var paramNames = string.Join(", ", properties.Select(p => $"@{p.Name}"));

            //Build the SQL query
            string query = StringHelper.BuildInsertQuery(_tableName, supplierVO);

            var id = await _connection.ExecuteScalarAsync<int>(query, new { 
                supplierVO.Name,
                supplierVO.ContactName,
                supplierVO.Phone,
                supplierVO.Email,
                supplierVO.Address,
                supplierVO.SupplierTypeId

            });
        }

        public async Task Delete(int id)
        {
            var query = $"DELETE FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            await _connection.ExecuteAsync(query, new { Id = id });
        }

        public async Task<IEnumerable<ListSupplierVO>> GetAll()
        {
            var query = $"SELECT * FROM \"{_tableName}\"";
            var entities = await _connection.QueryAsync<ListSupplierVO>(query);

            return entities;
        }

        public async Task<ResultObject> GetSuppliersPaginated(int Page, int RowsPerPage)
        {
            ResultObject result = new();
            
            int totalRegistros = 0;
            string query = $@"SELECT
                                COUNT(*)
                            FROM ""{_tableName}""
                            WHERE ""DeletedAt"" IS NULL;

                            SELECT * FROM ""{_tableName}""
                            WHERE ""DeletedAt"" IS NULL
                            ORDER BY ""Id""
                            OFFSET @Offset LIMIT @RowsPerPage;
                            ";

            var reader = await _connection.QueryMultipleAsync(query, new { Offset = (Page - 1) * RowsPerPage, RowsPerPage });
            totalRegistros = reader.Read<int>().FirstOrDefault();
            List<ListSupplierVO> resultSuppliers = reader.Read<ListSupplierVO>().ToList();
            result.Data = Paginado.CargarPaginado(resultSuppliers, RowsPerPage, totalRegistros, Page);
            result.Status = true;
            return result;
        }

        public async Task<GetSupplierVO> GetById(int id)
        {
            var query = $"SELECT * FROM \"{_tableName}\" WHERE \"{_idColumn}\" = @Id";
            var entity = await _connection.QuerySingleOrDefaultAsync<GetSupplierVO>(query, new { Id = id });

            return entity;
        }

        public async Task Update(UpdateSupplierVO supplierVO)
        {
            string query = $@"UPDATE ""{_tableName}"" SET 
                                ""Name"" = @Name,
                                ""ContactName"" = @ContactName,
                                ""Phone"" = @Phone,
                                ""Email"" = @Email,
                                ""Address"" = @Address,
                                ""SupplierTypeId"" = @SupplierTypeId,
                                WHERE ""Id"" = @Id";

            int rowsAffected = await _connection.ExecuteAsync(query, new
            {
                supplierVO.Id,
                supplierVO.Name,
                supplierVO.ContactName,
                supplierVO.Phone,
                supplierVO.Email,
                supplierVO.Address,
                supplierVO.SupplierTypeId
            });
        }
    }
}
