# UI-PAYABLE-001 — Reconciliation

Status: `ACTIVE_VISUAL_PREVIEW / API_PENDING`

Mapping:

```text
WEB-011 -> UI-PAYABLE-001
```

Governed route: `/cuentas-por-pagar`

## Product scope

`WEB-011 — Accounts Payable and Supplier Payments` lets treasury/accounting/admin users inspect supplier obligations, aging, allocations and balances and, when backend authority exists, register partial/full supplier payments with allocation history.

Source requirements: `FR-026`, `FR-027`, `FR-071`, `FR-072`, `FR-073`, `FR-074`.

Source lifecycle: `UC-AP-001 — Record supplier payment and allocate`.

Critical invariant: an entered supplier payment must never be silently capped/truncated. Any unapplied amount requires explicit policy handling.

## Contracted API dependencies

Accepted contracts for this surface are:

- `API-AP-001` — `listPayables` — GET `/api/v1/payables`;
- `API-AP-002` — `getPayable` — GET `/api/v1/payables/{payableId}`;
- `API-AP-003` — `getPayablesAging` — GET `/api/v1/payables/aging`;
- `API-AP-004` — `createPayableAdjustment` — POST `/api/v1/payables/{payableId}/adjustments`;
- `API-PAY-001` — `createSupplierPayment` — POST `/api/v1/supplier-payments`;
- `API-PAY-002` — `getSupplierPayment` — GET `/api/v1/supplier-payments/{paymentId}`;
- `API-PAY-003` — `reverseSupplierPayment` — POST `/api/v1/supplier-payments/{paymentId}/reverse`.

Wave 3 currently records all seven as `MISSING_HTTP`, with no WebApi evidence and `NOT_YET_AUDITED` deep readiness.

## Approved visual authority

Baseline: `UI-PAYABLE-001 / v1-responsive-suite`.

Approved artifacts:

- desktop gen_id `764a3a30-08a6-4f4f-8bda-2bdaf245b8c2`;
- mobile gen_id `06ffefef-b352-4967-9585-8012b03e7c6a`.

## Preview implementation decision

`UI-PAYABLE-001` is promoted to `ACTIVE_VISUAL_PREVIEW` under `PREVIEW_ROUTE_POLICY_AMENDMENT.md`.

The React surface may provide:

- responsive desktop ledger + mobile obligation cards;
- client-side supplier/document search;
- client-side status and due-date filtering;
- selected-payable detail;
- local demonstration history;
- KPI/aging cards derived from local fixtures;
- a visually reserved supplier-payment form.

The preview must keep these hard boundaries:

- all financial data is explicit local demonstration data;
- `capabilities.ts` registers `operations: []` for `UI-PAYABLE-001`;
- no `API-AP-*` or `API-PAY-*` HTTP call is made;
- payment, adjustment and reversal controls remain disabled;
- no authoritative balance is mutated locally;
- no cash/bank consequence is simulated as real;
- no silent payment truncation or invented unapplied-payment policy is introduced.

## Relationship with neighboring modules

`WEB-009 — Purchase Orders and Receipts` may create/link supplier payable evidence in the target lifecycle, but supplier payment/allocation remains owned by `WEB-011`.

`WEB-005 — Suppliers` may provide supplier context and future navigation into payable state.

`WEB-012 — Cash Shift and Reconciliation` remains a separate module. A future live supplier payment can have cash/bank consequences, but this preview must not claim that those consequences occurred.

## Runtime gate

After merge/deploy, `/cuentas-por-pagar` requires separate visual/runtime review. Runtime acceptance does not promote the missing HTTP operations to executable status.
