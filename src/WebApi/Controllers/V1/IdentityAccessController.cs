using EFactura.Application.Common.Security;
using EFactura.Application.IdentityAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1.Contracts;
using WebApi.CrossCutting.Authorization;

namespace WebApi.Controllers.V1;

[ApiController]
[Authorize]
[Route("api/v1")]
public sealed class IdentityAccessController : ControllerBase
{
    private readonly GetCurrentActorUseCase _getCurrentActor;
    private readonly ListPermissionsUseCase _listPermissions;

    public IdentityAccessController(
        GetCurrentActorUseCase getCurrentActor,
        ListPermissionsUseCase listPermissions)
    {
        _getCurrentActor = getCurrentActor;
        _listPermissions = listPermissions;
    }

    [HttpGet("me", Name = "getCurrentActor")]
    [ProducesResponseType(typeof(CurrentActorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<CurrentActorDto> GetCurrentActor()
    {
        var actor = _getCurrentActor.Execute();
        return Ok(new CurrentActorDto(
            actor.ActorId,
            actor.DisplayName,
            actor.Permissions,
            actor.CompanyScopes,
            actor.LocationScopes,
            actor.TerminalScopes));
    }

    [HttpGet("permissions", Name = "listPermissions")]
    [RequirePermission(Permissions.SecurityRolesRead)]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public ActionResult<IReadOnlyList<PermissionDto>> ListPermissions()
    {
        var permissions = _listPermissions.Execute();
        return Ok(permissions.Select(permission => new PermissionDto(permission)).ToArray());
    }
}
