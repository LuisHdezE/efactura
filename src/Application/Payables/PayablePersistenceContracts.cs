using EFactura.Domain.Payables;

namespace EFactura.Application.Payables;

public interface IPayableRepository
{
    Task<Payable?> GetAsync(
        string organizationId,
        Guid payableId,
        CancellationToken cancellationToken = default);

    Task<Payable?> GetBySourceAsync(
        string organizationId,
        PayableSourceKind sourceKind,
        string sourceId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Payable payable, CancellationToken cancellationToken = default);
}
