# UI-CON-001 — Contingencia / Sincronización

Status: `APPROVED_VISUAL_SPECIFICATION / IMPLEMENTED_VISUAL_PREVIEW / RUNTIME_ACCEPTANCE_PENDING`

WEB mapping: `WEB-015`

Route: `/contingencia`

Approved visual baseline: `v1-responsive-composite`

Approved artifact gen_id: `4f3edc3e-cb6e-45f2-b0aa-ecad5854d76f`

Approved aspect ratio: `4:3`

## Goal

Provide a supervised operational surface for formal CFC contingency and offline synchronization without confusing local/client availability with DGI/provider transport availability and without inventing fiscal numbering or server authority.

## Primary users

- fiscal administrator;
- administrator;
- auditor.

## Desktop information architecture

1. Fiscal breadcrumb and `Contingencia / Sincronización` heading.
2. Explicit demo/API-pending banner while authoritative HTTP is unavailable.
3. Separate operational status areas for:
   - Cliente/API;
   - DGI/Proveedor;
   - formal contingency state;
   - synchronization/queue summary.
4. Main supervision surface with local demo queue/list data.
5. Filters and search appropriate to document/operation supervision.
6. Textual status badges.
7. Detail/review panel for the selected contingency document or synchronization operation.
8. History/review context where visually appropriate.
9. Server-owned actions rendered only as disabled placement while API/session authority is unavailable.

## Important visual correction to generated copy

The approved image is a visual composition authority, not a literal API contract screenshot.

Implementation must correct generated wording when necessary:

- do not display `DGI Online` as authoritative live data in local preview;
- do not expose an enabled `Forzar sincronización` control;
- do not imply synchronization succeeded against DGI;
- do not expose configuration mutations unsupported by WEB-015 contracts;
- do not invent endpoint IDs or routes from generated image text.

Use explicit labels such as `Dato de demostración`, `Sin consulta HTTP`, `API pendiente` or equivalent where a status is local fixture data.

## Contingency concepts

The surface should make these concepts visually separable:

- client/API online vs offline;
- DGI/provider transport available vs unavailable;
- formal CFC contingency inactive vs active;
- recovery/reconciliation pending vs resolved.

Formal contingency must preserve CFC identity and must never suggest that an offline client can reserve or choose arbitrary normal CAE/CFE numbering.

## Synchronization concepts

The UI should support list/detail visualization for batches/operations and canonical per-operation statuses:

- Aplicada;
- Ya aplicada;
- Rechazada;
- Conflicto;
- Requiere revisión;
- Dependencia bloqueada.

For conflict/review items, detail should reserve space for operation identity, reason/evidence and canonical server result/reference when those contracts become available.

## Implemented preview behavior

The first implementation activates `/contingencia` exclusively as `IMPLEMENTED_VISUAL_PREVIEW` and provides:

- deterministic local fixtures separated from the React view;
- local search and filtering;
- local record selection;
- explicit separation of `Cliente / API` and `DGI / Proveedor` status surfaces;
- all six canonical synchronization result labels with text;
- local CFC/synchronization detail and history examples;
- desktop table plus mobile card presentation;
- responsive filter disclosure;
- light/dark behavior through shared WebApp shell tokens;
- disabled placement for server-owned actions;
- `operations: []` in `UI-CON-001` capability registration;
- dedicated `verify-contingency-preview.mjs` QA guard in normal frontend checks.

## Forbidden preview behavior

- direct `fetch`/Axios bypass of the governed service layer;
- execution of contingency entry/exit;
- CFC registration/reconciliation mutation;
- synchronization batch submission;
- claims of live DGI/provider state;
- claims of live permission checks;
- automatic synchronization side effects;
- invented fiscal numbering authority.

## API dependency

Contingency:

- `API-CNT-001..007`.

Synchronization:

- `API-SYN-001..003`.

Current HTTP readiness: all ten operations are `MISSING_HTTP` in the accepted completion matrices.

## State requirements

Must visually accommodate:

- default;
- loading;
- empty;
- error;
- offline;
- 403;
- 409;
- 422.

Status must always have text and must not depend on color alone.

The current deterministic fixture set explicitly demonstrates normal preview data, empty filtered results, conflict/review and validation/dependency outcomes. Authoritative loading/error/HTTP states remain future integration behavior and must not be simulated as live server responses.

## Responsive behavior

Desktop is authoritative for complex reconciliation and dense supervision.

Tablet may stack status and detail while retaining filters and review context.

Mobile should:

- stack heading and status cards;
- collapse filters behind a clear control when needed;
- replace dense tables with cards;
- keep operation/document status and identifiers readable;
- place detail below the list;
- keep blocked actions clearly disabled and explanatory.

## Theme parity

Implementation uses shared WebApp shell tokens rather than feature-local fixed light/dark palettes.

## Acceptance gate

Implementation CI and merge review are required before deployment. Runtime visual acceptance remains separate and must cover desktop Claro, desktop Oscuro and mobile 390×844.
