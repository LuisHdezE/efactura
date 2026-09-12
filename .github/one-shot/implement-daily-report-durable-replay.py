from pathlib import Path
from textwrap import dedent

ROOT = Path(__file__).resolve().parents[2]


def write(path: str, content: str) -> None:
    target = ROOT / path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(dedent(content).lstrip(), encoding="utf-8")


def replace_once(path: str, old: str, new: str) -> None:
    target = ROOT / path
    text = target.read_text(encoding="utf-8")
    if text.count(old) != 1:
        raise SystemExit(f"Expected exactly one anchor in {path}: {old!r}; found {text.count(old)}")
    target.write_text(text.replace(old, new, 1), encoding="utf-8")


write(
    "src/Application/Fiscal/FiscalDailyReportDurableWorkflow.cs",
    r'''
    using System.Security.Cryptography;
    using System.Text;
    using EFactura.Application.Common.Auditing;
    using EFactura.Application.Common.Context;
    using EFactura.Application.Common.Errors;
    using EFactura.Application.Common.Messaging;
    using EFactura.Application.Common.Persistence;
    using EFactura.Domain.Common;
    using EFactura.Domain.Fiscal;

    namespace EFactura.Application.Fiscal;

    public sealed record StoredFiscalDailyReportSigningEvidence(
        Guid Id,
        string OrganizationId,
        string IssuerRuc,
        DateOnly SummaryDate,
        int Sequence,
        string FunctionalFormatVersion,
        string ProjectionFingerprint,
        string UnsignedContentHash,
        DateTimeOffset SigningTimestamp);

    public interface IFiscalDailyReportSigningEvidenceRepository
    {
        Task<StoredFiscalDailyReportSigningEvidence?> GetByIdentityAsync(
            string organizationId,
            string issuerRuc,
            DateOnly summaryDate,
            int sequence,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            StoredFiscalDailyReportSigningEvidence evidence,
            CancellationToken cancellationToken = default);
    }

    public sealed record PrepareFiscalDailyReportSigningEvidenceCommand(
        FiscalDailyReportWireProjection Projection);

    public sealed record FiscalDailyReportSigningEvidenceResult(
        Guid SigningEvidenceId,
        string OrganizationId,
        string IssuerRuc,
        DateOnly SummaryDate,
        int Sequence,
        string ProjectionFingerprint,
        string UnsignedContentHash,
        DateTimeOffset SigningTimestamp,
        bool Replayed);

    public sealed record FiscalDailyReportSigningEvidenceEstablishedIntegrationEvent(
        Guid EventId,
        DateTimeOffset OccurredAt,
        Guid SigningEvidenceId,
        string OrganizationId,
        string IssuerRuc,
        DateOnly SummaryDate,
        int Sequence,
        string FunctionalFormatVersion,
        string ProjectionFingerprint,
        string UnsignedContentHash,
        DateTimeOffset SigningTimestamp) : IIntegrationEvent;

    /// <summary>
    /// Freezes Reporte Diario signing evidence before any certificate/private-key boundary is crossed.
    /// Replay resolves durable evidence first, rebuilds the deterministic unsigned report with the stored
    /// timestamp, and never consults the clock again.
    /// </summary>
    public sealed class PrepareFiscalDailyReportSigningEvidenceUseCase
    {
        private readonly IFiscalDailyReportXmlBuilder _builder;
        private readonly IFiscalDailyReportSigningEvidenceRepository _evidence;
        private readonly IFiscalSigningTimeSource _signingTime;
        private readonly ITransactionManager _transactions;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditWriter _audit;
        private readonly IOutboxWriter _outbox;
        private readonly IActorContextAccessor _actors;
        private readonly ICorrelationContextAccessor _correlations;

        public PrepareFiscalDailyReportSigningEvidenceUseCase(
            IFiscalDailyReportXmlBuilder builder,
            IFiscalDailyReportSigningEvidenceRepository evidence,
            IFiscalSigningTimeSource signingTime,
            ITransactionManager transactions,
            IUnitOfWork unitOfWork,
            IAuditWriter audit,
            IOutboxWriter outbox,
            IActorContextAccessor actors,
            ICorrelationContextAccessor correlations)
        {
            _builder = builder;
            _evidence = evidence;
            _signingTime = signingTime;
            _transactions = transactions;
            _unitOfWork = unitOfWork;
            _audit = audit;
            _outbox = outbox;
            _actors = actors;
            _correlations = correlations;
        }

        public Task<FiscalDailyReportSigningEvidenceResult> ExecuteAsync(
            PrepareFiscalDailyReportSigningEvidenceCommand command,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);
            var projection = RequireProjection(command.Projection);

            return _transactions.ExecuteAsync(async ct =>
            {
                var existing = await _evidence.GetByIdentityAsync(
                    projection.OrganizationId,
                    projection.IssuerRuc,
                    projection.SummaryDate,
                    projection.Sequence,
                    ct);

                if (existing is not null)
                {
                    var replayUnsigned = Build(projection, existing.SigningTimestamp);
                    EnsureEvidenceMatches(existing, projection, replayUnsigned);
                    return EvidenceResult(existing, true);
                }

                var signingTimestamp = WholeSecond(_signingTime.GetSigningTimestamp());
                var unsigned = Build(projection, signingTimestamp);
                var evidence = new StoredFiscalDailyReportSigningEvidence(
                    Guid.NewGuid(),
                    projection.OrganizationId,
                    projection.IssuerRuc,
                    projection.SummaryDate,
                    projection.Sequence,
                    unsigned.FormatVersion,
                    projection.ProjectionFingerprint,
                    unsigned.ContentHash,
                    signingTimestamp);

                await _evidence.AddAsync(evidence, ct);

                var actor = _actors.Current;
                var correlation = _correlations.Current;
                var occurredAt = signingTimestamp.ToUniversalTime();
                await _audit.AppendAsync(new AuditEvent(
                    Guid.NewGuid(),
                    occurredAt,
                    "FISCAL_DAILY_REPORT_SIGNING_EVIDENCE_ESTABLISHED",
                    actor.ActorId,
                    projection.OrganizationId,
                    null,
                    null,
                    "FiscalDailyReportSigningEvidence",
                    evidence.Id.ToString(),
                    AuditOutcome.Succeeded,
                    correlation.CorrelationId,
                    null,
                    new Dictionary<string, string?>
                    {
                        ["issuerRuc"] = projection.IssuerRuc,
                        ["summaryDate"] = projection.SummaryDate.ToString("yyyy-MM-dd"),
                        ["sequence"] = projection.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["projectionFingerprint"] = projection.ProjectionFingerprint,
                        ["unsignedContentHash"] = unsigned.ContentHash,
                        ["signingTimestamp"] = signingTimestamp.ToString("O")
                    }),
                    ct);

                await _outbox.EnqueueAsync(
                    new FiscalDailyReportSigningEvidenceEstablishedIntegrationEvent(
                        Guid.NewGuid(),
                        occurredAt,
                        evidence.Id,
                        projection.OrganizationId,
                        projection.IssuerRuc,
                        projection.SummaryDate,
                        projection.Sequence,
                        unsigned.FormatVersion,
                        projection.ProjectionFingerprint,
                        unsigned.ContentHash,
                        signingTimestamp),
                    new OutboxContext(
                        correlation.CorrelationId,
                        null,
                        projection.OrganizationId,
                        actor.ActorId),
                    ct);

                await _unitOfWork.SaveChangesAsync(ct);
                return EvidenceResult(evidence, false);
            }, cancellationToken);
        }

        private UnsignedDailyReportArtifact Build(
            FiscalDailyReportWireProjection projection,
            DateTimeOffset signingTimestamp)
        {
            try
            {
                var unsigned = _builder.Build(projection, signingTimestamp);
                if (!string.Equals(unsigned.ProjectionFingerprint, projection.ProjectionFingerprint, StringComparison.Ordinal)
                    || !string.Equals(unsigned.ContentHash, Sha256(unsigned.Xml), StringComparison.Ordinal))
                {
                    throw Conflict(
                        "fiscal.daily_report.signing_evidence.builder_contract_invalid",
                        "Unsigned Reporte Diario builder output does not match the supplied frozen projection.",
                        "provider_contract_violation");
                }
                return unsigned;
            }
            catch (DomainRuleException ex)
            {
                throw Validation(ex.Code, ex.Message);
            }
        }

        internal static void EnsureEvidenceMatches(
            StoredFiscalDailyReportSigningEvidence evidence,
            FiscalDailyReportWireProjection projection,
            UnsignedDailyReportArtifact unsigned)
        {
            if (evidence.Id == Guid.Empty
                || !string.Equals(evidence.OrganizationId, projection.OrganizationId, StringComparison.Ordinal)
                || !string.Equals(evidence.IssuerRuc, projection.IssuerRuc, StringComparison.Ordinal)
                || evidence.SummaryDate != projection.SummaryDate
                || evidence.Sequence != projection.Sequence
                || !string.Equals(evidence.FunctionalFormatVersion, unsigned.FormatVersion, StringComparison.Ordinal)
                || !string.Equals(evidence.ProjectionFingerprint, projection.ProjectionFingerprint, StringComparison.Ordinal)
                || !string.Equals(evidence.UnsignedContentHash, unsigned.ContentHash, StringComparison.Ordinal)
                || evidence.SigningTimestamp != unsigned.SigningTimestamp)
            {
                throw Conflict(
                    "fiscal.daily_report.signing_evidence.replay_mismatch",
                    "Durable Reporte Diario signing evidence no longer matches the requested deterministic projection.",
                    "inconsistent_replay");
            }
        }

        private static FiscalDailyReportWireProjection RequireProjection(FiscalDailyReportWireProjection projection)
        {
            ArgumentNullException.ThrowIfNull(projection);
            if (string.IsNullOrWhiteSpace(projection.OrganizationId))
                throw Validation("fiscal.daily_report.signing_evidence.organization_required", "Organization id is required.");
            if (string.IsNullOrWhiteSpace(projection.IssuerRuc))
                throw Validation("fiscal.daily_report.signing_evidence.ruc_required", "Issuer RUC is required.");
            if (projection.Sequence is < 1 or > 99)
                throw Validation("fiscal.daily_report.signing_evidence.sequence_invalid", "Reporte Diario sequence must be between 1 and 99.");
            RequiredHash(projection.ProjectionFingerprint, "fiscal.daily_report.signing_evidence.projection_fingerprint_invalid");
            return projection;
        }

        private static DateTimeOffset WholeSecond(DateTimeOffset value) =>
            new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, value.Offset);

        private static FiscalDailyReportSigningEvidenceResult EvidenceResult(
            StoredFiscalDailyReportSigningEvidence evidence,
            bool replayed) =>
            new(
                evidence.Id,
                evidence.OrganizationId,
                evidence.IssuerRuc,
                evidence.SummaryDate,
                evidence.Sequence,
                evidence.ProjectionFingerprint,
                evidence.UnsignedContentHash,
                evidence.SigningTimestamp,
                replayed);

        internal static string RequiredHash(string value, string code)
        {
            if (value is null || value.Length != 64 || value.Any(c => !Uri.IsHexDigit(c)))
                throw Validation(code, "Expected a SHA-256 hexadecimal fingerprint.");
            return value.ToLowerInvariant();
        }

        internal static string Sha256(string value) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

        internal static ApplicationProblemException Validation(string code, string message) =>
            new(ApplicationProblemKind.Validation, code, message);

        internal static ApplicationProblemException Conflict(string code, string message, string conflictType) =>
            new(ApplicationProblemKind.Conflict, code, message, conflictType: conflictType);
    }

    public sealed record StoredFiscalDailyReportSignedArtifact(
        Guid Id,
        Guid SigningEvidenceId,
        string OrganizationId,
        string IssuerRuc,
        DateOnly SummaryDate,
        int Sequence,
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
        string SignedXml);

    public interface IFiscalDailyReportSignedArtifactRepository
    {
        Task<StoredFiscalDailyReportSignedArtifact?> GetByIdentityAsync(
            string organizationId,
            string issuerRuc,
            DateOnly summaryDate,
            int sequence,
            CancellationToken cancellationToken = default);

        Task AddAsync(
            StoredFiscalDailyReportSignedArtifact artifact,
            CancellationToken cancellationToken = default);
    }

    public sealed record SignFiscalDailyReportCommand(FiscalDailyReportWireProjection Projection);

    public sealed record FiscalDailyReportSignedArtifactResult(
        Guid SignedArtifactId,
        Guid SigningEvidenceId,
        string OrganizationId,
        string IssuerRuc,
        DateOnly SummaryDate,
        int Sequence,
        string ProjectionFingerprint,
        string UnsignedContentHash,
        string SignedContentHash,
        DateTimeOffset SigningTimestamp,
        string SignatureProfileId,
        string SchemaSetId,
        string SchemaArchiveVersion,
        string SchemaSetFingerprint,
        bool Replayed);

    public sealed record FiscalDailyReportSignedArtifactCreatedIntegrationEvent(
        Guid EventId,
        DateTimeOffset OccurredAt,
        Guid SignedArtifactId,
        Guid SigningEvidenceId,
        string OrganizationId,
        string IssuerRuc,
        DateOnly SummaryDate,
        int Sequence,
        string ProjectionFingerprint,
        string UnsignedContentHash,
        string SignedContentHash,
        DateTimeOffset SigningTimestamp,
        string SignatureProfileId,
        string CertificateThumbprint,
        string CertificateSerialNumber,
        string SchemaSetId,
        string SchemaArchiveVersion,
        string SchemaSetFingerprint) : IIntegrationEvent;

    /// <summary>
    /// Signs a Reporte Diario only after durable signing evidence exists. Replay validates the persisted
    /// signed artifact and the untouched pinned schema before returning without certificate/private-key access.
    /// </summary>
    public sealed class SignFiscalDailyReportUseCase
    {
        private readonly IFiscalDailyReportXmlBuilder _builder;
        private readonly IFiscalDailyReportSigningEvidenceRepository _evidence;
        private readonly IFiscalDailyReportSignedArtifactRepository _artifacts;
        private readonly IFiscalDailyReportSignatureProvider _signatureProvider;
        private readonly IFiscalDailyReportSignedSchemaValidator _schemaValidator;
        private readonly ITransactionManager _transactions;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuditWriter _audit;
        private readonly IOutboxWriter _outbox;
        private readonly IActorContextAccessor _actors;
        private readonly ICorrelationContextAccessor _correlations;

        public SignFiscalDailyReportUseCase(
            IFiscalDailyReportXmlBuilder builder,
            IFiscalDailyReportSigningEvidenceRepository evidence,
            IFiscalDailyReportSignedArtifactRepository artifacts,
            IFiscalDailyReportSignatureProvider signatureProvider,
            IFiscalDailyReportSignedSchemaValidator schemaValidator,
            ITransactionManager transactions,
            IUnitOfWork unitOfWork,
            IAuditWriter audit,
            IOutboxWriter outbox,
            IActorContextAccessor actors,
            ICorrelationContextAccessor correlations)
        {
            _builder = builder;
            _evidence = evidence;
            _artifacts = artifacts;
            _signatureProvider = signatureProvider;
            _schemaValidator = schemaValidator;
            _transactions = transactions;
            _unitOfWork = unitOfWork;
            _audit = audit;
            _outbox = outbox;
            _actors = actors;
            _correlations = correlations;
        }

        public Task<FiscalDailyReportSignedArtifactResult> ExecuteAsync(
            SignFiscalDailyReportCommand command,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);
            var projection = command.Projection ?? throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation(
                "fiscal.daily_report.signed_artifact.projection_required",
                "Reporte Diario projection is required.");

            return _transactions.ExecuteAsync(async ct =>
            {
                var evidence = await _evidence.GetByIdentityAsync(
                    projection.OrganizationId,
                    projection.IssuerRuc,
                    projection.SummaryDate,
                    projection.Sequence,
                    ct)
                    ?? throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                        "fiscal.daily_report.signed_artifact.signing_evidence_required",
                        "Durable Reporte Diario signing evidence must exist before signing.",
                        "missing_prerequisite");

                UnsignedDailyReportArtifact unsigned;
                try
                {
                    unsigned = _builder.Build(projection, evidence.SigningTimestamp);
                }
                catch (DomainRuleException ex)
                {
                    throw PrepareFiscalDailyReportSigningEvidenceUseCase.Validation(ex.Code, ex.Message);
                }
                PrepareFiscalDailyReportSigningEvidenceUseCase.EnsureEvidenceMatches(evidence, projection, unsigned);

                var existing = await _artifacts.GetByIdentityAsync(
                    projection.OrganizationId,
                    projection.IssuerRuc,
                    projection.SummaryDate,
                    projection.Sequence,
                    ct);
                if (existing is not null)
                {
                    EnsureArtifactMatches(existing, evidence, unsigned);
                    var replayValidation = EnsureSchemaValid(existing.SignedXml);
                    EnsureSchemaEvidenceMatches(existing, replayValidation);
                    return ArtifactResult(existing, true);
                }

                var signature = await _signatureProvider.SignAsync(
                    new FiscalDailyReportSignatureRequest(
                        projection.OrganizationId,
                        unsigned.FormatVersion,
                        projection.ProjectionFingerprint,
                        unsigned.Xml,
                        unsigned.ContentHash,
                        evidence.SigningTimestamp),
                    ct);

                if (signature is null || string.IsNullOrWhiteSpace(signature.SignedXml))
                {
                    throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                        "fiscal.daily_report.signed_artifact.signature_result_invalid",
                        "Reporte Diario signature provider returned an empty result.",
                        "provider_contract_violation");
                }

                var signedHash = PrepareFiscalDailyReportSigningEvidenceUseCase.Sha256(signature.SignedXml);
                if (!string.Equals(signedHash, signature.SignedContentHash, StringComparison.Ordinal))
                {
                    throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                        "fiscal.daily_report.signed_artifact.signed_hash_mismatch",
                        "Reporte Diario signed bytes do not match the provider hash.",
                        "provider_contract_violation");
                }

                var schema = EnsureSchemaValid(signature.SignedXml);
                var artifact = new StoredFiscalDailyReportSignedArtifact(
                    Guid.NewGuid(),
                    evidence.Id,
                    evidence.OrganizationId,
                    evidence.IssuerRuc,
                    evidence.SummaryDate,
                    evidence.Sequence,
                    evidence.FunctionalFormatVersion,
                    evidence.ProjectionFingerprint,
                    evidence.UnsignedContentHash,
                    signedHash,
                    evidence.SigningTimestamp,
                    Required(signature.SignatureProfileId, 120, "fiscal.daily_report.signed_artifact.profile_required"),
                    Required(signature.CertificateThumbprint, 160, "fiscal.daily_report.signed_artifact.certificate_thumbprint_required"),
                    Required(signature.CertificateSerialNumber, 160, "fiscal.daily_report.signed_artifact.certificate_serial_required"),
                    Required(schema.SchemaSetId, 120, "fiscal.daily_report.signed_artifact.schema_set_required"),
                    Required(schema.FunctionalFormatVersion, 40, "fiscal.daily_report.signed_artifact.schema_format_required"),
                    Required(schema.SchemaArchiveVersion, 40, "fiscal.daily_report.signed_artifact.schema_archive_required"),
                    PrepareFiscalDailyReportSigningEvidenceUseCase.RequiredHash(
                        schema.SchemaSetFingerprint,
                        "fiscal.daily_report.signed_artifact.schema_fingerprint_required"),
                    signature.SignedXml);

                await _artifacts.AddAsync(artifact, ct);

                var actor = _actors.Current;
                var correlation = _correlations.Current;
                var occurredAt = evidence.SigningTimestamp.ToUniversalTime();
                await _audit.AppendAsync(new AuditEvent(
                    Guid.NewGuid(),
                    occurredAt,
                    "FISCAL_DAILY_REPORT_SIGNED_ARTIFACT_CREATED",
                    actor.ActorId,
                    evidence.OrganizationId,
                    null,
                    null,
                    "FiscalDailyReportSignedArtifact",
                    artifact.Id.ToString(),
                    AuditOutcome.Succeeded,
                    correlation.CorrelationId,
                    null,
                    new Dictionary<string, string?>
                    {
                        ["signingEvidenceId"] = evidence.Id.ToString(),
                        ["issuerRuc"] = evidence.IssuerRuc,
                        ["summaryDate"] = evidence.SummaryDate.ToString("yyyy-MM-dd"),
                        ["sequence"] = evidence.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["projectionFingerprint"] = evidence.ProjectionFingerprint,
                        ["unsignedContentHash"] = evidence.UnsignedContentHash,
                        ["signedContentHash"] = signedHash,
                        ["signatureProfileId"] = artifact.SignatureProfileId,
                        ["schemaSetId"] = artifact.SchemaSetId,
                        ["schemaArchiveVersion"] = artifact.SchemaArchiveVersion,
                        ["schemaSetFingerprint"] = artifact.SchemaSetFingerprint
                    }),
                    ct);

                await _outbox.EnqueueAsync(
                    new FiscalDailyReportSignedArtifactCreatedIntegrationEvent(
                        Guid.NewGuid(),
                        occurredAt,
                        artifact.Id,
                        evidence.Id,
                        evidence.OrganizationId,
                        evidence.IssuerRuc,
                        evidence.SummaryDate,
                        evidence.Sequence,
                        evidence.ProjectionFingerprint,
                        evidence.UnsignedContentHash,
                        signedHash,
                        evidence.SigningTimestamp,
                        artifact.SignatureProfileId,
                        artifact.CertificateThumbprint,
                        artifact.CertificateSerialNumber,
                        artifact.SchemaSetId,
                        artifact.SchemaArchiveVersion,
                        artifact.SchemaSetFingerprint),
                    new OutboxContext(
                        correlation.CorrelationId,
                        null,
                        evidence.OrganizationId,
                        actor.ActorId),
                    ct);

                await _unitOfWork.SaveChangesAsync(ct);
                return ArtifactResult(artifact, false);
            }, cancellationToken);
        }

        private FiscalDailyReportSignedSchemaValidationResult EnsureSchemaValid(string signedXml)
        {
            var validation = _schemaValidator.Validate(signedXml);
            ArgumentNullException.ThrowIfNull(validation);
            if (validation.Status == FiscalDailyReportSignedSchemaValidationStatus.SchemaSetInvalid)
            {
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.signed_artifact.schema_set_invalid",
                    ValidationMessage("Pinned Reporte Diario schema set cannot be used safely.", validation),
                    "schema_set_invalid");
            }
            if (!validation.IsValid || validation.Status != FiscalDailyReportSignedSchemaValidationStatus.Valid)
            {
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.signed_artifact.xsd_invalid",
                    ValidationMessage("Signed Reporte Diario failed untouched-root XSD validation.", validation),
                    "schema_validation_failed");
            }
            return validation;
        }

        private static string ValidationMessage(
            string prefix,
            FiscalDailyReportSignedSchemaValidationResult validation)
        {
            var detail = validation.Errors.FirstOrDefault();
            return detail is null
                ? $"{prefix} Schema={validation.SchemaSetId}."
                : $"{prefix} Schema={validation.SchemaSetId}; {detail.Code}: {detail.Message}";
        }

        private static void EnsureArtifactMatches(
            StoredFiscalDailyReportSignedArtifact artifact,
            StoredFiscalDailyReportSigningEvidence evidence,
            UnsignedDailyReportArtifact unsigned)
        {
            if (artifact.Id == Guid.Empty
                || artifact.SigningEvidenceId != evidence.Id
                || !string.Equals(artifact.OrganizationId, evidence.OrganizationId, StringComparison.Ordinal)
                || !string.Equals(artifact.IssuerRuc, evidence.IssuerRuc, StringComparison.Ordinal)
                || artifact.SummaryDate != evidence.SummaryDate
                || artifact.Sequence != evidence.Sequence
                || !string.Equals(artifact.FunctionalFormatVersion, evidence.FunctionalFormatVersion, StringComparison.Ordinal)
                || !string.Equals(artifact.ProjectionFingerprint, evidence.ProjectionFingerprint, StringComparison.Ordinal)
                || !string.Equals(artifact.UnsignedContentHash, unsigned.ContentHash, StringComparison.Ordinal)
                || artifact.SigningTimestamp != evidence.SigningTimestamp
                || !string.Equals(
                    artifact.SignedContentHash,
                    PrepareFiscalDailyReportSigningEvidenceUseCase.Sha256(artifact.SignedXml),
                    StringComparison.Ordinal))
            {
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.signed_artifact.replay_mismatch",
                    "Persisted signed Reporte Diario no longer matches durable signing evidence.",
                    "inconsistent_replay");
            }

            Required(artifact.SignatureProfileId, 120, "fiscal.daily_report.signed_artifact.profile_required");
            Required(artifact.CertificateThumbprint, 160, "fiscal.daily_report.signed_artifact.certificate_thumbprint_required");
            Required(artifact.CertificateSerialNumber, 160, "fiscal.daily_report.signed_artifact.certificate_serial_required");
            Required(artifact.SchemaSetId, 120, "fiscal.daily_report.signed_artifact.schema_set_required");
            Required(artifact.SchemaFunctionalFormatVersion, 40, "fiscal.daily_report.signed_artifact.schema_format_required");
            Required(artifact.SchemaArchiveVersion, 40, "fiscal.daily_report.signed_artifact.schema_archive_required");
            PrepareFiscalDailyReportSigningEvidenceUseCase.RequiredHash(
                artifact.SchemaSetFingerprint,
                "fiscal.daily_report.signed_artifact.schema_fingerprint_required");
        }

        private static void EnsureSchemaEvidenceMatches(
            StoredFiscalDailyReportSignedArtifact artifact,
            FiscalDailyReportSignedSchemaValidationResult validation)
        {
            if (!string.Equals(artifact.SchemaSetId, validation.SchemaSetId, StringComparison.Ordinal)
                || !string.Equals(artifact.SchemaFunctionalFormatVersion, validation.FunctionalFormatVersion, StringComparison.Ordinal)
                || !string.Equals(artifact.SchemaArchiveVersion, validation.SchemaArchiveVersion, StringComparison.Ordinal)
                || !string.Equals(artifact.SchemaSetFingerprint, validation.SchemaSetFingerprint, StringComparison.Ordinal))
            {
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    "fiscal.daily_report.signed_artifact.schema_evidence_mismatch",
                    "Persisted Reporte Diario schema evidence no longer matches the pinned validator baseline.",
                    "inconsistent_replay");
            }
        }

        private static string Required(string value, int maxLength, string code)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
                throw PrepareFiscalDailyReportSigningEvidenceUseCase.Conflict(
                    code,
                    "Required Reporte Diario durable metadata is invalid.",
                    "provider_contract_violation");
            return value.Trim();
        }

        private static FiscalDailyReportSignedArtifactResult ArtifactResult(
            StoredFiscalDailyReportSignedArtifact artifact,
            bool replayed) =>
            new(
                artifact.Id,
                artifact.SigningEvidenceId,
                artifact.OrganizationId,
                artifact.IssuerRuc,
                artifact.SummaryDate,
                artifact.Sequence,
                artifact.ProjectionFingerprint,
                artifact.UnsignedContentHash,
                artifact.SignedContentHash,
                artifact.SigningTimestamp,
                artifact.SignatureProfileId,
                artifact.SchemaSetId,
                artifact.SchemaArchiveVersion,
                artifact.SchemaSetFingerprint,
                replayed);
    }
    '''
)

write(
    "src/Infrastructure/Persistence/V1/Write/Models/FiscalDailyReportRecords.cs",
    r'''
    namespace Infrastructure.Persistence.V1.Write.Models;

    public sealed class V1FiscalDailyReportSigningEvidenceRecord
    {
        public Guid Id { get; set; }
        public string OrganizationId { get; set; } = string.Empty;
        public string IssuerRuc { get; set; } = string.Empty;
        public DateTime SummaryDate { get; set; }
        public int Sequence { get; set; }
        public string FunctionalFormatVersion { get; set; } = string.Empty;
        public string ProjectionFingerprint { get; set; } = string.Empty;
        public string UnsignedContentHash { get; set; } = string.Empty;
        public DateTimeOffset SigningTimestamp { get; set; }
    }

    public sealed class V1FiscalDailyReportSignedArtifactRecord
    {
        public Guid Id { get; set; }
        public Guid SigningEvidenceId { get; set; }
        public string OrganizationId { get; set; } = string.Empty;
        public string IssuerRuc { get; set; } = string.Empty;
        public DateTime SummaryDate { get; set; }
        public int Sequence { get; set; }
        public string FunctionalFormatVersion { get; set; } = string.Empty;
        public string ProjectionFingerprint { get; set; } = string.Empty;
        public string UnsignedContentHash { get; set; } = string.Empty;
        public string SignedContentHash { get; set; } = string.Empty;
        public DateTimeOffset SigningTimestamp { get; set; }
        public string SignatureProfileId { get; set; } = string.Empty;
        public string CertificateThumbprint { get; set; } = string.Empty;
        public string CertificateSerialNumber { get; set; } = string.Empty;
        public string SchemaSetId { get; set; } = string.Empty;
        public string SchemaFunctionalFormatVersion { get; set; } = string.Empty;
        public string SchemaArchiveVersion { get; set; } = string.Empty;
        public string SchemaSetFingerprint { get; set; } = string.Empty;
        public string SignedXml { get; set; } = string.Empty;
    }
    '''
)

write(
    "src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalDailyReportRepositories.cs",
    r'''
    using EFactura.Application.Fiscal;
    using Infrastructure.Persistence.V1.Write.Models;
    using Microsoft.EntityFrameworkCore;

    namespace Infrastructure.Persistence.V1.Write.Repositories;

    public sealed class EfFiscalDailyReportSigningEvidenceRepository : IFiscalDailyReportSigningEvidenceRepository
    {
        private readonly V1PersistenceDbContext _dbContext;

        public EfFiscalDailyReportSigningEvidenceRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

        public async Task<StoredFiscalDailyReportSigningEvidence?> GetByIdentityAsync(
            string organizationId,
            string issuerRuc,
            DateOnly summaryDate,
            int sequence,
            CancellationToken cancellationToken = default)
        {
            var date = summaryDate.ToDateTime(TimeOnly.MinValue);
            var record = await _dbContext.Set<V1FiscalDailyReportSigningEvidenceRecord>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.OrganizationId == organizationId
                        && x.IssuerRuc == issuerRuc
                        && x.SummaryDate == date
                        && x.Sequence == sequence,
                    cancellationToken);

            return record is null ? null : new StoredFiscalDailyReportSigningEvidence(
                record.Id,
                record.OrganizationId,
                record.IssuerRuc,
                DateOnly.FromDateTime(record.SummaryDate),
                record.Sequence,
                record.FunctionalFormatVersion,
                record.ProjectionFingerprint,
                record.UnsignedContentHash,
                record.SigningTimestamp);
        }

        public Task AddAsync(
            StoredFiscalDailyReportSigningEvidence evidence,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(evidence);
            _dbContext.Set<V1FiscalDailyReportSigningEvidenceRecord>().Add(new V1FiscalDailyReportSigningEvidenceRecord
            {
                Id = evidence.Id,
                OrganizationId = evidence.OrganizationId,
                IssuerRuc = evidence.IssuerRuc,
                SummaryDate = evidence.SummaryDate.ToDateTime(TimeOnly.MinValue),
                Sequence = evidence.Sequence,
                FunctionalFormatVersion = evidence.FunctionalFormatVersion,
                ProjectionFingerprint = evidence.ProjectionFingerprint,
                UnsignedContentHash = evidence.UnsignedContentHash,
                SigningTimestamp = evidence.SigningTimestamp
            });
            return Task.CompletedTask;
        }
    }

    public sealed class EfFiscalDailyReportSignedArtifactRepository : IFiscalDailyReportSignedArtifactRepository
    {
        private readonly V1PersistenceDbContext _dbContext;

        public EfFiscalDailyReportSignedArtifactRepository(V1PersistenceDbContext dbContext) => _dbContext = dbContext;

        public async Task<StoredFiscalDailyReportSignedArtifact?> GetByIdentityAsync(
            string organizationId,
            string issuerRuc,
            DateOnly summaryDate,
            int sequence,
            CancellationToken cancellationToken = default)
        {
            var date = summaryDate.ToDateTime(TimeOnly.MinValue);
            var record = await _dbContext.Set<V1FiscalDailyReportSignedArtifactRecord>()
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.OrganizationId == organizationId
                        && x.IssuerRuc == issuerRuc
                        && x.SummaryDate == date
                        && x.Sequence == sequence,
                    cancellationToken);

            return record is null ? null : new StoredFiscalDailyReportSignedArtifact(
                record.Id,
                record.SigningEvidenceId,
                record.OrganizationId,
                record.IssuerRuc,
                DateOnly.FromDateTime(record.SummaryDate),
                record.Sequence,
                record.FunctionalFormatVersion,
                record.ProjectionFingerprint,
                record.UnsignedContentHash,
                record.SignedContentHash,
                record.SigningTimestamp,
                record.SignatureProfileId,
                record.CertificateThumbprint,
                record.CertificateSerialNumber,
                record.SchemaSetId,
                record.SchemaFunctionalFormatVersion,
                record.SchemaArchiveVersion,
                record.SchemaSetFingerprint,
                record.SignedXml);
        }

        public Task AddAsync(
            StoredFiscalDailyReportSignedArtifact artifact,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(artifact);
            _dbContext.Set<V1FiscalDailyReportSignedArtifactRecord>().Add(new V1FiscalDailyReportSignedArtifactRecord
            {
                Id = artifact.Id,
                SigningEvidenceId = artifact.SigningEvidenceId,
                OrganizationId = artifact.OrganizationId,
                IssuerRuc = artifact.IssuerRuc,
                SummaryDate = artifact.SummaryDate.ToDateTime(TimeOnly.MinValue),
                Sequence = artifact.Sequence,
                FunctionalFormatVersion = artifact.FunctionalFormatVersion,
                ProjectionFingerprint = artifact.ProjectionFingerprint,
                UnsignedContentHash = artifact.UnsignedContentHash,
                SignedContentHash = artifact.SignedContentHash,
                SigningTimestamp = artifact.SigningTimestamp,
                SignatureProfileId = artifact.SignatureProfileId,
                CertificateThumbprint = artifact.CertificateThumbprint,
                CertificateSerialNumber = artifact.CertificateSerialNumber,
                SchemaSetId = artifact.SchemaSetId,
                SchemaFunctionalFormatVersion = artifact.SchemaFunctionalFormatVersion,
                SchemaArchiveVersion = artifact.SchemaArchiveVersion,
                SchemaSetFingerprint = artifact.SchemaSetFingerprint,
                SignedXml = artifact.SignedXml
            });
            return Task.CompletedTask;
        }
    }
    '''
)

write(
    "src/Infrastructure/Persistence/V1/Migrations/20260912032000_V1FiscalDailyReportDurableReplay.cs",
    r'''
    using Infrastructure.Persistence.V1.Write;
    using Microsoft.EntityFrameworkCore.Infrastructure;
    using Microsoft.EntityFrameworkCore.Migrations;

    namespace Infrastructure.Persistence.V1.Migrations;

    [DbContext(typeof(V1PersistenceDbContext))]
    [Migration("20260912032000_V1FiscalDailyReportDurableReplay")]
    public sealed class V1FiscalDailyReportDurableReplay : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "v1_fiscal_daily_report_signing_evidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                    IssuerRuc = table.Column<string>(maxLength: 12, nullable: false),
                    SummaryDate = table.Column<DateTime>(type: "date", nullable: false),
                    Sequence = table.Column<int>(nullable: false),
                    FunctionalFormatVersion = table.Column<string>(maxLength: 40, nullable: false),
                    ProjectionFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                    UnsignedContentHash = table.Column<string>(maxLength: 64, nullable: false),
                    SigningTimestamp = table.Column<DateTimeOffset>(precision: 0, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_v1_fdr_signing_evidence", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_v1_fdr_evidence_identity",
                table: "v1_fiscal_daily_report_signing_evidence",
                columns: new[] { "OrganizationId", "IssuerRuc", "SummaryDate", "Sequence" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "v1_fiscal_daily_report_signed_artifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    SigningEvidenceId = table.Column<Guid>(nullable: false),
                    OrganizationId = table.Column<string>(maxLength: 200, nullable: false),
                    IssuerRuc = table.Column<string>(maxLength: 12, nullable: false),
                    SummaryDate = table.Column<DateTime>(type: "date", nullable: false),
                    Sequence = table.Column<int>(nullable: false),
                    FunctionalFormatVersion = table.Column<string>(maxLength: 40, nullable: false),
                    ProjectionFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                    UnsignedContentHash = table.Column<string>(maxLength: 64, nullable: false),
                    SignedContentHash = table.Column<string>(maxLength: 64, nullable: false),
                    SigningTimestamp = table.Column<DateTimeOffset>(precision: 0, nullable: false),
                    SignatureProfileId = table.Column<string>(maxLength: 120, nullable: false),
                    CertificateThumbprint = table.Column<string>(maxLength: 160, nullable: false),
                    CertificateSerialNumber = table.Column<string>(maxLength: 160, nullable: false),
                    SchemaSetId = table.Column<string>(maxLength: 120, nullable: false),
                    SchemaFunctionalFormatVersion = table.Column<string>(maxLength: 40, nullable: false),
                    SchemaArchiveVersion = table.Column<string>(maxLength: 40, nullable: false),
                    SchemaSetFingerprint = table.Column<string>(maxLength: 64, nullable: false),
                    SignedXml = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_v1_fdr_signed_artifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_v1_fdr_artifact_evidence",
                        column: x => x.SigningEvidenceId,
                        principalTable: "v1_fiscal_daily_report_signing_evidence",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_v1_fdr_artifact_identity",
                table: "v1_fiscal_daily_report_signed_artifacts",
                columns: new[] { "OrganizationId", "IssuerRuc", "SummaryDate", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_v1_fdr_artifact_evidence",
                table: "v1_fiscal_daily_report_signed_artifacts",
                column: "SigningEvidenceId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "v1_fiscal_daily_report_signed_artifacts");
            migrationBuilder.DropTable(name: "v1_fiscal_daily_report_signing_evidence");
        }
    }
    '''
)

replace_once(
    "src/Infrastructure/Persistence/V1/V1PersistenceModelCustomizer.cs",
    "        ConfigureFiscalSigningEvidence(modelBuilder);\n        ConfigureFiscalSignedArtifact(modelBuilder);",
    "        ConfigureFiscalSigningEvidence(modelBuilder);\n        ConfigureFiscalSignedArtifact(modelBuilder);\n        ConfigureFiscalDailyReportDurability(modelBuilder);"
)

customizer = ROOT / "src/Infrastructure/Persistence/V1/V1PersistenceModelCustomizer.cs"
text = customizer.read_text(encoding="utf-8")
marker = "\n}"
if not text.endswith(marker + "\n") and not text.endswith(marker):
    raise SystemExit("Unexpected V1PersistenceModelCustomizer terminator")
method = dedent(r'''

    private static void ConfigureFiscalDailyReportDurability(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<V1FiscalDailyReportSigningEvidenceRecord>(entity =>
        {
            entity.ToTable("v1_fiscal_daily_report_signing_evidence");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.IssuerRuc).HasMaxLength(12).IsRequired();
            entity.Property(x => x.SummaryDate).HasColumnType("date");
            entity.Property(x => x.FunctionalFormatVersion).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ProjectionFingerprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.UnsignedContentHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SigningTimestamp).HasPrecision(0);
            entity.HasIndex(x => new { x.OrganizationId, x.IssuerRuc, x.SummaryDate, x.Sequence })
                .IsUnique()
                .HasDatabaseName("UX_v1_fdr_evidence_identity");
        });

        modelBuilder.Entity<V1FiscalDailyReportSignedArtifactRecord>(entity =>
        {
            entity.ToTable("v1_fiscal_daily_report_signed_artifacts");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.OrganizationId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.IssuerRuc).HasMaxLength(12).IsRequired();
            entity.Property(x => x.SummaryDate).HasColumnType("date");
            entity.Property(x => x.FunctionalFormatVersion).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ProjectionFingerprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.UnsignedContentHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SignedContentHash).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SigningTimestamp).HasPrecision(0);
            entity.Property(x => x.SignatureProfileId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.CertificateThumbprint).HasMaxLength(160).IsRequired();
            entity.Property(x => x.CertificateSerialNumber).HasMaxLength(160).IsRequired();
            entity.Property(x => x.SchemaSetId).HasMaxLength(120).IsRequired();
            entity.Property(x => x.SchemaFunctionalFormatVersion).HasMaxLength(40).IsRequired();
            entity.Property(x => x.SchemaArchiveVersion).HasMaxLength(40).IsRequired();
            entity.Property(x => x.SchemaSetFingerprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SignedXml).IsRequired();
            entity.HasOne<V1FiscalDailyReportSigningEvidenceRecord>()
                .WithMany()
                .HasForeignKey(x => x.SigningEvidenceId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_v1_fdr_artifact_evidence");
            entity.HasIndex(x => new { x.OrganizationId, x.IssuerRuc, x.SummaryDate, x.Sequence })
                .IsUnique()
                .HasDatabaseName("UX_v1_fdr_artifact_identity");
            entity.HasIndex(x => x.SigningEvidenceId)
                .IsUnique()
                .HasDatabaseName("UX_v1_fdr_artifact_evidence");
        });
    }
''')
idx = text.rfind("\n}")
customizer.write_text(text[:idx] + method + text[idx:], encoding="utf-8")

replace_once(
    "src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs",
    "        services.AddScoped<IFiscalSigningEvidenceRepository, EfFiscalSigningEvidenceRepository>();\n        services.AddScoped<IFiscalSignedArtifactRepository, EfFiscalSignedArtifactRepository>();",
    "        services.AddScoped<IFiscalSigningEvidenceRepository, EfFiscalSigningEvidenceRepository>();\n        services.AddScoped<IFiscalSignedArtifactRepository, EfFiscalSignedArtifactRepository>();\n        services.AddScoped<IFiscalDailyReportSigningEvidenceRepository, EfFiscalDailyReportSigningEvidenceRepository>();\n        services.AddScoped<IFiscalDailyReportSignedArtifactRepository, EfFiscalDailyReportSignedArtifactRepository>();"
)
replace_once(
    "src/Infrastructure/Persistence/V1/V1PersistenceServiceCollectionExtensions.cs",
    "        services.AddScoped<PrepareFiscalSigningEvidenceUseCase>();\n        services.AddScoped<SignFiscalDocumentUseCase>();",
    "        services.AddScoped<PrepareFiscalSigningEvidenceUseCase>();\n        services.AddScoped<SignFiscalDocumentUseCase>();\n        services.AddScoped<PrepareFiscalDailyReportSigningEvidenceUseCase>();\n        services.AddScoped<SignFiscalDailyReportUseCase>();"
)

write(
    "test/ArchitectureTests/FiscalDailyReportDurableReplayArchitectureTests.cs",
    r'''
    using Xunit;

    namespace ArchitectureTests;

    public sealed class FiscalDailyReportDurableReplayArchitectureTests
    {
        private static readonly string RepositoryRoot = FindRepositoryRoot();

        [Fact]
        public void Daily_report_evidence_is_durable_before_private_key_and_replay_checks_storage_first()
        {
            var workflow = Read("src/Application/Fiscal/FiscalDailyReportDurableWorkflow.cs");
            var evidenceLookup = workflow.IndexOf("_evidence.GetByIdentityAsync", StringComparison.Ordinal);
            var clock = workflow.IndexOf("_signingTime.GetSigningTimestamp()", StringComparison.Ordinal);
            var artifactLookup = workflow.IndexOf("_artifacts.GetByIdentityAsync", StringComparison.Ordinal);
            var signer = workflow.IndexOf("_signatureProvider.SignAsync", StringComparison.Ordinal);

            Assert.True(evidenceLookup >= 0);
            Assert.True(clock > evidenceLookup);
            Assert.True(artifactLookup >= 0);
            Assert.True(signer > artifactLookup);
            Assert.Contains("return EvidenceResult(existing, true)", workflow, StringComparison.Ordinal);
            Assert.Contains("return ArtifactResult(existing, true)", workflow, StringComparison.Ordinal);
            Assert.Contains("WholeSecond(_signingTime.GetSigningTimestamp())", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("DateTimeOffset.UtcNow", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("EntityFrameworkCore", workflow, StringComparison.Ordinal);
            Assert.DoesNotContain("HttpClient", workflow, StringComparison.Ordinal);
        }

        [Fact]
        public void Persistence_uses_report_identity_uniqueness_and_keeps_CFE_artifact_tables_separate()
        {
            var migration = Read("src/Infrastructure/Persistence/V1/Migrations/20260912032000_V1FiscalDailyReportDurableReplay.cs");
            var repositories = Read("src/Infrastructure/Persistence/V1/Write/Repositories/EfFiscalDailyReportRepositories.cs");
            var model = Read("src/Infrastructure/Persistence/V1/V1PersistenceModelCustomizer.cs");

            Assert.Contains("v1_fiscal_daily_report_signing_evidence", migration, StringComparison.Ordinal);
            Assert.Contains("v1_fiscal_daily_report_signed_artifacts", migration, StringComparison.Ordinal);
            Assert.Contains("UX_v1_fdr_evidence_identity", migration, StringComparison.Ordinal);
            Assert.Contains("UX_v1_fdr_artifact_identity", migration, StringComparison.Ordinal);
            Assert.Contains("OrganizationId", migration, StringComparison.Ordinal);
            Assert.Contains("IssuerRuc", migration, StringComparison.Ordinal);
            Assert.Contains("SummaryDate", migration, StringComparison.Ordinal);
            Assert.Contains("Sequence", migration, StringComparison.Ordinal);
            Assert.Contains("AsNoTracking()", repositories, StringComparison.Ordinal);
            Assert.DoesNotContain("SaveChanges", repositories, StringComparison.Ordinal);
            Assert.Contains("ConfigureFiscalDailyReportDurability(modelBuilder)", model, StringComparison.Ordinal);
        }

        [Fact]
        public void Implementation_record_preserves_transport_and_formal_DGI_gates()
        {
            var document = Read("documentation/blueprint-api-implementation/48_FISCAL_DAILY_REPORT_DURABLE_REPLAY.md");

            Assert.Contains("RUC + FechaResumen + SecEnvio", document, StringComparison.Ordinal);
            Assert.Contains("private key", document, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("EFACRECEPCIONREPORTE", document, StringComparison.Ordinal);
            Assert.Contains("BLOCKED BY MISSING PRODUCT CAPABILITIES", document, StringComparison.Ordinal);
            Assert.DoesNotContain("READY FOR DGI TESTING", document, StringComparison.OrdinalIgnoreCase);
        }

        private static string Read(string relativePath) =>
            File.ReadAllText(Path.Combine(RepositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

        private static string FindRepositoryRoot()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "api-accounting.sln")))
                    return directory.FullName;
                directory = directory.Parent;
            }
            throw new DirectoryNotFoundException("Repository root containing api-accounting.sln was not found.");
        }
    }
    '''
)

write(
    "test/CrossCuttingTests/FiscalDailyReportDurableReplayTests.cs",
    r'''
    using System.Security.Cryptography;
    using System.Text;
    using EFactura.Application.Common.Auditing;
    using EFactura.Application.Common.Context;
    using EFactura.Application.Common.Errors;
    using EFactura.Application.Common.Messaging;
    using EFactura.Application.Common.Persistence;
    using EFactura.Application.Fiscal;
    using EFactura.Domain.Fiscal;
    using Xunit;

    namespace CrossCuttingTests;

    public sealed class FiscalDailyReportDurableReplayTests
    {
        [Fact]
        public async Task Prepare_freezes_whole_second_timestamp_once_and_replay_never_reads_clock_again()
        {
            var evidence = new InMemoryEvidenceRepository();
            var clock = new CountingClock(new DateTimeOffset(2026, 9, 12, 0, 15, 31, 987, TimeSpan.FromHours(-3)));
            var sut = Prepare(evidence, clock);
            var projection = Projection();

            var first = await sut.ExecuteAsync(new PrepareFiscalDailyReportSigningEvidenceCommand(projection));
            var replay = await sut.ExecuteAsync(new PrepareFiscalDailyReportSigningEvidenceCommand(projection));

            Assert.False(first.Replayed);
            Assert.True(replay.Replayed);
            Assert.Equal(first.SigningEvidenceId, replay.SigningEvidenceId);
            Assert.Equal(new DateTimeOffset(2026, 9, 12, 0, 15, 31, TimeSpan.FromHours(-3)), replay.SigningTimestamp);
            Assert.Equal(1, clock.Calls);
        }

        [Fact]
        public async Task Changed_projection_fingerprint_fails_closed_on_evidence_replay_without_new_clock_access()
        {
            var evidence = new InMemoryEvidenceRepository();
            var clock = new CountingClock(new DateTimeOffset(2026, 9, 12, 0, 15, 31, TimeSpan.FromHours(-3)));
            var sut = Prepare(evidence, clock);
            var projection = Projection();
            await sut.ExecuteAsync(new PrepareFiscalDailyReportSigningEvidenceCommand(projection));

            var error = await Assert.ThrowsAsync<ApplicationProblemException>(() => sut.ExecuteAsync(
                new PrepareFiscalDailyReportSigningEvidenceCommand(
                    projection with { ProjectionFingerprint = Fingerprint('d') })));

            Assert.Equal("fiscal.daily_report.signing_evidence.replay_mismatch", error.Code);
            Assert.Equal(1, clock.Calls);
        }

        [Fact]
        public async Task Signed_artifact_replay_returns_persisted_result_without_crossing_signer_again()
        {
            var evidence = new InMemoryEvidenceRepository();
            var artifacts = new InMemoryArtifactRepository();
            var clock = new CountingClock(new DateTimeOffset(2026, 9, 12, 0, 15, 31, TimeSpan.FromHours(-3)));
            var projection = Projection();
            await Prepare(evidence, clock).ExecuteAsync(new PrepareFiscalDailyReportSigningEvidenceCommand(projection));
            var signer = new CountingSigner();
            var sut = Sign(evidence, artifacts, signer);

            var first = await sut.ExecuteAsync(new SignFiscalDailyReportCommand(projection));
            var replay = await sut.ExecuteAsync(new SignFiscalDailyReportCommand(projection));

            Assert.False(first.Replayed);
            Assert.True(replay.Replayed);
            Assert.Equal(first.SignedArtifactId, replay.SignedArtifactId);
            Assert.Equal(first.SignedContentHash, replay.SignedContentHash);
            Assert.Equal(1, signer.Calls);
        }

        private static PrepareFiscalDailyReportSigningEvidenceUseCase Prepare(
            IFiscalDailyReportSigningEvidenceRepository evidence,
            IFiscalSigningTimeSource clock) =>
            new(
                new DeterministicUnsignedDailyReportXmlBuilder(),
                evidence,
                clock,
                new InlineTransactionManager(),
                new CountingUnitOfWork(),
                new NoOpAuditWriter(),
                new NoOpOutboxWriter(),
                new FixedActorContextAccessor(),
                new FixedCorrelationContextAccessor());

        private static SignFiscalDailyReportUseCase Sign(
            IFiscalDailyReportSigningEvidenceRepository evidence,
            IFiscalDailyReportSignedArtifactRepository artifacts,
            IFiscalDailyReportSignatureProvider signer) =>
            new(
                new DeterministicUnsignedDailyReportXmlBuilder(),
                evidence,
                artifacts,
                signer,
                new AlwaysValidSchemaValidator(),
                new InlineTransactionManager(),
                new CountingUnitOfWork(),
                new NoOpAuditWriter(),
                new NoOpOutboxWriter(),
                new FixedActorContextAccessor(),
                new FixedCorrelationContextAccessor());

        private static FiscalDailyReportWireProjection Projection()
        {
            var row = new FiscalDailyReportWireAmountRow(
                CfeFamily.EFactura,
                new DateOnly(2026, 9, 11),
                "0001",
                false,
                0m, 0m, 0m, 0m, 0m, 100m, 0m, 0m, 22m, 0m,
                null, 22m, 122m, 0m, 0m);
            var counter = new FiscalDailyReportWireTypeCounters(
                CfeFamily.EFactura,
                1,
                0,
                0,
                1,
                [new FiscalDailyReportNumberRange("A", 1, 1)],
                Array.Empty<FiscalDailyReportNumberRange>());
            return new FiscalDailyReportWireProjection(
                "company-1",
                "214748364700",
                new DateOnly(2026, 9, 11),
                1,
                1,
                [row],
                [counter],
                Fingerprint('a'),
                Fingerprint('b'),
                Fingerprint('c'));
        }

        private static string Fingerprint(char value) => new(value, 64);
        private static string Sha256(string value) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

        private sealed class InMemoryEvidenceRepository : IFiscalDailyReportSigningEvidenceRepository
        {
            private StoredFiscalDailyReportSigningEvidence? _value;

            public Task<StoredFiscalDailyReportSigningEvidence?> GetByIdentityAsync(
                string organizationId,
                string issuerRuc,
                DateOnly summaryDate,
                int sequence,
                CancellationToken cancellationToken = default) =>
                Task.FromResult(
                    _value is not null
                    && _value.OrganizationId == organizationId
                    && _value.IssuerRuc == issuerRuc
                    && _value.SummaryDate == summaryDate
                    && _value.Sequence == sequence
                        ? _value
                        : null);

            public Task AddAsync(StoredFiscalDailyReportSigningEvidence evidence, CancellationToken cancellationToken = default)
            {
                _value = evidence;
                return Task.CompletedTask;
            }
        }

        private sealed class InMemoryArtifactRepository : IFiscalDailyReportSignedArtifactRepository
        {
            private StoredFiscalDailyReportSignedArtifact? _value;

            public Task<StoredFiscalDailyReportSignedArtifact?> GetByIdentityAsync(
                string organizationId,
                string issuerRuc,
                DateOnly summaryDate,
                int sequence,
                CancellationToken cancellationToken = default) =>
                Task.FromResult(
                    _value is not null
                    && _value.OrganizationId == organizationId
                    && _value.IssuerRuc == issuerRuc
                    && _value.SummaryDate == summaryDate
                    && _value.Sequence == sequence
                        ? _value
                        : null);

            public Task AddAsync(StoredFiscalDailyReportSignedArtifact artifact, CancellationToken cancellationToken = default)
            {
                _value = artifact;
                return Task.CompletedTask;
            }
        }

        private sealed class CountingClock(DateTimeOffset value) : IFiscalSigningTimeSource
        {
            public int Calls { get; private set; }
            public DateTimeOffset GetSigningTimestamp()
            {
                Calls++;
                return value;
            }
        }

        private sealed class CountingSigner : IFiscalDailyReportSignatureProvider
        {
            public int Calls { get; private set; }

            public Task<FiscalDailyReportSignatureResult> SignAsync(
                FiscalDailyReportSignatureRequest request,
                CancellationToken cancellationToken = default)
            {
                Calls++;
                var signed = request.UnsignedXml + "<!--test-signature-envelope-->";
                return Task.FromResult(new FiscalDailyReportSignatureResult(
                    signed,
                    Sha256(signed),
                    "test-report-profile",
                    "thumbprint",
                    "serial"));
            }
        }

        private sealed class AlwaysValidSchemaValidator : IFiscalDailyReportSignedSchemaValidator
        {
            public FiscalDailyReportSignedSchemaValidationResult Validate(string signedXml) =>
                new(
                    FiscalDailyReportSignedSchemaValidationStatus.Valid,
                    "dgi-fe-1.44.2-reporte-13.2",
                    "13.2",
                    "1.44.2",
                    Fingerprint('e'),
                    Array.Empty<FiscalDailyReportSignedSchemaValidationError>());
        }

        private sealed class InlineTransactionManager : ITransactionManager
        {
            public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
            public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
        }

        private sealed class CountingUnitOfWork : IUnitOfWork
        {
            public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        }

        private sealed class NoOpAuditWriter : IAuditWriter
        {
            public Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private sealed class NoOpOutboxWriter : IOutboxWriter
        {
            public Task EnqueueAsync<TEvent>(TEvent integrationEvent, OutboxContext context, CancellationToken cancellationToken = default)
                where TEvent : IIntegrationEvent => Task.CompletedTask;
        }

        private sealed class FixedActorContextAccessor : IActorContextAccessor
        {
            public ActorContext Current { get; } = new(
                "actor-1",
                "Test Actor",
                true,
                new HashSet<string>(StringComparer.Ordinal),
                new HashSet<string>(StringComparer.Ordinal) { "company-1" },
                new HashSet<string>(StringComparer.Ordinal),
                new HashSet<string>(StringComparer.Ordinal),
                "device-1");
        }

        private sealed class FixedCorrelationContextAccessor : ICorrelationContextAccessor
        {
            public CorrelationContext Current { get; } = new("corr-report-replay", "trace-report-replay");
        }
    }
    '''
)

write(
    "documentation/blueprint-api-implementation/48_FISCAL_DAILY_REPORT_DURABLE_REPLAY.md",
    r'''
    # Fiscal Daily Report durable signing evidence and replay

    ## Scope

    This increment gives the signed Reporte Diario its own durable identity and replay boundary. It does not reuse CFE signed-artifact tables or pretend a Reporte is a CFE.

    The durable business identity is the tuple **RUC + FechaResumen + SecEnvio**, scoped additionally by the internal organization id. The current slice does not allocate `SecEnvio`; it persists and protects the sequence already present in the accepted wire projection.

    ## Before the private key

    `PrepareFiscalDailyReportSigningEvidenceUseCase` runs before any certificate/private key boundary. On first execution it:

    1. receives the deterministic `FiscalDailyReportWireProjection`;
    2. freezes the Uruguay signing timestamp at whole-second precision;
    3. builds the unsigned Reporte Diario including `TmstFirmaEnv`;
    4. persists format version, projection fingerprint, unsigned-content SHA-256 and the frozen timestamp;
    5. emits audit/outbox evidence in the same local transaction.

    On replay, durable evidence is resolved before the clock. The unsigned report is rebuilt with the stored timestamp and must match the stored projection fingerprint and unsigned hash. The clock is not consulted again.

    ## Signed artifact replay

    `SignFiscalDailyReportUseCase` requires the durable evidence above. It rebuilds the exact unsigned payload with the persisted timestamp and resolves the signed artifact before calling `IFiscalDailyReportSignatureProvider`.

    When an artifact already exists, replay verifies:

    - report identity and signing-evidence association;
    - functional format and projection fingerprint;
    - unsigned and signed content hashes;
    - frozen signing timestamp;
    - persisted signature/certificate metadata;
    - current validation against the untouched byte-pinned Reporte schema closure;
    - persisted schema evidence against the current pinned validator baseline.

    A valid replay returns the stored artifact without crossing the private key boundary again. Any mismatch fails closed as inconsistent replay.

    ## Persistence

    Two provider-neutral V1 tables are introduced:

    - `v1_fiscal_daily_report_signing_evidence`;
    - `v1_fiscal_daily_report_signed_artifacts`.

    Both enforce unique report identity by organization, issuer RUC, summary date and sequence. The signed artifact also has a one-to-one unique reference to its signing evidence.

    ## Deliberate non-scope

    This increment still does **not** implement:

    - automatic `SecEnvio` allocation or reliquidation orchestration;
    - DGI `EFACRECEPCIONREPORTE` transport;
    - acknowledgement lifecycle from `Recibido` to `Reporte Procesado`;
    - Sobre v05 packaging;
    - live BCU acquisition or unresolved foreign-currency B-C27 semantics;
    - online certificate revocation / DGI habilitation checks;
    - formal DGI Testing certification or Production readiness.

    Formal traditional DGI Testing readiness remains exactly:

    **BLOCKED BY MISSING PRODUCT CAPABILITIES**
    '''
)

print("Daily Report durable replay slice staged.")
