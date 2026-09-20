# UI-CATALOG-001 v1-theme-pair — Approved visual authority

Status: `VISUAL_APPROVED / SOURCE_FINGERPRINTED / BINARY_PRESERVATION_PENDING`

Approved by Luis on 2026-09-20 with the explicit statement:

> Úsalo como baseline visual UI-CATALOG-001

This approval applies to the `UI-CATALOG-001 v1-theme-pair` light/dark composition presented immediately before the approval statement.

## Approved source fingerprint

- source presentation filename: `a_clean_split_screen_ui_design_mockup_with_two_si.png`
- image generation id: `2ceda7fa-b758-4301-a072-099ff4d5c8cb`
- dimensions: `1536 x 1024`
- source bytes: `1629592`
- SHA-256: `74aa441961fea09473d59b1f29237fdc2d5155940f1b22905a4ff1defc9a4364`

The exact approved PNG source is the visual authority. It must not be silently regenerated, substituted, recompressed or reinterpreted as a different approved version.

The current repository connector cannot transfer this generated binary directly into Git in this lane, so physical byte-identical preservation remains pending. The fingerprint above is not a substitute for the binary and the debt must remain visible until the exact source is committed.

## Governed visual interpretation

The approved composition establishes:

- paired desktop light and dark Products and Services catalog views;
- the existing compact reusable eFactura shell;
- `Productos y Servicios` visually active inside `Comercial`;
- dense searchable catalog table/list;
- compact type, category and state filters;
- clear `Producto` / `Servicio` distinction;
- unit, category, inventory-control behavior, fiscal-profile reference and active state;
- selected-item master-detail composition;
- restrained status badges and compact business-app density;
- no authoritative catalog price, cost, stock quantity, barcode, product media or supplier-item relationship.

The approved image is a visual authority, not backend authority. Sample item names, codes, descriptions, categories, units, dates, labels and statuses are illustrative fixtures unless independently supported by the executable contract.

## Functional boundary retained

Implementation must continue to obey `UI-CATALOG-001_PRODUCTS_SERVICES.md` and current executable API evidence.

In particular:

- `CommercialItem` remains the authoritative catalog concept for both `PRODUCT` and `SERVICE`;
- services cannot track inventory;
- products may track inventory or not;
- categories and tax profiles remain references governed by existing catalog contracts;
- units remain configurable commercial text with runtime suggestions, not a fabricated closed DGI enumeration;
- catalog price, cost, stock quantity, barcode/GTIN, item media, supplier-item linkage, variants and dimensions remain outside the v1 authoritative item contract;
- sale-line `unitPrice` and POS-local visual metadata do not become catalog-master fields by implication;
- any `Creado` or `Última actualización` values visible in the approved mockup are illustrative visual content only and must not be implemented as authoritative fields unless a later executable contract explicitly supports them;
- tabs, labels or actions present in the mockup do not authorize additional routes or backend operations.

## Shell boundary retained

`Sidebar`, `Topbar`, `BottomBar` and `MobileNavigation` remain shared platform components. The Catalog implementation extends the existing shell and must not repaint or fork it.

The route `/catalogo` remains non-executable until the implementation increment registers `UI-CATALOG-001` and converts the existing planned navigation entry to active.

## Implementation checkpoint

With visual approval complete, the next governed step is implementation of `UI-CATALOG-001` against this baseline, followed by Frontend Demo CI, repository gates, explicit merge authorization, deployment and runtime light/dark visual review.
