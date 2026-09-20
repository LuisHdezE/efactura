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
[Route("api/v1/users")]
public sealed class UsersController : ControllerBase
{
    private readonly V1OrganizationContextResolver _organization;
    private readonly ListUsersUseCase _list;
    private readonly GetUserUseCase _get;
    private readonly CreateUserUseCase _create;
    private readonly UpdateUserUseCase _update;
    private readonly AssignUserRolesUseCase _assignRoles;

    public UsersController(
        V1OrganizationContextResolver organization,
        ListUsersUseCase list,
        GetUserUseCase get,
        CreateUserUseCase create,
        UpdateUserUseCase update,
        AssignUserRolesUseCase assignRoles)
    {
        _organization = organization;
        _list = list;
        _get = get;
        _create = create;
        _update = update;
        _assignRoles = assignRoles;
    }

    [HttpGet(Name = "listUsers")]
    [RequirePermission(Permissions.SecurityUsersRead)]
    [ProducesResponseType(typeof(IReadOnlyList<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> List(CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var users = await _list.ExecuteAsync(organizationId, cancellationToken);
        return Ok(users.Select(Map).ToArray());
    }

    [HttpGet("{userId}", Name = "getUser")]
    [RequirePermission(Permissions.SecurityUsersRead)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> Get(string userId, CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        return Ok(Map(await _get.ExecuteAsync(organizationId, userId, cancellationToken)));
    }

    [HttpPost(Name = "createUser")]
    [RequirePermission(Permissions.SecurityUsersManage)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<UserDto>> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _create.ExecuteAsync(
            new CreateUserCommand(
                organizationId,
                request.IdentityProvider,
                request.ExternalSubject,
                request.DisplayName,
                request.Email,
                request.LocationScopes,
                request.TerminalScopes,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);

        SetReplayHeader(result.Replayed);
        return StatusCode(StatusCodes.Status201Created, Map(result.User));
    }

    [HttpPatch("{userId}", Name = "updateUser")]
    [RequirePermission(Permissions.SecurityUsersManage)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<UserDto>> Update(
        string userId,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _update.ExecuteAsync(
            new UpdateUserCommand(
                organizationId,
                userId,
                request.DisplayName,
                request.Email,
                request.Active,
                request.LocationScopes,
                request.TerminalScopes,
                request.ExpectedVersion,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);

        SetReplayHeader(result.Replayed);
        return Ok(Map(result.User));
    }

    [HttpPut("{userId}/roles", Name = "assignUserRoles")]
    [RequirePermission(Permissions.SecurityManageRoles)]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<UserDto>> AssignRoles(
        string userId,
        [FromBody] AssignUserRolesRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _assignRoles.ExecuteAsync(
            new AssignUserRolesCommand(
                organizationId,
                userId,
                request.RoleIds,
                request.ExpectedVersion,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);

        SetReplayHeader(result.Replayed);
        return Ok(Map(result.User));
    }

    private void SetReplayHeader(bool replayed)
    {
        if (replayed)
            Response.Headers["Idempotent-Replayed"] = "true";
    }

    private static UserDto Map(UserView user) =>
        new(
            user.Id,
            user.IdentityProvider,
            user.ExternalSubject,
            user.DisplayName,
            user.Email,
            user.Active,
            user.Version,
            user.LocationScopes,
            user.TerminalScopes,
            user.RoleIds);
}
