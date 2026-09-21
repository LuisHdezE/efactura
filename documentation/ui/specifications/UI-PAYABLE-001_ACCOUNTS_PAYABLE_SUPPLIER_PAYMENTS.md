# UI-PAYABLE-001 — Accounts Payable and Supplier Payments

Status: `IMPLEMENTED_VISUAL_PREVIEW / API_PENDING`

WEB mapping: `WEB-011`

Route: `/cuentas-por-pagar`

## Purpose

Provide treasury/accounting/admin users with a responsive view of supplier obligations, aging, due dates, allocations and balances, while preserving server authority for all financial mutations.

## Source authority

- interface scope: `documentation/blueprint-interface/interface-scope-baseline.json` (`WEB-011`);
- use case: `UC-AP-001 — Record supplier payment and allocate`;
- requirements: `FR-026`, `FR-027`, `FR-071`, `FR-072`, `FR-073`, `FR-074`;
- API reconciliation: `documentation/blueprint-api-contract/09_INTERFACE_SCOPE_RECONCILIATION.md`;
- completion matrix: `documentation/api-completion-matrix/WAVE_3.md`;
- approved visuals: `documentation/ui/references/approved/UI-PAYABLE-001/v1-responsive-suite/README.md`;
- preview governance: `documentation/ui/PREVIEW_ROUTE_POLICY_AMENDMENT.md`.

## Roles

- treasury;
- accountant;
- administrator.

## Implemented preview information architecture

### Header

- module breadcrumb: `Finanzas > Cuentas por pagar`;
- title and concise purpose text;
- explicit `Preview UI · API pendiente` disclosure;
- visually reserved `Registrar factura` action, disabled because no authoritative intake/create contract is implemented for this surface.

### KPI / aging summary

The preview renders local demonstration cards for:

- total open payable amount;
- pending supplier-document count;
- overdue count;
- amount due in the next 30 days.

These are fixtures only and are never represented as authoritative server projections.

### Search and filters

Client-side controls support:

- supplier/document/reference search;
- status filter;
- due-date bucket filter;
- reset/clear.

No server-side query is implied.

### Payable ledger

Desktop presentation includes:

- supplier document/reference;
- supplier;
- issue date;
- due date;
- original amount;
- open balance;
- textual status.

Rows are keyboard-selectable and only change local selected-detail state.

Mobile presentation replaces the wide table with stacked obligation cards preserving supplier, document, due date, open balance and textual state.

### Selected payable detail

The selected obligation displays:

- supplier/document identity;
- supplier fiscal identity from the local fixture;
- issue date;
- due date;
- original amount;
- open balance;
- reference;
- `Información` / `Historial` client-side tabs.

The history is explicitly demonstration data and does not claim immutable backend evidence.

### Supplier-payment composer

The preview reserves placement for `Registrar pago` but the submit control is disabled while `API-PAY-001` remains `MISSING_HTTP`.

Inputs for amount, payment medium and external reference remain non-executable. The preview also shows the critical policy warning that excess payment may not be silently truncated.

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

The source interface baseline requires support for default, loading, empty, filtered_empty, error, 403, 409 and 422.

The current visual preview implements truthful local default/filtering/filtered-empty behavior only. It does not fabricate backend failures or authorization decisions as if observed from a live service.

## Responsive behavior

The approved baseline and implementation cover:

- desktop: KPI row + filterable ledger + selected detail/payment panel;
- mobile: single-column hierarchy, compact KPI grid, search/filter controls, obligation cards and selected detail below the list;
- tablet/intermediate layouts that collapse progressively without changing execution authority.

## Accessibility

- currency and balances remain readable without relying on color alone;
- states such as `Pendiente`, `Vencida`, `Parcial` and `Pagada` use visible text;
- search/filter controls use labels;
- disabled financial actions expose semantic disabled state;
- rows/cards and detail tabs are keyboard operable.

## Accepted API dependencies

- `API-AP-001..004`;
- `API-PAY-001..003`.

All seven remain `MISSING_HTTP`; `UI-PAYABLE-001` therefore registers no executable API operations.

## Runtime gate

After merge/deploy, `/cuentas-por-pagar` requires separate desktop/mobile visual runtime review against `v1-responsive-suite`. Live API integration remains a later, independent reconciliation gate.
