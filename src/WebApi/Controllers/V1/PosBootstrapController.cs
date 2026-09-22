using EFactura.Application.Common.Security;
using EFactura.Application.Sales;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1.Contracts;
using WebApi.CrossCutting.Authorization;
using WebApi.CrossCutting.Requests;

namespace WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/pos/bootstrap")]
public sealed class PosBootstrapController : ControllerBase
{
    private readonly V1OrganizationContextResolver _organization;
    private readonly GetPosBootstrapUseCase _get;

    public PosBootstrapController(
        V1OrganizationContextResolver organization,
        GetPosBootstrapUseCase get)
    {
        _organization = organization;
        _get = get;
    }

    [HttpGet(Name = "getPosBootstrap")]
    [RequirePermission(Permissions.SalesRead)]
    public async Task<ActionResult<PosBootstrapDto>> Get(CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var view = await _get.ExecuteAsync(organizationId, cancellationToken);

        Response.Headers.CacheControl = "private, no-cache";
        Response.Headers.ETag = view.ProjectionTag;

        if (MatchesIfNoneMatch(Request.Headers["If-None-Match"], view.ProjectionTag))
            return StatusCode(StatusCodes.Status304NotModified);

        return Ok(Map(view));
    }

    private static bool MatchesIfNoneMatch(
        Microsoft.Extensions.Primitives.StringValues values,
        string projectionTag)
    {
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            foreach (var candidate in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (candidate == "*" || string.Equals(candidate, projectionTag, StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }

    private static PosBootstrapDto Map(PosBootstrapView view) =>
        new(
            view.OrganizationId,
            view.GeneratedAtUtc,
            view.Contexts
                .Select(context => new PosBootstrapContextDto(
                    context.LocationId,
                    context.LocationName,
                    context.DgiBranchCode,
                    context.LocationVersion,
                    context.Terminals
                        .Select(terminal => new PosBootstrapTerminalDto(
                            terminal.TerminalId,
                            terminal.Code,
                            terminal.Name,
                            terminal.TerminalVersion))
                        .ToArray()))
                .ToArray());
}
