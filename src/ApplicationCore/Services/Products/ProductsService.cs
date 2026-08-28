using ApplicationCore.Entites;
using ApplicationCore.Interfaces.Repositories.Cache;
using ApplicationCore.Interfaces.Repositories.Products;
using ApplicationCore.ValueObjects.Products;
using ApplicationCore.ValueObjects.Result;

namespace ApplicationCore.Services.CustomerType
{
    public class ProductsService : IProductsService
    {
        private readonly IProductsRepository _productRepository;
        private readonly ICacheService _cacheService;

        public ProductsService(IProductsRepository productRepository, ICacheService cache)
        {
            _productRepository = productRepository;
            _cacheService = cache;
        }

        public async Task<ResultObject> Create(CreateProductVO productVO)
        {
            return await _productRepository.Create(productVO);
        }

        public async Task<ResultObject> Delete(int customerTypeId, int deletedBy)
        {
            return await _productRepository.Delete(customerTypeId, deletedBy);
        }

        public async Task<ResultObject> GetProductsPaginated(int Page, int RowsPerPage)
        {            
            return await _productRepository.GetProductsPaginated(Page, RowsPerPage);
        }

        public async Task<ResultObject> GetProductById(int productId)
        {
            return await _productRepository.GetProductById(productId);
        }

        public async Task<ResultObject> Update(UpdateProductVO productVO)
        {
            return await _productRepository.Update(productVO);
        }

    }
}
