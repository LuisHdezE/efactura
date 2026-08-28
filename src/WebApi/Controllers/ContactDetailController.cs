using ApplicationCore.Interfaces.Services.ContactDetail;
using ApplicationCore.ValueObjects.ContactDetail;
using ApplicationCore.ValueObjects.Result;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactDetailController : ControllerBase
    {
        private readonly IContactDetailService _contactDetailService;

        public ContactDetailController(IContactDetailService contactDetailService)
        {
            _contactDetailService = contactDetailService;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ResultObject>> GetById(int id)
        {
            var result = await _contactDetailService.GetById(id);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpGet]
        public async Task<ActionResult<ResultObject>> GetAll()
        {
            var results = await _contactDetailService.GetAll();
            return results.Status ? Ok(results) : BadRequest(results);
        }

        [HttpPost]
        public async Task<ActionResult<ResultObject>> Create([FromBody] CreateContactDetailVO contactDetailVO)
        {
            var result = await _contactDetailService.Create(contactDetailVO);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ResultObject>> Update(int id, [FromBody] UpdateContactDetailVO contactDetailVO)
        {
            if (contactDetailVO.Id != id)
                return BadRequest("ID mismatch.");

            var result = await _contactDetailService.Update(contactDetailVO);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ResultObject>> Delete(int id)
        {
            var result = await _contactDetailService.Delete(id);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpGet("by-customer/{customerId}")]
        public async Task<ActionResult<ResultObject>> GetByCustomerId(long customerId)
        {
            var result = await _contactDetailService.GetByCustomerIdAsync((int)customerId);
            return result.Status ? Ok(result) : BadRequest(result);
        }

    }
}
