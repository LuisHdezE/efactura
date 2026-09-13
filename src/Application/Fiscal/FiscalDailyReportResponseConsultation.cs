using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Common.Persistence;

namespace EFactura.Application.Fiscal;

public enum FiscalDailyReportConsultationTargetKind
{
    RootSubmission = 1,
    BrCorrectionRevision = 2
}

public enum FiscalDailyReportConsultationConsistency
{
    NoImmediateAck = 1,
    MatchesImmediateAck = 2,
    ConflictsWithImmediateAck = 3
}

public sealed record FiscalDailyReportConsultationTarget(
    FiscalDailyReportConsultationTargetKind Kind,
    Guid TargetId,
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    int? LocalRevision,
    string DgiReceiverId,
    string? ImmediateAckStateCode);

public sealed record FiscalDailyReportResponseConsultationRequest(
    string OrganizationId,
    string DgiReceiverId);

public sealed record FiscalDailyReportResponseConsultationResponse(
    string DgiReceiverId,
    string AckStateCode,
    string AckXml);

public interface IFiscalDailyReportResponseConsultationGateway
{
    Task<FiscalDailyReportResponseConsultationResponse> QueryAsync(
        FiscalDailyReportResponseConsultationRequest request,
        CancellationToken cancellationToken = default);
}

public interface IFiscalDailyReportConsultationTargetReader
{
    Task<IReadOnlyList<FiscalDailyReportConsultationTarget>> FindByReceiverIdAsync(
        string organizationId,
        string dgiReceiverId,
        CancellationToken cancellationToken = default);
}

public sealed record StoredFiscalDailyReportResponseConsultation(
    Guid Id,
    Guid? RootSubmissionId,
    Guid? BrCorrectionRevisionId,
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    int? LocalRevision,
    string OperationId,
    string DgiReceiverId,
    string AckStateCode,
    string AckXml,
    string AckXmlHash,
    FiscalDailyReportConsultationConsistency Consistency,
    DateTimeOffset ConsultedAtUtc);

public interface IFiscalDailyReportResponseConsultationRepository
{
    Task<StoredFiscalDailyReportResponseConsultation?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        StoredFiscalDailyReportResponseConsultation consultation,
        CancellationToken cancellationToken = default);
}

public sealed record ConsultFiscalDailyReportResponseCommand(
    string OrganizationId,
    string DgiReceiverId,
    string OperationId);

public sealed record FiscalDailyReportResponseConsultationResult(
    Guid ConsultationId,
    FiscalDailyReportConsultationTargetKind TargetKind,
    Guid TargetId,
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    int? LocalRevision,
    string DgiReceiverId,
    string AckStateCode,
    string AckXml,
    string AckXmlHash,
    FiscalDailyReportConsultationConsistency Consistency,
    DateTimeOffset ConsultedAtUtc,
    bool Replayed);

/// <summary>
/// Queries DGI for the original ACKRepDiario associated with a durable IdReceptor and stores the
/// returned evidence append-only. This use case deliberately does not mutate the root submission or
/// a BR correction revision. A later reconciliation capability must decide how observed evidence can
/// affect local fiscal state.
/// </summary>
public sealed class ConsultFiscalDailyReportResponseUseCase
{
    private readonly IFiscalDailyReportConsultationTargetReader _targets;
    private readonly IFiscalDailyReportResponseConsultationRepository _consultations;
    private readonly IFiscalDailyReportResponseConsultationGateway _gateway;
    private readonly IFiscalDailyReportTransportClock _clock;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;

    public ConsultFiscalDailyReportResponseUseCase(
        IFiscalDailyReportConsultationTargetReader targets,
        IFiscalDailyReportResponseConsultationRepository consultations,
        IFiscalDailyReportResponseConsultationGateway gateway,
        IFiscalDailyReportTransportClock clock,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork)
    {
        _targets = targets;
        _consultations = consultations;
        _gateway = gateway;
        _clock = clock;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
    }

    public async Task<FiscalDailyReportResponseConsultationResult> ExecuteAsync(
        ConsultFiscalDailyReportResponseCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        var organizationId = command.OrganizationId.Trim();
        var receiverId = command.DgiReceiverId.Trim();
        var operationId = command.OperationId.Trim();

        var replay = await _consultations.GetByOperationIdAsync(organizationId, operationId, cancellationToken);
        if (replay is not null)
        {
            EnsureReplayMatches(replay, receiverId);
            return Result(replay, true);
        }

        var candidates = await _targets.FindByReceiverIdAsync(organizationId, receiverId, cancellationToken);
        if (candidates.Count == 0)
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.consultation.receiver_not_known",
                "The DGI IdReceptor is not linked to durable local Reporte Diario evidence.",
                "missing_prerequisite");
        }
        if (candidates.Count != 1)
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.consultation.receiver_ambiguous",
                "The DGI IdReceptor resolves to more than one local Reporte Diario artifact.",
                "invalid_persisted_evidence");
        }

        var target = candidates[0];
        var response = await _gateway.QueryAsync(
            new FiscalDailyReportResponseConsultationRequest(organizationId, receiverId),
            cancellationToken);

        if (!string.Equals(response.DgiReceiverId, receiverId, StringComparison.Ordinal))
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.consultation.receiver_mismatch",
                "DGI consultation returned ACKRepDiario for a different IdReceptor.",
                "external_evidence_mismatch");
        }
        if (response.AckStateCode is not ("AR" or "BR"))
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.consultation.original_ack_state_invalid",
                "The original ACKRepDiario returned by DGI must carry AR or BR state.",
                "external_evidence_invalid");
        }
        if (string.IsNullOrWhiteSpace(response.AckXml))
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.consultation.ack_missing",
                "DGI consultation did not return durable ACKRepDiario XML evidence.",
                "external_evidence_invalid");
        }

        var consistency = string.IsNullOrWhiteSpace(target.ImmediateAckStateCode)
            ? FiscalDailyReportConsultationConsistency.NoImmediateAck
            : string.Equals(target.ImmediateAckStateCode, response.AckStateCode, StringComparison.Ordinal)
                ? FiscalDailyReportConsultationConsistency.MatchesImmediateAck
                : FiscalDailyReportConsultationConsistency.ConflictsWithImmediateAck;

        var ackHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(response.AckXml))).ToLowerInvariant();
        var now = PrepareFiscalDailyReportSubmissionUseCase.WholeSecond(_clock.UtcNow);
        var consultation = new StoredFiscalDailyReportResponseConsultation(
            Guid.NewGuid(),
            target.Kind == FiscalDailyReportConsultationTargetKind.RootSubmission ? target.TargetId : null,
            target.Kind == FiscalDailyReportConsultationTargetKind.BrCorrectionRevision ? target.TargetId : null,
            target.OrganizationId,
            target.IssuerRuc,
            target.SummaryDate,
            target.Sequence,
            target.LocalRevision,
            operationId,
            receiverId,
            response.AckStateCode,
            response.AckXml,
            ackHash,
            consistency,
            now);

        return await _transactions.ExecuteAsync(async ct =>
        {
            var concurrentReplay = await _consultations.GetByOperationIdAsync(organizationId, operationId, ct);
            if (concurrentReplay is not null)
            {
                EnsureReplayMatches(concurrentReplay, receiverId);
                return Result(concurrentReplay, true);
            }

            await _consultations.AddAsync(consultation, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result(consultation, false);
        }, cancellationToken);
    }

    private static void Validate(ConsultFiscalDailyReportResponseCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.OrganizationId) || command.OrganizationId.Trim().Length > 200)
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation(
                "fiscal.daily_report.consultation.organization_invalid",
                "Organization id is required and must not exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(command.DgiReceiverId) || command.DgiReceiverId.Trim().Length > 120)
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation(
                "fiscal.daily_report.consultation.receiver_invalid",
                "DGI IdReceptor is required and must not exceed 120 characters.");
        if (string.IsNullOrWhiteSpace(command.OperationId) || command.OperationId.Trim().Length > 120)
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation(
                "fiscal.daily_report.consultation.operation_id_invalid",
                "Operation id is required and must not exceed 120 characters.");
    }

    private static void EnsureReplayMatches(
        StoredFiscalDailyReportResponseConsultation consultation,
        string receiverId)
    {
        if (!string.Equals(consultation.DgiReceiverId, receiverId, StringComparison.Ordinal))
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.consultation.operation_replay_mismatch",
                "The consultation operation id was already used for a different DGI IdReceptor.",
                "inconsistent_replay");
        }
    }

    private static FiscalDailyReportResponseConsultationResult Result(
        StoredFiscalDailyReportResponseConsultation value,
        bool replayed)
    {
        var kind = value.RootSubmissionId.HasValue
            ? FiscalDailyReportConsultationTargetKind.RootSubmission
            : FiscalDailyReportConsultationTargetKind.BrCorrectionRevision;
        var targetId = value.RootSubmissionId ?? value.BrCorrectionRevisionId
            ?? throw new InvalidOperationException("Consultation evidence has no local target.");

        return new FiscalDailyReportResponseConsultationResult(
            value.Id,
            kind,
            targetId,
            value.OrganizationId,
            value.IssuerRuc,
            value.SummaryDate,
            value.Sequence,
            value.LocalRevision,
            value.DgiReceiverId,
            value.AckStateCode,
            value.AckXml,
            value.AckXmlHash,
            value.Consistency,
            value.ConsultedAtUtc,
            replayed);
    }
}
