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
/// Narrow Application seam for the four already-governed ACKCFE boundaries composed by the explicit
/// evidence cycle. It exists so orchestration behavior can be tested independently while production
/// still delegates to the canonical consultation, XMLDSig, PKI trust and coverage use cases.
/// </summary>
public interface IFiscalCfeEnvelopeDocumentResponseEvidenceCycleSteps
{
    Task<FiscalCfeEnvelopeDocumentResponseConsultationResult> ConsultAsync(
        CollectFiscalCfeEnvelopeDocumentResponseEvidenceCommand command,
        CancellationToken cancellationToken = default);

    Task<FiscalCfeEnvelopeDocumentResponseSignatureVerificationResult> VerifySignatureAsync(
        string organizationId,
        string consultationOperationId,
        CancellationToken cancellationToken = default);

    Task<FiscalCfeEnvelopeDocumentResponseCertificateTrustValidationResult> ValidateTrustAsync(
        string organizationId,
        string consultationOperationId,
        string operationId,
        CancellationToken cancellationToken = default);

    Task<FiscalCfeEnvelopeDocumentResponseCoverageAssessmentResult> AssessCoverageAsync(
        CollectFiscalCfeEnvelopeDocumentResponseEvidenceCommand command,
        CancellationToken cancellationToken = default);
}

public sealed class FiscalCfeEnvelopeDocumentResponseEvidenceCycleSteps :
    IFiscalCfeEnvelopeDocumentResponseEvidenceCycleSteps
{
    private readonly ConsultFiscalCfeEnvelopeDocumentResponseUseCase _consult;
    private readonly VerifyFiscalCfeEnvelopeDocumentResponseSignatureUseCase _verifySignature;
    private readonly ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustUseCase _validateTrust;
    private readonly AssessFiscalCfeEnvelopeDocumentResponseCoverageUseCase _assessCoverage;

    public FiscalCfeEnvelopeDocumentResponseEvidenceCycleSteps(
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

    public Task<FiscalCfeEnvelopeDocumentResponseConsultationResult> ConsultAsync(
        CollectFiscalCfeEnvelopeDocumentResponseEvidenceCommand command,
        CancellationToken cancellationToken = default) =>
        _consult.ExecuteAsync(
            new ConsultFiscalCfeEnvelopeDocumentResponseCommand(
                command.OrganizationId,
                command.IssuerRuc,
                command.ReceiverRut,
                command.SenderEnvelopeId,
                command.OperationId),
            cancellationToken);

    public Task<FiscalCfeEnvelopeDocumentResponseSignatureVerificationResult> VerifySignatureAsync(
        string organizationId,
        string consultationOperationId,
        CancellationToken cancellationToken = default) =>
        _verifySignature.ExecuteAsync(
            new VerifyFiscalCfeEnvelopeDocumentResponseSignatureCommand(
                organizationId,
                consultationOperationId),
            cancellationToken);

    public Task<FiscalCfeEnvelopeDocumentResponseCertificateTrustValidationResult> ValidateTrustAsync(
        string organizationId,
        string consultationOperationId,
        string operationId,
        CancellationToken cancellationToken = default) =>
        _validateTrust.ExecuteAsync(
            new ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustCommand(
                organizationId,
                consultationOperationId,
                operationId),
            cancellationToken);

    public Task<FiscalCfeEnvelopeDocumentResponseCoverageAssessmentResult> AssessCoverageAsync(
        CollectFiscalCfeEnvelopeDocumentResponseEvidenceCommand command,
        CancellationToken cancellationToken = default) =>
        _assessCoverage.ExecuteAsync(
            new AssessFiscalCfeEnvelopeDocumentResponseCoverageCommand(
                command.OrganizationId,
                command.IssuerRuc,
                command.ReceiverRut,
                command.SenderEnvelopeId),
            cancellationToken);
}

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
    private readonly IFiscalCfeEnvelopeDocumentResponseEvidenceCycleSteps _steps;

    public CollectFiscalCfeEnvelopeDocumentResponseEvidenceUseCase(
        ConsultFiscalCfeEnvelopeDocumentResponseUseCase consult,
        VerifyFiscalCfeEnvelopeDocumentResponseSignatureUseCase verifySignature,
        ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustUseCase validateTrust,
        AssessFiscalCfeEnvelopeDocumentResponseCoverageUseCase assessCoverage)
        : this(new FiscalCfeEnvelopeDocumentResponseEvidenceCycleSteps(
            consult,
            verifySignature,
            validateTrust,
            assessCoverage))
    {
    }

    public CollectFiscalCfeEnvelopeDocumentResponseEvidenceUseCase(
        IFiscalCfeEnvelopeDocumentResponseEvidenceCycleSteps steps)
    {
        _steps = steps ?? throw new ArgumentNullException(nameof(steps));
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

        var consultation = await _steps.ConsultAsync(normalized, cancellationToken);
        var signature = await _steps.VerifySignatureAsync(
            normalized.OrganizationId,
            normalized.OperationId,
            cancellationToken);
        var trust = await _steps.ValidateTrustAsync(
            normalized.OrganizationId,
            normalized.OperationId,
            normalized.OperationId,
            cancellationToken);
        var coverage = await _steps.AssessCoverageAsync(normalized, cancellationToken);

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
