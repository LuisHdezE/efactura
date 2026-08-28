using ApplicationCore.Entities;
using ApplicationCore.Interfaces.Repositories.Customer;
using ApplicationCore.Interfaces.Repositories.Supplier;
using ApplicationCore.Interfaces.Services.Supplier;
using ApplicationCore.ValueObjects.Result;
using ApplicationCore.ValueObjects.Supplier;
using AutoMapper;

namespace ApplicationCore.Services.Supplier
{
    public class SupplierService : ISupplierService
    {
        private readonly IMapper _mapper;
        private readonly ISupplierRepository _supplierRepository;

        public SupplierService(ISupplierRepository supplierRepository, IMapper mapper)
        {
            _mapper = mapper;
            _supplierRepository = supplierRepository;
        }

        public async Task<ResultObject> Create(CreateSupplierVO supplierVO)
        {
            try
            {
                await _supplierRepository.Create(supplierVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Supplier created successfully",
                    Data = supplierVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error creating Supplier",
                    Detail = ex.Message,
                    ErrorCode = "CREATE_SUPPLIER_ERROR"
                };
            }
        }

        public async Task<ResultObject> Delete(int id)
        {
            try
            {
                await _supplierRepository.Delete(id);

                return new ResultObject
                {
                    Status = true,
                    Message = "Supplier deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error deleting Supplier",
                    Detail = ex.Message,
                    ErrorCode = "DELETE_SUPPLIER_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetAll()
        {
            try
            {
                var suppliers = await _supplierRepository.GetAll();
                var suppliersVO = _mapper.Map<IEnumerable<ListSupplierVO>>(suppliers);

                return new ResultObject
                {
                    Status = true,
                    Message = "Suppliers retrieved successfully",
                    Data = suppliersVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Suppliers",
                    Detail = ex.Message,
                    ErrorCode = "GET_SUPPLIER_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetById(int id)
        {
            try
            {
                var supplier = await _supplierRepository.GetById(id);
                var supplierVO = _mapper.Map<GetSupplierVO>(supplier);

                return new ResultObject
                {
                    Status = true,
                    Message = "Supplier retrieved successfully",
                    Data = supplierVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Supplier",
                    Detail = ex.Message,
                    ErrorCode = "GET_SUPPLIER_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetSuppliersPaginated(int Page, int RowsPerPage)
        {
            try
            {
                var suppliers = await _supplierRepository.GetSuppliersPaginated(Page, RowsPerPage);
                var suppliersVO = _mapper.Map<IEnumerable<ListSupplierVO>>(suppliers);

                return new ResultObject
                {
                    Status = true,
                    Message = "Suppliers retrieved successfully",
                    Data = suppliersVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Suppliers",
                    Detail = ex.Message,
                    ErrorCode = "GET_SUPPLIER_ERROR"
                };
            }
        }

        public async Task<ResultObject> Update(UpdateSupplierVO supplierVO)
        {
            try
            {
                var supplier = _mapper.Map<ContactType>(supplierVO);
                await _supplierRepository.Update(supplierVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Supplier updated successfully",
                    Data = supplier
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error updating Supplier",
                    Detail = ex.Message,
                    ErrorCode = "UPDATE_SUPPLIERE_ERROR"
                };
            }
        }
    }
}
