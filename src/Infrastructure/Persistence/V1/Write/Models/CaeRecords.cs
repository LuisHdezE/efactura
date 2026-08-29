namespace Infrastructure.Persistence.V1.Write.Models;

public sealed class V1CaeAuthorizationRecord
{
    public Guid Id { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public int CfeType { get; set; }
    public string AuthorizationNumber { get; set; } = string.Empty;
    public string Series { get; set; } = string.Empty;
    public long RangeFrom { get; set; }
    public long RangeTo { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly ValidTo { get; set; }
    public int Status { get; set; }
    public string VerificationMethod { get; set; } = string.Empty;
    public string SourceArtifactId { get; set; } = string.Empty;
    public string SourceArtifactHash { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public long NextNumber { get; set; }
    public long Version { get; set; }
    public DateTimeOffset ImportedAtUtc { get; set; }
    public DateTimeOffset? ActivatedAtUtc { get; set; }
    public List<V1CaeAllocationRecord> Allocations { get; set; } = new();
    public List<V1FiscalNumberReservationRecord> Reservations { get; set; } = new();
}

public sealed class V1CaeAllocationRecord
{
    public Guid Id { get; set; }
    public Guid CaeAuthorizationId { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public string? TerminalId { get; set; }
    public long RangeFrom { get; set; }
    public long RangeTo { get; set; }
    public long NextNumber { get; set; }
    public int Status { get; set; }
    public long Version { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public V1CaeAuthorizationRecord Authorization { get; set; } = null!;
}

public sealed class V1FiscalNumberReservationRecord
{
    public Guid Id { get; set; }
    public Guid CaeAuthorizationId { get; set; }
    public Guid? AllocationId { get; set; }
    public string OrganizationId { get; set; } = string.Empty;
    public int CfeType { get; set; }
    public string Series { get; set; } = string.Empty;
    public long Number { get; set; }
    public string? LocationId { get; set; }
    public string? TerminalId { get; set; }
    public string OperationId { get; set; } = string.Empty;
    public DateTimeOffset ReservedAtUtc { get; set; }
    public V1CaeAuthorizationRecord Authorization { get; set; } = null!;
}
