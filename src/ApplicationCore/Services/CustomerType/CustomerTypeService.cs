using ApplicationCore.Interfaces.Repositories.Cache;
using ApplicationCore.Interfaces.Repositories.CustomerType;
using ApplicationCore.Interfaces.Services.CustomerType;
using ApplicationCore.ValueObjects.CustomerType;
using ApplicationCore.ValueObjects.Result;

namespace ApplicationCore.Services.CustomerType
{
    public class CustomerTypeService : ICustomerTypeService
    {
        private readonly ICustomerTypeRepository _customerTypeRepository;
        private readonly ICacheService _cacheService;

        // Constructor con inyección de dependencias
        public CustomerTypeService(ICustomerTypeRepository customerTypeRepository, ICacheService cache)
        {
            _customerTypeRepository = customerTypeRepository;
            _cacheService = cache;
        }

        public async Task<ResultObject> Create(CreateCustomerTypeVO customerTypeVO)
        {
            return await _customerTypeRepository.Create(customerTypeVO);
        }

        public async Task<ResultObject> Delete(int customerTypeId)
        {
            return await _customerTypeRepository.Delete(customerTypeId);
        }

        public async Task<ResultObject> GetCustomerTypresPaginated(int Page, int RowsPerPage)
        {            
            return await _customerTypeRepository.GetCustomerTypresPaginated(Page, RowsPerPage);
        }

        public async Task<ResultObject> GetCustomerTypeById(int customerTypeId)
        {
            return await _customerTypeRepository.GetCustomerTypeById(customerTypeId);
        }

        public async Task<ResultObject> Update(UpdateCustomerTypeVO customerTypeVO)
        {
            return await _customerTypeRepository.Update(customerTypeVO);
        }
    }
}
