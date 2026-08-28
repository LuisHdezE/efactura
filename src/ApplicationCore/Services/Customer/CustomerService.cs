using ApplicationCore.Interfaces.Repositories.Cache;
using ApplicationCore.Interfaces.Repositories.Customer;
using ApplicationCore.Interfaces.Services.Customer;
using ApplicationCore.ValueObjects.Customer;
using ApplicationCore.ValueObjects.Result;

namespace ApplicationCore.Services.Customer
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepository;
        private readonly ICacheService _cacheService;

        // Constructor con inyección de dependencias
        public CustomerService(ICustomerRepository customerRepository, ICacheService cache)
        {
            _customerRepository = customerRepository;
            _cacheService = cache;
        }

        public async Task<ResultObject> Create(CreateCustomerVO customerVO)
        {
            return await _customerRepository.Create(customerVO);
        }

        public async Task<ResultObject> Delete(DeleteCustomerVO customervo)
        {
            return await _customerRepository.Delete(customervo);
        }

        public async Task<ResultObject> GetCustomerPaginated(int Page, int RowsPerPage)
        {            
            return await _customerRepository.GetCustomerPaginated(Page, RowsPerPage);
        }

        public async Task<ResultObject> GetCustomerById(int customerId)
        {
            return await _customerRepository.GetCustomerById(customerId);
        }

        public async Task<ResultObject> Update(UpdateCustomerVO customerTypeVO)
        {
            return await _customerRepository.Update(customerTypeVO);
        }
    }
}
