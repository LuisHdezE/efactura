# UI-CATALOG-001 — Runtime Visual Acceptance

Status: `VISUAL_RUNTIME_ACCEPTED`

Date: `2026-09-20`

Route: `/catalogo`

Accepted deployed WebApp commit: `f9280214e819a5bc0093972b223da9b828bc779f`

## Related governed evidence

- approved baseline: `UI-CATALOG-001 v1-theme-pair`;
- baseline record: `documentation/ui/references/approved/UI-CATALOG-001/v1-theme-pair/README.md`;
- source manifest: `documentation/ui/references/approved/UI-CATALOG-001/v1-theme-pair/visual-source-manifest.md`;
- visual-governance PR: `#176`;
- implementation PR: `#178`;
- runtime-polish PR: `#179`;
- deployed WebApp workflow: `Deploy eFactura Demo #30` — `SUCCESS`;
- post-merge frontend workflow: `Frontend Demo CI #69` — `SUCCESS`;
- post-merge architecture workflow: `Clean Architecture Guard #621` — `SUCCESS`.

## Human acceptance

Luis reviewed the deployed light and dark runtime after PR `#179` and explicitly approved closure with:

`Apruebo cierre runtime UI-CATALOG-001`

## Runtime scenarios reviewed

The deployed runtime was reviewed in both desktop themes with the reusable eFactura shell visible and `Productos y Servicios` active.

Observed and accepted behavior includes:

- compact searchable product/service catalog table;
- selected-item master-detail panel;
- type distinction between `Producto` and `Servicio`;
- category, unit, inventory-tracking behavior, tax profile and active/inactive state;
- service inventory invariant preserved without numeric stock presentation;
- future Sidebar entries remain visible but disabled/non-clickable;
- `Nuevo item`, category maintenance and edit affordances remain unavailable in the current demo/runtime integration state;
- no authoritative catalog price, cost, numeric stock, barcode/GTIN, media/image or supplier linkage is presented;
- light/dark geometry, density, contrast and selected-row treatment remain coherent with the approved visual authority.

## Runtime polish accepted

The first deployed review after PR `#178` identified three minor presentation issues. PR `#179` isolated and corrected them without changing contracts or behavior:

- native overflow chrome on the detail tab strip was removed on desktop;
- the selected-item detail panel was extended to use the available viewport more effectively;
- the disabled `Nuevo item` action was visually subdued so it no longer reads as an available primary CTA.

The final light/dark review confirmed those corrections without a visual regression requiring another polish increment.

## Decision

`UI-CATALOG-001` closes its governed visual/runtime implementation lane as:

`REVIEWED / VISUAL_RUNTIME_ACCEPTED / TRACEABILITY_GAPS_RECORDED / BINARY_PRESERVATION_PENDING`

The route remains `ACTIVE` at `/catalogo`.

This runtime acceptance does **not** mean that live catalog write integration is enabled. The current WebApp still uses explicit demo/mock data where the governed integration lane has not been enabled, and write affordances remain disabled rather than fabricating backend behavior.

This runtime acceptance also does **not** promote the view to final repository-wide `ACCEPTED`.

## Independent governance debt that remains open

- no governed dedicated `US-*` catalog-master artifact is currently evidenced;
- no dedicated governed `UC-CATALOG-*` lifecycle is currently evidenced;
- the exact approved source PNG remains `BINARY_PRESERVATION_PENDING` even though its fingerprint and generation metadata are governed.

These debts do not reopen the accepted runtime lane, but they must not be silently represented as resolved or fabricated by frontend documentation.
