using ApplicationCore.Interfaces.Services.VoucherType;
using ApplicationCore.ValueObjects.Result;
using ApplicationCore.ValueObjects.VoucherType;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VoucherTypeController : ControllerBase
    {
        private readonly IVoucherTypeService _voucherTypeService;

        public VoucherTypeController(IVoucherTypeService voucherTypeService)
        {
            _voucherTypeService = voucherTypeService;
        }

        [HttpGet]
        public async Task<ActionResult<ResultObject>> GetAll()
        {
            var results = await _voucherTypeService.GetAll();
            return results.Status ? Ok(results) : BadRequest(results);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ResultObject>> GetById(int id)
        {
            var result = await _voucherTypeService.GetById(id);
            return result.Status ? Ok(result) : BadRequest(result);

        }

        [HttpPost]
        public async Task<ActionResult<ResultObject>> Create([FromBody] CreateVoucherTypeVO voucherTypeVO)
        {
            var result = await _voucherTypeService.Create(voucherTypeVO);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ResultObject>> Update(int id, [FromBody] UpdateVaucherTypeVO voucherTypeyVO)
        {
            if (id != voucherTypeyVO.Id) return BadRequest();
            var result = await _voucherTypeService.Update(voucherTypeyVO);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ResultObject>> Delete(int id)
        {
            var result = await _voucherTypeService.Delete(id);
            return result.Status ? Ok(result) : BadRequest(result);
        }
    }
}
