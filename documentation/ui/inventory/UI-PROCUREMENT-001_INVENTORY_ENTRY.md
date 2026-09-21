# UI-PROCUREMENT-001 — Governed UI Inventory Entry

Status: `VISUAL_APPROVED / SOURCE_FINGERPRINTED / BINARY_PRESERVATION_PENDING / EXECUTION_BLOCKED_BY_API`

Mapping:

```text
WEB-009 -> UI-PROCUREMENT-001
```

Reserved route: `/compras`

Navigation state: `PLANNED_DISABLED`

## Evidence

- upstream scope: `documentation/blueprint-interface/interface-scope-baseline.json` (`WEB-009`);
- requirements: `FR-067`, `FR-068`, `FR-071`;
- lifecycle: `UC-PROC-002 — Approve purchase order and receive goods`;
- reconciliation: `documentation/ui/procurement/UI-PROCUREMENT-001_RECONCILIATION.md`;
- specification: `documentation/ui/specifications/UI-PROCUREMENT-001_PURCHASE_ORDERS_RECEIPTS.md`;
- approved visual authority: `documentation/ui/references/approved/UI-PROCUREMENT-001/v1-responsive-suite/README.md`;
- candidate history: `documentation/ui/references/drafts/UI-PROCUREMENT-001/v1-responsive-suite/README.md`;
- route/navigation authority: `documentation/ui/WEBAPP_NAVIGATION_MAP.md`.

## Approved visual baseline

Luis explicitly approved:

> Apruebo UI-PROCUREMENT-001 v1-responsive-suite como baseline visual

The exact approved source identity is:

- generation id: `4ee17308-4cac-4f95-b3f6-84c8f8ff1f6c`;
- dimensions: `1312 x 1199`;
- bytes: `1666762`;
- SHA-256: `a0f59fcc7b4a54f9bf9cd966b8dffad7165178ff9861cfe7f6d13266a797c6a5`.

The exact PNG binary is not yet byte-identically preserved in Git, so `BINARY_PRESERVATION_PENDING` remains explicit.

## Execution gate

Wave 4 currently records `API-PRC-001..006` and `API-GRC-001..004` as `MISSING_HTTP` with no current WebApi evidence.

Therefore visual approval does not authorize:

- React route activation;
- server-backed purchase-order commands;
- server-backed goods-receipt commands;
- local inventory mutation;
- local PPP/FIFO authority;
- accounts-payable payment/allocation behavior.

## Next gate

The visual-governance gate is complete. React implementation/activation remains blocked until executable procurement/receipt API evidence exists and a fresh UI/API reconciliation explicitly authorizes implementation.