# UI-CON-001 — Contingency / Synchronization Reconciliation

Status: `VISUAL_BASELINE_APPROVED / IMPLEMENTED_VISUAL_PREVIEW / RUNTIME_ACCEPTANCE_PENDING`

Mapping:

```text
WEB-015 -> UI-CON-001
```

Route: `/contingencia`

## Product authority

`WEB-015 — Contingency and Synchronization Supervision` belongs to the Fiscal area and supervises formal CFC contingency plus offline synchronization outcomes, conflicts, review and recovery status.

Accepted requirements: `FR-050..FR-056`.

Accepted roles:

- fiscal administrator;
- administrator;
- auditor.

## Functional invariants

The UI preserves these domain rules:

- distinguish client/API offline state from DGI/provider transport outage;
- formal CFC contingency keeps CFC identity and must not allocate arbitrary normal CAE/CFE numbers;
- offline operations use globally unique client operation IDs;
- synchronization returns deterministic per-operation status;
- same client operation ID with materially different payload is a conflict and auditable event;
- queued high-risk operations are revalidated against current permission before application.

## Accepted API dependency

WEB-015 maps exactly to contingency operations:

- `API-CNT-001` `getContingencyStatus`;
- `API-CNT-002` `enterContingency`;
- `API-CNT-003` `exitContingency`;
- `API-CNT-004` `listContingencyDocuments`;
- `API-CNT-005` `registerContingencyDocument`;
- `API-CNT-006` `getContingencyDocument`;
- `API-CNT-007` `reconcileContingencyDocument`;

and synchronization operations:

- `API-SYN-001` `createSyncBatch`;
- `API-SYN-002` `getSyncBatch`;
- `API-SYN-003` `getSyncOperation`.

The interface-scope reconciliation does not assign `API-SYN-004 getSyncChanges` to WEB-015.

## Current implementation boundary

Wave 5 currently records `API-CNT-001..007` as `MISSING_HTTP`.

Wave 6 currently records `API-SYN-001..003` as `MISSING_HTTP`.

Therefore the WebApp implementation is intentionally `IMPLEMENTED_VISUAL_PREVIEW` with deterministic local fixtures and `operations: []`.

It does not claim live contingency state, live DGI/provider connectivity, successful synchronization, current server permissions or executable recovery authority.

Implemented local-only behavior is limited to:

- search;
- filters;
- record selection;
- textual contract-status presentation;
- demo detail/history;
- responsive list/card transformation;
- disabled server-owned action placement.

No CNT/SYN HTTP route is registered in `UI-CON-001`.

## Accepted permissions

Read concepts are governed by `fiscal.read` and synchronization usage by `sync.use` according to the accepted API contract. Server-owned contingency mutations require `fiscal.manage_contingency`.

The current demo-shell identity is not authoritative permission context and the preview derives no permission decision from it.

## Visual authority

Approved baseline: `UI-CON-001 / v1-responsive-composite`.

- gen_id: `4f3edc3e-cb6e-45f2-b0aa-ecad5854d76f`;
- aspect ratio: `4:3`;
- authority includes desktop light, desktop dark and mobile responsive compositions;
- explicitly approved by the user on 2026-09-23.

Visual authority is limited to composition and UX intent. Generated labels that imply non-existent endpoints, live DGI connectivity, automatic synchronization guarantees or executable configuration are non-authoritative and are reconciled to the canonical contracts in the implementation.

## Required state semantics

The surface supports textual presentation for canonical synchronization results:

- `APPLIED` → Aplicada;
- `ALREADY_APPLIED` → Ya aplicada;
- `REJECTED` → Rechazada;
- `CONFLICT` → Conflicto;
- `REVIEW_REQUIRED` → Requiere revisión;
- `DEPENDENCY_BLOCKED` → Dependencia bloqueada.

State is never communicated by color alone.

HTTP-derived `loading`, `error`, `403`, `409` and `422` remain contract states for future authoritative integration. The preview may visually illustrate related semantic outcomes but must not claim they came from a live server response.

## Responsive intent

Supervision remains usable on tablet and mobile. Complex reconciliation is desktop-first. Mobile prioritizes status summary, filter disclosure, queue cards and readable operation detail in one column.

## QA guard

`src/WebApp/scripts/verify-contingency-preview.mjs` is included in normal frontend `check` and `build:deploy` commands.

It guards at least:

- WEB-015 active route ownership by UI-CON-001;
- `/contingencia` capability route;
- `IMPLEMENTED_VISUAL_PREVIEW`;
- `operations: []`;
- absence of direct `fetch`/Axios or CNT/SYN HTTP paths;
- explicit demo/API-pending language;
- all six canonical textual statuses;
- shared theme tokens and responsive markers.

## Remaining gate

Merge still requires explicit user approval after CI/review verification.

After merge, deployment and runtime visual acceptance are separate gates. Desktop Claro, desktop Oscuro and mobile 390×844 must be reviewed before UI-CON-001 can be considered runtime-closed.
