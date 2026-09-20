# UI-INVENTORY-001 — Runtime Visual Acceptance

Status: `VISUAL_RUNTIME_ACCEPTED`

Date: `2026-09-20`

Route: `/inventario`

Accepted deployed WebApp commit: `45a57dab16d27d6d455aeebef895c495968da63b`

## Related governed evidence

- approved baseline: `UI-INVENTORY-001 v1-responsive-suite`;
- baseline record: `documentation/ui/references/approved/UI-INVENTORY-001/v1-responsive-suite/README.md`;
- visual-governance PR: `#183`;
- implementation PR: `#184`;
- deployed WebApp workflow: `Deploy eFactura Demo #31` — `SUCCESS`;
- post-merge frontend workflow: `Frontend Demo CI #72` — `SUCCESS`;
- post-merge architecture workflow: `Clean Architecture Guard #638` — `SUCCESS`.

## Human acceptance

Luis reviewed the deployed inventory runtime and explicitly approved closure with:

`Apruebo cierre runtime UI-INVENTORY-001`

## Runtime evidence reviewed

Two deployed desktop screenshots were supplied in the runtime review, one in light theme and one in dark theme, both at `/inventario` with the reusable eFactura shell visible and `Inventario` active.

Observed and accepted desktop behavior includes:

- compact stock-position table with article, location, quantity and version;
- selected-position master-detail panel;
- current quantity, version, location and position identifier presented without inventing reserved or available-to-sell quantities;
- Catalog code/name enrichment explicitly identified as presentation enrichment rather than Inventory authority;
- quantity semantics explicitly state that the view does not fabricate reservation, available-to-sell, minimum, EOQ or valuation data;
- `Ajustar stock` remains visible but disabled while governed WebApp write integration is not enabled;
- no `Stock bajo`, `Exportar`, generic `Editar`, transfer, procurement, replenishment, EOQ/ROP or valuation capability is presented as executable;
- future Sidebar entries remain visible but disabled;
- light/dark hierarchy, selection treatment, density and contrast are coherent with the approved visual family.

## Responsive evidence note

`UI-INVENTORY-001 v1-responsive-suite` includes approved mobile light/dark and responsive-flow visual references, but separate deployed mobile runtime screenshots were not supplied in this closure thread.

Therefore this record does **not** claim that the assistant independently inspected deployed mobile runtime pixels. Runtime closure is recorded because Luis, as product owner, explicitly approved `UI-INVENTORY-001` closure after reviewing the deployed result.

The absence of separately archived mobile runtime screenshots is retained as an evidence limitation and must not be rewritten later as if mobile screenshots had been independently reviewed here.

## Decision

`UI-INVENTORY-001` closes its governed visual/runtime implementation lane as:

`REVIEWED / VISUAL_RUNTIME_ACCEPTED / TRACEABILITY_GAPS_RECORDED / BINARY_PRESERVATION_PENDING`

The route remains `ACTIVE` at `/inventario`.

This runtime acceptance does **not** mean that live stock-adjustment write integration is enabled. The current demo remains explicit about mock/demo data where governed integration is not enabled, and `Ajustar stock` stays disabled rather than fabricating mutation behavior.

This runtime acceptance also does **not** promote the view to final repository-wide `ACCEPTED`.

## Independent governance and execution debt that remains open

- no governed dedicated `US-INV-*` story artifact is currently evidenced;
- `API-INV-003` retains the known non-`Adjustment` movement projection gap until backend mapping is reconciled;
- live WebApp write integration for `API-INV-004` remains intentionally disabled in the current demo lane;
- separate deployed mobile runtime screenshots were not archived in this closure record;
- the exact approved source PNGs remain `BINARY_PRESERVATION_PENDING` even though fingerprints and generation metadata are governed.

These debts and evidence limitations do not reopen the owner-approved runtime lane, but none may be silently represented as resolved or fabricated by frontend documentation.
