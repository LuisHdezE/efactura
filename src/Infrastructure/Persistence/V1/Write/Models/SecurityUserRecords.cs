namespace Infrastructure.Persistence.V1.Write.Models;

public sealed class V1SecurityUserRecord
{
    public string Id { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public string IdentityProvider { get; set; } = string.Empty;
    public string NormalizedIdentityProvider { get; set; } = string.Empty;
    public string ExternalSubject { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool Active { get; set; }
    public long Version { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public List<V1SecurityUserLocationScopeRecord> LocationScopes { get; set; } = new();
    public List<V1SecurityUserTerminalScopeRecord> TerminalScopes { get; set; } = new();
    public List<V1SecurityUserRoleRecord> Roles { get; set; } = new();
}

public sealed class V1SecurityUserLocationScopeRecord
{
    public string UserId { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public V1SecurityUserRecord User { get; set; } = null!;
}

public sealed class V1SecurityUserTerminalScopeRecord
{
    public string UserId { get; set; } = string.Empty;
    public string TerminalId { get; set; } = string.Empty;
    public V1SecurityUserRecord User { get; set; } = null!;
}

public sealed class V1SecurityUserRoleRecord
{
    public string UserId { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public V1SecurityUserRecord User { get; set; } = null!;
    public V1SecurityRoleRecord Role { get; set; } = null!;
}
