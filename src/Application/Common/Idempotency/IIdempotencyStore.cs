namespace EFactura.Application.Common.Idempotency;

public enum IdempotencyReservationStatus
{
    Acquired,
    ExistingCompleted,
    ExistingInProgress,
    PayloadMismatch
}

public sealed record IdempotencyReservation(
    string Scope,
    string Key,
    string RequestHash,
    string? ActorId,
    string CorrelationId,
    DateTimeOffset ExpiresAt);

public sealed record IdempotencyReservationResult(
    IdempotencyReservationStatus Status,
    string? OriginalRequestHash = null,
    string? OutcomeCode = null,
    string? ResourceType = null,
    string? ResourceId = null);

public sealed record IdempotencyCompletion(
    string Scope,
    string Key,
    string RequestHash,
    string OutcomeCode,
    string? ResourceType,
    string? ResourceId,
    string CorrelationId,
    DateTimeOffset CompletedAt);

public interface IIdempotencyStore
{
    Task<IdempotencyReservationResult> TryReserveAsync(
        IdempotencyReservation reservation,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        IdempotencyCompletion completion,
        CancellationToken cancellationToken = default);

    Task AbandonAsync(
        string scope,
        string key,
        string requestHash,
        string correlationId,
        CancellationToken cancellationToken = default);
}
