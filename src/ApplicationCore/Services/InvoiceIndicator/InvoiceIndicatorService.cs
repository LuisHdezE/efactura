using ApplicationCore.Interfaces.Repositories.InvoiceIndicator;
using ApplicationCore.Interfaces.Services.InvoiceIndicator;
using ApplicationCore.ValueObjects.InvoiceIndicator;
using ApplicationCore.ValueObjects.Result;
using AutoMapper;

namespace ApplicationCore.Services.InvoiceIndicator
{
    public class InvoiceIndicatorService: IInvoiceIndicatorService
    {
        private readonly IInvoiceIndicatorRepository _invoiceIndicatorRepository;
        private readonly IMapper _mapper;

        public InvoiceIndicatorService(IInvoiceIndicatorRepository invoiceIndicatorRepository, IMapper mapper)
        {
            _invoiceIndicatorRepository = invoiceIndicatorRepository;
            _mapper = mapper;
        }

        public async Task<ResultObject> Create(CreateInvoiceIndicatorVO invoiceIndicatorVO)
        {
            try
            {
                await _invoiceIndicatorRepository.Create(invoiceIndicatorVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Invoice Indicator created successfully",
                    Data = invoiceIndicatorVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error creating Invoice Indicator",
                    Detail = ex.Message,
                    ErrorCode = "CREATE_INVOICE_INDICATOR_ERROR"
                };
            }
        }

        public async Task<ResultObject> Delete(int id)
        {
            try
            {
                await _invoiceIndicatorRepository.Delete(id);

                return new ResultObject
                {
                    Status = true,
                    Message = "Invoice Indicator deleted successfully"
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error deleting Invoice Indicator",
                    Detail = ex.Message,
                    ErrorCode = "DELETE_INVOICE_INDICATOR_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetAll()
        {
            try
            {
                var invoiceIndicator = await _invoiceIndicatorRepository.GetAll();
                var invoiceIndicatorVO = _mapper.Map<IEnumerable<ListInvoiceIndicatorVO>>(invoiceIndicator);

                return new ResultObject
                {
                    Status = true,
                    Message = "Invoice Indicator retrieved successfully",
                    Data = invoiceIndicatorVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Invoice Indicator",
                    Detail = ex.Message,
                    ErrorCode = "GET_INVOICE_INDICATOR_ERROR"
                };
            }
        }

        public async Task<ResultObject> GetById(int id)
        {
            try
            {
                var invoiceIndicator = await _invoiceIndicatorRepository.GetById(id);
                var invoiceIndicatorVO = _mapper.Map<GetInvoiceIndicatorVO>(invoiceIndicator);

                return new ResultObject
                {
                    Status = true,
                    Message = "Invoice Indicator retrieved successfully",
                    Data = invoiceIndicatorVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error retrieving Invoice Indicator",
                    Detail = ex.Message,
                    ErrorCode = "GET_INVOICE_INDICATOR_ERROR"
                };
            }
        }

        public async Task<ResultObject> Update(UpdateInvoiceIndicatorVO invoiceIndicatorVO)
        {
            try
            {
                await _invoiceIndicatorRepository.Update(invoiceIndicatorVO);

                return new ResultObject
                {
                    Status = true,
                    Message = "Invoice Indicator updated successfully",
                    Data = invoiceIndicatorVO
                };
            }
            catch (Exception ex)
            {
                return new ResultObject
                {
                    Status = false,
                    Message = "Error updating Invoice Indicator",
                    Detail = ex.Message,
                    ErrorCode = "UPDATE_INVOICE_INDICATOR_ERROR"
                };
            }
        }
    }
}
