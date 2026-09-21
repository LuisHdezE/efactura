# UI-RECEIVABLE-001 — Governed UI Inventory Entry

Status: `SPECIFICATION_READY / VISUAL_BASELINE_APPROVED / ACTIVE_VISUAL_PREVIEW`

Mapping:

```text
WEB-010 -> UI-RECEIVABLE-001
```

Route: `/cuentas-por-cobrar`

Navigation state: `ACTIVE_VISUAL_PREVIEW`

## Evidence

- upstream scope: `documentation/blueprint-interface/interface-scope-baseline.json` (`WEB-010`);
- requirements: `FR-023`, `FR-026`, `FR-027`, `FR-070`, `FR-072`, `FR-073`, `FR-074`;
- source receivable lifecycle: `UC-SALE-003 — Confirm credit sale and create receivable`;
- collection lifecycle: `UC-AR-001 — Record customer collection and allocate payment`;
- reconciliation: `documentation/ui/receivables/UI-RECEIVABLE-001_RECONCILIATION.md`;
- specification: `documentation/ui/specifications/UI-RECEIVABLE-001_ACCOUNTS_RECEIVABLE_COLLECTIONS.md`;
- approved visual reference: `documentation/ui/references/approved/UI-RECEIVABLE-001/v1-approved-view/README.md`;
- preview-route authority: `documentation/ui/PREVIEW_ROUTE_POLICY_AMENDMENT.md`;
- route/navigation authority: `documentation/ui/WEBAPP_NAVIGATION_MAP.md`.

## Approved visual baseline

Luis explicitly approved the generated `Cuentas por cobrar` view on 2026-09-21 with:

> correcto, aprobada esta vista

Approved artifact identity:

- baseline: `UI-RECEIVABLE-001 / v1-approved-view`;
- image generation id: `1fad639a-815f-431b-8c1c-7e6a50cdae77`;
- scope: the exact approved accounts-receivable composition showing aging KPIs, receivable list, selected-account detail, collection history, disabled/demo collection composer and the mobile inset represented in the approved image.

The approved composition is visual authority. The implementation may adapt responsively inside the shared eFactura shell and global theme, but it must not reinterpret financial policy or imply live API readiness.

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

## Visual-preview boundary

The active preview is allowed to provide:

- explicit local demonstration fixtures;
- client-side search and status filtering;
- selection of a receivable fixture;
- master/detail navigation;
- visual aging/KPI cards clearly marked as demonstration values;
- visual collection/allocation composition with disabled controls;
- light/dark adaptation through the existing global shell theme;
- responsive desktop/tablet/mobile layout.

The preview does not register any `API-AR-*` or `API-COL-*` operation as executable.

## Financial-history boundary

The governed view preserves:

- original obligation amount as historical fact;
- allocations and adjustments as append-only evidence;
- open balance as derived/server-authoritative state when live integration exists;
- partial/full collection history;
- explicit overpayment/advance policy results;
- compensating reversal instead of destructive deletion.

Demo fixtures are presentation examples only and are never authoritative financial state.

## Execution gate

Route activation as `IMPLEMENTED_VISUAL_PREVIEW` does **not** authorize:

- server-backed receivable/aging claims;
- collection posting;
- receivable adjustment;
- collection reversal;
- local authoritative balance mutation;
- silent overpayment truncation;
- cash-shift reconciliation;
- supplier-payment behavior.

All server-owned mutation controls remain disabled.

## Next gate

After merge and deployment, perform runtime visual review of `/cuentas-por-cobrar` in the canonical `https://efactura.eliasworks.uy/` WebApp.

When executable receivables/collections APIs become available, live integration will require fresh DTO/error/permission/idempotency/concurrency reconciliation before promotion away from `IMPLEMENTED_VISUAL_PREVIEW`.
