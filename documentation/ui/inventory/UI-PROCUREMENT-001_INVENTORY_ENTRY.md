# UI-PROCUREMENT-001 — Governed UI Inventory Entry

Status: `SPECIFICATION_READY / VISUAL_BASELINE_PENDING / EXECUTION_BLOCKED_BY_API`

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
- route/navigation authority: `documentation/ui/WEBAPP_NAVIGATION_MAP.md`.

## Execution gate

Wave 4 currently records `API-PRC-001..006` and `API-GRC-001..004` as `MISSING_HTTP` with no current WebApi evidence.

Therefore this inventory entry authorizes visual-baseline work only. It does not authorize:

- React route activation;
- server-backed purchase-order commands;
- server-backed goods-receipt commands;
- local inventory mutation;
- local PPP/FIFO authority;
- accounts-payable payment/allocation behavior.

## Next gate

Create and review a responsive light/dark visual candidate for `UI-PROCUREMENT-001`. Visual approval remains separate from route activation and implementation readiness.
