using ApplicationCore.ValueObjects.Department;
using ApplicationCore.ValueObjects.Result;

namespace ApplicationCore.Interfaces.Services.Department
{
    public interface IDepartmentService
    {
        public Task<ResultObject> GetById(int id);

        public Task<ResultObject> GetAll();

        public Task<ResultObject> Create(CreateDepartmentVO departmentVo);

        public Task<ResultObject> Update(UpdateDepartmentVO departmentVo);

        public Task<ResultObject> Delete(int id);

        //Task<IEnumerable<ResultObject>> GetByCountryId(int countryId);
    }
}
