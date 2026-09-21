# UI-CASH-001 — Approved Visual Reference

Baseline: `v1-responsive-composite`

Status: `APPROVED_VISUAL_BASELINE`

Approval date: `2026-09-21`

Approval statement:

> aprobado

## Artifact identity

- UI: `UI-CASH-001 — Cash Shift and Reconciliation`
- WEB mapping: `WEB-012`
- reserved route candidate: `/caja`
- approved composite image generation id: `4af42487-0a36-43aa-832e-ef96c7f11a61`
- artifact contains one desktop composition plus one mobile responsive composition in the same approved image.

## Approved visual composition

The approved composite establishes presentation authority for:

- `Caja y conciliación` hierarchy inside the shared eFactura shell;
- finance navigation with `Caja y conciliación` selected;
- summary/KPI cards above the working surface;
- movement/reconciliation tabs and search/filter controls;
- a dense desktop ledger with textual status treatment;
- selected-item detail/history panel on desktop;
- a single-column mobile hierarchy with compact KPI cards and movement cards;
- explicit demo/preview disclosure;
- the established eFactura dark-blue visual language and responsive treatment.

## Functional authority boundary

The artifact is **visual authority only**. Current product authority for `WEB-012` is narrower and comes from the governed source documents:

- open/close cash shifts;
- inspect expected amounts;
- enter counted values;
- calculate and reconcile variances;
- inspect shift movements.

Some labels in the approved visual, such as generic bank-account wording, account totals or export-oriented copy, are illustrative visual fixtures and do not create new banking/account-management requirements. A future React implementation must preserve the approved hierarchy and visual language while using terminology and behavior supported by the accepted CashManagement contract.

## Governance boundaries

This baseline does not authorize:

- route activation;
- live cash-shift state claims;
- opening a canonical shift;
- posting a manual cash movement;
- closing a shift;
- reconciling a variance;
- inventing expected/count totals;
- inventing approval/tolerance decisions;
- mutating closed-shift history;
- generic bank-account management not present in the accepted product scope.

`API-CSH-001..007` remain separate backend/API authority and are currently `MISSING_HTTP`.

## Responsive authority

Because desktop and mobile are present inside one explicitly approved image, this baseline is named `v1-responsive-composite` rather than a multi-artifact suite. Intermediate tablet behavior may adapt while preserving hierarchy, information priority, textual status cues and execution boundaries.

## Next gate

Before activation of `/caja`, reconcile this visual baseline with the active preview-route policy and implement a responsive React preview using explicit local demonstration data. All server-owned cash commands must remain disabled until fresh executable API evidence exists.