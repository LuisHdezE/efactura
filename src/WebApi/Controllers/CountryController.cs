using ApplicationCore.Interfaces.Services.Country;
using ApplicationCore.ValueObjects.Country;
using ApplicationCore.ValueObjects.Result;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CountryController : ControllerBase
    {
        private readonly ICountryService _countryService;

        public CountryController(ICountryService countryService)
        {
            _countryService = countryService;
        }

        [HttpGet]
        public async Task<ActionResult<ResultObject>> GetAll()
        {
            var results = await _countryService.GetAll();
            return results.Status ? Ok(results) : BadRequest(results);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ResultObject>> GetById(int id)
        {
                var result = await _countryService.GetById(id);
                return result.Status ? Ok(result) : BadRequest(result);

        }

        [HttpPost]
        public async Task<ActionResult<ResultObject>> Create([FromBody] CreateCountryVO countryVO)
        {
            var result = await _countryService.Create(countryVO);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ResultObject>> Update(int id, [FromBody] UpdateCountryVO countryVO)
        {
            if (id != countryVO.Id) return BadRequest();
            var result = await _countryService.Update(countryVO);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ResultObject>> Delete(int id)
        {
            var result = await _countryService.Delete(id);
            return result.Status ? Ok(result) : BadRequest(result);
        }
    }

}
