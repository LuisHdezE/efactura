using EFactura.Application.Payables;
using EFactura.Domain.Payables;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfPayableRepository : IPayableRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfPayableRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public Task AddAsync(Payable payable, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payable);

        _dbContext.Set<V1PayableRecord>().Add(new V1PayableRecord
        {
            Id = payable.Id,
            OrganizationId = payable.OrganizationId,
            SupplierPartyId = payable.SupplierPartyId,
            SourceKind = (int)payable.SourceKind,
            SourceId = payable.SourceId,
            OriginalAmount = payable.OriginalAmount,
            CurrencyCode = payable.CurrencyCode,
            SourceEffectiveOn = payable.SourceEffectiveOn.ToDateTime(TimeOnly.MinValue),
            DueDate = payable.DueDate.ToDateTime(TimeOnly.MinValue),
            Version = payable.Version,
            CreatedAtUtc = payable.CreatedAtUtc
        });

        return Task.CompletedTask;
    }

    public async Task<Payable?> GetAsync(
        string organizationId,
        Guid payableId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1PayableRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == payableId,
                cancellationToken);

        return record is null ? null : Map(record);
    }

    public async Task<Payable?> GetBySourceAsync(
        string organizationId,
        PayableSourceKind sourceKind,
        string sourceId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1PayableRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId
                     && x.SourceKind == (int)sourceKind
                     && x.SourceId == sourceId,
                cancellationToken);

        return record is null ? null : Map(record);
    }

    private static Payable Map(V1PayableRecord record) =>
        Payable.Rehydrate(
            record.Id,
            record.OrganizationId,
            record.SupplierPartyId,
            (PayableSourceKind)record.SourceKind,
            record.SourceId,
            record.OriginalAmount,
            record.CurrencyCode,
            DateOnly.FromDateTime(record.SourceEffectiveOn),
            DateOnly.FromDateTime(record.DueDate),
            record.Version,
            record.CreatedAtUtc);
}
