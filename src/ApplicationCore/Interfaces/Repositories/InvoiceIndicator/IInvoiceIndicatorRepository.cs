using ApplicationCore.ValueObjects.InvoiceIndicator;

namespace ApplicationCore.Interfaces.Repositories.InvoiceIndicator
{
    public interface IInvoiceIndicatorRepository
    {
        Task<GetInvoiceIndicatorVO> GetById(int id);
        
        Task<IEnumerable<ListInvoiceIndicatorVO>> GetAll();
        
        Task Create(CreateInvoiceIndicatorVO invoiceIndicatorVO);
        
        Task Update(UpdateInvoiceIndicatorVO invoiceIndicatorVO);
        
        Task Delete(int id);

    }

}
