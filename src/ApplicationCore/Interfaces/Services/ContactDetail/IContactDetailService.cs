using ApplicationCore.ValueObjects.ContactDetail;
using ApplicationCore.ValueObjects.Result;

namespace ApplicationCore.Interfaces.Services.ContactDetail
{
    public interface IContactDetailService
    {


        public Task<ResultObject> GetById(int id);

        public Task<ResultObject> GetAll();

        public Task<ResultObject> Create(CreateContactDetailVO contactDetailVO);

        public Task<ResultObject> Update(UpdateContactDetailVO contactDetailVO);

        public Task<ResultObject> Delete(int id);

        public Task<ResultObject> GetByCustomerIdAsync(int customerId);

        public Task<ResultObject> GetByCustomerIdAndContactTypeId(long customerId, long contactTypeId);
    }

}
