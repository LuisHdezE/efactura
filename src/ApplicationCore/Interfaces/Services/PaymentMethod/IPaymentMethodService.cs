using ApplicationCore.ValueObjects.PaymentMethod;
using ApplicationCore.ValueObjects.Result;

namespace ApplicationCore.Interfaces.Services.PaymentMethod
{
    public interface IPaymentMethodService
    {
        public Task<ResultObject> GetAll();
        
        public Task<ResultObject> GetById(long id);
        
        public Task<ResultObject> Create(CreatePaymentMethodVO paymentMethodVO);
        
        public Task<ResultObject> Update(UpdatePaymentMethodVO paymentMethodVO);
        
        public Task<ResultObject> Delete(long id);
    }
}
