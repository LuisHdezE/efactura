using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Persistence;

namespace EFactura.Application.Fiscal;

public sealed record FiscalCfeEnvelopeDocumentResponseCertificateTrustEvidence(
    bool IsTrusted,
    string ValidationProfileId,
    string CertificateSha256,
    string TrustedRootSha256,
    IReadOnlyList<string> ChainCertificateSha256,
    bool ChainBuilt,
    bool RevocationChecked,
    string RevocationMode,
    bool DgiIdentityValidated,
    string? FailureCode);

/// <summary>
/// Validates PKI Uruguay trust for the X.509 certificate embedded in an already verified ACKCFE.
/// This port deliberately does not assert DGI legal identity or signer habilitation.
/// </summary>
public interface IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidator
{
    FiscalCfeEnvelopeDocumentResponseCertificateTrustEvidence Validate(
        string responseXml,
        string expectedCertificateSha256,
        DateTimeOffset validationTimeUtc);
}

public sealed record StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation(
    Guid Id,
    Guid SignatureVerificationId,
    Guid ConsultationId,
    Guid AckObservationId,
    Guid SubmissionId,
    Guid EnvelopeId,
    string OrganizationId,
    string OperationId,
    string ResponseSha256,
    string ValidationProfileId,
    string CertificateSha256,
    string TrustedRootSha256,
    string ChainCertificateSha256Json,
    string RevocationMode,
    bool PkiUruguayTrustValidated,
    bool DgiIdentityValidated,
    DateTimeOffset ValidatedAtUtc);

public interface IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationRepository
{
    Task<StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation?> GetByOperationAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation validation,
        CancellationToken cancellationToken = default);
}

public sealed record ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustCommand(
    string OrganizationId,
    string ConsultationOperationId,
    string OperationId);

public sealed record FiscalCfeEnvelopeDocumentResponseCertificateTrustValidationResult(
    Guid ValidationId,
    Guid SignatureVerificationId,
    Guid ConsultationId,
    Guid AckObservationId,
    Guid SubmissionId,
    Guid EnvelopeId,
    string OrganizationId,
    string OperationId,
    string ResponseSha256,
    string ValidationProfileId,
    string CertificateSha256,
    string TrustedRootSha256,
    IReadOnlyList<string> ChainCertificateSha256,
    string RevocationMode,
    bool PkiUruguayTrustValidated,
    bool DgiIdentityValidated,
    DateTimeOffset ValidatedAtUtc,
    bool Replayed);

public sealed class ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustUseCase
{
    private readonly IFiscalCfeEnvelopeDocumentResponseConsultationRepository _consultations;
    private readonly IFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository _signatureVerifications;
    private readonly IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationRepository _trustValidations;
    private readonly IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidator _validator;
    private readonly IFiscalCfeEnvelopePersistenceConflictClassifier _conflicts;
    private readonly IFiscalCfeEnvelopeTransportClock _clock;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;

    public ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustUseCase(
        IFiscalCfeEnvelopeDocumentResponseConsultationRepository consultations,
        IFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository signatureVerifications,
        IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationRepository trustValidations,
        IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidator validator,
        IFiscalCfeEnvelopePersistenceConflictClassifier conflicts,
        IFiscalCfeEnvelopeTransportClock clock,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork)
    {
        _consultations = consultations;
        _signatureVerifications = signatureVerifications;
        _trustValidations = trustValidations;
        _validator = validator;
        _conflicts = conflicts;
        _clock = clock;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
    }

    public async Task<FiscalCfeEnvelopeDocumentResponseCertificateTrustValidationResult> ExecuteAsync(
        ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.OrganizationId) || command.OrganizationId.Trim().Length > 200)
        {
            throw Conflict(
                "fiscal.envelope.document_response.trust.organization_invalid",
                "Organization id is required and must contain at most 200 characters.",
                "invalid_request");
        }
        if (string.IsNullOrWhiteSpace(command.ConsultationOperationId)
            || command.ConsultationOperationId.Trim().Length > 120)
        {
            throw Conflict(
                "fiscal.envelope.document_response.trust.consultation_operation_invalid",
                "Consultation operation id is required and must contain at most 120 characters.",
                "invalid_request");
        }
        if (string.IsNullOrWhiteSpace(command.OperationId) || command.OperationId.Trim().Length > 120)
        {
            throw Conflict(
                "fiscal.envelope.document_response.trust.operation_id_invalid",
                "Operation id is required and must contain at most 120 characters.",
                "invalid_request");
        }

        var normalized = command with
        {
            OrganizationId = command.OrganizationId.Trim(),
            ConsultationOperationId = command.ConsultationOperationId.Trim(),
            OperationId = command.OperationId.Trim()
        };

        try
        {
            return await _transactions.ExecuteAsync(async ct =>
            {
                var source = await RequiredSourceAsync(normalized, ct);
                var existing = await _trustValidations.GetByOperationAsync(
                    normalized.OrganizationId,
                    normalized.OperationId,
                    ct);
                if (existing is not null)
                {
                    EnsureValidationIntegrity(existing);
                    EnsureSameSource(existing, source);
                    return Result(existing, replayed: true);
                }

                var validationTime = WholeSecond(_clock.UtcNow);
                var evidence = _validator.Validate(
                    source.Consultation.ResponseXml,
                    source.SignatureVerification.CertificateSha256,
                    validationTime);
                if (!evidence.IsTrusted)
                {
                    throw Conflict(
                        evidence.FailureCode ?? "fiscal.envelope.document_response.trust.validation_failed",
                        "The ACKCFE certificate did not satisfy governed PKI Uruguay trust validation.",
                        "untrusted_external_certificate");
                }

                EnsureValidatorEvidence(evidence, source.SignatureVerification.CertificateSha256);
                var validation = new StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation(
                    Guid.NewGuid(),
                    source.SignatureVerification.Id,
                    source.Consultation.Id,
                    source.Consultation.AckObservationId,
                    source.Consultation.SubmissionId,
                    source.Consultation.EnvelopeId,
                    source.Consultation.OrganizationId,
                    normalized.OperationId,
                    source.Consultation.ResponseSha256,
                    evidence.ValidationProfileId,
                    evidence.CertificateSha256,
                    evidence.TrustedRootSha256,
                    JsonSerializer.Serialize(evidence.ChainCertificateSha256),
                    evidence.RevocationMode,
                    PkiUruguayTrustValidated: true,
                    DgiIdentityValidated: false,
                    validationTime);

                EnsureValidationIntegrity(validation);
                await _trustValidations.AddAsync(validation, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                return Result(validation, replayed: false);
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && _conflicts.IsUniqueConstraintConflict(ex))
        {
            return await RecoverConcurrentValidationAsync(normalized, cancellationToken);
        }
    }

    private async Task<FiscalCfeEnvelopeDocumentResponseCertificateTrustValidationResult> RecoverConcurrentValidationAsync(
        ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustCommand command,
        CancellationToken cancellationToken)
    {
        var source = await RequiredSourceAsync(command, cancellationToken);
        var existing = await _trustValidations.GetByOperationAsync(
            command.OrganizationId,
            command.OperationId,
            cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.document_response.trust.concurrent_validation_unresolved",
                "A concurrent ACKCFE certificate-trust conflict could not be reconciled to durable evidence.",
                "concurrent_uniqueness_conflict");
        EnsureValidationIntegrity(existing);
        EnsureSameSource(existing, source);
        return Result(existing, replayed: true);
    }

    private async Task<SourceEvidence> RequiredSourceAsync(
        ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustCommand command,
        CancellationToken cancellationToken)
    {
        var consultation = await _consultations.GetByOperationAsync(
            command.OrganizationId,
            command.ConsultationOperationId,
            cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.document_response.trust.consultation_required",
                "A durable ACKCFE consultation is required before certificate-trust validation.",
                "missing_prerequisite");
        ConsultFiscalCfeEnvelopeDocumentResponseUseCase.EnsureStoredIntegrity(consultation);
        if (!string.Equals(consultation.OrganizationId, command.OrganizationId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(consultation.ResponseXml)
            || !Sha256Value(consultation.ResponseSha256)
            || !string.Equals(consultation.ResponseSha256, Sha256(consultation.ResponseXml), StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.envelope.document_response.trust.consultation_invalid",
                "The durable ACKCFE consultation no longer matches its exact response evidence.",
                "invalid_persisted_evidence");
        }

        var signatureVerification = await _signatureVerifications.GetByConsultationIdAsync(
            consultation.Id,
            cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.document_response.trust.signature_verification_required",
                "Successful durable ACKCFE XMLDSig verification is required before certificate-trust validation.",
                "missing_prerequisite");
        VerifyFiscalCfeEnvelopeDocumentResponseSignatureUseCase.EnsureVerificationIntegrity(signatureVerification);

        if (signatureVerification.ConsultationId != consultation.Id
            || signatureVerification.AckObservationId != consultation.AckObservationId
            || signatureVerification.SubmissionId != consultation.SubmissionId
            || signatureVerification.EnvelopeId != consultation.EnvelopeId
            || !string.Equals(signatureVerification.OrganizationId, consultation.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(signatureVerification.ResponseSha256, consultation.ResponseSha256, StringComparison.Ordinal)
            || !Sha256Value(signatureVerification.CertificateSha256))
        {
            throw Conflict(
                "fiscal.envelope.document_response.trust.source_mismatch",
                "ACKCFE signature verification no longer matches the durable trust-validation source.",
                "invalid_persisted_evidence");
        }

        return new(consultation, signatureVerification);
    }

    private static void EnsureValidatorEvidence(
        FiscalCfeEnvelopeDocumentResponseCertificateTrustEvidence evidence,
        string expectedCertificateSha256)
    {
        if (string.IsNullOrWhiteSpace(evidence.ValidationProfileId)
            || !Sha256Value(evidence.CertificateSha256)
            || !string.Equals(evidence.CertificateSha256, expectedCertificateSha256, StringComparison.OrdinalIgnoreCase)
            || !Sha256Value(evidence.TrustedRootSha256)
            || evidence.ChainCertificateSha256 is null
            || evidence.ChainCertificateSha256.Count < 2
            || evidence.ChainCertificateSha256.Any(value => !Sha256Value(value))
            || !evidence.ChainBuilt
            || !evidence.RevocationChecked
            || !string.Equals(evidence.RevocationMode, "Online", StringComparison.Ordinal)
            || evidence.DgiIdentityValidated)
        {
            throw Conflict(
                "fiscal.envelope.document_response.trust.validator_evidence_invalid",
                "ACKCFE certificate-trust validator returned incomplete or overclaimed evidence.",
                "invalid_external_certificate");
        }
    }

    internal static void EnsureValidationIntegrity(
        StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation validation)
    {
        IReadOnlyList<string>? chain;
        try
        {
            chain = JsonSerializer.Deserialize<List<string>>(validation.ChainCertificateSha256Json);
        }
        catch (JsonException)
        {
            chain = null;
        }

        if (validation.Id == Guid.Empty
            || validation.SignatureVerificationId == Guid.Empty
            || validation.ConsultationId == Guid.Empty
            || validation.AckObservationId == Guid.Empty
            || validation.SubmissionId == Guid.Empty
            || validation.EnvelopeId == Guid.Empty
            || string.IsNullOrWhiteSpace(validation.OrganizationId)
            || string.IsNullOrWhiteSpace(validation.OperationId)
            || validation.OperationId.Length > 120
            || !Sha256Value(validation.ResponseSha256)
            || string.IsNullOrWhiteSpace(validation.ValidationProfileId)
            || !Sha256Value(validation.CertificateSha256)
            || !Sha256Value(validation.TrustedRootSha256)
            || chain is null
            || chain.Count < 2
            || chain.Any(value => !Sha256Value(value))
            || !string.Equals(validation.RevocationMode, "Online", StringComparison.Ordinal)
            || !validation.PkiUruguayTrustValidated
            || validation.DgiIdentityValidated
            || validation.ValidatedAtUtc == default)
        {
            throw Conflict(
                "fiscal.envelope.document_response.trust.persisted_evidence_invalid",
                "Persisted ACKCFE certificate-trust evidence is incomplete or internally inconsistent.",
                "invalid_persisted_evidence");
        }
    }

    private static void EnsureSameSource(
        StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation validation,
        SourceEvidence source)
    {
        if (validation.SignatureVerificationId != source.SignatureVerification.Id
            || validation.ConsultationId != source.Consultation.Id
            || validation.AckObservationId != source.Consultation.AckObservationId
            || validation.SubmissionId != source.Consultation.SubmissionId
            || validation.EnvelopeId != source.Consultation.EnvelopeId
            || !string.Equals(validation.OrganizationId, source.Consultation.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(validation.ResponseSha256, source.Consultation.ResponseSha256, StringComparison.Ordinal)
            || !string.Equals(validation.CertificateSha256, source.SignatureVerification.CertificateSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw Conflict(
                "fiscal.envelope.document_response.trust.persisted_source_mismatch",
                "Persisted ACKCFE certificate-trust validation no longer matches its durable source evidence.",
                "invalid_persisted_evidence");
        }
    }

    private static FiscalCfeEnvelopeDocumentResponseCertificateTrustValidationResult Result(
        StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation value,
        bool replayed)
    {
        var chain = JsonSerializer.Deserialize<List<string>>(value.ChainCertificateSha256Json)
            ?? throw Conflict(
                "fiscal.envelope.document_response.trust.persisted_evidence_invalid",
                "Persisted ACKCFE certificate-chain evidence is invalid.",
                "invalid_persisted_evidence");

        return new(
            value.Id,
            value.SignatureVerificationId,
            value.ConsultationId,
            value.AckObservationId,
            value.SubmissionId,
            value.EnvelopeId,
            value.OrganizationId,
            value.OperationId,
            value.ResponseSha256,
            value.ValidationProfileId,
            value.CertificateSha256,
            value.TrustedRootSha256,
            chain,
            value.RevocationMode,
            value.PkiUruguayTrustValidated,
            value.DgiIdentityValidated,
            value.ValidatedAtUtc,
            replayed);
    }

    private static DateTimeOffset WholeSecond(DateTimeOffset value) =>
        new(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, TimeSpan.Zero);

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static bool Sha256Value(string? value) =>
        value is not null && value.Length == 64 && value.All(Uri.IsHexDigit);

    private static ApplicationProblemException Conflict(string code, string message, string conflictType) =>
        new(ApplicationProblemKind.Conflict, code, message, conflictType: conflictType);

    private sealed record SourceEvidence(
        StoredFiscalCfeEnvelopeDocumentResponseConsultation Consultation,
        StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification SignatureVerification);
}
