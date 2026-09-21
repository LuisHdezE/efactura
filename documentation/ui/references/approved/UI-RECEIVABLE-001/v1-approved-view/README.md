# UI-RECEIVABLE-001 — Approved Visual Reference

Baseline: `v1-approved-view`

Status: `APPROVED_VISUAL_BASELINE`

Approval date: `2026-09-21`

Approval statement:

> correcto, aprobada esta vista

## Artifact identity

- UI: `UI-RECEIVABLE-001 — Accounts Receivable and Collections`
- WEB mapping: `WEB-010`
- reserved route: `/cuentas-por-cobrar`
- image generation id: `1fad639a-815f-431b-8c1c-7e6a50cdae77`

## Approved composition

The approved visual establishes the working presentation authority for:

- `Cuentas por cobrar` page hierarchy inside the eFactura shell;
- aging/open-balance KPI cards;
- filterable receivables ledger/list;
- textual financial states such as `Por vencer`, `Vencido`, `Parcial` and `Cobrado`;
- selected-account master/detail presentation;
- immutable-looking history/timeline presentation;
- `Registrar cobro` composer placement and information hierarchy;
- explicit demo/non-executable treatment for collection posting;
- explicit excess/policy-result area without inventing backend policy;
- mobile/narrow-layout direction represented by the inset in the approved image.

## Governance boundaries

This baseline is **visual authority only**.

It does not authorize:

- route activation;
- live receivable/aging API claims;
- posting a collection;
- local mutation of authoritative balances;
- receivable adjustment;
- collection reversal;
- silent overpayment truncation;
- invented customer-advance/unapplied-payment policy;
- destructive financial-history editing.

`API-AR-001..004` and `API-COL-001..003` remain separate backend/API authority.

## Remaining visual coverage debt

The approved image contains the accepted desktop composition and a mobile/narrow-layout inset. A separately reviewed full dark-desktop companion is not represented by this exact artifact and must not be inferred from the approval.

## Next gate

Before React preview implementation or activation of `/cuentas-por-cobrar`, reconcile this approved baseline with the active preview-route governance. Live API integration remains a later, independent gate.