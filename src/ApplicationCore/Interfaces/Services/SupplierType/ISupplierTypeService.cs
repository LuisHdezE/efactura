using ApplicationCore.ValueObjects.Result;
using ApplicationCore.ValueObjects.SupplierType;

namespace ApplicationCore.Interfaces.Services.SupplierType
{
    public interface ISupplierTypeService
    {
        public Task<ResultObject> GetByIdAsync(int id);

        public Task<ResultObject> GetAll();

        public Task<ResultObject> Create(CreateSupplierTypeVO supplierTypeVO);

        public Task<ResultObject> Update(UpdateSupplierTypeVO supplierTypeVO);

        public Task<ResultObject> Delete(int id);
    }

}
