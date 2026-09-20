# UI-INVENTORY-001 — Inventory and Movements Reconciliation

Status: `RECONCILED / SPECIFICATION_READY_WITH_EXECUTION_GAPS`

Upstream interface scope: `WEB-007` — Inventory and Movements

Reconciled against: `main@d727aa9aacc5adaa7feaf696e5116d11fdfdc7d3`

## Decision

`WEB-007` is promoted to the governed UI boundary:

```text
WEB-007 -> UI-INVENTORY-001
```

The governed route is reserved as:

```text
/inventario
```

The first governed view covers executable stock-position reads, immutable movement inspection and authorized manual stock adjustments. It does not absorb transfer, procurement, replenishment or costing workflows that belong to separate boundaries or are not executable today.

## Upstream scope preserved

The Interface Scope Baseline defines `WEB-007` with:

- module: `Inventory`;
- purpose: view stock by location and perform authorized adjustments with immutable movement history;
- requirements: `FR-060`, `FR-061`, `FR-062`, `FR-064`, `FR-065`, `FR-066`;
- roles: inventory operator and administrator;
- authoritative data: stock positions, movements and replenishment indicators;
- proposed actions: create stock adjustment and run EOQ/ROP advisory simulation.

The current requirements establish:

- `FR-060`: immutable stock movements plus current positions by location;
- `FR-061`: sale, purchase receipt, transfer and adjustment are explicit movement sources;
- `FR-062`: manual adjustment requires authorization and auditable before/after reason context;
- `FR-064`: stock-minimum/replenishment alerts are a target capability;
- `FR-065`: EOQ/ROP advisory must use real historical/configured inputs, not hardcoded demand;
- `FR-066`: EOQ/ROP is advisory and must not mutate stock directly.

## Use-case and traceability status

A governed lifecycle does exist for the executable write operation:

- `UC-INV-001 — Manual stock adjustment`.

Its flow requires item/location identification, current quantity/version, non-zero positive or negative delta, reason, permission, immutable movement append, atomic position update and audit.

`UC-INV-002 — Transfer stock between locations` is not part of this view. It belongs to `WEB-008 / Stock Transfers` and must not be pulled into `/inventario` merely because it shares the Inventory domain.

`UC-PROC-001 — Generate replenishment/EOQ proposal` is relevant to the upstream scope but its HTTP surface is not implemented yet.

No dedicated governed `US-INV-*` artifact was found during this reconciliation. That story-level traceability gap remains explicit and is not fabricated by frontend work.

## Executable dependencies

Wave 4 records exactly four implemented inventory operations:

| API ID | operationId | Route | Permission | UI disposition |
| --- | --- | --- | --- | --- |
| `API-INV-001` | `listInventoryPositions` | `GET /api/v1/inventory/positions` | `inventory.read` | `SUPPORTED` |
| `API-INV-002` | `getInventoryPosition` | `GET /api/v1/inventory/positions/{positionId}` | `inventory.read` | `SUPPORTED` |
| `API-INV-003` | `listStockMovements` | `GET /api/v1/inventory/movements` | `inventory.read` | `SUPPORTED_WITH_PROJECTION_GAP` |
| `API-INV-004` | `createStockAdjustment` | `POST /api/v1/inventory/adjustments` | `inventory.adjust` | `SUPPORTED` |

The replenishment operations remain non-executable:

| API ID | operationId | Route | Current state | UI disposition |
| --- | --- | --- | --- | --- |
| `API-RPL-001` | `simulateReplenishment` | `POST /api/v1/replenishment/simulations` | `MISSING_HTTP` | `NOT_EXECUTABLE_V1` |
| `API-RPL-002` | `listReplenishmentRecommendations` | `GET /api/v1/replenishment/recommendations` | `MISSING_HTTP` | `NOT_EXECUTABLE_V1` |

`API-TRF-*` operations are also `MISSING_HTTP`, but independently they belong to `WEB-008` rather than this view.

## Authoritative inventory projections

`InventoryPositionDto` exposes only:

- `id`;
- `version`;
- `itemId`;
- `locationId`;
- `quantity`.

`StockMovementDto` exposes:

- `id`;
- `positionId`;
- `itemId`;
- `locationId`;
- `kind`;
- `quantityBefore`;
- `quantityDelta`;
- `quantityAfter`;
- `reasonCode`;
- `explanation`;
- `occurredAtUtc`.

The stock-adjustment request exposes:

- `itemId`;
- `locationId`;
- `quantityDelta`;
- `reasonCode`;
- `expectedVersion`;
- optional `explanation`.

The adjustment command additionally requires an `Idempotency-Key` at HTTP level.

## Search/filter semantics actually available

Position list supports server-side:

- optional `itemId`;
- optional `locationId`;
- `page` / `pageSize`.

Movement list supports server-side:

- optional `itemId`;
- optional `locationId`;
- optional `positionId`;
- `page` / `pageSize`.

The current inventory API does **not** expose authoritative server-side filters for item name/code text, movement date range, reason code, movement kind, positive/negative delta or quantity thresholds. The UI must not pretend those are complete server filters over the full result set.

## Authorization and location scope

Inventory read/write authorization is location-scoped in Application:

- actor must be authenticated;
- actor must have the required inventory permission;
- actor must be inside organization scope;
- actor must have explicit location scopes;
- a requested location must belong to the actor's allowed location scopes.

This means the frontend must not offer unrestricted cross-location inventory access simply because a location ID is known.

Friendly item/location labels are optional enrichment, not Inventory authority:

- item names/codes may be enriched from Catalog only when the actor can read that surface;
- location names may be enriched from `listLocations` only when the actor has the necessary organization permission;
- the inventory route must remain valid with authoritative IDs if those optional reads are unavailable.

## Adjustment semantics and constraints

An adjustment:

- is allowed only for an active `PRODUCT` with `TrackInventory=true`;
- may have positive or negative `quantityDelta`;
- rejects zero delta;
- requires `reasonCode` and permits optional `explanation`;
- uses optimistic versioning;
- is idempotent and auditable;
- creates an immutable movement and updates the position atomically.

Current domain limits include:

- location required, max 200 characters;
- reason required, max 80 characters;
- explanation optional, max 1000 characters.

If no position exists yet, the first adjustment is allowed only with `expectedVersion = 0`; the position is then created from zero and adjusted atomically.

The repository requirements still list the detailed negative-stock/backorder policy as open. The UI must therefore show the projected before/delta/after values and server outcomes, but must not invent a client-side prohibition or approval policy that the backend has not finalized.

## Movement-projection gap

The Domain currently defines at least:

- `Adjustment`;
- `SaleConsumption`.

Sale confirmation can persist `SaleConsumption` movements. However, the current `InventoryController.MapMovement` maps only `Adjustment -> "ADJUSTMENT"` and throws `inventory.unsupported_movement_kind` for other stored kinds.

Therefore `API-INV-003` is executable but has a current projection gap for non-adjustment movement kinds. The frontend must not claim complete sale/purchase/transfer movement history until that backend projection gap is reconciled. This is an API dependency, not a frontend problem to hide with invented mappings.

## Explicitly unsupported claims in UI-INVENTORY-001 v1

The current authoritative inventory surface does not provide:

- reserved quantity;
- available-to-sell quantity distinct from `quantity`;
- reorder point/minimum stock;
- EOQ;
- safety stock;
- replenishment recommendation;
- inventory monetary valuation;
- FIFO/PPP layer detail;
- pending inbound/outbound transfer quantities;
- purchase-receipt state;
- supplier linkage;
- lot/serial/expiry tracking;
- warehouse bin hierarchy;
- arbitrary movement deletion/editing.

A paginated list of positions also does not authorize a fake organization-wide stock total produced by summing one visible page.

## Reconciliation outcome

`UI-INVENTORY-001` is sufficiently bounded for functional specification and visual drafting.

The first governed version should provide:

- paginated stock positions by item/location;
- position detail with authoritative quantity/version;
- immutable movement history for the selected position or scoped inventory;
- manual adjustment flow with reason, explanation, delta, expected version and explicit before/after preview;
- clear read/write permission affordances;
- no transfer, procurement or replenishment workflow inside this route;
- explicit degraded/error state for backend movement-projection failures rather than fabricated movement support.

The route remains `PLANNED_DISABLED` until visual approval and frontend implementation complete the governed sequence.
