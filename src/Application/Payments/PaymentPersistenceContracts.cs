using EFactura.Domain.Payments;

namespace EFactura.Application.Payments;

public interface IPaymentMethodRepository
{
    Task<PaymentMethod?> GetAsync(
        string organizationId,
        Guid paymentMethodId,
        CancellationToken cancellationToken = default);

    Task AddAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default);
    Task SaveAsync(PaymentMethod paymentMethod, CancellationToken cancellationToken = default);
}

public interface IPaymentRepository
{
    Task<Payment?> GetAsync(
        string organizationId,
        Guid paymentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Payment>> ListBySaleAsync(
        string organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);
}
