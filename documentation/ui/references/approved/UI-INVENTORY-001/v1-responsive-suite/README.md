# UI-INVENTORY-001 v1-responsive-suite — Approved visual working baseline

Status: `VISUAL_APPROVED / SOURCE_FINGERPRINTED / BINARY_PRESERVATION_PENDING`

Approved by Luis on 2026-09-20 with the explicit statement:

> ya esta bien, podemos pasar a usar esas propuestas

This approval authorizes the presented proposals as the visual working baseline for implementing `UI-INVENTORY-001`, while the functional specification remains authoritative whenever an illustrative mockup element exceeds current executable contracts.

## Approved visual sources

### 1. Desktop light/dark composition

- source presentation filename: `a_wide_composite_ui_mockup_screenshot_of_an_invent.png`
- image generation id: `f445a31a-650e-48d6-b23d-8e699df13461`
- dimensions: `1536 x 1024`
- source bytes: `1555080`
- SHA-256: `ef59b96bb06911985dd2872f99ba68227fedaeef2a1ecef843eb714228f97abb`

### 2. Mobile light/dark composition

- source presentation filename: `inventario_móvil_en_modo_claro_y_oscuro.png`
- image generation id: `67d15647-44c0-47e2-92a2-df2859a1412a`
- dimensions: `1122 x 1402`
- source bytes: `1541255`
- SHA-256: `7f671a2af019a4f0d286d7e870000d47632106b65288cc04051d5bdd5c322ae0`

### 3. Mobile responsive flow board

- source presentation filename: `a_clean_high_resolution_ui_ux_mockup_image_showin.png`
- image generation id: `13d575d2-48bb-4418-b104-e9a06b12d327`
- dimensions: `1226 x 1283`
- source bytes: `1682055`
- SHA-256: `44d5ee81464fd14d67a13607824c1629f1a653df51bbabf08f1b24af2a66569c`

The exact source PNGs are the approved visual references. They must not be silently regenerated, substituted, recompressed or represented as byte-identical copies.

The current repository connector cannot insert these generated binary assets directly into Git, so physical byte-identical preservation remains pending. The fingerprints above identify the approved sources but do not replace the missing binary preservation.

## Visual authority

The approved suite establishes:

- the existing compact reusable eFactura shell rather than a parallel inventory shell;
- paired light and dark desktop treatment;
- responsive mobile treatment with list-first navigation and detail/action surfaces;
- `Inventario` visually active inside `Inventario y Compras`;
- a dense operational stock-position list;
- clear selection and quantity hierarchy;
- master-detail behavior on desktop;
- card/list-first inspection on mobile;
- compact tabs for position information and movement history;
- an explicit manual-adjustment interaction surface when the executable write lane is enabled;
- restrained badges, borders and spacing consistent with the already accepted WebApp family.

## Functional specification overrides illustrative mockup content

The images are visual references, not backend authority. Implementation must obey:

- `documentation/ui/specifications/UI-INVENTORY-001_INVENTORY_MOVEMENTS.md`;
- `documentation/ui/inventory/UI-INVENTORY-001_RECONCILIATION.md`;
- current executable API evidence.

Therefore the following elements visible in one or more mockups are **illustrative only** and do not authorize implementation as authoritative features:

- `Stock bajo`, `Disponible` or `Sin stock` classification derived from thresholds that the current contract does not expose;
- category, state, type, sign, date or other filters not supported by the inventory endpoints;
- `Exportar` action;
- generic `Editar` action for an inventory position;
- product imagery/media as inventory authority;
- purchase/receipt movement labels unless and until the HTTP projection actually exposes such movement kinds;
- reserved stock, available-to-sell stock, reorder point, EOQ/ROP, in-transit quantity or monetary valuation;
- any aggregate/global stock total inferred from a paginated page;
- any negative-stock policy not explicitly governed by the backend.

When sample code/name/category/unit metadata appears, it is presentation enrichment only. `InventoryPositionDto` remains authoritative for position id, version, item id, location id and quantity. Catalog/location labels may enrich the display only when separately authorized and available.

## Supported v1 interaction model

Implementation should preserve the visual grammar while limiting executable behavior to the governed v1 boundary:

- list positions by authorized item/location scope;
- inspect current quantity and version;
- inspect immutable movement history through the current HTTP projection;
- create a manual stock adjustment only when `inventory.adjust` and the governed write integration are available;
- show stale-version/concurrency conflict without silently overwriting newer stock state.

Stock transfers remain `WEB-008`. Procurement/goods receipt remains `WEB-009`. EOQ/ROP remains non-executable while `API-RPL-001/002` are `MISSING_HTTP`.

## Shell and route boundary

Shared `Sidebar`, `Topbar`, `BottomBar` and `MobileNavigation` remain platform components and must not be forked for Inventory.

The governed route remains `/inventario`, but it stays `PLANNED_DISABLED` until implementation registers `UI-INVENTORY-001` in the unified route/capability registry and passes the normal merge/deploy/runtime gates.

## Implementation checkpoint

With this visual suite approved, the next governed step is frontend implementation against the functional specification and these visual references. Any disagreement between the images and the specification is resolved in favor of the specification, not by inventing backend behavior.