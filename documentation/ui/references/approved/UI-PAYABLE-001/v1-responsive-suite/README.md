# UI-PAYABLE-001 — Approved Visual Reference

Baseline: `v1-responsive-suite`

Status: `APPROVED_VISUAL_BASELINE`

Approval date: `2026-09-21`

Approval statements:

> Usa esta propuesta como baseline visual UI-PAYABLE-001.

> aprobado, adelante

## Artifact identity

- UI: `UI-PAYABLE-001 — Accounts Payable and Supplier Payments`
- WEB mapping: `WEB-011`
- reserved route candidate: `/cuentas-por-pagar`
- desktop image generation id: `764a3a30-08a6-4f4f-8bda-2bdaf245b8c2`
- mobile image generation id: `06ffefef-b352-4967-9585-8012b03e7c6a`

## Approved desktop composition

The desktop visual establishes presentation authority for:

- `Cuentas por pagar` hierarchy inside the shared eFactura shell;
- payable/open-balance KPI cards;
- pending and overdue obligation counts;
- supplier/document search and filtering;
- payable ledger/table with supplier, issue date, due date, amount and textual status;
- selected payable detail panel;
- supplier identity and fiscal reference context;
- information/history tabs;
- explicit pending-payment state;
- disabled supplier-payment execution control while backend authority is absent;
- preview disclosure making the non-live state explicit.

## Approved mobile composition

The mobile visual establishes responsive authority for:

- single-column page hierarchy;
- compact demo notice;
- two-column KPI grid;
- mobile search/filter row;
- obligation cards replacing the wide ledger table;
- readable supplier/document/due-date/status presentation;
- inline selected-payable detail below the obligation list;
- disabled `Registrar pago` control;
- persistent access to document/detail actions without implying live execution.

## Governance boundaries

This baseline is **visual authority only**.

It does not authorize:

- route activation;
- live payable or aging API claims;
- supplier-payment posting;
- payable adjustment;
- supplier-payment reversal;
- local authoritative balance mutation;
- silent truncation/capping of an entered payment;
- invented unapplied-payment/advance policy;
- destructive financial-history editing;
- cash/bank side effects without server authority.

`API-AP-001..004` and `API-PAY-001..003` remain separate backend/API authority.

## Responsive authority

Unlike the earlier receivables baseline, this baseline includes two separately reviewed artifacts: a full desktop view and a dedicated mobile view. Both are part of `v1-responsive-suite`.

The React implementation may adapt intermediate tablet breakpoints while preserving the hierarchy, semantics and execution boundaries established by these approved references.

## Next gate

Before React preview implementation or activation of `/cuentas-por-pagar`, reconcile this approved baseline with the active preview-route governance. Live API integration remains a later, independent gate.