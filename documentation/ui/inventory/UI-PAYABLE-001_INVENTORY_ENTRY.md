# UI-PAYABLE-001 — Governed UI Inventory Entry

Status: `IMPLEMENTED_VISUAL_PREVIEW / RUNTIME_REVIEW_PENDING`

Mapping:

```text
WEB-011 -> UI-PAYABLE-001
```

Route: `/cuentas-por-pagar`

Navigation state: `ACTIVE_VISUAL_PREVIEW`

## Evidence

- upstream scope: `documentation/blueprint-interface/interface-scope-baseline.json` (`WEB-011`);
- requirements: `FR-026`, `FR-027`, `FR-071`, `FR-072`, `FR-073`, `FR-074`;
- lifecycle: `UC-AP-001 — Record supplier payment and allocate`;
- reconciliation: `documentation/ui/payables/UI-PAYABLE-001_RECONCILIATION.md`;
- specification: `documentation/ui/specifications/UI-PAYABLE-001_ACCOUNTS_PAYABLE_SUPPLIER_PAYMENTS.md`;
- approved visual reference: `documentation/ui/references/approved/UI-PAYABLE-001/v1-responsive-suite/README.md`;
- preview policy: `documentation/ui/PREVIEW_ROUTE_POLICY_AMENDMENT.md`;
- React implementation: `src/WebApp/src/features/payables/PayablesPage.tsx`;
- source-level guard: `src/WebApp/scripts/verify-payables-preview.mjs`.

## Approved visual baseline

Approved baseline: `UI-PAYABLE-001 / v1-responsive-suite`.

Artifact identities:

- desktop gen_id: `764a3a30-08a6-4f4f-8bda-2bdaf245b8c2`;
- mobile gen_id: `06ffefef-b352-4967-9585-8012b03e7c6a`.

The suite governs desktop and mobile composition, hierarchy, status presentation, selected-payable detail and disabled supplier-payment treatment.

## Accepted API dependency

The view depends on:

- `API-AP-001` list payables;
- `API-AP-002` payable detail;
- `API-AP-003` payable aging;
- `API-AP-004` append payable adjustment;
- `API-PAY-001` create supplier payment/allocation;
- `API-PAY-002` supplier-payment detail;
- `API-PAY-003` compensating supplier-payment reversal.

All seven remain `MISSING_HTTP` in Wave 3.

## Preview execution boundary

The active preview is authorized to use local demonstration fixtures for client-side inspection only. It does **not** authorize:

- authoritative payable/aging claims;
- supplier-payment posting;
- payable adjustment;
- supplier-payment reversal;
- local authoritative balance mutation;
- cash/bank side effects;
- silent payment capping/truncation;
- invented unapplied-payment/advance policy.

`UI-PAYABLE-001` therefore remains `IMPLEMENTED_VISUAL_PREVIEW` with `operations: []` until fresh API evidence exists.

## Runtime review

After merge/deploy, desktop and mobile behavior must be inspected separately against the approved responsive suite. Search, filters, selection/detail and disabled financial controls are part of runtime acceptance.
