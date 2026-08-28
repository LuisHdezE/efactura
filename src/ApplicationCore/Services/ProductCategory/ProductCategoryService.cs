using ApplicationCore.Entities;
using ApplicationCore.Interfaces.Repositories.ContactTypeRepository;
using ApplicationCore.Interfaces.Repositories.ProductCategory;
using ApplicationCore.Interfaces.Services.ProductCategory;
using ApplicationCore.ValueObjects.ContactType;
using ApplicationCore.ValueObjects.ProductCategory;
using ApplicationCore.ValueObjects.Result;
using AutoMapper;

namespace ApplicationCore.Services.ProductCategory
{
    public class ProductCategoryService : IProductCategoryService
    {
        private readonly IMapper _mapper;

        private readonly IProductCategoryRepository _productCategoryRepository;

        public ProductCategoryService(IProductCategoryRepository productCategoryRepository, IMapper mapper)
        {
            _productCategoryRepository = productCategoryRepository;
            _mapper = mapper;
        }

        public async Task<ResultObject> Create(CreateProductCategoryVO productCategoryVO)
        {
            try
            {
                await _productCategoryRepository.Create(productCategoryVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Product Category created successfully",
                    Data = productCategoryVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error creating Product Category",
                    Detail = ex.Message,
                    ErrorCode = "CREATE_PRODUCT_CATEGORY_ERROR"
                };
            }
        }

        public async Task<ResultObject> Delete(int id)
        {
            try
            {
                await _productCategoryRepository.Delete(id);

                return new ResultObject
                {
                    Status = true,
                    Message = "Product Category deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error deleting Product Category",
                    Detail = ex.Message,
                    ErrorCode = "DELETE_PRODUCT_CATEGORY_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetAll()
        {
            try
            {
                var productCategories = await _productCategoryRepository.GetAll();
                var productCategoriesVO = _mapper.Map<IEnumerable<ListProductCategoryVO>>(productCategories);

                return new ResultObject
                {
                    Status = true,
                    Message = "Product Category retrieved successfully",
                    Data = productCategoriesVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Product Category",
                    Detail = ex.Message,
                    ErrorCode = "GET_PRODUCT_CATEGORY_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetById(int id)
        {
            try
            {
                var productCategory = await _productCategoryRepository.GetById(id);
                var productCategoryVO = _mapper.Map<GetProductCategoryVO>(productCategory);

                return new ResultObject
                {
                    Status = true,
                    Message = "Product Category retrieved successfully",
                    Data = productCategoryVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Product Category",
                    Detail = ex.Message,
                    ErrorCode = "GET_PRODUCT_CATEGORY_ERROR"
                };
            }
        }

        public async Task<ResultObject> Update(UpdateProductCategoryVO productCategoryVO)
        {
            try
            {
                var productCategory = _mapper.Map<ContactType>(productCategoryVO);
                await _productCategoryRepository.Update(productCategoryVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Product Category updated successfully",
                    Data = productCategory
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error updating Product Category",
                    Detail = ex.Message,
                    ErrorCode = "UPDATE_PRODUCT_CATEGORY_ERROR"
                };
            }
        }
    }
}
