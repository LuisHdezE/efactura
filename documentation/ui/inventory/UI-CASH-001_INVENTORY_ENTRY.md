# UI-CASH-001 — Governed UI Inventory Entry

Status: `IMPLEMENTED_VISUAL_PREVIEW / RUNTIME_REVIEW_PENDING`

Mapping:

```text
WEB-012 -> UI-CASH-001
```

Route: `/caja`

Navigation state: `ACTIVE_VISUAL_PREVIEW`

## Evidence

- upstream scope: `documentation/blueprint-interface/interface-scope-baseline.json` (`WEB-012`);
- requirements: `FR-025`, `FR-026`, `FR-027`;
- lifecycles: `UC-CASH-001`, `UC-CASH-002`;
- API matrix: `documentation/api-completion-matrix/WAVE_3.md`;
- reconciliation: `documentation/ui/cash/UI-CASH-001_RECONCILIATION.md`;
- specification: `documentation/ui/specifications/UI-CASH-001_CASH_SHIFT_RECONCILIATION.md`;
- approved visual reference: `documentation/ui/references/approved/UI-CASH-001/v1-responsive-composite/README.md`;
- preview policy: `documentation/ui/PREVIEW_ROUTE_POLICY_AMENDMENT.md`;
- React implementation: `src/WebApp/src/features/cash/CashPage.tsx`;
- source-level guard: `src/WebApp/scripts/verify-cash-preview.mjs`.

## Approved visual baseline

Approved baseline: `UI-CASH-001 / v1-responsive-composite`.

Artifact identity:

- composite gen_id: `4af42487-0a36-43aa-832e-ef96c7f11a61`;
- includes desktop and mobile responsive compositions in one explicitly approved artifact.

The baseline governs layout hierarchy, navigation placement, KPI presentation, movement/reconciliation workspace, selected detail treatment, responsive cards and preview disclosure.

## Accepted API dependency

The view depends on:

- `API-CSH-001` current shift;
- `API-CSH-002` open shift;
- `API-CSH-003` shift detail;
- `API-CSH-004` movement ledger;
- `API-CSH-005` manual movement;
- `API-CSH-006` close shift;
- `API-CSH-007` reconcile shift.

All seven remain `MISSING_HTTP` in Wave 3.

## Preview execution boundary

The active preview is authorized to use deterministic local fixtures for client-side inspection only. It does **not** authorize:

- canonical shift opening/closing;
- manual cash posting;
- variance reconciliation;
- persistence of counted values;
- invented tolerance/approval decisions;
- authoritative balance or bank-account claims;
- mutation of historical cash state.

`UI-CASH-001` therefore remains `IMPLEMENTED_VISUAL_PREVIEW` with `operations: []` until fresh API evidence exists.

The React implementation intentionally maps unsupported generic bank/account wording from the approved visual artifact onto source-backed cash-shift/payment-medium concepts while preserving the approved hierarchy and composition.

## Runtime review

After merge/deploy, desktop and mobile behavior must be inspected separately. Search/filter/selection, tab switching, responsive movement cards and disabled authoritative controls are part of runtime acceptance.
