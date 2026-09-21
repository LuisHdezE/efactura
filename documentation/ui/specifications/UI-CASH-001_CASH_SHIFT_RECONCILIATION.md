# UI-CASH-001 — Cash Shift and Reconciliation

Status: `FUNCTIONAL_SPECIFICATION / VISUAL_BASELINE_APPROVED / EXECUTION_BLOCKED_BY_API`

WEB mapping: `WEB-012`

Reserved route candidate: `/caja`

Approved visual baseline: `UI-CASH-001 / v1-responsive-composite`

## 1. Purpose

Provide the governed WebApp surface for cashier/treasury inspection of the active cash shift, expected totals, counted values, movements and reconciliation variance without inventing server authority.

## 2. Roles and permissions

Accepted roles:

- cashier;
- treasury;
- administrator.

Accepted permission vocabulary:

- `cash.read`;
- `cash.open`;
- `cash.move`;
- `cash.close`;
- `cash.reconcile`.

UI visibility must never substitute for backend authorization.

## 3. Authoritative lifecycle

### Open shift

From `UC-CASH-001`:

1. select terminal/branch;
2. ensure no conflicting open shift exists under policy;
3. record opening float by currency/payment bucket as applicable;
4. persist shift identity, operator and opening timestamp;
5. audit opening.

The server is authoritative for opening a canonical shift.

### Close and reconcile shift

From `UC-CASH-002`:

1. freeze/select shift for closing;
2. calculate expected totals by payment medium from recorded transactions;
3. capture physical counts/vouchers/transfers/checks;
4. calculate variances;
5. require explanation/approval above configured tolerance;
6. close shift and prevent ordinary mutation of closed records;
7. audit counted values, expected values, variance and approvals.

## 4. Required information architecture

The preview should reserve the following regions while preserving the approved visual hierarchy:

- page header and preview/demo status;
- current-shift summary;
- expected/count/variance KPI region;
- movement search/filter region;
- movement ledger/list;
- selected shift or movement detail;
- reconciliation workspace;
- history/audit-oriented timeline where supported by fixture semantics;
- responsive mobile cards/detail.

## 5. Current API dependency

- `API-CSH-001` GET current cash shift;
- `API-CSH-002` POST open cash shift;
- `API-CSH-003` GET cash shift;
- `API-CSH-004` GET cash movements;
- `API-CSH-005` POST manual cash movement;
- `API-CSH-006` POST close cash shift;
- `API-CSH-007` POST reconcile cash shift.

All seven are `MISSING_HTTP`.

## 6. Preview behavior before API readiness

A governed visual preview may support local-only interactions such as:

- selecting demo movement/shift records;
- searching/filtering fixture rows/cards;
- switching informational tabs;
- displaying deterministic fixture expected/count/variance examples;
- responsive layout behavior.

It must visibly identify all business values as demonstration data.

## 7. Disabled server-owned actions

Until fresh executable API evidence exists, the preview must disable or otherwise make non-executable:

- `Abrir turno`;
- manual cash movement posting;
- `Cerrar turno`;
- `Conciliar` / approve variance;
- persistence of counted values;
- any operation that mutates canonical cash state.

No local interaction may pretend to have changed server truth.

## 8. Variance and tolerance semantics

The client may display fixture examples of expected, counted and variance values. It must not invent:

- the configured tolerance threshold;
- who must approve a variance;
- whether a variance is accepted;
- accounting/cash consequences of the variance.

Those outcomes are server/policy authority.

## 9. Movement history

Movement fixtures may represent business sources such as collections, supplier payments, POS cash operations or manual movements for visual inspection. The preview must not claim those cross-module effects were actually posted.

Closed shift history is immutable from this UI except through future accepted compensating/reconciliation workflows.

## 10. Approved visual reconciliation

The approved composite contains generic account/bank wording. That wording is not itself a product contract. React implementation should preserve the approved layout, density, visual hierarchy, responsive behavior and status treatment while mapping labels to source-backed cash-shift concepts.

## 11. Responsive behavior

Desktop/tablet:

- dense movement ledger permitted;
- detail/reconciliation panel may coexist beside the ledger;
- filters remain accessible without hiding execution boundaries.

Mobile:

- cards replace wide tables where necessary;
- expected/count/variance values remain readable;
- selected detail follows the list or opens in a readable stacked region;
- disabled authoritative actions remain visibly disabled;
- status must never depend on color alone.

## 12. Error/state reservation

Future API integration must handle at minimum:

- default;
- loading;
- success;
- error;
- `403`;
- `409`;
- `422`.

Preview fixtures do not manufacture authoritative responses for those states.

## 13. Route activation gate

The approved visual baseline alone does not activate `/caja`.

Activation as `ACTIVE_VISUAL_PREVIEW` requires:

1. preview-policy reconciliation;
2. responsive React implementation;
3. explicit local-demo labeling;
4. all server-owned mutations disabled;
5. `operations: []` while all cash HTTP contracts remain missing;
6. source-level regression guard;
7. repository CI green;
8. separate deployed runtime review.

Live integration is a later, independent gate.