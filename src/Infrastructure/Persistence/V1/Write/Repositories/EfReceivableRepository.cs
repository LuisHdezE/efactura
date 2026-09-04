using EFactura.Application.Receivables;
using EFactura.Domain.Receivables;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfReceivableRepository : IReceivableRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfReceivableRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public Task AddAsync(Receivable receivable, CancellationToken cancellationToken = default)
    {
        _dbContext.Set<V1ReceivableRecord>().Add(new V1ReceivableRecord
        {
            Id = receivable.Id,
            OrganizationId = receivable.OrganizationId,
            CustomerPartyId = receivable.CustomerPartyId,
            SaleId = receivable.SaleId,
            OriginalAmount = receivable.OriginalAmount,
            CurrencyCode = receivable.CurrencyCode,
            DueDate = receivable.DueDate.ToDateTime(TimeOnly.MinValue),
            ConfirmationFingerprint = receivable.ConfirmationFingerprint,
            SettlementFingerprint = receivable.SettlementFingerprint,
            Version = receivable.Version,
            CreatedAtUtc = receivable.CreatedAtUtc
        });
        return Task.CompletedTask;
    }

    public async Task<Receivable?> GetAsync(
        string organizationId,
        Guid receivableId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1ReceivableRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == receivableId,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<Receivable?> GetBySaleAsync(
        string organizationId,
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1ReceivableRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.SaleId == saleId,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    private static Receivable Map(V1ReceivableRecord record) =>
        Receivable.Rehydrate(
            record.Id,
            record.OrganizationId,
            record.CustomerPartyId,
            record.SaleId,
            record.OriginalAmount,
            record.CurrencyCode,
            DateOnly.FromDateTime(record.DueDate),
            record.ConfirmationFingerprint,
            record.SettlementFingerprint,
            record.Version,
            record.CreatedAtUtc);
}
