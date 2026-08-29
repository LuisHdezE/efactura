using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Results;
using EFactura.Application.Parties;
using EFactura.Domain.Parties;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfPartyRepository : IPartyRepository, IPartyMaintenanceRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfPartyRepository(V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(Party party, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var record = new V1PartyRecord
        {
            Id = party.Id,
            OrganizationId = party.OrganizationId,
            Kind = (int)party.Kind,
            Name = party.Name,
            ResidenceCountry = party.ResidenceCountry,
            TaxResidenceCountry = party.TaxResidenceCountry,
            Active = party.Active,
            Version = party.Version,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Roles = party.Roles.Select(role => new V1PartyRoleRecord
            {
                PartyId = party.Id,
                Role = (int)role
            }).ToList(),
            FiscalIdentities = party.FiscalIdentities.Select(identity => MapIdentity(party.Id, identity)).ToList()
        };

        _dbContext.Parties.Add(record);
        return Task.CompletedTask;
    }

    public async Task<Party?> GetAsync(
        string organizationId,
        Guid partyId,
        CancellationToken cancellationToken = default)
    {
        var record = await BaseQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == partyId,
                cancellationToken);

        return record is null ? null : Map(record);
    }

    public async Task<PageResult<Party>> SearchAsync(
        PartySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = BaseQuery()
            .AsNoTracking()
            .Where(x => x.OrganizationId == request.OrganizationId);

        if (request.Active.HasValue)
        {
            query = query.Where(x => x.Active == request.Active.Value);
        }

        if (request.Role.HasValue)
        {
            var role = (int)request.Role.Value;
            query = query.Where(x => x.Roles.Any(r => r.Role == role));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var normalized = request.Search.Trim().ToUpper();
            query = query.Where(x =>
                x.Name.ToUpper().Contains(normalized)
                || x.FiscalIdentities.Any(identity => identity.Number.ToUpper().Contains(normalized)));
        }

        var total = await query.LongCountAsync(cancellationToken);
        var skip = (request.Page - 1) * request.PageSize;
        var records = await query
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Skip(skip)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PageResult<Party>(records.Select(Map).ToArray(), request.Page, request.PageSize, total);
    }

    public async Task SaveAsync(Party party, CancellationToken cancellationToken = default)
    {
        var record = await BaseQuery()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == party.OrganizationId && x.Id == party.Id,
                cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound,
                "party.not_found",
                "The requested party was not found.");

        var priorVersion = party.Version - 1;
        if (record.Version != priorVersion)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                "The party changed before this operation could be persisted.",
                conflictType: "stale_version",
                currentVersion: record.Version.ToString());
        }

        _dbContext.Entry(record).Property(x => x.Version).OriginalValue = priorVersion;
        record.Kind = (int)party.Kind;
        record.Name = party.Name;
        record.ResidenceCountry = party.ResidenceCountry;
        record.TaxResidenceCountry = party.TaxResidenceCountry;
        record.Active = party.Active;
        record.Version = party.Version;
        record.UpdatedAtUtc = DateTime.UtcNow;

        var desiredRoles = party.Roles.Select(x => (int)x).ToHashSet();
        foreach (var existing in record.Roles.Where(x => !desiredRoles.Contains(x.Role)).ToArray())
        {
            _dbContext.PartyRoles.Remove(existing);
        }

        var existingRoleCodes = record.Roles.Select(x => x.Role).ToHashSet();
        foreach (var role in desiredRoles.Where(x => !existingRoleCodes.Contains(x)))
        {
            record.Roles.Add(new V1PartyRoleRecord { PartyId = party.Id, Role = role });
        }

        var existingIdentities = record.FiscalIdentities.ToDictionary(x => x.Id);
        foreach (var identity in party.FiscalIdentities)
        {
            if (existingIdentities.TryGetValue(identity.Id, out var existing))
            {
                existing.TypeCode = identity.TypeCode;
                existing.Number = identity.Number;
                existing.IssuingCountry = identity.IssuingCountry;
                existing.ValidFromUtc = ToUtcDate(identity.ValidFrom);
                existing.ValidToUtc = ToUtcDate(identity.ValidTo);
                existing.Active = identity.Active;
            }
            else
            {
                record.FiscalIdentities.Add(MapIdentity(party.Id, identity));
            }
        }
    }

    public Task<bool> FiscalIdentityExistsAsync(
        string organizationId,
        string typeCode,
        string number,
        string issuingCountry,
        Guid? excludingPartyId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedType = typeCode.Trim().ToUpperInvariant();
        var normalizedNumber = number.Trim().ToUpperInvariant();
        var normalizedCountry = issuingCountry.Trim().ToUpperInvariant();

        return _dbContext.PartyFiscalIdentities.AnyAsync(
            x => x.Party.OrganizationId == organizationId
                 && x.TypeCode.ToUpper() == normalizedType
                 && x.Number.ToUpper() == normalizedNumber
                 && x.IssuingCountry == normalizedCountry
                 && (!excludingPartyId.HasValue || x.PartyId != excludingPartyId.Value),
            cancellationToken);
    }

    private IQueryable<V1PartyRecord> BaseQuery() =>
        _dbContext.Parties
            .Include(x => x.Roles)
            .Include(x => x.FiscalIdentities);

    private static V1PartyFiscalIdentityRecord MapIdentity(Guid partyId, PartyFiscalIdentity identity) =>
        new()
        {
            Id = identity.Id,
            PartyId = partyId,
            TypeCode = identity.TypeCode,
            Number = identity.Number,
            IssuingCountry = identity.IssuingCountry,
            ValidFromUtc = ToUtcDate(identity.ValidFrom),
            ValidToUtc = ToUtcDate(identity.ValidTo),
            Active = identity.Active
        };

    private static DateTime? ToUtcDate(DateOnly? value) =>
        value?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

    private static Party Map(V1PartyRecord record)
    {
        var identities = record.FiscalIdentities.Select(identity => PartyFiscalIdentity.Rehydrate(
            identity.Id,
            identity.TypeCode,
            identity.Number,
            identity.IssuingCountry,
            identity.ValidFromUtc.HasValue ? DateOnly.FromDateTime(identity.ValidFromUtc.Value) : null,
            identity.ValidToUtc.HasValue ? DateOnly.FromDateTime(identity.ValidToUtc.Value) : null,
            identity.Active));

        return Party.Rehydrate(
            record.Id,
            record.OrganizationId,
            (PartyKind)record.Kind,
            record.Name,
            record.ResidenceCountry,
            record.TaxResidenceCountry,
            record.Roles.Select(role => (PartyRole)role.Role),
            identities,
            record.Active,
            record.Version);
    }
}
