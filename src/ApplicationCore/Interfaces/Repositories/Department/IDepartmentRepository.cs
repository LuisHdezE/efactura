using ApplicationCore.ValueObjects.Department;

namespace ApplicationCore.Interfaces.Repositories.Department
{
    public interface IDepartmentRepository 
    {
        Task<GetDepartmentVO> GetById(int id);
       
        Task<IEnumerable<ListDepartmentVO>> GetAll();
        
        Task Create(CreateDepartmentVO departmentVO);
        
        Task Update(UpdateDepartmentVO departmentVO);
        
        Task Delete(int id);
    }
}
