using ApplicationCore.ValueObjects.ContactType;
using ApplicationCore.ValueObjects.Result;

namespace ApplicationCore.Interfaces.Services.ContactType
{
    public interface IContactTypeService
    {
        public Task<ResultObject> GetById(int id);

        public Task<ResultObject> GetAll();

        public Task<ResultObject> Create(CreateContactTypeVO contactType);

        public Task<ResultObject> Update(UpdateContactTypeVO documentType);

        public Task<ResultObject> Delete(int id);
    }
}
