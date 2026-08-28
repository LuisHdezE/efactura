using ApplicationCore.ValueObjects.TaxType;

namespace ApplicationCore.Interfaces.Repositories.TaxType
{
    public interface ITaxTypeRepository
    {
        public Task<GetTaxTypeVO> GetById(int id);

        public Task<IEnumerable<ListTaxTypeVO>> GetAll();

        public Task Create(CreateTaxTypeVO taxType);

        public Task Update(UpdateTaxTypeVO taxType);

        public Task Delete(int id);
    }

}
