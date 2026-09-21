# UI-PAYABLE-001 — Accounts Payable and Supplier Payments

Status: `SPECIFICATION_READY / VISUAL_BASELINE_APPROVED`

WEB mapping: `WEB-011`

Candidate route: `/cuentas-por-pagar`

## Purpose

Provide treasury/accounting/admin users with a responsive view of supplier obligations, aging, due dates, allocations and balances, while preserving server authority for all financial mutations.

## Source authority

- interface scope: `documentation/blueprint-interface/interface-scope-baseline.json` (`WEB-011`);
- use case: `UC-AP-001 — Record supplier payment and allocate`;
- requirements: `FR-026`, `FR-027`, `FR-071`, `FR-072`, `FR-073`, `FR-074`;
- API reconciliation: `documentation/blueprint-api-contract/09_INTERFACE_SCOPE_RECONCILIATION.md`;
- completion matrix: `documentation/api-completion-matrix/WAVE_3.md`;
- approved visuals: `documentation/ui/references/approved/UI-PAYABLE-001/v1-responsive-suite/README.md`.

## Roles

- treasury;
- accountant;
- administrator.

## Information architecture

### Header

- module breadcrumb: `Finanzas > Cuentas por pagar`;
- title: `Cuentas por pagar`;
- concise purpose text;
- preview/demo disclosure when the route is not live-backed.

### KPI / aging summary

The approved visual supports demonstration cards for:

- total open payable amount;
- pending supplier-document count;
- overdue count;
- amount due in the next 30 days.

When live integration exists, these values must come from authoritative payable projections/API responses. A preview may use explicit local fixtures only.

### Search and filters

The preview may support client-side filtering of demo fixtures by:

- supplier/document search;
- status;
- due-date bucket.

These controls must not imply server-side querying while APIs are absent.

### Payable ledger

Desktop presentation may include:

- supplier document/reference;
- supplier;
- issue date;
- due date;
- amount;
- textual status;
- non-destructive inspection action.

Mobile presentation replaces the wide table with stacked obligation cards while preserving supplier, document, due date, amount and textual status.

### Selected payable detail

The selected obligation may show:

- supplier/document identity;
- supplier fiscal identity when fixture evidence exists;
- issue date;
- due date;
- original amount;
- open balance;
- current textual state;
- history/allocation tab structure;
- document/context navigation only where a real governed destination exists.

### Supplier-payment composer

The approved visual reserves placement for `Registrar pago`, but while `API-PAY-001` is `MISSING_HTTP` the control must remain disabled/non-executable.

The view must not locally invent:

- canonical payment IDs;
- bank/cash posting;
- allocation persistence;
- authoritative balance changes;
- unapplied-payment/advance treatment;
- reversal state.

## Financial-history semantics

The eventual live implementation must preserve:

- original supplier obligation as historical fact;
- allocations/adjustments as append-only evidence;
- open balance as derived/server-authoritative state;
- partial/full payment history;
- compensating reversal instead of destructive deletion.

Critical invariant from `UC-AP-001`: an entered supplier payment must never be silently capped. Any amount that cannot be allocated requires explicit policy handling.

## States

The source interface baseline requires support for:

- default;
- loading;
- empty;
- filtered_empty;
- error;
- 403;
- 409;
- 422.

A governed visual preview may represent only presentation states that can be truthful with local fixtures; it must not fabricate backend failures or authorization decisions as if observed from a live service.

## Responsive behavior

The approved baseline explicitly covers:

- desktop: KPI row + filterable ledger + selected detail panel;
- mobile: single-column hierarchy, compact KPI grid, search/filter row, obligation cards and selected detail below the list.

Tablet/intermediate breakpoints may adapt between those compositions while preserving content priority and execution boundaries.

## Accessibility

- currency and balances must remain readable without relying on color alone;
- states such as `Pendiente`, `Vencida` and `Pagada` require visible text;
- search/filter controls require programmatic labels in implementation;
- disabled financial actions must expose disabled state semantically, not only visually;
- tab/detail navigation must remain keyboard operable.

## Accepted API dependencies

- `API-AP-001..004`;
- `API-PAY-001..003`.

All seven are currently `MISSING_HTTP`. Therefore this specification does not authorize live integration or financial mutation.

## Implementation gate

React preview implementation requires a separate governance promotion to `ACTIVE_VISUAL_PREVIEW`. Route activation and live API integration are independent gates.