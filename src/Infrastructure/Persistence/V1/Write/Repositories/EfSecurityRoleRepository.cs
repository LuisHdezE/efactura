using EFactura.Application.Common.Errors;
using EFactura.Application.IdentityAccess;
using EFactura.Domain.IdentityAccess;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfSecurityRoleRepository : ISecurityRoleRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfSecurityRoleRepository(V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<SecurityRole>> ListAsync(
        string organizationId,
        CancellationToken cancellationToken = default)
    {
        var records = await _dbContext.Set<V1SecurityRoleRecord>()
            .AsNoTracking()
            .Include(x => x.Permissions)
            .Where(x => x.OrganizationId == organizationId)
            .OrderBy(x => x.NormalizedName)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return records.Select(Map).ToArray();
    }

    public async Task<SecurityRole?> GetAsync(
        string organizationId,
        string roleId,
        CancellationToken cancellationToken = default)
    {
        var record = await _dbContext.Set<V1SecurityRoleRecord>()
            .AsNoTracking()
            .Include(x => x.Permissions)
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == roleId,
                cancellationToken);

        return record is null ? null : Map(record);
    }

    public Task<bool> NormalizedNameExistsAsync(
        string organizationId,
        string normalizedName,
        string? excludingRoleId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Set<V1SecurityRoleRecord>().AnyAsync(
            x => x.OrganizationId == organizationId
                 && x.NormalizedName == normalizedName
                 && (excludingRoleId == null || x.Id != excludingRoleId),
            cancellationToken);
    }

    public Task AddAsync(SecurityRole role, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var record = new V1SecurityRoleRecord
        {
            Id = role.Id,
            OrganizationId = role.OrganizationId,
            Name = role.Name,
            NormalizedName = NormalizeName(role.Name),
            Description = role.Description,
            Active = role.Active,
            Version = role.Version,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Permissions = role.Permissions
                .Select(permission => new V1SecurityRolePermissionRecord
                {
                    RoleId = role.Id,
                    PermissionCode = permission
                })
                .ToList()
        };

        _dbContext.Set<V1SecurityRoleRecord>().Add(record);
        return Task.CompletedTask;
    }

    public async Task SaveAsync(SecurityRole role, CancellationToken cancellationToken = default)
    {
        var roles = _dbContext.Set<V1SecurityRoleRecord>();
        var permissions = _dbContext.Set<V1SecurityRolePermissionRecord>();

        var record = await roles
            .Include(x => x.Permissions)
            .SingleOrDefaultAsync(
                x => x.OrganizationId == role.OrganizationId && x.Id == role.Id,
                cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound,
                "identity.role.not_found",
                "The requested role was not found.");

        var priorVersion = role.Version - 1;
        if (record.Version != priorVersion)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                "The role changed before this operation could be persisted.",
                conflictType: "stale_version",
                currentVersion: record.Version.ToString());
        }

        _dbContext.Entry(record).Property(x => x.Version).OriginalValue = priorVersion;
        record.Name = role.Name;
        record.NormalizedName = NormalizeName(role.Name);
        record.Description = role.Description;
        record.Active = role.Active;
        record.Version = role.Version;
        record.UpdatedAtUtc = DateTimeOffset.UtcNow;

        var desiredCodes = role.Permissions.ToHashSet(StringComparer.Ordinal);
        var existingByCode = record.Permissions.ToDictionary(x => x.PermissionCode, StringComparer.Ordinal);

        var removed = record.Permissions
            .Where(permission => !desiredCodes.Contains(permission.PermissionCode))
            .ToArray();
        permissions.RemoveRange(removed);
        foreach (var permission in removed)
            record.Permissions.Remove(permission);

        foreach (var permissionCode in desiredCodes.OrderBy(code => code, StringComparer.Ordinal))
        {
            if (existingByCode.ContainsKey(permissionCode))
                continue;

            record.Permissions.Add(new V1SecurityRolePermissionRecord
            {
                RoleId = role.Id,
                PermissionCode = permissionCode,
                Role = record
            });
        }
    }

    private static SecurityRole Map(V1SecurityRoleRecord record) =>
        SecurityRole.Rehydrate(
            record.Id,
            record.OrganizationId,
            record.Name,
            record.Description,
            record.Active,
            record.Permissions
                .Select(permission => permission.PermissionCode)
                .OrderBy(permission => permission, StringComparer.Ordinal),
            record.Version);

    private static string NormalizeName(string name) => name.Trim().ToUpperInvariant();
}
