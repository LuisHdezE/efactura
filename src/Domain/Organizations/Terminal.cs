using EFactura.Domain.Common;

namespace EFactura.Domain.Organizations;

/// <summary>
/// Organization-owned operational POS terminal master data.
/// Device enrollment, sync credentials and hardware identity are deliberately outside this aggregate.
/// </summary>
public sealed class Terminal
{
    private Terminal(
        string id,
        string organizationId,
        string code,
        string name,
        string locationId,
        bool active,
        long version)
    {
        Id = Required(id, 200, "organization.terminal.id_required", "Terminal id is required.");
        OrganizationId = Required(organizationId, 200, "organization.terminal.organization_required", "Terminal organization is required.");
        Code = NormalizeCode(code);
        Name = Required(name, 120, "organization.terminal.name_required", "Terminal name is required.");
        LocationId = Required(locationId, 200, "organization.terminal.location_required", "Terminal location is required.");

        if (version <= 0)
            throw Rule("organization.terminal.version_invalid", "Terminal version must be positive.");

        Active = active;
        Version = version;
    }

    public string Id { get; }
    public string OrganizationId { get; }
    public string Code { get; }
    public string Name { get; private set; }
    public string LocationId { get; private set; }
    public bool Active { get; private set; }
    public long Version { get; private set; }

    public static Terminal Create(
        string id,
        string organizationId,
        string code,
        string name,
        string locationId) =>
        new(id, organizationId, code, name, locationId, true, 1);

    public static Terminal Rehydrate(
        string id,
        string organizationId,
        string code,
        string name,
        string locationId,
        bool active,
        long version) =>
        new(id, organizationId, code, name, locationId, active, version);

    public void Update(
        string name,
        string locationId,
        bool active,
        long expectedVersion)
    {
        EnsureVersion(expectedVersion);
        Name = Required(name, 120, "organization.terminal.name_required", "Terminal name is required.");
        LocationId = Required(locationId, 200, "organization.terminal.location_required", "Terminal location is required.");
        Active = active;
        Version++;
    }

    public static string NormalizeCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule("organization.terminal.code_required", "Terminal code is required.");

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length > 64 || normalized.Any(ch => !IsAllowedCodeCharacter(ch)))
        {
            throw Rule(
                "organization.terminal.code_invalid",
                "Terminal code may contain at most 64 characters from A-Z, 0-9, '.', '_' and '-'.");
        }

        return normalized;
    }

    private void EnsureVersion(long expectedVersion)
    {
        if (expectedVersion <= 0)
            throw Rule("organization.terminal.version_invalid", "Expected terminal version must be positive.");

        if (Version != expectedVersion)
            throw Rule("concurrency.stale_version", "The terminal changed before this operation was applied.");
    }

    private static bool IsAllowedCodeCharacter(char ch) =>
        ch is >= 'A' and <= 'Z'
        || ch is >= '0' and <= '9'
        || ch is '.' or '_' or '-';

    private static string Required(string value, int max, string code, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Rule(code, message);

        var normalized = value.Trim();
        if (normalized.Length > max)
            throw Rule(code, $"{message.TrimEnd('.')} and cannot exceed {max} characters.");

        return normalized;
    }

    private static DomainRuleException Rule(string code, string message) => new(code, message);
}
