using ApplicationCore.ValueObjects.Customer;
using ApplicationCore.ValueObjects.Result;
namespace ApplicationCore.Interfaces.Services.Customer
{
    public interface ICustomerService
    {
        Task<ResultObject> GetCustomerPaginated(int page, int RowsPerPage);

        Task<ResultObject> GetCustomerById(int customerId);

        Task<ResultObject> Create(CreateCustomerVO customerVO);

        Task<ResultObject> Update(UpdateCustomerVO customerVO);

        Task<ResultObject> Delete(DeleteCustomerVO customerVO);


    }
}
