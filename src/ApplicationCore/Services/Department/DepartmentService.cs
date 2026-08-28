using ApplicationCore.Entities;
using ApplicationCore.Interfaces.Repositories.ContactTypeRepository;
using ApplicationCore.Interfaces.Repositories.Department;
using ApplicationCore.Interfaces.Services.Department;
using ApplicationCore.ValueObjects.ContactType;
using ApplicationCore.ValueObjects.Department;
using ApplicationCore.ValueObjects.Result;
using AutoMapper;

namespace ApplicationCore.Services.Department
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IDepartmentRepository _departmentRepository;

        private readonly IMapper _mapper;

        public DepartmentService(IDepartmentRepository departmentRepository, IMapper mapper)
        {
            _departmentRepository = departmentRepository;
            _mapper = mapper;
        }

        public async Task<ResultObject> GetById(int id)
        {
            try
            {
                var department = await _departmentRepository.GetById(id);
                var departmentVO = _mapper.Map<GetDepartmentVO>(department);

                return new ResultObject
                {
                    Status = true,
                    Message = "Department retrieved successfully",
                    Data = departmentVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Department",
                    Detail = ex.Message,
                    ErrorCode = "GET_DEPARTMENT_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetAll()
        {
            try
            {
                var departments = await _departmentRepository.GetAll();
                var departmentsVO = _mapper.Map<IEnumerable<ListDepartmentVO>>(departments);

                return new ResultObject
                {
                    Status = true,
                    Message = "Department retrieved successfully",
                    Data = departmentsVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Deparment",
                    Detail = ex.Message,
                    ErrorCode = "GET_DEPARTMENT_ERROR"
                };
            }
        }

        public async Task<ResultObject> Create(CreateDepartmentVO departmentVo)
        {
            try
            {
                await _departmentRepository.Create(departmentVo);

                return new ResultObject
                {
                    Status = true,
                    Message = "Department created successfully",
                    Data = departmentVo
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error creating Department",
                    Detail = ex.Message,
                    ErrorCode = "CREATE_DEPARTMENT_ERROR"
                };
            }
        }

        public async Task<ResultObject> Update(UpdateDepartmentVO departmentVo)
        {
            throw new NotImplementedException();
        }

        public async Task<ResultObject> Delete(int id)
        {
            try
            {
                await _departmentRepository.Delete(id);

                return new ResultObject
                {
                    Status = true,
                    Message = "Department deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error deleting Department",
                    Detail = ex.Message,
                    ErrorCode = "DELETE_DEPARTMENT_ERROR"
                };
            }
        }
    }
}
