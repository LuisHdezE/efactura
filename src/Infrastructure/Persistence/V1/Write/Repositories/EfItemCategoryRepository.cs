using EFactura.Application.Catalog;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Results;
using EFactura.Domain.Catalog;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfItemCategoryRepository : IItemCategoryRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfItemCategoryRepository(V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(ItemCategory category, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        _dbContext.ItemCategories.Add(new V1ItemCategoryRecord
        {
            Id = category.Id,
            OrganizationId = category.OrganizationId,
            Code = category.Code,
            Name = category.Name,
            Active = category.Active,
            Version = category.Version,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        return Task.CompletedTask;
    }

    public async Task<ItemCategory?> GetAsync(
        string organizationId,
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ItemCategories.AsNoTracking().SingleOrDefaultAsync(
            x => x.OrganizationId == organizationId && x.Id == categoryId,
            cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<PageResult<ItemCategory>> SearchAsync(
        ItemCategorySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.ItemCategories
            .AsNoTracking()
            .Where(x => x.OrganizationId == request.OrganizationId);

        if (request.Active.HasValue)
        {
            query = query.Where(x => x.Active == request.Active.Value);
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

        return new PageResult<ItemCategory>(records.Select(Map).ToArray(), request.Page, request.PageSize, total);
    }

    public async Task SaveAsync(ItemCategory category, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.ItemCategories.SingleOrDefaultAsync(
            x => x.OrganizationId == category.OrganizationId && x.Id == category.Id,
            cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound,
                "catalog.category_not_found",
                "The requested category was not found.");

        var priorVersion = category.Version - 1;
        if (record.Version != priorVersion)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                "The category changed before this operation could be persisted.",
                conflictType: "stale_version",
                currentVersion: record.Version.ToString());
        }

        _dbContext.Entry(record).Property(x => x.Version).OriginalValue = priorVersion;
        record.Code = category.Code;
        record.Name = category.Name;
        record.Active = category.Active;
        record.Version = category.Version;
        record.UpdatedAtUtc = DateTime.UtcNow;
    }

    public Task<bool> CodeExistsAsync(
        string organizationId,
        string code,
        Guid? excludingCategoryId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return _dbContext.ItemCategories.AnyAsync(
            x => x.OrganizationId == organizationId
                 && x.Code == normalizedCode
                 && (!excludingCategoryId.HasValue || x.Id != excludingCategoryId.Value),
            cancellationToken);
    }

    private static ItemCategory Map(V1ItemCategoryRecord record) =>
        ItemCategory.Rehydrate(
            record.Id,
            record.OrganizationId,
            record.Code,
            record.Name,
            record.Active,
            record.Version);
}
