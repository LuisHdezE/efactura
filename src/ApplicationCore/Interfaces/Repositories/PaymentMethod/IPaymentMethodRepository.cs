using ApplicationCore.ValueObjects.PaymentMethod;

namespace ApplicationCore.Interfaces.Repositories.PaymentMethod
{
    public interface IPaymentMethodRepository
    {
        public Task<IEnumerable<ListPaymentMethodVO>> GetAll();

        public Task<GetPaymentMethodVO> GetById(long id);

        public Task Create(CreatePaymentMethodVO paymentMethodVO);

        public Task Update(UpdatePaymentMethodVO paymentMethodVO);
        
        public Task Delete(long id);

    }
}
