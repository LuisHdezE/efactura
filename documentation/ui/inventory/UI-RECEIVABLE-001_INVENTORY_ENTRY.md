# UI-RECEIVABLE-001 — Governed UI Inventory Entry

Status: `SPECIFICATION_READY / VISUAL_BASELINE_PENDING / EXECUTION_BLOCKED_BY_API`

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
- route/navigation authority: `documentation/ui/WEBAPP_NAVIGATION_MAP.md`.

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

This inventory entry authorizes visual-baseline work only. It does not authorize:

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

Create and review a responsive light/dark visual candidate for `UI-RECEIVABLE-001`.

Visual approval remains separate from route activation. Before implementation, executable receivables/collections HTTP evidence and fresh UI/API reconciliation are required.