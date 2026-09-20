using EFactura.Application.Common.Errors;
using EFactura.Application.IdentityAccess;
using EFactura.Domain.IdentityAccess;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfSecurityUserRepository : ISecurityUserRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfSecurityUserRepository(V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<SecurityUser>> ListAsync(
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        var records = await Query()
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .OrderBy(x => x.DisplayName)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return records.Select(Map).ToArray();
    }

    public async Task<SecurityUser?> GetAsync(
        string organizationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var record = await Query()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == userId,
                cancellationToken);

        return record is null ? null : Map(record);
    }

    public Task<bool> IdentityLinkExistsAsync(
        string organizationId,
        string normalizedIdentityProvider,
        string externalSubject,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<V1SecurityUserRecord>().AnyAsync(
            x => x.OrganizationId == organizationId
                 && x.NormalizedIdentityProvider == normalizedIdentityProvider
                 && x.ExternalSubject == externalSubject,
            cancellationToken);
    }

    public Task AddAsync(SecurityUser user, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var record = new V1SecurityUserRecord
        {
            Id = user.Id,
            OrganizationId = user.OrganizationId,
            IdentityProvider = user.IdentityProvider,
            NormalizedIdentityProvider = NormalizeProvider(user.IdentityProvider),
            ExternalSubject = user.ExternalSubject,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Active = user.Active,
            Version = user.Version,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            LocationScopes = user.LocationScopes
                .Select(locationId => new V1SecurityUserLocationScopeRecord
                {
                    UserId = user.Id,
                    LocationId = locationId
                })
                .ToList(),
            TerminalScopes = user.TerminalScopes
                .Select(terminalId => new V1SecurityUserTerminalScopeRecord
                {
                    UserId = user.Id,
                    TerminalId = terminalId
                })
                .ToList(),
            Roles = user.RoleIds
                .Select(roleId => new V1SecurityUserRoleRecord
                {
                    UserId = user.Id,
                    RoleId = roleId
                })
                .ToList()
        };

        _dbContext.Set<V1SecurityUserRecord>().Add(record);
        return Task.CompletedTask;
    }

    public async Task SaveAsync(SecurityUser user, CancellationToken cancellationToken = default)
    {
        var record = await Query()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == user.OrganizationId && x.Id == user.Id,
                cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound,
                "identity.user.not_found",
                "The requested user was not found.");

        var priorVersion = user.Version - 1;
        if (record.Version != priorVersion)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                "The user changed before this operation could be persisted.",
                conflictType: "stale_version",
                currentVersion: record.Version.ToString());
        }

        _dbContext.Entry(record).Property(x => x.Version).OriginalValue = priorVersion;
        record.DisplayName = user.DisplayName;
        record.Email = user.Email;
        record.Active = user.Active;
        record.Version = user.Version;
        record.UpdatedAtUtc = DateTimeOffset.UtcNow;

        ReplaceLocationScopes(record, user.LocationScopes);
        ReplaceTerminalScopes(record, user.TerminalScopes);
        ReplaceRoles(record, user.RoleIds);
    }

    private IQueryable<V1SecurityUserRecord> Query() =>
        _dbContext.Set<V1SecurityUserRecord>()
            .Include(x => x.LocationScopes)
            .Include(x => x.TerminalScopes)
            .Include(x => x.Roles);

    private void ReplaceLocationScopes(V1SecurityUserRecord record, IReadOnlyCollection<string> desired)
    {
        var set = _dbContext.Set<V1SecurityUserLocationScopeRecord>();
        var desiredSet = desired.ToHashSet(StringComparer.Ordinal);
        var removed = record.LocationScopes.Where(x => !desiredSet.Contains(x.LocationId)).ToArray();
        set.RemoveRange(removed);
        foreach (var item in removed)
            record.LocationScopes.Remove(item);

        var existing = record.LocationScopes.Select(x => x.LocationId).ToHashSet(StringComparer.Ordinal);
        foreach (var locationId in desired.OrderBy(x => x, StringComparer.Ordinal))
        {
            if (existing.Contains(locationId))
                continue;
            record.LocationScopes.Add(new V1SecurityUserLocationScopeRecord
            {
                UserId = record.Id,
                LocationId = locationId,
                User = record
            });
        }
    }

    private void ReplaceTerminalScopes(V1SecurityUserRecord record, IReadOnlyCollection<string> desired)
    {
        var set = _dbContext.Set<V1SecurityUserTerminalScopeRecord>();
        var desiredSet = desired.ToHashSet(StringComparer.Ordinal);
        var removed = record.TerminalScopes.Where(x => !desiredSet.Contains(x.TerminalId)).ToArray();
        set.RemoveRange(removed);
        foreach (var item in removed)
            record.TerminalScopes.Remove(item);

        var existing = record.TerminalScopes.Select(x => x.TerminalId).ToHashSet(StringComparer.Ordinal);
        foreach (var terminalId in desired.OrderBy(x => x, StringComparer.Ordinal))
        {
            if (existing.Contains(terminalId))
                continue;
            record.TerminalScopes.Add(new V1SecurityUserTerminalScopeRecord
            {
                UserId = record.Id,
                TerminalId = terminalId,
                User = record
            });
        }
    }

    private void ReplaceRoles(V1SecurityUserRecord record, IReadOnlyCollection<string> desired)
    {
        var set = _dbContext.Set<V1SecurityUserRoleRecord>();
        var desiredSet = desired.ToHashSet(StringComparer.Ordinal);
        var removed = record.Roles.Where(x => !desiredSet.Contains(x.RoleId)).ToArray();
        set.RemoveRange(removed);
        foreach (var item in removed)
            record.Roles.Remove(item);

        var existing = record.Roles.Select(x => x.RoleId).ToHashSet(StringComparer.Ordinal);
        foreach (var roleId in desired.OrderBy(x => x, StringComparer.Ordinal))
        {
            if (existing.Contains(roleId))
                continue;
            record.Roles.Add(new V1SecurityUserRoleRecord
            {
                UserId = record.Id,
                RoleId = roleId,
                User = record
            });
        }
    }

    private static SecurityUser Map(V1SecurityUserRecord record) =>
        SecurityUser.Rehydrate(
            record.Id,
            record.OrganizationId,
            record.IdentityProvider,
            record.ExternalSubject,
            record.DisplayName,
            record.Email,
            record.Active,
            record.LocationScopes.Select(x => x.LocationId).OrderBy(x => x, StringComparer.Ordinal),
            record.TerminalScopes.Select(x => x.TerminalId).OrderBy(x => x, StringComparer.Ordinal),
            record.Roles.Select(x => x.RoleId).OrderBy(x => x, StringComparer.Ordinal),
            record.Version);

    private static string NormalizeProvider(string identityProvider) => identityProvider.Trim().ToUpperInvariant();
}
