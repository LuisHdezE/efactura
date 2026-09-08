using EFactura.Application.Common.Errors;
using EFactura.Application.Organizations;
using EFactura.Domain.Organizations;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfOrganizationRepository : ICompanyFiscalProfileRepository, IFiscalLocationRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfOrganizationRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

    public async Task<CompanyFiscalProfile?> GetAsync(string organizationId, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1CompanyFiscalProfileRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.OrganizationId == organizationId, cancellationToken);

        return record is null
            ? null
            : CompanyFiscalProfile.Rehydrate(
                record.OrganizationId,
                record.Ruc,
                record.LegalName,
                record.CommercialName,
                record.Version);
    }

    public Task AddAsync(CompanyFiscalProfile company, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        _dbContext.Set<V1CompanyFiscalProfileRecord>().Add(new V1CompanyFiscalProfileRecord
        {
            OrganizationId = company.OrganizationId,
            Ruc = company.Ruc,
            LegalName = company.LegalName,
            CommercialName = company.CommercialName,
            Version = company.Version,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        return Task.CompletedTask;
    }

    public async Task SaveAsync(CompanyFiscalProfile company, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1CompanyFiscalProfileRecord>()
            .SingleOrDefaultAsync(x => x.OrganizationId == company.OrganizationId, cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound,
                "organization.company.not_found",
                "The company fiscal profile was not found.");

        var priorVersion = company.Version - 1;
        if (record.Version != priorVersion)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                "The company fiscal profile changed before this operation could be persisted.",
                conflictType: "stale_version",
                currentVersion: record.Version.ToString());
        }

        _dbContext.Entry(record).Property(x => x.Version).OriginalValue = priorVersion;
        record.Ruc = company.Ruc;
        record.LegalName = company.LegalName;
        record.CommercialName = company.CommercialName;
        record.Version = company.Version;
        record.UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public async Task<IReadOnlyCollection<FiscalLocation>> ListAsync(
        string organizationId,
        bool? active,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Set<V1FiscalLocationRecord>()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId);

        if (active.HasValue)
            query = query.Where(x => x.Active == active.Value);

        return (await query
                .OrderBy(x => x.DgiBranchCode)
                .ThenBy(x => x.Name)
                .ToArrayAsync(cancellationToken))
            .Select(Map)
            .ToArray();
    }

    public async Task<FiscalLocation?> GetAsync(
        string organizationId,
        string locationId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalLocationRecord>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == locationId,
                cancellationToken);

        return record is null ? null : Map(record);
    }

    public Task<bool> BranchCodeExistsAsync(
        string organizationId,
        string dgiBranchCode,
        string? excludingLocationId = null,
        CancellationToken cancellationToken = default) =>
        _dbContext.Set<V1FiscalLocationRecord>()
            .AsNoTracking()
            .AnyAsync(
                x => x.OrganizationId == organizationId
                     && x.DgiBranchCode == dgiBranchCode
                     && (excludingLocationId == null || x.Id != excludingLocationId),
                cancellationToken);

    public Task AddAsync(FiscalLocation location, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        _dbContext.Set<V1FiscalLocationRecord>().Add(new V1FiscalLocationRecord
        {
            Id = location.Id,
            OrganizationId = location.OrganizationId,
            Name = location.Name,
            DgiBranchCode = location.DgiBranchCode,
            FiscalAddress = location.FiscalAddress,
            City = location.City,
            Department = location.Department,
            Active = location.Active,
            Version = location.Version,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        return Task.CompletedTask;
    }

    public async Task SaveAsync(FiscalLocation location, CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1FiscalLocationRecord>()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == location.OrganizationId && x.Id == location.Id,
                cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound,
                "organization.location.not_found",
                "The fiscal location was not found.");

        var priorVersion = location.Version - 1;
        if (record.Version != priorVersion)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                "The fiscal location changed before this operation could be persisted.",
                conflictType: "stale_version",
                currentVersion: record.Version.ToString());
        }

        _dbContext.Entry(record).Property(x => x.Version).OriginalValue = priorVersion;
        record.Name = location.Name;
        record.DgiBranchCode = location.DgiBranchCode;
        record.FiscalAddress = location.FiscalAddress;
        record.City = location.City;
        record.Department = location.Department;
        record.Active = location.Active;
        record.Version = location.Version;
        record.UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static FiscalLocation Map(V1FiscalLocationRecord record) =>
        FiscalLocation.Rehydrate(
            record.Id,
            record.OrganizationId,
            record.Name,
            record.DgiBranchCode,
            record.FiscalAddress,
            record.City,
            record.Department,
            record.Active,
            record.Version);
}
