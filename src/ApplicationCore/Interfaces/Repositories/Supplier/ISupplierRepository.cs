using ApplicationCore.ValueObjects.Result;
using ApplicationCore.ValueObjects.Supplier;

namespace ApplicationCore.Interfaces.Repositories.Supplier
{
    public interface ISupplierRepository 
    {
        public Task<GetSupplierVO> GetById(int id);
        
        public Task<IEnumerable<ListSupplierVO>> GetAll();

        public Task<ResultObject> GetSuppliersPaginated(int Page, int RowsPerPage);

        public Task Create(CreateSupplierVO supplierVO);
        
        public Task Update(UpdateSupplierVO supplierVO);
        
        public Task Delete(int id);
    }

}
