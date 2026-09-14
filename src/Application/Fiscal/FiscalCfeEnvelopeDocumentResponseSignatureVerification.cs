using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Persistence;

namespace EFactura.Application.Fiscal;

public sealed record FiscalCfeEnvelopeDocumentResponseSignatureVerificationEvidence(
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
/// Verifies XMLDSig signature mathematics for an already durable ACKCFE consultation response.
/// Certificate-chain trust and DGI signer identity are deliberately separate concerns.
/// </summary>
public interface IFiscalCfeEnvelopeDocumentResponseSignatureVerifier
{
    FiscalCfeEnvelopeDocumentResponseSignatureVerificationEvidence Verify(string responseXml);
}

public sealed record StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification(
    Guid Id,
    Guid ConsultationId,
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

public interface IFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository
{
    Task<StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification?> GetByConsultationIdAsync(
        Guid consultationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification verification,
        CancellationToken cancellationToken = default);
}

public sealed record VerifyFiscalCfeEnvelopeDocumentResponseSignatureCommand(
    string OrganizationId,
    string ConsultationOperationId);

public sealed record FiscalCfeEnvelopeDocumentResponseSignatureVerificationResult(
    Guid VerificationId,
    Guid ConsultationId,
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

public sealed class VerifyFiscalCfeEnvelopeDocumentResponseSignatureUseCase
{
    private readonly IFiscalCfeEnvelopeDocumentResponseConsultationRepository _consultations;
    private readonly IFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository _verifications;
    private readonly IFiscalCfeEnvelopeDocumentResponseSignatureVerifier _verifier;
    private readonly IFiscalCfeEnvelopePersistenceConflictClassifier _conflicts;
    private readonly IFiscalCfeEnvelopeTransportClock _clock;
    private readonly ITransactionManager _transactions;
    private readonly IUnitOfWork _unitOfWork;

    public VerifyFiscalCfeEnvelopeDocumentResponseSignatureUseCase(
        IFiscalCfeEnvelopeDocumentResponseConsultationRepository consultations,
        IFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository verifications,
        IFiscalCfeEnvelopeDocumentResponseSignatureVerifier verifier,
        IFiscalCfeEnvelopePersistenceConflictClassifier conflicts,
        IFiscalCfeEnvelopeTransportClock clock,
        ITransactionManager transactions,
        IUnitOfWork unitOfWork)
    {
        _consultations = consultations;
        _verifications = verifications;
        _verifier = verifier;
        _conflicts = conflicts;
        _clock = clock;
        _transactions = transactions;
        _unitOfWork = unitOfWork;
    }

    public async Task<FiscalCfeEnvelopeDocumentResponseSignatureVerificationResult> ExecuteAsync(
        VerifyFiscalCfeEnvelopeDocumentResponseSignatureCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (string.IsNullOrWhiteSpace(command.OrganizationId) || command.OrganizationId.Trim().Length > 200)
        {
            throw Conflict(
                "fiscal.envelope.document_response.signature.organization_invalid",
                "Organization id is required and must contain at most 200 characters.",
                "invalid_request");
        }
        if (string.IsNullOrWhiteSpace(command.ConsultationOperationId)
            || command.ConsultationOperationId.Trim().Length > 120)
        {
            throw Conflict(
                "fiscal.envelope.document_response.signature.consultation_operation_invalid",
                "Consultation operation id is required and must contain at most 120 characters.",
                "invalid_request");
        }

        var organizationId = command.OrganizationId.Trim();
        var consultationOperationId = command.ConsultationOperationId.Trim();
        var source = await RequiredSourceAsync(organizationId, consultationOperationId, cancellationToken);

        var existing = await _verifications.GetByConsultationIdAsync(source.Id, cancellationToken);
        if (existing is not null)
        {
            EnsureVerificationIntegrity(existing);
            EnsureSameSource(existing, source);
            return Result(existing, replayed: true);
        }

        var evidence = _verifier.Verify(source.ResponseXml);
        if (!evidence.IsValid)
        {
            throw Conflict(
                evidence.FailureCode ?? "fiscal.envelope.document_response.signature.invalid",
                "The durable ACKCFE XMLDSig failed governed cryptographic verification.",
                "invalid_external_signature");
        }

        EnsureVerifierEvidence(evidence);
        var verification = new StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification(
            Guid.NewGuid(),
            source.Id,
            source.AckObservationId,
            source.SubmissionId,
            source.EnvelopeId,
            source.OrganizationId,
            source.ResponseSha256,
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
            JsonSerializer.Serialize(evidence.ReferenceTransforms),
            CertificateTrustValidated: false,
            WholeSecond(_clock.UtcNow));
        EnsureVerificationIntegrity(verification);

        try
        {
            return await _transactions.ExecuteAsync(async ct =>
            {
                var concurrentReplay = await _verifications.GetByConsultationIdAsync(source.Id, ct);
                if (concurrentReplay is not null)
                {
                    EnsureVerificationIntegrity(concurrentReplay);
                    EnsureSameSource(concurrentReplay, source);
                    return Result(concurrentReplay, replayed: true);
                }

                await _verifications.AddAsync(verification, ct);
                await _unitOfWork.SaveChangesAsync(ct);
                return Result(verification, replayed: false);
            }, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && _conflicts.IsUniqueConstraintConflict(ex))
        {
            var concurrent = await _verifications.GetByConsultationIdAsync(source.Id, cancellationToken)
                ?? throw Conflict(
                    "fiscal.envelope.document_response.signature.concurrent_verification_unresolved",
                    "A concurrent ACKCFE signature-verification conflict could not be reconciled to durable evidence.",
                    "concurrent_uniqueness_conflict");
            EnsureVerificationIntegrity(concurrent);
            EnsureSameSource(concurrent, source);
            return Result(concurrent, replayed: true);
        }
    }

    private async Task<StoredFiscalCfeEnvelopeDocumentResponseConsultation> RequiredSourceAsync(
        string organizationId,
        string consultationOperationId,
        CancellationToken cancellationToken)
    {
        var source = await _consultations.GetByOperationAsync(
            organizationId,
            consultationOperationId,
            cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.document_response.signature.consultation_required",
                "A durable ACKCFE consultation is required before signature verification.",
                "missing_prerequisite");
        ConsultFiscalCfeEnvelopeDocumentResponseUseCase.EnsureStoredIntegrity(source);

        if (!string.Equals(source.OrganizationId, organizationId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(source.ResponseXml)
            || !Sha256Value(source.ResponseSha256)
            || !string.Equals(source.ResponseSha256, Sha256(source.ResponseXml), StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.envelope.document_response.signature.source_invalid",
                "The durable ACKCFE consultation no longer matches its exact response evidence.",
                "invalid_persisted_evidence");
        }

        return source;
    }

    private static void EnsureVerifierEvidence(FiscalCfeEnvelopeDocumentResponseSignatureVerificationEvidence evidence)
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
                "fiscal.envelope.document_response.signature.verifier_evidence_invalid",
                "ACKCFE signature verifier returned incomplete or overclaimed evidence.",
                "invalid_external_signature");
        }
    }

    internal static void EnsureVerificationIntegrity(StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification value)
    {
        IReadOnlyList<string>? transforms;
        try
        {
            transforms = JsonSerializer.Deserialize<List<string>>(value.ReferenceTransformsJson);
        }
        catch (JsonException)
        {
            transforms = null;
        }

        if (value.Id == Guid.Empty
            || value.ConsultationId == Guid.Empty
            || value.AckObservationId == Guid.Empty
            || value.SubmissionId == Guid.Empty
            || value.EnvelopeId == Guid.Empty
            || string.IsNullOrWhiteSpace(value.OrganizationId)
            || !Sha256Value(value.ResponseSha256)
            || string.IsNullOrWhiteSpace(value.VerificationProfileId)
            || !Sha256Value(value.CertificateSha256)
            || string.IsNullOrWhiteSpace(value.CertificateThumbprint)
            || string.IsNullOrWhiteSpace(value.CertificateSerialNumber)
            || string.IsNullOrWhiteSpace(value.CertificateSubject)
            || string.IsNullOrWhiteSpace(value.CertificateIssuer)
            || string.IsNullOrWhiteSpace(value.CanonicalizationMethod)
            || string.IsNullOrWhiteSpace(value.SignatureMethod)
            || string.IsNullOrWhiteSpace(value.DigestMethod)
            || value.ReferenceUri != string.Empty
            || transforms is null
            || transforms.Count == 0
            || value.CertificateTrustValidated
            || value.VerifiedAtUtc == default)
        {
            throw Conflict(
                "fiscal.envelope.document_response.signature.persisted_evidence_invalid",
                "Persisted ACKCFE signature-verification evidence is incomplete or internally inconsistent.",
                "invalid_persisted_evidence");
        }
    }

    private static void EnsureSameSource(
        StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification verification,
        StoredFiscalCfeEnvelopeDocumentResponseConsultation source)
    {
        if (verification.ConsultationId != source.Id
            || verification.AckObservationId != source.AckObservationId
            || verification.SubmissionId != source.SubmissionId
            || verification.EnvelopeId != source.EnvelopeId
            || !string.Equals(verification.OrganizationId, source.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(verification.ResponseSha256, source.ResponseSha256, StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.envelope.document_response.signature.persisted_source_mismatch",
                "Persisted ACKCFE signature verification no longer matches its durable consultation source.",
                "invalid_persisted_evidence");
        }
    }

    private static FiscalCfeEnvelopeDocumentResponseSignatureVerificationResult Result(
        StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification value,
        bool replayed)
    {
        var transforms = JsonSerializer.Deserialize<List<string>>(value.ReferenceTransformsJson)
            ?? throw Conflict(
                "fiscal.envelope.document_response.signature.persisted_evidence_invalid",
                "Persisted ACKCFE XMLDSig transform evidence is invalid.",
                "invalid_persisted_evidence");

        return new(
            value.Id,
            value.ConsultationId,
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
}
