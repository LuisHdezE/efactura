using EFactura.Application.Common.Results;
using EFactura.Application.Taxation;
using EFactura.Domain.Taxation;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfTaxProfileRepository : ITaxProfileRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfTaxProfileRepository(V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(TaxProfile profile, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        _dbContext.TaxProfiles.Add(new V1TaxProfileRecord
        {
            Id = profile.Id,
            OrganizationId = profile.OrganizationId,
            Code = profile.Code,
            Name = profile.Name,
            TreatmentCode = profile.TreatmentCode,
            RatePercent = profile.RatePercent,
            EffectiveFromUtc = profile.EffectiveFrom.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            EffectiveToUtc = profile.EffectiveTo?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            SourceName = profile.SourceName,
            SourceReference = profile.SourceReference,
            SourceVersion = profile.SourceVersion,
            Active = profile.Active,
            Version = profile.Version,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        return Task.CompletedTask;
    }

    public async Task<TaxProfile?> GetAsync(
        string organizationId,
        Guid profileId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.TaxProfiles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OrganizationId == organizationId && x.Id == profileId, cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<PageResult<TaxProfile>> SearchAsync(
        TaxProfileSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var from = request.EffectiveOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var query = _dbContext.TaxProfiles.AsNoTracking()
            .Where(x => x.OrganizationId == request.OrganizationId)
            .Where(x => x.EffectiveFromUtc <= from && (!x.EffectiveToUtc.HasValue || from <= x.EffectiveToUtc.Value));

        if (request.ActiveOnly)
        {
            query = query.Where(x => x.Active);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToUpperInvariant();
            query = query.Where(x => x.Code.ToUpper().Contains(search)
                                     || x.Name.ToUpper().Contains(search)
                                     || x.TreatmentCode.ToUpper().Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);
        var records = await query
            .OrderBy(x => x.Code)
            .ThenByDescending(x => x.EffectiveFromUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PageResult<TaxProfile>(records.Select(Map).ToArray(), request.Page, request.PageSize, total);
    }

    private static TaxProfile Map(V1TaxProfileRecord record) => TaxProfile.Rehydrate(
        record.Id,
        record.OrganizationId,
        record.Code,
        record.Name,
        record.TreatmentCode,
        record.RatePercent,
        DateOnly.FromDateTime(record.EffectiveFromUtc),
        record.EffectiveToUtc.HasValue ? DateOnly.FromDateTime(record.EffectiveToUtc.Value) : null,
        record.SourceName,
        record.SourceReference,
        record.SourceVersion,
        record.Active,
        record.Version);
}
