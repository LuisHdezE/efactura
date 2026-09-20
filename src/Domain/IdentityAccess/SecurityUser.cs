using EFactura.Domain.Common;

namespace EFactura.Domain.IdentityAccess;

public sealed class SecurityUser
{
    private readonly List<string> _locationScopes;
    private readonly List<string> _terminalScopes;
    private readonly List<string> _roleIds;

    private SecurityUser(
        string id,
        string organizationId,
        string identityProvider,
        string externalSubject,
        string displayName,
        string? email,
        bool active,
        IEnumerable<string> locationScopes,
        IEnumerable<string> terminalScopes,
        IEnumerable<string> roleIds,
        long version)
    {
        Id = Required(id, 200, "identity.user.id_required");
        OrganizationId = Required(organizationId, 200, "identity.user.organization_required");
        IdentityProvider = Required(identityProvider, 120, "identity.user.identity_provider_required");
        ExternalSubject = Required(externalSubject, 300, "identity.user.external_subject_required");
        DisplayName = Required(displayName, 200, "identity.user.display_name_required");
        Email = OptionalEmail(email);
        Active = active;
        _locationScopes = NormalizeIds(locationScopes, "identity.user.location_scope_invalid");
        _terminalScopes = NormalizeIds(terminalScopes, "identity.user.terminal_scope_invalid");
        _roleIds = NormalizeIds(roleIds, "identity.user.role_invalid");

        if (version <= 0)
            throw Rule("identity.user.version_invalid", "User version must be positive.");

        Version = version;
    }

    public string Id { get; }
    public string OrganizationId { get; }
    public string IdentityProvider { get; }
    public string ExternalSubject { get; }
    public string DisplayName { get; private set; }
    public string? Email { get; private set; }
    public bool Active { get; private set; }
    public IReadOnlyList<string> LocationScopes => _locationScopes;
    public IReadOnlyList<string> TerminalScopes => _terminalScopes;
    public IReadOnlyList<string> RoleIds => _roleIds;
    public long Version { get; private set; }

    public static SecurityUser Create(
        string id,
        string organizationId,
        string identityProvider,
        string externalSubject,
        string displayName,
        string? email,
        IEnumerable<string> locationScopes,
        IEnumerable<string> terminalScopes) =>
        new(
            id,
            organizationId,
            identityProvider,
            externalSubject,
            displayName,
            email,
            true,
            locationScopes,
            terminalScopes,
            Array.Empty<string>(),
            1);

    public static SecurityUser Rehydrate(
        string id,
        string organizationId,
        string identityProvider,
        string externalSubject,
        string displayName,
        string? email,
        bool active,
        IEnumerable<string> locationScopes,
        IEnumerable<string> terminalScopes,
        IEnumerable<string> roleIds,
        long version) =>
        new(
            id,
            organizationId,
            identityProvider,
            externalSubject,
            displayName,
            email,
            active,
            locationScopes,
            terminalScopes,
            roleIds,
            version);

    public void Patch(
        string? displayName,
        string? email,
        bool? active,
        IEnumerable<string>? locationScopes,
        IEnumerable<string>? terminalScopes,
        long expectedVersion)
    {
        EnsureVersion(expectedVersion);

        if (displayName is not null)
            DisplayName = Required(displayName, 200, "identity.user.display_name_required");

        if (email is not null)
            Email = OptionalEmail(email);

        if (active.HasValue)
            Active = active.Value;

        if (locationScopes is not null)
        {
            _locationScopes.Clear();
            _locationScopes.AddRange(NormalizeIds(locationScopes, "identity.user.location_scope_invalid"));
        }

        if (terminalScopes is not null)
        {
            _terminalScopes.Clear();
            _terminalScopes.AddRange(NormalizeIds(terminalScopes, "identity.user.terminal_scope_invalid"));
        }

        Version++;
    }

    public void ReplaceRoles(IEnumerable<string> roleIds, long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        _roleIds.Clear();
        _roleIds.AddRange(NormalizeIds(roleIds, "identity.user.role_invalid"));
        Version++;
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw Rule("concurrency.stale_version", "The user changed before this operation was applied.");
    }

    private static List<string> NormalizeIds(IEnumerable<string> values, string code)
    {
        ArgumentNullException.ThrowIfNull(values);

        return values
            .Select(value => Required(value, 200, code))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();
    }

    private static string Required(string value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule(code, "Required user value is missing.");

        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule(code, $"User value cannot exceed {max} characters.");

        return normalized;
    }

    private static string? OptionalEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();
        if (normalized.Length > 320 || !normalized.Contains('@', StringComparison.Ordinal))
            throw Rule("identity.user.email_invalid", "User email metadata is invalid.");

        return normalized;
    }

    private static DomainRuleException Rule(string code, string message) => new(code, message);
}
