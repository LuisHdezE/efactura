using EFactura.Application.Common.Security;
using EFactura.Application.Organizations;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1.Contracts;
using WebApi.CrossCutting.Authorization;
using WebApi.CrossCutting.Requests;

namespace WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/company")]
public sealed class CompanyController : ControllerBase
{
    private readonly V1OrganizationContextResolver _organization;
    private readonly GetCurrentCompanyUseCase _get;
    private readonly UpsertCompanyFiscalProfileUseCase _upsert;

    public CompanyController(V1OrganizationContextResolver organization, GetCurrentCompanyUseCase get, UpsertCompanyFiscalProfileUseCase upsert)
    { _organization = organization; _get = get; _upsert = upsert; }

    [HttpGet]
    [RequirePermission(Permissions.OrganizationRead)]
    public async Task<ActionResult<CompanyFiscalProfileDto>> Get(CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        return Ok(Map(await _get.ExecuteAsync(organizationId, cancellationToken)));
    }

    [HttpPatch]
    [RequirePermission(Permissions.OrganizationManage)]
    public async Task<ActionResult<CompanyFiscalProfileDto>> Update([FromBody] CompanyFiscalProfileUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _upsert.ExecuteAsync(new UpsertCompanyFiscalProfileCommand(organizationId, request.Ruc, request.LegalName, request.CommercialName, request.ExpectedVersion, V1RequestContract.RequireIdempotencyKey(Request), V1RequestContract.ComputeRequestHash(request)), cancellationToken);
        if (result.Replayed) Response.Headers["Idempotent-Replayed"] = "true";
        return Ok(Map(await _get.ExecuteAsync(organizationId, cancellationToken)));
    }

    private static CompanyFiscalProfileDto Map(CompanyFiscalProfileView company) => new(company.OrganizationId, company.Ruc, company.LegalName, company.CommercialName, company.Version);
}
