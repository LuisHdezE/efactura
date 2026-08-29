using EFactura.Application.Parties;
using EFactura.Domain.Parties;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfPartyRepository : IPartyRepository
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
            FiscalIdentities = party.FiscalIdentities.Select(identity => new V1PartyFiscalIdentityRecord
            {
                Id = identity.Id,
                PartyId = party.Id,
                TypeCode = identity.TypeCode,
                Number = identity.Number,
                IssuingCountry = identity.IssuingCountry,
                ValidFromUtc = identity.ValidFrom?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                ValidToUtc = identity.ValidTo?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                Active = identity.Active
            }).ToList()
        };

        _dbContext.Parties.Add(record);
        return Task.CompletedTask;
    }

    public async Task<Party?> GetAsync(
        string organizationId,
        Guid partyId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Parties
            .AsNoTracking()
            .Include(x => x.Roles)
            .Include(x => x.FiscalIdentities)
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == partyId,
                cancellationToken);

        return record is null ? null : Map(record);
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
