# WebApp Visual Preview Route Policy Amendment

Status: `ACTIVE / GOVERNED`

Date: `2026-09-21`

## Purpose

Reconcile an ambiguity discovered during runtime review of the Sidebar: previous `UI-TRANSFER-001` and `UI-PROCUREMENT-001` specifications treated missing HTTP implementation as a blocker for any React route activation, even though the WebApp already uses explicit demo/mock presentation for incomplete integration boundaries.

This amendment separates **frontend route availability** from **backend command readiness**.

## Decision

A governed WebApp view may be exposed as `ACTIVE_VISUAL_PREVIEW` before its authoritative HTTP API is executable only when all of the following are true:

1. the `WEB-* -> UI-*` boundary is reconciled;
2. the final route is governed;
3. an exact visual baseline has been explicitly approved;
4. a real responsive React page is implemented inside the shared shell;
5. all displayed business data is explicitly identified as local demonstration data;
6. every server-owned mutation remains disabled;
7. no missing HTTP operation is registered or described as executable;
8. CI/repository gates pass;
9. deployed runtime review remains a separate gate.

Capability status for this mode is:

```text
IMPLEMENTED_VISUAL_PREVIEW
```

## Current application

This amendment applies immediately to:

- `WEB-008 / UI-TRANSFER-001` at `/transferencias`;
- `WEB-009 / UI-PROCUREMENT-001` at `/compras`.

Their accepted API operations remain `MISSING_HTTP`. This amendment authorizes navigable frontend previews only; it does not authorize live create/update/approve/dispatch/receive/reconcile/post behavior.

`WEB-010 / UI-RECEIVABLE-001` does not qualify yet because its exact visual baseline has not been approved and no governed React preview has been implemented. It remains `PLANNED_DISABLED`.

## Supersession scope

For `UI-TRANSFER-001` and `UI-PROCUREMENT-001`, this amendment supersedes only prior statements that missing HTTP evidence prohibits **all route activation**.

It does **not** supersede any requirement concerning:

- API ownership;
- permissions;
- idempotency;
- concurrency/version handling;
- server-authoritative inventory, costing or payable effects;
- discrepancy handling;
- mutation restrictions;
- runtime acceptance;
- binary preservation debt.

When executable APIs become available, each visual preview requires fresh reconciliation before promotion to live integrated behavior.
