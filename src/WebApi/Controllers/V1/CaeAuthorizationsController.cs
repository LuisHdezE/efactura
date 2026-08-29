using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Security;
using EFactura.Application.Fiscal;
using EFactura.Domain.Fiscal;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1.Contracts;
using WebApi.CrossCutting.Authorization;
using WebApi.CrossCutting.Requests;

namespace WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/cae-authorizations")]
public sealed class CaeAuthorizationsController : ControllerBase
{
    private readonly V1OrganizationContextResolver _organization;
    private readonly ListCaeAuthorizationsUseCase _list;
    private readonly GetCaeAuthorizationUseCase _get;
    private readonly ImportCaeAuthorizationUseCase _import;
    private readonly ActivateCaeAuthorizationUseCase _activate;
    private readonly ListCaeAllocationsUseCase _listAllocations;
    private readonly CreateCaeAllocationUseCase _createAllocation;
    private readonly CloseCaeAllocationUseCase _closeAllocation;

    public CaeAuthorizationsController(
        V1OrganizationContextResolver organization,
        ListCaeAuthorizationsUseCase list,
        GetCaeAuthorizationUseCase get,
        ImportCaeAuthorizationUseCase import,
        ActivateCaeAuthorizationUseCase activate,
        ListCaeAllocationsUseCase listAllocations,
        CreateCaeAllocationUseCase createAllocation,
        CloseCaeAllocationUseCase closeAllocation)
    {
        _organization = organization;
        _list = list;
        _get = get;
        _import = import;
        _activate = activate;
        _listAllocations = listAllocations;
        _createAllocation = createAllocation;
        _closeAllocation = closeAllocation;
    }

    [HttpGet]
    [RequirePermission(Permissions.FiscalRead)]
    public async Task<ActionResult<PageResponse<CaeAuthorizationDto>>> List(
        [FromQuery] int? cfeType = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        EnsurePagination(page, pageSize);
        var organizationId = _organization.Resolve(Request);
        var parsedType = cfeType.HasValue ? ParseCfeType(cfeType.Value) : null;
        var result = await _list.ExecuteAsync(
            organizationId, parsedType, null, page, pageSize, cancellationToken);
        return Ok(new PageResponse<CaeAuthorizationDto>(
            result.Items.Select(x => MapAuthorization(x, false)).ToArray(),
            result.Page, result.PageSize, result.Total));
    }

    [HttpGet("{caeId:guid}")]
    [RequirePermission(Permissions.FiscalRead)]
    public async Task<ActionResult<CaeAuthorizationDto>> Get(
        Guid caeId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        return Ok(MapAuthorization(
            await _get.ExecuteAsync(organizationId, caeId, cancellationToken), false));
    }

    [HttpPost("import")]
    [RequirePermission(Permissions.FiscalManageCae)]
    public async Task<ActionResult<CaeAuthorizationDto>> Import(
        [FromBody] CaeImportRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _import.ExecuteAsync(
            new ImportCaeAuthorizationCommand(
                organizationId,
                ParseCfeType(request.CfeType),
                request.AuthorizationNumber,
                request.Series,
                request.RangeFrom,
                request.RangeTo,
                request.ValidFrom,
                request.ValidTo,
                request.SourceArtifactId,
                request.SourceArtifactHash,
                request.SourceName,
                request.SourceReference,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);
        SetReplayHeader(result.Replayed);
        return Ok(MapAuthorization(result.Authorization, result.Replayed));
    }

    [HttpPost("{caeId:guid}/activate")]
    [RequirePermission(Permissions.FiscalManageCae)]
    public async Task<ActionResult<CaeAuthorizationDto>> Activate(
        Guid caeId,
        [FromBody] CaeActivateRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _activate.ExecuteAsync(
            new ActivateCaeAuthorizationCommand(
                organizationId, caeId, request.ExpectedVersion,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);
        SetReplayHeader(result.Replayed);
        return Ok(MapAuthorization(result.Authorization, result.Replayed));
    }

    [HttpGet("{caeId:guid}/allocations")]
    [RequirePermission(Permissions.FiscalRead)]
    public async Task<ActionResult<PageResponse<CaeAllocationDto>>> ListAllocations(
        Guid caeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        EnsurePagination(page, pageSize);
        var organizationId = _organization.Resolve(Request);
        var result = await _listAllocations.ExecuteAsync(
            organizationId, caeId, page, pageSize, cancellationToken);
        return Ok(new PageResponse<CaeAllocationDto>(
            result.Items.Select(x => MapAllocation(x, false)).ToArray(),
            result.Page, result.PageSize, result.Total));
    }

    [HttpPost("{caeId:guid}/allocations")]
    [RequirePermission(Permissions.FiscalManageCae)]
    public async Task<ActionResult<CaeAllocationDto>> CreateAllocation(
        Guid caeId,
        [FromBody] CaeAllocationCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _createAllocation.ExecuteAsync(
            new CreateCaeAllocationCommand(
                organizationId, caeId, request.LocationId, request.TerminalId,
                request.RangeFrom, request.RangeTo, request.ExpectedVersion,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);
        SetReplayHeader(result.Replayed);
        return Ok(MapAllocation(result.Allocation, result.Replayed));
    }

    [HttpPost("{caeId:guid}/allocations/{allocationId:guid}/close")]
    [RequirePermission(Permissions.FiscalManageCae)]
    public async Task<ActionResult<CaeAllocationDto>> CloseAllocation(
        Guid caeId,
        Guid allocationId,
        [FromBody] CaeAllocationCloseRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _closeAllocation.ExecuteAsync(
            new CloseCaeAllocationCommand(
                organizationId, caeId, allocationId, request.ExpectedVersion,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);
        SetReplayHeader(result.Replayed);
        return Ok(MapAllocation(result.Allocation, result.Replayed));
    }

    private void SetReplayHeader(bool replayed)
    {
        if (replayed)
            Response.Headers["Idempotent-Replayed"] = "true";
    }

    private static CaeAuthorizationDto MapAuthorization(CaeAuthorization authorization, bool replayed)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var effective = authorization.EffectiveStatus(today);
        var alert = effective switch
        {
            CaeAuthorizationStatus.Expired => "cae.expired",
            CaeAuthorizationStatus.Exhausted => "cae.exhausted",
            _ when authorization.NextNumber > authorization.RangeTo => "cae.direct_range_exhausted",
            _ => null
        };
        return new CaeAuthorizationDto(
            authorization.Id.ToString(), authorization.Version, (int)authorization.CfeType,
            authorization.AuthorizationNumber, authorization.Series,
            authorization.RangeFrom, authorization.RangeTo,
            authorization.ValidFrom, authorization.ValidTo,
            effective.ToString().ToUpperInvariant(), authorization.VerificationMethod,
            authorization.SourceArtifactId, authorization.SourceArtifactHash,
            authorization.SourceName, authorization.SourceReference,
            authorization.ImportedAtUtc, authorization.ActivatedAtUtc, alert, replayed);
    }

    private static CaeAllocationDto MapAllocation(CaeAllocation allocation, bool replayed) => new(
        allocation.Id.ToString(), allocation.CaeAuthorizationId.ToString(), allocation.Version,
        allocation.LocationId, allocation.TerminalId, allocation.RangeFrom, allocation.RangeTo,
        allocation.Status.ToString().ToUpperInvariant(), allocation.CreatedAtUtc, allocation.ClosedAtUtc,
        replayed);

    private static CfeFamily ParseCfeType(int value)
    {
        var parsed = (CfeFamily)value;
        if (!Enum.IsDefined(parsed))
            throw new ApplicationProblemException(
                ApplicationProblemKind.Validation, "cae.unsupported_cfe_type",
                "The requested CFE type is not enabled in this API release.");
        return parsed;
    }

    private static void EnsurePagination(int page, int pageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > 200)
            throw new ApplicationProblemException(
                ApplicationProblemKind.Validation, "pagination.invalid",
                "Page must be >= 1 and pageSize must be between 1 and 200.");
    }
}
