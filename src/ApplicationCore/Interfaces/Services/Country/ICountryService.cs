using ApplicationCore.ValueObjects.Country;
using ApplicationCore.ValueObjects.Result;

namespace ApplicationCore.Interfaces.Services.Country
{
    public interface ICountryService
    {
        public Task<ResultObject> GetById(int id);
        
        public Task<ResultObject> GetAll();
        
        public Task<ResultObject> Create(CreateCountryVO countryVO);

        public Task<ResultObject> Update(UpdateCountryVO countryVO);

        public Task<ResultObject> Delete(int id);
    }
}
