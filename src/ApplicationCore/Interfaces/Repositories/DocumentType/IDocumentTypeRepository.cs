using ApplicationCore.ValueObjects.DocumentType;

namespace ApplicationCore.Interfaces.Repositories.DocumentType
{
    public interface IDocumentTypeRepository
    {
        Task<GetDocumentTypeVO> GetById(int id);
        
        Task<IEnumerable<ListDocumentTypeVO>> GetAll();
        
        Task Create(CreateDocumentTypeVO documentTypeVO);
        
        Task Update(UpdateDocumentTypeVO documentTypeVO);
        
        Task Delete(int id);
    }
}
