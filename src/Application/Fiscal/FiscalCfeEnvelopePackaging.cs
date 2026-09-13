using System.Security.Cryptography;
using System.Text;
using EFactura.Application.Common.Errors;

namespace EFactura.Application.Fiscal;

public sealed record PackageFiscalCfeEnvelopeCommand(
    string OrganizationId,
    string ReceiverRut,
    string IssuerRuc,
    long SenderEnvelopeId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<Guid> FiscalDocumentIds);

public sealed record FiscalCfeEnvelopeSource(
    Guid FiscalDocumentId,
    string SignedContentHash,
    string CertificateThumbprint,
    string CertificateSerialNumber,
    string SignedXml);

public sealed record FiscalCfeEnvelopeBuildRequest(
    string ReceiverRut,
    string IssuerRuc,
    long SenderEnvelopeId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<FiscalCfeEnvelopeSource> Sources);

public sealed record FiscalCfeEnvelopeBuildArtifact(
    string Xml,
    string CertificateThumbprint,
    string CertificateSerialNumber);

public interface IFiscalCfeEnvelopeBuilder
{
    FiscalCfeEnvelopeBuildArtifact Build(FiscalCfeEnvelopeBuildRequest request);
}

public enum FiscalCfeEnvelopeSchemaValidationStatus
{
    Valid = 1,
    DocumentInvalid = 2,
    SchemaSetInvalid = 3
}

public sealed record FiscalCfeEnvelopeSchemaValidationError(
    string Code,
    string Message,
    int? LineNumber = null,
    int? LinePosition = null);

public sealed record FiscalCfeEnvelopeSchemaValidationResult(
    FiscalCfeEnvelopeSchemaValidationStatus Status,
    string SchemaSetId,
    string SchemaVersion,
    string SchemaSetFingerprint,
    IReadOnlyList<FiscalCfeEnvelopeSchemaValidationError> Errors)
{
    public bool IsValid => Status == FiscalCfeEnvelopeSchemaValidationStatus.Valid;
}

public interface IFiscalCfeEnvelopeSchemaValidator
{
    FiscalCfeEnvelopeSchemaValidationResult Validate(string envelopeXml);
}

public sealed record FiscalCfeEnvelopePackageResult(
    string OrganizationId,
    string ReceiverRut,
    string IssuerRuc,
    long SenderEnvelopeId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<Guid> FiscalDocumentIds,
    int CfeCount,
    string CertificateThumbprint,
    string CertificateSerialNumber,
    string EnvelopeXml,
    string EnvelopeSha256,
    string SchemaSetId,
    string SchemaVersion,
    string SchemaSetFingerprint);

/// <summary>
/// Packages already-signed CFE artifacts into a deterministic DGI EnvioCFE/Sobre candidate.
/// This boundary is deliberately read-only: it does not persist, dispatch, retry, interpret ACKs,
/// allocate sender ids or access private keys. The caller must supply the explicit Sobre identity
/// and creation timestamp. All included CFEs must use the same certificate.
/// </summary>
public sealed class PackageFiscalCfeEnvelopeUseCase
{
    private const int MaxCfePerEnvelope = 250;
    private const long MaxSenderEnvelopeId = 9_999_999_999L;

    private readonly IFiscalSignedArtifactRepository _signedArtifacts;
    private readonly IFiscalSignedCfeSchemaValidator _signedCfeValidator;
    private readonly IFiscalCfeEnvelopeBuilder _builder;
    private readonly IFiscalCfeEnvelopeSchemaValidator _envelopeValidator;

    public PackageFiscalCfeEnvelopeUseCase(
        IFiscalSignedArtifactRepository signedArtifacts,
        IFiscalSignedCfeSchemaValidator signedCfeValidator,
        IFiscalCfeEnvelopeBuilder builder,
        IFiscalCfeEnvelopeSchemaValidator envelopeValidator)
    {
        _signedArtifacts = signedArtifacts;
        _signedCfeValidator = signedCfeValidator;
        _builder = builder;
        _envelopeValidator = envelopeValidator;
    }

    public async Task<FiscalCfeEnvelopePackageResult> ExecuteAsync(
        PackageFiscalCfeEnvelopeCommand command,
        CancellationToken cancellationToken = default)
    {
        Validate(command);

        var organizationId = command.OrganizationId.Trim();
        var receiverRut = command.ReceiverRut.Trim();
        var issuerRuc = command.IssuerRuc.Trim();
        var ids = command.FiscalDocumentIds.ToArray();
        if (ids.Distinct().Count() != ids.Length)
            throw Validation(
                "fiscal.envelope.duplicate_document",
                "A CFE may appear only once in the same Sobre.");

        var sources = new List<FiscalCfeEnvelopeSource>(ids.Length);
        string? commonThumbprint = null;
        string? commonSerial = null;

        foreach (var fiscalDocumentId in ids)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var artifact = await _signedArtifacts.GetByFiscalDocumentAsync(
                organizationId,
                fiscalDocumentId,
                cancellationToken)
                ?? throw Conflict(
                    "fiscal.envelope.signed_artifact_required",
                    "Sobre packaging requires a durable signed CFE artifact for every requested fiscal document.",
                    "missing_prerequisite");

            EnsureArtifactIntegrity(artifact, organizationId, fiscalDocumentId);
            EnsureSignedCfeSchemaEvidence(artifact);

            if (commonThumbprint is null)
            {
                commonThumbprint = artifact.CertificateThumbprint;
                commonSerial = artifact.CertificateSerialNumber;
            }
            else if (!string.Equals(commonThumbprint, artifact.CertificateThumbprint, StringComparison.Ordinal)
                || !string.Equals(commonSerial, artifact.CertificateSerialNumber, StringComparison.Ordinal))
            {
                throw Conflict(
                    "fiscal.envelope.certificate_mismatch",
                    "All CFEs included in one DGI Sobre must have been signed with the same certificate.",
                    "inconsistent_fiscal_evidence");
            }

            sources.Add(new FiscalCfeEnvelopeSource(
                artifact.FiscalDocumentId,
                artifact.SignedContentHash,
                artifact.CertificateThumbprint,
                artifact.CertificateSerialNumber,
                artifact.SignedXml));
        }

        FiscalCfeEnvelopeBuildArtifact built;
        try
        {
            built = _builder.Build(new FiscalCfeEnvelopeBuildRequest(
                receiverRut,
                issuerRuc,
                command.SenderEnvelopeId,
                command.CreatedAt,
                sources));
        }
        catch (ApplicationProblemException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw Conflict(
                "fiscal.envelope.builder_failed",
                $"Sobre packaging failed: {ex.Message}",
                "provider_contract_violation");
        }

        ArgumentNullException.ThrowIfNull(built);
        if (string.IsNullOrWhiteSpace(built.Xml))
            throw Conflict(
                "fiscal.envelope.xml_required",
                "Sobre builder returned empty XML.",
                "provider_contract_violation");

        if (!string.Equals(commonThumbprint, built.CertificateThumbprint, StringComparison.Ordinal)
            || !string.Equals(commonSerial, built.CertificateSerialNumber, StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.envelope.builder_certificate_mismatch",
                "Sobre builder certificate evidence does not match the signed CFE artifacts.",
                "provider_contract_violation");
        }

        var validation = _envelopeValidator.Validate(built.Xml);
        ArgumentNullException.ThrowIfNull(validation);
        if (validation.Status == FiscalCfeEnvelopeSchemaValidationStatus.SchemaSetInvalid)
            throw Conflict(
                "fiscal.envelope.schema_set_invalid",
                ValidationMessage("Pinned DGI Sobre schema set cannot be used safely.", validation),
                "schema_set_invalid");
        if (!validation.IsValid)
            throw Conflict(
                "fiscal.envelope.xsd_invalid",
                ValidationMessage("Generated Sobre failed DGI XSD validation.", validation),
                "schema_validation_failed");

        return new FiscalCfeEnvelopePackageResult(
            organizationId,
            receiverRut,
            issuerRuc,
            command.SenderEnvelopeId,
            command.CreatedAt,
            ids,
            ids.Length,
            commonThumbprint!,
            commonSerial!,
            built.Xml,
            Sha256(built.Xml),
            Required(validation.SchemaSetId, "fiscal.envelope.schema_set_id_required"),
            Required(validation.SchemaVersion, "fiscal.envelope.schema_version_required"),
            RequiredHash(validation.SchemaSetFingerprint, "fiscal.envelope.schema_fingerprint_required"));
    }

    private void EnsureSignedCfeSchemaEvidence(StoredFiscalSignedArtifact artifact)
    {
        var validation = _signedCfeValidator.Validate(artifact.SignedXml);
        ArgumentNullException.ThrowIfNull(validation);
        if (!validation.IsValid)
            throw Conflict(
                "fiscal.envelope.source_cfe_xsd_invalid",
                "A signed CFE selected for Sobre packaging no longer validates against the pinned DGI CFE schema set.",
                "invalid_persisted_evidence");

        if (!string.Equals(artifact.SchemaSetId, validation.SchemaSetId, StringComparison.Ordinal)
            || !string.Equals(artifact.SchemaVersion, validation.SchemaVersion, StringComparison.Ordinal)
            || !string.Equals(artifact.SchemaSetFingerprint, validation.SchemaSetFingerprint, StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.envelope.source_schema_evidence_mismatch",
                "Persisted signed CFE schema evidence no longer matches the pinned validator baseline.",
                "invalid_persisted_evidence");
        }
    }

    private static void EnsureArtifactIntegrity(
        StoredFiscalSignedArtifact artifact,
        string organizationId,
        Guid fiscalDocumentId)
    {
        if (artifact.Id == Guid.Empty
            || artifact.FiscalDocumentId != fiscalDocumentId
            || !string.Equals(artifact.OrganizationId, organizationId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(artifact.SignedXml)
            || string.IsNullOrWhiteSpace(artifact.CertificateThumbprint)
            || string.IsNullOrWhiteSpace(artifact.CertificateSerialNumber))
        {
            throw Conflict(
                "fiscal.envelope.source_artifact_invalid",
                "Persisted signed CFE evidence is incomplete or belongs to another fiscal identity.",
                "invalid_persisted_evidence");
        }

        if (!string.Equals(artifact.SignedContentHash, Sha256(artifact.SignedXml), StringComparison.Ordinal))
            throw Conflict(
                "fiscal.envelope.source_hash_mismatch",
                "Persisted signed CFE XML no longer matches its durable content hash.",
                "invalid_persisted_evidence");
    }

    private static void Validate(PackageFiscalCfeEnvelopeCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.OrganizationId) || command.OrganizationId.Trim().Length > 200)
            throw Validation("fiscal.envelope.organization_invalid", "Organization id is required and must not exceed 200 characters.");
        if (string.IsNullOrWhiteSpace(command.ReceiverRut) || command.ReceiverRut.Trim().Length > 32)
            throw Validation("fiscal.envelope.receiver_rut_invalid", "Receiver RUT is required.");
        if (string.IsNullOrWhiteSpace(command.IssuerRuc) || command.IssuerRuc.Trim().Length > 32)
            throw Validation("fiscal.envelope.issuer_ruc_invalid", "Issuer RUC is required.");
        if (command.SenderEnvelopeId < 0 || command.SenderEnvelopeId > MaxSenderEnvelopeId)
            throw Validation("fiscal.envelope.sender_id_invalid", "DGI Idemisor must be between 0 and 9999999999.");
        if (command.CreatedAt == default)
            throw Validation("fiscal.envelope.created_at_required", "Sobre creation timestamp is required.");
        if (command.FiscalDocumentIds is null
            || command.FiscalDocumentIds.Count < 1
            || command.FiscalDocumentIds.Count > MaxCfePerEnvelope)
        {
            throw Validation("fiscal.envelope.cfe_count_invalid", "A DGI Sobre must contain between 1 and 250 CFEs.");
        }
        if (command.FiscalDocumentIds.Any(id => id == Guid.Empty))
            throw Validation("fiscal.envelope.document_id_invalid", "Every fiscal document id must be non-empty.");
    }

    private static string ValidationMessage(
        string prefix,
        FiscalCfeEnvelopeSchemaValidationResult validation)
    {
        var detail = validation.Errors.FirstOrDefault();
        return detail is null
            ? $"{prefix} Schema={validation.SchemaSetId} v{validation.SchemaVersion}."
            : $"{prefix} Schema={validation.SchemaSetId} v{validation.SchemaVersion}; {detail.Code}: {detail.Message}";
    }

    private static string Required(string value, string code) =>
        string.IsNullOrWhiteSpace(value)
            ? throw Conflict(code, "Required Sobre schema evidence is missing.", "provider_contract_violation")
            : value.Trim();

    private static string RequiredHash(string value, string code)
    {
        var normalized = Required(value, code).ToLowerInvariant();
        if (normalized.Length != 64 || normalized.Any(ch => !Uri.IsHexDigit(ch)))
            throw Conflict(code, "Required Sobre schema fingerprint is not a SHA-256 value.", "provider_contract_violation");
        return normalized;
    }

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static ApplicationProblemException Validation(string code, string message) =>
        new(ApplicationProblemKind.Validation, code, message);

    private static ApplicationProblemException Conflict(string code, string message, string conflictType) =>
        new(ApplicationProblemKind.Conflict, code, message, conflictType: conflictType);
}
