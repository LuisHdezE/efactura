using EFactura.Domain.Fiscal;

namespace EFactura.Application.Fiscal;

public interface IFiscalizationRequestRepository
{
    Task<FiscalizationRequest?> GetAsync(
        string organizationId,
        Guid requestId,
        CancellationToken cancellationToken = default);

    Task<FiscalizationRequest?> GetBySaleAsync(
        string organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default);

    Task AddAsync(FiscalizationRequest request, CancellationToken cancellationToken = default);
}
