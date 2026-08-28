using ApplicationCore.ValueObjects.Result;
using ApplicationCore.ValueObjects.VoucherType;

namespace ApplicationCore.Interfaces.Services.VoucherType
{
    public interface IVoucherTypeService
    {
        public Task<ResultObject> GetById(int id);

        public Task<ResultObject> GetAll();

        public Task<ResultObject> Create(CreateVoucherTypeVO voucherType);

        public Task<ResultObject> Update(UpdateVaucherTypeVO voucherType);

        public Task<ResultObject> Delete(int id);
    }
}
