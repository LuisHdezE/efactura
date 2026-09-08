using EFactura.Application.Common.Security;
using EFactura.Application.Organizations;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1.Contracts;
using WebApi.CrossCutting.Authorization;
using WebApi.CrossCutting.Requests;

namespace WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/locations")]
public sealed class LocationsController : ControllerBase
{
    private readonly V1OrganizationContextResolver _organization;
    private readonly ListFiscalLocationsUseCase _list;
    private readonly GetFiscalLocationUseCase _get;
    private readonly CreateFiscalLocationUseCase _create;
    private readonly UpdateFiscalLocationUseCase _update;

    public LocationsController(V1OrganizationContextResolver organization, ListFiscalLocationsUseCase list, GetFiscalLocationUseCase get, CreateFiscalLocationUseCase create, UpdateFiscalLocationUseCase update)
    { _organization = organization; _list = list; _get = get; _create = create; _update = update; }

    [HttpGet]
    [RequirePermission(Permissions.OrganizationRead)]
    public async Task<ActionResult<IReadOnlyCollection<FiscalLocationDto>>> List([FromQuery] bool? active = true, CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        return Ok((await _list.ExecuteAsync(organizationId, active, cancellationToken)).Select(Map).ToArray());
    }

    [HttpGet("{locationId}")]
    [RequirePermission(Permissions.OrganizationRead)]
    public async Task<ActionResult<FiscalLocationDto>> Get(string locationId, CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        return Ok(Map(await _get.ExecuteAsync(organizationId, locationId, cancellationToken)));
    }

    [HttpPost]
    [RequirePermission(Permissions.OrganizationManage)]
    public async Task<ActionResult<FiscalLocationDto>> Create([FromBody] FiscalLocationCreateRequest request, CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _create.ExecuteAsync(new CreateFiscalLocationCommand(organizationId, request.Name, request.DgiBranchCode, request.FiscalAddress, request.City, request.Department, V1RequestContract.RequireIdempotencyKey(Request), V1RequestContract.ComputeRequestHash(request)), cancellationToken);
        if (result.Replayed) Response.Headers["Idempotent-Replayed"] = "true";
        var resource = Map(await _get.ExecuteAsync(organizationId, result.ResourceId, cancellationToken));
        return CreatedAtAction(nameof(Get), new { locationId = resource.Id }, resource);
    }

    [HttpPatch("{locationId}")]
    [RequirePermission(Permissions.OrganizationManage)]
    public async Task<ActionResult<FiscalLocationDto>> Update(string locationId, [FromBody] FiscalLocationUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _update.ExecuteAsync(new UpdateFiscalLocationCommand(organizationId, locationId, request.Name, request.DgiBranchCode, request.FiscalAddress, request.City, request.Department, request.Active, request.ExpectedVersion, V1RequestContract.RequireIdempotencyKey(Request), V1RequestContract.ComputeRequestHash(request)), cancellationToken);
        if (result.Replayed) Response.Headers["Idempotent-Replayed"] = "true";
        return Ok(Map(await _get.ExecuteAsync(organizationId, locationId, cancellationToken)));
    }

    private static FiscalLocationDto Map(FiscalLocationView location) => new(location.Id, location.OrganizationId, location.Name, location.DgiBranchCode, location.FiscalAddress, location.City, location.Department, location.Active, location.Version);
}
