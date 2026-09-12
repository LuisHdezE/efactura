using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Domain.Fiscal;

namespace EFactura.Application.Fiscal;

public enum FiscalDailyReportRevisionKind
{
    Initial = 1,
    Correction = 2,
    FxReliquidation = 3
}

public sealed record StoredFiscalDailyReportVersion(
    Guid Id,
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    Guid? PreviousVersionId,
    string OperationId,
    FiscalDailyReportRevisionKind RevisionKind,
    string ReasonCode,
    string ReconciliationFingerprint,
    bool RequiresFxReliquidation,
    DateTimeOffset CreatedAtUtc,
    string VersionFingerprint)
{
    public void EnsureIntegrity()
    {
        if (Id == Guid.Empty)
            throw Conflict("fiscal.daily_report.version.id_invalid", "Daily Report version identity is invalid.", "invalid_persisted_evidence");
        if (string.IsNullOrWhiteSpace(OrganizationId) || OrganizationId.Length > 200)
            throw Conflict("fiscal.daily_report.version.organization_invalid", "Daily Report version organization is invalid.", "invalid_persisted_evidence");
        if (string.IsNullOrWhiteSpace(IssuerRuc) || IssuerRuc.Length != 12 || IssuerRuc.Any(c => !char.IsDigit(c)))
            throw Conflict("fiscal.daily_report.version.ruc_invalid", "Daily Report version issuer RUC is invalid.", "invalid_persisted_evidence");
        if (SummaryDate == default)
            throw Conflict("fiscal.daily_report.version.summary_date_invalid", "Daily Report version summary date is invalid.", "invalid_persisted_evidence");
        if (Sequence is < 1 or > 99)
            throw Conflict("fiscal.daily_report.version.sequence_invalid", "Daily Report version sequence must be between 1 and 99.", "invalid_persisted_evidence");
        if (Sequence == 1 && PreviousVersionId is not null)
            throw Conflict("fiscal.daily_report.version.initial_previous_forbidden", "Initial Daily Report version cannot reference a previous version.", "invalid_persisted_evidence");
        if (Sequence > 1 && PreviousVersionId is null)
            throw Conflict("fiscal.daily_report.version.previous_required", "Corrected Daily Report version requires its previous version identity.", "invalid_persisted_evidence");
        Required(OperationId, 120, "fiscal.daily_report.version.operation_id_invalid");
        if (!Enum.IsDefined(RevisionKind))
            throw Conflict("fiscal.daily_report.version.revision_kind_invalid", "Daily Report revision kind is invalid.", "invalid_persisted_evidence");
        if (Sequence == 1 && RevisionKind != FiscalDailyReportRevisionKind.Initial)
            throw Conflict("fiscal.daily_report.version.initial_kind_invalid", "Sequence 1 must be the initial Daily Report version.", "invalid_persisted_evidence");
        if (Sequence > 1 && RevisionKind == FiscalDailyReportRevisionKind.Initial)
            throw Conflict("fiscal.daily_report.version.correction_kind_required", "Sequence greater than 1 must be a correction or reliquidation.", "invalid_persisted_evidence");
        Required(ReasonCode, 120, "fiscal.daily_report.version.reason_invalid");
        RequiredHash(ReconciliationFingerprint, "fiscal.daily_report.version.reconciliation_fingerprint_invalid");
        RequiredHash(VersionFingerprint, "fiscal.daily_report.version.fingerprint_invalid");
        if (!string.Equals(VersionFingerprint, ComputeFingerprint(), StringComparison.Ordinal))
            throw Conflict("fiscal.daily_report.version.fingerprint_mismatch", "Persisted Daily Report version fingerprint does not match its immutable evidence.", "invalid_persisted_evidence");
    }

    public string ComputeFingerprint() => Hash(string.Join(
        "|",
        Id.ToString("N"),
        OrganizationId,
        IssuerRuc,
        SummaryDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
        PreviousVersionId?.ToString("N") ?? "-",
        OperationId,
        ((int)RevisionKind).ToString(System.Globalization.CultureInfo.InvariantCulture),
        ReasonCode,
        ReconciliationFingerprint,
        RequiresFxReliquidation ? "1" : "0",
        CreatedAtUtc.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture)));

    internal static string Required(string? value, int maxLength, string code)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength)
            throw Validation(code, "Required Daily Report version value is missing or too long.");
        return value;
    }

    internal static string RequiredHash(string? value, string code)
    {
        if (value is null || value.Length != 64 || value.Any(c => !Uri.IsHexDigit(c)))
            throw Validation(code, "Expected a SHA-256 hexadecimal fingerprint.");
        return value.ToLowerInvariant();
    }

    internal static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    internal static ApplicationProblemException Validation(string code, string message) =>
        new(ApplicationProblemKind.Validation, code, message);

    internal static ApplicationProblemException Conflict(string code, string message, string conflictType) =>
        new(ApplicationProblemKind.Conflict, code, message, conflictType: conflictType);
}

public interface IFiscalDailyReportVersionRepository
{
    Task<bool> AcquireOrganizationSequenceLockAsync(string organizationId, CancellationToken cancellationToken = default);

    Task<StoredFiscalDailyReportVersion?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default);

    Task<StoredFiscalDailyReportVersion?> GetLatestAsync(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        CancellationToken cancellationToken = default);

    Task<StoredFiscalDailyReportVersion?> GetByIdentityAsync(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        int sequence,
        CancellationToken cancellationToken = default);

    Task AddAsync(StoredFiscalDailyReportVersion version, CancellationToken cancellationToken = default);
}

public sealed record AllocateFiscalDailyReportVersionCommand(
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    string OperationId,
    FiscalDailyReportRevisionKind RevisionKind,
    string? ReasonCode,
    IReadOnlyCollection<FiscalDailyReportDocumentEvidence>? CompleteDocuments = null,
    IReadOnlyCollection<FiscalDailyReportAnnulmentEvidence>? CompleteAnnulments = null);

public sealed record FiscalDailyReportVersionAllocatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid VersionId,
    Guid? PreviousVersionId,
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    FiscalDailyReportRevisionKind RevisionKind,
    string ReasonCode,
    string ReconciliationFingerprint,
    bool RequiresFxReliquidation) : IIntegrationEvent;

public sealed record FiscalDailyReportVersionAllocationResult(
    Guid VersionId,
    Guid? PreviousVersionId,
    FiscalDailyReportSnapshot Snapshot,
    FiscalDailyReportRevisionKind RevisionKind,
    string ReasonCode,
    bool RequiresFxReliquidation,
    string VersionFingerprint,
    bool Replayed);

/// <summary>
/// Allocates the DGI Reporte Diario SecEnvio lifecycle. Sequence 1 is the first complete daily
/// report. Corrections/reliquidations are complete replacement reports and must use previous + 1.
/// The application never exposes a patch/delta report command.
/// </summary>
public sealed class AllocateFiscalDailyReportVersionUseCase
{
    private readonly IFiscalDailyReportVersionRepository _versions;
    private readonly IFiscalDailyReportSignedArtifactRepository _signedArtifacts;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public AllocateFiscalDailyReportVersionUseCase(
        IFiscalDailyReportVersionRepository versions,
        IFiscalDailyReportSignedArtifactRepository signedArtifacts,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _versions = versions;
        _signedArtifacts = signedArtifacts;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<FiscalDailyReportVersionAllocationResult> ExecuteAsync(
        AllocateFiscalDailyReportVersionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        StoredFiscalDailyReportVersion.Required(command.OrganizationId, 200, "fiscal.daily_report.version.organization_required");
        StoredFiscalDailyReportVersion.Required(command.OperationId, 120, "fiscal.daily_report.version.operation_id_required");
        if (command.SummaryDate == default)
            throw StoredFiscalDailyReportVersion.Validation("fiscal.daily_report.version.summary_date_required", "Summary date is required.");
        if (!Enum.IsDefined(command.RevisionKind))
            throw StoredFiscalDailyReportVersion.Validation("fiscal.daily_report.version.revision_kind_invalid", "Daily Report revision kind is invalid.");

        var documents = (command.CompleteDocuments ?? Array.Empty<FiscalDailyReportDocumentEvidence>()).ToArray();
        var annulments = (command.CompleteAnnulments ?? Array.Empty<FiscalDailyReportAnnulmentEvidence>()).ToArray();

        return _transactions.ExecuteAsync(async ct =>
        {
            if (!await _versions.AcquireOrganizationSequenceLockAsync(command.OrganizationId, ct))
            {
                throw StoredFiscalDailyReportVersion.Validation(
                    "fiscal.daily_report.version.organization_not_configured",
                    "Daily Report sequence allocation requires an existing company fiscal profile.");
            }

            var operationId = command.OperationId.Trim();
            var replay = await _versions.GetByOperationIdAsync(command.OrganizationId, operationId, ct);
            if (replay is not null)
            {
                replay.EnsureIntegrity();
                var replaySnapshot = CreateSnapshot(command, replay.Sequence, documents, annulments);
                var expectedReason = NormalizeReason(command.RevisionKind, command.ReasonCode);
                EnsureReplayMatches(replay, command, replaySnapshot, expectedReason);
                return Result(replay, replaySnapshot, true);
            }

            var latest = await _versions.GetLatestAsync(command.OrganizationId, command.IssuerRuc, command.SummaryDate, ct);
            latest?.EnsureIntegrity();

            int sequence;
            Guid? previousVersionId;
            if (latest is null)
            {
                if (command.RevisionKind != FiscalDailyReportRevisionKind.Initial)
                {
                    throw StoredFiscalDailyReportVersion.Conflict(
                        "fiscal.daily_report.version.initial_required",
                        "The first Daily Report version for the day must use the initial revision kind and SecEnvio 1.",
                        "sequence_transition_invalid");
                }
                sequence = 1;
                previousVersionId = null;
            }
            else
            {
                if (command.RevisionKind == FiscalDailyReportRevisionKind.Initial)
                {
                    throw StoredFiscalDailyReportVersion.Conflict(
                        "fiscal.daily_report.version.initial_already_exists",
                        "An initial Daily Report version already exists for this issuer and summary date.",
                        "sequence_transition_invalid");
                }
                if (latest.Sequence >= 99)
                {
                    throw StoredFiscalDailyReportVersion.Conflict(
                        "fiscal.daily_report.version.sequence_exhausted",
                        "Reporte Diario SecEnvio exhausted the pinned two-digit DGI field.",
                        "sequence_exhausted");
                }

                var priorSigned = await _signedArtifacts.GetByIdentityAsync(
                    latest.OrganizationId,
                    latest.IssuerRuc,
                    latest.SummaryDate,
                    latest.Sequence,
                    ct);
                if (priorSigned is null)
                {
                    throw StoredFiscalDailyReportVersion.Conflict(
                        "fiscal.daily_report.version.prior_not_signed",
                        "A corrected Daily Report version cannot be allocated until the immediately previous version has a durable signed artifact.",
                        "sequence_transition_invalid");
                }

                sequence = checked(latest.Sequence + 1);
                previousVersionId = latest.Id;
            }

            var snapshot = CreateSnapshot(command, sequence, documents, annulments);
            var requiresFxReliquidation = snapshot.Documents.Any(document => document.CurrencyConversionRequiresReliquidation);
            var reasonCode = NormalizeReason(command.RevisionKind, command.ReasonCode);

            if (command.RevisionKind == FiscalDailyReportRevisionKind.FxReliquidation)
            {
                if (latest is null || !latest.RequiresFxReliquidation)
                {
                    throw StoredFiscalDailyReportVersion.Conflict(
                        "fiscal.daily_report.version.fx_reliquidation_not_pending",
                        "FX reliquidation requires an immediately previous Daily Report version marked as requiring reliquidation.",
                        "sequence_transition_invalid");
                }
                if (requiresFxReliquidation)
                {
                    throw StoredFiscalDailyReportVersion.Conflict(
                        "fiscal.daily_report.version.fx_reliquidation_unresolved",
                        "FX reliquidation cannot close while the complete replacement report still carries future-date reliquidation evidence.",
                        "sequence_transition_invalid");
                }
            }

            var rawNow = DateTimeOffset.UtcNow;
            var now = new DateTimeOffset(
                rawNow.Ticks - rawNow.Ticks % TimeSpan.TicksPerSecond,
                rawNow.Offset);
            var provisional = new StoredFiscalDailyReportVersion(
                Guid.NewGuid(),
                snapshot.OrganizationId,
                snapshot.IssuerRuc,
                snapshot.SummaryDate,
                snapshot.Sequence,
                previousVersionId,
                operationId,
                command.RevisionKind,
                reasonCode,
                snapshot.ReconciliationFingerprint,
                requiresFxReliquidation,
                now,
                new string('0', 64));
            var version = provisional with { VersionFingerprint = provisional.ComputeFingerprint() };
            version.EnsureIntegrity();

            await _versions.AddAsync(version, ct);

            var actor = _actors.Current;
            var correlation = _correlations.Current;
            await _audit.AppendAsync(new AuditEvent(
                Guid.NewGuid(),
                now,
                "FISCAL_DAILY_REPORT_VERSION_ALLOCATED",
                actor.ActorId,
                version.OrganizationId,
                null,
                null,
                "FiscalDailyReportVersion",
                version.Id.ToString(),
                AuditOutcome.Succeeded,
                correlation.CorrelationId,
                null,
                new Dictionary<string, string?>
                {
                    ["issuerRuc"] = version.IssuerRuc,
                    ["summaryDate"] = version.SummaryDate.ToString("yyyy-MM-dd"),
                    ["sequence"] = version.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["previousVersionId"] = version.PreviousVersionId?.ToString(),
                    ["revisionKind"] = version.RevisionKind.ToString(),
                    ["reasonCode"] = version.ReasonCode,
                    ["reconciliationFingerprint"] = version.ReconciliationFingerprint,
                    ["requiresFxReliquidation"] = version.RequiresFxReliquidation ? "true" : "false"
                }),
                ct);

            await _outbox.EnqueueAsync(
                new FiscalDailyReportVersionAllocatedIntegrationEvent(
                    Guid.NewGuid(),
                    now,
                    version.Id,
                    version.PreviousVersionId,
                    version.OrganizationId,
                    version.IssuerRuc,
                    version.SummaryDate,
                    version.Sequence,
                    version.RevisionKind,
                    version.ReasonCode,
                    version.ReconciliationFingerprint,
                    version.RequiresFxReliquidation),
                new OutboxContext(correlation.CorrelationId, null, version.OrganizationId, actor.ActorId),
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return Result(version, snapshot, false);
        }, cancellationToken);
    }

    private static FiscalDailyReportSnapshot CreateSnapshot(
        AllocateFiscalDailyReportVersionCommand command,
        int sequence,
        IReadOnlyCollection<FiscalDailyReportDocumentEvidence> documents,
        IReadOnlyCollection<FiscalDailyReportAnnulmentEvidence> annulments)
    {
        try
        {
            return FiscalDailyReportSnapshot.Create(
                command.OrganizationId,
                command.IssuerRuc,
                command.SummaryDate,
                sequence,
                documents,
                annulments);
        }
        catch (EFactura.Domain.Common.DomainRuleException ex)
        {
            throw StoredFiscalDailyReportVersion.Validation(ex.Code, ex.Message);
        }
    }

    private static string NormalizeReason(FiscalDailyReportRevisionKind kind, string? reasonCode)
    {
        if (kind == FiscalDailyReportRevisionKind.Initial)
        {
            if (!string.IsNullOrWhiteSpace(reasonCode) && !string.Equals(reasonCode.Trim(), "initial", StringComparison.Ordinal))
            {
                throw StoredFiscalDailyReportVersion.Validation(
                    "fiscal.daily_report.version.initial_reason_invalid",
                    "Initial Daily Report version uses the fixed reason code 'initial'.");
            }
            return "initial";
        }

        return StoredFiscalDailyReportVersion.Required(
            reasonCode?.Trim(),
            120,
            "fiscal.daily_report.version.correction_reason_required");
    }

    private static void EnsureReplayMatches(
        StoredFiscalDailyReportVersion stored,
        AllocateFiscalDailyReportVersionCommand command,
        FiscalDailyReportSnapshot snapshot,
        string expectedReason)
    {
        var requiresFxReliquidation = snapshot.Documents.Any(document => document.CurrencyConversionRequiresReliquidation);
        if (!string.Equals(stored.OrganizationId, command.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(stored.IssuerRuc, command.IssuerRuc, StringComparison.Ordinal)
            || stored.SummaryDate != command.SummaryDate
            || stored.RevisionKind != command.RevisionKind
            || !string.Equals(stored.ReasonCode, expectedReason, StringComparison.Ordinal)
            || !string.Equals(stored.ReconciliationFingerprint, snapshot.ReconciliationFingerprint, StringComparison.Ordinal)
            || stored.RequiresFxReliquidation != requiresFxReliquidation)
        {
            throw StoredFiscalDailyReportVersion.Conflict(
                "fiscal.daily_report.version.operation_replay_mismatch",
                "Daily Report version operation id was already used with different immutable input.",
                "inconsistent_replay");
        }
    }

    private static FiscalDailyReportVersionAllocationResult Result(
        StoredFiscalDailyReportVersion version,
        FiscalDailyReportSnapshot snapshot,
        bool replayed) =>
        new(
            version.Id,
            version.PreviousVersionId,
            snapshot,
            version.RevisionKind,
            version.ReasonCode,
            version.RequiresFxReliquidation,
            version.VersionFingerprint,
            replayed);
}
