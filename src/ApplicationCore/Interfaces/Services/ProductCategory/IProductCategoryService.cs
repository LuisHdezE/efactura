using ApplicationCore.ValueObjects.ProductCategory;
using ApplicationCore.ValueObjects.Result;

namespace ApplicationCore.Interfaces.Services.ProductCategory
{
    public interface IProductCategoryService
    {
        public Task<ResultObject> GetById(int id);

        public Task<ResultObject> GetAll();

        public Task<ResultObject> Create(CreateProductCategoryVO productCategoryVO);

        public Task<ResultObject> Update(UpdateProductCategoryVO productCategoryVO);

        public Task<ResultObject> Delete(int id);
    }

}
