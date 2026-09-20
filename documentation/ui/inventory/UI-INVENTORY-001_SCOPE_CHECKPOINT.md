# UI-INVENTORY-001 Scope Checkpoint

Status: `RECONCILIATION_COMPLETE / VISUAL_BASELINE_PENDING`

This checkpoint records the governed boundary established for `WEB-007 / Inventory and Movements` before visual drafting.

## Governed identity

```text
WEB-007 -> UI-INVENTORY-001
route candidate -> /inventario
navigation state -> PLANNED_DISABLED
```

## Included in v1

- inventory positions by item/location;
- position detail and optimistic version;
- immutable movement history through the current inventory HTTP projection;
- authorized manual stock adjustment;
- explicit concurrency/idempotency/error states.

## Excluded from v1

- stock transfers (`WEB-008`);
- purchase orders / receipts (`WEB-009`);
- EOQ/ROP execution and recommendations while `API-RPL-001/002` remain `MISSING_HTTP`;
- valuation/costing;
- reserved/available-to-sell quantities not present in `InventoryPositionDto`;
- fake global stock totals from one paginated page;
- invented negative-stock policy.

## Known backend dependency

The Domain can persist `SaleConsumption` movements, while the current `InventoryController` HTTP mapper only supports `Adjustment`. The UI must not hide this mismatch or fabricate complete movement-kind support.

## Next gate

Produce and explicitly approve a light/dark visual baseline before route activation or React implementation.
