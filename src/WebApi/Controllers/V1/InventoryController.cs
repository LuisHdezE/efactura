using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Security;
using EFactura.Application.Inventory;
using EFactura.Domain.Inventory;
using Microsoft.AspNetCore.Mvc;
using WebApi.Controllers.V1.Contracts;
using WebApi.CrossCutting.Authorization;
using WebApi.CrossCutting.Requests;

namespace WebApi.Controllers.V1;

[ApiController]
[Route("api/v1/inventory")]
public sealed class InventoryController : ControllerBase
{
    private readonly V1OrganizationContextResolver _organization;
    private readonly ListInventoryPositionsUseCase _listPositions;
    private readonly GetInventoryPositionUseCase _getPosition;
    private readonly ListStockMovementsUseCase _listMovements;
    private readonly CreateStockAdjustmentUseCase _adjust;

    public InventoryController(
        V1OrganizationContextResolver organization,
        ListInventoryPositionsUseCase listPositions,
        GetInventoryPositionUseCase getPosition,
        ListStockMovementsUseCase listMovements,
        CreateStockAdjustmentUseCase adjust)
    {
        _organization = organization;
        _listPositions = listPositions;
        _getPosition = getPosition;
        _listMovements = listMovements;
        _adjust = adjust;
    }

    [HttpGet("positions")]
    [RequirePermission(Permissions.InventoryRead)]
    public async Task<ActionResult<PageResponse<InventoryPositionDto>>> ListPositions(
        [FromQuery] Guid? itemId = null,
        [FromQuery] string? locationId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        EnsurePagination(page, pageSize);
        var organizationId = _organization.Resolve(Request);
        var result = await _listPositions.ExecuteAsync(
            organizationId, itemId, locationId, page, pageSize, cancellationToken);

        return Ok(new PageResponse<InventoryPositionDto>(
            result.Items.Select(MapPosition).ToArray(),
            result.Page,
            result.PageSize,
            result.Total));
    }

    [HttpGet("positions/{positionId:guid}")]
    [RequirePermission(Permissions.InventoryRead)]
    public async Task<ActionResult<InventoryPositionDto>> GetPosition(
        Guid positionId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        return Ok(MapPosition(await _getPosition.ExecuteAsync(organizationId, positionId, cancellationToken)));
    }

    [HttpGet("movements")]
    [RequirePermission(Permissions.InventoryRead)]
    public async Task<ActionResult<PageResponse<StockMovementDto>>> ListMovements(
        [FromQuery] Guid? itemId = null,
        [FromQuery] string? locationId = null,
        [FromQuery] Guid? positionId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        EnsurePagination(page, pageSize);
        var organizationId = _organization.Resolve(Request);
        var result = await _listMovements.ExecuteAsync(
            organizationId, itemId, locationId, positionId, page, pageSize, cancellationToken);

        return Ok(new PageResponse<StockMovementDto>(
            result.Items.Select(MapMovement).ToArray(),
            result.Page,
            result.PageSize,
            result.Total));
    }

    [HttpPost("adjustments")]
    [RequirePermission(Permissions.InventoryAdjust)]
    public async Task<ActionResult<StockAdjustmentResultDto>> CreateAdjustment(
        [FromBody] StockAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var organizationId = _organization.Resolve(Request);
        var result = await _adjust.ExecuteAsync(
            new CreateStockAdjustmentCommand(
                organizationId,
                ParseGuid(request.ItemId, "inventory.invalid_item_id"),
                request.LocationId,
                request.QuantityDelta,
                request.ReasonCode,
                request.Explanation,
                request.ExpectedVersion,
                V1RequestContract.RequireIdempotencyKey(Request),
                V1RequestContract.ComputeRequestHash(request)),
            cancellationToken);

        if (result.Replayed)
            Response.Headers["Idempotent-Replayed"] = "true";

        return Ok(new StockAdjustmentResultDto(
            result.PositionId.ToString(),
            result.MovementId.ToString(),
            result.Version,
            result.Quantity,
            result.Replayed));
    }

    private static InventoryPositionDto MapPosition(InventoryPosition position) => new(
        position.Id.ToString(),
        position.Version,
        position.ItemId.ToString(),
        position.LocationId,
        position.Quantity);

    private static StockMovementDto MapMovement(StockMovement movement) => new(
        movement.Id.ToString(),
        movement.PositionId.ToString(),
        movement.ItemId.ToString(),
        movement.LocationId,
        movement.Kind switch
        {
            StockMovementKind.Adjustment => "ADJUSTMENT",
            _ => throw new ApplicationProblemException(
                ApplicationProblemKind.DependencyUnavailable,
                "inventory.unsupported_movement_kind",
                "The stored stock movement kind is not supported by this API version.")
        },
        movement.QuantityBefore,
        movement.QuantityDelta,
        movement.QuantityAfter,
        movement.ReasonCode,
        movement.Explanation,
        movement.OccurredAtUtc);

    private static Guid ParseGuid(string value, string code) =>
        Guid.TryParse(value, out var parsed)
            ? parsed
            : throw new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                code,
                "The supplied identifier is invalid.");

    private static void EnsurePagination(int page, int pageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > 200)
            throw new ApplicationProblemException(
                ApplicationProblemKind.Validation,
                "pagination.invalid",
                "Page must be >= 1 and pageSize must be between 1 and 200.");
    }
}
