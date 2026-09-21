# UI-PAYABLE-001 — Reconciliation

Status: `VISUAL_BASELINE_APPROVED / IMPLEMENTATION_NOT_STARTED`

Mapping:

```text
WEB-011 -> UI-PAYABLE-001
```

Candidate route: `/cuentas-por-pagar`

## Product scope

`WEB-011 — Accounts Payable and Supplier Payments` exists to let treasury/accounting/admin users inspect supplier obligations, aging, allocations and balances and, when backend authority exists, register partial/full supplier payments with allocation history.

Source requirements: `FR-026`, `FR-027`, `FR-071`, `FR-072`, `FR-073`, `FR-074`.

Source lifecycle: `UC-AP-001 — Record supplier payment and allocate`.

The lifecycle is explicitly the payable-side mirror of `UC-AR-001`, with supplier/treasury permissions and cash/bank consequences.

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

The visual authority is preserved in:

`documentation/ui/references/approved/UI-PAYABLE-001/v1-responsive-suite/README.md`

## Preview eligibility

The missing APIs block live payable claims and all server-owned mutations, but they do not inherently block a governed visual preview under the existing preview-route policy, provided that:

- all financial data is explicit local demonstration data;
- payable aging/KPIs are labeled demonstration values;
- no `API-AP-*` or `API-PAY-*` operation is registered as executable;
- payment, adjustment and reversal controls remain disabled;
- the view does not mutate authoritative balances locally;
- no cash/bank consequence is simulated as real;
- no silent payment truncation or invented unapplied-payment policy is introduced.

## Relationship with neighboring modules

`WEB-009 — Purchase Orders and Receipts` may create/link supplier payable evidence in the target lifecycle, but supplier payment/allocation remains owned by `WEB-011`.

`WEB-005 — Suppliers` may provide supplier context and future navigation into payable state, but this view must not invent dead links before the route is governed and active.

`WEB-012 — Cash Shift and Reconciliation` remains a separate module. A future live supplier payment can have cash/bank consequences, but the payable preview must not claim that those consequences occurred.

## Next gate

1. preserve this baseline and specification in `main`;
2. reconcile `UI-PAYABLE-001` with `PREVIEW_ROUTE_POLICY_AMENDMENT.md`;
3. implement a local-demo React preview only after governance promotion;
4. keep all supplier-payment mutations disabled until the seven accepted API dependencies are implemented and freshly reconciled.