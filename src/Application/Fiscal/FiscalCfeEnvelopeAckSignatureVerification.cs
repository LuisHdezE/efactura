using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Persistence;

namespace EFactura.Application.Fiscal;

public sealed record FiscalCfeEnvelopeAckSignatureVerificationEvidence(
    bool IsValid,
    string VerificationProfileId,
    string CertificateSha256,
    string CertificateThumbprint,
    string CertificateSerialNumber,
    string CertificateSubject,
    string CertificateIssuer,
    string CanonicalizationMethod,
    string SignatureMethod,
    string DigestMethod,
    string ReferenceUri,
    IReadOnlyList<string> ReferenceTransforms,
    bool CertificateTrustValidated,
    string? FailureCode);

/// <summary>
/// Verifies the cryptographic XMLDSig integrity of an already durable ACKSobre response. Certificate
/// chain/trust validation is deliberately outside this port and must not be inferred from signature math.
/// </summary>
public interface IFiscalCfeEnvelopeAckSignatureVerifier
{
    FiscalCfeEnvelopeAckSignatureVerificationEvidence Verify(string responseXml);
}

public sealed record StoredFiscalCfeEnvelopeAckSignatureVerification(
    Guid Id,
    Guid AckObservationId,
    Guid SubmissionId,
    Guid EnvelopeId,
    string OrganizationId,
    string ResponseSha256,
    string VerificationProfileId,
    string CertificateSha256,
    string CertificateThumbprint,
    string CertificateSerialNumber,
    string CertificateSubject,
    string CertificateIssuer,
    string CanonicalizationMethod,
    string SignatureMethod,
    string DigestMethod,
    string ReferenceUri,
    string ReferenceTransformsJson,
    bool CertificateTrustValidated,
    DateTimeOffset VerifiedAtUtc);

public interface IFiscalCfeEnvelopeAckSignatureVerificationRepository
{
    Task<StoredFiscalCfeEnvelopeAckSignatureVerification?> GetByAckObservationIdAsync(
        Guid ackObservationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        StoredFiscalCfeEnvelopeAckSignatureVerification verification,
        CancellationToken cancellationToken = default);
}

public sealed record VerifyFiscalCfeEnvelopeAckSignatureCommand(
    string OrganizationId,
    string IssuerRuc,
    string ReceiverRut,
    long SenderEnvelopeId);

public sealed record FiscalCfeEnvelopeAckSignatureVerificationResult(
    Guid VerificationId,
    Guid AckObservationId,
    Guid SubmissionId,
    Guid EnvelopeId,
    string OrganizationId,
    string ResponseSha256,
    string VerificationProfileId,
    string CertificateSha256,
    string CertificateThumbprint,
    string CertificateSerialNumber,
    string CertificateSubject,
    string CertificateIssuer,
    string CanonicalizationMethod,
    string SignatureMethod,
    string DigestMethod,
    string ReferenceUri,
    IReadOnlyList<string> ReferenceTransforms,
    bool SignatureValid,
    bool CertificateTrustValidated,
    DateTimeOffset VerifiedAtUtc,
    bool Replayed);

public sealed class VerifyFiscalCfeEnvelopeAckSignatureUseCase
{
    private readonly IFiscalCfeEnvelopeRepository _envelopes;
    private readonly IFiscalCfeEnvelopeSubmissionRepository _submissions;
    private readonly IFiscalCfeEnvelopeAckObservationRepository _ackObservations;
    private readonly IFiscalCfeEnvelopeAckSignatureVerificationRepository _verifications;
    private readonly IFiscalCfeEnvelopeAckSignatureVerifier _verifier;
    private readonly IFiscalCfeEnvelopePersistenceConflictClassifier _conflicts;
    private readonly IFiscalCfeEnvelopeTransportClock _clock;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;

    public VerifyFiscalCfeEnvelopeAckSignatureUseCase(
        IFiscalCfeEnvelopeRepository envelopes,
        IFiscalCfeEnvelopeSubmissionRepository submissions,
        IFiscalCfeEnvelopeAckObservationRepository ackObservations,
        IFiscalCfeEnvelopeAckSignatureVerificationRepository verifications,
        IFiscalCfeEnvelopeAckSignatureVerifier verifier,
        IFiscalCfeEnvelopePersistenceConflictClassifier conflicts,
        IFiscalCfeEnvelopeTransportClock clock,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork)
    {
        _envelopes = envelopes;
        _submissions = submissions;
        _ackObservations = ackObservations;
        _verifications = verifications;
        _verifier = verifier;
        _conflicts = conflicts;
        _clock = clock;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
    }

    public async Task<FiscalCfeEnvelopeAckSignatureVerificationResult> ExecuteAsync(
        VerifyFiscalCfeEnvelopeAckSignatureCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        PrepareFiscalCfeEnvelopeSubmissionUseCase.Validate(
            command.OrganizationId,
            command.IssuerRuc,
            command.ReceiverRut,
            command.SenderEnvelopeId);

        var normalized = command with
        {
            OrganizationId = command.OrganizationId.Trim(),
            IssuerRuc = command.IssuerRuc.Trim(),
            ReceiverRut = command.ReceiverRut.Trim()
        };

        try
        {
            return await _transactions.ExecuteAsync(async ct =>
            {
                var source = await RequiredSourceAsync(normalized, ct);
                var existing = await _verifications.GetByAckObservationIdAsync(source.Observation.Id, ct);
                if (existing is not null)
                {
                    EnsureVerificationIntegrity(existing);
                    EnsureSameSource(existing, source.Observation, source.Submission);
                    return Result(existing, replayed: true);
                }

                var evidence = _verifier.Verify(source.Submission.ResponseXml!);
                if (!evidence.IsValid)
                    throw Conflict(
                        evidence.FailureCode ?? "fiscal.envelope.ack.signature.invalid",
                        "The durable ACKSobre XMLDSig failed governed cryptographic verification.",
                        "invalid_external_signature");

                EnsureVerifierEvidence(evidence);
                var transformsJson = JsonSerializer.Serialize(evidence.ReferenceTransforms);
                var verification = new StoredFiscalCfeEnvelopeAckSignatureVerification(
                    Guid.NewGuid(),
                    source.Observation.Id,
                    source.Submission.Id,
                    source.Envelope.Id,
                    source.Envelope.OrganizationId,
                    source.Submission.ResponseSha256!,
                    evidence.VerificationProfileId,
                    evidence.CertificateSha256,
                    evidence.CertificateThumbprint,
                    evidence.CertificateSerialNumber,
                    evidence.CertificateSubject,
                    evidence.CertificateIssuer,
                    evidence.CanonicalizationMethod,
                    evidence.SignatureMethod,
                    evidence.DigestMethod,
                    evidence.ReferenceUri,
                    transformsJson,
                    CertificateTrustValidated: false,
                    WholeSecond(_clock.UtcNow));

                EnsureVerificationIntegrity(verification);
                await _verifications.AddAsync(verification, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                return Result(verification, replayed: false);
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && _conflicts.IsUniqueConstraintConflict(ex))
        {
            return await RecoverConcurrentVerificationAsync(normalized, cancellationToken);
        }
    }

    private async Task<FiscalCfeEnvelopeAckSignatureVerificationResult> RecoverConcurrentVerificationAsync(
        VerifyFiscalCfeEnvelopeAckSignatureCommand command,
        CancellationToken cancellationToken)
    {
        var source = await RequiredSourceAsync(command, cancellationToken);
        var existing = await _verifications.GetByAckObservationIdAsync(source.Observation.Id, cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.ack.signature.concurrent_verification_unresolved",
                "A concurrent ACKSobre signature-verification conflict could not be reconciled to durable evidence.",
                "concurrent_uniqueness_conflict");
        EnsureVerificationIntegrity(existing);
        EnsureSameSource(existing, source.Observation, source.Submission);
        return Result(existing, replayed: true);
    }

    private async Task<SourceEvidence> RequiredSourceAsync(
        VerifyFiscalCfeEnvelopeAckSignatureCommand command,
        CancellationToken cancellationToken)
    {
        var envelope = await _envelopes.GetByIdentityAsync(
            command.OrganizationId,
            command.IssuerRuc,
            command.ReceiverRut,
            command.SenderEnvelopeId,
            cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.ack.signature.envelope_required",
                "A durable Sobre is required before ACKSobre signature verification.",
                "missing_prerequisite");
        PrepareFiscalCfeEnvelopeSubmissionUseCase.EnsureEnvelopeIntegrity(envelope);

        var submission = await _submissions.GetByEnvelopeIdAsync(envelope.Id, cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.ack.signature.submission_required",
                "A durable Sobre transport submission is required before ACKSobre signature verification.",
                "missing_prerequisite");
        PrepareFiscalCfeEnvelopeSubmissionUseCase.EnsureSubmissionIntegrity(submission);

        if (submission.State != FiscalCfeEnvelopeSubmissionState.ResponseReceived
            || string.IsNullOrWhiteSpace(submission.ResponseXml)
            || !Sha256Value(submission.ResponseSha256)
            || !string.Equals(submission.ResponseSha256, Sha256(submission.ResponseXml), StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.envelope.ack.signature.response_required",
                "ACKSobre signature verification requires exact durable ResponseReceived XML.",
                "missing_prerequisite");
        }

        var observation = await _ackObservations.GetBySubmissionIdAsync(submission.Id, cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.ack.signature.observation_required",
                "A durable ACKSobre semantic observation is required before signature verification is recorded.",
                "missing_prerequisite");
        ObserveFiscalCfeEnvelopeAckUseCase.EnsureObservationIntegrity(observation);

        if (observation.EnvelopeId != envelope.Id
            || observation.SubmissionId != submission.Id
            || !string.Equals(observation.OrganizationId, envelope.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(observation.ResponseSha256, submission.ResponseSha256, StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.envelope.ack.signature.source_mismatch",
                "ACKSobre observation no longer matches the durable transport source.",
                "invalid_persisted_evidence");
        }

        return new(envelope, submission, observation);
    }

    private static void EnsureVerifierEvidence(FiscalCfeEnvelopeAckSignatureVerificationEvidence evidence)
    {
        if (string.IsNullOrWhiteSpace(evidence.VerificationProfileId)
            || !Sha256Value(evidence.CertificateSha256)
            || string.IsNullOrWhiteSpace(evidence.CertificateThumbprint)
            || string.IsNullOrWhiteSpace(evidence.CertificateSerialNumber)
            || string.IsNullOrWhiteSpace(evidence.CertificateSubject)
            || string.IsNullOrWhiteSpace(evidence.CertificateIssuer)
            || string.IsNullOrWhiteSpace(evidence.CanonicalizationMethod)
            || string.IsNullOrWhiteSpace(evidence.SignatureMethod)
            || string.IsNullOrWhiteSpace(evidence.DigestMethod)
            || evidence.ReferenceUri != string.Empty
            || evidence.ReferenceTransforms is null
            || evidence.ReferenceTransforms.Count == 0
            || evidence.CertificateTrustValidated)
        {
            throw Conflict(
                "fiscal.envelope.ack.signature.verifier_evidence_invalid",
                "ACKSobre signature verifier returned incomplete or overclaimed evidence.",
                "invalid_external_signature");
        }
    }

    internal static void EnsureVerificationIntegrity(StoredFiscalCfeEnvelopeAckSignatureVerification verification)
    {
        IReadOnlyList<string>? transforms;
        try
        {
            transforms = JsonSerializer.Deserialize<List<string>>(verification.ReferenceTransformsJson);
        }
        catch (JsonException)
        {
            transforms = null;
        }

        if (verification.Id == Guid.Empty
            || verification.AckObservationId == Guid.Empty
            || verification.SubmissionId == Guid.Empty
            || verification.EnvelopeId == Guid.Empty
            || string.IsNullOrWhiteSpace(verification.OrganizationId)
            || !Sha256Value(verification.ResponseSha256)
            || string.IsNullOrWhiteSpace(verification.VerificationProfileId)
            || !Sha256Value(verification.CertificateSha256)
            || string.IsNullOrWhiteSpace(verification.CertificateThumbprint)
            || string.IsNullOrWhiteSpace(verification.CertificateSerialNumber)
            || string.IsNullOrWhiteSpace(verification.CertificateSubject)
            || string.IsNullOrWhiteSpace(verification.CertificateIssuer)
            || string.IsNullOrWhiteSpace(verification.CanonicalizationMethod)
            || string.IsNullOrWhiteSpace(verification.SignatureMethod)
            || string.IsNullOrWhiteSpace(verification.DigestMethod)
            || verification.ReferenceUri != string.Empty
            || transforms is null
            || transforms.Count == 0
            || verification.CertificateTrustValidated
            || verification.VerifiedAtUtc == default)
        {
            throw Conflict(
                "fiscal.envelope.ack.signature.persisted_evidence_invalid",
                "Persisted ACKSobre signature-verification evidence is incomplete or internally inconsistent.",
                "invalid_persisted_evidence");
        }
    }

    private static void EnsureSameSource(
        StoredFiscalCfeEnvelopeAckSignatureVerification verification,
        StoredFiscalCfeEnvelopeAckObservation observation,
        StoredFiscalCfeEnvelopeSubmission submission)
    {
        if (verification.AckObservationId != observation.Id
            || verification.SubmissionId != submission.Id
            || verification.EnvelopeId != observation.EnvelopeId
            || !string.Equals(verification.OrganizationId, observation.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(verification.ResponseSha256, observation.ResponseSha256, StringComparison.Ordinal)
            || !string.Equals(verification.ResponseSha256, submission.ResponseSha256, StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.envelope.ack.signature.persisted_source_mismatch",
                "Persisted ACKSobre signature verification no longer matches its durable source evidence.",
                "invalid_persisted_evidence");
        }
    }

    private static FiscalCfeEnvelopeAckSignatureVerificationResult Result(
        StoredFiscalCfeEnvelopeAckSignatureVerification value,
        bool replayed)
    {
        var transforms = JsonSerializer.Deserialize<List<string>>(value.ReferenceTransformsJson)
            ?? throw Conflict(
                "fiscal.envelope.ack.signature.persisted_evidence_invalid",
                "Persisted XMLDSig transform evidence is invalid.",
                "invalid_persisted_evidence");

        return new(
            value.Id,
            value.AckObservationId,
            value.SubmissionId,
            value.EnvelopeId,
            value.OrganizationId,
            value.ResponseSha256,
            value.VerificationProfileId,
            value.CertificateSha256,
            value.CertificateThumbprint,
            value.CertificateSerialNumber,
            value.CertificateSubject,
            value.CertificateIssuer,
            value.CanonicalizationMethod,
            value.SignatureMethod,
            value.DigestMethod,
            value.ReferenceUri,
            transforms,
            SignatureValid: true,
            value.CertificateTrustValidated,
            value.VerifiedAtUtc,
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
        StoredFiscalCfeEnvelopeAckObservation Observation);
}
