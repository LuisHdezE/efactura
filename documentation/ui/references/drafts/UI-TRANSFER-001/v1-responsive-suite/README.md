# UI-TRANSFER-001 v1-responsive-suite - Governed visual candidate history

Status: `VISUAL_CANDIDATE_HISTORY / PROMOTED_TO_APPROVED / SOURCE_FINGERPRINTED / BINARY_PRESERVATION_PENDING`

Originally registered by Luis on 2026-09-20 with the explicit instruction:

> Registra esta suite como candidato visual gobernado

Luis later approved this exact candidate on 2026-09-20 with:

> Apruebo UI-TRANSFER-001 v1-responsive-suite como baseline visual

This file preserves the candidate-stage history. The approved authority now lives at:

`documentation/ui/references/approved/UI-TRANSFER-001/v1-responsive-suite/README.md`

The promotion does not change the candidate source identity.

## Candidate source

- source presentation filename: `a_clean_ui_ux_dashboard_screenshot_collage_showing.png`
- image generation id: `1015b786-078d-4aec-b0af-b560e26352ea`
- dimensions: `1312 x 1199`
- pixel mode: `RGB`
- source bytes: `1586620`
- SHA-256: `1c6826ce147e7fe4a4dd009c67ebdbd41275ed43bc6040be2cd81ac1dd8272ed`

The exact generated PNG is the source that was reviewed and approved. It must not be silently regenerated, recompressed, substituted or represented as byte-identical evidence under the same version.

The current GitHub connector in this lane cannot directly insert the generated binary from the chat runtime into the repository. Therefore physical byte-identical preservation remains `BINARY_PRESERVATION_PENDING`.

## Candidate visual scope

The suite presents one responsive composition covering:

- desktop light mode;
- tablet light mode;
- mobile light mode;
- desktop dark mode;
- tablet dark mode;
- mobile dark mode.

The candidate established the intended visual grammar for:

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

This visual never becomes backend authority. Functional truth remains governed by:

- `documentation/ui/specifications/UI-TRANSFER-001_STOCK_TRANSFERS.md`;
- `documentation/ui/transfers/UI-TRANSFER-001_RECONCILIATION.md`;
- current executable API evidence.

`API-TRF-001..007` remain `MISSING_HTTP`. Therefore:

- `/transferencias` remains `PLANNED_DISABLED`;
- the visual list content is illustrative/mock data;
- lifecycle labels shown are visual concepts and are not final wire enum guarantees;
- `Nueva transferencia` must not be enabled as a live action;
- no approve/dispatch/receive/reconcile action may be presented as server-backed behavior;
- no visual element may be used to invent unsupported transport, ETA, valuation, cancellation, reservation or discrepancy policy.

## Promotion outcome

This exact candidate was promoted to the approved visual baseline without changing its version, generation id, dimensions, byte count or SHA-256 fingerprint.

The draft history remains intentionally preserved so the repository records both the candidate stage and the later human approval event.
