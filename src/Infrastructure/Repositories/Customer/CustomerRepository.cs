using ApplicationCore.Interfaces.Repositories.Customer;
using ApplicationCore.Utilities.Paginado;
using ApplicationCore.ValueObjects.Customer;
using ApplicationCore.ValueObjects.Result;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Polly.Caching;

namespace Infrastructure.Repositories.Customer
{
    public class CustomerRepository(IConfiguration configuration) : ICustomerRepository
    {
        private readonly IConfiguration _configuration = configuration;

        

        public async Task<ResultObject> Create(CreateCustomerVO customerVO)
        {
            ResultObject result = new();
            using var connection = new NpgsqlConnection(_configuration.GetConnectionString("PostgresConnection"));

            string query = @"
        INSERT INTO ""Customers"" (
            ""Name"",
            ""Email"",
            ""Phone"",
            ""Address"",
            ""CustomerTypeId"",
            ""CreatedAt"",
            ""CreatedBy""
        ) 
        VALUES (
            @Name,
            @Email,
            @Phone,
            @Address,
            @CustomerTypeId,
            @CreatedAt,
            @CreatedBy
        ) 
        RETURNING ""Id""";

            try
            {
                var insertedId = await connection.ExecuteScalarAsync<long>(query, new
                {
                    customerVO.Name,
                    customerVO.Email,
                    customerVO.Phone,
                    customerVO.Address,
                    customerVO.CustomerTypeId,
                    customerVO.CreatedAt,
                    customerVO.CreatedBy
                });

                result.Status = true;
                result.Data = insertedId;
                return result;
            }
            catch (Exception ex)
            {
                result.Status = false;
                result.Message = ex.Message;
                return result;
            }
        }
        public async Task<ResultObject> Delete(DeleteCustomerVO customervo)
        {
            using var connection = new NpgsqlConnection(_configuration.GetConnectionString("PostgresConnection"));

            // En lugar de eliminar, actualizamos los campos DeletedAt y DeletedBy
            string query = @"
        UPDATE ""Customers"" 
        SET ""DeletedAt"" = @DeletedAt, ""DeletedBy"" = @DeletedBy 
        WHERE ""Id"" = @Id";

            try
            {
                // Ejecutar el comando de actualización
                int rowsAffected = await connection.ExecuteAsync(query, new
                {
                    Id = customervo.Id,
                    DeletedBy = customervo.DeletedBy,
                    DeletedAt = DateTime.UtcNow  // Marca el momento de eliminación
                });

                if (rowsAffected == 0)
                {
                    return new ResultObject
                    {
                        Status = false,
                        Message = $"No se encontró Customer con ID {customervo.Id} para eliminar."
                    };
                }

                return new ResultObject
                {
                    Status = true,
                    Message = "Customer eliminado (lógicamente) exitosamente."
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


        public async Task<ResultObject> GetCustomerPaginated(int Page, int RowsPerPage)
        {
            ResultObject result = new();
            using var connection = new NpgsqlConnection(_configuration.GetConnectionString("PostgresConnection"));
            int totalRegistros = 0;
            string query = @"SELECT
                                COUNT(*)
                            FROM ""Customers""
                            WHERE ""DeletedAt"" IS NULL;

                            SELECT * FROM ""Customers""
                            WHERE ""DeletedAt"" IS NULL
                            ORDER BY ""Id""
                            OFFSET @Offset LIMIT @RowsPerPage;
                            ";

            var reader = await connection.QueryMultipleAsync(query, new { Offset = (Page - 1) * RowsPerPage, RowsPerPage });
            totalRegistros = reader.Read<int>().FirstOrDefault();
            List<ListCustomerVO> resultCustomers = reader.Read<ListCustomerVO>().ToList();
            result.Data = Paginado.CargarPaginado(resultCustomers, RowsPerPage, totalRegistros, Page);
            result.Status = true;
            return result;
        }

        public async Task<ResultObject> GetCustomerById(int Id)
        {
            ResultObject result = new();
            using var connection = new NpgsqlConnection(_configuration.GetConnectionString("PostgresConnection"));
            string query = @"SELECT * FROM ""Customers"" WHERE ""Id"" = @Id ";
            result.Data = await connection.QueryAsync<ListCustomerVO>(query, new { Id });
            result.Status = true;
            return result;
        }

        public async Task<ResultObject> Update(UpdateCustomerVO customerVO)
        {
            using var connection = new NpgsqlConnection(_configuration.GetConnectionString("PostgresConnection"));
            string query = @"
        UPDATE ""Customers"" 
        SET 
            ""Name"" = @Name,
            ""Email"" = @Email,
            ""Phone"" = @Phone,
            ""Address"" = @Address,
            ""CustomerTypeId"" = @CustomerTypeId,
            ""UpdatedBy"" = @UpdatedBy,
            ""UpdatedAt"" = @UpdatedAt
            WHERE ""Id"" = @Id";

            try
            {
                // Ejecuta la consulta con los parámetros proporcionados en customerVO
                int rowsAffected = await connection.ExecuteAsync(query, new
                {
                    customerVO.Name,
                    customerVO.Email,
                    customerVO.Phone,
                    customerVO.Address,
                    customerVO.CustomerTypeId,
                    customerVO.UpdatedBy,
                    customerVO.UpdatedAt,
                    customerVO.Id
                });

                if (rowsAffected == 0)
                {
                    return new ResultObject
                    {
                        Status = false,
                        Message = $"No se encontró el Customer con ID {customerVO.Id} para actualizar."
                    };
                }

                return new ResultObject
                {
                    Status = true,
                    Message = "Customer actualizado exitosamente."
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
    }
}
