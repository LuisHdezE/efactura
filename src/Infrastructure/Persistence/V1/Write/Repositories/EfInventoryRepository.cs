using EFactura.Application.Common.Errors;
using EFactura.Application.Common.Results;
using EFactura.Application.Inventory;
using EFactura.Domain.Inventory;
using Infrastructure.Persistence.V1.Write.Models;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.V1.Write.Repositories;

public sealed class EfInventoryRepository : IInventoryRepository
{
    private readonly V1PersistenceDbContext _dbContext;

    public EfInventoryRepository(V1PersistenceDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InventoryPosition?> GetPositionAsync(
        string organizationId,
        Guid positionId,
        CancellationToken cancellationToken = default)
    {
        var local = _dbContext.InventoryPositions.Local
            .SingleOrDefault(x => x.OrganizationId == organizationId && x.Id == positionId);
        if (local is not null)
            return Map(local);

        var record = await _dbContext.InventoryPositions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == positionId,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<InventoryPosition?> GetPositionAsync(
        string organizationId,
        Guid itemId,
        string locationId,
        CancellationToken cancellationToken = default)
    {
        var normalizedLocation = locationId.Trim();
        var local = _dbContext.InventoryPositions.Local
            .SingleOrDefault(x => x.OrganizationId == organizationId
                                  && x.ItemId == itemId
                                  && x.LocationId == normalizedLocation);
        if (local is not null)
            return Map(local);

        var record = await _dbContext.InventoryPositions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId
                     && x.ItemId == itemId
                     && x.LocationId == normalizedLocation,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<PageResult<InventoryPosition>> SearchPositionsAsync(
        InventoryPositionSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var allowedLocations = request.AllowedLocationIds.ToArray();
        var query = _dbContext.InventoryPositions
            .AsNoTracking()
            .Where(x => x.OrganizationId == request.OrganizationId
                        && allowedLocations.Contains(x.LocationId));

        if (request.ItemId.HasValue)
            query = query.Where(x => x.ItemId == request.ItemId.Value);
        if (!string.IsNullOrWhiteSpace(request.LocationId))
        {
            var location = request.LocationId.Trim();
            query = query.Where(x => x.LocationId == location);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var records = await query
            .OrderBy(x => x.LocationId)
            .ThenBy(x => x.ItemId)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PageResult<InventoryPosition>(
            records.Select(Map).ToArray(), request.Page, request.PageSize, total);
    }

    public async Task<PageResult<StockMovement>> SearchMovementsAsync(
        StockMovementSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var allowedLocations = request.AllowedLocationIds.ToArray();
        var query = _dbContext.StockMovements
            .AsNoTracking()
            .Where(x => x.OrganizationId == request.OrganizationId
                        && allowedLocations.Contains(x.LocationId));

        if (request.ItemId.HasValue)
            query = query.Where(x => x.ItemId == request.ItemId.Value);
        if (!string.IsNullOrWhiteSpace(request.LocationId))
        {
            var location = request.LocationId.Trim();
            query = query.Where(x => x.LocationId == location);
        }
        if (request.PositionId.HasValue)
            query = query.Where(x => x.PositionId == request.PositionId.Value);

        var total = await query.LongCountAsync(cancellationToken);
        var records = await query
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PageResult<StockMovement>(
            records.Select(Map).ToArray(), request.Page, request.PageSize, total);
    }

    public async Task<StockMovement?> GetMovementAsync(
        string organizationId,
        Guid movementId,
        CancellationToken cancellationToken = default)
    {
        var local = _dbContext.StockMovements.Local
            .SingleOrDefault(x => x.OrganizationId == organizationId && x.Id == movementId);
        if (local is not null)
            return Map(local);

        var record = await _dbContext.StockMovements
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.OrganizationId == organizationId && x.Id == movementId,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    public Task AddPositionAsync(InventoryPosition position, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        _dbContext.InventoryPositions.Add(new V1InventoryPositionRecord
        {
            Id = position.Id,
            OrganizationId = position.OrganizationId,
            ItemId = position.ItemId,
            LocationId = position.LocationId,
            Quantity = position.Quantity,
            Version = position.Version,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        return Task.CompletedTask;
    }

    public async Task SavePositionAsync(InventoryPosition position, CancellationToken cancellationToken = default)
    {
        var local = _dbContext.InventoryPositions.Local
            .SingleOrDefault(x => x.OrganizationId == position.OrganizationId && x.Id == position.Id);
        if (local is not null && _dbContext.Entry(local).State == EntityState.Added)
        {
            local.Quantity = position.Quantity;
            local.Version = position.Version;
            local.UpdatedAtUtc = DateTimeOffset.UtcNow;
            return;
        }

        var record = await _dbContext.InventoryPositions
            .SingleOrDefaultAsync(
                x => x.OrganizationId == position.OrganizationId && x.Id == position.Id,
                cancellationToken)
            ?? throw new ApplicationProblemException(
                ApplicationProblemKind.NotFound, "inventory.position_not_found",
                "Inventory position was not found.");

        var priorVersion = position.Version - 1;
        if (record.Version != priorVersion)
        {
            throw new ApplicationProblemException(
                ApplicationProblemKind.Conflict,
                "concurrency_conflict",
                "The inventory position changed before this operation could be persisted.",
                conflictType: "stale_version",
                currentVersion: record.Version.ToString());
        }

        _dbContext.Entry(record).Property(x => x.Version).OriginalValue = priorVersion;
        record.Quantity = position.Quantity;
        record.Version = position.Version;
        record.UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Task AddMovementAsync(StockMovement movement, CancellationToken cancellationToken = default)
    {
        _dbContext.StockMovements.Add(new V1StockMovementRecord
        {
            Id = movement.Id,
            PositionId = movement.PositionId,
            OrganizationId = movement.OrganizationId,
            ItemId = movement.ItemId,
            LocationId = movement.LocationId,
            Kind = (int)movement.Kind,
            QuantityBefore = movement.QuantityBefore,
            QuantityDelta = movement.QuantityDelta,
            QuantityAfter = movement.QuantityAfter,
            PositionVersionAfter = movement.PositionVersionAfter,
            ReasonCode = movement.ReasonCode,
            Explanation = movement.Explanation,
            OccurredAtUtc = movement.OccurredAtUtc
        });
        return Task.CompletedTask;
    }

    private static InventoryPosition Map(V1InventoryPositionRecord record) => InventoryPosition.Rehydrate(
        record.Id,
        record.OrganizationId,
        record.ItemId,
        record.LocationId,
        record.Quantity,
        record.Version);

    private static StockMovement Map(V1StockMovementRecord record) => StockMovement.Rehydrate(
        record.Id,
        record.PositionId,
        record.OrganizationId,
        record.ItemId,
        record.LocationId,
        (StockMovementKind)record.Kind,
        record.QuantityBefore,
        record.QuantityDelta,
        record.QuantityAfter,
        record.PositionVersionAfter,
        record.ReasonCode,
        record.Explanation,
        record.OccurredAtUtc);
}
