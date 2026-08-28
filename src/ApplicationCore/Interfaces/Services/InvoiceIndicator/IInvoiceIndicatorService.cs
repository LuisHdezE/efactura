using ApplicationCore.ValueObjects.InvoiceIndicator;
using ApplicationCore.ValueObjects.Result;

namespace ApplicationCore.Interfaces.Services.InvoiceIndicator
{
    public interface IInvoiceIndicatorService
    {
        public Task<ResultObject> GetById(int id);
        
        public Task<ResultObject> GetAll();
        
        public Task<ResultObject> Create(CreateInvoiceIndicatorVO invoiceIndicatorVO);
        
        public Task<ResultObject> Update(UpdateInvoiceIndicatorVO invoiceIndicatorVO);
       
        public Task<ResultObject> Delete(int id);
    }

}
