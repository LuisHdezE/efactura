namespace Infrastructure.Persistence.V1.Write.Models;

public sealed class V1SecurityRoleRecord
{
    public string Id { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Active { get; set; }
    public long Version { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public List<V1SecurityRolePermissionRecord> Permissions { get; set; } = new();
}

public sealed class V1SecurityRolePermissionRecord
{
    public string RoleId { get; set; } = string.Empty;
    public string PermissionCode { get; set; } = string.Empty;
    public V1SecurityRoleRecord Role { get; set; } = null!;
}
