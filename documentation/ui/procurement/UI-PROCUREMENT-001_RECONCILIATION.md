# UI-PROCUREMENT-001 — Purchase Orders and Receipts Reconciliation

Status: `RECONCILED / SPECIFICATION_READY / EXECUTION_BLOCKED_BY_API`

Upstream interface scope: `WEB-009` — Purchase Orders and Receipts

Reconciled against: `main@d4e87a300add687fa6500ea866ccb092144f08ab`

## Decision

`WEB-009` is promoted to the governed UI boundary:

```text
WEB-009 -> UI-PROCUREMENT-001
```

The governed route is reserved as:

```text
/compras
```

The route remains `PLANNED_DISABLED`. This reconciliation authorizes functional specification and visual drafting only. It does not authorize React route activation or simulated live procurement behavior while the authoritative purchase-order and goods-receipt HTTP surfaces are absent.

## Upstream scope preserved

The Interface Scope Baseline defines `WEB-009` with:

- module: `Procurement`;
- purpose: manage purchase orders, approval and goods receipt while linking inventory/payable evidence;
- requirements: `FR-067`, `FR-068`, `FR-071`;
- roles: `purchaser`, `inventory_operator`, `administrator`;
- authoritative data: purchase orders and receipts;
- proposed actions: manage purchase order and post goods receipt;
- responsive intent: administrative and warehouse-friendly layout;
- accessibility intent: accessible line-item editing.

The target requirements establish:

- `FR-067`: support purchase-order and goods-receipt workflows;
- `FR-068`: costing may use approved PPP/FIFO policies when enabled, with policy/version traceability;
- `FR-071`: accounts payable links obligations to supplier/source evidence.

`FR-068` and `FR-071` are downstream effects/boundaries for this view, not permission to embed a costing-policy editor or supplier-payment workflow into `/compras`.

## Use-case boundary

The governed lifecycle is primarily `UC-PROC-002 — Approve purchase order and receive goods`:

1. convert a proposal/manual request to a purchase order;
2. select supplier, currency, expected dates and lines;
3. approve according to permission/workflow;
4. record received quantities/costs/discrepancies;
5. append inventory receipt movements for stock-tracked items;
6. optionally create/link supplier payable and received fiscal-document evidence;
7. update costing according to enabled policy;
8. audit approval and receipt.

`UC-PROC-001` remains the separate replenishment/EOQ advisory boundary and does not directly mutate stock.

Supplier payment/allocation remains `WEB-011` / `UC-AP-001`, not this view.

## Contracted API dependencies

The accepted API contract assigns ten operations:

| API ID | operationId | Method / path | Permission | Idempotency |
| --- | --- | --- | --- | --- |
| `API-PRC-001` | `listPurchaseOrders` | `GET /api/v1/purchase-orders` | `procurement.read` | NO |
| `API-PRC-002` | `createPurchaseOrder` | `POST /api/v1/purchase-orders` | `procurement.manage` | REQUIRED |
| `API-PRC-003` | `getPurchaseOrder` | `GET /api/v1/purchase-orders/{purchaseOrderId}` | `procurement.read` | NO |
| `API-PRC-004` | `updatePurchaseOrderDraft` | `PATCH /api/v1/purchase-orders/{purchaseOrderId}` | `procurement.manage` | REQUIRED |
| `API-PRC-005` | `approvePurchaseOrder` | `POST /api/v1/purchase-orders/{purchaseOrderId}/approve` | `procurement.approve` | REQUIRED |
| `API-PRC-006` | `cancelPurchaseOrder` | `POST /api/v1/purchase-orders/{purchaseOrderId}/cancel` | `procurement.manage` | REQUIRED |
| `API-GRC-001` | `listGoodsReceipts` | `GET /api/v1/goods-receipts` | `procurement.read` | NO |
| `API-GRC-002` | `createGoodsReceipt` | `POST /api/v1/goods-receipts` | `procurement.receive` | REQUIRED |
| `API-GRC-003` | `getGoodsReceipt` | `GET /api/v1/goods-receipts/{receiptId}` | `procurement.read` | NO |
| `API-GRC-004` | `postGoodsReceipt` | `POST /api/v1/goods-receipts/{receiptId}/post` | `procurement.receive` | REQUIRED |

Wave 4 currently records all ten operations as `MISSING_HTTP` with no current WebApi evidence.

Therefore:

- no purchase-order list/detail is executable through WebApi today;
- no create/update/approve/cancel purchase-order command is executable today;
- no goods-receipt list/detail/create/post operation is executable today;
- visual prototypes may use clearly marked illustrative/mock procurement data;
- the running WebApp must not present those transitions as live server-backed capability;
- route activation requires later API implementation evidence and a fresh executable reconciliation.

## Authorization boundary

Read operations require `procurement.read`.

Purchase-order mutation requires `procurement.manage`; approval requires `procurement.approve`; receipt creation/posting requires `procurement.receive`.

The UI must not infer authorization from role labels alone.

## Downstream evidence boundaries

Posting a governed goods receipt may eventually produce inventory movement, costing and payable/source evidence according to backend policy. The frontend must not manufacture those side effects or calculate their authoritative result locally.

The view may show linked evidence only when executable DTOs expose it.

## Explicitly unsupported claims in UI-PROCUREMENT-001 v1

Until the HTTP surface exists, the UI must not claim authoritative support for:

- server-backed purchase-order or receipt mutation;
- final wire lifecycle enum names;
- costing-policy administration or local PPP/FIFO calculation;
- supplier payment/allocation;
- automatic payable creation unless returned by the backend;
- received-CFE ingestion/validation workflow;
- inventory quantities derived locally from receipt mock data;
- arbitrary document deletion/history rewrite;
- unrestricted supplier/location access;
- carrier/logistics tracking;
- export functionality not contracted by the API.

## Reconciliation outcome

`UI-PROCUREMENT-001` is sufficiently bounded for functional specification and visual drafting.

The first governed visual should show, as clearly illustrative data:

- purchase-order list and status;
- supplier and expected-date context;
- line-item quantities/cost presentation;
- order detail with approval affordance;
- receipt history/context;
- requested/ordered versus received quantity comparison;
- discrepancy visibility;
- permission-aware action placement;
- responsive desktop/tablet/mobile behavior;
- light/dark parity with the existing eFactura shell.

Because `API-PRC-001..006` and `API-GRC-001..004` remain `MISSING_HTTP`, `/compras` stays `PLANNED_DISABLED` and no live procurement action may be enabled yet.
