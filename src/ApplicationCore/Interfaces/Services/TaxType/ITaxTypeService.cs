using ApplicationCore.ValueObjects.Result;
using ApplicationCore.ValueObjects.TaxType;

namespace ApplicationCore.Interfaces.Services.TaxType
{
    public interface ITaxTypeService
    {
        public Task<ResultObject> GetByIdAsync(int id);

        public Task<ResultObject> GetAll();

        public Task<ResultObject> Create(CreateTaxTypeVO taxTypeVO);

        public Task<ResultObject> Update(UpdateTaxTypeVO taxTypeVO);

        public Task<ResultObject> Delete(int id);
    }

}
