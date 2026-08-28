using ApplicationCore.Interfaces.Services.CustomerType;
using ApplicationCore.ValueObjects.CustomerType;
using ApplicationCore.ValueObjects.Result;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerTypeController(ICustomerTypeService CustomerTypeService) : ControllerBase
    {
        private readonly ICustomerTypeService _customerTypeService = CustomerTypeService;

        [HttpGet("GetCustomerTypeById")]
        public async Task<ActionResult<ResultObject>> GetCustomerTypeById(int Id)
        {
            var result = await _customerTypeService.GetCustomerTypeById(Id);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpGet("GetCustomerTypresPaginated")]
        public async Task<ActionResult<ResultObject>> GetCustomerTypresPaginated(int Page = 1, int RowsPerPage = 10)
        {
            var result = await _customerTypeService.GetCustomerTypresPaginated(Page, RowsPerPage);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        public async Task<ActionResult<ResultObject>> Post([FromBody] CreateCustomerTypeVO customerTypeVo)
        {
            var result = await _customerTypeService.Create(customerTypeVo);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpPut]
        public async Task<ActionResult<ResultObject>> Put([FromBody] UpdateCustomerTypeVO customerTypeVo)
        {
            var result = await _customerTypeService.Update(customerTypeVo);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpDelete]
        public async Task<ActionResult<ResultObject>> Delete(int Id)
        {
            var result = await _customerTypeService.Delete(Id);
            return result.Status ? Ok(result) : BadRequest(result);
        }

    }
}
