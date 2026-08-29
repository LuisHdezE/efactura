using EFactura.Application.Catalog;
using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Security;
using EFactura.Domain.Catalog;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1.Contracts;
using WebApi.CrossCutting.Authorization;
using WebApi.CrossCutting.Requests;

namespace WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/items")]
public sealed class ItemsController : ControllerBase
{
    private readonly V1OrganizationContextResolver _organization;
    private readonly ListCommercialItemsUseCase _list;
    private readonly GetCommercialItemUseCase _get;
    private readonly CreateCommercialItemUseCase _create;
    private readonly UpdateCommercialItemUseCase _update;
    private readonly DeactivateCommercialItemUseCase _deactivate;

    public ItemsController(
        V1OrganizationContextResolver organization,
        ListCommercialItemsUseCase list,
        GetCommercialItemUseCase get,
        CreateCommercialItemUseCase create,
        UpdateCommercialItemUseCase update,
        DeactivateCommercialItemUseCase deactivate)
    {
        _organization = organization;
        _list = list;
        _get = get;
        _create = create;
        _update = update;
        _deactivate = deactivate;
    }

    [HttpGet]
    [RequirePermission(Permissions.CatalogRead)]
    public async Task<ActionResult<PageResponse<CommercialItemDto>>> List(
        [FromQuery] string? search = null,
        [FromQuery] string? kind = null,
        [FromQuery] bool? active = true,
        [FromQuery] bool? trackInventory = null,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        CommercialItemKind? parsedKind = string.IsNullOrWhiteSpace(kind) ? null : ParseKind(kind);
        var result = await _list.ExecuteAsync(
            new CommercialItemSearchRequest(
                organizationId,
                search,
                parsedKind,
                active,
                trackInventory,
                categoryId,
                page,
                pageSize),
            cancellationToken);

        return Ok(new PageResponse<CommercialItemDto>(
            result.Items.Select(Map).ToArray(),
            result.Page,
            result.PageSize,
            result.Total));
    }

    [HttpGet("{itemId:guid}")]
    [RequirePermission(Permissions.CatalogRead)]
    public async Task<ActionResult<CommercialItemDto>> Get(Guid itemId, CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        return Ok(Map(await _get.ExecuteAsync(organizationId, itemId, cancellationToken)));
    }

    [HttpPost]
    [RequirePermission(Permissions.CatalogManage)]
    public async Task<ActionResult<CommercialItemDto>> Create(
        [FromBody] CommercialItemCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _create.ExecuteAsync(
            new CreateCommercialItemCommand(
                organizationId,
                request.Code,
                request.Name,
                request.Description,
                ParseKind(request.Kind),
                request.Unit,
                request.TrackInventory,
                ParseOptionalGuid(request.TaxProfileId, "catalog.invalid_tax_profile_id"),
                ParseOptionalGuid(request.CategoryId, "catalog.invalid_category_id"),
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);

        SetReplayHeader(result.Replayed);
        var resource = Map(await _get.ExecuteAsync(organizationId, result.ItemId, cancellationToken));
        return CreatedAtAction(nameof(Get), new { itemId = result.ItemId }, resource);
    }

    [HttpPatch("{itemId:guid}")]
    [RequirePermission(Permissions.CatalogManage)]
    public async Task<ActionResult<CommercialItemDto>> Update(
        Guid itemId,
        [FromBody] CommercialItemUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var taxProfileId = ParseOptionalGuid(request.TaxProfileId, "catalog.invalid_tax_profile_id");
        var categoryId = ParseOptionalGuid(request.CategoryId, "catalog.invalid_category_id");
        var result = await _update.ExecuteAsync(
            new UpdateCommercialItemCommand(
                organizationId,
                itemId,
                request.Code,
                request.Name,
                request.Description,
                string.IsNullOrWhiteSpace(request.Kind) ? null : ParseKind(request.Kind),
                request.Unit,
                request.TrackInventory,
                taxProfileId,
                !string.IsNullOrWhiteSpace(request.TaxProfileId),
                categoryId,
                !string.IsNullOrWhiteSpace(request.CategoryId),
                request.ExpectedVersion,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);

        SetReplayHeader(result.Replayed);
        return Ok(Map(await _get.ExecuteAsync(organizationId, itemId, cancellationToken)));
    }

    [HttpPost("{itemId:guid}/deactivate")]
    [RequirePermission(Permissions.CatalogManage)]
    public async Task<ActionResult<CommercialItemDto>> Deactivate(
        Guid itemId,
        [FromBody] CommercialItemDeactivateRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _deactivate.ExecuteAsync(
            new DeactivateCommercialItemCommand(
                organizationId,
                itemId,
                request.ExpectedVersion,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);

        SetReplayHeader(result.Replayed);
        return Ok(Map(await _get.ExecuteAsync(organizationId, itemId, cancellationToken)));
    }

    private void SetReplayHeader(bool replayed)
    {
        if (replayed)
        {
            Response.Headers["Idempotent-Replayed"] = "true";
        }
    }

    private static CommercialItemKind ParseKind(string value) =>
        Enum.TryParse<CommercialItemKind>(value, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw Validation("catalog.invalid_kind", "Item kind must be PRODUCT or SERVICE.");

    private static Guid? ParseOptionalGuid(string? value, string code)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Guid.TryParse(value, out var parsed)
            ? parsed
            : throw Validation(code, "The supplied identifier is not valid.");
    }

    private static CommercialItemDto Map(CommercialItemView item) =>
        new(
            item.Id.ToString(),
            item.Version,
            item.Active,
            item.Code,
            item.Name,
            item.Description,
            item.Kind.ToString().ToUpperInvariant(),
            item.Unit,
            item.TrackInventory,
            item.TaxProfileId?.ToString(),
            item.CategoryId?.ToString());

    private static ApplicationProblemException Validation(string code, string detail) =>
        new(ApplicationProblemKind.Validation, code, detail);
}
