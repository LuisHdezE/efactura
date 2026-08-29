namespace EFactura.Application.Common.Auditing;

public enum AuditOutcome
{
    Requested,
    Succeeded,
    Failed,
    Denied
}

public sealed record AuditEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    string EventName,
    string? ActorId,
    string? OrganizationId,
    string? LocationId,
    string? TerminalId,
    string? TargetType,
    string? TargetId,
    AuditOutcome Outcome,
    string CorrelationId,
    string? CausationId,
    IReadOnlyDictionary<string, string?> Metadata);

public interface IAuditWriter
{
    Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default);
}
