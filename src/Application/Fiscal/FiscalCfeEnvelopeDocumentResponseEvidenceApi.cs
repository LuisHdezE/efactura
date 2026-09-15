using EFactura.Application.Common.Errors;

namespace EFactura.Application.Fiscal;

public sealed record CollectFiscalCfeEnvelopeDocumentResponseEvidenceByEnvelopeIdCommand(
    string OrganizationId,
    Guid EnvelopeId,
    string OperationId);

/// <summary>
/// Public-API-facing Application boundary for the explicit ACKCFE evidence cycle.
/// The caller identifies only the durable Sobre resource; issuer/receiver/Idemisor identity is
/// resolved from the already durable submission evidence and is never accepted as authoritative
/// client input.
/// </summary>
public sealed class CollectFiscalCfeEnvelopeDocumentResponseEvidenceByEnvelopeIdUseCase
{
    private readonly IFiscalCfeEnvelopeSubmissionRepository _submissions;
    private readonly CollectFiscalCfeEnvelopeDocumentResponseEvidenceUseCase _collect;

    public CollectFiscalCfeEnvelopeDocumentResponseEvidenceByEnvelopeIdUseCase(
        IFiscalCfeEnvelopeSubmissionRepository submissions,
        CollectFiscalCfeEnvelopeDocumentResponseEvidenceUseCase collect)
    {
        _submissions = submissions ?? throw new ArgumentNullException(nameof(submissions));
        _collect = collect ?? throw new ArgumentNullException(nameof(collect));
    }

    public async Task<FiscalCfeEnvelopeDocumentResponseEvidenceCycleResult> ExecuteAsync(
        CollectFiscalCfeEnvelopeDocumentResponseEvidenceByEnvelopeIdCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var organizationId = command.OrganizationId?.Trim() ?? string.Empty;
        var operationId = command.OperationId?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(organizationId) || organizationId.Length > 200)
        {
            throw Validation(
                "fiscal.envelope.document_response.evidence_api.organization_invalid",
                "Organization id is required and must not exceed 200 characters.");
        }
        if (command.EnvelopeId == Guid.Empty)
        {
            throw Validation(
                "fiscal.envelope.document_response.evidence_api.envelope_id_invalid",
                "Fiscal envelope id must be a non-empty GUID.");
        }
        if (string.IsNullOrWhiteSpace(operationId) || operationId.Length > 120)
        {
            throw Validation(
                "fiscal.envelope.document_response.evidence_api.operation_id_invalid",
                "Operation id is required and must not exceed 120 characters.");
        }

        var submission = await _submissions.GetByEnvelopeIdAsync(command.EnvelopeId, cancellationToken);
        if (submission is null
            || !string.Equals(submission.OrganizationId, organizationId, StringComparison.Ordinal))
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound,
                "fiscal.envelope.document_response.evidence_api.envelope_not_found",
                "Fiscal envelope was not found in the current organization scope.");
        }

        PrepareFiscalCfeEnvelopeSubmissionUseCase.EnsureSubmissionIntegrity(submission);
        if (submission.EnvelopeId != command.EnvelopeId)
        {
            throw Conflict(
                "fiscal.envelope.document_response.evidence_api.submission_lineage_mismatch",
                "Persisted fiscal-envelope submission evidence no longer matches the requested envelope.");
        }

        var result = await _collect.ExecuteAsync(
            new CollectFiscalCfeEnvelopeDocumentResponseEvidenceCommand(
                organizationId,
                submission.IssuerRuc,
                submission.ReceiverRut,
                submission.SenderEnvelopeId,
                operationId),
            cancellationToken);

        if (result.Consultation.EnvelopeId != command.EnvelopeId
            || result.Coverage.EnvelopeId != command.EnvelopeId
            || !string.Equals(result.Consultation.OrganizationId, organizationId, StringComparison.Ordinal)
            || !string.Equals(result.Coverage.OrganizationId, organizationId, StringComparison.Ordinal)
            || result.DgiIdentityValidated
            || result.ProtocolFinalityProven
            || result.TokenExhaustionProven
            || result.AutomaticReconsultationAuthorized)
        {
            throw Conflict(
                "fiscal.envelope.document_response.evidence_api.result_lineage_mismatch",
                "The ACKCFE evidence cycle result does not match the requested durable envelope boundary.");
        }

        return result;
    }

    private static ApplicationProblemException Validation(string code, string message) =>
        new(ApplicationProblemKind.Validation, code, message);

    private static ApplicationProblemException Conflict(string code, string message) =>
        new(ApplicationProblemKind.Conflict, code, message, conflictType: "invalid_persisted_evidence");
}
