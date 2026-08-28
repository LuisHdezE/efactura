using ApplicationCore.ValueObjects.SupplierType;

namespace ApplicationCore.Interfaces.Repositories.SupplierType
{
    public interface ISupplierTypeRepository
    {
        public Task<GetSupplierTypeVO> GetById(int id);

        public Task<IEnumerable<ListSupplierTypeVO>> GetAll();

        public Task Create(CreateSupplierTypeVO supplierType);

        public Task Update(UpdateSupplierTypeVO supplierType);

        public Task Delete(int id);
    }

}
