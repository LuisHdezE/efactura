# UI-INVENTORY-001 — Visual checkpoint

Status: `VISUAL_APPROVED / IMPLEMENTATION_READY / BINARY_PRESERVATION_PENDING`

Date: 2026-09-20

Upstream: `WEB-007 — Inventory and Movements`

Governed UI: `UI-INVENTORY-001`

Reserved route: `/inventario`

## Human approval

Luis approved moving forward with the presented visual proposals with the statement:

> ya esta bien, podemos pasar a usar esas propuestas

This approval authorizes implementation from the responsive visual suite recorded at:

`documentation/ui/references/approved/UI-INVENTORY-001/v1-responsive-suite/README.md`

## Governance interpretation

The approval is visual, not a relaxation of the functional contract.

Implementation must preserve the approved visual direction while taking functional truth from:

- `UI-INVENTORY-001_INVENTORY_MOVEMENTS.md`;
- `UI-INVENTORY-001_RECONCILIATION.md`;
- current executable API/Application evidence.

Illustrative mockup elements outside that contract remain non-authoritative and must be omitted or rendered unavailable rather than simulated as real business capability.

## Approved direction

- compact eFactura shell reused;
- light/dark parity;
- dense desktop inventory list + selected-position detail;
- responsive mobile list-first experience;
- movement-history detail treatment;
- manual-adjustment surface aligned with `UC-INV-001` when write integration is enabled;
- no fork of shared Sidebar, Topbar, BottomBar or MobileNavigation.

## Explicit implementation exclusions

The visual proposals do not authorize:

- stock-threshold classifications (`Stock bajo`, `Sin stock`, etc.) without authoritative policy/data;
- unsupported filters;
- generic inventory edit;
- export;
- product media as inventory truth;
- transfers;
- procurement/receipts;
- EOQ/ROP execution;
- valuation/costing;
- reserved/available-to-sell quantities;
- invented movement kinds;
- invented negative-stock policy.

## Next gate

Create the React implementation increment, activate `/inventario` only through the unified registry, keep demo/mock boundaries explicit where real WebApp API integration is not enabled, pass Frontend Demo CI and repository gates, then request explicit merge authorization and perform deployed light/dark/mobile runtime review.
