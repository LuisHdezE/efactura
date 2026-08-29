using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Common.Idempotency;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfIdempotencyStore : IIdempotencyStore
{
    private const int InProgressState = 0;
    private const int CompletedState = 1;

    private readonly V1PersistenceDbContext _dbContext;

    public EfIdempotencyStore(V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IdempotencyReservationResult> TryReserveAsync(
        IdempotencyReservation reservation,
        CancellationToken cancellationToken = default)
    {
        ValidateScope(reservation.Scope);
        var keyHash = HashKey(reservation.Key);
        var existing = await _dbContext.IdempotencyRecords.SingleOrDefaultAsync(
            x => x.Scope == reservation.Scope && x.KeyHash == keyHash,
            cancellationToken);

        if (existing is null)
        {
            _dbContext.IdempotencyRecords.Add(new V1IdempotencyRecord
            {
                Scope = reservation.Scope,
                KeyHash = keyHash,
                RequestHash = reservation.RequestHash,
                ActorId = reservation.ActorId,
                CorrelationId = reservation.CorrelationId,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = reservation.ExpiresAt.UtcDateTime,
                State = InProgressState
            });

            return new IdempotencyReservationResult(IdempotencyReservationStatus.Acquired);
        }

        if (!string.Equals(existing.RequestHash, reservation.RequestHash, StringComparison.Ordinal))
        {
            return new IdempotencyReservationResult(
                IdempotencyReservationStatus.PayloadMismatch,
                existing.RequestHash,
                existing.OutcomeCode,
                existing.ResourceType,
                existing.ResourceId);
        }

        if (existing.State == CompletedState)
        {
            return new IdempotencyReservationResult(
                IdempotencyReservationStatus.ExistingCompleted,
                existing.RequestHash,
                existing.OutcomeCode,
                existing.ResourceType,
                existing.ResourceId);
        }

        if (existing.ExpiresAtUtc <= DateTime.UtcNow)
        {
            existing.ActorId = reservation.ActorId;
            existing.CorrelationId = reservation.CorrelationId;
            existing.CreatedAtUtc = DateTime.UtcNow;
            existing.ExpiresAtUtc = reservation.ExpiresAt.UtcDateTime;
            return new IdempotencyReservationResult(IdempotencyReservationStatus.Acquired, existing.RequestHash);
        }

        return new IdempotencyReservationResult(
            IdempotencyReservationStatus.ExistingInProgress,
            existing.RequestHash);
    }

    public async Task CompleteAsync(
        IdempotencyCompletion completion,
        CancellationToken cancellationToken = default)
    {
        ValidateScope(completion.Scope);
        var keyHash = HashKey(completion.Key);
        var existing = await _dbContext.IdempotencyRecords.SingleOrDefaultAsync(
            x => x.Scope == completion.Scope && x.KeyHash == keyHash,
            cancellationToken)
            ?? throw new InvalidOperationException("Cannot complete an idempotency key that is not reserved.");

        if (!string.Equals(existing.RequestHash, completion.RequestHash, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Cannot complete an idempotency key with a different request hash.");
        }

        existing.State = CompletedState;
        existing.OutcomeCode = completion.OutcomeCode;
        existing.ResourceType = completion.ResourceType;
        existing.ResourceId = completion.ResourceId;
        existing.CorrelationId = completion.CorrelationId;
        existing.CompletedAtUtc = completion.CompletedAt.UtcDateTime;
    }

    public async Task AbandonAsync(
        string scope,
        string key,
        string requestHash,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ValidateScope(scope);
        var keyHash = HashKey(key);
        var existing = await _dbContext.IdempotencyRecords.SingleOrDefaultAsync(
            x => x.Scope == scope && x.KeyHash == keyHash,
            cancellationToken);

        if (existing is null || existing.State == CompletedState)
        {
            return;
        }

        if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Cannot abandon an idempotency key with a different request hash.");
        }

        _dbContext.IdempotencyRecords.Remove(existing);
    }

    private static string HashKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
    }

    private static void ValidateScope(string scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);
        if (scope.Length > 160)
        {
            throw new ArgumentOutOfRangeException(nameof(scope), "Idempotency scope cannot exceed 160 characters.");
        }
    }
}
