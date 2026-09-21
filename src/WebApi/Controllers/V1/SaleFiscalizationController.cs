using EFactura.Application.Common.Security;
using EFactura.Application.Sales;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1.Contracts;
using WebApi.CrossCutting.Authorization;
using WebApi.CrossCutting.Requests;

namespace WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/sales/{saleId:guid}/fiscalization")]
public sealed class SaleFiscalizationController : ControllerBase
{
    private const string StatusAuthority = "LOCAL_WORKFLOW_ONLY_NOT_DGI_ACCEPTANCE";

    private readonly V1OrganizationContextResolver _organization;
    private readonly GetSaleFiscalizationStatusUseCase _get;

    public SaleFiscalizationController(
        V1OrganizationContextResolver organization,
        GetSaleFiscalizationStatusUseCase get)
    {
        _organization = organization;
        _get = get;
    }

    [HttpGet(Name = "getSaleFiscalizationStatus")]
    [RequirePermission(Permissions.SalesRead)]
    public async Task<ActionResult<SaleFiscalizationStatusDto>> Get(
        Guid saleId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        return Ok(Map(await _get.ExecuteAsync(organizationId, saleId, cancellationToken)));
    }

    private static SaleFiscalizationStatusDto Map(SaleFiscalizationStatusView view) =>
        new(
            view.SaleId.ToString(),
            ToApiEnum(view.SaleStatus),
            ToApiEnum(view.WorkflowStatus),
            view.FiscalizationRequestId?.ToString(),
            view.FiscalizationVersion,
            view.RequestedAtUtc,
            view.CfeFamily.HasValue ? (int)view.CfeFamily.Value : null,
            view.CfeFamily.HasValue ? ToApiEnum(view.CfeFamily.Value) : null,
            view.FormatVersion,
            view.FiscalDocument is null
                ? null
                : new SaleFiscalDocumentIdentityDto(
                    view.FiscalDocument.Id.ToString(),
                    (int)view.FiscalDocument.CfeType,
                    ToApiEnum(view.FiscalDocument.CfeType),
                    view.FiscalDocument.Series,
                    view.FiscalDocument.Number,
                    view.FiscalDocument.FiscalDate,
                    view.FiscalDocument.IdentityCreatedAtUtc),
            StatusAuthority);

    private static string ToApiEnum<T>(T value) where T : struct, Enum =>
        ToSnake(value.ToString()).ToUpperInvariant();

    private static string ToSnake(string value)
    {
        var chars = new List<char>(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];
            if (index > 0 && char.IsUpper(current) && !char.IsUpper(value[index - 1]))
                chars.Add('_');
            chars.Add(current);
        }
        return new string(chars.ToArray());
    }
}
