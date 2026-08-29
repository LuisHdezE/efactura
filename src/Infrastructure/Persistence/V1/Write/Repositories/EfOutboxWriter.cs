using System.Text.Json;
using EFactura.Application.Common.Messaging;
using Infrastructure.Persistence.V1.Write.Models;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfOutboxWriter : IOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly V1PersistenceDbContext _dbContext;

    public EfOutboxWriter(V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task EnqueueAsync<TEvent>(
        TEvent integrationEvent,
        OutboxContext context,
        CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent
    {
        var eventType = integrationEvent.GetType();

        _dbContext.OutboxMessages.Add(new V1OutboxMessageRecord
        {
            EventId = integrationEvent.EventId,
            OccurredAtUtc = integrationEvent.OccurredAt.UtcDateTime,
            EventType = eventType.FullName ?? eventType.Name,
            PayloadJson = JsonSerializer.Serialize(integrationEvent, eventType, JsonOptions),
            CorrelationId = context.CorrelationId,
            CausationId = context.CausationId,
            OrganizationId = context.OrganizationId,
            ActorId = context.ActorId,
            CreatedAtUtc = DateTime.UtcNow,
            State = 0,
            AttemptCount = 0
        });

        return Task.CompletedTask;
    }
}
