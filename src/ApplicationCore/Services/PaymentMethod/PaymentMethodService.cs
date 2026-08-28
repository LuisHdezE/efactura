
using ApplicationCore.Interfaces.Services.PaymentMethod;
using ApplicationCore.Interfaces.Repositories.PaymentMethod;
using ApplicationCore.ValueObjects.PaymentMethod;
using ApplicationCore.ValueObjects.Result;
using AutoMapper;

namespace ApplicationCore.Services.PaymentMethod
{
    public class PaymentMethodService : IPaymentMethodService
    {
        private readonly IPaymentMethodRepository _paymentMethodRepository;
        private readonly IMapper _mapper;

        public PaymentMethodService(IPaymentMethodRepository paymentMethodRepository, IMapper mapper)
        {
            _paymentMethodRepository = paymentMethodRepository;
            _mapper = mapper;
        }
        
        public async Task<ResultObject> GetById(long id)
        {
            try
            {
                var paymentMethod = await _paymentMethodRepository.GetById(id);
                var paymentMethodVO = _mapper.Map<GetPaymentMethodVO>(paymentMethod);

                return new ResultObject
                {
                    Status = true,
                    Message = "Payment Method retrieved successfully",
                    Data = paymentMethodVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Payment Method",
                    Detail = ex.Message,
                    ErrorCode = "GET_PAYMENT_METHOD_ERROR"
                };
            }
        }

        public async Task<ResultObject> Create(CreatePaymentMethodVO paymentMethodVO)
        {
            try
            {
                await _paymentMethodRepository.Create(paymentMethodVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Payment Method created successfully",
                    Data = paymentMethodVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error creating Payment Method",
                    Detail = ex.Message,
                    ErrorCode = "CREATE_PAYMENT_METHOD_ERROR"
                };
            }
        }

        public async Task<ResultObject> Delete(long id)
        {
            try
            {
                await _paymentMethodRepository.Delete(id);

                return new ResultObject
                {
                    Status = true,
                    Message = "Payment Method deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error deleting Payment Method",
                    Detail = ex.Message,
                    ErrorCode = "DELETE_PAYMENT_METHOD_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetAll()
        {
            try
            {
                var paymentMethod = await _paymentMethodRepository.GetAll();
                var paymentMethodVO = _mapper.Map<IEnumerable<ListPaymentMethodVO>>(paymentMethod);

                return new ResultObject
                {
                    Status = true,
                    Message = "Payment Method retrieved successfully",
                    Data = paymentMethodVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Payment Method",
                    Detail = ex.Message,
                    ErrorCode = "GET_PAYMENT_METHOD_ERROR"
                };
            }
        }

        public async Task<ResultObject> Update(UpdatePaymentMethodVO paymentMethodVO)
        {
            try
            {
                await _paymentMethodRepository.Update(paymentMethodVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Payment Method updated successfully",
                    Data = paymentMethodVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error updating Payment Method",
                    Detail = ex.Message,
                    ErrorCode = "UPDATE_PAYMENT_METHOD_ERROR"
                };
            }
        }

        
    }
}
