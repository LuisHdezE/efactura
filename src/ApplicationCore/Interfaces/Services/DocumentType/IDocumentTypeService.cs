using ApplicationCore.ValueObjects.DocumentType;
using ApplicationCore.ValueObjects.Result;

namespace ApplicationCore.Interfaces.Services.DocumentType
{
    public interface IDocumentTypeService
    {
        public Task<ResultObject> GetById(int id);
        
        public Task<ResultObject> GetAll();
        
        public Task<ResultObject> Create(CreateDocumentTypeVO documentType);
        
        public Task<ResultObject> Update(UpdateDocumentTypeVO documentType);
        
        public Task<ResultObject> Delete(int id);
    }
}
