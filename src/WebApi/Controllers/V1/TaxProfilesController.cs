using EFactura.Application.Common.Security;
using EFactura.Application.Taxation;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1.Contracts;
using WebApi.CrossCutting.Authorization;
using WebApi.CrossCutting.Requests;

namespace WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/tax-profiles")]
public sealed class TaxProfilesController : ControllerBase
{
    private readonly V1OrganizationContextResolver _organization;
    private readonly ListTaxProfilesUseCase _list;

    public TaxProfilesController(
        V1OrganizationContextResolver organization,
        ListTaxProfilesUseCase list)
    {
        _organization = organization;
        _list = list;
    }

    [HttpGet]
    [RequirePermission(Permissions.CatalogRead)]
    public async Task<ActionResult<PageResponse<TaxProfileDto>>> List(
        [FromQuery] DateOnly? onDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var effectiveDate = onDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var result = await _list.ExecuteAsync(
            new TaxProfileSearchRequest(organizationId, effectiveDate, page, pageSize),
            cancellationToken);

        return Ok(new PageResponse<TaxProfileDto>(
            result.Items.Select(Map).ToArray(),
            result.Page,
            result.PageSize,
            result.Total));
    }

    private static TaxProfileDto Map(TaxProfileView profile) =>
        new(
            profile.Id.ToString(),
            profile.Code,
            profile.Name,
            profile.Treatment.ToString().ToUpperInvariant(),
            profile.RatePercent,
            profile.CfeBillingIndicator,
            profile.EffectiveFrom,
            profile.EffectiveTo,
            profile.RuleVersion,
            profile.SourceAuthority,
            profile.SourceReference,
            profile.SourceUri,
            profile.CfeSpecificationVersion,
            profile.VerifiedAt,
            profile.SystemProfile);
}
