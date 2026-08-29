using EFactura.Application.Catalog;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Results;
using EFactura.Domain.Catalog;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfCommercialItemRepository : ICommercialItemRepository, ICommercialItemMaintenanceRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfCommercialItemRepository(V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(CommercialItem item, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        _dbContext.CommercialItems.Add(MapRecord(item, now, now));
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

        return record is null ? null : Map(record);
    }

    public async Task<PageResult<CommercialItem>> SearchAsync(
        CommercialItemSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CommercialItems
            .AsNoTracking()
            .Where(x => x.OrganizationId == request.OrganizationId);

        if (request.Active.HasValue)
        {
            query = query.Where(x => x.Active == request.Active.Value);
        }

        if (request.Kind.HasValue)
        {
            var kind = (int)request.Kind.Value;
            query = query.Where(x => x.Kind == kind);
        }

        if (request.TrackInventory.HasValue)
        {
            query = query.Where(x => x.TrackInventory == request.TrackInventory.Value);
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == request.CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var normalized = request.Search.Trim().ToUpper();
            query = query.Where(x => x.Code.ToUpper().Contains(normalized) || x.Name.ToUpper().Contains(normalized));
        }

        var total = await query.LongCountAsync(cancellationToken);
        var records = await query
            .OrderBy(x => x.Code)
            .ThenBy(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PageResult<CommercialItem>(records.Select(Map).ToArray(), request.Page, request.PageSize, total);
    }

    public async Task SaveAsync(CommercialItem item, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.CommercialItems.SingleOrDefaultAsync(
            x => x.OrganizationId == item.OrganizationId && x.Id == item.Id,
            cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound,
                "catalog.item_not_found",
                "The requested catalog item was not found.");

        var priorVersion = item.Version - 1;
        if (record.Version != priorVersion)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                "The catalog item changed before this operation could be persisted.",
                conflictType: "stale_version",
                currentVersion: record.Version.ToString());
        }

        _dbContext.Entry(record).Property(x => x.Version).OriginalValue = priorVersion;
        record.Code = item.Code;
        record.Name = item.Name;
        record.Description = item.Description;
        record.Kind = (int)item.Kind;
        record.Unit = item.Unit;
        record.TrackInventory = item.TrackInventory;
        record.TaxProfileId = item.TaxProfileId;
        record.CategoryId = item.CategoryId;
        record.Active = item.Active;
        record.Version = item.Version;
        record.UpdatedAtUtc = DateTime.UtcNow;
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

    private static V1CommercialItemRecord MapRecord(CommercialItem item, DateTime createdAtUtc, DateTime updatedAtUtc) =>
        new()
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
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = updatedAtUtc
        };

    private static CommercialItem Map(V1CommercialItemRecord record) =>
        CommercialItem.Rehydrate(
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
