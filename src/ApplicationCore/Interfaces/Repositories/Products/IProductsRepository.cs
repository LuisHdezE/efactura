
using ApplicationCore.ValueObjects.Products;
using ApplicationCore.ValueObjects.Result;

namespace ApplicationCore.Interfaces.Repositories.Products
{
    public interface IProductsRepository
    {
        Task<ResultObject> GetProductsPaginated(int page, int RowsPerPage);

        Task<ResultObject> GetProductById(int customerTypeId);

        Task<ResultObject> Create(CreateProductVO customerTypeVO);

        Task<ResultObject> Update(UpdateProductVO customerTypeVO);

        Task<ResultObject> Delete(int customerTypeId, int deletedBy);
    }
}
