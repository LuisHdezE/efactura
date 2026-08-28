using ApplicationCore.Interfaces.Services.ContactType;
using ApplicationCore.Interfaces.Services.ProductCategory;
using ApplicationCore.ValueObjects.ContactType;
using ApplicationCore.ValueObjects.ProductCategory;
using ApplicationCore.ValueObjects.Result;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductCategoryController : ControllerBase
    {
        private readonly IProductCategoryService _productCategoryService;

        public ProductCategoryController(IProductCategoryService productCategoryService)
        {
            _productCategoryService = productCategoryService;
        }

        [HttpGet]
        public async Task<ActionResult<ResultObject>> GetAll()
        {
            var results = await _productCategoryService.GetAll();
            return results.Status ? Ok(results) : BadRequest(results);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ResultObject>> GetById(int id)
        {
            var result = await _productCategoryService.GetById(id);
            return result.Status ? Ok(result) : BadRequest(result);

        }

        [HttpPost]
        public async Task<ActionResult<ResultObject>> Create([FromBody] CreateProductCategoryVO productCategoryVO)
        {
            var result = await _productCategoryService.Create(productCategoryVO);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ResultObject>> Update(int id, [FromBody] UpdateProductCategoryVO productCategoryVO)
        {
            if (id != productCategoryVO.Id) return BadRequest();
            var result = await _productCategoryService.Update(productCategoryVO);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ResultObject>> Delete(int id)
        {
            var result = await _productCategoryService.Delete(id);
            return result.Status ? Ok(result) : BadRequest(result);
        }
    }
}
