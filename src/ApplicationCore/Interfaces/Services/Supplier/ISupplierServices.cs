using ApplicationCore.ValueObjects.Result;
using ApplicationCore.ValueObjects.Supplier;

namespace ApplicationCore.Interfaces.Services.Supplier
{
    public interface ISupplierService
    {
        public Task<ResultObject> GetById(int id);
        
        public Task<ResultObject> GetAll();

        public Task<ResultObject> GetSuppliersPaginated(int Page, int RowsPerPage);

        public Task<ResultObject> Create(CreateSupplierVO supplierVO);
        
        public Task<ResultObject> Update(UpdateSupplierVO supplierVO);
        
        public Task<ResultObject> Delete(int id);
    }

}
