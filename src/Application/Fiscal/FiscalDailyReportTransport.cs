using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;

namespace EFactura.Application.Fiscal;

public enum FiscalDailyReportSubmissionState
{
    Prepared = 1,
    InFlight = 2,
    Received = 3,
    Rejected = 4,
    Unknown = 5
}

public sealed record StoredFiscalDailyReportSubmission(
    Guid Id,
    Guid SignedArtifactId,
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    string OperationId,
    string SignedContentHash,
    FiscalDailyReportSubmissionState State,
    int AttemptCount,
    DateTimeOffset PreparedAtUtc,
    DateTimeOffset? LastAttemptAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? DgiReceiverId,
    string? AckStateCode,
    string? AckXml,
    string? FailureCode);

public interface IFiscalDailyReportSubmissionRepository
{
    Task<StoredFiscalDailyReportSubmission?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default);

    Task<StoredFiscalDailyReportSubmission?> GetByIdentityAsync(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        int sequence,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        StoredFiscalDailyReportSubmission submission,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        StoredFiscalDailyReportSubmission submission,
        CancellationToken cancellationToken = default);
}

public interface IFiscalDailyReportTransportClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed record FiscalDailyReportTransportRequest(
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    string SignedContentHash,
    string SignedXml);

public sealed record FiscalDailyReportTransportResponse(
    string AckStateCode,
    string? DgiReceiverId,
    string AckXml);

public interface IFiscalDailyReportTransportGateway
{
    Task<FiscalDailyReportTransportResponse> SendAsync(
        FiscalDailyReportTransportRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class FiscalDailyReportTransportException : Exception
{
    public FiscalDailyReportTransportException(
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

public sealed record PrepareFiscalDailyReportSubmissionCommand(
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    string OperationId);

public sealed record FiscalDailyReportSubmissionResult(
    Guid SubmissionId,
    Guid SignedArtifactId,
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    string SignedContentHash,
    FiscalDailyReportSubmissionState State,
    int AttemptCount,
    string? DgiReceiverId,
    string? AckStateCode,
    string? AckXml,
    string? FailureCode,
    bool Replayed);

public sealed record FiscalDailyReportSubmissionPreparedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid SubmissionId,
    Guid SignedArtifactId,
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    string SignedContentHash) : IIntegrationEvent;

public sealed record FiscalDailyReportSubmissionCompletedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid SubmissionId,
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    FiscalDailyReportSubmissionState State,
    string? DgiReceiverId,
    string? AckStateCode) : IIntegrationEvent;

/// <summary>
/// Creates durable intent to send one already-signed Reporte Diario. The wire call is deliberately
/// separated from preparation so process failure cannot erase the fact that a submission was planned.
/// SecEnvio N+1 cannot be prepared until N has durable AR (Reporte Recibido) evidence.
/// </summary>
public sealed class PrepareFiscalDailyReportSubmissionUseCase
{
    private readonly IFiscalDailyReportSignedArtifactRepository _artifacts;
    private readonly IFiscalDailyReportSubmissionRepository _submissions;
    private readonly IFiscalDailyReportTransportClock _clock;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public PrepareFiscalDailyReportSubmissionUseCase(
        IFiscalDailyReportSignedArtifactRepository artifacts,
        IFiscalDailyReportSubmissionRepository submissions,
        IFiscalDailyReportTransportClock clock,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _artifacts = artifacts;
        _submissions = submissions;
        _clock = clock;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<FiscalDailyReportSubmissionResult> ExecuteAsync(
        PrepareFiscalDailyReportSubmissionCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        return _transactions.ExecuteAsync(async ct =>
        {
            var existingOperation = await _submissions.GetByOperationIdAsync(command.OrganizationId, command.OperationId, ct);
            if (existingOperation is not null)
            {
                EnsureMatches(existingOperation, command);
                return Result(existingOperation, true);
            }

            var artifact = await _artifacts.GetByIdentityAsync(
                command.OrganizationId,
                command.IssuerRuc,
                command.SummaryDate,
                command.Sequence,
                ct)
                ?? throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.transport.signed_artifact_required",
                    "A durable signed Reporte Diario artifact is required before transport preparation.",
                    "missing_prerequisite");

            var existingIdentity = await _submissions.GetByIdentityAsync(
                command.OrganizationId,
                command.IssuerRuc,
                command.SummaryDate,
                command.Sequence,
                ct);
            if (existingIdentity is not null)
            {
                if (!string.Equals(existingIdentity.SignedContentHash, artifact.SignedContentHash, StringComparison.Ordinal))
                {
                    throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                        "fiscal.daily_report.transport.identity_payload_conflict",
                        "The Reporte Diario transport identity already points to different signed bytes.",
                        "inconsistent_replay");
                }
                return Result(existingIdentity, true);
            }

            if (command.Sequence > 1)
            {
                var previous = await _submissions.GetByIdentityAsync(
                    command.OrganizationId,
                    command.IssuerRuc,
                    command.SummaryDate,
                    command.Sequence - 1,
                    ct);
                if (previous is null || previous.State != FiscalDailyReportSubmissionState.Received
                    || !string.Equals(previous.AckStateCode, "AR", StringComparison.Ordinal))
                {
                    throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                        "fiscal.daily_report.transport.previous_sequence_not_received",
                        "Reporte Diario SecEnvio N+1 cannot be prepared until N has durable DGI AR evidence.",
                        "missing_prerequisite");
                }
            }

            var now = WholeSecond(_clock.UtcNow);
            var submission = new StoredFiscalDailyReportSubmission(
                Guid.NewGuid(),
                artifact.Id,
                artifact.OrganizationId,
                artifact.IssuerRuc,
                artifact.SummaryDate,
                artifact.Sequence,
                command.OperationId.Trim(),
                artifact.SignedContentHash,
                FiscalDailyReportSubmissionState.Prepared,
                0,
                now,
                null,
                null,
                null,
                null,
                null,
                null);

            await _submissions.AddAsync(submission, ct);
            var actor = _actors.Current;
            var correlation = _correlations.Current;
            await _audit.AppendAsync(new AuditEvent(
                Guid.NewGuid(), now,
                "FISCAL_DAILY_REPORT_SUBMISSION_PREPARED",
                actor.ActorId,
                submission.OrganizationId,
                null,
                null,
                "FiscalDailyReportSubmission",
                submission.Id.ToString(),
                AuditOutcome.Succeeded,
                correlation.CorrelationId,
                null,
                new Dictionary<string, string?>
                {
                    ["issuerRuc"] = submission.IssuerRuc,
                    ["summaryDate"] = submission.SummaryDate.ToString("yyyy-MM-dd"),
                    ["sequence"] = submission.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["signedContentHash"] = submission.SignedContentHash,
                    ["operationId"] = submission.OperationId
                }),
                ct);
            await _outbox.EnqueueAsync(
                new FiscalDailyReportSubmissionPreparedIntegrationEvent(
                    Guid.NewGuid(), now, submission.Id, submission.SignedArtifactId,
                    submission.OrganizationId, submission.IssuerRuc, submission.SummaryDate,
                    submission.Sequence, submission.SignedContentHash),
                new OutboxContext(correlation.CorrelationId, null, submission.OrganizationId, actor.ActorId),
                ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result(submission, false);
        }, cancellationToken);
    }

    private static void Validate(PrepareFiscalDailyReportSubmissionCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.OrganizationId))
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation("fiscal.daily_report.transport.organization_required", "Organization id is required.");
        if (string.IsNullOrWhiteSpace(command.IssuerRuc) || command.IssuerRuc.Length != 12 || command.IssuerRuc.Any(c => !char.IsDigit(c)))
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation("fiscal.daily_report.transport.ruc_invalid", "Issuer RUC must contain exactly 12 digits.");
        if (command.SummaryDate == default)
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation("fiscal.daily_report.transport.summary_date_required", "Summary date is required.");
        if (command.Sequence is < 1 or > 99)
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation("fiscal.daily_report.transport.sequence_invalid", "SecEnvio must be between 1 and 99.");
        if (string.IsNullOrWhiteSpace(command.OperationId) || command.OperationId.Trim().Length > 120)
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation("fiscal.daily_report.transport.operation_id_invalid", "Operation id is required and must not exceed 120 characters.");
    }

    private static void EnsureMatches(StoredFiscalDailyReportSubmission submission, PrepareFiscalDailyReportSubmissionCommand command)
    {
        if (!string.Equals(submission.OrganizationId, command.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(submission.IssuerRuc, command.IssuerRuc, StringComparison.Ordinal)
            || submission.SummaryDate != command.SummaryDate
            || submission.Sequence != command.Sequence)
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.transport.operation_replay_mismatch",
                "The transport operation id was already used for different immutable Reporte Diario input.",
                "inconsistent_replay");
        }
    }

    internal static FiscalDailyReportSubmissionResult Result(StoredFiscalDailyReportSubmission value, bool replayed) =>
        new(value.Id, value.SignedArtifactId, value.OrganizationId, value.IssuerRuc, value.SummaryDate,
            value.Sequence, value.SignedContentHash, value.State, value.AttemptCount, value.DgiReceiverId,
            value.AckStateCode, value.AckXml, value.FailureCode, replayed);

    internal static DateTimeOffset WholeSecond(DateTimeOffset value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, TimeSpan.Zero);
}

public sealed record DispatchFiscalDailyReportSubmissionCommand(
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence);

/// <summary>
/// Moves a prepared submission to InFlight durably before crossing the network boundary. A transport
/// failure that may have reached DGI is persisted as Unknown and is never retried automatically.
/// AR and BR responses are persisted verbatim and replayed without another network call.
/// </summary>
public sealed class DispatchFiscalDailyReportSubmissionUseCase
{
    private readonly IFiscalDailyReportSignedArtifactRepository _artifacts;
    private readonly IFiscalDailyReportSubmissionRepository _submissions;
    private readonly IFiscalDailyReportTransportGateway _gateway;
    private readonly IFiscalDailyReportTransportClock _clock;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public DispatchFiscalDailyReportSubmissionUseCase(
        IFiscalDailyReportSignedArtifactRepository artifacts,
        IFiscalDailyReportSubmissionRepository submissions,
        IFiscalDailyReportTransportGateway gateway,
        IFiscalDailyReportTransportClock clock,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _artifacts = artifacts;
        _submissions = submissions;
        _gateway = gateway;
        _clock = clock;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public async Task<FiscalDailyReportSubmissionResult> ExecuteAsync(
        DispatchFiscalDailyReportSubmissionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var dispatch = await _transactions.ExecuteAsync(async ct =>
        {
            var submission = await RequiredSubmission(command, ct);
            if (submission.State is FiscalDailyReportSubmissionState.Received or FiscalDailyReportSubmissionState.Rejected)
                return (submission, (StoredFiscalDailyReportSignedArtifact?)null, true);
            if (submission.State is FiscalDailyReportSubmissionState.InFlight or FiscalDailyReportSubmissionState.Unknown)
            {
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.transport.reconciliation_required",
                    "The Reporte Diario delivery outcome is not safe to retry automatically; reconcile with DGI first.",
                    "transport_outcome_unknown");
            }

            var artifact = await _artifacts.GetByIdentityAsync(
                submission.OrganizationId, submission.IssuerRuc, submission.SummaryDate, submission.Sequence, ct)
                ?? throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.transport.signed_artifact_missing",
                    "The durable signed Reporte Diario artifact no longer exists.",
                    "invalid_persisted_evidence");
            if (artifact.Id != submission.SignedArtifactId
                || !string.Equals(artifact.SignedContentHash, submission.SignedContentHash, StringComparison.Ordinal))
            {
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.transport.signed_artifact_mismatch",
                    "The durable signed Reporte Diario artifact no longer matches the prepared submission.",
                    "invalid_persisted_evidence");
            }

            var now = PrepareFiscalDailyReportSubmissionUseCase.WholeSecond(_clock.UtcNow);
            var inFlight = submission with
            {
                State = FiscalDailyReportSubmissionState.InFlight,
                AttemptCount = submission.AttemptCount + 1,
                LastAttemptAtUtc = now,
                FailureCode = null
            };
            await _submissions.UpdateAsync(inFlight, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return (inFlight, artifact, false);
        }, cancellationToken);

        if (dispatch.Item3)
            return PrepareFiscalDailyReportSubmissionUseCase.Result(dispatch.Item1, true);

        var inFlightSubmission = dispatch.Item1;
        var signedArtifact = dispatch.Item2!;
        try
        {
            var response = await _gateway.SendAsync(
                new FiscalDailyReportTransportRequest(
                    inFlightSubmission.OrganizationId,
                    inFlightSubmission.IssuerRuc,
                    inFlightSubmission.SummaryDate,
                    inFlightSubmission.Sequence,
                    signedArtifact.SignedContentHash,
                    signedArtifact.SignedXml),
                cancellationToken);

            if (response is null || string.IsNullOrWhiteSpace(response.AckXml)
                || (response.AckStateCode != "AR" && response.AckStateCode != "BR"))
            {
                throw new FiscalDailyReportTransportException(
                    "fiscal.daily_report.transport.response_invalid",
                    "DGI transport returned an invalid immediate Reporte Diario response.",
                    true);
            }

            return await CompleteAsync(inFlightSubmission, response, cancellationToken);
        }
        catch (FiscalDailyReportTransportException ex)
        {
            if (!ex.DeliveryAmbiguous)
            {
                return await ResetPreparedAsync(inFlightSubmission, ex.Code, CancellationToken.None);
            }
            return await MarkUnknownAsync(inFlightSubmission, ex.Code, CancellationToken.None);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return await MarkUnknownAsync(
                inFlightSubmission,
                "fiscal.daily_report.transport.unexpected_failure",
                CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            return await MarkUnknownAsync(
                inFlightSubmission,
                "fiscal.daily_report.transport.cancelled_after_dispatch",
                CancellationToken.None);
        }
    }

    private Task<StoredFiscalDailyReportSubmission> RequiredSubmission(
        DispatchFiscalDailyReportSubmissionCommand command,
        CancellationToken cancellationToken) =>
        _submissions.GetByIdentityAsync(command.OrganizationId, command.IssuerRuc, command.SummaryDate, command.Sequence, cancellationToken)
            .ContinueWith(task => task.Result ?? throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.transport.submission_required",
                "A prepared Reporte Diario submission is required before dispatch.",
                "missing_prerequisite"), cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    private Task<FiscalDailyReportSubmissionResult> CompleteAsync(
        StoredFiscalDailyReportSubmission expected,
        FiscalDailyReportTransportResponse response,
        CancellationToken cancellationToken) =>
        _transactions.ExecuteAsync(async ct =>
        {
            var current = await _submissions.GetByIdentityAsync(expected.OrganizationId, expected.IssuerRuc, expected.SummaryDate, expected.Sequence, ct)
                ?? throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.transport.submission_missing_after_send",
                    "Durable Reporte Diario submission disappeared after transport.",
                    "invalid_persisted_evidence");
            if (current.State != FiscalDailyReportSubmissionState.InFlight || current.AttemptCount != expected.AttemptCount)
            {
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.transport.concurrent_completion",
                    "Reporte Diario submission state changed while the DGI request was in flight.",
                    "concurrent_change");
            }

            var now = PrepareFiscalDailyReportSubmissionUseCase.WholeSecond(_clock.UtcNow);
            var state = response.AckStateCode == "AR"
                ? FiscalDailyReportSubmissionState.Received
                : FiscalDailyReportSubmissionState.Rejected;
            var completed = current with
            {
                State = state,
                CompletedAtUtc = now,
                DgiReceiverId = string.IsNullOrWhiteSpace(response.DgiReceiverId) ? null : response.DgiReceiverId.Trim(),
                AckStateCode = response.AckStateCode,
                AckXml = response.AckXml,
                FailureCode = null
            };
            await _submissions.UpdateAsync(completed, ct);
            await AppendCompletionEvidence(completed, now, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return PrepareFiscalDailyReportSubmissionUseCase.Result(completed, false);
        }, cancellationToken);

    private Task<FiscalDailyReportSubmissionResult> ResetPreparedAsync(
        StoredFiscalDailyReportSubmission expected,
        string failureCode,
        CancellationToken cancellationToken) =>
        _transactions.ExecuteAsync(async ct =>
        {
            var current = await _submissions.GetByIdentityAsync(expected.OrganizationId, expected.IssuerRuc, expected.SummaryDate, expected.Sequence, ct)
                ?? throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.transport.submission_missing_after_failure",
                    "Durable Reporte Diario submission disappeared after transport failure.",
                    "invalid_persisted_evidence");
            var reset = current with { State = FiscalDailyReportSubmissionState.Prepared, FailureCode = failureCode };
            await _submissions.UpdateAsync(reset, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return PrepareFiscalDailyReportSubmissionUseCase.Result(reset, false);
        }, cancellationToken);

    private Task<FiscalDailyReportSubmissionResult> MarkUnknownAsync(
        StoredFiscalDailyReportSubmission expected,
        string failureCode,
        CancellationToken cancellationToken) =>
        _transactions.ExecuteAsync(async ct =>
        {
            var current = await _submissions.GetByIdentityAsync(expected.OrganizationId, expected.IssuerRuc, expected.SummaryDate, expected.Sequence, ct)
                ?? throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.transport.submission_missing_after_failure",
                    "Durable Reporte Diario submission disappeared after ambiguous transport failure.",
                    "invalid_persisted_evidence");
            var now = PrepareFiscalDailyReportSubmissionUseCase.WholeSecond(_clock.UtcNow);
            var unknown = current with
            {
                State = FiscalDailyReportSubmissionState.Unknown,
                CompletedAtUtc = now,
                FailureCode = failureCode
            };
            await _submissions.UpdateAsync(unknown, ct);
            await AppendCompletionEvidence(unknown, now, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return PrepareFiscalDailyReportSubmissionUseCase.Result(unknown, false);
        }, cancellationToken);

    private async Task AppendCompletionEvidence(
        StoredFiscalDailyReportSubmission submission,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var actor = _actors.Current;
        var correlation = _correlations.Current;
        await _audit.AppendAsync(new AuditEvent(
            Guid.NewGuid(), occurredAt,
            "FISCAL_DAILY_REPORT_SUBMISSION_COMPLETED",
            actor.ActorId,
            submission.OrganizationId,
            null,
            null,
            "FiscalDailyReportSubmission",
            submission.Id.ToString(),
            submission.State == FiscalDailyReportSubmissionState.Unknown ? AuditOutcome.Failed : AuditOutcome.Succeeded,
            correlation.CorrelationId,
            submission.FailureCode,
            new Dictionary<string, string?>
            {
                ["issuerRuc"] = submission.IssuerRuc,
                ["summaryDate"] = submission.SummaryDate.ToString("yyyy-MM-dd"),
                ["sequence"] = submission.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["state"] = submission.State.ToString(),
                ["ackStateCode"] = submission.AckStateCode,
                ["dgiReceiverId"] = submission.DgiReceiverId,
                ["attemptCount"] = submission.AttemptCount.ToString(System.Globalization.CultureInfo.InvariantCulture)
            }),
            cancellationToken);
        await _outbox.EnqueueAsync(
            new FiscalDailyReportSubmissionCompletedIntegrationEvent(
                Guid.NewGuid(), occurredAt, submission.Id, submission.OrganizationId, submission.IssuerRuc,
                submission.SummaryDate, submission.Sequence, submission.State, submission.DgiReceiverId,
                submission.AckStateCode),
            new OutboxContext(correlation.CorrelationId, null, submission.OrganizationId, actor.ActorId),
            cancellationToken);
    }
}
