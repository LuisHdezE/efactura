# UI-RECEIVABLE-001 — Accounts Receivable and Collections

Status: `FUNCTIONAL_SPECIFICATION / VISUAL_DRAFT_READY / EXECUTION_BLOCKED_BY_API`

Mapping:

```text
WEB-010 -> UI-RECEIVABLE-001
```

Reserved route:

```text
/cuentas-por-cobrar
```

Navigation state: `PLANNED_DISABLED`

## 1. Purpose

Provide the governed WebApp surface for customer receivables, aging and collections while preserving immutable financial history, allocation-derived balances and explicit overpayment policy boundaries.

This specification authorizes visual drafting only while `API-AR-001..004` and `API-COL-001..003` remain non-executable.

## 2. Functional authority

The view is derived from:

- `WEB-010 — Accounts Receivable and Collections`;
- `FR-023`, `FR-026`, `FR-027`, `FR-070`, `FR-072`, `FR-073`, `FR-074`;
- `UC-SALE-003 — Confirm credit sale and create receivable`;
- `UC-AR-001 — Record customer collection and allocate payment`;
- accepted `API-AR-*` and `API-COL-*` contracts;
- `documentation/ui/receivables/UI-RECEIVABLE-001_RECONCILIATION.md`.

The API completion matrix remains execution authority. Contract acceptance alone does not prove runtime capability.

## 3. Intended operators and permissions

Primary roles from the interface baseline:

- treasury;
- seller;
- administrator.

Permissions:

- `receivables.read` for receivables, aging and collection history;
- `receivables.adjust` for append-only receivable adjustments;
- `receivables.collect` for collections, allocations and compensating reversals.

Role labels are presentation context, not authorization shortcuts.

## 4. Information architecture

### 4.1 Page header

Show:

- title: `Cuentas por cobrar`;
- concise subtitle focused on saldos, vencimientos y cobranzas;
- optional API/demo availability indicator while execution is unavailable;
- visually reserved `Registrar cobro` action.

Until executable API evidence exists, prototype actions remain illustrative/disabled in a running WebApp.

### 4.2 Aging summary

The visual baseline may reserve an aging summary backed eventually by `API-AR-003`.

Possible presentation dimensions include:

- total open balance;
- current/not-yet-due;
- overdue groups/buckets;
- count of obligations.

Exact bucket boundaries, labels and totals are backend authority and must not be invented as contract facts before the executable DTO exists.

### 4.3 Receivables list

The primary operational list should support responsive presentation of:

- receivable identity/reference;
- customer context;
- source-sale/fiscal reference when exposed;
- issue/origin date when exposed;
- due date;
- currency;
- original amount;
- allocated amount when exposed;
- derived open balance;
- lifecycle/aging status concept.

The frontend must not derive authoritative balance by editing or overwriting the original amount.

### 4.4 Receivable detail

Selecting an obligation should provide space for:

- receivable identity;
- customer identity/context;
- source evidence references when exposed;
- original amount/currency;
- due date and aging/status;
- allocation history;
- adjustments;
- current derived balance;
- linked collections;
- version/concurrency metadata only when the executable contract exposes it.

### 4.5 Collection history

Collection history should distinguish:

- collection/payment identity;
- date/time;
- payment medium/reference when exposed;
- gross collection amount;
- allocations by receivable;
- unapplied/advance outcome only when returned by backend policy;
- reversal state/evidence when applicable.

A reversal is a compensating financial fact, never visual deletion of the original collection.

## 5. Register collection visual flow

A visual candidate may reserve a drawer/modal/page for:

- customer context;
- collection amount/currency;
- payment medium;
- external reference;
- one or more receivable allocations;
- allocation amount per receivable;
- remaining/unapplied amount area;
- validation/conflict/policy result area.

The eventual executable flow must submit through `API-COL-001` with the governed idempotency mechanism.

The UI must never silently truncate an entered amount to the currently selected open balance.

## 6. Partial/full allocation behavior

The interface must clearly communicate:

- original obligation amount;
- already allocated amount/history;
- current open balance;
- amount being allocated now;
- resulting server-authoritative balance after success.

A collection may allocate to one or multiple receivables according to backend policy. The visual may demonstrate the concept, but final allocation constraints remain contract/implementation authority.

## 7. Overpayment and advance policy

`FR-072` and `FR-073` prohibit silent truncation and require explicit business policy.

Therefore the visual baseline should reserve an explicit result/decision area for cases where entered collection exceeds allocated open balances.

The UI must not invent whether the excess becomes:

- customer advance/credit;
- unapplied payment;
- rejected amount;
- another policy outcome.

Only the executable backend may decide and return that result.

## 8. Receivable adjustments

The visual may reserve a permission-gated adjustment action backed eventually by `API-AR-004`.

Adjustments are append-only financial evidence. The UI must not rewrite historical original amounts or allocation history.

Final reason codes, allowed adjustment types, validation rules and concurrency semantics remain backend authority.

## 9. Collection reversal

The visual may reserve a permission-gated reversal action backed eventually by `API-COL-003`.

A reversal must:

- preserve the original collection;
- communicate compensating status/history;
- require backend policy/authorization;
- never masquerade as deletion.

Exact reason requirements and reversal eligibility remain backend authority.

## 10. Cash-shift relationship

`UC-AR-001` permits a collection to affect cash-shift expected totals when applicable.

This view may show linked cash/terminal evidence only when exposed by backend DTOs. It does not own:

- opening a cash shift;
- counted closing values;
- variance calculation;
- cash reconciliation.

Those belong to `WEB-012`.

## 11. Projected cash-flow relationship

`FR-074` links receivables/payables to projected cash-flow data.

`UI-RECEIVABLE-001` may expose due dates and aging that feed such projections, but it is not the full cash-flow forecasting/reporting surface. Cross-module projection belongs to Dashboard/Reports according to their governed contracts.

## 12. Idempotency and conflict handling

Future executable mutation flows for adjustment, collection and reversal must use the shared idempotency infrastructure.

The UI must:

- preserve client operation identity across safe retries;
- surface `409`/conflict outcomes without destructive recovery;
- refresh authoritative receivable/allocation state before retrying where required;
- avoid double-posting a collection after transport uncertainty.

## 13. States

The governed visual suite should cover at least:

- populated receivables list;
- selected receivable detail;
- aging summary;
- collection/allocation composer;
- partial payment state;
- settled state;
- overdue state;
- collection history;
- reversed collection representation;
- explicit overpayment/policy-result area;
- empty state;
- filtered empty state;
- loading;
- generic error;
- forbidden/permission state;
- conflict/idempotency state;
- validation-error state;
- narrow responsive layout.

Exact server error copy remains non-authoritative until HTTP implementation exists.

## 14. Responsive behavior

Desktop:

- aging/header summary plus dense master-detail receivables layout;
- collection/allocation context visible without overwhelming the financial ledger history.

Tablet:

- list/detail sections may stack;
- collection composer remains usable for treasury workflows.

Mobile:

- receivables become cards/compact rows;
- detail becomes single-column;
- currency, due date, open balance and state remain textually explicit;
- allocation lines remain readable without a wide table;
- actions do not depend on hover.

## 15. Light/dark requirements

The baseline must provide light/dark parity with the established eFactura shell.

Aging, overdue, partial, settled, reversed and policy-result states must not depend on color alone.

## 16. Accessibility

Required design properties:

- keyboard-operable filters/list/detail/forms/actions;
- accessible currency and balance representation;
- programmatic labels for amounts, dates, references and payment media;
- non-color-only status indicators;
- visible focus states;
- validation/conflict/policy feedback associated with the relevant control/context.

## 17. Explicit exclusions for v1 visual authority

The visual baseline must not authorize:

- customer master editing;
- supplier payables/payment workflows;
- cash-shift open/close/reconciliation;
- bank reconciliation;
- general-ledger journal editing;
- destructive deletion/history rewrite;
- client-side authoritative balance mutation;
- silent overpayment truncation;
- invented advance/unapplied-payment policy;
- fiscal-document mutation;
- full cash-flow reporting/forecasting;
- any live API claim while the seven required operations remain `MISSING_HTTP`.

## 18. Visual baseline gate

The next governed artifact is a responsive light/dark visual candidate for `UI-RECEIVABLE-001`.

Visual approval will preserve visual authority only. It will not activate `/cuentas-por-cobrar`.

React implementation/activation remains blocked until the receivables/collections API lane provides executable evidence and this specification is reconciled again against actual DTOs, error contracts, permissions and mutation semantics.