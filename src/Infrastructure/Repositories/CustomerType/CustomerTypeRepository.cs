using ApplicationCore.Constants;
using ApplicationCore.Interfaces.Repositories.CustomerType;
using ApplicationCore.Utilities.Paginado;
using ApplicationCore.ValueObjects.CustomerType;
using ApplicationCore.ValueObjects.Result;
using Dapper;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Algorithm;
using Npgsql;
using Polly.Caching;
using System.Data;

namespace Infrastructure.Repositories.CustomerType
{
    public class CustomerTypeRepository(IConfiguration configuration) : ICustomerTypeRepository
    {
        private readonly IConfiguration _configuration = configuration;
        public async Task<ResultObject> Create(CreateCustomerTypeVO customerTypeVO)
        {
            ResultObject result = new();
            using var connection = new NpgsqlConnection(_configuration.GetConnectionString("PostgresConnection"));
            string query = @"INSERT INTO CustomerTypes (""Name"", ""UserId"" ) VALUES (@Name, @UserId) RETURNING Id";
            var insertedId = await connection.ExecuteScalarAsync<int>(query, new { customerTypeVO.Name, customerTypeVO.UserId });
            result.Status = true;
            result.Data = insertedId;
            return result;
        }
        public async Task<ResultObject> Delete(int customerTypeId)
        {
            using var connection = new NpgsqlConnection(_configuration.GetConnectionString("PostgresConnection"));
            string query = @"DELETE FROM ""CustomerTypes"" WHERE id = @Id";

            try
            {
                int rowsAffected = await connection.ExecuteAsync(query, new { Id = customerTypeId });

                if (rowsAffected == 0)
                {
                    return new ResultObject
                    {
                        Status = false,
                        Message = $"No se encontró CustomerType con ID {customerTypeId} para eliminar."
                    };
                }

                return new ResultObject
                {
                    Status = true,
                    Message = "CustomerType eliminado exitosamente."
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = ex.Message
                };
            }
        }
        public async Task<ResultObject> GetCustomerTypresPaginated(int Page, int RowsPerPage)
        {
            ResultObject result = new();
            using var connection = new NpgsqlConnection(_configuration.GetConnectionString("PostgresConnection"));
            int totalRegistros = 0;
            string query = @"SELECT
                                COUNT(*)
                            FROM ""CustomerTypes"";

                            SELECT * FROM ""CustomerTypes""
                            ORDER BY ""Id""
                            OFFSET @Offset LIMIT @RowsPerPage;
                            ";

            var reader = await connection.QueryMultipleAsync(query, new { Offset = (Page - 1) * RowsPerPage, RowsPerPage });

            totalRegistros = reader.Read<int>().FirstOrDefault();
            List<ListCustomerTypeVO> resultCustomers = reader.Read<ListCustomerTypeVO>().ToList();
            result.Data = Paginado.CargarPaginado(resultCustomers, RowsPerPage, totalRegistros, Page);
            result.Status = true;
            return result;
        }
        public async Task<ResultObject> GetCustomerTypeById(int Id)
        {
            ResultObject result = new();
            using var connection = new NpgsqlConnection(_configuration.GetConnectionString("PostgresConnection"));
            string query = @"SELECT * FROM ""CustomerTypes"" WHERE ""Id"" = @Id ";
            result.Data = await connection.QueryAsync<ListCustomerTypeVO>(query, new { Id });
            result.Status = true;
            return result;
        }
        public async Task<ResultObject> Update(UpdateCustomerTypeVO customerTypeVO)
        {
            ResultObject result = new();
            using var connection = new NpgsqlConnection(_configuration.GetConnectionString("PostgresConnection"));
            string query = @"UPDATE ""CustomerTypes"" SET ""Name"", ""UserId"" = @Name WHERE ""Id"" = @Id";
            result.Data = await connection.ExecuteAsync(query, new
            {
                customerTypeVO.Name,
                customerTypeVO.UserId
            });
            result.Status = true;
            return result;
        }
    }
}
