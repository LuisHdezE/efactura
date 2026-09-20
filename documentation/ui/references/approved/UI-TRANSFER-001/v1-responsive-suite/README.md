# UI-TRANSFER-001 v1-responsive-suite — Approved visual working baseline

Status: `VISUAL_APPROVED / SOURCE_FINGERPRINTED / BINARY_PRESERVATION_PENDING`

Approved by Luis on 2026-09-20 with the explicit statement:

> Apruebo UI-TRANSFER-001 v1-responsive-suite como baseline visual

This approval promotes the exact previously governed candidate to the approved visual working baseline for `UI-TRANSFER-001`.

The approval is visual authority only. It does **not** authorize React route activation, live transfer integration or any server-backed transfer mutation while the authoritative transfer HTTP surface remains non-executable.

## Approved visual source

- source presentation filename: `a_clean_ui_ux_dashboard_screenshot_collage_showing.png`
- image generation id: `1015b786-078d-4aec-b0af-b560e26352ea`
- dimensions: `1312 x 1199`
- pixel mode: `RGB`
- source bytes: `1586620`
- SHA-256: `1c6826ce147e7fe4a4dd009c67ebdbd41275ed43bc6040be2cd81ac1dd8272ed`

This is the same exact fingerprint registered previously under:

`documentation/ui/references/drafts/UI-TRANSFER-001/v1-responsive-suite/`

The draft record is retained as governance history and must not be deleted or rewritten as though approval existed before Luis granted it.

The exact generated PNG is the approved visual source. It must not be silently regenerated, recompressed, substituted or represented as byte-identical evidence under the same version.

The current GitHub connector in this lane cannot directly insert the generated binary from the chat runtime into the repository. Therefore the approved source is fingerprinted, but physical byte-identical preservation remains `BINARY_PRESERVATION_PENDING`. The fingerprint identifies the approved source but does not replace the missing binary-preservation obligation.

## Approved visual authority

The approved suite establishes the visual direction for:

- the existing reusable eFactura shell rather than a forked transfer shell;
- `Transferencias` as the active visual context inside `Inventario y Compras`;
- desktop, tablet and mobile responsive treatment;
- paired light and dark themes;
- dense transfer list/table behavior on desktop;
- compact card/list behavior on mobile;
- clear source-to-destination directional reading;
- lifecycle/status badges with text, not color-only meaning;
- selected-transfer master-detail treatment;
- explicit discrepancy warning treatment;
- a visually reserved `Nueva transferencia` action location;
- restrained spacing, borders and hierarchy consistent with the governed eFactura WebApp family.

## Functional specification overrides illustrative mockup content

The approved image is visual authority, not backend authority. Implementation must obey:

- `documentation/ui/specifications/UI-TRANSFER-001_STOCK_TRANSFERS.md`;
- `documentation/ui/transfers/UI-TRANSFER-001_RECONCILIATION.md`;
- current executable API evidence.

`API-TRF-001..007` remain `MISSING_HTTP`. Therefore:

- `/transferencias` remains `PLANNED_DISABLED`;
- list rows, counts, dates, operator names, exact labels and locations shown in the visual remain illustrative/mock data;
- lifecycle labels shown in the baseline are governed visual concepts, not final wire enum guarantees;
- `Nueva transferencia` must not be enabled as a live command;
- approve, dispatch, receive and reconcile actions must not be presented as server-backed behavior;
- the baseline does not authorize invented carrier, ETA, freight, valuation, reservation, cancellation or discrepancy-resolution policy;
- the baseline does not authorize procurement/goods-receipt workflow inside this view.

When later executable transfer DTOs or transition contracts differ from illustrative details, functional/API authority prevails while the visual grammar should be preserved where compatible.

## Responsive scope

This single approved composition explicitly covers:

- desktop light mode;
- tablet light mode;
- mobile light mode;
- desktop dark mode;
- tablet dark mode;
- mobile dark mode.

It therefore establishes responsive and theme parity as part of the approved baseline rather than as optional later polish.

## Route and implementation boundary

The governed route remains reserved as:

`/transferencias`

Navigation remains `PLANNED_DISABLED`.

Visual approval does not satisfy the executable-capability gate. React route activation requires later evidence that `API-TRF-001..007` are executable, a fresh UI/API reconciliation, implementation under the shared shell and the normal merge/deploy/runtime review sequence.

## Change control

Any material redesign creates a new visual version and requires a new explicit approval. This `v1-responsive-suite` reference must not be silently replaced.
