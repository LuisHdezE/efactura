# UI-FIS-001 — Fiscal Documents

Status: `SPECIFICATION_READY / VISUAL_BASELINE_APPROVED / ACTIVE_VISUAL_PREVIEW`

WEB mapping: `WEB-013`

Route: `/documentos-fiscales`

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

In visual-preview mode these values are demonstration-only and are labeled/contained so they cannot be interpreted as live DGI totals. Once API integration exists, status semantics must come from the server contract rather than client inference.

## Search and filters

The active preview provides client-side demo controls for:

- document number;
- RUT/customer;
- CFE type;
- lifecycle/result state.

The approved baseline also reserves issue-date filtering. The preview does not fabricate a server query contract for date ranges while HTTP coverage is absent. Search/filtering operates only over local fixtures.

## Fiscal document ledger

Desktop uses a dense table with:

- issue date;
- type;
- number;
- customer / RUT;
- amount and currency;
- textual state.

Mobile replaces the dense table with stacked cards preserving:

- date;
- fiscal identity/number;
- customer;
- amount;
- textual state.

Selection is local UI state only while `API-FIS-001/002` remain unavailable.

## Selected document detail

The desktop detail panel and mobile stacked detail present demo fixtures for:

- document number/fiscal identity;
- type;
- issue timestamp;
- customer/receiver and RUT;
- total amount/currency;
- illustrative lifecycle/result state;
- CAE metadata;
- reference metadata;
- observations.

No local fixture is canonical fiscal evidence.

## Lifecycle and events

A dedicated tab presents local chronological fixture events for presentation validation. It explicitly states that these events do not come from DGI, transport infrastructure or `API-FIS-005`.

## Representation and XML

The approved visual reserves affordances for:

- view representation;
- download XML.

While `API-FIS-003` and `API-FIS-004` remain `MISSING_HTTP`, these controls are disabled. The preview displays an artifact placeholder and does not synthesize authoritative XML, PDF or printable fiscal representation.

## Correction and regularization

The view reserves visual placement for correction and regularization context. `API-FIS-006..009` remain server authority. Preview mode does not create, resolve or locally mutate correction/regularization state.

## Delivery context

The view reserves a disabled delivery-request affordance. `API-FDL-001/002` remain server authority. Preview mode does not claim real delivery attempts or send delivery requests.

## Export boundary

The approved visual contains an `Exportar` affordance. No accepted `WEB-013` bulk-export operation is mapped. The preview therefore renders export disabled and does not imply an executable server export exists.

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

Intermediate widths adapt while preserving information hierarchy and the explicit demo boundary.

## Accessibility

- fiscal state is not encoded by color alone;
- textual state labels remain visible;
- keyboard selection is supported on desktop rows;
- iconography is supplemental rather than the only status cue;
- RUT, fiscal number, amount/currency and dates remain readable;
- disabled server-owned controls remain explicit.

## API dependencies

WEB-013 depends on:

- `API-FIS-001..009`;
- `API-FDL-001..002`.

These are currently `MISSING_HTTP` in Wave 5.

`API-FIS-010` is implemented separately but does not provide the full UI contract required by this view.

## Active preview implementation boundary

`/documentos-fiscales` is eligible as `ACTIVE_VISUAL_PREVIEW` because:

1. the approved baseline is preserved;
2. a real responsive React surface exists in the shared shell;
3. business data is explicitly local/demo;
4. server-owned fiscal actions remain disabled;
5. no FIS/FDL HTTP operation is registered as executable;
6. `operations: []` remains true for the preview capability;
7. `verify-fiscal-documents-preview.mjs` guards the boundary;
8. repository CI/architecture gates must pass before merge;
9. deployed runtime review remains separate from baseline approval.

Promotion to live API-integrated behavior requires fresh executable API evidence and a separate integration reconciliation.
