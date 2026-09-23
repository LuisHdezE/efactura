# UI-CON-001 — Inventory Entry

Status: `APPROVED_VISUAL_BASELINE / IMPLEMENTATION_PENDING`

## Identity

- WEB: `WEB-015`;
- UI: `UI-CON-001`;
- name: `Contingencia / Sincronización`;
- product group: Fiscal;
- reserved route candidate: `/contingencia`;
- current navigation state: `PLANNED_DISABLED`.

## Requirements and roles

- requirements: `FR-050..FR-056`;
- roles: fiscal administrator, administrator, auditor.

## Purpose

Supervise formal CFC contingency, offline client operations, synchronization outcomes, conflicts/review and recovery status while clearly separating client/API availability from DGI/provider transport availability.

## Approved visual baseline

Approved baseline: `UI-CON-001 / v1-responsive-composite`.

- artifact gen_id: `4f3edc3e-cb6e-45f2-b0aa-ecad5854d76f`;
- aspect ratio: `4:3`;
- desktop light, desktop dark and mobile responsive compositions are represented in the approved visual;
- explicit user approval: 2026-09-23.

Generated copy is not contract authority. The implementation specification documents which labels/actions must be corrected or blocked.

## API dependencies

Contingency:

- `API-CNT-001..007`.

Synchronization:

- `API-SYN-001..003`.

Current readiness:

- all `API-CNT-001..007`: `MISSING_HTTP`;
- all `API-SYN-001..003`: `MISSING_HTTP`.

## Planned implementation mode

Because required HTTP authority is unavailable, the first executable surface may only be:

`IMPLEMENTED_VISUAL_PREVIEW`

with deterministic local fixtures and capability `operations: []`.

## Safety boundary

The preview must not execute:

- enter/exit contingency;
- contingency document registration;
- contingency reconciliation;
- synchronization batch submission;
- server-owned review/recovery transitions.

It must not claim live DGI/provider connectivity, live permissions or live synchronization outcomes.

## Canonical synchronization statuses

- `APPLIED`;
- `ALREADY_APPLIED`;
- `REJECTED`;
- `CONFLICT`;
- `REVIEW_REQUIRED`;
- `DEPENDENCY_BLOCKED`.

Each status requires a textual label in the UI.

## Responsive target

- desktop: dense supervision + detail/review context;
- tablet: usable supervision with stacked detail;
- mobile: status cards, collapsible filters, queue/operation cards and stacked detail;
- complex reconciliation remains desktop-first.

## Next gate

Preserve this baseline in documentation first. Route activation and React implementation require a separate PR and explicit merge approval.
