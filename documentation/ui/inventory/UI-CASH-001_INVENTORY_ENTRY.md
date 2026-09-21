# UI-CASH-001 — Governed UI Inventory Entry

Status: `SPECIFICATION_READY / VISUAL_BASELINE_APPROVED / ROUTE_PLANNED_DISABLED`

Mapping:

```text
WEB-012 -> UI-CASH-001
```

Reserved route candidate: `/caja`

Navigation state: `PLANNED_DISABLED`

## Evidence

- upstream scope: `documentation/blueprint-interface/interface-scope-baseline.json` (`WEB-012`);
- requirements: `FR-025`, `FR-026`, `FR-027`;
- lifecycles: `UC-CASH-001`, `UC-CASH-002`;
- API matrix: `documentation/api-completion-matrix/WAVE_3.md`;
- reconciliation: `documentation/ui/cash/UI-CASH-001_RECONCILIATION.md`;
- specification: `documentation/ui/specifications/UI-CASH-001_CASH_SHIFT_RECONCILIATION.md`;
- approved visual reference: `documentation/ui/references/approved/UI-CASH-001/v1-responsive-composite/README.md`.

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

## Current execution boundary

The route remains planned/disabled. This inventory approval does **not** authorize:

- canonical shift opening/closing;
- manual cash posting;
- variance reconciliation;
- persistence of counted values;
- invented tolerance/approval decisions;
- authoritative balance or bank-account claims.

A future preview may use explicit deterministic local fixtures only after preview-route governance is reconciled and a real responsive React surface is implemented.

## Next gate

Reconcile `UI-CASH-001` with `PREVIEW_ROUTE_POLICY_AMENDMENT.md`, then implement `/caja` as `IMPLEMENTED_VISUAL_PREVIEW` with `operations: []`, disabled cash commands and a dedicated source-level CI guard.