namespace EFactura.Application.Fiscal;

public enum FiscalDailyReportReconciliationDisposition
{
    Consistent = 1,
    ManualReviewRequired = 2,
    ReliquidatedExternally = 3
}

public interface IFiscalDailyReportLatestObservationReader
{
    Task<StoredFiscalDailyReportLaterStateObservation?> GetLatestByReceiverIdAsync(
        string organizationId,
        string dgiReceiverId,
        CancellationToken cancellationToken = default);
}

public sealed record AssessFiscalDailyReportReconciliationCommand(
    string OrganizationId,
    string DgiReceiverId);

public sealed record FiscalDailyReportReconciliationAssessment(
    Guid ObservationId,
    FiscalDailyReportConsultationTargetKind TargetKind,
    Guid TargetId,
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    int? LocalRevision,
    string DgiReceiverId,
    FiscalDailyReportLaterState ObservedState,
    string DgiStateCode,
    FiscalDailyReportReconciliationDisposition Disposition,
    string InterpretationCode,
    bool RequiresManualReview,
    bool AutomaticReliquidationAuthorized,
    bool AutomaticLocalMutationAuthorized,
    DateTimeOffset ObservedAtUtc);

/// <summary>
/// Interprets the latest durable DGI Reporte Diario later-state observation without mutating fiscal
/// evidence or authorizing an automatic sequence transition. DGI DR means no inconsistencies were
/// identified, ER requires issuer analysis and FR proves only that DGI reliquidated the prior report.
/// ER does not by itself authorize a new report because DGI explicitly conditions reliquidation on
/// the issuer's analysis ("si corresponde").
/// </summary>
public sealed class AssessFiscalDailyReportReconciliationUseCase
{
    private readonly IFiscalDailyReportLatestObservationReader _observations;

    public AssessFiscalDailyReportReconciliationUseCase(
        IFiscalDailyReportLatestObservationReader observations)
    {
        _observations = observations;
    }

    public async Task<FiscalDailyReportReconciliationAssessment> ExecuteAsync(
        AssessFiscalDailyReportReconciliationCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);
        var organizationId = command.OrganizationId.Trim();
        var receiverId = command.DgiReceiverId.Trim();

        var observation = await _observations.GetLatestByReceiverIdAsync(
            organizationId,
            receiverId,
            cancellationToken);
        if (observation is null)
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.reconciliation.observation_required",
                "Daily Report reconciliation requires durable DR, ER or FR observation evidence for the requested DGI IdReceptor.",
                "missing_prerequisite");
        }

        var (disposition, interpretationCode, requiresManualReview) = observation.State switch
        {
            FiscalDailyReportLaterState.Processed => (
                FiscalDailyReportReconciliationDisposition.Consistent,
                "dgi_processed_no_inconsistencies",
                false),
            FiscalDailyReportLaterState.InManagement => (
                FiscalDailyReportReconciliationDisposition.ManualReviewRequired,
                "dgi_inconsistencies_require_analysis",
                true),
            FiscalDailyReportLaterState.Reliquidated => (
                FiscalDailyReportReconciliationDisposition.ReliquidatedExternally,
                "dgi_prior_report_reliquidated",
                false),
            _ => throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.reconciliation.unsupported_observation",
                "Persisted Daily Report later-state evidence is outside the governed DR, ER or FR set.",
                "invalid_persisted_evidence")
        };

        var targetKind = observation.RootSubmissionId.HasValue
            ? FiscalDailyReportConsultationTargetKind.RootSubmission
            : FiscalDailyReportConsultationTargetKind.BrCorrectionRevision;
        var targetId = observation.RootSubmissionId ?? observation.BrCorrectionRevisionId
            ?? throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.reconciliation.target_missing",
                "Persisted Daily Report later-state evidence has no local target.",
                "invalid_persisted_evidence");

        return new FiscalDailyReportReconciliationAssessment(
            observation.Id,
            targetKind,
            targetId,
            observation.OrganizationId,
            observation.IssuerRuc,
            observation.SummaryDate,
            observation.Sequence,
            observation.LocalRevision,
            observation.DgiReceiverId,
            observation.State,
            observation.DgiStateCode,
            disposition,
            interpretationCode,
            requiresManualReview,
            AutomaticReliquidationAuthorized: false,
            AutomaticLocalMutationAuthorized: false,
            observation.ObservedAtUtc);
    }

    private static void Validate(AssessFiscalDailyReportReconciliationCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.OrganizationId) || command.OrganizationId.Trim().Length > 200)
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation(
                "fiscal.daily_report.reconciliation.organization_invalid",
                "Organization id is required and must not exceed 200 characters.");
        }
        if (string.IsNullOrWhiteSpace(command.DgiReceiverId) || command.DgiReceiverId.Trim().Length > 120)
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation(
                "fiscal.daily_report.reconciliation.receiver_invalid",
                "DGI IdReceptor is required and must not exceed 120 characters.");
        }
    }
}
