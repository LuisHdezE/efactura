using ApplicationCore.Interfaces.Services.ContactType;
using ApplicationCore.ValueObjects.ContactType;
using ApplicationCore.ValueObjects.Result;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactTypeController: ControllerBase
    {
        private readonly IContactTypeService _contactTypeService;

        public ContactTypeController(IContactTypeService contactTypeService)
        {
            _contactTypeService = contactTypeService;
        }

        [HttpGet]
        public async Task<ActionResult<ResultObject>> GetAll()
        {
            var results = await _contactTypeService.GetAll();
            return results.Status ? Ok(results) : BadRequest(results);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ResultObject>> GetById(int id)
        {
            var result = await _contactTypeService.GetById(id);
            return result.Status ? Ok(result) : BadRequest(result);

        }

        [HttpPost]
        public async Task<ActionResult<ResultObject>> Create([FromBody] CreateContactTypeVO contactTypeVO)
        {
            var result = await _contactTypeService.Create(contactTypeVO);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ResultObject>> Update(int id, [FromBody] UpdateContactTypeVO documentTypeyVO)
        {
            if (id != documentTypeyVO.Id) return BadRequest();
            var result = await _contactTypeService.Update(documentTypeyVO);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ResultObject>> Delete(int id)
        {
            var result = await _contactTypeService.Delete(id);
            return result.Status ? Ok(result) : BadRequest(result);
        }

    }
}
