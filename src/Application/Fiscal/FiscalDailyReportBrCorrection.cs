using System.Globalization;
using System.Text.Json;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;

namespace EFactura.Application.Fiscal;

public sealed record FiscalDailyReportRejectionReason(
    string Code,
    string Glosa,
    string? Detail);

public sealed record FiscalDailyReportBrAckParseResult(
    bool IsValid,
    IReadOnlyList<FiscalDailyReportRejectionReason> Reasons,
    string? FailureCode);

/// <summary>
/// DGI wire parsing remains an Infrastructure concern. Application consumes only typed BR evidence.
/// </summary>
public interface IFiscalDailyReportBrAckEvidenceParser
{
    FiscalDailyReportBrAckParseResult Parse(string ackXml);
}

public static class FiscalDailyReportRejectionReasonEvidence
{
    private static readonly HashSet<string> AllowedCodes =
        ["R01", "R02", "R03", "R04", "R05", "R06"];

    public static bool TryValidate(
        IReadOnlyCollection<FiscalDailyReportRejectionReason>? reasons,
        out string? error)
    {
        if (reasons is null || reasons.Count is < 1 or > 30)
        {
            error = "A BR acknowledgement must preserve between 1 and 30 DGI rejection reasons.";
            return false;
        }

        foreach (var reason in reasons)
        {
            if (reason is null || !AllowedCodes.Contains(reason.Code))
            {
                error = "DGI Reporte Diario rejection reason code must be R01..R06.";
                return false;
            }
            if (string.IsNullOrWhiteSpace(reason.Glosa) || reason.Glosa.Length > 100)
            {
                error = "DGI Reporte Diario rejection reason glosa is required and cannot exceed 100 characters.";
                return false;
            }
            if (reason.Detail is not null && reason.Detail.Length > 500)
            {
                error = "DGI Reporte Diario rejection reason detail cannot exceed 500 characters.";
                return false;
            }
        }

        error = null;
        return true;
    }

    public static string Serialize(IReadOnlyCollection<FiscalDailyReportRejectionReason> reasons)
    {
        if (!TryValidate(reasons, out var error))
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation(
                "fiscal.daily_report.br_reason.invalid", error!);
        return JsonSerializer.Serialize(reasons);
    }

    public static IReadOnlyList<FiscalDailyReportRejectionReason> DeserializeRequired(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.br_correction.rejection_reason_evidence_required",
                "Same-SecEnvio correction requires durable typed DGI BR rejection-reason evidence.",
                "missing_prerequisite");
        }

        try
        {
            var reasons = JsonSerializer.Deserialize<List<FiscalDailyReportRejectionReason>>(json);
            if (!TryValidate(reasons, out var error))
            {
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.br_correction.rejection_reason_evidence_invalid",
                    error!,
                    "invalid_persisted_evidence");
            }
            return reasons!;
        }
        catch (JsonException)
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.br_correction.rejection_reason_evidence_invalid",
                "Persisted DGI BR rejection-reason evidence is not valid JSON.",
                "invalid_persisted_evidence");
        }
    }

    public static bool ContainsR05(IEnumerable<FiscalDailyReportRejectionReason> reasons) =>
        reasons.Any(reason => string.Equals(reason.Code, "R05", StringComparison.Ordinal));
}

public sealed record StoredFiscalDailyReportBrCorrectionRevision(
    Guid Id,
    Guid RootSubmissionId,
    Guid RootSignedArtifactId,
    Guid? PreviousRevisionId,
    Guid SigningEvidenceId,
    Guid SignedArtifactId,
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    int LocalRevision,
    string OperationId,
    string CorrectionReasonCode,
    string SourceAckXmlHash,
    string SourceAckReasonsJson,
    string FunctionalFormatVersion,
    string ProjectionFingerprint,
    string UnsignedContentHash,
    string SignedContentHash,
    DateTimeOffset SigningTimestamp,
    string SignatureProfileId,
    string CertificateThumbprint,
    string CertificateSerialNumber,
    string SchemaSetId,
    string SchemaFunctionalFormatVersion,
    string SchemaArchiveVersion,
    string SchemaSetFingerprint,
    string SignedXml,
    DateTimeOffset CreatedAtUtc,
    string RevisionFingerprint,
    FiscalDailyReportSubmissionState State,
    int AttemptCount,
    DateTimeOffset PreparedAtUtc,
    DateTimeOffset? LastAttemptAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? DgiReceiverId,
    string? AckStateCode,
    string? AckXml,
    string? AckReasonsJson,
    string? FailureCode)
{
    public void EnsureIntegrity()
    {
        if (Id == Guid.Empty || RootSubmissionId == Guid.Empty || RootSignedArtifactId == Guid.Empty
            || SigningEvidenceId == Guid.Empty || SignedArtifactId == Guid.Empty)
            throw PersistedConflict("fiscal.daily_report.br_correction.identity_invalid", "BR correction durable identities are invalid.");
        if (LocalRevision < 2)
            throw PersistedConflict("fiscal.daily_report.br_correction.local_revision_invalid", "BR correction local revision must start at 2.");
        if (LocalRevision == 2 && PreviousRevisionId is not null)
            throw PersistedConflict("fiscal.daily_report.br_correction.first_previous_forbidden", "First BR correction cannot reference another correction revision.");
        if (LocalRevision > 2 && PreviousRevisionId is null)
            throw PersistedConflict("fiscal.daily_report.br_correction.previous_required", "Later BR corrections require explicit previous-revision lineage.");
        Required(OrganizationId, 200, "fiscal.daily_report.br_correction.organization_invalid");
        if (IssuerRuc.Length != 12 || IssuerRuc.Any(c => !char.IsDigit(c)))
            throw PersistedConflict("fiscal.daily_report.br_correction.ruc_invalid", "BR correction issuer RUC is invalid.");
        if (SummaryDate == default || Sequence is < 1 or > 99)
            throw PersistedConflict("fiscal.daily_report.br_correction.dgi_identity_invalid", "BR correction DGI identity is invalid.");
        Required(OperationId, 120, "fiscal.daily_report.br_correction.operation_invalid");
        Required(CorrectionReasonCode, 120, "fiscal.daily_report.br_correction.reason_invalid");
        Hash(SourceAckXmlHash, "fiscal.daily_report.br_correction.source_ack_hash_invalid");
        _ = FiscalDailyReportRejectionReasonEvidence.DeserializeRequired(SourceAckReasonsJson);
        Required(FunctionalFormatVersion, 40, "fiscal.daily_report.br_correction.format_invalid");
        Hash(ProjectionFingerprint, "fiscal.daily_report.br_correction.projection_hash_invalid");
        Hash(UnsignedContentHash, "fiscal.daily_report.br_correction.unsigned_hash_invalid");
        Hash(SignedContentHash, "fiscal.daily_report.br_correction.signed_hash_invalid");
        Required(SignatureProfileId, 120, "fiscal.daily_report.br_correction.signature_profile_invalid");
        Required(CertificateThumbprint, 160, "fiscal.daily_report.br_correction.certificate_thumbprint_invalid");
        Required(CertificateSerialNumber, 160, "fiscal.daily_report.br_correction.certificate_serial_invalid");
        Required(SchemaSetId, 120, "fiscal.daily_report.br_correction.schema_set_invalid");
        Required(SchemaFunctionalFormatVersion, 40, "fiscal.daily_report.br_correction.schema_format_invalid");
        Required(SchemaArchiveVersion, 40, "fiscal.daily_report.br_correction.schema_archive_invalid");
        Hash(SchemaSetFingerprint, "fiscal.daily_report.br_correction.schema_fingerprint_invalid");
        if (string.IsNullOrWhiteSpace(SignedXml)
            || !string.Equals(SignedContentHash, PrepareFiscalDailyReportSigningEvidenceUseCase.Sha256(SignedXml), StringComparison.Ordinal))
            throw PersistedConflict("fiscal.daily_report.br_correction.signed_xml_invalid", "BR correction signed XML does not match its durable SHA-256 hash.");
        Hash(RevisionFingerprint, "fiscal.daily_report.br_correction.revision_fingerprint_invalid");
        if (!string.Equals(RevisionFingerprint, ComputeFingerprint(), StringComparison.Ordinal))
            throw PersistedConflict("fiscal.daily_report.br_correction.revision_fingerprint_mismatch", "BR correction revision fingerprint does not match immutable evidence.");
        if (!Enum.IsDefined(State) || AttemptCount < 0)
            throw PersistedConflict("fiscal.daily_report.br_correction.transport_state_invalid", "BR correction transport state is invalid.");
    }

    public string ComputeFingerprint() => PrepareFiscalDailyReportSigningEvidenceUseCase.Sha256(string.Join(
        "|",
        Id.ToString("N"), RootSubmissionId.ToString("N"), RootSignedArtifactId.ToString("N"),
        PreviousRevisionId?.ToString("N") ?? "-", SigningEvidenceId.ToString("N"), SignedArtifactId.ToString("N"),
        OrganizationId, IssuerRuc, SummaryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        Sequence.ToString(CultureInfo.InvariantCulture), LocalRevision.ToString(CultureInfo.InvariantCulture),
        OperationId, CorrectionReasonCode, SourceAckXmlHash, SourceAckReasonsJson,
        FunctionalFormatVersion, ProjectionFingerprint, UnsignedContentHash, SignedContentHash,
        SigningTimestamp.ToString("O", CultureInfo.InvariantCulture), SignatureProfileId,
        CertificateThumbprint, CertificateSerialNumber, SchemaSetId, SchemaFunctionalFormatVersion,
        SchemaArchiveVersion, SchemaSetFingerprint,
        CreatedAtUtc.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)));

    private static void Required(string? value, int max, string code)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > max)
            throw PersistedConflict(code, "Persisted BR correction evidence contains a missing or oversized required value.");
    }

    private static void Hash(string? value, string code)
    {
        if (value is null || value.Length != 64 || value.Any(c => !Uri.IsHexDigit(c)))
            throw PersistedConflict(code, "Persisted BR correction evidence contains an invalid SHA-256 fingerprint.");
    }

    private static ApplicationProblemException PersistedConflict(string code, string message) =>
        PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(code, message, "invalid_persisted_evidence");
}

public interface IFiscalDailyReportBrCorrectionRepository
{
    Task<StoredFiscalDailyReportBrCorrectionRevision?> GetByOperationIdAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default);

    Task<StoredFiscalDailyReportBrCorrectionRevision?> GetLatestAsync(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        int sequence,
        CancellationToken cancellationToken = default);

    Task<StoredFiscalDailyReportBrCorrectionRevision?> GetByRevisionAsync(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        int sequence,
        int localRevision,
        CancellationToken cancellationToken = default);

    Task AddAsync(StoredFiscalDailyReportBrCorrectionRevision revision, CancellationToken cancellationToken = default);
    Task UpdateAsync(StoredFiscalDailyReportBrCorrectionRevision revision, CancellationToken cancellationToken = default);
}

public interface IFiscalDailyReportSameSequenceReceiptReader
{
    Task<bool> HasReceivedCorrectionAsync(
        string organizationId,
        string issuerRuc,
        DateOnly summaryDate,
        int sequence,
        CancellationToken cancellationToken = default);
}

public sealed record PrepareFiscalDailyReportBrCorrectionCommand(
    FiscalDailyReportWireProjection Projection,
    string OperationId,
    string CorrectionReasonCode);

public sealed record DispatchFiscalDailyReportBrCorrectionCommand(
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    int LocalRevision);

public sealed record FiscalDailyReportBrCorrectionResult(
    Guid RevisionId,
    Guid RootSubmissionId,
    Guid SignedArtifactId,
    Guid? PreviousRevisionId,
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    int LocalRevision,
    string ProjectionFingerprint,
    string SignedContentHash,
    FiscalDailyReportSubmissionState State,
    int AttemptCount,
    string? DgiReceiverId,
    string? AckStateCode,
    string? FailureCode,
    bool Replayed);

public sealed record FiscalDailyReportBrCorrectionPreparedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid RevisionId,
    Guid RootSubmissionId,
    Guid SignedArtifactId,
    Guid? PreviousRevisionId,
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    int LocalRevision,
    string ProjectionFingerprint,
    string SignedContentHash) : IIntegrationEvent;

public sealed record FiscalDailyReportBrCorrectionCompletedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid RevisionId,
    string OrganizationId,
    string IssuerRuc,
    DateOnly SummaryDate,
    int Sequence,
    int LocalRevision,
    FiscalDailyReportSubmissionState State,
    string? AckStateCode) : IIntegrationEvent;

/// <summary>
/// Creates a newly signed local revision only after durable BR evidence authorizes same-SecEnvio correction.
/// The rejected artifact remains immutable. R05 is deliberately fail-closed because it means the supplied
/// sequence itself is incorrect and requires a separate reconciliation decision.
/// </summary>
public sealed class PrepareFiscalDailyReportBrCorrectionUseCase
{
    private readonly IFiscalDailyReportSubmissionRepository _submissions;
    private readonly IFiscalDailyReportBrCorrectionRepository _revisions;
    private readonly IFiscalDailyReportBrAckEvidenceParser _ackParser;
    private readonly IFiscalDailyReportXmlBuilder _builder;
    private readonly IFiscalDailyReportSignatureProvider _signer;
    private readonly IFiscalDailyReportSignedSchemaValidator _schemaValidator;
    private readonly IFiscalSigningTimeSource _signingTime;
    private readonly IFiscalDailyReportTransportClock _clock;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public PrepareFiscalDailyReportBrCorrectionUseCase(
        IFiscalDailyReportSubmissionRepository submissions,
        IFiscalDailyReportBrCorrectionRepository revisions,
        IFiscalDailyReportBrAckEvidenceParser ackParser,
        IFiscalDailyReportXmlBuilder builder,
        IFiscalDailyReportSignatureProvider signer,
        IFiscalDailyReportSignedSchemaValidator schemaValidator,
        IFiscalSigningTimeSource signingTime,
        IFiscalDailyReportTransportClock clock,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _submissions = submissions;
        _revisions = revisions;
        _ackParser = ackParser;
        _builder = builder;
        _signer = signer;
        _schemaValidator = schemaValidator;
        _signingTime = signingTime;
        _clock = clock;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<FiscalDailyReportBrCorrectionResult> ExecuteAsync(
        PrepareFiscalDailyReportBrCorrectionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var projection = command.Projection ?? throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation(
            "fiscal.daily_report.br_correction.projection_required", "Corrected Reporte Diario projection is required.");
        ValidateCommand(projection, command.OperationId, command.CorrectionReasonCode);

        return _transactions.ExecuteAsync(async ct =>
        {
            var operationId = command.OperationId.Trim();
            var replay = await _revisions.GetByOperationIdAsync(projection.OrganizationId, operationId, ct);
            if (replay is not null)
            {
                EnsureReplayMatches(replay, projection, command.CorrectionReasonCode);
                EnsureStoredArtifactMatches(replay, projection);
                return Result(replay, true);
            }

            // This SELECT ... FOR UPDATE path on the stable original submission serializes all local
            // corrections for the same DGI identity on both PostgreSQL and MySQL.
            var root = await _submissions.GetByIdentityAsync(
                projection.OrganizationId, projection.IssuerRuc, projection.SummaryDate, projection.Sequence, ct)
                ?? throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.br_correction.root_submission_required",
                    "A durable submission for this DGI Reporte Diario identity is required before BR correction.",
                    "missing_prerequisite");

            var duplicateAfterLock = await _revisions.GetByOperationIdAsync(projection.OrganizationId, operationId, ct);
            if (duplicateAfterLock is not null)
            {
                EnsureReplayMatches(duplicateAfterLock, projection, command.CorrectionReasonCode);
                EnsureStoredArtifactMatches(duplicateAfterLock, projection);
                return Result(duplicateAfterLock, true);
            }

            var baseOperation = await _submissions.GetByOperationIdAsync(projection.OrganizationId, operationId, ct);
            if (baseOperation is not null)
            {
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.br_correction.operation_already_used",
                    "The BR correction operation id is already bound to a base Reporte Diario submission.",
                    "idempotency_conflict");
            }

            var latest = await _revisions.GetLatestAsync(
                projection.OrganizationId, projection.IssuerRuc, projection.SummaryDate, projection.Sequence, ct);
            var sourceState = latest?.State ?? root.State;
            var sourceAckCode = latest?.AckStateCode ?? root.AckStateCode;
            var sourceAckXml = latest?.AckXml ?? root.AckXml;

            if (sourceState is FiscalDailyReportSubmissionState.InFlight or FiscalDailyReportSubmissionState.Unknown)
            {
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.br_correction.reconciliation_required",
                    "The latest same-SecEnvio delivery outcome must be reconciled before a correction can be created.",
                    "transport_outcome_unknown");
            }
            if (sourceState != FiscalDailyReportSubmissionState.Rejected
                || !string.Equals(sourceAckCode, "BR", StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(sourceAckXml))
            {
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.br_correction.br_required",
                    "Same-SecEnvio correction is allowed only after durable DGI BR evidence.",
                    "missing_prerequisite");
            }

            var parsed = _ackParser.Parse(sourceAckXml);
            if (!parsed.IsValid || !FiscalDailyReportRejectionReasonEvidence.TryValidate(parsed.Reasons, out var reasonError))
            {
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    parsed.FailureCode ?? "fiscal.daily_report.br_correction.rejection_reason_evidence_invalid",
                    reasonError ?? "Durable DGI BR acknowledgement does not contain trustworthy typed rejection-reason evidence.",
                    "invalid_persisted_evidence");
            }
            if (FiscalDailyReportRejectionReasonEvidence.ContainsR05(parsed.Reasons))
            {
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.br_correction.r05_reconciliation_required",
                    "DGI R05 states that the submitted Reporte Diario sequence is incorrect; same-SecEnvio correction is blocked until sequence reconciliation is implemented.",
                    "regulatory_reconciliation_required");
            }

            var localRevision = latest is null ? 2 : checked(latest.LocalRevision + 1);
            if (latest is not null && latest.RootSubmissionId != root.Id)
            {
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.br_correction.root_lineage_mismatch",
                    "Persisted BR correction lineage no longer points to the stable original submission.",
                    "invalid_persisted_evidence");
            }

            var signingTimestamp = WholeSecondPreserveOffset(_signingTime.GetSigningTimestamp());
            UnsignedDailyReportArtifact unsigned;
            try
            {
                unsigned = _builder.Build(projection, signingTimestamp);
            }
            catch (DomainRuleException ex)
            {
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation(ex.Code, ex.Message);
            }
            if (!string.Equals(unsigned.ProjectionFingerprint, projection.ProjectionFingerprint, StringComparison.Ordinal)
                || !string.Equals(unsigned.ContentHash, PrepareFiscalDailyReportSigningEvidenceUseCase.Sha256(unsigned.Xml), StringComparison.Ordinal))
            {
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.br_correction.builder_contract_invalid",
                    "Corrected Reporte Diario unsigned builder output does not match the supplied frozen projection.",
                    "provider_contract_violation");
            }

            var signature = await _signer.SignAsync(new FiscalDailyReportSignatureRequest(
                projection.OrganizationId, unsigned.FormatVersion, projection.ProjectionFingerprint,
                unsigned.Xml, unsigned.ContentHash, signingTimestamp), ct);
            if (signature is null || string.IsNullOrWhiteSpace(signature.SignedXml))
            {
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.br_correction.signature_result_invalid",
                    "Reporte Diario signature provider returned an empty corrected result.",
                    "provider_contract_violation");
            }

            var signedHash = PrepareFiscalDailyReportSigningEvidenceUseCase.Sha256(signature.SignedXml);
            if (!string.Equals(signedHash, signature.SignedContentHash, StringComparison.Ordinal))
            {
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.br_correction.signed_hash_mismatch",
                    "Corrected Reporte Diario signed bytes do not match the provider hash.",
                    "provider_contract_violation");
            }

            var schema = RequireValidSchema(signature.SignedXml);
            var now = PrepareFiscalDailyReportSubmissionUseCase.WholeSecond(_clock.UtcNow);
            var sourceAckHash = PrepareFiscalDailyReportSigningEvidenceUseCase.Sha256(sourceAckXml);
            var sourceReasonsJson = FiscalDailyReportRejectionReasonEvidence.Serialize(parsed.Reasons);
            var provisional = new StoredFiscalDailyReportBrCorrectionRevision(
                Guid.NewGuid(), root.Id, root.SignedArtifactId, latest?.Id,
                Guid.NewGuid(), Guid.NewGuid(),
                projection.OrganizationId, projection.IssuerRuc, projection.SummaryDate, projection.Sequence,
                localRevision, operationId, command.CorrectionReasonCode.Trim(), sourceAckHash, sourceReasonsJson,
                unsigned.FormatVersion, projection.ProjectionFingerprint, unsigned.ContentHash, signedHash,
                signingTimestamp,
                Required(signature.SignatureProfileId, 120, "fiscal.daily_report.br_correction.signature_profile_required"),
                Required(signature.CertificateThumbprint, 160, "fiscal.daily_report.br_correction.certificate_thumbprint_required"),
                Required(signature.CertificateSerialNumber, 160, "fiscal.daily_report.br_correction.certificate_serial_required"),
                Required(schema.SchemaSetId, 120, "fiscal.daily_report.br_correction.schema_set_required"),
                Required(schema.FunctionalFormatVersion, 40, "fiscal.daily_report.br_correction.schema_format_required"),
                Required(schema.SchemaArchiveVersion, 40, "fiscal.daily_report.br_correction.schema_archive_required"),
                PrepareFiscalDailyReportSigningEvidenceUseCase.RequiredHash(schema.SchemaSetFingerprint,
                    "fiscal.daily_report.br_correction.schema_fingerprint_required"),
                signature.SignedXml, now, new string('0', 64),
                FiscalDailyReportSubmissionState.Prepared, 0, now, null, null, null, null, null, null, null, null);
            var revision = provisional with { RevisionFingerprint = provisional.ComputeFingerprint() };
            revision.EnsureIntegrity();

            await _revisions.AddAsync(revision, ct);
            await AppendPreparedEvidence(revision, parsed.Reasons, now, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return Result(revision, false);
        }, cancellationToken);
    }

    private void EnsureStoredArtifactMatches(
        StoredFiscalDailyReportBrCorrectionRevision revision,
        FiscalDailyReportWireProjection projection)
    {
        revision.EnsureIntegrity();
        UnsignedDailyReportArtifact unsigned;
        try
        {
            unsigned = _builder.Build(projection, revision.SigningTimestamp);
        }
        catch (DomainRuleException ex)
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation(ex.Code, ex.Message);
        }
        if (!string.Equals(revision.ProjectionFingerprint, projection.ProjectionFingerprint, StringComparison.Ordinal)
            || !string.Equals(revision.UnsignedContentHash, unsigned.ContentHash, StringComparison.Ordinal))
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.br_correction.replay_mismatch",
                "BR correction replay no longer matches the persisted corrected projection.",
                "inconsistent_replay");
        }
        var schema = RequireValidSchema(revision.SignedXml);
        if (!string.Equals(revision.SchemaSetId, schema.SchemaSetId, StringComparison.Ordinal)
            || !string.Equals(revision.SchemaArchiveVersion, schema.SchemaArchiveVersion, StringComparison.Ordinal)
            || !string.Equals(revision.SchemaSetFingerprint, schema.SchemaSetFingerprint, StringComparison.Ordinal))
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.br_correction.schema_evidence_mismatch",
                "BR correction replay schema evidence no longer matches the pinned validator result.",
                "invalid_persisted_evidence");
        }
    }

    private FiscalDailyReportSignedSchemaValidationResult RequireValidSchema(string signedXml)
    {
        var validation = _schemaValidator.Validate(signedXml);
        if (validation is null || !validation.IsValid || validation.Status != FiscalDailyReportSignedSchemaValidationStatus.Valid)
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.br_correction.xsd_invalid",
                "Corrected signed Reporte Diario failed untouched-root XSD validation.",
                "schema_validation_failed");
        }
        return validation;
    }

    private static void ValidateCommand(FiscalDailyReportWireProjection projection, string operationId, string reason)
    {
        if (string.IsNullOrWhiteSpace(projection.OrganizationId))
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation("fiscal.daily_report.br_correction.organization_required", "Organization id is required.");
        if (projection.IssuerRuc.Length != 12 || projection.IssuerRuc.Any(c => !char.IsDigit(c)))
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation("fiscal.daily_report.br_correction.ruc_invalid", "Issuer RUC must contain exactly 12 digits.");
        if (projection.SummaryDate == default || projection.Sequence is < 1 or > 99)
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation("fiscal.daily_report.br_correction.identity_invalid", "Corrected Reporte Diario DGI identity is invalid.");
        PrepareFiscalDailyReportSigningEvidenceUseCase.RequiredHash(projection.ProjectionFingerprint,
            "fiscal.daily_report.br_correction.projection_fingerprint_invalid");
        if (string.IsNullOrWhiteSpace(operationId) || operationId.Trim().Length > 120)
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation("fiscal.daily_report.br_correction.operation_invalid", "Operation id is required and cannot exceed 120 characters.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 120)
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation("fiscal.daily_report.br_correction.reason_invalid", "Correction reason is required and cannot exceed 120 characters.");
    }

    private static void EnsureReplayMatches(
        StoredFiscalDailyReportBrCorrectionRevision revision,
        FiscalDailyReportWireProjection projection,
        string correctionReasonCode)
    {
        if (!string.Equals(revision.OrganizationId, projection.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(revision.IssuerRuc, projection.IssuerRuc, StringComparison.Ordinal)
            || revision.SummaryDate != projection.SummaryDate
            || revision.Sequence != projection.Sequence
            || !string.Equals(revision.ProjectionFingerprint, projection.ProjectionFingerprint, StringComparison.Ordinal)
            || !string.Equals(revision.CorrectionReasonCode, correctionReasonCode.Trim(), StringComparison.Ordinal))
        {
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                "fiscal.daily_report.br_correction.operation_replay_mismatch",
                "BR correction operation id was already used for different immutable input.",
                "idempotency_conflict");
        }
    }

    private async Task AppendPreparedEvidence(
        StoredFiscalDailyReportBrCorrectionRevision revision,
        IReadOnlyCollection<FiscalDailyReportRejectionReason> sourceReasons,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var actor = _actors.Current;
        var correlation = _correlations.Current;
        await _audit.AppendAsync(new AuditEvent(
            Guid.NewGuid(), occurredAt,
            "FISCAL_DAILY_REPORT_BR_CORRECTION_PREPARED",
            actor.ActorId, revision.OrganizationId, null, null,
            "FiscalDailyReportBrCorrectionRevision", revision.Id.ToString(), AuditOutcome.Succeeded,
            correlation.CorrelationId, null,
            new Dictionary<string, string?>
            {
                ["rootSubmissionId"] = revision.RootSubmissionId.ToString(),
                ["previousRevisionId"] = revision.PreviousRevisionId?.ToString(),
                ["issuerRuc"] = revision.IssuerRuc,
                ["summaryDate"] = revision.SummaryDate.ToString("yyyy-MM-dd"),
                ["sequence"] = revision.Sequence.ToString(CultureInfo.InvariantCulture),
                ["localRevision"] = revision.LocalRevision.ToString(CultureInfo.InvariantCulture),
                ["sourceDgiReasons"] = string.Join(",", sourceReasons.Select(x => x.Code)),
                ["projectionFingerprint"] = revision.ProjectionFingerprint,
                ["signedContentHash"] = revision.SignedContentHash
            }), cancellationToken);
        await _outbox.EnqueueAsync(new FiscalDailyReportBrCorrectionPreparedIntegrationEvent(
            Guid.NewGuid(), occurredAt, revision.Id, revision.RootSubmissionId, revision.SignedArtifactId,
            revision.PreviousRevisionId, revision.OrganizationId, revision.IssuerRuc, revision.SummaryDate,
            revision.Sequence, revision.LocalRevision, revision.ProjectionFingerprint, revision.SignedContentHash),
            new OutboxContext(correlation.CorrelationId, null, revision.OrganizationId, actor.ActorId), cancellationToken);
    }

    private static string Required(string? value, int maxLength, string code)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength)
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(code, "Signature/schema provider returned missing or oversized evidence.", "provider_contract_violation");
        return value;
    }

    private static DateTimeOffset WholeSecondPreserveOffset(DateTimeOffset value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, value.Offset);

    internal static FiscalDailyReportBrCorrectionResult Result(StoredFiscalDailyReportBrCorrectionRevision revision, bool replayed) =>
        new(revision.Id, revision.RootSubmissionId, revision.SignedArtifactId, revision.PreviousRevisionId,
            revision.OrganizationId, revision.IssuerRuc, revision.SummaryDate, revision.Sequence, revision.LocalRevision,
            revision.ProjectionFingerprint, revision.SignedContentHash, revision.State, revision.AttemptCount,
            revision.DgiReceiverId, revision.AckStateCode, revision.FailureCode, replayed);
}

/// <summary>
/// Dispatches one already-signed BR correction revision. Replays never sign again. Unknown delivery is
/// never retried automatically. A later BR can authorize another local revision under the same SecEnvio.
/// </summary>
public sealed class DispatchFiscalDailyReportBrCorrectionUseCase
{
    private readonly IFiscalDailyReportSubmissionRepository _submissions;
    private readonly IFiscalDailyReportBrCorrectionRepository _revisions;
    private readonly IFiscalDailyReportBrAckEvidenceParser _ackParser;
    private readonly IFiscalDailyReportTransportGateway _gateway;
    private readonly IFiscalDailyReportTransportClock _clock;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public DispatchFiscalDailyReportBrCorrectionUseCase(
        IFiscalDailyReportSubmissionRepository submissions,
        IFiscalDailyReportBrCorrectionRepository revisions,
        IFiscalDailyReportBrAckEvidenceParser ackParser,
        IFiscalDailyReportTransportGateway gateway,
        IFiscalDailyReportTransportClock clock,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _submissions = submissions;
        _revisions = revisions;
        _ackParser = ackParser;
        _gateway = gateway;
        _clock = clock;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public async Task<FiscalDailyReportBrCorrectionResult> ExecuteAsync(
        DispatchFiscalDailyReportBrCorrectionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.LocalRevision < 2)
            throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation("fiscal.daily_report.br_correction.local_revision_invalid", "Local correction revision must be at least 2.");

        var dispatch = await _transactions.ExecuteAsync(async ct =>
        {
            _ = await _submissions.GetByIdentityAsync(command.OrganizationId, command.IssuerRuc, command.SummaryDate, command.Sequence, ct)
                ?? throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.br_correction.root_submission_required",
                    "Stable original Reporte Diario submission is required before corrected dispatch.",
                    "missing_prerequisite");
            var revision = await RequiredRevision(command, ct);
            revision.EnsureIntegrity();
            if (revision.State is FiscalDailyReportSubmissionState.Received or FiscalDailyReportSubmissionState.Rejected)
                return (revision, true);
            if (revision.State is FiscalDailyReportSubmissionState.InFlight or FiscalDailyReportSubmissionState.Unknown)
            {
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.br_correction.reconciliation_required",
                    "Corrected Reporte Diario delivery outcome is not safe to retry automatically; reconcile with DGI first.",
                    "transport_outcome_unknown");
            }

            var now = PrepareFiscalDailyReportSubmissionUseCase.WholeSecond(_clock.UtcNow);
            var inFlight = revision with
            {
                State = FiscalDailyReportSubmissionState.InFlight,
                AttemptCount = revision.AttemptCount + 1,
                LastAttemptAtUtc = now,
                FailureCode = null
            };
            await _revisions.UpdateAsync(inFlight, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return (inFlight, false);
        }, cancellationToken);

        if (dispatch.Item2)
            return PrepareFiscalDailyReportBrCorrectionUseCase.Result(dispatch.Item1, true);

        var inFlight = dispatch.Item1;
        try
        {
            var response = await _gateway.SendAsync(new FiscalDailyReportTransportRequest(
                inFlight.OrganizationId, inFlight.IssuerRuc, inFlight.SummaryDate, inFlight.Sequence,
                inFlight.SignedContentHash, inFlight.SignedXml), cancellationToken);
            if (response is null || string.IsNullOrWhiteSpace(response.AckXml)
                || response.AckStateCode is not ("AR" or "BR"))
            {
                throw new FiscalDailyReportTransportException(
                    "fiscal.daily_report.br_correction.response_invalid",
                    "DGI transport returned an invalid immediate response for corrected Reporte Diario.",
                    true);
            }
            return await CompleteAsync(inFlight, response, cancellationToken);
        }
        catch (FiscalDailyReportTransportException ex)
        {
            return ex.DeliveryAmbiguous
                ? await MarkUnknownAsync(inFlight, ex.Code, CancellationToken.None)
                : await ResetPreparedAsync(inFlight, ex.Code, CancellationToken.None);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return await MarkUnknownAsync(inFlight, "fiscal.daily_report.br_correction.unexpected_failure", CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            return await MarkUnknownAsync(inFlight, "fiscal.daily_report.br_correction.cancelled_after_dispatch", CancellationToken.None);
        }
    }

    private async Task<StoredFiscalDailyReportBrCorrectionRevision> RequiredRevision(
        DispatchFiscalDailyReportBrCorrectionCommand command,
        CancellationToken cancellationToken) =>
        await _revisions.GetByRevisionAsync(command.OrganizationId, command.IssuerRuc, command.SummaryDate,
            command.Sequence, command.LocalRevision, cancellationToken)
        ?? throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
            "fiscal.daily_report.br_correction.revision_required",
            "A prepared corrected Reporte Diario revision is required before dispatch.",
            "missing_prerequisite");

    private Task<FiscalDailyReportBrCorrectionResult> CompleteAsync(
        StoredFiscalDailyReportBrCorrectionRevision expected,
        FiscalDailyReportTransportResponse response,
        CancellationToken cancellationToken) =>
        _transactions.ExecuteAsync(async ct =>
        {
            _ = await _submissions.GetByIdentityAsync(expected.OrganizationId, expected.IssuerRuc, expected.SummaryDate, expected.Sequence, ct)
                ?? throw MissingAfterSend();
            var current = await _revisions.GetByRevisionAsync(expected.OrganizationId, expected.IssuerRuc,
                expected.SummaryDate, expected.Sequence, expected.LocalRevision, ct) ?? throw MissingAfterSend();
            if (current.State != FiscalDailyReportSubmissionState.InFlight || current.AttemptCount != expected.AttemptCount)
            {
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.br_correction.concurrent_completion",
                    "Corrected Reporte Diario revision changed while the DGI request was in flight.",
                    "concurrent_change");
            }

            var now = PrepareFiscalDailyReportSubmissionUseCase.WholeSecond(_clock.UtcNow);
            string? reasonsJson = null;
            string? evidenceFailure = null;
            if (response.AckStateCode == "BR")
            {
                var parsed = _ackParser.Parse(response.AckXml);
                if (parsed.IsValid && FiscalDailyReportRejectionReasonEvidence.TryValidate(parsed.Reasons, out _))
                    reasonsJson = FiscalDailyReportRejectionReasonEvidence.Serialize(parsed.Reasons);
                else
                    evidenceFailure = parsed.FailureCode ?? "fiscal.daily_report.br_correction.rejection_reason_evidence_missing";
            }

            var completed = current with
            {
                State = response.AckStateCode == "AR" ? FiscalDailyReportSubmissionState.Received : FiscalDailyReportSubmissionState.Rejected,
                CompletedAtUtc = now,
                DgiReceiverId = string.IsNullOrWhiteSpace(response.DgiReceiverId) ? null : response.DgiReceiverId.Trim(),
                AckStateCode = response.AckStateCode,
                AckXml = response.AckXml,
                AckReasonsJson = reasonsJson,
                FailureCode = evidenceFailure
            };
            await _revisions.UpdateAsync(completed, ct);
            await AppendCompletionEvidence(completed, now, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return PrepareFiscalDailyReportBrCorrectionUseCase.Result(completed, false);
        }, cancellationToken);

    private Task<FiscalDailyReportBrCorrectionResult> ResetPreparedAsync(
        StoredFiscalDailyReportBrCorrectionRevision expected,
        string failureCode,
        CancellationToken cancellationToken) =>
        _transactions.ExecuteAsync(async ct =>
        {
            _ = await _submissions.GetByIdentityAsync(expected.OrganizationId, expected.IssuerRuc, expected.SummaryDate, expected.Sequence, ct)
                ?? throw MissingAfterSend();
            var current = await _revisions.GetByRevisionAsync(expected.OrganizationId, expected.IssuerRuc,
                expected.SummaryDate, expected.Sequence, expected.LocalRevision, ct) ?? throw MissingAfterSend();
            var reset = current with { State = FiscalDailyReportSubmissionState.Prepared, FailureCode = failureCode };
            await _revisions.UpdateAsync(reset, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return PrepareFiscalDailyReportBrCorrectionUseCase.Result(reset, false);
        }, cancellationToken);

    private Task<FiscalDailyReportBrCorrectionResult> MarkUnknownAsync(
        StoredFiscalDailyReportBrCorrectionRevision expected,
        string failureCode,
        CancellationToken cancellationToken) =>
        _transactions.ExecuteAsync(async ct =>
        {
            _ = await _submissions.GetByIdentityAsync(expected.OrganizationId, expected.IssuerRuc, expected.SummaryDate, expected.Sequence, ct)
                ?? throw MissingAfterSend();
            var current = await _revisions.GetByRevisionAsync(expected.OrganizationId, expected.IssuerRuc,
                expected.SummaryDate, expected.Sequence, expected.LocalRevision, ct) ?? throw MissingAfterSend();
            var now = PrepareFiscalDailyReportSubmissionUseCase.WholeSecond(_clock.UtcNow);
            var unknown = current with
            {
                State = FiscalDailyReportSubmissionState.Unknown,
                CompletedAtUtc = now,
                FailureCode = failureCode
            };
            await _revisions.UpdateAsync(unknown, ct);
            await AppendCompletionEvidence(unknown, now, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return PrepareFiscalDailyReportBrCorrectionUseCase.Result(unknown, false);
        }, cancellationToken);

    private async Task AppendCompletionEvidence(
        StoredFiscalDailyReportBrCorrectionRevision revision,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        var actor = _actors.Current;
        var correlation = _correlations.Current;
        await _audit.AppendAsync(new AuditEvent(
            Guid.NewGuid(), occurredAt,
            "FISCAL_DAILY_REPORT_BR_CORRECTION_COMPLETED",
            actor.ActorId, revision.OrganizationId, null, null,
            "FiscalDailyReportBrCorrectionRevision", revision.Id.ToString(),
            revision.State == FiscalDailyReportSubmissionState.Unknown ? AuditOutcome.Failed : AuditOutcome.Succeeded,
            correlation.CorrelationId, revision.FailureCode,
            new Dictionary<string, string?>
            {
                ["issuerRuc"] = revision.IssuerRuc,
                ["summaryDate"] = revision.SummaryDate.ToString("yyyy-MM-dd"),
                ["sequence"] = revision.Sequence.ToString(CultureInfo.InvariantCulture),
                ["localRevision"] = revision.LocalRevision.ToString(CultureInfo.InvariantCulture),
                ["state"] = revision.State.ToString(),
                ["ackStateCode"] = revision.AckStateCode,
                ["attemptCount"] = revision.AttemptCount.ToString(CultureInfo.InvariantCulture)
            }), cancellationToken);
        await _outbox.EnqueueAsync(new FiscalDailyReportBrCorrectionCompletedIntegrationEvent(
            Guid.NewGuid(), occurredAt, revision.Id, revision.OrganizationId, revision.IssuerRuc,
            revision.SummaryDate, revision.Sequence, revision.LocalRevision, revision.State, revision.AckStateCode),
            new OutboxContext(correlation.CorrelationId, null, revision.OrganizationId, actor.ActorId), cancellationToken);
    }

    private static ApplicationProblemException MissingAfterSend() =>
        PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
            "fiscal.daily_report.br_correction.revision_missing_after_send",
            "Durable corrected Reporte Diario revision disappeared during transport.",
            "invalid_persisted_evidence");
}
