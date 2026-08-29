namespace Infrastructure.Persistence.V1.Write.Models;

public sealed class V1AuditEventRecord
{
    public Guid EventId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string? OrganizationId { get; set; }
    public string? LocationId { get; set; }
    public string? TerminalId { get; set; }
    public string? TargetType { get; set; }
    public string? TargetId { get; set; }
    public int Outcome { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? CausationId { get; set; }
    public string MetadataJson { get; set; } = "{}";
}

public sealed class V1IdempotencyRecord
{
    public string Scope { get; set; } = string.Empty;
    public string KeyHash { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public int State { get; set; }
    public string? OutcomeCode { get; set; }
    public string? ResourceType { get; set; }
    public string? ResourceId { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}

public sealed class V1OutboxMessageRecord
{
    public Guid EventId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string? CausationId { get; set; }
    public string? OrganizationId { get; set; }
    public string? ActorId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public int State { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? NextAttemptAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public string? LastErrorCode { get; set; }
}

public sealed class V1InboxMessageRecord
{
    public string Consumer { get; set; } = string.Empty;
    public string MessageIdHash { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public int State { get; set; }
    public string? OutcomeCode { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
