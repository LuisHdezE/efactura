using ApplicationCore.ValueObjects.ContactType;

namespace ApplicationCore.Interfaces.Repositories.ContactTypeRepository
{
    public interface IContactTypeRepository
    {
        Task<GetContactTypeVO> GetById(int id);
        
        Task<IEnumerable<ListContactTypeVO>> GetAll();
        
        Task Create(CreateContactTypeVO contactTypeeVO);
        
        Task Update(UpdateContactTypeVO contactTypeVO);
        
        Task Delete(int id);
    }
}
