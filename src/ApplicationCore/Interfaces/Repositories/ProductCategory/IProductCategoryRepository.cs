using ApplicationCore.ValueObjects.ProductCategory;

namespace ApplicationCore.Interfaces.Repositories.ProductCategory
{
    public interface IProductCategoryRepository
    {
        public Task<GetProductCategoryVO> GetById(int id);

        public Task<IEnumerable<ListProductCategoryVO>> GetAll();

        public Task Create(CreateProductCategoryVO productCategoryVO);

        public Task Update(UpdateProductCategoryVO productCategoryVO);

        public Task Delete(int id);
    }

}
