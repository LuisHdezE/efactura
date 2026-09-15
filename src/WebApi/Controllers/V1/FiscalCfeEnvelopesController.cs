using EFactura.Application.Common.Security;
using EFactura.Application.Fiscal;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1.Contracts;
using WebApi.CrossCutting.Authorization;
using WebApi.CrossCutting.Requests;

namespace WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/fiscal-envelopes")]
public sealed class FiscalCfeEnvelopesController : ControllerBase
{
    private const string OperationContractId = "API-FIS-010";

    private readonly V1OrganizationContextResolver _organization;
    private readonly CollectFiscalCfeEnvelopeDocumentResponseEvidenceByEnvelopeIdUseCase _collect;

    public FiscalCfeEnvelopesController(
        V1OrganizationContextResolver organization,
        CollectFiscalCfeEnvelopeDocumentResponseEvidenceByEnvelopeIdUseCase collect)
    {
        _organization = organization ?? throw new ArgumentNullException(nameof(organization));
        _collect = collect ?? throw new ArgumentNullException(nameof(collect));
    }

    [HttpPost("{envelopeId:guid}/document-response-evidence")]
    [RequirePermission(Permissions.FiscalRegularizationManage)]
    public async Task<ActionResult<FiscalCfeEnvelopeDocumentResponseEvidenceDto>> CollectDocumentResponseEvidence(
        Guid envelopeId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var idempotencyKey = V1RequestContract.RequireIdempotencyKey(Request);
        var operationHash = V1RequestContract.ComputeRequestHash(new EvidenceOperationSeed(
            OperationContractId,
            organizationId,
            envelopeId,
            idempotencyKey));
        var operationId = $"{OperationContractId}:{operationHash}";

        var result = await _collect.ExecuteAsync(
            new CollectFiscalCfeEnvelopeDocumentResponseEvidenceByEnvelopeIdCommand(
                organizationId,
                envelopeId,
                operationId),
            cancellationToken);

        SetReplayHeader(result.FullyReplayed);
        return Ok(Map(result));
    }

    private static FiscalCfeEnvelopeDocumentResponseEvidenceDto Map(
        FiscalCfeEnvelopeDocumentResponseEvidenceCycleResult result) =>
        new(
            result.Consultation.EnvelopeId.ToString(),
            result.Consultation.SubmissionId.ToString(),
            result.Consultation.AckObservationId.ToString(),
            result.Consultation.ConsultationId.ToString(),
            result.Consultation.ConsultedAtUtc,
            result.Consultation.ResponseSha256,
            result.Consultation.EnvelopeCfeCount,
            result.Consultation.RespondedCount,
            result.Consultation.AcceptedCount,
            result.Consultation.RejectedCount,
            result.Consultation.ObservedCount,
            result.Consultation.OtherRejectedCount,
            MapCoverageStatus(result.Coverage.CoverageStatus),
            result.Coverage.ConsultationObservationCount,
            result.Coverage.DistinctResponseMessageCount,
            result.Coverage.CoveredDocumentCount,
            result.Coverage.MissingDocumentCount,
            result.Coverage.CoveredDocuments.Select(MapCoveredDocument).ToArray(),
            result.Coverage.MissingDocuments.Select(MapMissingDocument).ToArray(),
            result.SignatureVerification.SignatureValid,
            result.TrustValidation.PkiUruguayTrustValidated,
            result.ConsultationReplayed,
            result.SignatureVerificationReplayed,
            result.TrustValidationReplayed,
            result.FullyReplayed,
            result.DgiIdentityValidated,
            result.ProtocolFinalityProven,
            result.TokenExhaustionProven,
            result.AutomaticReconsultationAuthorized);

    private static FiscalCfeEnvelopeDocumentResponseCoveredDocumentDto MapCoveredDocument(
        FiscalCfeEnvelopeDocumentResponseCoverageDocument document) =>
        new(
            document.CfeType,
            document.Series,
            document.Number,
            document.StateCode,
            MapSemanticState(document.State),
            document.EvidenceMessageCount);

    private static FiscalCfeEnvelopeDocumentResponseMissingDocumentDto MapMissingDocument(
        FiscalCfeEnvelopeDocumentResponseMissingDocument document) =>
        new(document.CfeType, document.Series, document.Number);

    private static string MapCoverageStatus(FiscalCfeEnvelopeDocumentResponseCoverageStatus status) =>
        status switch
        {
            FiscalCfeEnvelopeDocumentResponseCoverageStatus.NoDocumentCoverage => "NO_DOCUMENT_COVERAGE",
            FiscalCfeEnvelopeDocumentResponseCoverageStatus.PartialDocumentCoverage => "PARTIAL_DOCUMENT_COVERAGE",
            FiscalCfeEnvelopeDocumentResponseCoverageStatus.FullDocumentCoverage => "FULL_DOCUMENT_COVERAGE",
            _ => throw new InvalidOperationException("Unsupported ACKCFE coverage status.")
        };

    private static string MapSemanticState(FiscalCfeEnvelopeDocumentResponseSemanticState state) =>
        state switch
        {
            FiscalCfeEnvelopeDocumentResponseSemanticState.Received => "RECEIVED",
            FiscalCfeEnvelopeDocumentResponseSemanticState.Rejected => "REJECTED",
            FiscalCfeEnvelopeDocumentResponseSemanticState.ObservedContingency => "OBSERVED_CONTINGENCY",
            _ => throw new InvalidOperationException("Unsupported ACKCFE semantic state.")
        };

    private void SetReplayHeader(bool replayed)
    {
        if (replayed)
            Response.Headers["Idempotent-Replayed"] = "true";
    }

    private sealed record EvidenceOperationSeed(
        string Operation,
        string OrganizationId,
        Guid EnvelopeId,
        string IdempotencyKey);
}
