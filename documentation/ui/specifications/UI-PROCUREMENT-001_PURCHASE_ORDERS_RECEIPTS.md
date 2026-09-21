# UI-PROCUREMENT-001 — Purchase Orders and Receipts

Status: `FUNCTIONAL_SPECIFICATION / VISUAL_APPROVED / EXECUTION_BLOCKED_BY_API / BINARY_PRESERVATION_PENDING`

Mapping:

```text
WEB-009 -> UI-PROCUREMENT-001
```

Reserved route:

```text
/compras
```

Navigation state: `PLANNED_DISABLED`

## 1. Purpose

Provide the governed WebApp surface for purchase-order lifecycle and goods receipt, preserving supplier, inventory, costing and payable evidence boundaries without moving those backend rules into the frontend.

This specification now has an explicitly approved visual working baseline, but implementation/activation remains blocked until `API-PRC-001..006` and `API-GRC-001..004` have executable WebApi evidence. The frontend must not activate `/compras` or present mock operations as live server behavior before that gate.

## 2. Functional authority

The view is derived from:

- `WEB-009 — Purchase Orders and Receipts`;
- `FR-067`, `FR-068`, `FR-071`;
- `UC-PROC-002 — Approve purchase order and receive goods`;
- accepted `API-PRC-*` and `API-GRC-*` contracts;
- `documentation/ui/procurement/UI-PROCUREMENT-001_RECONCILIATION.md`.

The API completion matrix remains the execution authority. Accepted contract design and visual approval do not by themselves prove executable capability.

## 3. Intended operators

Primary roles from the interface baseline:

- purchaser;
- inventory operator;
- administrator.

Permissions are operation-specific:

- `procurement.read` for read surfaces;
- `procurement.manage` for create/update/cancel purchase-order commands;
- `procurement.approve` for approval;
- `procurement.receive` for goods receipt creation/posting.

Role names must not substitute for permission evaluation.

## 4. Information architecture

### 4.1 Page header

Show:

- title: `Órdenes de compra y Recepciones`;
- concise procurement subtitle;
- optional API/demo availability indicator while execution is unavailable;
- visually reserved `Nueva orden de compra` action.

Until API execution exists, any prototype action is illustrative/disabled in a running WebApp context.

### 4.2 Purchase-order list

The approved baseline provides a dense operational list/table with responsive equivalents for:

- purchase-order reference/ID;
- supplier context;
- lifecycle/status concept;
- currency when present in illustrative data;
- expected date when present;
- compact line or quantity summary;
- receipt/progress context when visually useful.

The visual must not invent authoritative totals, taxes, payment state or costing fields beyond what later DTOs expose.

### 4.3 Purchase-order detail

Selecting an order should provide:

- order identity;
- supplier;
- lifecycle progression;
- expected dates;
- line items;
- ordered quantities;
- illustrative unit-cost/currency placement where appropriate;
- received quantity/progress context;
- linked receipt references when available;
- discrepancy indication;
- concurrency/version metadata only when the executable contract exposes it.

### 4.4 Lifecycle presentation

The visual language should make the conceptual progression legible without claiming final wire enum names:

1. draft/requested;
2. approved;
3. receipt in progress when applicable;
4. received/posted;
5. cancelled when permitted.

Exact transition eligibility remains backend authority.

### 4.5 Receipt surface

Goods-receipt inspection should support:

- linked purchase-order context;
- receipt identity;
- supplier/location context when available;
- ordered versus actually received quantities;
- discrepancy visibility;
- receipt line costs as captured evidence where applicable;
- posting-state concept;
- linked downstream evidence only when exposed by the backend.

The UI must not silently normalize quantity/cost mismatches.

## 5. Purchase-order editing visual flow

The approved visual baseline reserves the interaction language for a future drawer/modal/page composition covering:

- supplier selection;
- currency;
- expected date;
- one or more item lines;
- quantity;
- unit cost presentation;
- optional reference/reason context.

Because the HTTP implementation is missing, final required fields, validation copy and DTO constraints remain non-authoritative.

## 6. Approval flow

The approved baseline may reserve contextual approval affordances, but eventual availability must depend on `procurement.approve`, current order state and executable backend rules.

The frontend must not calculate or assume approval thresholds/workflow levels that are not exposed by backend policy.

## 7. Goods receipt flow

The receipt visual flow should make three things unambiguous:

- what was ordered;
- what is being received;
- what differs.

Posting a receipt belongs to the server transaction boundary. The UI must not locally apply stock, costing or payable mutations.

## 8. Costing and payable evidence

`FR-068` and `FR-071` create important downstream traceability, but this view is not a costing-policy editor and not an accounts-payable workbench.

The baseline may reserve compact linked-evidence areas for:

- inventory receipt/movement reference;
- costing result/policy reference;
- payable/source evidence reference.

Those areas remain non-authoritative until executable DTOs expose them.

## 9. Concurrency and idempotency

Mutation operations are idempotent where contracted. The future executable UI must:

- use the shared idempotency infrastructure for create/update/approve/cancel/receipt/post commands as required;
- submit expected version when the executable contract requires it;
- surface conflict outcomes instead of silently overwriting newer state;
- refresh/reconcile authoritative server state before retrying.

The visual baseline leaves room for conflict/error feedback without destructive UI recovery.

## 10. States

The governed visual authority covers the visual language for:

- populated purchase-order list;
- selected purchase-order detail;
- goods-receipt detail;
- discrepancy state;
- empty state;
- loading;
- generic error;
- forbidden/permission state;
- conflict/version concept;
- validation-error concept;
- narrow responsive layout.

Because HTTP implementation is missing, exact server error copy remains non-authoritative.

## 11. Responsive behavior

Desktop:

- master-detail purchase-order composition preferred;
- receipt/discrepancy context visible without overwhelming line editing.

Tablet:

- administrative and warehouse-friendly layout;
- detail sections may stack while keeping approval/receipt context reachable.

Mobile:

- purchase orders become cards/compact rows;
- detail becomes single-column inspection;
- line items remain readable without relying on wide tables;
- actions remain reachable without hover;
- ordered/received/discrepancy values remain textually explicit.

## 12. Light/dark requirements

The approved baseline provides light/dark parity with the established eFactura shell.

Lifecycle, discrepancy and approval meaning must not depend on color alone.

## 13. Accessibility

Required design properties:

- keyboard-operable list/detail/forms/actions;
- accessible line-item editing;
- programmatic labels for supplier, currency, dates, quantities and costs;
- non-color-only lifecycle/discrepancy indicators;
- visible focus states;
- validation/conflict feedback associated with the affected control/context.

## 14. Explicit exclusions for v1 visual authority

The visual baseline does not authorize:

- EOQ/ROP execution or proposal management inside this route;
- supplier payment/allocation;
- accounts-payable aging/workbench behavior;
- received-CFE XML intake/validation;
- costing-policy configuration;
- local PPP/FIFO authoritative calculation;
- live inventory mutation from the client;
- automatic payable creation claims;
- arbitrary deletion/history rewrite;
- unrestricted supplier/location access;
- carrier/logistics tracking;
- export actions not contracted by the API;
- any live API claim while `API-PRC-*` and `API-GRC-*` remain `MISSING_HTTP`.

## 15. Approved visual baseline

Luis explicitly approved the exact governed candidate with:

> Apruebo UI-PROCUREMENT-001 v1-responsive-suite como baseline visual

Approved version:

```text
UI-PROCUREMENT-001 v1-responsive-suite
```

Approved visual authority:

`documentation/ui/references/approved/UI-PROCUREMENT-001/v1-responsive-suite/`

Immutable source identity:

- generation id: `4ee17308-4cac-4f95-b3f6-84c8f8ff1f6c`;
- dimensions: `1312 x 1199`;
- bytes: `1666762`;
- SHA-256: `a0f59fcc7b4a54f9bf9cd966b8dffad7165178ff9861cfe7f6d13266a797c6a5`.

The original candidate history remains under:

`documentation/ui/references/drafts/UI-PROCUREMENT-001/v1-responsive-suite/`

The PNG binary is not yet byte-identically preserved in Git, therefore `BINARY_PRESERVATION_PENDING` remains explicit and substitution/regeneration is forbidden.

Visual approval authorizes preservation of the visual reference only. It does not authorize `/compras` activation. React implementation/activation remains blocked until the procurement/receipt API lane provides executable evidence and this specification is reconciled again against that implementation.