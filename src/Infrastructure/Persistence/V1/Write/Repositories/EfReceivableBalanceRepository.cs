using EFactura.Application.Receivables;
using EFactura.Domain.Receivables;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfReceivableBalanceRepository :
    IReceivableBalanceEffectRepository,
    IReceivableBalanceSourceReader
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfReceivableBalanceRepository(V1PersistenceDbContext dbContext) =>
        _dbContext = dbContext;

    public Task AddAsync(
        ReceivableBalanceEffect effect,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(effect);

        _dbContext.Set<V1ReceivableBalanceEffectRecord>().Add(new V1ReceivableBalanceEffectRecord
        {
            Id = effect.Id,
            OrganizationId = effect.OrganizationId,
            ReceivableId = effect.ReceivableId,
            Kind = (int)effect.Kind,
            Amount = effect.Amount,
            SourceId = effect.SourceId,
            SourceSequence = effect.SourceSequence,
            ReversesEffectId = effect.ReversesEffectId,
            OccurredAtUtc = effect.OccurredAtUtc
        });

        return Task.CompletedTask;
    }

    public async Task<ReceivableBalanceEffect?> GetAsync(
        string organizationId,
        Guid effectId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1ReceivableBalanceEffectRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == effectId,
                cancellationToken);

        return record is null ? null : MapEffect(record);
    }

    public async Task<IReadOnlyList<ReceivableBalanceSource>> ListByPartyAsync(
        string organizationId,
        Guid partyId,
        CancellationToken cancellationToken = default)
    {
        var receivables = await _dbContext.Set<V1ReceivableRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.CustomerPartyId == partyId)
            .OrderBy(x => x.CurrencyCode)
            .ThenBy(x => x.DueDate)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        if (receivables.Count == 0)
            return Array.Empty<ReceivableBalanceSource>();

        var receivableIds = receivables.Select(x => x.Id).ToArray();
        var effects = await _dbContext.Set<V1ReceivableBalanceEffectRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && receivableIds.Contains(x.ReceivableId))
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var effectsByReceivable = effects
            .GroupBy(x => x.ReceivableId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<ReceivableBalanceEffect>)x.Select(MapEffect).ToArray());

        return receivables
            .Select(x => new ReceivableBalanceSource(
                x.Id,
                x.OrganizationId,
                x.CustomerPartyId,
                x.OriginalAmount,
                x.CurrencyCode,
                DateOnly.FromDateTime(x.DueDate),
                x.CreatedAtUtc,
                effectsByReceivable.TryGetValue(x.Id, out var receivableEffects)
                    ? receivableEffects
                    : Array.Empty<ReceivableBalanceEffect>()))
            .ToArray();
    }

    private static ReceivableBalanceEffect MapEffect(V1ReceivableBalanceEffectRecord record) =>
        ReceivableBalanceEffect.Rehydrate(
            record.Id,
            record.OrganizationId,
            record.ReceivableId,
            (ReceivableBalanceEffectKind)record.Kind,
            record.Amount,
            record.SourceId,
            record.SourceSequence,
            record.ReversesEffectId,
            record.OccurredAtUtc);
}
