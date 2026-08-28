using ApplicationCore.Interfaces.Services.InvoiceIndicator;
using ApplicationCore.Services.InvoiceIndicator;
using ApplicationCore.ValueObjects.InvoiceIndicator;
using ApplicationCore.ValueObjects.Result;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceIndicatorController : ControllerBase
    {
        private readonly IInvoiceIndicatorService _invoiceIndicatorService;

        public InvoiceIndicatorController(IInvoiceIndicatorService invoiceIndicatorService)
        {
            _invoiceIndicatorService = invoiceIndicatorService;
        }

        [HttpGet]
        public async Task<ActionResult<ResultObject>> GetAll()
        {
            var results = await _invoiceIndicatorService.GetAll();
            return results.Status ? Ok(results) : BadRequest(results);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ResultObject>> GetById(int id)
        {
            var result = await _invoiceIndicatorService.GetById(id);
            return result.Status ? Ok(result) : BadRequest(result);

        }

        [HttpPost]
        public async Task<ActionResult<ResultObject>> Create([FromBody] CreateInvoiceIndicatorVO invoiceIndicatorVO)
        {
            var result = await _invoiceIndicatorService.Create(invoiceIndicatorVO);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ResultObject>> Update(int id, [FromBody] UpdateInvoiceIndicatorVO invoiceIndicatorVO)
        {
            if (id != invoiceIndicatorVO.Id) return BadRequest();
            var result = await _invoiceIndicatorService.Update(invoiceIndicatorVO);
            return result.Status ? Ok(result) : BadRequest(result);
        } 

        [HttpDelete("{id}")]
        public async Task<ActionResult<ResultObject>> Delete(int id)
        {
            var result = await _invoiceIndicatorService.Delete(id);
            return result.Status ? Ok(result) : BadRequest(result);
        } 
    }
}
