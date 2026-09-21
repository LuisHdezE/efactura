# UI-CASH-001 — Cash Shift and Reconciliation Reconciliation

Status: `RECONCILED / VISUAL_BASELINE_APPROVED / EXECUTION_BLOCKED_BY_API`

Mapping:

```text
WEB-012 -> UI-CASH-001
```

Reserved route candidate: `/caja`

## Upstream product authority

`WEB-012 — Cash Shift and Reconciliation` belongs to `CashManagement` and is defined to:

- open and close cash shifts;
- view expected amounts;
- capture counted values;
- reconcile variances.

Accepted source requirements: `FR-025`, `FR-026`, `FR-027`.

Accepted roles: cashier, treasury, administrator.

Accepted lifecycle authority:

- `UC-CASH-001 — Open POS/cash shift`;
- `UC-CASH-002 — Close and reconcile cash shift`.

Known application permissions already include:

- `cash.read`;
- `cash.open`;
- `cash.move`;
- `cash.close`;
- `cash.reconcile`.

## Accepted API dependency

The governed HTTP surface for this UI is:

- `API-CSH-001` `getCurrentCashShift`;
- `API-CSH-002` `openCashShift`;
- `API-CSH-003` `getCashShift`;
- `API-CSH-004` `listCashMovements`;
- `API-CSH-005` `createCashMovement`;
- `API-CSH-006` `closeCashShift`;
- `API-CSH-007` `reconcileCashShift`.

All seven are currently `MISSING_HTTP` in Wave 3.

## Visual authority

Approved baseline: `UI-CASH-001 / v1-responsive-composite`.

Approved artifact gen_id: `4af42487-0a36-43aa-832e-ef96c7f11a61`.

The artifact includes desktop and mobile responsive compositions within the same approved image.

## Visual-to-domain reconciliation

The approved visual establishes composition, density, hierarchy, responsive behavior and visual language. It does not override source-backed CashManagement semantics.

The following visual concepts are directly compatible with current scope:

- cash/finance summary cards;
- movement ledger/list;
- reconciliation workspace;
- selected movement/shift detail;
- status history;
- search/filter controls;
- responsive mobile cards;
- explicit demo disclosure.

The following visual copy is not independently supported by current `WEB-012` authority and must remain illustrative until separate product evidence exists:

- generic bank-account administration;
- authoritative bank balances;
- arbitrary inter-account transfer management;
- export behavior not attached to an accepted contract.

Implementation may reconcile those labels to cash-shift concepts while preserving the approved composition.

## Preview boundary

A future visual preview may use local fixtures for inspection and client-side interaction, but must not:

- claim a canonical shift is open/closed from server state;
- post movements;
- close a shift;
- reconcile differences;
- persist counted values;
- invent tolerance approval outcomes;
- mutate historical shift data;
- claim bank-account side effects.

All server-owned actions remain disabled until executable API evidence exists.

## Next gate

Create the functional specification/inventory record, preserve the approved baseline, then separately implement `/caja` as `ACTIVE_VISUAL_PREVIEW` under the shared preview policy. Route activation is not authorized by this reconciliation alone.