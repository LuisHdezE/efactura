using ApplicationCore.ValueObjects.Country;

namespace ApplicationCore.Interfaces.Repositories.Country
{
    public interface ICountryRepository 
    {
        Task<GetCountryByIdVO> GetById(int id);

        Task<IEnumerable<ListCountryVO>> GetAll();

        Task Create(CreateCountryVO countryVO);

        Task Update(UpdateCountryVO countryVO);

        Task Delete(int id);
    }
}
