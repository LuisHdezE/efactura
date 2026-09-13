using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Common.Persistence;

namespace EFactura.Application.Fiscal;

public sealed record FiscalDailyReportReceiverDiscoveryTarget(
    FiscalDailyReportConsultationTargetKind Kind,
    Guid TargetId,
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    int? LocalRevision,
    FiscalDailyReportSubmissionState State,
    string? DgiReceiverId);

public sealed record FiscalDailyReportReceiverDiscoveryRequest(
    string OrganizationId,
    DateOnly SummaryDate,
    int Sequence);

public sealed record FiscalDailyReportReceiverDiscoveryItem(
    string DgiEmitterId,
    string DgiReceiverId,
    string StateCode,
    string ReceptionTimestampText);

public sealed record FiscalDailyReportReceiverDiscoveryResponse(
    string EvidenceXml,
    IReadOnlyList<FiscalDailyReportReceiverDiscoveryItem> Items);

public interface IFiscalDailyReportReceiverDiscoveryGateway
{
    Task<FiscalDailyReportReceiverDiscoveryResponse> QueryAsync(
        FiscalDailyReportReceiverDiscoveryRequest request,
        CancellationToken cancellationToken = default);
}

public interface IFiscalDailyReportReceiverDiscoveryTargetReader
{
    Task<FiscalDailyReportReceiverDiscoveryTarget?> GetTargetAsync(
        string organizationId,
        FiscalDailyReportConsultationTargetKind targetKind,
        Guid targetId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> GetKnownReceiverIdsAsync(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        int sequence,
        CancellationToken cancellationToken = default);
}

public sealed record StoredFiscalDailyReportReceiverDiscovery(
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
    string DgiStateCode,
    string DgiReceptionTimestampText,
    string EvidenceXml,
    string EvidenceXmlHash,
    DateTimeOffset DiscoveredAtUtc);

public interface IFiscalDailyReportReceiverDiscoveryRepository
{
    Task<StoredFiscalDailyReportReceiverDiscovery?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default);

    Task<StoredFiscalDailyReportReceiverDiscovery?> GetByTargetAsync(
        string organizationId,
        FiscalDailyReportConsultationTargetKind targetKind,
        Guid targetId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        StoredFiscalDailyReportReceiverDiscovery discovery,
        CancellationToken cancellationToken = default);
}

public sealed record DiscoverFiscalDailyReportReceiverCommand(
    string OrganizationId,
    FiscalDailyReportConsultationTargetKind TargetKind,
    Guid TargetId,
    string OperationId);

public sealed record FiscalDailyReportReceiverDiscoveryResult(
    Guid DiscoveryId,
    FiscalDailyReportConsultationTargetKind TargetKind,
    Guid TargetId,
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    int? LocalRevision,
    string DgiEmitterId,
    string DgiReceiverId,
    string DgiStateCode,
    string DgiReceptionTimestampText,
    string EvidenceXmlHash,
    DateTimeOffset DiscoveredAtUtc,
    bool Replayed);

/// <summary>
/// Discovers a DGI IdReceptor for one durable Reporte Diario transport target whose outcome is Unknown
/// and which has no receiver id. The lookup uses the published FechaResumen + Secuencia query and only
/// accepts an unambiguous set difference against receiver ids already linked to local evidence for the
/// same fiscal identity. It never mutates the transport state and never interprets the returned DGI
/// Estado as later-state reconciliation evidence.
/// </summary>
public sealed class DiscoverFiscalDailyReportReceiverUseCase
{
    private readonly IFiscalDailyReportReceiverDiscoveryTargetReader _targets;
    private readonly IFiscalDailyReportReceiverDiscoveryRepository _discoveries;
    private readonly IFiscalDailyReportReceiverDiscoveryGateway _gateway;
    private readonly IFiscalDailyReportTransportClock _clock;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;

    public DiscoverFiscalDailyReportReceiverUseCase(
        IFiscalDailyReportReceiverDiscoveryTargetReader targets,
        IFiscalDailyReportReceiverDiscoveryRepository discoveries,
        IFiscalDailyReportReceiverDiscoveryGateway gateway,
        IFiscalDailyReportTransportClock clock,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork)
    {
        _targets = targets;
        _discoveries = discoveries;
        _gateway = gateway;
        _clock = clock;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
    }

    public async Task<FiscalDailyReportReceiverDiscoveryResult> ExecuteAsync(
        DiscoverFiscalDailyReportReceiverCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        var organizationId = command.OrganizationId.Trim();
        var operationId = command.OperationId.Trim();

        var operationReplay = await _discoveries.GetByOperationIdAsync(
            organizationId,
            operationId,
            cancellationToken);
        if (operationReplay is not null)
        {
            EnsureReplayMatches(operationReplay, command.TargetKind, command.TargetId);
            return Result(operationReplay, true);
        }

        var target = await _targets.GetTargetAsync(
            organizationId,
            command.TargetKind,
            command.TargetId,
            cancellationToken)
            ?? throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.receiver_discovery.target_not_found",
                "The local Reporte Diario transport target does not exist.",
                "missing_prerequisite");

        var targetReplay = await _discoveries.GetByTargetAsync(
            organizationId,
            target.Kind,
            target.TargetId,
            cancellationToken);
        if (targetReplay is not null)
            return Result(targetReplay, true);

        EnsureDiscoverable(target);

        var knownReceiverIds = await _targets.GetKnownReceiverIdsAsync(
            target.OrganizationId,
            target.IssuerRuc,
            target.SummaryDate,
            target.Sequence,
            cancellationToken);
        var known = new HashSet<string>(knownReceiverIds.Where(x => !string.IsNullOrWhiteSpace(x)), StringComparer.Ordinal);

        var response = await _gateway.QueryAsync(
            new FiscalDailyReportReceiverDiscoveryRequest(
                target.OrganizationId,
                target.SummaryDate,
                target.Sequence),
            cancellationToken);

        if (string.IsNullOrWhiteSpace(response.EvidenceXml))
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.receiver_discovery.evidence_missing",
                "DGI receiver discovery did not return durable response evidence.",
                "external_evidence_invalid");
        }

        var novel = response.Items
            .Where(item => !known.Contains(item.DgiReceiverId))
            .ToArray();

        if (novel.GroupBy(item => item.DgiReceiverId, StringComparer.Ordinal).Any(group => group.Count() > 1))
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.receiver_discovery.external_duplicate",
                "DGI receiver discovery returned duplicate rows for an unaccounted IdReceptor.",
                "external_evidence_ambiguous");
        }

        if (novel.Length == 0)
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.receiver_discovery.receiver_not_found",
                "DGI did not return an unaccounted report receiver that can be linked safely to the Unknown local target.",
                "external_evidence_not_found");
        }
        if (novel.Length != 1)
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.receiver_discovery.receiver_ambiguous",
                "More than one unaccounted DGI report receiver matches the same FechaResumen and Secuencia.",
                "external_evidence_ambiguous");
        }

        var selected = novel[0];
        var evidenceHash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(response.EvidenceXml))).ToLowerInvariant();
        var now = PrepareFiscalDailyReportSubmissionUseCase.WholeSecond(_clock.UtcNow);
        var discovery = new StoredFiscalDailyReportReceiverDiscovery(
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
            selected.StateCode,
            selected.ReceptionTimestampText,
            response.EvidenceXml,
            evidenceHash,
            now);

        return await _transactions.ExecuteAsync(async ct =>
        {
            var concurrentOperation = await _discoveries.GetByOperationIdAsync(organizationId, operationId, ct);
            if (concurrentOperation is not null)
            {
                EnsureReplayMatches(concurrentOperation, target.Kind, target.TargetId);
                return Result(concurrentOperation, true);
            }

            var concurrentTarget = await _discoveries.GetByTargetAsync(
                organizationId,
                target.Kind,
                target.TargetId,
                ct);
            if (concurrentTarget is not null)
                return Result(concurrentTarget, true);

            await _discoveries.AddAsync(discovery, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result(discovery, false);
        }, cancellationToken);
    }

    private static void Validate(DiscoverFiscalDailyReportReceiverCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.OrganizationId) || command.OrganizationId.Trim().Length > 200)
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation(
                "fiscal.daily_report.receiver_discovery.organization_invalid",
                "Organization id is required and must not exceed 200 characters.");
        if (command.TargetId == Guid.Empty || !Enum.IsDefined(command.TargetKind))
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation(
                "fiscal.daily_report.receiver_discovery.target_invalid",
                "A valid local Reporte Diario target is required.");
        if (string.IsNullOrWhiteSpace(command.OperationId) || command.OperationId.Trim().Length > 120)
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation(
                "fiscal.daily_report.receiver_discovery.operation_id_invalid",
                "Operation id is required and must not exceed 120 characters.");
    }

    private static void EnsureDiscoverable(FiscalDailyReportReceiverDiscoveryTarget target)
    {
        if (target.State != FiscalDailyReportSubmissionState.Unknown)
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.receiver_discovery.target_not_unknown",
                "Receiver discovery is restricted to a durable Reporte Diario transport target in Unknown state.",
                "invalid_state");
        }
        if (!string.IsNullOrWhiteSpace(target.DgiReceiverId))
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.receiver_discovery.receiver_already_known",
                "The local Reporte Diario target already has a durable DGI IdReceptor.",
                "invalid_state");
        }
    }

    private static void EnsureReplayMatches(
        StoredFiscalDailyReportReceiverDiscovery discovery,
        FiscalDailyReportConsultationTargetKind targetKind,
        Guid targetId)
    {
        var storedKind = discovery.RootSubmissionId.HasValue
            ? FiscalDailyReportConsultationTargetKind.RootSubmission
            : FiscalDailyReportConsultationTargetKind.BrCorrectionRevision;
        var storedTargetId = discovery.RootSubmissionId ?? discovery.BrCorrectionRevisionId
            ?? throw new InvalidOperationException("Receiver discovery evidence has no local target.");

        if (storedKind != targetKind || storedTargetId != targetId)
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.receiver_discovery.operation_replay_mismatch",
                "The receiver-discovery operation id was already used for a different local Reporte Diario target.",
                "inconsistent_replay");
        }
    }

    private static FiscalDailyReportReceiverDiscoveryResult Result(
        StoredFiscalDailyReportReceiverDiscovery value,
        bool replayed)
    {
        var kind = value.RootSubmissionId.HasValue
            ? FiscalDailyReportConsultationTargetKind.RootSubmission
            : FiscalDailyReportConsultationTargetKind.BrCorrectionRevision;
        var targetId = value.RootSubmissionId ?? value.BrCorrectionRevisionId
            ?? throw new InvalidOperationException("Receiver discovery evidence has no local target.");

        return new FiscalDailyReportReceiverDiscoveryResult(
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
            value.DgiStateCode,
            value.DgiReceptionTimestampText,
            value.EvidenceXmlHash,
            value.DiscoveredAtUtc,
            replayed);
    }
}
