using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Common.Persistence;

namespace EFactura.Application.Fiscal;

public enum FiscalDailyReportLaterState
{
    Processed = 1,
    InManagement = 2,
    Reliquidated = 3
}

public sealed record StoredFiscalDailyReportLaterStateObservation(
    Guid Id,
    Guid? RootSubmissionId,
    Guid? BrCorrectionRevisionId,
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    int? LocalRevision,
    string OperationId,
    string DgiEmitterId,
    string DgiReceiverId,
    FiscalDailyReportLaterState State,
    string DgiStateCode,
    string DgiReceptionTimestampText,
    string EvidenceXml,
    string EvidenceXmlHash,
    DateTimeOffset ObservedAtUtc);

public interface IFiscalDailyReportLaterStateObservationRepository
{
    Task<StoredFiscalDailyReportLaterStateObservation?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        StoredFiscalDailyReportLaterStateObservation observation,
        CancellationToken cancellationToken = default);
}

public sealed record ObserveFiscalDailyReportLaterStateCommand(
    string OrganizationId,
    string DgiReceiverId,
    string OperationId);

public sealed record FiscalDailyReportLaterStateObservationResult(
    Guid ObservationId,
    FiscalDailyReportConsultationTargetKind TargetKind,
    Guid TargetId,
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    int? LocalRevision,
    string DgiEmitterId,
    string DgiReceiverId,
    FiscalDailyReportLaterState State,
    string DgiStateCode,
    string DgiReceptionTimestampText,
    string EvidenceXmlHash,
    DateTimeOffset ObservedAtUtc,
    bool Replayed);

/// <summary>
/// Observes DGI's current Reporte Diario state for one already-known IdReceptor using the
/// authoritative EFACCONSULTARENVIOSREPORTE collection. Only later processing states DR, ER and FR
/// are persisted by this bounded capability. AR/BR remain immediate/original response states and
/// therefore indicate that no later-state observation is available yet. This use case never mutates
/// root/correction transport state, ACK evidence, sequence authorization or retry policy.
/// </summary>
public sealed class ObserveFiscalDailyReportLaterStateUseCase
{
    private readonly IFiscalDailyReportConsultationTargetReader _targets;
    private readonly IFiscalDailyReportLaterStateObservationRepository _observations;
    private readonly IFiscalDailyReportReceiverDiscoveryGateway _gateway;
    private readonly IFiscalDailyReportTransportClock _clock;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;

    public ObserveFiscalDailyReportLaterStateUseCase(
        IFiscalDailyReportConsultationTargetReader targets,
        IFiscalDailyReportLaterStateObservationRepository observations,
        IFiscalDailyReportReceiverDiscoveryGateway gateway,
        IFiscalDailyReportTransportClock clock,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork)
    {
        _targets = targets;
        _observations = observations;
        _gateway = gateway;
        _clock = clock;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
    }

    public async Task<FiscalDailyReportLaterStateObservationResult> ExecuteAsync(
        ObserveFiscalDailyReportLaterStateCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        var organizationId = command.OrganizationId.Trim();
        var receiverId = command.DgiReceiverId.Trim();
        var operationId = command.OperationId.Trim();

        var replay = await _observations.GetByOperationIdAsync(
            organizationId,
            operationId,
            cancellationToken);
        if (replay is not null)
        {
            EnsureReplayMatches(replay, receiverId);
            return Result(replay, true);
        }

        var targets = await _targets.FindByReceiverIdAsync(
            organizationId,
            receiverId,
            cancellationToken);
        if (targets.Count == 0)
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.later_state.receiver_not_known",
                "Later-state observation requires a DGI IdReceptor already linked to durable local Reporte Diario evidence.",
                "missing_prerequisite");
        }
        if (targets.Count != 1)
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.later_state.receiver_ambiguous",
                "The DGI IdReceptor resolves to more than one local Reporte Diario target.",
                "invalid_persisted_evidence");
        }

        var target = targets[0];
        var response = await _gateway.QueryAsync(
            new FiscalDailyReportReceiverDiscoveryRequest(
                target.OrganizationId,
                target.SummaryDate,
                target.Sequence),
            cancellationToken);

        if (string.IsNullOrWhiteSpace(response.EvidenceXml))
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.later_state.evidence_missing",
                "DGI later-state observation did not return durable response evidence.",
                "external_evidence_invalid");
        }

        var matching = response.Items
            .Where(item => string.Equals(item.DgiReceiverId, receiverId, StringComparison.Ordinal))
            .ToArray();
        if (matching.Length == 0)
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.later_state.receiver_not_returned",
                "DGI did not return the requested IdReceptor for its authoritative FechaResumen and Secuencia.",
                "external_evidence_not_found");
        }
        if (matching.Length != 1)
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.later_state.receiver_duplicate",
                "DGI returned duplicate rows for the requested IdReceptor.",
                "external_evidence_ambiguous");
        }

        var selected = matching[0];
        var state = ParseLaterState(selected.StateCode);
        var evidenceHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(response.EvidenceXml))).ToLowerInvariant();
        var now = PrepareFiscalDailyReportSubmissionUseCase.WholeSecond(_clock.UtcNow);
        var observation = new StoredFiscalDailyReportLaterStateObservation(
            Guid.NewGuid(),
            target.Kind == FiscalDailyReportConsultationTargetKind.RootSubmission ? target.TargetId : null,
            target.Kind == FiscalDailyReportConsultationTargetKind.BrCorrectionRevision ? target.TargetId : null,
            target.OrganizationId,
            target.IssuerRuc,
            target.SummaryDate,
            target.Sequence,
            target.LocalRevision,
            operationId,
            selected.DgiEmitterId,
            selected.DgiReceiverId,
            state,
            selected.StateCode,
            selected.ReceptionTimestampText,
            response.EvidenceXml,
            evidenceHash,
            now);

        return await _transactions.ExecuteAsync(async ct =>
        {
            var concurrent = await _observations.GetByOperationIdAsync(organizationId, operationId, ct);
            if (concurrent is not null)
            {
                EnsureReplayMatches(concurrent, receiverId);
                return Result(concurrent, true);
            }

            await _observations.AddAsync(observation, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result(observation, false);
        }, cancellationToken);
    }

    private static FiscalDailyReportLaterState ParseLaterState(string code)
    {
        return code?.Trim() switch
        {
            "DR" => FiscalDailyReportLaterState.Processed,
            "ER" => FiscalDailyReportLaterState.InManagement,
            "FR" => FiscalDailyReportLaterState.Reliquidated,
            "AR" or "BR" => throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.later_state.not_available",
                "DGI still reports an immediate/original Reporte Diario state; no DR, ER or FR observation is available yet.",
                "external_evidence_not_ready"),
            _ => throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.later_state.unsupported_state",
                "DGI returned a Reporte Diario state outside the governed AR/BR/DR/ER/FR set.",
                "external_evidence_invalid")
        };
    }

    private static void Validate(ObserveFiscalDailyReportLaterStateCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.OrganizationId) || command.OrganizationId.Trim().Length > 200)
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation(
                "fiscal.daily_report.later_state.organization_invalid",
                "Organization id is required and must not exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(command.DgiReceiverId) || command.DgiReceiverId.Trim().Length > 120)
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation(
                "fiscal.daily_report.later_state.receiver_invalid",
                "DGI IdReceptor is required and must not exceed 120 characters.");
        if (string.IsNullOrWhiteSpace(command.OperationId) || command.OperationId.Trim().Length > 120)
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation(
                "fiscal.daily_report.later_state.operation_id_invalid",
                "Operation id is required and must not exceed 120 characters.");
    }

    private static void EnsureReplayMatches(
        StoredFiscalDailyReportLaterStateObservation observation,
        string receiverId)
    {
        if (!string.Equals(observation.DgiReceiverId, receiverId, StringComparison.Ordinal))
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.later_state.operation_replay_mismatch",
                "The later-state observation operation id was already used for another DGI IdReceptor.",
                "inconsistent_replay");
        }
    }

    private static FiscalDailyReportLaterStateObservationResult Result(
        StoredFiscalDailyReportLaterStateObservation value,
        bool replayed)
    {
        var kind = value.RootSubmissionId.HasValue
            ? FiscalDailyReportConsultationTargetKind.RootSubmission
            : FiscalDailyReportConsultationTargetKind.BrCorrectionRevision;
        var targetId = value.RootSubmissionId ?? value.BrCorrectionRevisionId
            ?? throw new InvalidOperationException("Later-state observation evidence has no local target.");

        return new FiscalDailyReportLaterStateObservationResult(
            value.Id,
            kind,
            targetId,
            value.OrganizationId,
            value.IssuerRuc,
            value.SummaryDate,
            value.Sequence,
            value.LocalRevision,
            value.DgiEmitterId,
            value.DgiReceiverId,
            value.State,
            value.DgiStateCode,
            value.DgiReceptionTimestampText,
            value.EvidenceXmlHash,
            value.ObservedAtUtc,
            replayed);
    }
}
