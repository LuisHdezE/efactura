using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Persistence;

namespace EFactura.Application.Fiscal;

public enum FiscalCfeEnvelopeAckState
{
    Received = 1,
    Rejected = 2
}

public sealed record FiscalCfeEnvelopeAckRejectionReason(
    string Code,
    string Glosa,
    string? Detail);

public sealed record FiscalCfeEnvelopeAckParseResult(
    bool IsValid,
    FiscalCfeEnvelopeAckState? State,
    string ReceiverRut,
    string IssuerRuc,
    long? DgiResponseId,
    long? SenderEnvelopeId,
    long? DgiReceiverId,
    int? CfeCount,
    string ReceptionTimestampText,
    string SigningTimestampText,
    string? ConsultationToken,
    string? ConsultationAvailableAtText,
    IReadOnlyList<FiscalCfeEnvelopeAckRejectionReason> RejectionReasons,
    string? FailureCode);

/// <summary>
/// DGI ACKSobre XML parsing stays outside Application behind an adapter. Application consumes
/// typed, non-mutating observation evidence only.
/// </summary>
public interface IFiscalCfeEnvelopeAckParser
{
    FiscalCfeEnvelopeAckParseResult Parse(string responseXml);
}

public static class FiscalCfeEnvelopeAckRejectionReasonEvidence
{
    private static readonly HashSet<string> AllowedDgiCodes =
        ["S01", "S02", "S03", "S04", "S05", "S06", "S07", "S08"];

    public static bool TryValidate(
        IReadOnlyCollection<FiscalCfeEnvelopeAckRejectionReason>? reasons,
        bool required,
        out string? error)
    {
        if (reasons is null)
        {
            error = "DGI Sobre rejection-reason evidence is required to be present as a collection.";
            return false;
        }

        if (required && reasons.Count is < 1 or > 30)
        {
            error = "A rejected DGI Sobre acknowledgement must preserve between 1 and 30 rejection reasons.";
            return false;
        }

        if (!required && reasons.Count != 0)
        {
            error = "An AS Sobre acknowledgement must not be reinterpreted as rejected from rejection-reason evidence.";
            return false;
        }

        foreach (var reason in reasons)
        {
            if (reason is null || !AllowedDgiCodes.Contains(reason.Code))
            {
                error = "DGI Sobre rejection reason code must be S01..S08 for the DGI reception boundary.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(reason.Glosa) || reason.Glosa.Length > 100)
            {
                error = "DGI Sobre rejection reason glosa is required and cannot exceed 100 characters.";
                return false;
            }
            if (reason.Detail is not null && reason.Detail.Length > 500)
            {
                error = "DGI Sobre rejection reason detail cannot exceed 500 characters.";
                return false;
            }
        }

        error = null;
        return true;
    }

    public static string Serialize(
        IReadOnlyCollection<FiscalCfeEnvelopeAckRejectionReason> reasons,
        bool required)
    {
        if (!TryValidate(reasons, required, out var error))
            throw Validation("fiscal.envelope.ack.reasons_invalid", error!);
        return JsonSerializer.Serialize(reasons);
    }

    public static IReadOnlyList<FiscalCfeEnvelopeAckRejectionReason> DeserializeRequired(
        string? json,
        bool required)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw Conflict(
                "fiscal.envelope.ack.persisted_reasons_invalid",
                "Persisted DGI Sobre rejection-reason evidence is missing.",
                "invalid_persisted_evidence");

        try
        {
            var reasons = JsonSerializer.Deserialize<List<FiscalCfeEnvelopeAckRejectionReason>>(json);
            if (!TryValidate(reasons, required, out var error))
                throw Conflict(
                    "fiscal.envelope.ack.persisted_reasons_invalid",
                    error!,
                    "invalid_persisted_evidence");
            return reasons!;
        }
        catch (JsonException)
        {
            throw Conflict(
                "fiscal.envelope.ack.persisted_reasons_invalid",
                "Persisted DGI Sobre rejection-reason evidence is not valid JSON.",
                "invalid_persisted_evidence");
        }
    }

    private static ApplicationProblemException Validation(string code, string message) =>
        new(ApplicationProblemKind.Validation, code, message);

    private static ApplicationProblemException Conflict(string code, string message, string conflictType) =>
        new(ApplicationProblemKind.Conflict, code, message, conflictType: conflictType);
}

public sealed record StoredFiscalCfeEnvelopeAckObservation(
    Guid Id,
    Guid SubmissionId,
    Guid EnvelopeId,
    string OrganizationId,
    string IssuerRuc,
    string ReceiverRut,
    long SenderEnvelopeId,
    string ResponseSha256,
    long DgiResponseId,
    long DgiReceiverId,
    int CfeCount,
    FiscalCfeEnvelopeAckState State,
    string ReceptionTimestampText,
    string SigningTimestampText,
    string? ConsultationToken,
    string? ConsultationAvailableAtText,
    string RejectionReasonsJson,
    DateTimeOffset ObservedAtUtc);

public interface IFiscalCfeEnvelopeAckObservationRepository
{
    Task<StoredFiscalCfeEnvelopeAckObservation?> GetBySubmissionIdAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        StoredFiscalCfeEnvelopeAckObservation observation,
        CancellationToken cancellationToken = default);
}

public sealed record ObserveFiscalCfeEnvelopeAckCommand(
    string OrganizationId,
    string IssuerRuc,
    string ReceiverRut,
    long SenderEnvelopeId);

public sealed record FiscalCfeEnvelopeAckObservationResult(
    Guid ObservationId,
    Guid SubmissionId,
    Guid EnvelopeId,
    string OrganizationId,
    string IssuerRuc,
    string ReceiverRut,
    long SenderEnvelopeId,
    string ResponseSha256,
    long DgiResponseId,
    long DgiReceiverId,
    int CfeCount,
    FiscalCfeEnvelopeAckState State,
    string ReceptionTimestampText,
    string SigningTimestampText,
    string? ConsultationToken,
    string? ConsultationAvailableAtText,
    IReadOnlyList<FiscalCfeEnvelopeAckRejectionReason> RejectionReasons,
    DateTimeOffset ObservedAtUtc,
    bool Replayed);

public sealed class ObserveFiscalCfeEnvelopeAckUseCase
{
    private readonly IFiscalCfeEnvelopeRepository _envelopes;
    private readonly IFiscalCfeEnvelopeSubmissionRepository _submissions;
    private readonly IFiscalCfeEnvelopeAckObservationRepository _observations;
    private readonly IFiscalCfeEnvelopeAckParser _parser;
    private readonly IFiscalCfeEnvelopePersistenceConflictClassifier _conflicts;
    private readonly IFiscalCfeEnvelopeTransportClock _clock;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;

    public ObserveFiscalCfeEnvelopeAckUseCase(
        IFiscalCfeEnvelopeRepository envelopes,
        IFiscalCfeEnvelopeSubmissionRepository submissions,
        IFiscalCfeEnvelopeAckObservationRepository observations,
        IFiscalCfeEnvelopeAckParser parser,
        IFiscalCfeEnvelopePersistenceConflictClassifier conflicts,
        IFiscalCfeEnvelopeTransportClock clock,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork)
    {
        _envelopes = envelopes;
        _submissions = submissions;
        _observations = observations;
        _parser = parser;
        _conflicts = conflicts;
        _clock = clock;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
    }

    public async Task<FiscalCfeEnvelopeAckObservationResult> ExecuteAsync(
        ObserveFiscalCfeEnvelopeAckCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        PrepareFiscalCfeEnvelopeSubmissionUseCase.Validate(
            command.OrganizationId,
            command.IssuerRuc,
            command.ReceiverRut,
            command.SenderEnvelopeId);

        var normalized = command with
        {
            OrganizationId = command.OrganizationId.Trim(),
            IssuerRuc = command.IssuerRuc.Trim(),
            ReceiverRut = command.ReceiverRut.Trim()
        };

        try
        {
            return await _transactions.ExecuteAsync(async ct =>
            {
                var (envelope, submission) = await RequiredSourceAsync(normalized, ct);
                var existing = await _observations.GetBySubmissionIdAsync(submission.Id, ct);
                if (existing is not null)
                {
                    EnsureObservationIntegrity(existing);
                    EnsureSameSource(existing, envelope, submission);
                    return Result(existing, true);
                }

                var parsed = _parser.Parse(submission.ResponseXml!);
                if (!parsed.IsValid || parsed.State is null)
                    throw Conflict(
                        parsed.FailureCode ?? "fiscal.envelope.ack.parse_invalid",
                        "The durable DGI Sobre response cannot be interpreted as governed ACKSobre evidence.",
                        "invalid_external_evidence");

                EnsureCorrelation(parsed, envelope);
                EnsureStateEvidence(parsed);

                var reasonsJson = FiscalCfeEnvelopeAckRejectionReasonEvidence.Serialize(
                    parsed.RejectionReasons,
                    parsed.State == FiscalCfeEnvelopeAckState.Rejected);

                var observation = new StoredFiscalCfeEnvelopeAckObservation(
                    Guid.NewGuid(),
                    submission.Id,
                    envelope.Id,
                    envelope.OrganizationId,
                    envelope.IssuerRuc,
                    envelope.ReceiverRut,
                    envelope.SenderEnvelopeId,
                    submission.ResponseSha256!,
                    parsed.DgiResponseId!.Value,
                    parsed.DgiReceiverId!.Value,
                    parsed.CfeCount!.Value,
                    parsed.State.Value,
                    parsed.ReceptionTimestampText,
                    parsed.SigningTimestampText,
                    parsed.ConsultationToken,
                    parsed.ConsultationAvailableAtText,
                    reasonsJson,
                    WholeSecond(_clock.UtcNow));

                EnsureObservationIntegrity(observation);
                await _observations.AddAsync(observation, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                return Result(observation, false);
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && _conflicts.IsUniqueConstraintConflict(ex))
        {
            return await RecoverConcurrentObservationAsync(normalized, cancellationToken);
        }
    }

    private async Task<FiscalCfeEnvelopeAckObservationResult> RecoverConcurrentObservationAsync(
        ObserveFiscalCfeEnvelopeAckCommand command,
        CancellationToken cancellationToken)
    {
        var (envelope, submission) = await RequiredSourceAsync(command, cancellationToken);
        var existing = await _observations.GetBySubmissionIdAsync(submission.Id, cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.ack.concurrent_observation_unresolved",
                "A concurrent ACKSobre observation conflict could not be reconciled to durable evidence.",
                "concurrent_uniqueness_conflict");
        EnsureObservationIntegrity(existing);
        EnsureSameSource(existing, envelope, submission);
        return Result(existing, true);
    }

    private async Task<(StoredFiscalCfeEnvelope Envelope, StoredFiscalCfeEnvelopeSubmission Submission)> RequiredSourceAsync(
        ObserveFiscalCfeEnvelopeAckCommand command,
        CancellationToken cancellationToken)
    {
        var envelope = await _envelopes.GetByIdentityAsync(
            command.OrganizationId,
            command.IssuerRuc,
            command.ReceiverRut,
            command.SenderEnvelopeId,
            cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.ack.envelope_required",
                "A durable Sobre is required before ACK observation.",
                "missing_prerequisite");
        PrepareFiscalCfeEnvelopeSubmissionUseCase.EnsureEnvelopeIntegrity(envelope);

        var submission = await _submissions.GetByEnvelopeIdAsync(envelope.Id, cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.ack.submission_required",
                "A durable Sobre transport submission is required before ACK observation.",
                "missing_prerequisite");
        PrepareFiscalCfeEnvelopeSubmissionUseCase.EnsureSubmissionIntegrity(submission);

        if (submission.State != FiscalCfeEnvelopeSubmissionState.ResponseReceived
            || string.IsNullOrWhiteSpace(submission.ResponseXml)
            || !Sha256Value(submission.ResponseSha256)
            || !string.Equals(submission.ResponseSha256, Sha256(submission.ResponseXml), StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.envelope.ack.response_required",
                "ACK observation requires an exact durable ResponseReceived payload from the accepted Sobre transport boundary.",
                "missing_prerequisite");
        }

        if (!string.Equals(submission.EnvelopeSha256, envelope.EnvelopeSha256, StringComparison.Ordinal))
            throw Conflict(
                "fiscal.envelope.ack.envelope_hash_mismatch",
                "The transport response no longer correlates to the durable Sobre bytes.",
                "invalid_persisted_evidence");

        return (envelope, submission);
    }

    private static void EnsureCorrelation(FiscalCfeEnvelopeAckParseResult parsed, StoredFiscalCfeEnvelope envelope)
    {
        if (!string.Equals(parsed.IssuerRuc, envelope.IssuerRuc, StringComparison.Ordinal)
            || !string.Equals(parsed.ReceiverRut, envelope.ReceiverRut, StringComparison.Ordinal)
            || parsed.SenderEnvelopeId != envelope.SenderEnvelopeId
            || parsed.CfeCount != envelope.CfeCount)
        {
            throw Conflict(
                "fiscal.envelope.ack.correlation_mismatch",
                "The ACKSobre caratula does not correlate to the durable Sobre identity and count.",
                "invalid_external_evidence");
        }
    }

    private static void EnsureStateEvidence(FiscalCfeEnvelopeAckParseResult parsed)
    {
        var rejected = parsed.State == FiscalCfeEnvelopeAckState.Rejected;
        if (!FiscalCfeEnvelopeAckRejectionReasonEvidence.TryValidate(parsed.RejectionReasons, rejected, out var error))
            throw Conflict(
                "fiscal.envelope.ack.state_evidence_invalid",
                error!,
                "invalid_external_evidence");

        var consultationPairValid = string.IsNullOrWhiteSpace(parsed.ConsultationToken)
            ? string.IsNullOrWhiteSpace(parsed.ConsultationAvailableAtText)
            : !string.IsNullOrWhiteSpace(parsed.ConsultationAvailableAtText);
        if (!consultationPairValid)
            throw Conflict(
                "fiscal.envelope.ack.consultation_pair_invalid",
                "ACKSobre consultation token and availability timestamp must be preserved together when present.",
                "invalid_external_evidence");
    }

    internal static void EnsureObservationIntegrity(StoredFiscalCfeEnvelopeAckObservation observation)
    {
        var rejected = observation.State == FiscalCfeEnvelopeAckState.Rejected;
        var reasons = FiscalCfeEnvelopeAckRejectionReasonEvidence.DeserializeRequired(observation.RejectionReasonsJson, rejected);
        var consultationPairValid = string.IsNullOrWhiteSpace(observation.ConsultationToken)
            ? string.IsNullOrWhiteSpace(observation.ConsultationAvailableAtText)
            : !string.IsNullOrWhiteSpace(observation.ConsultationAvailableAtText);

        if (observation.Id == Guid.Empty
            || observation.SubmissionId == Guid.Empty
            || observation.EnvelopeId == Guid.Empty
            || string.IsNullOrWhiteSpace(observation.OrganizationId)
            || !TwelveDigits(observation.IssuerRuc)
            || !TwelveDigits(observation.ReceiverRut)
            || observation.SenderEnvelopeId is < 0 or > 9_999_999_999L
            || !Sha256Value(observation.ResponseSha256)
            || observation.DgiResponseId is < 0 or > 9_999_999_999L
            || observation.DgiReceiverId is < 0 or > 9_999_999_999L
            || observation.CfeCount is < 1 or > 250
            || !Enum.IsDefined(observation.State)
            || string.IsNullOrWhiteSpace(observation.ReceptionTimestampText)
            || string.IsNullOrWhiteSpace(observation.SigningTimestampText)
            || !consultationPairValid
            || (rejected && reasons.Count == 0)
            || (!rejected && reasons.Count != 0)
            || observation.ObservedAtUtc == default)
        {
            throw Conflict(
                "fiscal.envelope.ack.persisted_evidence_invalid",
                "Persisted ACKSobre observation evidence is incomplete or internally inconsistent.",
                "invalid_persisted_evidence");
        }
    }

    private static void EnsureSameSource(
        StoredFiscalCfeEnvelopeAckObservation observation,
        StoredFiscalCfeEnvelope envelope,
        StoredFiscalCfeEnvelopeSubmission submission)
    {
        if (observation.SubmissionId != submission.Id
            || observation.EnvelopeId != envelope.Id
            || !string.Equals(observation.OrganizationId, envelope.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(observation.IssuerRuc, envelope.IssuerRuc, StringComparison.Ordinal)
            || !string.Equals(observation.ReceiverRut, envelope.ReceiverRut, StringComparison.Ordinal)
            || observation.SenderEnvelopeId != envelope.SenderEnvelopeId
            || !string.Equals(observation.ResponseSha256, submission.ResponseSha256, StringComparison.Ordinal)
            || observation.CfeCount != envelope.CfeCount)
        {
            throw Conflict(
                "fiscal.envelope.ack.persisted_source_mismatch",
                "Persisted ACKSobre observation no longer matches its durable transport source.",
                "invalid_persisted_evidence");
        }
    }

    private static FiscalCfeEnvelopeAckObservationResult Result(
        StoredFiscalCfeEnvelopeAckObservation value,
        bool replayed)
    {
        var reasons = FiscalCfeEnvelopeAckRejectionReasonEvidence.DeserializeRequired(
            value.RejectionReasonsJson,
            value.State == FiscalCfeEnvelopeAckState.Rejected);
        return new(
            value.Id,
            value.SubmissionId,
            value.EnvelopeId,
            value.OrganizationId,
            value.IssuerRuc,
            value.ReceiverRut,
            value.SenderEnvelopeId,
            value.ResponseSha256,
            value.DgiResponseId,
            value.DgiReceiverId,
            value.CfeCount,
            value.State,
            value.ReceptionTimestampText,
            value.SigningTimestampText,
            value.ConsultationToken,
            value.ConsultationAvailableAtText,
            reasons,
            value.ObservedAtUtc,
            replayed);
    }

    private static DateTimeOffset WholeSecond(DateTimeOffset value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, TimeSpan.Zero);

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static bool Sha256Value(string? value) =>
        value is not null && value.Length == 64 && value.All(Uri.IsHexDigit);

    private static bool TwelveDigits(string? value) =>
        value is not null && value.Length == 12 && value.All(char.IsDigit);

    private static ApplicationProblemException Conflict(string code, string message, string conflictType) =>
        new(ApplicationProblemKind.Conflict, code, message, conflictType: conflictType);
}