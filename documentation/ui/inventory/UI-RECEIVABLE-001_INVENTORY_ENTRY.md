# UI-RECEIVABLE-001 — Governed UI Inventory Entry

Status: `SPECIFICATION_READY / VISUAL_BASELINE_APPROVED / EXECUTION_BLOCKED_BY_API`

Mapping:

```text
WEB-010 -> UI-RECEIVABLE-001
```

Reserved route: `/cuentas-por-cobrar`

Navigation state: `PLANNED_DISABLED`

## Evidence

- upstream scope: `documentation/blueprint-interface/interface-scope-baseline.json` (`WEB-010`);
- requirements: `FR-023`, `FR-026`, `FR-027`, `FR-070`, `FR-072`, `FR-073`, `FR-074`;
- source receivable lifecycle: `UC-SALE-003 — Confirm credit sale and create receivable`;
- collection lifecycle: `UC-AR-001 — Record customer collection and allocate payment`;
- reconciliation: `documentation/ui/receivables/UI-RECEIVABLE-001_RECONCILIATION.md`;
- specification: `documentation/ui/specifications/UI-RECEIVABLE-001_ACCOUNTS_RECEIVABLE_COLLECTIONS.md`;
- approved visual reference: `documentation/ui/references/approved/UI-RECEIVABLE-001/v1-approved-view/README.md`;
- route/navigation authority: `documentation/ui/WEBAPP_NAVIGATION_MAP.md`.

## Approved visual baseline

Luis explicitly approved the generated `Cuentas por cobrar` view on 2026-09-21 with:

> correcto, aprobada esta vista

Approved artifact identity:

- baseline: `UI-RECEIVABLE-001 / v1-approved-view`;
- image generation id: `1fad639a-815f-431b-8c1c-7e6a50cdae77`;
- scope: the exact approved accounts-receivable composition showing aging KPIs, receivable list, selected-account detail, collection history, disabled/demo collection composer and the mobile inset represented in the approved image.

This approval is visual authority only. It does not activate `/cuentas-por-cobrar` and does not authorize any server-owned financial mutation.

## Accepted API dependency

The view depends on:

- `API-AR-001` list receivables;
- `API-AR-002` receivable detail;
- `API-AR-003` receivables aging;
- `API-AR-004` append receivable adjustment;
- `API-COL-001` create collection/allocation;
- `API-COL-002` collection detail;
- `API-COL-003` compensating collection reversal.

Wave 3 currently records all seven as `MISSING_HTTP` with no WebApi evidence and `NOT_YET_AUDITED` deep readiness.

## Financial-history boundary

The governed view preserves:

- original obligation amount as historical fact;
- allocations and adjustments as append-only evidence;
- open balance as derived/server-authoritative state;
- partial/full collection history;
- explicit overpayment/advance policy results;
- compensating reversal instead of destructive deletion.

## Execution gate

This inventory entry currently authorizes the approved visual baseline only. It does not authorize:

- React route activation;
- server-backed receivable/aging claims;
- collection posting;
- receivable adjustment;
- collection reversal;
- local balance mutation;
- silent overpayment truncation;
- cash-shift reconciliation;
- supplier-payment behavior.

## Next gate

Reconcile `UI-RECEIVABLE-001` against the active visual-preview route policy before any React preview implementation or route activation.

The existing `PREVIEW_ROUTE_POLICY_AMENDMENT.md` established a general visual-preview model but its explicit supersession scope currently names only `UI-TRANSFER-001` and `UI-PROCUREMENT-001`. `UI-RECEIVABLE-001` therefore remains `PLANNED_DISABLED` until that governance boundary is explicitly reconciled.

When executable receivables/collections APIs become available, live integration will still require fresh DTO/error/permission/idempotency/concurrency reconciliation.