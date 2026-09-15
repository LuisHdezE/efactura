using EFactura.Application.Common.Security;
using EFactura.Application.Fiscal;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1.Contracts;
using WebApi.CrossCutting.Authorization;
using WebApi.CrossCutting.Requests;

namespace WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/fiscal-documents")]
public sealed class FiscalCfeStateEvidenceController : ControllerBase
{
    private const string OperationContractId = "API-FIS-011";

    private readonly V1OrganizationContextResolver _organization;
    private readonly ConsultFiscalCfeStateUseCase _consult;

    public FiscalCfeStateEvidenceController(
        V1OrganizationContextResolver organization,
        ConsultFiscalCfeStateUseCase consult)
    {
        _organization = organization ?? throw new ArgumentNullException(nameof(organization));
        _consult = consult ?? throw new ArgumentNullException(nameof(consult));
    }

    [HttpPost("{fiscalDocumentId:guid}/dgi-state-evidence")]
    [RequirePermission(Permissions.FiscalRegularizationManage)]
    public async Task<ActionResult<FiscalCfeStateEvidenceDto>> CollectDgiStateEvidence(
        Guid fiscalDocumentId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var idempotencyKey = V1RequestContract.RequireIdempotencyKey(Request);
        var operationHash = V1RequestContract.ComputeRequestHash(new StateEvidenceOperationSeed(
            OperationContractId,
            organizationId,
            fiscalDocumentId,
            idempotencyKey));
        var operationId = $"{OperationContractId}:{operationHash}";

        var result = await _consult.ExecuteAsync(
            new ConsultFiscalCfeStateCommand(
                organizationId,
                fiscalDocumentId,
                operationId),
            cancellationToken);

        SetReplayHeader(result.Replayed);
        return Ok(Map(result));
    }

    private static FiscalCfeStateEvidenceDto Map(FiscalCfeStateConsultationResult result) =>
        new(
            result.ConsultationId.ToString(),
            result.FiscalDocumentId.ToString(),
            (int)result.CfeType,
            result.Series,
            result.Number,
            result.StateCode,
            result.ConsultedAtUtc,
            result.ResponseSha256,
            result.Replayed,
            StateMeaningResolved: false,
            LocalLifecycleMutationAuthorized: false);

    private void SetReplayHeader(bool replayed)
    {
        if (replayed)
            Response.Headers["Idempotent-Replayed"] = "true";
    }

    private sealed record StateEvidenceOperationSeed(
        string Operation,
        string OrganizationId,
        Guid FiscalDocumentId,
        string IdempotencyKey);
}
