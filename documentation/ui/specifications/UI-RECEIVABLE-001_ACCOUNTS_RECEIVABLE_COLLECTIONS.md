# UI-RECEIVABLE-001 — Accounts Receivable and Collections

Status: `FUNCTIONAL_SPECIFICATION / VISUAL_BASELINE_APPROVED / ACTIVE_VISUAL_PREVIEW / API_INTEGRATION_BLOCKED`

Mapping:

```text
WEB-010 -> UI-RECEIVABLE-001
```

Route:

```text
/cuentas-por-cobrar
```

Navigation state: `ACTIVE_VISUAL_PREVIEW`

## 1. Purpose

Provide the governed WebApp surface for customer receivables, aging and collections while preserving immutable financial history, allocation-derived balances and explicit overpayment policy boundaries.

The exact visual baseline is approved and the route may be exposed under `PREVIEW_ROUTE_POLICY_AMENDMENT.md` as an explicit local-data visual preview. `API-AR-001..004` and `API-COL-001..003` remain non-executable, so live receivable/collection integration and all server-owned financial mutations remain blocked.

## 2. Functional authority

The view is derived from:

- `WEB-010 — Accounts Receivable and Collections`;
- `FR-023`, `FR-026`, `FR-027`, `FR-070`, `FR-072`, `FR-073`, `FR-074`;
- `UC-SALE-003 — Confirm credit sale and create receivable`;
- `UC-AR-001 — Record customer collection and allocate payment`;
- accepted `API-AR-*` and `API-COL-*` contracts;
- `documentation/ui/receivables/UI-RECEIVABLE-001_RECONCILIATION.md`;
- `documentation/ui/PREVIEW_ROUTE_POLICY_AMENDMENT.md`.

The API completion matrix remains execution authority. Contract acceptance or visual route availability does not prove runtime backend capability.

## 3. Intended operators and permissions

Primary roles from the interface baseline:

- treasury;
- seller;
- administrator.

Permissions:

- `receivables.read` for receivables, aging and collection history;
- `receivables.adjust` for append-only receivable adjustments;
- `receivables.collect` for collections, allocations and compensating reversals.

Role labels are presentation context, not authorization shortcuts. The visual preview does not enforce or simulate server authorization outcomes.

## 4. Information architecture

### 4.1 Page header

Show:

- title: `Cuentas por cobrar`;
- concise subtitle focused on saldos, vencimientos y cobranzas;
- explicit preview/API-pending indicator;
- visually reserved `Registrar cobro` action.

While executable API evidence is absent, collection/adjustment/reversal controls remain visibly disabled.

### 4.2 Aging summary

The approved visual reserves an aging summary backed eventually by `API-AR-003`.

Presentation dimensions include:

- total open balance;
- current/not-yet-due;
- overdue amount;
- customers/documents represented by the demonstration fixture.

All preview totals must be explicitly described as demonstration values. Exact backend bucket boundaries, labels and totals remain server authority until an executable aging DTO exists.

### 4.3 Receivables list

The primary operational list supports responsive presentation of:

- receivable identity/reference;
- customer context;
- source-sale/fiscal reference when exposed;
- issue/origin date;
- due date;
- currency;
- original amount;
- derived open balance;
- lifecycle/aging status concept.

In visual-preview mode these values come only from explicit local fixtures. The frontend must not present them as live financial state and must not derive authoritative balance by editing or overwriting the original amount.

### 4.4 Receivable detail

Selecting an obligation provides space for:

- receivable identity;
- customer identity/context;
- source evidence references;
- original amount/currency;
- due date and visual status;
- demonstration allocation/history entries;
- current demonstration open balance;
- linked collection context when represented;
- version/concurrency metadata only after the executable contract exposes it.

### 4.5 Collection history

Collection history should distinguish:

- collection/payment identity when exposed;
- date/time;
- payment medium/reference when exposed;
- gross collection amount;
- allocations by receivable;
- unapplied/advance outcome only when returned by backend policy;
- reversal state/evidence when applicable.

A reversal is a compensating financial fact, never visual deletion of the original collection. Preview history entries are explicitly illustrative and do not claim persisted collection facts.

## 5. Register collection visual flow

The approved visual reserves a collection composer for:

- customer context;
- collection amount/currency;
- payment medium;
- external reference;
- one or more receivable allocations;
- allocation amount per receivable;
- remaining/unapplied amount area;
- validation/conflict/policy result area.

In `IMPLEMENTED_VISUAL_PREVIEW` this composer is non-executable. Inputs/actions that would imply financial mutation remain disabled.

The eventual executable flow must submit through `API-COL-001` with the governed idempotency mechanism. The UI must never silently truncate an entered amount to the currently selected open balance.

## 6. Partial/full allocation behavior

The interface must clearly communicate:

- original obligation amount;
- already allocated amount/history when available;
- current open balance;
- amount being allocated now;
- resulting server-authoritative balance after successful live execution.

A collection may allocate to one or multiple receivables according to backend policy. The preview may demonstrate the concept with local fixture values, but final allocation constraints remain contract/implementation authority.

## 7. Overpayment and advance policy

`FR-072` and `FR-073` prohibit silent truncation and require explicit business policy.

The visual baseline therefore reserves an explicit result/decision area for cases where an entered collection exceeds allocated open balances.

The preview must not invent whether the excess becomes:

- customer advance/credit;
- unapplied payment;
- rejected amount;
- another policy outcome.

Only the executable backend may decide and return that result. Preview copy must state this boundary explicitly.

## 8. Receivable adjustments

The visual reserves a permission-gated adjustment action backed eventually by `API-AR-004`.

Adjustments are append-only financial evidence. The UI must not rewrite historical original amounts or allocation history.

In visual-preview mode the adjustment action remains disabled. Final reason codes, allowed adjustment types, validation rules and concurrency semantics remain backend authority.

## 9. Collection reversal

The visual reserves a permission-gated reversal action backed eventually by `API-COL-003`.

A reversal must:

- preserve the original collection;
- communicate compensating status/history;
- require backend policy/authorization;
- never masquerade as deletion.

In visual-preview mode the reversal action remains disabled. Exact reason requirements and reversal eligibility remain backend authority.

## 10. Cash-shift relationship

`UC-AR-001` permits a collection to affect cash-shift expected totals when applicable.

This view may show linked cash/terminal evidence only when exposed by backend DTOs. It does not own:

- opening a cash shift;
- counted closing values;
- variance calculation;
- cash reconciliation.

Those belong to `WEB-012`. The preview does not simulate cash-shift effects.

## 11. Projected cash-flow relationship

`FR-074` links receivables/payables to projected cash-flow data.

`UI-RECEIVABLE-001` may expose due dates and aging that feed such projections, but it is not the full cash-flow forecasting/reporting surface. Cross-module projection belongs to Dashboard/Reports according to their governed contracts.

## 12. Idempotency and conflict handling

Future executable mutation flows for adjustment, collection and reversal must use the shared idempotency infrastructure.

The live UI must:

- preserve client operation identity across safe retries;
- surface `409`/conflict outcomes without destructive recovery;
- refresh authoritative receivable/allocation state before retrying where required;
- avoid double-posting a collection after transport uncertainty.

The visual preview does not simulate successful mutation, conflict or retry execution as persisted outcomes.

## 13. States

The governed product design covers or reserves:

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

The initial visual preview is not required to fabricate unavailable server error/permission/conflict states. Those become executable acceptance scope only after the HTTP contracts are implemented and freshly reconciled.

## 14. Responsive behavior

Desktop:

- aging/header summary plus dense master-detail receivables layout;
- collection/allocation context visible without overwhelming the financial ledger history.

Tablet:

- list/detail sections may stack;
- collection composer remains readable and explicitly non-executable.

Mobile:

- receivables become cards/compact rows;
- detail becomes single-column;
- currency, due date, open balance and state remain textually explicit;
- allocation lines remain readable without a wide table;
- actions do not depend on hover.

## 15. Light/dark requirements

The implementation adapts to the established eFactura global light/dark shell theme.

The exact approved baseline authority is `v1-approved-view`; dark-mode adaptation follows shared theme tokens and remains subject to deployed runtime review. Aging, overdue, partial, settled, reversed and policy-result states must not depend on color alone.

## 16. Accessibility

Required design properties:

- keyboard-operable filters/list/detail controls;
- accessible currency and balance representation;
- programmatic labels for amounts, dates, references and payment media;
- non-color-only status indicators;
- visible focus states;
- validation/conflict/policy feedback associated with the relevant control/context once those states become executable.

Disabled preview actions must remain identifiable as unavailable and must not appear to post financial state.

## 17. Explicit exclusions for v1 visual authority

The visual baseline and preview do not authorize:

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

## 18. Approved baseline and preview gate

Approved visual authority:

- baseline: `UI-RECEIVABLE-001 / v1-approved-view`;
- image generation id: `1fad639a-815f-431b-8c1c-7e6a50cdae77`;
- owner approval: `correcto, aprobada esta vista` on 2026-09-21.

Under `PREVIEW_ROUTE_POLICY_AMENDMENT.md`, the route `/cuentas-por-cobrar` may be activated as `IMPLEMENTED_VISUAL_PREVIEW` when the React implementation:

1. remains inside the shared AppShell;
2. uses explicit local demonstration data only;
3. marks KPI/list/detail values as non-authoritative demo content;
4. keeps all collection, adjustment and reversal mutations disabled;
5. registers no missing `API-AR-*` or `API-COL-*` operation as executable;
6. passes repository CI/gates;
7. undergoes separate deployed runtime review.

Live API integration remains blocked until receivables/collections HTTP evidence exists and this UI is freshly reconciled against the executable DTOs, errors, permissions, idempotency and concurrency semantics.
