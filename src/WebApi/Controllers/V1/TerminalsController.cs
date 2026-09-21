using EFactura.Application.Common.Security;
using EFactura.Application.Organizations;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1.Contracts;
using WebApi.CrossCutting.Authorization;
using WebApi.CrossCutting.Requests;

namespace WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/terminals")]
public sealed class TerminalsController : ControllerBase
{
    private readonly V1OrganizationContextResolver _organization;
    private readonly ListTerminalsUseCase _list;
    private readonly GetTerminalUseCase _get;
    private readonly RegisterTerminalUseCase _register;
    private readonly UpdateTerminalUseCase _update;

    public TerminalsController(
        V1OrganizationContextResolver organization,
        ListTerminalsUseCase list,
        GetTerminalUseCase get,
        RegisterTerminalUseCase register,
        UpdateTerminalUseCase update)
    {
        _organization = organization;
        _list = list;
        _get = get;
        _register = register;
        _update = update;
    }

    [HttpGet(Name = "listTerminals")]
    [RequirePermission(Permissions.OrganizationRead)]
    public async Task<ActionResult<IReadOnlyCollection<TerminalDto>>> List(
        [FromQuery] bool? active = true,
        [FromQuery] string? locationId = null,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var terminals = await _list.ExecuteAsync(organizationId, active, locationId, cancellationToken);
        return Ok(terminals.Select(Map).ToArray());
    }

    [HttpGet("{terminalId}", Name = "getTerminal")]
    [RequirePermission(Permissions.OrganizationRead)]
    public async Task<ActionResult<TerminalDto>> Get(
        string terminalId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        return Ok(Map(await _get.ExecuteAsync(organizationId, terminalId, cancellationToken)));
    }

    [HttpPost(Name = "registerTerminal")]
    [RequirePermission(Permissions.OrganizationManage)]
    public async Task<ActionResult<TerminalDto>> Register(
        [FromBody] TerminalCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var canonicalRequest = new TerminalCreateRequest(
            NormalizeCodeForHash(request.Code),
            NormalizeTextForHash(request.Name),
            NormalizeTextForHash(request.LocationId));

        var result = await _register.ExecuteAsync(
            new RegisterTerminalCommand(
                organizationId,
                request.Code,
                request.Name,
                request.LocationId,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(canonicalRequest)),
            cancellationToken);

        if (result.Replayed)
            Response.Headers["Idempotent-Replayed"] = "true";

        var resource = Map(await _get.ExecuteAsync(organizationId, result.ResourceId, cancellationToken));
        return CreatedAtAction(nameof(Get), new { terminalId = resource.Id }, resource);
    }

    [HttpPatch("{terminalId}", Name = "updateTerminal")]
    [RequirePermission(Permissions.OrganizationManage)]
    public async Task<ActionResult<TerminalDto>> Update(
        string terminalId,
        [FromBody] TerminalUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var canonicalRequest = new TerminalUpdateRequest(
            NormalizeTextForHash(request.Name),
            NormalizeTextForHash(request.LocationId),
            request.Active,
            request.ExpectedVersion);

        var result = await _update.ExecuteAsync(
            new UpdateTerminalCommand(
                organizationId,
                terminalId,
                request.Name,
                request.LocationId,
                request.Active,
                request.ExpectedVersion,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(canonicalRequest)),
            cancellationToken);

        if (result.Replayed)
            Response.Headers["Idempotent-Replayed"] = "true";

        return Ok(Map(await _get.ExecuteAsync(organizationId, terminalId, cancellationToken)));
    }

    private static TerminalDto Map(TerminalView terminal) =>
        new(
            terminal.Id,
            terminal.OrganizationId,
            terminal.Code,
            terminal.Name,
            terminal.LocationId,
            terminal.Active,
            terminal.Version);

    private static string NormalizeCodeForHash(string? value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();

    private static string NormalizeTextForHash(string? value) =>
        (value ?? string.Empty).Trim();
}
