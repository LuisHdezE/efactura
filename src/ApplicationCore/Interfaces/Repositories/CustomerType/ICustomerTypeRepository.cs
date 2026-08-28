
using ApplicationCore.ValueObjects.CustomerType;
using ApplicationCore.ValueObjects.Result;

namespace ApplicationCore.Interfaces.Repositories.CustomerType
{
    public interface ICustomerTypeRepository
    {
        Task<ResultObject> GetCustomerTypresPaginated(int page, int RowsPerPage);

        Task<ResultObject> GetCustomerTypeById(int customerTypeId);

        Task<ResultObject> Create(CreateCustomerTypeVO customerTypeVO);

        Task<ResultObject> Update(UpdateCustomerTypeVO customerTypeVO);

        Task<ResultObject> Delete(int customerTypeId);
    }
}
