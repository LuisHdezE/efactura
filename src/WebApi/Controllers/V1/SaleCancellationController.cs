using EFactura.Application.Common.Security;
using EFactura.Application.Sales;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1.Contracts;
using WebApi.CrossCutting.Authorization;
using WebApi.CrossCutting.Requests;

namespace WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/sales/{saleId:guid}/cancel")]
public sealed class SaleCancellationController : ControllerBase
{
    private readonly V1OrganizationContextResolver _organization;
    private readonly CancelSaleUseCase _cancel;

    public SaleCancellationController(
        V1OrganizationContextResolver organization,
        CancelSaleUseCase cancel)
    {
        _organization = organization;
        _cancel = cancel;
    }

    [HttpPost(Name = "cancelSale")]
    [RequirePermission(Permissions.SalesCancel)]
    public async Task<ActionResult<SaleCancellationDto>> Cancel(
        Guid saleId,
        [FromBody] SaleCancelRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _cancel.ExecuteAsync(
            new CancelSaleCommand(
                organizationId,
                saleId,
                request.ExpectedVersion,
                request.OperatorReason,
                request.OperatorContext,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);

        if (result.Replayed)
            Response.Headers["Idempotent-Replayed"] = "true";

        return Ok(new SaleCancellationDto(
            result.SaleId.ToString(),
            result.Version,
            "CANCELLED",
            result.Replayed));
    }
}
