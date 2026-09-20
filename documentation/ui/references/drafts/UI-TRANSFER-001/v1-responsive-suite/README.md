# UI-TRANSFER-001 v1-responsive-suite — Governed visual candidate

Status: `VISUAL_CANDIDATE / SOURCE_FINGERPRINTED / NOT_APPROVED / BINARY_PRESERVATION_PENDING`

Registered by Luis on 2026-09-20 with the explicit instruction:

> Registra esta suite como candidato visual gobernado

This instruction registers the presented suite as the governed visual candidate for `UI-TRANSFER-001`. It does **not** approve the candidate, promote it to `references/approved`, authorize React route activation, or authorize any live transfer behavior.

## Candidate source

- source presentation filename: `a_clean_ui_ux_dashboard_screenshot_collage_showing.png`
- image generation id: `1015b786-078d-4aec-b0af-b560e26352ea`
- dimensions: `1312 x 1199`
- pixel mode: `RGB`
- source bytes: `1586620`
- SHA-256: `1c6826ce147e7fe4a4dd009c67ebdbd41275ed43bc6040be2cd81ac1dd8272ed`

The exact generated PNG is the candidate visual source. It must not be silently regenerated, recompressed, substituted or represented as byte-identical evidence under the same version.

The current GitHub connector in this lane cannot directly insert the generated binary from the chat runtime into the repository. Therefore the candidate is source-fingerprinted but physical byte-identical preservation remains `BINARY_PRESERVATION_PENDING`. This debt must remain explicit until the exact PNG is stored in repository evidence.

## Candidate visual scope

The suite presents one responsive composition covering:

- desktop light mode;
- tablet light mode;
- mobile light mode;
- desktop dark mode;
- tablet dark mode;
- mobile dark mode.

The candidate establishes the intended visual grammar for:

- the existing reusable eFactura shell rather than a forked transfer shell;
- `Transferencias` as the active visual context inside `Inventario y Compras`;
- dense transfer list/table behavior on desktop;
- compact card/list behavior on mobile;
- source and destination visibility;
- lifecycle/status badges;
- selected transfer master-detail treatment;
- discrepancy warning treatment;
- light/dark parity;
- responsive layout across desktop, tablet and mobile;
- a visually reserved `Nueva transferencia` action location.

## Functional authority and restrictions

This image is a visual candidate, not backend authority. Functional truth remains governed by:

- `documentation/ui/specifications/UI-TRANSFER-001_STOCK_TRANSFERS.md`;
- `documentation/ui/transfers/UI-TRANSFER-001_RECONCILIATION.md`;
- current executable API evidence.

`API-TRF-001..007` remain `MISSING_HTTP`. Therefore:

- `/transferencias` remains `PLANNED_DISABLED`;
- the visual list content is illustrative/mock data;
- lifecycle labels shown in the candidate are visual concepts and are not final wire enum guarantees;
- `Nueva transferencia` must not be enabled as a live action;
- no approve/dispatch/receive/reconcile action may be presented as server-backed behavior;
- no visual element may be used to invent unsupported transport, ETA, valuation, cancellation, reservation or discrepancy policy.

## Candidate-specific review notes

The candidate intentionally demonstrates:

- clear transfer identity such as `TRF-0012`;
- source-to-destination directional reading;
- status segmentation such as draft, pending approval, approved, dispatched, received and discrepancy;
- selected detail context with tabs/sections;
- explicit discrepancy alerting;
- a compact responsive mobile representation;
- consistent light/dark shell treatment.

Elements such as counts, dates, operator names, exact state labels and sample locations are illustrative presentation data until executable transfer DTOs define authoritative values.

## Approval gate

This version is **not approved**.

If Luis explicitly approves this exact candidate, it may be promoted under:

`documentation/ui/references/approved/UI-TRANSFER-001/v1-responsive-suite/`

Approval must identify this exact version and source fingerprint. Promotion must preserve the candidate history and must not imply route activation while the transfer HTTP surface remains non-executable.
