# WebApp Visual Preview Route Policy Amendment

Status: `ACTIVE / GOVERNED`

Date: `2026-09-21`

## Purpose

Reconcile the distinction between an inspectable frontend surface and executable backend capability. A governed WebApp view may be navigable for visual/runtime review while its authoritative HTTP operations remain unavailable, provided the preview cannot masquerade as live business execution.

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

This amendment applies to:

- `WEB-008 / UI-TRANSFER-001` at `/transferencias`;
- `WEB-009 / UI-PROCUREMENT-001` at `/compras`;
- `WEB-010 / UI-RECEIVABLE-001` at `/cuentas-por-cobrar`.

For `UI-RECEIVABLE-001`, the approved visual authority is `v1-approved-view`, image generation id `1fad639a-815f-431b-8c1c-7e6a50cdae77`.

Its preview implementation may provide client-side search/filter/selection/detail behavior over explicit local fixtures and may visually reserve the collection/allocation composer. The following remain non-executable while the authoritative HTTP surface is absent:

- collection posting;
- receivable adjustment;
- collection reversal;
- local authoritative balance mutation;
- silent overpayment truncation;
- invented advance/unapplied-payment policy;
- cash-shift reconciliation.

`API-AR-001..004` and `API-COL-001..003` remain `MISSING_HTTP`; therefore `UI-RECEIVABLE-001` registers no executable API operations in `capabilities.ts` while it is a visual preview.

## Supersession scope

For `UI-TRANSFER-001`, `UI-PROCUREMENT-001` and `UI-RECEIVABLE-001`, this amendment supersedes prior statements that missing HTTP evidence prohibits **all route activation**.

It does **not** supersede any requirement concerning:

- API ownership;
- permissions;
- idempotency;
- concurrency/version handling;
- server-authoritative balances, inventory, costing, payable or receivable effects;
- discrepancy handling;
- mutation restrictions;
- runtime acceptance;
- binary preservation debt.

When executable APIs become available, each visual preview requires fresh reconciliation before promotion to live integrated behavior.
