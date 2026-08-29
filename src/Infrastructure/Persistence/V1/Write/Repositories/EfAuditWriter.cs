using System.Text.Json;
using EFactura.Application.Common.Auditing;
using Infrastructure.Persistence.V1.Write.Models;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfAuditWriter : IAuditWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly V1PersistenceDbContext _dbContext;

    public EfAuditWriter(V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        var metadata = new SortedDictionary<string, string?>(StringComparer.Ordinal);
        foreach (var pair in auditEvent.Metadata)
        {
            metadata[pair.Key] = pair.Value;
        }

        _dbContext.AuditEvents.Add(new V1AuditEventRecord
        {
            EventId = auditEvent.EventId,
            OccurredAtUtc = auditEvent.OccurredAt.UtcDateTime,
            EventName = auditEvent.EventName,
            ActorId = auditEvent.ActorId,
            OrganizationId = auditEvent.OrganizationId,
            LocationId = auditEvent.LocationId,
            TerminalId = auditEvent.TerminalId,
            TargetType = auditEvent.TargetType,
            TargetId = auditEvent.TargetId,
            Outcome = (int)auditEvent.Outcome,
            CorrelationId = auditEvent.CorrelationId,
            CausationId = auditEvent.CausationId,
            MetadataJson = JsonSerializer.Serialize(metadata, JsonOptions)
        });

        return Task.CompletedTask;
    }
}
