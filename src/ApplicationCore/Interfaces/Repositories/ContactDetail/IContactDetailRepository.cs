
using ApplicationCore.ValueObjects.ContactDetail;

namespace ApplicationCore.Interfaces.Repositories.ContactDetail
{
    public interface IContactDetailRepository
    {
        Task<GetContactDetailVO> GetById(int id);
        
        Task Create(CreateContactDetailVO contactDetail);
        
        Task Update(UpdateContactDetailVO contactDetail);
        
        Task Delete(int id);

        Task<IEnumerable<ListContactDetailVO>> GetAll();
        
        Task<IEnumerable<GetContactDetailVO>> GetByCustomerId(long customerId);
        
        Task<IEnumerable<GetContactDetailVO>> GetByContactTypeId(long contactTypeId);
        
        Task<GetContactDetailVO> GetByCustomerIdAndContactTypeId(long customerId, long contactTypeId);
    }

}
