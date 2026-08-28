using ApplicationCore.Interfaces.Repositories.VoucherType;
using ApplicationCore.Interfaces.Services.VoucherType;
using ApplicationCore.ValueObjects.Result;
using ApplicationCore.ValueObjects.VoucherType;
using AutoMapper;

namespace ApplicationCore.Services.VoucherType
{
    public class VoucherTypeService: IVoucherTypeService
    {
        private readonly IVoucherTypeRepository _voucherTypeRepository;
        private readonly IMapper _mapper;

        public VoucherTypeService(IVoucherTypeRepository voucherTypeRepository, IMapper mapper)
        {
            _voucherTypeRepository = voucherTypeRepository;
            _mapper = mapper;
        }

        public async Task<ResultObject> Create(CreateVoucherTypeVO createVoucherTypeVO)
        {
            try
            {
                await _voucherTypeRepository.Create(createVoucherTypeVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Voucher Type created successfully",
                    Data = createVoucherTypeVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error creating Voucher Type",
                    Detail = ex.Message,
                    ErrorCode = "CREATE_VOUCHER_TYPE_ERROR"
                };
            }
        }

        public async Task<ResultObject> Delete(int id)
        {
            try
            {
                await _voucherTypeRepository.Delete(id);

                return new ResultObject
                {
                    Status = true,
                    Message = "Voucher type deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error deleting voucher type",
                    Detail = ex.Message,
                    ErrorCode = "DELETE_VOUCHER_TYPE_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetAll()
        {
            try
            {
                var voucherTypes = await _voucherTypeRepository.GetAll();
                var voucherTypeVO = _mapper.Map<IEnumerable<ListVoucherTypeVO>>(voucherTypes);

                return new ResultObject
                {
                    Status = true,
                    Message = "Voucher Type retrieved successfully",
                    Data = voucherTypeVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving voucher type",
                    Detail = ex.Message,
                    ErrorCode = "GET_VOUCHER_TYPE_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetById(int id)
        {
            try
            {
                var voucherType = await _voucherTypeRepository.GetById(id);
                var voucherTypeVO = _mapper.Map<GetVoucherTypeVO>(voucherType);

                return new ResultObject
                {
                    Status = true,
                    Message = "Voucher Type retrieved successfully",
                    Data = voucherTypeVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Voucher Type",
                    Detail = ex.Message,
                    ErrorCode = "GET_VOUCHER_TYPE_ERROR"
                };
            }
        }

        public async Task<ResultObject> Update(UpdateVaucherTypeVO voucherTypeVO)
        {
            try
            {
                await _voucherTypeRepository.Update(voucherTypeVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Voucher Type updated successfully",
                    Data = voucherTypeVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error updating Voucher Type",
                    Detail = ex.Message,
                    ErrorCode = "UPDATE_VOUCHER_TYPE_ERROR"
                };
            }
        }
    }
}

