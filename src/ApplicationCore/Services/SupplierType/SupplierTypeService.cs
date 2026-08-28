using ApplicationCore.Entities;
using ApplicationCore.Interfaces.Repositories.SupplierType;
using ApplicationCore.Interfaces.Services.SupplierType;
using ApplicationCore.ValueObjects.Result;
using ApplicationCore.ValueObjects.SupplierType;
using AutoMapper;

namespace ApplicationCore.Services.SupplierType
{
    public class SupplierTypeService : ISupplierTypeService
    {
        private readonly IMapper _mapper;

        private readonly ISupplierTypeRepository _supplierTypeRepository;

        public SupplierTypeService(ISupplierTypeRepository supplierTypeRepository, IMapper mapper)
        {
            _mapper = mapper;
            _supplierTypeRepository = supplierTypeRepository;
        }

        public async Task<ResultObject> Create(CreateSupplierTypeVO supplierTypeVO)
        {
            try
            {
                await _supplierTypeRepository.Create(supplierTypeVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Supplier Type created successfully",
                    Data = supplierTypeVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error creating Supplier Type",
                    Detail = ex.Message,
                    ErrorCode = "CREATE_SUPPLIER_TYPE_ERROR"
                };
            }
        }

        public async Task<ResultObject> Delete(int id)
        {
            try
            {
                await _supplierTypeRepository.Delete(id);

                return new ResultObject
                {
                    Status = true,
                    Message = "Supplier Type deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error deleting Supplier Type",
                    Detail = ex.Message,
                    ErrorCode = "DELETE_SUPPLIER_TYPE_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetAll()
        {
            try
            {
                var suppliertTypes = await _supplierTypeRepository.GetAll();
                var suppliertTypesVO = _mapper.Map<IEnumerable<ListSupplierTypeVO>>(suppliertTypes);

                return new ResultObject
                {
                    Status = true,
                    Message = "Supplier Type retrieved successfully",
                    Data = suppliertTypesVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Supplier Type",
                    Detail = ex.Message,
                    ErrorCode = "GET_SUPPLIER_TYPE_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetByIdAsync(int id)
        {
            try
            {
                var supplierType = await _supplierTypeRepository.GetById(id);
                var supplierTypeVO = _mapper.Map<GetSupplierTypeVO>(supplierType);

                return new ResultObject
                {
                    Status = true,
                    Message = "Supplier Type retrieved successfully",
                    Data = supplierTypeVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Supplier Type",
                    Detail = ex.Message,
                    ErrorCode = "GET_SUPPLIER_TYPE_ERROR"
                };
            }
        }

        public async Task<ResultObject> Update(UpdateSupplierTypeVO supplierTypeVO)
        {
            try
            {
                var supplierType = _mapper.Map<ContactType>(supplierTypeVO);
                await _supplierTypeRepository.Update(supplierTypeVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Supplier Type updated successfully",
                    Data = supplierType
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error updating Supplier type",
                    Detail = ex.Message,
                    ErrorCode = "UPDATE_SUPPLIER_TYPE_ERROR"
                };
            }
        }
    }
}
