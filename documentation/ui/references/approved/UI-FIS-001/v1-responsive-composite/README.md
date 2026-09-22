# UI-FIS-001 — Approved Visual Reference

Baseline: `v1-responsive-composite`

Status: `APPROVED_VISUAL_BASELINE`

Approval date: `2026-09-21`

Approval statement:

> aprobado

## Artifact identity

- UI: `UI-FIS-001 — Fiscal Documents`
- WEB mapping: `WEB-013`
- reserved route candidate: `/documentos-fiscales`
- approved composite image generation id: `f7f7b77c-d6c9-4366-9c1d-7767990c4746`
- artifact contains one desktop composition plus one mobile responsive composition in the same approved image.

## Approved visual composition

The approved composite establishes presentation authority for:

- `Documentos fiscales` hierarchy inside the shared eFactura shell;
- Fiscal navigation with `Documentos fiscales` selected;
- search/filter surface for document number, RUT, customer, type, state and issue date;
- summary cards for total documents and illustrative accepted/in-process/rejected groupings;
- desktop document ledger with textual status treatment and compact row actions;
- selected-document detail panel with CFE identity, issuer/receiver facts, amount, state, CAE/reference metadata and observations;
- tabs for document listing, lifecycle/events and representation/XML concerns;
- single-column mobile hierarchy with compact KPIs and fiscal-document cards;
- explicit preview/API-pending disclosure;
- the established eFactura dark-blue visual language and responsive treatment.

## Functional authority boundary

The artifact is **visual authority only**. Product authority for `WEB-013` remains the governed fiscal-document contract:

- search/list fiscal documents;
- inspect one fiscal document and its immutable snapshot/lifecycle projection;
- inspect DGI/transport/result state when exposed by the API;
- retrieve authorized XML and printable representation when server endpoints exist;
- inspect fiscal-document events;
- inspect correction and regularization context;
- inspect delivery attempts and request delivery only when the governed endpoint exists.

The visual includes active-looking affordances such as `Exportar`, `Ver representación`, `Descargar XML`, `Ver eventos` and `Solicitar corrección`. Their presence in the approved composition does **not** authorize live behavior. A future React implementation must preserve the visual hierarchy while disabling or clearly marking server-owned actions whenever the corresponding HTTP capability is unavailable.

## Governance boundaries

This baseline does not authorize:

- route activation;
- claiming authoritative fiscal-document totals or DGI result counts from local fixtures;
- live fiscal-document search against production data;
- XML download;
- printable representation download/rendering from a server artifact;
- correction creation;
- regularization resolution;
- delivery requests;
- bulk export behavior not backed by an accepted API contract;
- mutation of immutable fiscal history;
- inferring DGI acceptance/rejection semantics from local presentation-only fixtures.

The accepted `WEB-013` dependency set is `API-FIS-001..009` plus `API-FDL-001..002`. In the current Wave 5 matrix these remain `MISSING_HTTP`. `API-FIS-010` exists as a separate implemented response-evidence operation, but it does not replace the missing list/detail/artifact/event/correction/regularization/delivery surface required by this view.

## Responsive authority

Because desktop and mobile are present inside one explicitly approved image, this baseline is named `v1-responsive-composite`. Intermediate tablet behavior may adapt while preserving hierarchy, information priority, textual status cues, fiscal-document identity readability and execution boundaries.

## Next gate

Before activation of `/documentos-fiscales`, reconcile this visual baseline with the active preview-route policy and implement a responsive React preview using explicit local demonstration data. Server-owned artifact retrieval, correction, regularization and delivery operations must remain disabled until fresh executable API evidence exists.