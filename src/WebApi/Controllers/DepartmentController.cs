using ApplicationCore.Interfaces.Services.Department;
using ApplicationCore.ValueObjects.Department;
using ApplicationCore.ValueObjects.Result;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DepartmentController : ControllerBase
    {
        private readonly IDepartmentService _departmentService;
        
        public DepartmentController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        [HttpGet]
        public async Task<ActionResult<ResultObject>> GetAll()
        {
            var results = await _departmentService.GetAll();
            return results.Status ? Ok(results) : BadRequest(results);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ResultObject>> GetById(int id)
        {
            var result = await _departmentService.GetById(id);
            return result.Status ? Ok(result) : BadRequest(result);

        }

        [HttpPost]
        public async Task<ActionResult<ResultObject>> Create([FromBody] CreateDepartmentVO departmentVO)
        {
            var result = await _departmentService.Create(departmentVO);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ResultObject>> Update(int id, [FromBody] UpdateDepartmentVO departmentVO)
        {
            if (id != departmentVO.Id) return BadRequest();
            var result = await _departmentService.Update(departmentVO);
            return result.Status ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ResultObject>> Delete(int id)
        {
            var result = await _departmentService.Delete(id);
            return result.Status ? Ok(result) : BadRequest(result);
        }
    }
}
