using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Persistence;

namespace EFactura.Application.Fiscal;

public sealed record FiscalCfeEnvelopeAckCertificateTrustEvidence(
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
/// Validates PKI Uruguay trust for the X.509 certificate embedded in an already verified ACKSobre.
/// This port deliberately does not assert DGI legal identity of the end-entity certificate.
/// </summary>
public interface IFiscalCfeEnvelopeAckCertificateTrustValidator
{
    FiscalCfeEnvelopeAckCertificateTrustEvidence Validate(
        string responseXml,
        string expectedCertificateSha256,
        DateTimeOffset validationTimeUtc);
}

public sealed record StoredFiscalCfeEnvelopeAckCertificateTrustValidation(
    Guid Id,
    Guid SignatureVerificationId,
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

public interface IFiscalCfeEnvelopeAckCertificateTrustValidationRepository
{
    Task<StoredFiscalCfeEnvelopeAckCertificateTrustValidation?> GetByOperationAsync(
        string organizationId,
        string operationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        StoredFiscalCfeEnvelopeAckCertificateTrustValidation validation,
        CancellationToken cancellationToken = default);
}

public sealed record ValidateFiscalCfeEnvelopeAckCertificateTrustCommand(
    string OrganizationId,
    string IssuerRuc,
    string ReceiverRut,
    long SenderEnvelopeId,
    string OperationId);

public sealed record FiscalCfeEnvelopeAckCertificateTrustValidationResult(
    Guid ValidationId,
    Guid SignatureVerificationId,
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

public sealed class ValidateFiscalCfeEnvelopeAckCertificateTrustUseCase
{
    private readonly IFiscalCfeEnvelopeRepository _envelopes;
    private readonly IFiscalCfeEnvelopeSubmissionRepository _submissions;
    private readonly IFiscalCfeEnvelopeAckObservationRepository _ackObservations;
    private readonly IFiscalCfeEnvelopeAckSignatureVerificationRepository _signatureVerifications;
    private readonly IFiscalCfeEnvelopeAckCertificateTrustValidationRepository _trustValidations;
    private readonly IFiscalCfeEnvelopeAckCertificateTrustValidator _validator;
    private readonly IFiscalCfeEnvelopePersistenceConflictClassifier _conflicts;
    private readonly IFiscalCfeEnvelopeTransportClock _clock;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;

    public ValidateFiscalCfeEnvelopeAckCertificateTrustUseCase(
        IFiscalCfeEnvelopeRepository envelopes,
        IFiscalCfeEnvelopeSubmissionRepository submissions,
        IFiscalCfeEnvelopeAckObservationRepository ackObservations,
        IFiscalCfeEnvelopeAckSignatureVerificationRepository signatureVerifications,
        IFiscalCfeEnvelopeAckCertificateTrustValidationRepository trustValidations,
        IFiscalCfeEnvelopeAckCertificateTrustValidator validator,
        IFiscalCfeEnvelopePersistenceConflictClassifier conflicts,
        IFiscalCfeEnvelopeTransportClock clock,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork)
    {
        _envelopes = envelopes;
        _submissions = submissions;
        _ackObservations = ackObservations;
        _signatureVerifications = signatureVerifications;
        _trustValidations = trustValidations;
        _validator = validator;
        _conflicts = conflicts;
        _clock = clock;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
    }

    public async Task<FiscalCfeEnvelopeAckCertificateTrustValidationResult> ExecuteAsync(
        ValidateFiscalCfeEnvelopeAckCertificateTrustCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        PrepareFiscalCfeEnvelopeSubmissionUseCase.Validate(
            command.OrganizationId,
            command.IssuerRuc,
            command.ReceiverRut,
            command.SenderEnvelopeId);
        if (string.IsNullOrWhiteSpace(command.OperationId) || command.OperationId.Trim().Length > 120)
        {
            throw Conflict(
                "fiscal.envelope.ack.trust.operation_id_invalid",
                "Operation id is required and must contain at most 120 characters.",
                "invalid_request");
        }

        var normalized = command with
        {
            OrganizationId = command.OrganizationId.Trim(),
            IssuerRuc = command.IssuerRuc.Trim(),
            ReceiverRut = command.ReceiverRut.Trim(),
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
                    source.Submission.ResponseXml!,
                    source.SignatureVerification.CertificateSha256,
                    validationTime);
                if (!evidence.IsTrusted)
                {
                    throw Conflict(
                        evidence.FailureCode ?? "fiscal.envelope.ack.trust.validation_failed",
                        "The ACKSobre certificate did not satisfy governed PKI Uruguay trust validation.",
                        "untrusted_external_certificate");
                }

                EnsureValidatorEvidence(evidence, source.SignatureVerification.CertificateSha256);
                var validation = new StoredFiscalCfeEnvelopeAckCertificateTrustValidation(
                    Guid.NewGuid(),
                    source.SignatureVerification.Id,
                    source.Observation.Id,
                    source.Submission.Id,
                    source.Envelope.Id,
                    source.Envelope.OrganizationId,
                    normalized.OperationId,
                    source.Submission.ResponseSha256!,
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

    private async Task<FiscalCfeEnvelopeAckCertificateTrustValidationResult> RecoverConcurrentValidationAsync(
        ValidateFiscalCfeEnvelopeAckCertificateTrustCommand command,
        CancellationToken cancellationToken)
    {
        var source = await RequiredSourceAsync(command, cancellationToken);
        var existing = await _trustValidations.GetByOperationAsync(
            command.OrganizationId,
            command.OperationId,
            cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.ack.trust.concurrent_validation_unresolved",
                "A concurrent certificate-trust validation conflict could not be reconciled to durable evidence.",
                "concurrent_uniqueness_conflict");
        EnsureValidationIntegrity(existing);
        EnsureSameSource(existing, source);
        return Result(existing, replayed: true);
    }

    private async Task<SourceEvidence> RequiredSourceAsync(
        ValidateFiscalCfeEnvelopeAckCertificateTrustCommand command,
        CancellationToken cancellationToken)
    {
        var envelope = await _envelopes.GetByIdentityAsync(
            command.OrganizationId,
            command.IssuerRuc,
            command.ReceiverRut,
            command.SenderEnvelopeId,
            cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.ack.trust.envelope_required",
                "A durable Sobre is required before ACKSobre certificate-trust validation.",
                "missing_prerequisite");
        PrepareFiscalCfeEnvelopeSubmissionUseCase.EnsureEnvelopeIntegrity(envelope);

        var submission = await _submissions.GetByEnvelopeIdAsync(envelope.Id, cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.ack.trust.submission_required",
                "A durable Sobre transport submission is required before ACKSobre certificate-trust validation.",
                "missing_prerequisite");
        PrepareFiscalCfeEnvelopeSubmissionUseCase.EnsureSubmissionIntegrity(submission);
        if (submission.State != FiscalCfeEnvelopeSubmissionState.ResponseReceived
            || string.IsNullOrWhiteSpace(submission.ResponseXml)
            || !Sha256Value(submission.ResponseSha256)
            || !string.Equals(submission.ResponseSha256, Sha256(submission.ResponseXml), StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.envelope.ack.trust.response_required",
                "ACKSobre certificate-trust validation requires exact durable ResponseReceived XML.",
                "missing_prerequisite");
        }

        var observation = await _ackObservations.GetBySubmissionIdAsync(submission.Id, cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.ack.trust.observation_required",
                "A durable ACKSobre observation is required before certificate-trust validation.",
                "missing_prerequisite");
        ObserveFiscalCfeEnvelopeAckUseCase.EnsureObservationIntegrity(observation);

        var signatureVerification = await _signatureVerifications.GetByAckObservationIdAsync(
            observation.Id,
            cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.ack.trust.signature_verification_required",
                "Successful durable ACKSobre XMLDSig verification is required before certificate-trust validation.",
                "missing_prerequisite");
        VerifyFiscalCfeEnvelopeAckSignatureUseCase.EnsureVerificationIntegrity(signatureVerification);

        if (signatureVerification.AckObservationId != observation.Id
            || signatureVerification.SubmissionId != submission.Id
            || signatureVerification.EnvelopeId != envelope.Id
            || !string.Equals(signatureVerification.OrganizationId, envelope.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(signatureVerification.ResponseSha256, submission.ResponseSha256, StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.envelope.ack.trust.source_mismatch",
                "ACKSobre signature verification no longer matches the durable trust-validation source.",
                "invalid_persisted_evidence");
        }

        return new(envelope, submission, observation, signatureVerification);
    }

    private static void EnsureValidatorEvidence(
        FiscalCfeEnvelopeAckCertificateTrustEvidence evidence,
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
                "fiscal.envelope.ack.trust.validator_evidence_invalid",
                "Certificate-trust validator returned incomplete or overclaimed evidence.",
                "invalid_external_certificate");
        }
    }

    internal static void EnsureValidationIntegrity(StoredFiscalCfeEnvelopeAckCertificateTrustValidation validation)
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
                "fiscal.envelope.ack.trust.persisted_evidence_invalid",
                "Persisted ACKSobre certificate-trust evidence is incomplete or internally inconsistent.",
                "invalid_persisted_evidence");
        }
    }

    private static void EnsureSameSource(
        StoredFiscalCfeEnvelopeAckCertificateTrustValidation validation,
        SourceEvidence source)
    {
        if (validation.SignatureVerificationId != source.SignatureVerification.Id
            || validation.AckObservationId != source.Observation.Id
            || validation.SubmissionId != source.Submission.Id
            || validation.EnvelopeId != source.Envelope.Id
            || !string.Equals(validation.OrganizationId, source.Envelope.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(validation.ResponseSha256, source.Submission.ResponseSha256, StringComparison.Ordinal)
            || !string.Equals(validation.CertificateSha256, source.SignatureVerification.CertificateSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw Conflict(
                "fiscal.envelope.ack.trust.persisted_source_mismatch",
                "Persisted ACKSobre certificate-trust validation no longer matches its durable source evidence.",
                "invalid_persisted_evidence");
        }
    }

    private static FiscalCfeEnvelopeAckCertificateTrustValidationResult Result(
        StoredFiscalCfeEnvelopeAckCertificateTrustValidation value,
        bool replayed)
    {
        var chain = JsonSerializer.Deserialize<List<string>>(value.ChainCertificateSha256Json)
            ?? throw Conflict(
                "fiscal.envelope.ack.trust.persisted_evidence_invalid",
                "Persisted certificate-chain evidence is invalid.",
                "invalid_persisted_evidence");

        return new(
            value.Id,
            value.SignatureVerificationId,
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
        StoredFiscalCfeEnvelope Envelope,
        StoredFiscalCfeEnvelopeSubmission Submission,
        StoredFiscalCfeEnvelopeAckObservation Observation,
        StoredFiscalCfeEnvelopeAckSignatureVerification SignatureVerification);
}
