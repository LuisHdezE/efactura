# UI-CASH-001 — Cash Shift and Reconciliation Reconciliation

Status: `ACTIVE_VISUAL_PREVIEW / API_PENDING / RUNTIME_REVIEW_PENDING`

Mapping:

```text
WEB-012 -> UI-CASH-001
```

Route: `/caja`

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

Known application permissions:

- `cash.read`;
- `cash.open`;
- `cash.move`;
- `cash.close`;
- `cash.reconcile`.

## Accepted API dependency

The governed HTTP surface is:

- `API-CSH-001` `getCurrentCashShift`;
- `API-CSH-002` `openCashShift`;
- `API-CSH-003` `getCashShift`;
- `API-CSH-004` `listCashMovements`;
- `API-CSH-005` `createCashMovement`;
- `API-CSH-006` `closeCashShift`;
- `API-CSH-007` `reconcileCashShift`.

All seven remain `MISSING_HTTP` in Wave 3.

## Visual authority

Approved baseline: `UI-CASH-001 / v1-responsive-composite`.

Approved artifact gen_id: `4af42487-0a36-43aa-832e-ef96c7f11a61`.

The artifact includes desktop and mobile responsive compositions within the same approved image.

## Visual-to-domain reconciliation

The approved visual governs composition, density, hierarchy, responsive behavior and visual language. It does not override source-backed CashManagement semantics.

The React implementation intentionally maps unsupported generic bank/account wording onto source-backed cash-shift/payment-medium concepts. It does not expose bank-account administration, authoritative bank balances, arbitrary inter-account transfer management or ungoverned export behavior.

## Active preview boundary

The route is now permitted as `ACTIVE_VISUAL_PREVIEW` under `PREVIEW_ROUTE_POLICY_AMENDMENT.md` because:

- the approved baseline is preserved;
- a responsive React page exists;
- every displayed business value is identified as local demonstration data;
- `Abrir turno`, manual movement, `Cerrar turno` and reconciliation remain disabled;
- `capabilities.ts` registers `operations: []`;
- `verify-cash-preview.mjs` guards against accidental CSH HTTP integration.

The preview must not:

- claim canonical shift state from the server;
- post movements;
- close a shift;
- reconcile differences;
- persist counted values;
- invent tolerance or approval outcomes;
- mutate historical shift data;
- claim bank-account side effects.

## Runtime gate

Repository CI must pass before merge. After deployment, desktop/mobile runtime review remains a separate acceptance checkpoint. Live API integration requires fresh executable evidence and a separate reconciliation.
