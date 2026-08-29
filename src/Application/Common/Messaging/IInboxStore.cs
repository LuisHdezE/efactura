namespace EFactura.Application.Common.Messaging;

public enum InboxReservationStatus
{
    Acquired,
    ExistingCompleted,
    ExistingInProgress,
    PayloadMismatch
}

public sealed record InboxReservation(
    string Consumer,
    string MessageId,
    string PayloadHash,
    string CorrelationId,
    DateTimeOffset ExpiresAt);

public sealed record InboxReservationResult(
    InboxReservationStatus Status,
    string? OriginalPayloadHash = null,
    string? OutcomeCode = null);

public sealed record InboxCompletion(
    string Consumer,
    string MessageId,
    string PayloadHash,
    string OutcomeCode,
    string CorrelationId,
    DateTimeOffset CompletedAt);

public interface IInboxStore
{
    Task<InboxReservationResult> TryReserveAsync(
        InboxReservation reservation,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        InboxCompletion completion,
        CancellationToken cancellationToken = default);

    Task AbandonAsync(
        string consumer,
        string messageId,
        string payloadHash,
        string correlationId,
        CancellationToken cancellationToken = default);
}
