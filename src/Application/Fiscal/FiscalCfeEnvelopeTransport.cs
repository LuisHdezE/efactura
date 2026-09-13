using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Persistence;

namespace EFactura.Application.Fiscal;

public enum FiscalCfeEnvelopeSubmissionState
{
    Prepared = 1,
    InFlight = 2,
    ResponseReceived = 3,
    Unknown = 4
}

public sealed record StoredFiscalCfeEnvelopeSubmission(
    Guid Id,
    Guid EnvelopeId,
    string OrganizationId,
    string IssuerRuc,
    string ReceiverRut,
    long SenderEnvelopeId,
    string OperationId,
    string EnvelopeSha256,
    FiscalCfeEnvelopeSubmissionState State,
    int AttemptCount,
    DateTimeOffset PreparedAtUtc,
    DateTimeOffset? LastAttemptAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? ResponseXml,
    string? ResponseSha256,
    string? FailureCode);

public interface IFiscalCfeEnvelopeSubmissionRepository
{
    Task<StoredFiscalCfeEnvelopeSubmission?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default);

    Task<StoredFiscalCfeEnvelopeSubmission?> GetByEnvelopeIdAsync(
        Guid envelopeId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        StoredFiscalCfeEnvelopeSubmission submission,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        StoredFiscalCfeEnvelopeSubmission submission,
        CancellationToken cancellationToken = default);
}

public interface IFiscalCfeEnvelopeTransportClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed record FiscalCfeEnvelopeTransportRequest(
    string OrganizationId,
    string IssuerRuc,
    string ReceiverRut,
    long SenderEnvelopeId,
    string EnvelopeSha256,
    string EnvelopeXml);

public sealed record FiscalCfeEnvelopeTransportResponse(string ResponseXml);

public interface IFiscalCfeEnvelopeTransportGateway
{
    Task<FiscalCfeEnvelopeTransportResponse> SendAsync(
        FiscalCfeEnvelopeTransportRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class FiscalCfeEnvelopeTransportException : Exception
{
    public FiscalCfeEnvelopeTransportException(
        string code,
        string message,
        bool deliveryAmbiguous,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        DeliveryAmbiguous = deliveryAmbiguous;
    }

    public string Code { get; }
    public bool DeliveryAmbiguous { get; }
}

public sealed record PrepareFiscalCfeEnvelopeSubmissionCommand(
    string OrganizationId,
    string IssuerRuc,
    string ReceiverRut,
    long SenderEnvelopeId,
    string OperationId);

public sealed record DispatchFiscalCfeEnvelopeSubmissionCommand(
    string OrganizationId,
    string IssuerRuc,
    string ReceiverRut,
    long SenderEnvelopeId);

public sealed record FiscalCfeEnvelopeSubmissionResult(
    Guid SubmissionId,
    Guid EnvelopeId,
    string OrganizationId,
    string IssuerRuc,
    string ReceiverRut,
    long SenderEnvelopeId,
    string OperationId,
    string EnvelopeSha256,
    FiscalCfeEnvelopeSubmissionState State,
    int AttemptCount,
    string? ResponseXml,
    string? ResponseSha256,
    string? FailureCode,
    bool Replayed);

public sealed class PrepareFiscalCfeEnvelopeSubmissionUseCase
{
    private readonly IFiscalCfeEnvelopeRepository _envelopes;
    private readonly IFiscalCfeEnvelopeSubmissionRepository _submissions;
    private readonly IFiscalCfeEnvelopePersistenceConflictClassifier _conflicts;
    private readonly IFiscalCfeEnvelopeTransportClock _clock;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;

    public PrepareFiscalCfeEnvelopeSubmissionUseCase(
        IFiscalCfeEnvelopeRepository envelopes,
        IFiscalCfeEnvelopeSubmissionRepository submissions,
        IFiscalCfeEnvelopePersistenceConflictClassifier conflicts,
        IFiscalCfeEnvelopeTransportClock clock,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork)
    {
        _envelopes = envelopes;
        _submissions = submissions;
        _conflicts = conflicts;
        _clock = clock;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
    }

    public async Task<FiscalCfeEnvelopeSubmissionResult> ExecuteAsync(
        PrepareFiscalCfeEnvelopeSubmissionCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command.OrganizationId, command.IssuerRuc, command.ReceiverRut, command.SenderEnvelopeId);
        if (string.IsNullOrWhiteSpace(command.OperationId) || command.OperationId.Trim().Length > 120)
            throw Validation("fiscal.envelope.transport.operation_id_invalid", "Operation id is required and must not exceed 120 characters.");

        var normalized = command with
        {
            OrganizationId = command.OrganizationId.Trim(),
            IssuerRuc = command.IssuerRuc.Trim(),
            ReceiverRut = command.ReceiverRut.Trim(),
            OperationId = command.OperationId.Trim()
        };

        try
        {
            return await _transactions.ExecuteAsync(async ct =>
            {
                var existingOperation = await _submissions.GetByOperationIdAsync(normalized.OrganizationId, normalized.OperationId, ct);
                if (existingOperation is not null)
                {
                    EnsureSubmissionIntegrity(existingOperation);
                    EnsureSameIdentity(existingOperation, normalized, "fiscal.envelope.transport.operation_replay_mismatch");
                    return Result(existingOperation, true);
                }

                var envelope = await RequiredEnvelopeAsync(normalized.OrganizationId, normalized.IssuerRuc, normalized.ReceiverRut, normalized.SenderEnvelopeId, ct);
                var existingEnvelope = await _submissions.GetByEnvelopeIdAsync(envelope.Id, ct);
                if (existingEnvelope is not null)
                {
                    EnsureSubmissionIntegrity(existingEnvelope);
                    EnsureSameIdentity(existingEnvelope, normalized, "fiscal.envelope.transport.identity_replay_mismatch");
                    return Result(existingEnvelope, true);
                }

                var submission = new StoredFiscalCfeEnvelopeSubmission(
                    Guid.NewGuid(),
                    envelope.Id,
                    envelope.OrganizationId,
                    envelope.IssuerRuc,
                    envelope.ReceiverRut,
                    envelope.SenderEnvelopeId,
                    normalized.OperationId,
                    envelope.EnvelopeSha256,
                    FiscalCfeEnvelopeSubmissionState.Prepared,
                    0,
                    WholeSecond(_clock.UtcNow),
                    null,
                    null,
                    null,
                    null,
                    null);

                EnsureSubmissionIntegrity(submission);
                await _submissions.AddAsync(submission, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                return Result(submission, false);
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && _conflicts.IsUniqueConstraintConflict(ex))
        {
            return await RecoverConcurrentPrepareAsync(normalized, cancellationToken);
        }
    }

    private async Task<FiscalCfeEnvelopeSubmissionResult> RecoverConcurrentPrepareAsync(
        PrepareFiscalCfeEnvelopeSubmissionCommand command,
        CancellationToken cancellationToken)
    {
        var existingOperation = await _submissions.GetByOperationIdAsync(command.OrganizationId, command.OperationId, cancellationToken);
        if (existingOperation is not null)
        {
            EnsureSubmissionIntegrity(existingOperation);
            EnsureSameIdentity(existingOperation, command, "fiscal.envelope.transport.operation_replay_mismatch");
            return Result(existingOperation, true);
        }

        var envelope = await RequiredEnvelopeAsync(command.OrganizationId, command.IssuerRuc, command.ReceiverRut, command.SenderEnvelopeId, cancellationToken);
        var existingEnvelope = await _submissions.GetByEnvelopeIdAsync(envelope.Id, cancellationToken);
        if (existingEnvelope is not null)
        {
            EnsureSubmissionIntegrity(existingEnvelope);
            EnsureSameIdentity(existingEnvelope, command, "fiscal.envelope.transport.identity_replay_mismatch");
            return Result(existingEnvelope, true);
        }

        throw Conflict(
            "fiscal.envelope.transport.concurrent_prepare_unresolved",
            "A concurrent Sobre transport preparation conflict could not be reconciled to durable evidence.",
            "concurrent_uniqueness_conflict");
    }

    internal async Task<StoredFiscalCfeEnvelope> RequiredEnvelopeAsync(
        string organizationId,
        string issuerRuc,
        string receiverRut,
        long senderEnvelopeId,
        CancellationToken cancellationToken)
    {
        var envelope = await _envelopes.GetByIdentityAsync(
            organizationId, issuerRuc, receiverRut, senderEnvelopeId, cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.transport.envelope_required",
                "A durable Sobre is required before transport preparation.",
                "missing_prerequisite");
        EnsureEnvelopeIntegrity(envelope);
        return envelope;
    }

    internal static void EnsureEnvelopeIntegrity(StoredFiscalCfeEnvelope envelope)
    {
        if (envelope.Id == Guid.Empty
            || string.IsNullOrWhiteSpace(envelope.EnvelopeXml)
            || !Sha256Value(envelope.EnvelopeSha256)
            || !string.Equals(envelope.EnvelopeSha256, Sha256(envelope.EnvelopeXml), StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.envelope.transport.envelope_evidence_invalid",
                "Durable Sobre evidence is incomplete or does not match its SHA-256 identity.",
                "invalid_persisted_evidence");
        }
    }

    internal static void Validate(string organizationId, string issuerRuc, string receiverRut, long senderEnvelopeId)
    {
        if (string.IsNullOrWhiteSpace(organizationId) || organizationId.Trim().Length > 200)
            throw Validation("fiscal.envelope.transport.organization_invalid", "Organization id is required and must not exceed 200 characters.");
        if (!TwelveDigits(issuerRuc?.Trim()))
            throw Validation("fiscal.envelope.transport.issuer_ruc_invalid", "Issuer RUC must contain exactly 12 digits.");
        if (!TwelveDigits(receiverRut?.Trim()))
            throw Validation("fiscal.envelope.transport.receiver_rut_invalid", "Receiver RUT must contain exactly 12 digits.");
        if (senderEnvelopeId is < 0 or > 9_999_999_999L)
            throw Validation("fiscal.envelope.transport.sender_id_invalid", "DGI Idemisor must be between 0 and 9999999999.");
    }

    internal static void EnsureSubmissionIntegrity(StoredFiscalCfeEnvelopeSubmission submission)
    {
        var responsePairValid = submission.State == FiscalCfeEnvelopeSubmissionState.ResponseReceived
            ? !string.IsNullOrWhiteSpace(submission.ResponseXml)
                && Sha256Value(submission.ResponseSha256)
                && string.Equals(submission.ResponseSha256, Sha256(submission.ResponseXml!), StringComparison.Ordinal)
            : submission.ResponseXml is null && submission.ResponseSha256 is null;

        if (submission.Id == Guid.Empty
            || submission.EnvelopeId == Guid.Empty
            || string.IsNullOrWhiteSpace(submission.OrganizationId)
            || !TwelveDigits(submission.IssuerRuc)
            || !TwelveDigits(submission.ReceiverRut)
            || submission.SenderEnvelopeId is < 0 or > 9_999_999_999L
            || string.IsNullOrWhiteSpace(submission.OperationId)
            || submission.OperationId.Length > 120
            || !Sha256Value(submission.EnvelopeSha256)
            || !Enum.IsDefined(submission.State)
            || submission.AttemptCount < 0
            || submission.PreparedAtUtc == default
            || !responsePairValid)
        {
            throw Conflict(
                "fiscal.envelope.transport.persisted_evidence_invalid",
                "Persisted Sobre transport evidence is incomplete or internally inconsistent.",
                "invalid_persisted_evidence");
        }
    }

    private static void EnsureSameIdentity(
        StoredFiscalCfeEnvelopeSubmission submission,
        PrepareFiscalCfeEnvelopeSubmissionCommand command,
        string code)
    {
        if (!string.Equals(submission.OrganizationId, command.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(submission.IssuerRuc, command.IssuerRuc, StringComparison.Ordinal)
            || !string.Equals(submission.ReceiverRut, command.ReceiverRut, StringComparison.Ordinal)
            || submission.SenderEnvelopeId != command.SenderEnvelopeId)
        {
            throw Conflict(code, "The Sobre transport operation was already used for a different durable envelope identity.", "inconsistent_replay");
        }
    }

    internal static DateTimeOffset WholeSecond(DateTimeOffset value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, TimeSpan.Zero);

    internal static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    internal static bool Sha256Value(string? value) =>
        value is not null && value.Length == 64 && value.All(Uri.IsHexDigit);

    private static bool TwelveDigits(string? value) =>
        value is not null && value.Length == 12 && value.All(char.IsDigit);

    internal static FiscalCfeEnvelopeSubmissionResult Result(StoredFiscalCfeEnvelopeSubmission value, bool replayed) =>
        new(value.Id, value.EnvelopeId, value.OrganizationId, value.IssuerRuc, value.ReceiverRut,
            value.SenderEnvelopeId, value.OperationId, value.EnvelopeSha256, value.State, value.AttemptCount,
            value.ResponseXml, value.ResponseSha256, value.FailureCode, replayed);

    internal static ApplicationProblemException Validation(string code, string message) =>
        new(ApplicationProblemKind.Validation, code, message);

    internal static ApplicationProblemException Conflict(string code, string message, string conflictType) =>
        new(ApplicationProblemKind.Conflict, code, message, conflictType: conflictType);
}

public sealed class DispatchFiscalCfeEnvelopeSubmissionUseCase
{
    private readonly IFiscalCfeEnvelopeRepository _envelopes;
    private readonly IFiscalCfeEnvelopeSubmissionRepository _submissions;
    private readonly IFiscalCfeEnvelopeTransportGateway _gateway;
    private readonly IFiscalCfeEnvelopeTransportClock _clock;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;

    public DispatchFiscalCfeEnvelopeSubmissionUseCase(
        IFiscalCfeEnvelopeRepository envelopes,
        IFiscalCfeEnvelopeSubmissionRepository submissions,
        IFiscalCfeEnvelopeTransportGateway gateway,
        IFiscalCfeEnvelopeTransportClock clock,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork)
    {
        _envelopes = envelopes;
        _submissions = submissions;
        _gateway = gateway;
        _clock = clock;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
    }

    public async Task<FiscalCfeEnvelopeSubmissionResult> ExecuteAsync(
        DispatchFiscalCfeEnvelopeSubmissionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        PrepareFiscalCfeEnvelopeSubmissionUseCase.Validate(command.OrganizationId, command.IssuerRuc, command.ReceiverRut, command.SenderEnvelopeId);

        var organizationId = command.OrganizationId.Trim();
        var issuerRuc = command.IssuerRuc.Trim();
        var receiverRut = command.ReceiverRut.Trim();

        var dispatch = await _transactions.ExecuteAsync(async ct =>
        {
            var envelope = await _envelopes.GetByIdentityAsync(organizationId, issuerRuc, receiverRut, command.SenderEnvelopeId, ct)
                ?? throw PrepareFiscalCfeEnvelopeSubmissionUseCase.Conflict(
                    "fiscal.envelope.transport.envelope_required",
                    "A durable Sobre is required before dispatch.",
                    "missing_prerequisite");
            PrepareFiscalCfeEnvelopeSubmissionUseCase.EnsureEnvelopeIntegrity(envelope);

            var submission = await _submissions.GetByEnvelopeIdAsync(envelope.Id, ct)
                ?? throw PrepareFiscalCfeEnvelopeSubmissionUseCase.Conflict(
                    "fiscal.envelope.transport.submission_required",
                    "A prepared Sobre submission is required before dispatch.",
                    "missing_prerequisite");
            PrepareFiscalCfeEnvelopeSubmissionUseCase.EnsureSubmissionIntegrity(submission);

            if (!string.Equals(submission.EnvelopeSha256, envelope.EnvelopeSha256, StringComparison.Ordinal))
                throw PrepareFiscalCfeEnvelopeSubmissionUseCase.Conflict(
                    "fiscal.envelope.transport.envelope_hash_mismatch",
                    "The prepared Sobre submission no longer matches the durable envelope bytes.",
                    "invalid_persisted_evidence");

            if (submission.State == FiscalCfeEnvelopeSubmissionState.ResponseReceived)
                return (submission, envelope, true);
            if (submission.State is FiscalCfeEnvelopeSubmissionState.InFlight or FiscalCfeEnvelopeSubmissionState.Unknown)
                throw PrepareFiscalCfeEnvelopeSubmissionUseCase.Conflict(
                    "fiscal.envelope.transport.reconciliation_required",
                    "The Sobre delivery outcome is not safe to retry automatically; reconcile with DGI first.",
                    "transport_outcome_unknown");

            var inFlight = submission with
            {
                State = FiscalCfeEnvelopeSubmissionState.InFlight,
                AttemptCount = submission.AttemptCount + 1,
                LastAttemptAtUtc = PrepareFiscalCfeEnvelopeSubmissionUseCase.WholeSecond(_clock.UtcNow),
                FailureCode = null
            };
            await _submissions.UpdateAsync(inFlight, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return (inFlight, envelope, false);
        }, cancellationToken);

        if (dispatch.Item3)
            return PrepareFiscalCfeEnvelopeSubmissionUseCase.Result(dispatch.Item1, true);

        var inFlightSubmission = dispatch.Item1;
        var durableEnvelope = dispatch.Item2;
        try
        {
            var response = await _gateway.SendAsync(new FiscalCfeEnvelopeTransportRequest(
                durableEnvelope.OrganizationId,
                durableEnvelope.IssuerRuc,
                durableEnvelope.ReceiverRut,
                durableEnvelope.SenderEnvelopeId,
                durableEnvelope.EnvelopeSha256,
                durableEnvelope.EnvelopeXml), cancellationToken);

            if (response is null || string.IsNullOrWhiteSpace(response.ResponseXml))
                throw new FiscalCfeEnvelopeTransportException(
                    "fiscal.envelope.transport.response_invalid",
                    "DGI transport returned an empty Sobre response payload.",
                    deliveryAmbiguous: true);

            return await CompleteAsync(inFlightSubmission, response.ResponseXml, cancellationToken);
        }
        catch (FiscalCfeEnvelopeTransportException ex)
        {
            return ex.DeliveryAmbiguous
                ? await MarkUnknownAsync(inFlightSubmission, ex.Code, CancellationToken.None)
                : await ResetPreparedAsync(inFlightSubmission, ex.Code, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            return await MarkUnknownAsync(inFlightSubmission, "fiscal.envelope.transport.cancelled_after_dispatch", CancellationToken.None);
        }
        catch (Exception)
        {
            return await MarkUnknownAsync(inFlightSubmission, "fiscal.envelope.transport.unexpected_failure", CancellationToken.None);
        }
    }

    private Task<FiscalCfeEnvelopeSubmissionResult> CompleteAsync(
        StoredFiscalCfeEnvelopeSubmission expected,
        string responseXml,
        CancellationToken cancellationToken) =>
        _transactions.ExecuteAsync(async ct =>
        {
            var current = await RequiredCurrentAsync(expected, ct);
            var now = PrepareFiscalCfeEnvelopeSubmissionUseCase.WholeSecond(_clock.UtcNow);
            var completed = current with
            {
                State = FiscalCfeEnvelopeSubmissionState.ResponseReceived,
                CompletedAtUtc = now,
                ResponseXml = responseXml,
                ResponseSha256 = PrepareFiscalCfeEnvelopeSubmissionUseCase.Sha256(responseXml),
                FailureCode = null
            };
            PrepareFiscalCfeEnvelopeSubmissionUseCase.EnsureSubmissionIntegrity(completed);
            await _submissions.UpdateAsync(completed, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return PrepareFiscalCfeEnvelopeSubmissionUseCase.Result(completed, false);
        }, cancellationToken);

    private Task<FiscalCfeEnvelopeSubmissionResult> ResetPreparedAsync(
        StoredFiscalCfeEnvelopeSubmission expected,
        string failureCode,
        CancellationToken cancellationToken) =>
        _transactions.ExecuteAsync(async ct =>
        {
            var current = await RequiredCurrentAsync(expected, ct);
            var reset = current with { State = FiscalCfeEnvelopeSubmissionState.Prepared, FailureCode = failureCode };
            await _submissions.UpdateAsync(reset, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return PrepareFiscalCfeEnvelopeSubmissionUseCase.Result(reset, false);
        }, cancellationToken);

    private Task<FiscalCfeEnvelopeSubmissionResult> MarkUnknownAsync(
        StoredFiscalCfeEnvelopeSubmission expected,
        string failureCode,
        CancellationToken cancellationToken) =>
        _transactions.ExecuteAsync(async ct =>
        {
            var current = await RequiredCurrentAsync(expected, ct);
            var unknown = current with
            {
                State = FiscalCfeEnvelopeSubmissionState.Unknown,
                CompletedAtUtc = PrepareFiscalCfeEnvelopeSubmissionUseCase.WholeSecond(_clock.UtcNow),
                FailureCode = failureCode
            };
            await _submissions.UpdateAsync(unknown, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return PrepareFiscalCfeEnvelopeSubmissionUseCase.Result(unknown, false);
        }, cancellationToken);

    private async Task<StoredFiscalCfeEnvelopeSubmission> RequiredCurrentAsync(
        StoredFiscalCfeEnvelopeSubmission expected,
        CancellationToken cancellationToken)
    {
        var current = await _submissions.GetByEnvelopeIdAsync(expected.EnvelopeId, cancellationToken)
            ?? throw PrepareFiscalCfeEnvelopeSubmissionUseCase.Conflict(
                "fiscal.envelope.transport.submission_missing_after_send",
                "Durable Sobre transport evidence disappeared while the request was in flight.",
                "invalid_persisted_evidence");
        if (current.State != FiscalCfeEnvelopeSubmissionState.InFlight || current.AttemptCount != expected.AttemptCount)
            throw PrepareFiscalCfeEnvelopeSubmissionUseCase.Conflict(
                "fiscal.envelope.transport.concurrent_completion",
                "Sobre transport state changed while the DGI request was in flight.",
                "concurrent_change");
        return current;
    }
}
