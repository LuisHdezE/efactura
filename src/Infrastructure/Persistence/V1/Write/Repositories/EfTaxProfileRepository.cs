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

    public async Task<PageResult<TaxProfile>> SearchUsableAsync(
        TaxProfileSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var onDate = ToUtcStartOfDay(request.OnDate);
        var query = _dbContext.TaxProfiles
            .AsNoTracking()
            .Where(x => x.Active
                && (x.OrganizationId == null || x.OrganizationId == request.OrganizationId)
                && x.EffectiveFromUtc <= onDate
                && (!x.EffectiveToUtc.HasValue || x.EffectiveToUtc.Value >= onDate));

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(x => x.Code)
            .ThenByDescending(x => x.EffectiveFromUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PageResult<TaxProfile>(
            rows.Select(Map).ToArray(),
            request.Page,
            request.PageSize,
            total);
    }

    public async Task<TaxProfile?> GetUsableAsync(
        string organizationId,
        Guid taxProfileId,
        DateOnly onDate,
        CancellationToken cancellationToken = default)
    {
        var date = ToUtcStartOfDay(onDate);
        var row = await _dbContext.TaxProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == taxProfileId
                    && x.Active
                    && (x.OrganizationId == null || x.OrganizationId == organizationId)
                    && x.EffectiveFromUtc <= date
                    && (!x.EffectiveToUtc.HasValue || x.EffectiveToUtc.Value >= date),
                cancellationToken);

        return row is null ? null : Map(row);
    }

    private static DateTime ToUtcStartOfDay(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    private static TaxProfile Map(V1TaxProfileRecord row) =>
        TaxProfile.Rehydrate(
            row.Id,
            row.OrganizationId,
            row.Code,
            row.Name,
            (TaxTreatmentKind)row.Treatment,
            row.RatePercent,
            row.CfeBillingIndicator,
            DateOnly.FromDateTime(row.EffectiveFromUtc),
            row.EffectiveToUtc.HasValue ? DateOnly.FromDateTime(row.EffectiveToUtc.Value) : null,
            row.RuleVersion,
            row.SourceAuthority,
            row.SourceReference,
            row.SourceUri,
            row.CfeSpecificationVersion,
            new DateTimeOffset(DateTime.SpecifyKind(row.VerifiedAtUtc, DateTimeKind.Utc)),
            row.Active,
            row.Version);
}
