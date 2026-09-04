using EFactura.Domain.Receivables;

namespace EFactura.Application.Receivables;

public interface IReceivableRepository
{
    Task<Receivable?> GetAsync(
        string organizationId,
        Guid receivableId,
        CancellationToken cancellationToken = default);

    Task<Receivable?> GetBySaleAsync(
        string organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Receivable receivable, CancellationToken cancellationToken = default);
}
