using ApplicationCore.ValueObjects.VoucherType;

namespace ApplicationCore.Interfaces.Repositories.VoucherType
{
    public interface IVoucherTypeRepository
    {
        public Task<GetVoucherTypeVO> GetById(int id);
       
        public Task<IEnumerable<ListVoucherTypeVO>> GetAll();
        
        public Task Create(CreateVoucherTypeVO voucherTypeeVO);
        
        public Task Update(UpdateVaucherTypeVO voucherTypeVO);
        
        public Task Delete(int id);
    }
}
