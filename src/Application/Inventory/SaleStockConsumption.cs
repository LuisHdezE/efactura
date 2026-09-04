using EFactura.Application.Common.Errors;
using EFactura.Domain.Common;
using EFactura.Domain.Inventory;

namespace EFactura.Application.Inventory;

public sealed record SaleStockConsumptionRequest(
    string OrganizationId,
    string LocationId,
    Guid SaleId,
    string ConfirmationFingerprint,
    string SettlementFingerprint,
    IReadOnlyCollection<InventoryAvailabilityLineResult> InventoryLines);

public sealed record SaleStockConsumptionResult(IReadOnlyCollection<StockMovement> Movements);

/// <summary>
/// Stages authoritative stock effects for a future outer sale-confirmation transaction.
/// This component deliberately does not start/commit a transaction, own idempotency,
/// write audit/outbox evidence, mark a Sale confirmed or create fiscal artifacts.
/// </summary>
public sealed class SaleStockConsumer
{
    private readonly IInventoryRepository _inventory;

    public SaleStockConsumer(IInventoryRepository inventory) => _inventory = inventory;

    public async Task<SaleStockConsumptionResult> StageAsync(
        SaleStockConsumptionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.OrganizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.LocationId);
        if (request.SaleId == Guid.Empty)
            throw Problem("inventory.sale_id_required", "Sale stock consumption requires a source sale id.");
        if (request.InventoryLines is null)
            throw Problem("inventory.evidence_required", "Inventory evidence is required for sale stock consumption.");

        var tracked = request.InventoryLines.Where(line => line.TracksInventory).ToArray();
        if (tracked.Select(line => line.ItemId).Distinct().Count() != tracked.Length)
            throw Problem("inventory.evidence_duplicate", "Sale stock consumption requires one tracked inventory evidence row per item.");

        var movements = new List<StockMovement>(tracked.Length);
        foreach (var evidence in tracked)
        {
            if (!evidence.Sufficient || !evidence.PositionVersion.HasValue || !evidence.AvailableQuantity.HasValue)
                throw Conflict("inventory.evidence_stale", "Tracked inventory evidence is not authoritative enough to consume stock.");
            if (evidence.RequiredQuantity <= 0m)
                throw Problem("inventory.sale_consumption_quantity_invalid", "Tracked sale quantity must be greater than zero.");

            var position = await _inventory.GetPositionAsync(
                request.OrganizationId,
                evidence.ItemId,
                request.LocationId.Trim(),
                cancellationToken)
                ?? throw Conflict("inventory.position_missing", "The inventory position used during sale validation no longer exists.");

            if (position.Version != evidence.PositionVersion.Value
                || position.Quantity != evidence.AvailableQuantity.Value)
            {
                throw Conflict(
                    "inventory.evidence_stale",
                    "Inventory quantity/version changed after sale confirmation planning.",
                    position.Version.ToString());
            }

            StockMovement movement;
            try
            {
                movement = position.ConsumeForSale(
                    request.SaleId,
                    evidence.RequiredQuantity,
                    request.ConfirmationFingerprint,
                    request.SettlementFingerprint,
                    DateTimeOffset.UtcNow,
                    evidence.PositionVersion.Value);
            }
            catch (DomainRuleException ex) when (
                ex.Code is "concurrency.stale_version" or "inventory.insufficient_stock")
            {
                throw Conflict(ex.Code, ex.Message, position.Version.ToString());
            }
            catch (DomainRuleException ex)
            {
                throw Problem(ex.Code, ex.Message);
            }

            await _inventory.SavePositionAsync(position, cancellationToken);
            await _inventory.AddMovementAsync(movement, cancellationToken);
            movements.Add(movement);
        }

        return new SaleStockConsumptionResult(movements.ToArray());
    }

    private static ApplicationProblemException Problem(string code, string message) =>
        new(ApplicationProblemKind.Validation, code, message);

    private static ApplicationProblemException Conflict(string code, string message, string? currentVersion = null) =>
        new(
            ApplicationProblemKind.Conflict,
            code,
            message,
            conflictType: "stale_inventory",
            currentVersion: currentVersion);
}
