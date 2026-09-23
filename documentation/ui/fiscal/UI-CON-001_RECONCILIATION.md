# UI-CON-001 — Contingency / Synchronization Reconciliation

Status: `VISUAL_BASELINE_APPROVED / IMPLEMENTATION_PENDING`

Mapping:

```text
WEB-015 -> UI-CON-001
```

Reserved route candidate: `/contingencia`

## Product authority

`WEB-015 — Contingency and Synchronization Supervision` belongs to the Fiscal area and supervises formal CFC contingency plus offline synchronization outcomes, conflicts, review and recovery status.

Accepted requirements: `FR-050..FR-056`.

Accepted roles:

- fiscal administrator;
- administrator;
- auditor.

## Functional invariants

The UI must preserve these domain rules:

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

Therefore the first WebApp implementation may only become `IMPLEMENTED_VISUAL_PREVIEW` with deterministic local fixtures and `operations: []`.

It must not claim live contingency state, live DGI/provider connectivity, successful synchronization, current server permissions or executable recovery authority.

## Accepted permissions

Read concepts are governed by `fiscal.read` and synchronization usage by `sync.use` according to the accepted API contract. Server-owned contingency mutations require `fiscal.manage_contingency`.

The current demo-shell identity is not authoritative permission context.

## Visual authority

Approved baseline: `UI-CON-001 / v1-responsive-composite`.

- gen_id: `4f3edc3e-cb6e-45f2-b0aa-ecad5854d76f`;
- aspect ratio: `4:3`;
- authority includes desktop light, desktop dark and mobile responsive compositions;
- explicitly approved by the user on 2026-09-23.

Visual authority is limited to composition and UX intent. Generated labels that imply non-existent endpoints, live DGI connectivity, automatic synchronization guarantees or executable configuration are non-authoritative and must be reconciled to the canonical contracts during implementation.

## Required state semantics

The surface must support textual presentation for:

- normal / available;
- client/API offline;
- DGI/provider transport unavailable;
- formal contingency active/inactive;
- loading;
- empty;
- error;
- offline;
- `403`;
- `409`;
- `422`.

Synchronization operation results must support the canonical textual statuses:

- `APPLIED` → Aplicada;
- `ALREADY_APPLIED` → Ya aplicada;
- `REJECTED` → Rechazada;
- `CONFLICT` → Conflicto;
- `REVIEW_REQUIRED` → Requiere revisión;
- `DEPENDENCY_BLOCKED` → Dependencia bloqueada.

State must never be communicated by color alone.

## Responsive intent

Supervision must remain usable on tablet and mobile. Complex reconciliation is desktop-first. Mobile should prioritize status summary, filters, queue cards and readable operation detail in one column.

## Implementation gate

No route activation, capability registration, fixture creation or React implementation is authorized by this documentation PR. Those changes belong to a separate implementation PR after this baseline is merged.
