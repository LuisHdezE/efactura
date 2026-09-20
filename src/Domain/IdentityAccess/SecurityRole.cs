using EFactura.Domain.Common;

namespace EFactura.Domain.IdentityAccess;

public sealed class SecurityRole
{
    private readonly List<string> _permissions;

    private SecurityRole(
        string id,
        string organizationId,
        string name,
        string? description,
        bool active,
        IEnumerable<string> permissions,
        long version)
    {
        Id = Required(id, 200, "identity.role.id_required");
        OrganizationId = Required(organizationId, 200, "identity.role.organization_required");
        Name = Required(name, 120, "identity.role.name_required");
        Description = Optional(description, 500);
        Active = active;
        _permissions = NormalizePermissions(permissions);

        if (version <= 0)
            throw Rule("identity.role.version_invalid", "Role version must be positive.");

        Version = version;
    }

    public string Id { get; }
    public string OrganizationId { get; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public bool Active { get; private set; }
    public IReadOnlyList<string> Permissions => _permissions;
    public long Version { get; private set; }

    public static SecurityRole Create(
        string id,
        string organizationId,
        string name,
        string? description,
        IEnumerable<string> permissions) =>
        new(id, organizationId, name, description, true, permissions, 1);

    public static SecurityRole Rehydrate(
        string id,
        string organizationId,
        string name,
        string? description,
        bool active,
        IEnumerable<string> permissions,
        long version) =>
        new(id, organizationId, name, description, active, permissions, version);

    public void Replace(
        string name,
        string? description,
        bool active,
        IEnumerable<string> permissions,
        long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        Name = Required(name, 120, "identity.role.name_required");
        Description = Optional(description, 500);
        Active = active;
        _permissions.Clear();
        _permissions.AddRange(NormalizePermissions(permissions));
        Version++;
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw Rule("concurrency.stale_version", "The role changed before this operation was applied.");
    }

    private static List<string> NormalizePermissions(IEnumerable<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        return permissions
            .Select(permission => Required(permission, 160, "identity.role.permission_invalid"))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(permission => permission, StringComparer.Ordinal)
            .ToList();
    }

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule(code, "Required role value is missing.");

        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule(code, $"Role value cannot exceed {max} characters.");

        return normalized;
    }

    private static string? Optional(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule("identity.role.description_too_long", $"Role description cannot exceed {max} characters.");

        return normalized;
    }

    private static DomainRuleException Rule(string code, string message) => new(code, message);
}
