using ApplicationCore.Interfaces.Services.Supplier;
using ApplicationCore.ValueObjects.Result;
using ApplicationCore.ValueObjects.Supplier;
using ApplicationCore.ValueObjects.SupplierType;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SupplierController : ControllerBase
    {
        private readonly ISupplierService _supplierService;

        public SupplierController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        [HttpGet]
        public async Task<ActionResult<ResultObject>> GetAll()
        {
            var results = await _supplierService.GetAll();
            return results.Status ? Ok(results) : BadRequest(results);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ResultObject>> GetById(int id)
        {
            var result = await _supplierService.GetById(id);
            return result.Status ? Ok(result) : BadRequest(result);

        }

        [HttpPost]
        public async Task<ActionResult<ResultObject>> Create([FromBody] CreateSupplierVO supplierVO)
        {
            var result = await _supplierService.Create(supplierVO);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ResultObject>> Update(int id, [FromBody] UpdateSupplierVO supplierVO)
        {
            if (id != supplierVO.Id) return BadRequest();
            var result = await _supplierService.Update(supplierVO);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ResultObject>> Delete(int id)
        {
            var result = await _supplierService.Delete(id);
            return result.Status ? Ok(result) : BadRequest(result);
        }
    }
}
