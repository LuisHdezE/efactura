# UI-SUPPLIER-001 v1-theme-pair — Approved visual authority

Status: `VISUAL_APPROVED / SOURCE_FINGERPRINTED / BINARY_PRESERVATION_PENDING`

Approved by Luis on 2026-09-19 with the explicit statement:

> Úsalo como baseline visual UI-SUPPLIER-001.

This approval applies to the `UI-SUPPLIER-001 v1-theme-pair` light/dark composition presented immediately before the approval statement.

## Approved source fingerprint

- source presentation filename: `wide_split_screen_ui_mockup_showing_two_versions.png`
- image generation id: `742b47d8-fc9b-47ef-b30c-08af759d453e`
- dimensions: `1536 x 1024`
- source bytes: `1575821`
- SHA-256: `5eba4cf03854c7804bacd1ab31186a1a0817a92f0730d3679a814538f0c26f8b`

The exact approved PNG source is the visual authority. It must not be silently regenerated, substituted or reinterpreted as a different approved version.

The current repository connector cannot transfer this generated binary directly into Git in this lane, so physical byte-identical preservation remains pending. The fingerprint above is not a substitute for the binary and the debt must remain visible until the exact source is committed.

## Governed visual interpretation

The approved composition establishes:

- paired desktop light and dark Supplier management views;
- the existing compact reusable eFactura shell;
- `Proveedores` visually active inside `Comercial`;
- dense searchable supplier list/table;
- compact filters;
- `Nuevo proveedor` primary action;
- master-detail composition with selected supplier detail at right;
- explicit Party identity/fiscal/contact information;
- status badges and restrained iconography;
- no large decorative cards that waste working space.

The approved image is a visual authority, not backend authority. Sample names, RUT/document values, contacts, dates, websites, labels and status values are illustrative fixtures.

## Functional boundary retained

Implementation must continue to obey `UI-SUPPLIER-001_SUPPLIERS.md` and the current executable API evidence.

In particular:

- Supplier remains a `Party` with `SUPPLIER` role;
- a Party may also carry `CUSTOMER` without duplication;
- list semantics are based on Party filtering with `role=SUPPLIER`;
- Party identities, addresses and contacts remain governed by existing Party contracts;
- country and fiscal-identity-type references may use their real authenticated contracts when the WebApp integration path supports them;
- no payable balance, aging, purchase-order totals, received-CFE summary, source-document counts or other unsupported projections may be fabricated from the mockup;
- tabs or labels present in the mockup do not authorize additional routes or backend operations.

## Shell boundary retained

`Sidebar`, `Topbar`, `BottomBar` and `MobileNavigation` remain shared platform components. The Supplier implementation extends the existing shell and must not repaint or fork it.

The route `/proveedores` remains non-executable until the implementation increment registers `UI-SUPPLIER-001` and converts the existing planned navigation entry to active.

## Implementation checkpoint

With visual approval complete, the next governed step is implementation of `UI-SUPPLIER-001` against this baseline, followed by Frontend Demo CI, repository gates, explicit merge authorization, deployment and runtime visual review.
