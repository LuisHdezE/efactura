using EFactura.Application.Common.Errors;

namespace EFactura.Application.Fiscal;

public sealed record CollectFiscalCfeEnvelopeDocumentResponseEvidenceCommand(
    string OrganizationId,
    string IssuerRuc,
    string ReceiverRut,
    long SenderEnvelopeId,
    string OperationId);

public sealed record FiscalCfeEnvelopeDocumentResponseEvidenceCycleResult(
    FiscalCfeEnvelopeDocumentResponseConsultationResult Consultation,
    FiscalCfeEnvelopeDocumentResponseSignatureVerificationResult SignatureVerification,
    FiscalCfeEnvelopeDocumentResponseCertificateTrustValidationResult TrustValidation,
    FiscalCfeEnvelopeDocumentResponseCoverageAssessmentResult Coverage,
    bool ConsultationReplayed,
    bool SignatureVerificationReplayed,
    bool TrustValidationReplayed,
    bool FullyReplayed,
    bool DgiIdentityValidated,
    bool ProtocolFinalityProven,
    bool TokenExhaustionProven,
    bool AutomaticReconsultationAuthorized);

/// <summary>
/// Executes one explicit, caller-triggered ACKCFE evidence cycle for one durable Sobre source:
/// consultation by the accepted ACKSobre IdReceptor + Token, XMLDSig verification, PKI Uruguay
/// trust validation, and read-only known-document coverage assessment.
///
/// The same OperationId is deliberately reused across the append-only consultation and trust
/// checkpoints. Re-executing the same operation resumes/replays those checkpoints; supplying a new
/// OperationId represents another explicit consultation request. This boundary contains no loop,
/// scheduler, polling cadence, retry policy, token-exhaustion inference or protocol-finality claim.
/// </summary>
public sealed class CollectFiscalCfeEnvelopeDocumentResponseEvidenceUseCase
{
    private readonly ConsultFiscalCfeEnvelopeDocumentResponseUseCase _consult;
    private readonly VerifyFiscalCfeEnvelopeDocumentResponseSignatureUseCase _verifySignature;
    private readonly ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustUseCase _validateTrust;
    private readonly AssessFiscalCfeEnvelopeDocumentResponseCoverageUseCase _assessCoverage;

    public CollectFiscalCfeEnvelopeDocumentResponseEvidenceUseCase(
        ConsultFiscalCfeEnvelopeDocumentResponseUseCase consult,
        VerifyFiscalCfeEnvelopeDocumentResponseSignatureUseCase verifySignature,
        ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustUseCase validateTrust,
        AssessFiscalCfeEnvelopeDocumentResponseCoverageUseCase assessCoverage)
    {
        _consult = consult ?? throw new ArgumentNullException(nameof(consult));
        _verifySignature = verifySignature ?? throw new ArgumentNullException(nameof(verifySignature));
        _validateTrust = validateTrust ?? throw new ArgumentNullException(nameof(validateTrust));
        _assessCoverage = assessCoverage ?? throw new ArgumentNullException(nameof(assessCoverage));
    }

    public async Task<FiscalCfeEnvelopeDocumentResponseEvidenceCycleResult> ExecuteAsync(
        CollectFiscalCfeEnvelopeDocumentResponseEvidenceCommand command,
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
                "fiscal.envelope.document_response.evidence_cycle.operation_id_invalid",
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

        var consultation = await _consult.ExecuteAsync(
            new ConsultFiscalCfeEnvelopeDocumentResponseCommand(
                normalized.OrganizationId,
                normalized.IssuerRuc,
                normalized.ReceiverRut,
                normalized.SenderEnvelopeId,
                normalized.OperationId),
            cancellationToken);

        var signature = await _verifySignature.ExecuteAsync(
            new VerifyFiscalCfeEnvelopeDocumentResponseSignatureCommand(
                normalized.OrganizationId,
                normalized.OperationId),
            cancellationToken);

        var trust = await _validateTrust.ExecuteAsync(
            new ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustCommand(
                normalized.OrganizationId,
                normalized.OperationId,
                normalized.OperationId),
            cancellationToken);

        var coverage = await _assessCoverage.ExecuteAsync(
            new AssessFiscalCfeEnvelopeDocumentResponseCoverageCommand(
                normalized.OrganizationId,
                normalized.IssuerRuc,
                normalized.ReceiverRut,
                normalized.SenderEnvelopeId),
            cancellationToken);

        EnsureExactLineage(consultation, signature, trust, coverage);

        return new FiscalCfeEnvelopeDocumentResponseEvidenceCycleResult(
            consultation,
            signature,
            trust,
            coverage,
            consultation.Replayed,
            signature.Replayed,
            trust.Replayed,
            consultation.Replayed && signature.Replayed && trust.Replayed,
            DgiIdentityValidated: false,
            ProtocolFinalityProven: false,
            TokenExhaustionProven: false,
            AutomaticReconsultationAuthorized: false);
    }

    private static void EnsureExactLineage(
        FiscalCfeEnvelopeDocumentResponseConsultationResult consultation,
        FiscalCfeEnvelopeDocumentResponseSignatureVerificationResult signature,
        FiscalCfeEnvelopeDocumentResponseCertificateTrustValidationResult trust,
        FiscalCfeEnvelopeDocumentResponseCoverageAssessmentResult coverage)
    {
        if (!signature.SignatureValid
            || !trust.PkiUruguayTrustValidated
            || signature.ConsultationId != consultation.ConsultationId
            || signature.AckObservationId != consultation.AckObservationId
            || signature.SubmissionId != consultation.SubmissionId
            || signature.EnvelopeId != consultation.EnvelopeId
            || !string.Equals(signature.OrganizationId, consultation.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(signature.ResponseSha256, consultation.ResponseSha256, StringComparison.Ordinal)
            || trust.SignatureVerificationId != signature.VerificationId
            || trust.ConsultationId != consultation.ConsultationId
            || trust.AckObservationId != consultation.AckObservationId
            || trust.SubmissionId != consultation.SubmissionId
            || trust.EnvelopeId != consultation.EnvelopeId
            || !string.Equals(trust.OrganizationId, consultation.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(trust.ResponseSha256, consultation.ResponseSha256, StringComparison.Ordinal)
            || coverage.AckObservationId != consultation.AckObservationId
            || coverage.SubmissionId != consultation.SubmissionId
            || coverage.EnvelopeId != consultation.EnvelopeId
            || !string.Equals(coverage.OrganizationId, consultation.OrganizationId, StringComparison.Ordinal)
            || trust.DgiIdentityValidated
            || coverage.DgiIdentityValidated
            || coverage.ProtocolFinalityProven
            || coverage.TokenExhaustionProven
            || coverage.AutomaticReconsultationAuthorized)
        {
            throw Conflict(
                "fiscal.envelope.document_response.evidence_cycle.lineage_mismatch",
                "The explicit ACKCFE evidence cycle did not resolve to one exact trusted source lineage without overclaiming finality or DGI identity.",
                "invalid_persisted_evidence");
        }
    }

    private static ApplicationProblemException Conflict(string code, string message, string conflictType) =>
        new(ApplicationProblemKind.Conflict, code, message, conflictType: conflictType);
}
