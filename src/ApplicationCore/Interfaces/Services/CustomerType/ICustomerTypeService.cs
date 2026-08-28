using ApplicationCore.ValueObjects.CustomerType;
using ApplicationCore.ValueObjects.Result;
namespace ApplicationCore.Interfaces.Services.CustomerType
{
    public interface ICustomerTypeService
    {
        Task<ResultObject> GetCustomerTypresPaginated(int page, int RowsPerPage);

        Task<ResultObject> GetCustomerTypeById(int customerTypeId);

        Task<ResultObject> Create(CreateCustomerTypeVO customerTypeVO);

        Task<ResultObject> Update(UpdateCustomerTypeVO customerTypeVO);

        Task<ResultObject> Delete(int customerTypeId);
    }
}
