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
