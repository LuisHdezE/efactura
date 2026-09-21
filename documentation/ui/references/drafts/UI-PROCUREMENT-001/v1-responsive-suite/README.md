# UI-PROCUREMENT-001 v1-responsive-suite — Governed visual candidate history

Status: `VISUAL_CANDIDATE_HISTORY / PROMOTED_TO_APPROVED / SOURCE_FINGERPRINTED / BINARY_PRESERVATION_PENDING`

## Governance identity

- UI: `UI-PROCUREMENT-001`
- upstream mapping: `WEB-009 -> UI-PROCUREMENT-001`
- version: `v1-responsive-suite`
- reserved route: `/compras`
- navigation state: `PLANNED_DISABLED`
- generated source file: `a_clean_high_fidelity_ui_ux_mockup_board_for_an_e.png`
- generation id: `4ee17308-4cac-4f95-b3f6-84c8f8ff1f6c`
- dimensions: `1312 x 1199`
- bytes: `1666762`
- SHA-256: `a0f59fcc7b4a54f9bf9cd966b8dffad7165178ff9861cfe7f6d13266a797c6a5`

The fingerprint above identifies the exact generated candidate reviewed in the ChatGPT WebApp governance lane. No regenerated or visually similar image may silently replace this candidate under the same version.

## Candidate coverage

The visual board contains a coordinated responsive suite for:

- desktop light mode;
- tablet light mode;
- mobile light mode;
- desktop dark mode;
- tablet dark mode;
- mobile dark mode.

The candidate preserves the established eFactura visual language and proposes a `Compras` workspace with:

- reusable eFactura shell/navigation;
- purchase-order list and filters;
- selected purchase-order detail;
- lifecycle/status presentation;
- supplier context;
- order total/currency placement as illustrative content;
- purchase-order progress toward receipt;
- discrepancy visibility;
- responsive mobile card treatment;
- light/dark parity.

## Functional interpretation

All purchase-order, supplier, quantity, amount, status, date and receipt values shown in the board are illustrative visual fixtures. They are not evidence of executable server-backed procurement data.

The candidate may be used to evaluate hierarchy, density, responsive behavior, lifecycle clarity, discrepancy treatment and visual consistency only.

It does not authorize live behavior for:

- create purchase order;
- update purchase order;
- approve purchase order;
- cancel purchase order;
- create goods receipt;
- post goods receipt;
- inventory mutation;
- costing mutation/calculation authority;
- payable mutation;
- received-CFE processing.

Those behaviors remain blocked while `API-PRC-001..006` and `API-GRC-001..004` remain `MISSING_HTTP`.

## Promotion record

Luis explicitly approved this exact candidate with:

> Apruebo UI-PROCUREMENT-001 v1-responsive-suite como baseline visual

The exact same fingerprint is now promoted to:

`documentation/ui/references/approved/UI-PROCUREMENT-001/v1-responsive-suite/`

This file remains as immutable candidate-history evidence and must not be interpreted as a second visual version.

## Scope constraints preserved

The promoted baseline must continue to be interpreted together with:

- `documentation/ui/procurement/UI-PROCUREMENT-001_RECONCILIATION.md`;
- `documentation/ui/specifications/UI-PROCUREMENT-001_PURCHASE_ORDERS_RECEIPTS.md`;
- `documentation/ui/WEBAPP_NAVIGATION_MAP.md`.

In particular:

- `/compras` remains `PLANNED_DISABLED`;
- visual approval preserves visual authority only;
- React route activation requires executable procurement/receipt API evidence and a fresh reconciliation;
- the frontend must not become authority for stock, costing or accounts-payable effects.

## Binary preservation

The exact PNG exists in the current ChatGPT runtime and is cryptographically fingerprinted above. The current repository workflow in this lane has not inserted the binary bytes into Git, therefore:

`BINARY_PRESERVATION_PENDING`

remains explicit.

Until byte-identical preservation is completed, the SHA-256, dimensions, byte count and generation id are the immutable source identity. Regeneration or substitution is forbidden.