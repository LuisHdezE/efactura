# UI-PAYABLE-001 — Governed UI Inventory Entry

Status: `SPECIFICATION_READY / VISUAL_BASELINE_APPROVED / ROUTE_NOT_ACTIVE`

Mapping:

```text
WEB-011 -> UI-PAYABLE-001
```

Candidate route: `/cuentas-por-pagar`

Navigation state: `PLANNED_DISABLED`

## Evidence

- upstream scope: `documentation/blueprint-interface/interface-scope-baseline.json` (`WEB-011`);
- requirements: `FR-026`, `FR-027`, `FR-071`, `FR-072`, `FR-073`, `FR-074`;
- lifecycle: `UC-AP-001 — Record supplier payment and allocate`;
- reconciliation: `documentation/ui/payables/UI-PAYABLE-001_RECONCILIATION.md`;
- specification: `documentation/ui/specifications/UI-PAYABLE-001_ACCOUNTS_PAYABLE_SUPPLIER_PAYMENTS.md`;
- approved visual reference: `documentation/ui/references/approved/UI-PAYABLE-001/v1-responsive-suite/README.md`.

## Approved visual baseline

Approved baseline: `UI-PAYABLE-001 / v1-responsive-suite`.

Artifact identities:

- desktop gen_id: `764a3a30-08a6-4f4f-8bda-2bdaf245b8c2`;
- mobile gen_id: `06ffefef-b352-4967-9585-8012b03e7c6a`.

The suite governs desktop and mobile composition, hierarchy, status presentation, selected-payable detail and the disabled supplier-payment action treatment.

## Accepted API dependency

The view depends on:

- `API-AP-001` list payables;
- `API-AP-002` payable detail;
- `API-AP-003` payable aging;
- `API-AP-004` append payable adjustment;
- `API-PAY-001` create supplier payment/allocation;
- `API-PAY-002` supplier-payment detail;
- `API-PAY-003` compensating supplier-payment reversal.

Wave 3 currently records all seven as `MISSING_HTTP` with no WebApi evidence and `NOT_YET_AUDITED` deep readiness.

## Execution boundary

This approved baseline does **not** authorize:

- route activation;
- authoritative payable/aging claims;
- supplier-payment posting;
- payable adjustment;
- supplier-payment reversal;
- local authoritative balance mutation;
- cash/bank side effects;
- silent payment capping/truncation;
- invented unapplied-payment/advance policy.

## Next gate

Reconcile `UI-PAYABLE-001` with the existing visual-preview route policy. If promoted to `ACTIVE_VISUAL_PREVIEW`, implementation may use explicit local demo fixtures and client-side inspection/filtering only, with all server-owned financial operations disabled until the accepted API dependencies exist.