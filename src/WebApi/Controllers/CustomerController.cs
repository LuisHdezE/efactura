using ApplicationCore.Interfaces.Services.Customer;
using ApplicationCore.ValueObjects.Customer;
using ApplicationCore.ValueObjects.Result;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController(ICustomerService CustomerService) : ControllerBase
    {
        private readonly ICustomerService _customerService = CustomerService;

        [HttpGet("GetCustomerById")]
        public async Task<ActionResult<ResultObject>> GetCustomerTypeById(int id)
        {
            var result = await _customerService.GetCustomerById(id);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpGet("GetCustomerPaginated")]
        public async Task<ActionResult<ResultObject>> GetCustomerTypresPaginated(int Page = 1, int RowsPerPage = 10)
        {
            var result = await _customerService.GetCustomerPaginated(Page, RowsPerPage);
            return result.Status ? Ok(result) : BadRequest(result);
        }


        [HttpPost("CreateCustomer")]
        public async Task<ActionResult<ResultObject>> Post([FromBody] CreateCustomerVO customerVo)
        {
            var result = await _customerService.Create(customerVo);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpPut("UpdateCustomer")]
        public async Task<ActionResult<ResultObject>> Put([FromBody] UpdateCustomerVO customerVo)
        {
            var result = await _customerService.Update(customerVo);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("DeleteCustomer")]
        public async Task<ActionResult<ResultObject>> Delete([FromBody] DeleteCustomerVO customerVo)
        {
            var result = await _customerService.Delete(customerVo);
            return result.Status ? Ok(result) : BadRequest(result);
        }

     



    }
}
