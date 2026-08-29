namespace EFactura.Application.Common.Messaging;

public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTimeOffset OccurredAt { get; }
}

public sealed record OutboxContext(
    string CorrelationId,
    string? CausationId = null,
    string? OrganizationId = null,
    string? ActorId = null);

public interface IOutboxWriter
{
    Task EnqueueAsync<TEvent>(
        TEvent integrationEvent,
        OutboxContext context,
        CancellationToken cancellationToken = default)
        where TEvent : IIntegrationEvent;
}
