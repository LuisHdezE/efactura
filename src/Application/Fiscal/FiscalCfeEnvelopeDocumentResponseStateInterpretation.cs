using System.Text.Json;
using EFactura.Application.Common.Errors;

namespace EFactura.Application.Fiscal;

/// <summary>
/// Semantic meaning published by DGI for ACKCFE_det/Estado.
/// This classification is evidence-only and does not mutate any local fiscal lifecycle.
/// </summary>
public enum FiscalCfeEnvelopeDocumentResponseSemanticState
{
    Received = 1,
    Rejected = 2,
    ObservedContingency = 3
}

public sealed record FiscalCfeEnvelopeDocumentResponseSemanticDetail(
    int Ordinal,
    int CfeType,
    string Series,
    long Number,
    string StateCode,
    FiscalCfeEnvelopeDocumentResponseSemanticState State);

public sealed record InterpretFiscalCfeEnvelopeDocumentResponseStateCommand(
    string OrganizationId,
    string ConsultationOperationId,
    string TrustValidationOperationId);

public sealed record FiscalCfeEnvelopeDocumentResponseStateInterpretationResult(
    Guid ConsultationId,
    Guid SignatureVerificationId,
    Guid TrustValidationId,
    string OrganizationId,
    string ResponseSha256,
    int EnvelopeCfeCount,
    int RespondedCount,
    int AcceptedCount,
    int RejectedCount,
    int ObservedCount,
    int OtherRejectedCount,
    IReadOnlyList<FiscalCfeEnvelopeDocumentResponseSemanticDetail> Details,
    bool PkiUruguayTrustValidated,
    bool DgiIdentityValidated);

/// <summary>
/// Interprets the published ACKCFE detail-state taxonomy only after the exact consultation response
/// has durable XMLDSig verification and durable PKI Uruguay trust evidence. DGI's response format
/// documents AE = received, BE = rejected CFE, and CE = observed contingency document (CFC).
/// DGI also permits per-document results to arrive in one or multiple response messages, therefore
/// this use case never claims complete Sobre resolution and never changes FiscalDocument state.
/// </summary>
public sealed class InterpretFiscalCfeEnvelopeDocumentResponseStateUseCase
{
    private readonly IFiscalCfeEnvelopeDocumentResponseConsultationRepository _consultations;
    private readonly IFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository _signatureVerifications;
    private readonly IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationRepository _trustValidations;

    public InterpretFiscalCfeEnvelopeDocumentResponseStateUseCase(
        IFiscalCfeEnvelopeDocumentResponseConsultationRepository consultations,
        IFiscalCfeEnvelopeDocumentResponseSignatureVerificationRepository signatureVerifications,
        IFiscalCfeEnvelopeDocumentResponseCertificateTrustValidationRepository trustValidations)
    {
        _consultations = consultations ?? throw new ArgumentNullException(nameof(consultations));
        _signatureVerifications = signatureVerifications ?? throw new ArgumentNullException(nameof(signatureVerifications));
        _trustValidations = trustValidations ?? throw new ArgumentNullException(nameof(trustValidations));
    }

    public async Task<FiscalCfeEnvelopeDocumentResponseStateInterpretationResult> ExecuteAsync(
        InterpretFiscalCfeEnvelopeDocumentResponseStateCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var normalized = Normalize(command);

        var consultation = await _consultations.GetByOperationAsync(
            normalized.OrganizationId,
            normalized.ConsultationOperationId,
            cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.document_response.semantics.consultation_required",
                "A durable ACKCFE consultation is required before semantic interpretation.",
                "missing_prerequisite");
        ConsultFiscalCfeEnvelopeDocumentResponseUseCase.EnsureStoredIntegrity(consultation);
        if (!string.Equals(consultation.OrganizationId, normalized.OrganizationId, StringComparison.Ordinal))
        {
            throw Conflict(
                "fiscal.envelope.document_response.semantics.consultation_source_mismatch",
                "ACKCFE consultation does not belong to the requested organization.",
                "invalid_persisted_evidence");
        }

        var signature = await _signatureVerifications.GetByConsultationIdAsync(
            consultation.Id,
            cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.document_response.semantics.signature_verification_required",
                "Successful durable ACKCFE XMLDSig verification is required before semantic interpretation.",
                "missing_prerequisite");
        VerifyFiscalCfeEnvelopeDocumentResponseSignatureUseCase.EnsureVerificationIntegrity(signature);

        var trust = await _trustValidations.GetByOperationAsync(
            normalized.OrganizationId,
            normalized.TrustValidationOperationId,
            cancellationToken)
            ?? throw Conflict(
                "fiscal.envelope.document_response.semantics.trust_validation_required",
                "Successful durable ACKCFE PKI Uruguay trust validation is required before semantic interpretation.",
                "missing_prerequisite");
        ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustUseCase.EnsureValidationIntegrity(trust);

        EnsureSourceContinuity(consultation, signature, trust);

        IReadOnlyList<FiscalCfeEnvelopeDocumentResponseDetail>? rawDetails;
        try
        {
            rawDetails = JsonSerializer.Deserialize<List<FiscalCfeEnvelopeDocumentResponseDetail>>(consultation.DetailsJson);
        }
        catch (JsonException)
        {
            rawDetails = null;
        }
        if (rawDetails is null || rawDetails.Count != consultation.RespondedCount)
        {
            throw Conflict(
                "fiscal.envelope.document_response.semantics.details_invalid",
                "Persisted ACKCFE details cannot be interpreted safely.",
                "invalid_persisted_evidence");
        }

        var accepted = 0;
        var rejected = 0;
        var observed = 0;
        var interpreted = new List<FiscalCfeEnvelopeDocumentResponseSemanticDetail>(rawDetails.Count);
        foreach (var detail in rawDetails)
        {
            var state = detail.StateCode switch
            {
                "AE" => FiscalCfeEnvelopeDocumentResponseSemanticState.Received,
                "BE" => FiscalCfeEnvelopeDocumentResponseSemanticState.Rejected,
                "CE" => FiscalCfeEnvelopeDocumentResponseSemanticState.ObservedContingency,
                _ => throw Conflict(
                    "fiscal.envelope.document_response.semantics.state_code_unsupported",
                    $"ACKCFE detail state '{detail.StateCode}' is not in the governed DGI AE/BE/CE taxonomy.",
                    "unsupported_external_semantics")
            };

            switch (state)
            {
                case FiscalCfeEnvelopeDocumentResponseSemanticState.Received:
                    accepted++;
                    break;
                case FiscalCfeEnvelopeDocumentResponseSemanticState.Rejected:
                    rejected++;
                    break;
                case FiscalCfeEnvelopeDocumentResponseSemanticState.ObservedContingency:
                    observed++;
                    break;
            }

            interpreted.Add(new FiscalCfeEnvelopeDocumentResponseSemanticDetail(
                detail.Ordinal,
                detail.CfeType,
                detail.Series,
                detail.Number,
                detail.StateCode,
                state));
        }

        if (accepted != consultation.AcceptedCount
            || rejected != consultation.RejectedCount
            || observed != consultation.ObservedCount)
        {
            throw Conflict(
                "fiscal.envelope.document_response.semantics.count_mismatch",
                "ACKCFE Caratula counters do not match the governed per-document AE/BE/CE detail semantics.",
                "invalid_external_semantics");
        }

        return new FiscalCfeEnvelopeDocumentResponseStateInterpretationResult(
            consultation.Id,
            signature.Id,
            trust.Id,
            consultation.OrganizationId,
            consultation.ResponseSha256,
            consultation.EnvelopeCfeCount,
            consultation.RespondedCount,
            consultation.AcceptedCount,
            consultation.RejectedCount,
            consultation.ObservedCount,
            consultation.OtherRejectedCount,
            interpreted,
            trust.PkiUruguayTrustValidated,
            trust.DgiIdentityValidated);
    }

    private static InterpretFiscalCfeEnvelopeDocumentResponseStateCommand Normalize(
        InterpretFiscalCfeEnvelopeDocumentResponseStateCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.OrganizationId) || command.OrganizationId.Trim().Length > 200)
        {
            throw Conflict(
                "fiscal.envelope.document_response.semantics.organization_invalid",
                "Organization id is required and must contain at most 200 characters.",
                "invalid_request");
        }
        if (string.IsNullOrWhiteSpace(command.ConsultationOperationId)
            || command.ConsultationOperationId.Trim().Length > 120)
        {
            throw Conflict(
                "fiscal.envelope.document_response.semantics.consultation_operation_invalid",
                "Consultation operation id is required and must contain at most 120 characters.",
                "invalid_request");
        }
        if (string.IsNullOrWhiteSpace(command.TrustValidationOperationId)
            || command.TrustValidationOperationId.Trim().Length > 120)
        {
            throw Conflict(
                "fiscal.envelope.document_response.semantics.trust_operation_invalid",
                "Trust-validation operation id is required and must contain at most 120 characters.",
                "invalid_request");
        }

        return command with
        {
            OrganizationId = command.OrganizationId.Trim(),
            ConsultationOperationId = command.ConsultationOperationId.Trim(),
            TrustValidationOperationId = command.TrustValidationOperationId.Trim()
        };
    }

    private static void EnsureSourceContinuity(
        StoredFiscalCfeEnvelopeDocumentResponseConsultation consultation,
        StoredFiscalCfeEnvelopeDocumentResponseSignatureVerification signature,
        StoredFiscalCfeEnvelopeDocumentResponseCertificateTrustValidation trust)
    {
        if (signature.ConsultationId != consultation.Id
            || signature.AckObservationId != consultation.AckObservationId
            || signature.SubmissionId != consultation.SubmissionId
            || signature.EnvelopeId != consultation.EnvelopeId
            || !string.Equals(signature.OrganizationId, consultation.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(signature.ResponseSha256, consultation.ResponseSha256, StringComparison.Ordinal)
            || trust.SignatureVerificationId != signature.Id
            || trust.ConsultationId != consultation.Id
            || trust.AckObservationId != consultation.AckObservationId
            || trust.SubmissionId != consultation.SubmissionId
            || trust.EnvelopeId != consultation.EnvelopeId
            || !string.Equals(trust.OrganizationId, consultation.OrganizationId, StringComparison.Ordinal)
            || !string.Equals(trust.ResponseSha256, consultation.ResponseSha256, StringComparison.Ordinal)
            || !string.Equals(trust.CertificateSha256, signature.CertificateSha256, StringComparison.OrdinalIgnoreCase)
            || !trust.PkiUruguayTrustValidated
            || trust.DgiIdentityValidated)
        {
            throw Conflict(
                "fiscal.envelope.document_response.semantics.source_mismatch",
                "ACKCFE semantic interpretation source chain is incomplete or does not match the durable consultation, signature and trust evidence.",
                "invalid_persisted_evidence");
        }
    }

    private static ApplicationProblemException Conflict(string code, string message, string conflictType) =>
        new(ApplicationProblemKind.Conflict, code, message, conflictType: conflictType);
}
