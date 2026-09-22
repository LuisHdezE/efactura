# UI-CASH-001 — Cash Shift and Reconciliation

Status: `IMPLEMENTED_VISUAL_PREVIEW / API_PENDING / RUNTIME_REVIEW_PENDING`

WEB mapping: `WEB-012`

Route: `/caja`

Approved visual baseline: `UI-CASH-001 / v1-responsive-composite`

## 1. Purpose

Provide the governed WebApp surface for cashier/treasury inspection of a demonstration cash shift, expected totals, counted values, movements and reconciliation variance without inventing server authority.

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

## 4. Implemented preview information architecture

The active preview contains:

- page header and preview/demo status;
- demonstration current-shift summary;
- expected/income/expense/variance KPI region;
- movement search/filter region;
- responsive movement ledger/cards;
- selected movement detail and local history;
- reconciliation workspace;
- turn information workspace;
- disabled server-owned controls;
- responsive mobile behavior.

## 5. Current API dependency

- `API-CSH-001` GET current cash shift;
- `API-CSH-002` POST open cash shift;
- `API-CSH-003` GET cash shift;
- `API-CSH-004` GET cash movements;
- `API-CSH-005` POST manual cash movement;
- `API-CSH-006` POST close cash shift;
- `API-CSH-007` POST reconcile cash shift.

All seven are `MISSING_HTTP`.

## 6. Active preview behavior

The governed visual preview supports local-only interaction:

- selecting demo movement records;
- searching/filtering fixture rows/cards;
- switching `Movimientos`, `Conciliación` and `Turno` tabs;
- displaying deterministic fixture expected/count/variance examples;
- responsive layout behavior.

All business values are visibly identified as demonstration data.

## 7. Disabled server-owned actions

Until fresh executable API evidence exists, the preview keeps non-executable:

- `Abrir turno`;
- manual cash movement posting;
- `Cerrar turno`;
- `Conciliar` / approve variance;
- persistence of counted values;
- movement editing/reversal;
- any operation that mutates canonical cash state.

No local interaction may pretend to have changed server truth.

## 8. Variance and tolerance semantics

The client displays fixture examples of expected, counted and variance values. It does not invent:

- the configured tolerance threshold;
- who must approve a variance;
- whether a variance is accepted;
- accounting/cash consequences of the variance.

Those outcomes remain server/policy authority.

## 9. Movement history

Movement fixtures may visually represent business sources such as collections, supplier payments or POS cash operations. The preview does not claim those cross-module effects were actually posted.

Closed shift history remains immutable from this UI except through future accepted compensating/reconciliation workflows.

## 10. Approved visual reconciliation

The approved composite contains generic bank/account wording. That wording is not itself a product contract. The React implementation preserves the approved layout, density, visual hierarchy, responsive behavior and status treatment while mapping labels to source-backed cash-shift/payment-medium concepts.

It does not expose authoritative bank balances, bank-account administration or arbitrary inter-account transfers.

## 11. Responsive behavior

Desktop/tablet:

- dense movement ledger;
- detail panel beside ledger when space permits;
- reconciliation and turn panels readable within the shared shell;
- filters keep execution boundaries visible.

Mobile:

- movement cards replace the wide table;
- expected/count/variance values remain readable;
- selected detail follows the list;
- disabled authoritative actions remain visibly disabled;
- status never depends on color alone.

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

## 13. Current activation state

`/caja` is `ACTIVE_VISUAL_PREVIEW` under `PREVIEW_ROUTE_POLICY_AMENDMENT.md`.

Current guarantees:

1. responsive React implementation exists;
2. local-demo labeling is explicit;
3. all server-owned mutations are disabled;
4. `capabilities.ts` registers `operations: []`;
5. `verify-cash-preview.mjs` guards against accidental live CSH integration;
6. deployed runtime review remains pending until merge/deploy.

Live API integration is a later, independent gate.
