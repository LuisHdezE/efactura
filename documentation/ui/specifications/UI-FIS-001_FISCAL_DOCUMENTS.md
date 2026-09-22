# UI-FIS-001 — Fiscal Documents

Status: `SPECIFICATION_READY / VISUAL_BASELINE_APPROVED`

WEB mapping: `WEB-013`

Candidate route: `/documentos-fiscales`

Approved visual baseline: `v1-responsive-composite`

Approved artifact gen_id: `f7f7b77c-d6c9-4366-9c1d-7767990c4746`

## Purpose

Provide one governed fiscal-document workspace to search and inspect CFE identity, immutable fiscal snapshot, lifecycle/transport/result evidence, artifacts, references and related correction/regularization/delivery context without allowing the client to invent fiscal authority.

## Primary roles

- cashier;
- seller;
- accountant;
- fiscal administrator;
- auditor.

## Header and shell

The view lives inside the shared eFactura application shell under the `Fiscal` product group.

Header content:

- breadcrumb `Fiscal > Documentos fiscales`;
- title `Documentos fiscales`;
- concise purpose copy explaining CFE status and representations;
- explicit preview/API-pending indicator while authoritative HTTP coverage is missing.

## Summary cards

The approved visual reserves four summary cards:

- total documents;
- accepted;
- in process;
- rejected.

In visual-preview mode these values are demonstration-only and must be labeled/contained so they cannot be interpreted as live DGI totals. Once API integration exists, status semantics must come from the server contract rather than client inference.

## Search and filters

The working surface should provide client-side demo controls for:

- document number;
- RUT/customer;
- CFE type;
- lifecycle/result state;
- issue-date range.

Search/filtering in preview mode operates only over local fixtures.

## Fiscal document ledger

Desktop uses a dense table. Minimum columns from the approved composition:

- issue date;
- type;
- number;
- customer / RUT;
- amount and currency;
- state;
- compact action affordances.

Mobile replaces the dense table with stacked cards preserving:

- date;
- fiscal identity/number;
- customer;
- amount;
- textual state.

Selection is local UI state only until `API-FIS-001/002` exist.

## Selected document detail

The desktop detail panel and mobile detail flow should be able to present, from demo fixtures:

- document number/fiscal identity;
- type;
- issue timestamp;
- customer/receiver and RUT;
- total amount/currency;
- illustrative lifecycle/result state;
- CAE metadata where visually useful;
- reference metadata;
- observations.

No local fixture is canonical fiscal evidence.

## Lifecycle and events

A dedicated tab/section is reserved for document events and traceability.

Preview mode may display local chronological fixture events for presentation validation. It must not claim those events came from DGI, transport infrastructure or `API-FIS-005`.

## Representation and XML

The approved visual reserves affordances for:

- view/download representation;
- download XML.

While `API-FIS-003` and `API-FIS-004` remain `MISSING_HTTP`, these controls must remain disabled or explicitly non-executable demo affordances. The client must not synthesize an authoritative XML or fiscal representation.

## Correction and regularization

The view may reserve visual placement for:

- correction request;
- regularization case inspection;
- regularization disposition.

`API-FIS-006..009` remain server authority. Preview mode must not create, resolve or locally mutate correction/regularization state.

## Delivery context

The view may display delivery-attempt context or reserve a delivery action area.

`API-FDL-001/002` remain server authority. Preview mode must not claim real delivery attempts or send delivery requests.

## Export boundary

The approved visual contains an `Exportar` affordance. No accepted `WEB-013` bulk-export operation is currently mapped. Therefore the preview must not implement a server export or imply one exists. The affordance may be omitted, disabled or clearly marked illustrative without changing the approved hierarchy.

## States

Required product states:

- default;
- loading;
- success;
- error;
- 403;
- 409;
- 422.

The visual preview focuses on deterministic local demonstration states. Live error/authorization/conflict semantics wait for API integration.

## Responsive behavior

Desktop:

- summary cards in a horizontal grid;
- ledger and selected-detail panel side by side;
- tabbed fiscal workspace;
- compact but readable fiscal identity and monetary data.

Mobile:

- stacked summary cards;
- compact tabs/segmented navigation;
- search/filter controls optimized for one column;
- document cards instead of the full table;
- selected detail as a stacked drill-down surface.

Intermediate widths may adapt while preserving the information hierarchy and explicit demo boundary.

## Accessibility

- do not encode fiscal state by color alone;
- keep textual state labels;
- preserve keyboard-focus visibility;
- provide accessible names for icon-only row actions;
- keep RUT, fiscal number, amount/currency and dates readable without relying on tooltip-only content;
- maintain sufficient contrast in both shell and cards.

## API dependencies

WEB-013 depends on:

- `API-FIS-001..009`;
- `API-FDL-001..002`.

These are currently `MISSING_HTTP` in Wave 5.

`API-FIS-010` is implemented separately but does not provide the full UI contract required by this view.

## Preview implementation gate

Before `/documentos-fiscales` may become navigable as `ACTIVE_VISUAL_PREVIEW`:

1. this approved baseline must be preserved;
2. a real responsive React surface must exist in the shared shell;
3. business data must be explicitly local/demo;
4. server-owned fiscal actions must remain disabled;
5. no fiscal HTTP operations may be registered as executable without fresh evidence;
6. `operations: []` must remain true for the preview capability;
7. repository CI/architecture gates must pass;
8. deployed runtime review must remain separate from baseline approval.