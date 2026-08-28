using ApplicationCore.Entities;
using ApplicationCore.Interfaces.Repositories.Country;
using ApplicationCore.Interfaces.Services.Country;
using ApplicationCore.ValueObjects.Country;
using ApplicationCore.ValueObjects.Result;
using AutoMapper;

namespace ApplicationCore.Services.CountryService
{
    public class CountryService : ICountryService
    {
        private readonly ICountryRepository _countryRepository;
        private readonly IMapper _mapper;

        private const string TableName = "Countries";
        private const string IdColumn = "Id";

        public CountryService(ICountryRepository countryRepository, IMapper mapper)
        {
            _countryRepository = countryRepository;
            _mapper = mapper;
        }

        public async Task<ResultObject> GetById(int id)
        {
            try
            {
                var country = await _countryRepository.GetById(id);
                var countryVO = _mapper.Map<GetCountryByIdVO>(country);
                var result= new ResultObject
                {
                    Status = true,
                    Message = "Country retrieved successfully",
                    Data = countryVO
                };
                return result;
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving country",
                    Detail = ex.Message,
                    ErrorCode = "GET_COUNTRY_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetAll()
        {
            try
            {
                var countries = await _countryRepository.GetAll();
                var countriesVO = _mapper.Map<IEnumerable<ListCountryVO>>(countries);

                return new ResultObject
                {
                    Status = true,
                    Message = "Countries retrieved successfully",
                    Data = countriesVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving countries",
                    Detail = ex.Message,
                    ErrorCode = "GET_COUNTRIES_ERROR"
                };
            }
        }

        public async Task<ResultObject> Create(CreateCountryVO countryVO)
        {
            try
            {
                await _countryRepository.Create(countryVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Country created successfully",
                    Data = countryVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error creating country",
                    Detail = ex.Message,
                    ErrorCode = "CREATE_COUNTRY_ERROR"
                };
            }
        }

        public async Task<ResultObject> Update(UpdateCountryVO countryVO)
        {
            
            try
            {
                //var country = _mapper.Map<Country>(countryVO);

                await _countryRepository.Update(countryVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Country updated successfully",
                    Data = countryVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error updating country",
                    Detail = ex.Message,
                    ErrorCode = "UPDATE_COUNTRY_ERROR"
                };
            }
        }

        public async Task<ResultObject> Delete(int id)
        {
            try
            {
                await _countryRepository.Delete(id);

                return new ResultObject
                {
                    Status = true,
                    Message = "Country deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error deleting country",
                    Detail = ex.Message,
                    ErrorCode = "DELETE_COUNTRY_ERROR"
                };
            }
        }

        
    }
}
