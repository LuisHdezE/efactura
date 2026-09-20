using EFactura.Application.Common.Security;
using EFactura.Application.IdentityAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1.Contracts;
using WebApi.CrossCutting.Authorization;
using WebApi.CrossCutting.Requests;

namespace WebApi.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1/roles")]
public sealed class RolesController : ControllerBase
{
    private readonly V1OrganizationContextResolver _organization;
    private readonly ListRolesUseCase _list;
    private readonly GetRoleUseCase _get;
    private readonly CreateRoleUseCase _create;
    private readonly UpdateRoleUseCase _update;

    public RolesController(
        V1OrganizationContextResolver organization,
        ListRolesUseCase list,
        GetRoleUseCase get,
        CreateRoleUseCase create,
        UpdateRoleUseCase update)
    {
        _organization = organization;
        _list = list;
        _get = get;
        _create = create;
        _update = update;
    }

    [HttpGet(Name = "listRoles")]
    [RequirePermission(Permissions.SecurityRolesRead)]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> List(CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var roles = await _list.ExecuteAsync(organizationId, cancellationToken);
        return Ok(roles.Select(Map).ToArray());
    }

    [HttpGet("{roleId}", Name = "getRole")]
    [RequirePermission(Permissions.SecurityRolesRead)]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDto>> Get(string roleId, CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        return Ok(Map(await _get.ExecuteAsync(organizationId, roleId, cancellationToken)));
    }

    [HttpPost(Name = "createRole")]
    [RequirePermission(Permissions.SecurityManageRoles)]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleDto>> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _create.ExecuteAsync(
            new CreateRoleCommand(
                organizationId,
                request.Name,
                request.Description,
                request.Permissions,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);

        SetReplayHeader(result.Replayed);
        return StatusCode(StatusCodes.Status201Created, Map(result.Role));
    }

    [HttpPut("{roleId}", Name = "updateRole")]
    [RequirePermission(Permissions.SecurityManageRoles)]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleDto>> Update(
        string roleId,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _update.ExecuteAsync(
            new UpdateRoleCommand(
                organizationId,
                roleId,
                request.Name,
                request.Description,
                request.Active,
                request.Permissions,
                request.ExpectedVersion,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);

        SetReplayHeader(result.Replayed);
        return Ok(Map(result.Role));
    }

    private void SetReplayHeader(bool replayed)
    {
        if (replayed)
            Response.Headers["Idempotent-Replayed"] = "true";
    }

    private static RoleDto Map(RoleView role) =>
        new(role.Id, role.Name, role.Description, role.Active, role.Version, role.Permissions);
}
