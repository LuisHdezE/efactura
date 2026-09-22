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
- `WEB-010 / UI-RECEIVABLE-001` at `/cuentas-por-cobrar`;
- `WEB-011 / UI-PAYABLE-001` at `/cuentas-por-pagar`;
- `WEB-012 / UI-CASH-001` at `/caja`;
- `WEB-013 / UI-FIS-001` at `/documentos-fiscales`.

### Receivables boundary

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

### Payables boundary

For `UI-PAYABLE-001`, the approved visual authority is `v1-responsive-suite`:

- desktop gen_id `764a3a30-08a6-4f4f-8bda-2bdaf245b8c2`;
- mobile gen_id `06ffefef-b352-4967-9585-8012b03e7c6a`.

Its preview implementation may provide client-side supplier/document search, status/due-date filtering, row/card selection, payable detail and a local demonstration history. It may visually reserve supplier-payment controls.

The following remain non-executable while the authoritative HTTP surface is absent:

- supplier-payment posting/allocation;
- payable adjustment;
- supplier-payment reversal;
- creation of authoritative payable balances;
- local authoritative balance mutation;
- cash/bank posting or reconciliation;
- silent payment capping/truncation;
- invented unapplied-payment/advance policy.

`API-AP-001..004` and `API-PAY-001..003` remain `MISSING_HTTP`; therefore `UI-PAYABLE-001` registers `operations: []` in `capabilities.ts` while it is a visual preview.

The `UC-AP-001` invariant remains binding: a payment that exceeds allocatable balance may not be silently truncated. The preview displays this boundary but does not calculate a policy outcome.

### Cash boundary

For `UI-CASH-001`, the approved visual authority is `v1-responsive-composite`, image generation id `4af42487-0a36-43aa-832e-ef96c7f11a61`.

Its preview implementation may provide client-side movement search/filter/selection, informational tabs, deterministic demonstration expected/count/variance examples and responsive movement cards.

The following remain non-executable while the authoritative HTTP surface is absent:

- canonical shift opening;
- manual cash posting;
- canonical shift closing;
- variance reconciliation or approval;
- persistence of counted values;
- mutation/reversal of historical cash movements;
- invented tolerance decisions;
- authoritative bank-account or balance claims.

`API-CSH-001..007` remain `MISSING_HTTP`; therefore `UI-CASH-001` registers `operations: []` in `capabilities.ts` while it is a visual preview.

The approved visual contains generic bank/account wording. That visual wording is not product authority. The implementation maps it to source-backed cash-shift/payment-medium concepts while preserving the approved composition.

### Fiscal documents boundary

For `UI-FIS-001`, the approved visual authority is `v1-responsive-composite`, image generation id `f7f7b77c-d6c9-4366-9c1d-7767990c4746`.

Its preview implementation may provide client-side document search/filter/selection, demonstration fiscal-result states, document snapshot detail, local event timeline presentation and responsive fiscal-document cards.

The following remain non-executable while the authoritative HTTP surface is absent:

- canonical fiscal-document list/detail claims;
- authoritative DGI or transport/result evidence;
- XML download;
- printable/authorized representation retrieval;
- correction creation;
- regularization queue/case resolution;
- delivery-attempt claims or delivery requests;
- bulk export;
- any local lifecycle mutation presented as canonical fiscal state.

`API-FIS-001..009` and `API-FDL-001..002` remain `MISSING_HTTP`; therefore `UI-FIS-001` registers `operations: []` in `capabilities.ts` while it is a visual preview.

`API-FIS-010` is implemented separately but does not provide the list/detail/artifact/events/correction/regularization/delivery surface required by `WEB-013`.

## Supersession scope

For `UI-TRANSFER-001`, `UI-PROCUREMENT-001`, `UI-RECEIVABLE-001`, `UI-PAYABLE-001`, `UI-CASH-001` and `UI-FIS-001`, this amendment supersedes prior statements that missing HTTP evidence prohibits **all route activation**.

It does **not** supersede any requirement concerning:

- API ownership;
- permissions;
- idempotency;
- concurrency/version handling;
- server-authoritative balances, inventory, costing, payable, receivable, cash or fiscal effects;
- discrepancy handling;
- mutation restrictions;
- runtime acceptance;
- binary preservation debt.

When executable APIs become available, each visual preview requires fresh reconciliation before promotion to live integrated behavior.
