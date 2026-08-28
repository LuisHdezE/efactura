using ApplicationCore.Interfaces.Repositories.Products;
using ApplicationCore.ValueObjects.Products;
using ApplicationCore.ValueObjects.Result;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController(IProductsService productService) : ControllerBase
    {
        private readonly IProductsService _productsService = productService;

        [HttpGet("GetProductById")]
        public async Task<ActionResult<ResultObject>> GetProductById(int Id)
        {
            var result = await _productsService.GetProductById(Id);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpGet("GetProductsPaginated")]
        public async Task<ActionResult<ResultObject>> GetProductsPaginated(int Page = 1, int RowsPerPage = 10)
        {
            var result = await _productsService.GetProductsPaginated(Page, RowsPerPage);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        public async Task<ActionResult<ResultObject>> Post([FromBody] CreateProductVO productVO)
        {
            var result = await _productsService.Create(productVO);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpPut]
        public async Task<ActionResult<ResultObject>> Put([FromBody] UpdateProductVO productVO)
        {
            var result = await _productsService.Update(productVO);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpDelete]
        public async Task<ActionResult<ResultObject>> Delete(int customerTypeId, int deletedBy)
        {
            var result = await _productsService.Delete(customerTypeId, deletedBy);
            return result.Status ? Ok(result) : BadRequest(result);
        }

    }
}
