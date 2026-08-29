using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Common.Messaging;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfInboxStore : IInboxStore
{
    private const int InProgressState = 0;
    private const int CompletedState = 1;

    private readonly V1PersistenceDbContext _dbContext;

    public EfInboxStore(V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InboxReservationResult> TryReserveAsync(
        InboxReservation reservation,
        CancellationToken cancellationToken = default)
    {
        ValidateConsumer(reservation.Consumer);
        var messageIdHash = HashMessageId(reservation.MessageId);
        var existing = await _dbContext.InboxMessages.SingleOrDefaultAsync(
            x => x.Consumer == reservation.Consumer && x.MessageIdHash == messageIdHash,
            cancellationToken);

        if (existing is null)
        {
            _dbContext.InboxMessages.Add(new V1InboxMessageRecord
            {
                Consumer = reservation.Consumer,
                MessageIdHash = messageIdHash,
                PayloadHash = reservation.PayloadHash,
                CorrelationId = reservation.CorrelationId,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = reservation.ExpiresAt.UtcDateTime,
                State = InProgressState
            });

            return new InboxReservationResult(InboxReservationStatus.Acquired);
        }

        if (!string.Equals(existing.PayloadHash, reservation.PayloadHash, StringComparison.Ordinal))
        {
            return new InboxReservationResult(
                InboxReservationStatus.PayloadMismatch,
                existing.PayloadHash,
                existing.OutcomeCode);
        }

        if (existing.State == CompletedState)
        {
            return new InboxReservationResult(
                InboxReservationStatus.ExistingCompleted,
                existing.PayloadHash,
                existing.OutcomeCode);
        }

        if (existing.ExpiresAtUtc <= DateTime.UtcNow)
        {
            existing.CorrelationId = reservation.CorrelationId;
            existing.CreatedAtUtc = DateTime.UtcNow;
            existing.ExpiresAtUtc = reservation.ExpiresAt.UtcDateTime;
            return new InboxReservationResult(InboxReservationStatus.Acquired, existing.PayloadHash);
        }

        return new InboxReservationResult(
            InboxReservationStatus.ExistingInProgress,
            existing.PayloadHash);
    }

    public async Task CompleteAsync(
        InboxCompletion completion,
        CancellationToken cancellationToken = default)
    {
        ValidateConsumer(completion.Consumer);
        var messageIdHash = HashMessageId(completion.MessageId);
        var existing = await _dbContext.InboxMessages.SingleOrDefaultAsync(
            x => x.Consumer == completion.Consumer && x.MessageIdHash == messageIdHash,
            cancellationToken)
            ?? throw new InvalidOperationException("Cannot complete an inbox message that is not reserved.");

        if (!string.Equals(existing.PayloadHash, completion.PayloadHash, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Cannot complete an inbox message with a different payload hash.");
        }

        existing.State = CompletedState;
        existing.OutcomeCode = completion.OutcomeCode;
        existing.CorrelationId = completion.CorrelationId;
        existing.CompletedAtUtc = completion.CompletedAt.UtcDateTime;
    }

    public async Task AbandonAsync(
        string consumer,
        string messageId,
        string payloadHash,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        ValidateConsumer(consumer);
        var messageIdHash = HashMessageId(messageId);
        var existing = await _dbContext.InboxMessages.SingleOrDefaultAsync(
            x => x.Consumer == consumer && x.MessageIdHash == messageIdHash,
            cancellationToken);

        if (existing is null || existing.State == CompletedState)
        {
            return;
        }

        if (!string.Equals(existing.PayloadHash, payloadHash, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Cannot abandon an inbox message with a different payload hash.");
        }

        _dbContext.InboxMessages.Remove(existing);
    }

    private static string HashMessageId(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(messageId)));
    }

    private static void ValidateConsumer(string consumer)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumer);
        if (consumer.Length > 200)
        {
            throw new ArgumentOutOfRangeException(nameof(consumer), "Inbox consumer cannot exceed 200 characters.");
        }
    }
}
