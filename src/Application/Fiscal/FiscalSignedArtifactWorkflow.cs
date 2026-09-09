using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using EFactura.Application.Common.Auditing;
using EFactura.Application.Common.Context;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Messaging;
using EFactura.Application.Common.Persistence;
using EFactura.Domain.Common;
using EFactura.Domain.Fiscal;

namespace EFactura.Application.Fiscal;

public sealed record StoredFiscalSignedArtifact(
    Guid Id,
    string OrganizationId,
    Guid FiscalDocumentId,
    Guid SigningEvidenceId,
    string FiscalContentFingerprint,
    string UnsignedContentHash,
    string SigningPayloadHash,
    string SignedContentHash,
    DateTimeOffset SigningTimestamp,
    string SignatureProfileId,
    string CertificateThumbprint,
    string CertificateSerialNumber,
    string SchemaSetId,
    string SchemaVersion,
    string SchemaSetFingerprint,
    string SignedXml);

public interface IFiscalSignedArtifactRepository
{
    Task<StoredFiscalSignedArtifact?> GetByFiscalDocumentAsync(
        string organizationId,
        Guid fiscalDocumentId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        StoredFiscalSignedArtifact artifact,
        CancellationToken cancellationToken = default);
}

public sealed record SignFiscalDocumentCommand(
    string OrganizationId,
    Guid FiscalDocumentId);

public sealed record FiscalSignedArtifactResult(
    Guid SignedArtifactId,
    Guid FiscalDocumentId,
    Guid SigningEvidenceId,
    string SigningPayloadHash,
    string SignedContentHash,
    DateTimeOffset SigningTimestamp,
    string SignatureProfileId,
    string CertificateThumbprint,
    string CertificateSerialNumber,
    string SchemaSetId,
    string SchemaVersion,
    string SchemaSetFingerprint,
    bool Replayed);

public sealed record FiscalSignedArtifactCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid SignedArtifactId,
    Guid FiscalDocumentId,
    Guid SigningEvidenceId,
    string OrganizationId,
    string FiscalContentFingerprint,
    string UnsignedContentHash,
    string SigningPayloadHash,
    string SignedContentHash,
    DateTimeOffset SigningTimestamp,
    string SignatureProfileId,
    string CertificateThumbprint,
    string CertificateSerialNumber,
    string SchemaSetId,
    string SchemaVersion,
    string SchemaSetFingerprint) : IIntegrationEvent;

/// <summary>
/// Produces and persists the first durable signed CFE artifact. Replays rebuild the unsigned CFE and
/// deterministic TmstFirma payload from immutable evidence, validate the persisted signed artifact,
/// validate it against the pinned DGI XSD set, and return it without crossing the private-key boundary again.
/// </summary>
public sealed class SignFiscalDocumentUseCase
{
    private static readonly XNamespace XmlDsigNamespace = "http://www.w3.org/2000/09/xmldsig#";

    private readonly IFiscalDocumentRepository _documents;
    private readonly IFiscalContentSnapshotRepository _snapshots;
    private readonly IFiscalSigningEvidenceRepository _signingEvidence;
    private readonly IFiscalXmlBuilder _xmlBuilder;
    private readonly IFiscalSigningPayloadBuilder _payloadBuilder;
    private readonly IFiscalSignatureProvider _signatureProvider;
    private readonly IFiscalSignedCfeSchemaValidator _schemaValidator;
    private readonly IFiscalSignedArtifactRepository _signedArtifacts;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditWriter _audit;
    private readonly IOutboxWriter _outbox;
    private readonly IActorContextAccessor _actors;
    private readonly ICorrelationContextAccessor _correlations;

    public SignFiscalDocumentUseCase(
        IFiscalDocumentRepository documents,
        IFiscalContentSnapshotRepository snapshots,
        IFiscalSigningEvidenceRepository signingEvidence,
        IFiscalXmlBuilder xmlBuilder,
        IFiscalSigningPayloadBuilder payloadBuilder,
        IFiscalSignatureProvider signatureProvider,
        IFiscalSignedCfeSchemaValidator schemaValidator,
        IFiscalSignedArtifactRepository signedArtifacts,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork,
        IAuditWriter audit,
        IOutboxWriter outbox,
        IActorContextAccessor actors,
        ICorrelationContextAccessor correlations)
    {
        _documents = documents;
        _snapshots = snapshots;
        _signingEvidence = signingEvidence;
        _xmlBuilder = xmlBuilder;
        _payloadBuilder = payloadBuilder;
        _signatureProvider = signatureProvider;
        _schemaValidator = schemaValidator;
        _signedArtifacts = signedArtifacts;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
        _audit = audit;
        _outbox = outbox;
        _actors = actors;
        _correlations = correlations;
    }

    public Task<FiscalSignedArtifactResult> ExecuteAsync(
        SignFiscalDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.OrganizationId))
            throw Validation("fiscal.signed_artifact.organization_required", "Organization id is required.");
        if (command.FiscalDocumentId == Guid.Empty)
            throw Validation("fiscal.signed_artifact.document_id_required", "Fiscal document id is required.");

        var organizationId = command.OrganizationId.Trim();
        return _transactions.ExecuteAsync(async ct =>
        {
            var document = await _documents.GetAsync(organizationId, command.FiscalDocumentId, ct)
                ?? throw new ApplicationProblemException(
                    ApplicationProblemKind.NotFound,
                    "fiscal.document_not_found",
                    "Fiscal document was not found.");

            var storedSnapshot = await _snapshots.GetByFiscalDocumentAsync(organizationId, document.Id, ct)
                ?? throw Conflict(
                    "fiscal.signed_artifact.snapshot_required",
                    "Signing requires the immutable fiscal content snapshot.",
                    "missing_prerequisite");

            var evidence = await _signingEvidence.GetByFiscalDocumentAsync(organizationId, document.Id, ct)
                ?? throw Conflict(
                    "fiscal.signed_artifact.signing_evidence_required",
                    "Signing requires durable fiscal signing evidence.",
                    "missing_prerequisite");

            if (evidence.Id == Guid.Empty
                || evidence.FiscalDocumentId != document.Id
                || !string.Equals(evidence.OrganizationId, organizationId, StringComparison.Ordinal))
            {
                throw Conflict(
                    "fiscal.signed_artifact.signing_evidence_association_invalid",
                    "Persisted signing evidence does not belong to the requested fiscal document.",
                    "inconsistent_state");
            }

            FiscalSigningPayload payload;
            try
            {
                storedSnapshot.Snapshot.EnsureIntegrity();
                var unsigned = _xmlBuilder.Build(document, storedSnapshot.Snapshot);
                payload = _payloadBuilder.Build(document.Id, unsigned, evidence);
            }
            catch (DomainRuleException ex)
            {
                throw Conflict(ex.Code, ex.Message, "inconsistent_state");
            }

            var existing = await _signedArtifacts.GetByFiscalDocumentAsync(organizationId, document.Id, ct);
            if (existing is not null)
            {
                EnsureReplayMatches(existing, evidence, payload);
                var replayValidation = EnsureSchemaValid(existing.SignedXml);
                EnsureSchemaEvidenceMatches(existing, replayValidation);
                return Result(existing, true);
            }

            var signature = await _signatureProvider.SignAsync(
                new FiscalSignatureRequest(
                    document.Id,
                    payload.Family,
                    payload.FormatVersion,
                    payload.FiscalContentFingerprint,
                    payload.UnsignedContentHash,
                    payload.Xml,
                    payload.ContentHash,
                    payload.SigningTimestamp),
                ct);

            ValidateSignatureResult(signature, payload);
            var schemaValidation = EnsureSchemaValid(signature.SignedXml);

            var stored = new StoredFiscalSignedArtifact(
                Guid.NewGuid(),
                organizationId,
                document.Id,
                evidence.Id,
                evidence.FiscalContentFingerprint,
                evidence.UnsignedContentHash,
                payload.ContentHash,
                signature.SignedContentHash,
                evidence.SigningTimestamp,
                Required(signature.SignatureProfileId, 120, "fiscal.signed_artifact.profile_required"),
                Required(signature.CertificateThumbprint, 160, "fiscal.signed_artifact.certificate_thumbprint_required"),
                Required(signature.CertificateSerialNumber, 160, "fiscal.signed_artifact.certificate_serial_required"),
                Required(schemaValidation.SchemaSetId, 120, "fiscal.signed_artifact.schema_set_id_required"),
                Required(schemaValidation.SchemaVersion, 40, "fiscal.signed_artifact.schema_version_required"),
                RequiredHash(schemaValidation.SchemaSetFingerprint, "fiscal.signed_artifact.schema_fingerprint_required"),
                signature.SignedXml);

            await _signedArtifacts.AddAsync(stored, ct);

            var occurredAt = evidence.SigningTimestamp.ToUniversalTime();
            var actor = _actors.Current;
            var correlation = _correlations.Current;
            await _audit.AppendAsync(new AuditEvent(
                Guid.NewGuid(),
                occurredAt,
                "FISCAL_SIGNED_ARTIFACT_CREATED",
                actor.ActorId,
                organizationId,
                document.LocationId,
                document.TerminalId,
                "FiscalSignedArtifact",
                stored.Id.ToString(),
                AuditOutcome.Succeeded,
                correlation.CorrelationId,
                null,
                new Dictionary<string, string?>
                {
                    ["fiscalDocumentId"] = document.Id.ToString(),
                    ["signingEvidenceId"] = evidence.Id.ToString(),
                    ["fiscalContentFingerprint"] = evidence.FiscalContentFingerprint,
                    ["unsignedContentHash"] = evidence.UnsignedContentHash,
                    ["signingPayloadHash"] = payload.ContentHash,
                    ["signedContentHash"] = stored.SignedContentHash,
                    ["signingTimestamp"] = evidence.SigningTimestamp.ToString("O"),
                    ["signatureProfileId"] = stored.SignatureProfileId,
                    ["certificateThumbprint"] = stored.CertificateThumbprint,
                    ["certificateSerialNumber"] = stored.CertificateSerialNumber,
                    ["schemaSetId"] = stored.SchemaSetId,
                    ["schemaVersion"] = stored.SchemaVersion,
                    ["schemaSetFingerprint"] = stored.SchemaSetFingerprint
                }),
                ct);

            await _outbox.EnqueueAsync(
                new FiscalSignedArtifactCreatedIntegrationEvent(
                    Guid.NewGuid(),
                    occurredAt,
                    stored.Id,
                    document.Id,
                    evidence.Id,
                    organizationId,
                    evidence.FiscalContentFingerprint,
                    evidence.UnsignedContentHash,
                    payload.ContentHash,
                    stored.SignedContentHash,
                    evidence.SigningTimestamp,
                    stored.SignatureProfileId,
                    stored.CertificateThumbprint,
                    stored.CertificateSerialNumber,
                    stored.SchemaSetId,
                    stored.SchemaVersion,
                    stored.SchemaSetFingerprint),
                new OutboxContext(
                    correlation.CorrelationId,
                    null,
                    organizationId,
                    actor.ActorId),
                ct);

            await _unitOfWork.SaveChangesAsync(ct);
            return Result(stored, false);
        }, cancellationToken);
    }

    private FiscalSignedCfeSchemaValidationResult EnsureSchemaValid(string signedXml)
    {
        var validation = _schemaValidator.Validate(signedXml);
        ArgumentNullException.ThrowIfNull(validation);

        if (validation.Status == FiscalSignedCfeSchemaValidationStatus.SchemaSetInvalid)
        {
            throw Conflict(
                "fiscal.signed_artifact.schema_set_invalid",
                ValidationMessage("Pinned DGI schema set cannot be used safely.", validation),
                "schema_set_invalid");
        }

        if (validation.Status != FiscalSignedCfeSchemaValidationStatus.Valid || !validation.IsValid)
        {
            throw Conflict(
                "fiscal.signed_artifact.xsd_invalid",
                ValidationMessage("Signed CFE failed DGI XSD validation.", validation),
                "schema_validation_failed");
        }

        Required(validation.SchemaSetId, 120, "fiscal.signed_artifact.schema_set_id_required");
        Required(validation.SchemaVersion, 40, "fiscal.signed_artifact.schema_version_required");
        RequiredHash(validation.SchemaSetFingerprint, "fiscal.signed_artifact.schema_fingerprint_required");
        return validation;
    }

    private static string ValidationMessage(
        string prefix,
        FiscalSignedCfeSchemaValidationResult validation)
    {
        var detail = validation.Errors.FirstOrDefault();
        if (detail is null)
            return $"{prefix} Schema={validation.SchemaSetId} v{validation.SchemaVersion}.";
        return $"{prefix} Schema={validation.SchemaSetId} v{validation.SchemaVersion}; {detail.Code}: {detail.Message}";
    }

    private static void EnsureSchemaEvidenceMatches(
        StoredFiscalSignedArtifact existing,
        FiscalSignedCfeSchemaValidationResult validation)
    {
        if (!string.Equals(existing.SchemaSetId, validation.SchemaSetId, StringComparison.Ordinal)
            || !string.Equals(existing.SchemaVersion, validation.SchemaVersion, StringComparison.Ordinal)
            || !string.Equals(existing.SchemaSetFingerprint, validation.SchemaSetFingerprint, StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.signed_artifact.schema_validation_evidence_mismatch",
                "Persisted signed artifact schema evidence no longer matches the pinned validator baseline.",
                "inconsistent_replay");
        }
    }

    private static void ValidateSignatureResult(
        FiscalSignatureResult signature,
        FiscalSigningPayload payload)
    {
        ArgumentNullException.ThrowIfNull(signature);
        if (string.IsNullOrWhiteSpace(signature.SignedXml))
            throw Conflict(
                "fiscal.signed_artifact.signed_xml_required",
                "Signature provider returned empty signed XML.",
                "provider_contract_violation");

        var actualHash = Sha256(signature.SignedXml);
        if (!string.Equals(actualHash, signature.SignedContentHash, StringComparison.Ordinal))
            throw Conflict(
                "fiscal.signed_artifact.signed_hash_mismatch",
                "Signature provider returned signed bytes that do not match its signed-content hash.",
                "provider_contract_violation");

        XDocument signed;
        XDocument original;
        try
        {
            signed = XDocument.Parse(signature.SignedXml, LoadOptions.PreserveWhitespace);
            original = XDocument.Parse(payload.Xml, LoadOptions.PreserveWhitespace);
        }
        catch (XmlException ex)
        {
            throw Conflict(
                "fiscal.signed_artifact.signed_xml_invalid",
                $"Signature provider returned malformed XML: {ex.Message}",
                "provider_contract_violation");
        }

        var signatures = signed.Descendants(XmlDsigNamespace + "Signature").ToArray();
        var rootSignature = signed.Root?.Elements(XmlDsigNamespace + "Signature").FirstOrDefault();
        if (signatures.Length != 1
            || rootSignature is null
            || signed.Root!.Elements().LastOrDefault() != rootSignature)
        {
            throw Conflict(
                "fiscal.signed_artifact.signature_structure_invalid",
                "Signed CFE must contain exactly one root-level ds:Signature as the final CFE child.",
                "provider_contract_violation");
        }

        var unsignedClone = new XDocument(signed);
        unsignedClone.Root!.Elements(XmlDsigNamespace + "Signature").Single().Remove();
        if (!XNode.DeepEquals(original, unsignedClone))
        {
            throw Conflict(
                "fiscal.signed_artifact.payload_mutated",
                "Signature provider changed fiscal payload content while adding ds:Signature.",
                "provider_contract_violation");
        }
    }

    private static void EnsureReplayMatches(
        StoredFiscalSignedArtifact existing,
        FiscalSigningEvidence evidence,
        FiscalSigningPayload payload)
    {
        if (existing.Id == Guid.Empty
            || existing.FiscalDocumentId != evidence.FiscalDocumentId
            || existing.SigningEvidenceId != evidence.Id
            || !string.Equals(existing.OrganizationId, evidence.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(existing.FiscalContentFingerprint, evidence.FiscalContentFingerprint, StringComparison.Ordinal)
            || !string.Equals(existing.UnsignedContentHash, evidence.UnsignedContentHash, StringComparison.Ordinal)
            || !string.Equals(existing.SigningPayloadHash, payload.ContentHash, StringComparison.Ordinal)
            || existing.SigningTimestamp != evidence.SigningTimestamp
            || !string.Equals(existing.SignedContentHash, Sha256(existing.SignedXml), StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.signed_artifact.replay_mismatch",
                "Persisted signed artifact no longer matches durable signing evidence and deterministic payload.",
                "inconsistent_replay");
        }

        ValidateStoredStructure(existing.SignedXml, payload.Xml);
        Required(existing.SignatureProfileId, 120, "fiscal.signed_artifact.profile_required");
        Required(existing.CertificateThumbprint, 160, "fiscal.signed_artifact.certificate_thumbprint_required");
        Required(existing.CertificateSerialNumber, 160, "fiscal.signed_artifact.certificate_serial_required");
        Required(existing.SchemaSetId, 120, "fiscal.signed_artifact.schema_set_id_required");
        Required(existing.SchemaVersion, 40, "fiscal.signed_artifact.schema_version_required");
        RequiredHash(existing.SchemaSetFingerprint, "fiscal.signed_artifact.schema_fingerprint_required");
    }

    private static void ValidateStoredStructure(string signedXml, string payloadXml)
    {
        var signature = new FiscalSignatureResult(
            signedXml,
            Sha256(signedXml),
            "replay",
            "replay",
            "replay");
        var payload = new FiscalSigningPayload(
            Guid.NewGuid(),
            CfeFamily.EFactura,
            "replay",
            new string('a', 64),
            new string('b', 64),
            DateTimeOffset.UnixEpoch,
            payloadXml,
            Sha256(payloadXml));
        ValidateSignatureResult(signature, payload);
    }

    private static FiscalSignedArtifactResult Result(
        StoredFiscalSignedArtifact artifact,
        bool replayed) =>
        new(
            artifact.Id,
            artifact.FiscalDocumentId,
            artifact.SigningEvidenceId,
            artifact.SigningPayloadHash,
            artifact.SignedContentHash,
            artifact.SigningTimestamp,
            artifact.SignatureProfileId,
            artifact.CertificateThumbprint,
            artifact.CertificateSerialNumber,
            artifact.SchemaSetId,
            artifact.SchemaVersion,
            artifact.SchemaSetFingerprint,
            replayed);

    private static string Required(string value, int maxLength, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw Conflict(code, "Required signed-artifact evidence is missing.", "provider_contract_violation");
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
            throw Conflict(code, "Signed-artifact evidence exceeds its supported length.", "provider_contract_violation");
        return normalized;
    }

    private static string RequiredHash(string value, string code)
    {
        var normalized = Required(value, 64, code);
        if (normalized.Length != 64 || normalized.Any(ch => !Uri.IsHexDigit(ch)))
            throw Conflict(code, "Required signed-artifact hash evidence is not a SHA-256 hexadecimal value.", "provider_contract_violation");
        return normalized.ToLowerInvariant();
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static ApplicationProblemException Validation(string code, string message) =>
        new(ApplicationProblemKind.Validation, code, message);

    private static ApplicationProblemException Conflict(string code, string message, string conflictType) =>
        new(
            ApplicationProblemKind.Conflict,
            code,
            message,
            conflictType: conflictType);
}
