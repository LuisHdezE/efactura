using EFactura.Application.Catalog;
using EFactura.Application.Common.Security;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1.Contracts;
using WebApi.CrossCutting.Authorization;
using WebApi.CrossCutting.Requests;

namespace WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/item-categories")]
public sealed class ItemCategoriesController : ControllerBase
{
    private readonly V1OrganizationContextResolver _organization;
    private readonly ListItemCategoriesUseCase _list;
    private readonly GetItemCategoryUseCase _get;
    private readonly CreateItemCategoryUseCase _create;
    private readonly UpdateItemCategoryUseCase _update;

    public ItemCategoriesController(
        V1OrganizationContextResolver organization,
        ListItemCategoriesUseCase list,
        GetItemCategoryUseCase get,
        CreateItemCategoryUseCase create,
        UpdateItemCategoryUseCase update)
    {
        _organization = organization;
        _list = list;
        _get = get;
        _create = create;
        _update = update;
    }

    [HttpGet]
    [RequirePermission(Permissions.CatalogRead)]
    public async Task<ActionResult<PageResponse<ItemCategoryDto>>> List(
        [FromQuery] string? search = null,
        [FromQuery] bool? active = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _list.ExecuteAsync(
            new ItemCategorySearchRequest(organizationId, search, active, page, pageSize),
            cancellationToken);

        return Ok(new PageResponse<ItemCategoryDto>(
            result.Items.Select(Map).ToArray(),
            result.Page,
            result.PageSize,
            result.Total));
    }

    [HttpGet("{categoryId:guid}")]
    [RequirePermission(Permissions.CatalogRead)]
    public async Task<ActionResult<ItemCategoryDto>> Get(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        return Ok(Map(await _get.ExecuteAsync(organizationId, categoryId, cancellationToken)));
    }

    [HttpPost]
    [RequirePermission(Permissions.CatalogManage)]
    public async Task<ActionResult<ItemCategoryDto>> Create(
        [FromBody] ItemCategoryCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _create.ExecuteAsync(
            new CreateItemCategoryCommand(
                organizationId,
                request.Code,
                request.Name,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);

        SetReplayHeader(result.Replayed);
        var resource = Map(await _get.ExecuteAsync(organizationId, result.ResourceId, cancellationToken));
        return CreatedAtAction(nameof(Get), new { categoryId = result.ResourceId }, resource);
    }

    [HttpPatch("{categoryId:guid}")]
    [RequirePermission(Permissions.CatalogManage)]
    public async Task<ActionResult<ItemCategoryDto>> Update(
        Guid categoryId,
        [FromBody] ItemCategoryUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _update.ExecuteAsync(
            new UpdateItemCategoryCommand(
                organizationId,
                categoryId,
                request.Code,
                request.Name,
                request.Active,
                request.ExpectedVersion,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);

        SetReplayHeader(result.Replayed);
        return Ok(Map(await _get.ExecuteAsync(organizationId, categoryId, cancellationToken)));
    }

    private void SetReplayHeader(bool replayed)
    {
        if (replayed)
        {
            Response.Headers["Idempotent-Replayed"] = "true";
        }
    }

    private static ItemCategoryDto Map(ItemCategoryView category) =>
        new(
            category.Id.ToString(),
            category.Version,
            category.Active,
            category.Code,
            category.Name);
}
