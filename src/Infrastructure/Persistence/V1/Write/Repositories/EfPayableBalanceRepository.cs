using EFactura.Application.Payables;
using EFactura.Domain.Payables;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfPayableBalanceRepository :
    IPayableBalanceEffectRepository,
    IPayableBalanceSourceReader
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfPayableBalanceRepository(V1PersistenceDbContext dbContext) =>
        _dbContext = dbContext;

    public Task AddAsync(
        PayableBalanceEffect effect,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        _dbContext.Set<V1PayableBalanceEffectRecord>().Add(new V1PayableBalanceEffectRecord
        {
            Id = effect.Id,
            OrganizationId = effect.OrganizationId,
            PayableId = effect.PayableId,
            Kind = (int)effect.Kind,
            Amount = effect.Amount,
            SourceId = effect.SourceId,
            SourceSequence = effect.SourceSequence,
            ReversesEffectId = effect.ReversesEffectId,
            OccurredAtUtc = effect.OccurredAtUtc
        });

        return Task.CompletedTask;
    }

    public async Task<PayableBalanceEffect?> GetAsync(
        string organizationId,
        Guid effectId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1PayableBalanceEffectRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == effectId,
                cancellationToken);

        return record is null ? null : MapEffect(record);
    }

    public async Task<IReadOnlyList<PayableBalanceSource>> ListByPartyAsync(
        string organizationId,
        Guid partyId,
        CancellationToken cancellationToken = default)
    {
        var payables = await _dbContext.Set<V1PayableRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.SupplierPartyId == partyId)
            .OrderBy(x => x.CurrencyCode)
            .ThenBy(x => x.DueDate)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        if (payables.Count == 0)
            return Array.Empty<PayableBalanceSource>();

        var payableIds = payables.Select(x => x.Id).ToArray();
        var effects = await _dbContext.Set<V1PayableBalanceEffectRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && payableIds.Contains(x.PayableId))
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var effectsByPayable = effects
            .GroupBy(x => x.PayableId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<PayableBalanceEffect>)x.Select(MapEffect).ToArray());

        return payables
            .Select(x => new PayableBalanceSource(
                x.Id,
                x.OrganizationId,
                x.SupplierPartyId,
                x.OriginalAmount,
                x.CurrencyCode,
                DateOnly.FromDateTime(x.DueDate),
                x.CreatedAtUtc,
                effectsByPayable.TryGetValue(x.Id, out var payableEffects)
                    ? payableEffects
                    : Array.Empty<PayableBalanceEffect>()))
            .ToArray();
    }

    private static PayableBalanceEffect MapEffect(V1PayableBalanceEffectRecord record) =>
        PayableBalanceEffect.Rehydrate(
            record.Id,
            record.OrganizationId,
            record.PayableId,
            (PayableBalanceEffectKind)record.Kind,
            record.Amount,
            record.SourceId,
            record.SourceSequence,
            record.ReversesEffectId,
            record.OccurredAtUtc);
}
