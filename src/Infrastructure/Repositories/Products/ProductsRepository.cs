using ApplicationCore.Interfaces.Repositories.Products;
using ApplicationCore.Utilities.Paginado;
using ApplicationCore.ValueObjects.CustomerType;
using ApplicationCore.ValueObjects.Products;
using ApplicationCore.ValueObjects.Result;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Polly.Caching;
using System.Data;

namespace Infrastructure.Repositories.Products
{
    public class ProductsRepository(IConfiguration configuration) : IProductsRepository
    {
        private readonly IConfiguration _configuration = configuration;
        public async Task<ResultObject> Create(CreateProductVO productVO)
        {
            ResultObject result = new();
            using var connection = new NpgsqlConnection(_configuration.GetConnectionString("PostgresConnection"));

            string storedProcedure = "public.sp_insert_product";
            var parameters = new
            {
                p_name = productVO.Name,
                p_description = productVO.Description,
                p_price = productVO.Price,
                p_stock = productVO.Stock,
                p_product_category_id = productVO.ProductCategoryId,
                p_created_by = productVO.CreatedBy
            };

            var insertedId = await connection.ExecuteScalarAsync<int>(
                storedProcedure,
                parameters,
                commandType: CommandType.StoredProcedure
            );

            result.Status = true;
            result.Data = insertedId;
            return result;
        }
        public async Task<ResultObject> Update(UpdateProductVO productVO)
        {
            ResultObject result = new();
            using var connection = new NpgsqlConnection(_configuration.GetConnectionString("PostgresConnection"));

            string storedProcedure = "public.sp_update_product";
            var parameters = new
            {
                p_id = productVO.Id,
                p_name = productVO.Name,
                p_description = productVO.Description,
                p_price = productVO.Price,
                p_stock = productVO.Stock,
                p_product_category_id = productVO.ProductCategoryId,
                p_updated_by = productVO.UpdatedBy
            };

            await connection.ExecuteAsync(
                storedProcedure,
                parameters,
                commandType: CommandType.StoredProcedure
            );

            result.Status = true;
            result.Message = "Producto actualizado exitosamente.";
            return result;
        }
        public async Task<ResultObject> Delete(int productId, int deletedBy)
        {
            ResultObject result = new();
            using var connection = new NpgsqlConnection(_configuration.GetConnectionString("PostgresConnection"));

            string storedProcedure = "public.delete_product";
            var parameters = new
            {
                p_id = productId,
                p_deleted_by = deletedBy
            };

            await connection.ExecuteAsync(
                storedProcedure,
                parameters,
                commandType: CommandType.StoredProcedure
            );

            result.Status = true;
            result.Message = "Producto eliminado lógicamente.";
            return result;
        }


        public async Task<ResultObject> GetProductsPaginated(int Page, int RowsPerPage)
        {
            ResultObject result = new();
            using var connection = new NpgsqlConnection(_configuration.GetConnectionString("PostgresConnection"));
            await connection.OpenAsync();

            try
            {
                // Definir la consulta SQL para llamar a la función
                string sql = @"SELECT * FROM public.fn_get_products_paginated(@p_page, @p_rows_per_page);";

                // Configurar los parámetros
                var parameters = new
                {
                    p_page = Page,
                    p_rows_per_page = RowsPerPage
                };

                // Ejecutar la consulta y obtener los resultados
                var products = await connection.QueryAsync<ProductWithTotalRecordsVO>(
                    sql,
                    parameters
                );

                // Obtener el total de registros del primer elemento
                int totalRegistros = products.FirstOrDefault()?.TotalRecords ?? 0;

                // Mapear los productos a la clase ListProductsVO (si es necesario)
                var productList = products.Select(p => new ListProductsVO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Stock = p.Stock,
                    ProductCategoryId = p.ProductCategoryId,
                    CreatedAt = p.CreatedAt,
                    CreatedBy = p.CreatedBy,
                    UpdatedAt = p.UpdatedAt,
                    UpdatedBy = p.UpdatedBy,
                    DeletedAt = p.DeletedAt,
                    DeletedBy = p.DeletedBy
                }).ToList();

                // Preparar el resultado paginado
                result.Data = Paginado.CargarPaginado(productList, RowsPerPage, totalRegistros, Page);
                result.Status = true;
                return result;
            }
            catch (Exception ex)
            {
                result.Status = false;
                result.Message = ex.Message;
                return result;
            }
        }



        public async Task<ResultObject> GetProductById(int Id)
        {
            ResultObject result = new();
            using var connection = new NpgsqlConnection(_configuration.GetConnectionString("PostgresConnection"));
            string query = @"SELECT * FROM ""CustomerTypes"" WHERE ""Id"" = @Id ";
            result.Data = await connection.QueryAsync<ListCustomerTypeVO>(query, new { Id });
            result.Status = true;
            return result;
        }
    }
}
