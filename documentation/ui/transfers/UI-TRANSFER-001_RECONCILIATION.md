# UI-TRANSFER-001 — Stock Transfers Reconciliation

Status: `RECONCILED / SPECIFICATION_READY / EXECUTION_BLOCKED_BY_API`

Upstream interface scope: `WEB-008` — Stock Transfers

Reconciled against: `main@eac26a36d53fb54a71a0fabb2dfcbbab9db69b89`

## Decision

`WEB-008` is promoted to the governed UI boundary:

```text
WEB-008 -> UI-TRANSFER-001
```

The governed route is reserved as:

```text
/transferencias
```

The route remains `PLANNED_DISABLED`. This reconciliation authorizes functional specification and visual drafting only. It does not authorize React route activation or simulated live integration while the authoritative transfer HTTP surface is absent.

## Upstream scope preserved

The Interface Scope Baseline defines `WEB-008` with:

- module: `Inventory`;
- purpose: create, approve, dispatch and receive stock transfers with discrepancy handling;
- requirements: `FR-061`, `FR-063`;
- roles: `inventory_operator`, `administrator`;
- authoritative data: transfer and line state;
- proposed actions: create transfer and dispatch/receive transfer;
- responsive intent: tablet-friendly operational workflow;
- accessibility intent: state transitions announced and keyboard operable.

The target requirements establish:

- `FR-061`: transfer is an explicit stock-movement source;
- `FR-063`: the system supports transfer/dispatch/receipt workflow with discrepancy handling.

## Use-case boundary

The governed lifecycle exists as:

- `UC-INV-002 — Transfer stock between locations`.

Its target flow is:

1. create transfer order from source to destination;
2. reserve/dispatch from source;
3. record in-transit state when operationally needed;
4. receive at destination;
5. create paired traceable stock movements;
6. resolve discrepancies explicitly rather than hiding them through overwrite.

This view does not absorb manual stock adjustment (`UC-INV-001`) or procurement/receipt (`UC-PROC-002`). Those remain separate governed boundaries.

## Contracted API dependencies

The accepted API contract defines seven transfer operations:

| API ID | operationId | Method / path | Permission | Idempotency |
| --- | --- | --- | --- | --- |
| `API-TRF-001` | `listStockTransfers` | `GET /api/v1/stock-transfers` | `inventory.read` | NO |
| `API-TRF-002` | `createStockTransfer` | `POST /api/v1/stock-transfers` | `inventory.transfer` | REQUIRED |
| `API-TRF-003` | `getStockTransfer` | `GET /api/v1/stock-transfers/{transferId}` | `inventory.read` | NO |
| `API-TRF-004` | `approveStockTransfer` | `POST /api/v1/stock-transfers/{transferId}/approve` | `inventory.transfer` | REQUIRED |
| `API-TRF-005` | `dispatchStockTransfer` | `POST /api/v1/stock-transfers/{transferId}/dispatch` | `inventory.transfer` | REQUIRED |
| `API-TRF-006` | `receiveStockTransfer` | `POST /api/v1/stock-transfers/{transferId}/receive` | `inventory.transfer` | REQUIRED |
| `API-TRF-007` | `reconcileStockTransfer` | `POST /api/v1/stock-transfers/{transferId}/reconcile` | `inventory.transfer` | REQUIRED |

All seven contracts are accepted, but Wave 4 currently records every `API-TRF-*` operation as `MISSING_HTTP` with no current WebApi evidence.

Therefore:

- no transfer list/detail read is executable through WebApi today;
- no create/approve/dispatch/receive/reconcile command is executable through WebApi today;
- visual prototypes may use clearly marked illustrative/mock transfer data;
- the production/demo WebApp must not present those mock transitions as live server-backed capability;
- route activation requires later API implementation evidence and a fresh executable reconciliation.

## Request/transition semantics already governed

The contract registry defines `StockTransferCreateRequest` conceptually as:

- source;
- destination;
- lines;
- reason/reference.

Transition commands for `approve`, `dispatch`, `receive` and `reconcile` carry:

- expected version;
- transition-specific evidence;
- idempotency at HTTP level.

Receive may contain actual received quantities and discrepancy information.

The current contract does not yet provide a field-complete public `StockTransferDto` schema in executable OpenAPI. The frontend must not invent additional authoritative fields merely because a visual mockup would benefit from them.

## Lifecycle semantics

Architecture defines the `StockTransfer` aggregate as owning:

- requested;
- approved;
- dispatched;
- received;
- discrepancy resolution.

These are lifecycle concepts, not permission to hardcode final wire enum values before the HTTP implementation exists.

The UI may visualize the lifecycle as an operational progression, but exact response state codes, transition eligibility, error payload details and discrepancy DTO structure remain API implementation dependencies.

## Authorization boundary

Read operations require `inventory.read`.

Mutation/transition operations require `inventory.transfer`.

The frontend must also respect actor organization/location scope when that executable policy becomes available. It must not assume that knowledge of a source/destination location ID grants access to transfer from or to that location.

## Audit and concurrency

Transfer-changing commands are governed as idempotent operations.

Durable event mapping already distinguishes:

- transfer created;
- transfer approved;
- transfer dispatched plus source stock movement evidence;
- transfer received plus destination/discrepancy evidence;
- transfer reconciled.

Stock-changing transitions combine idempotency with aggregate version/locking and immutable movement uniqueness. The UI must therefore preserve expected-version conflicts instead of silently retrying with invented state.

## Explicitly unsupported claims in UI-TRANSFER-001 v1

Until the HTTP surface exists, the UI must not claim authoritative support for:

- server-backed transfer creation or transitions;
- final wire state enum names;
- arbitrary transfer cancellation;
- transfer deletion/edit-history rewrite;
- automatic discrepancy resolution;
- unrestricted cross-location transfer authority;
- transportation/carrier tracking;
- ETA/SLA fields not present in the accepted contract;
- monetary valuation/costing of in-transit inventory;
- transfer reservations or available-to-sell deductions unless the backend exposes them;
- purchase-order or goods-receipt workflow inside this route;
- generic export actions not contracted by the transfer API.

## Reconciliation outcome

`UI-TRANSFER-001` is sufficiently bounded for functional specification and visual drafting.

The first governed visual should show, as clearly illustrative data:

- a transfer list with lifecycle/status affordance;
- source and destination context;
- transfer detail with line-level requested quantities;
- lifecycle progression from request through approval, dispatch and receipt;
- discrepancy visibility and reconciliation affordance;
- permission-aware action placement;
- responsive desktop/tablet/mobile behavior;
- light/dark parity with the existing eFactura shell.

However, because `API-TRF-001..007` are `MISSING_HTTP`, the route stays `PLANNED_DISABLED` and no live transfer action may be enabled yet.
