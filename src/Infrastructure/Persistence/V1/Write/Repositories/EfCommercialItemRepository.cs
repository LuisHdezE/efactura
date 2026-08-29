using EFactura.Application.Catalog;
using EFactura.Domain.Catalog;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfCommercialItemRepository : ICommercialItemRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfCommercialItemRepository(V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(CommercialItem item, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        _dbContext.CommercialItems.Add(new V1CommercialItemRecord
        {
            Id = item.Id,
            OrganizationId = item.OrganizationId,
            Code = item.Code,
            Name = item.Name,
            Description = item.Description,
            Kind = (int)item.Kind,
            Unit = item.Unit,
            TrackInventory = item.TrackInventory,
            TaxProfileId = item.TaxProfileId,
            CategoryId = item.CategoryId,
            Active = item.Active,
            Version = item.Version,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        return Task.CompletedTask;
    }

    public async Task<CommercialItem?> GetAsync(
        string organizationId,
        Guid itemId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.CommercialItems
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == itemId,
                cancellationToken);

        return record is null
            ? null
            : CommercialItem.Rehydrate(
                record.Id,
                record.OrganizationId,
                record.Code,
                record.Name,
                record.Description,
                (CommercialItemKind)record.Kind,
                record.Unit,
                record.TrackInventory,
                record.TaxProfileId,
                record.CategoryId,
                record.Active,
                record.Version);
    }

    public Task<bool> CodeExistsAsync(
        string organizationId,
        string code,
        Guid? excludingItemId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return _dbContext.CommercialItems.AnyAsync(
            x => x.OrganizationId == organizationId
                 && x.Code == normalizedCode
                 && (!excludingItemId.HasValue || x.Id != excludingItemId.Value),
            cancellationToken);
    }
}
