# UI-CON-001 — Contingencia / Sincronización

Status: `APPROVED_VISUAL_SPECIFICATION / IMPLEMENTATION_PENDING`

WEB mapping: `WEB-015`

Reserved route candidate: `/contingencia`

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

## Allowed preview behavior before HTTP exists

- deterministic local fixtures;
- local search/filter;
- local selection;
- light/dark theme behavior;
- responsive cards/list presentation;
- disabled action placement;
- explanatory demo/API-pending messaging.

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

Implementation must use shared WebApp shell tokens rather than feature-local fixed light/dark palettes.

## Acceptance gate

A future implementation requires separate CI, merge approval, deploy and runtime visual acceptance in desktop Claro, desktop Oscuro and mobile 390×844.
