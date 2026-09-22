# UI-FIS-001 — Inventory Entry

Status: `SPECIFICATION_READY / VISUAL_BASELINE_APPROVED / ROUTE_NOT_ACTIVE`

## Identity

- WEB: `WEB-013`
- UI: `UI-FIS-001`
- Product area: `Fiscal`
- Name: `Documentos fiscales`
- Candidate route: `/documentos-fiscales`
- Navigation state: `PLANNED_DISABLED`
- Runtime mode: not implemented

## Visual evidence

Approved baseline:

`UI-FIS-001 / v1-responsive-composite`

Approved artifact:

`f7f7b77c-d6c9-4366-9c1d-7767990c4746`

The approved artifact includes one desktop composition and one mobile responsive composition inside the same image.

## Governed purpose

Search and inspect fiscal-document lifecycle, immutable fiscal snapshot, DGI/transport/result state, artifacts, references and authorized correction/regularization/delivery context.

## Accepted roles

- cashier
- seller
- accountant
- fiscal administrator
- auditor

## Requirements

`FR-030`, `FR-031`, `FR-034..041`, `FR-044..049`.

## API dependencies

- `API-FIS-001` list fiscal documents
- `API-FIS-002` fiscal document detail
- `API-FIS-003` authorized XML download
- `API-FIS-004` authorized representation
- `API-FIS-005` fiscal lifecycle events
- `API-FIS-006` correction command
- `API-FIS-007` regularization work queue
- `API-FIS-008` regularization case detail
- `API-FIS-009` regularization resolution
- `API-FDL-001` fiscal-document deliveries
- `API-FDL-002` delivery request

Current implementation evidence for the eleven `WEB-013` dependencies: `MISSING_HTTP` / `NOT_YET_AUDITED` in Wave 5.

`API-FIS-010` exists separately as implemented envelope response-evidence collection and does not satisfy the UI dependency set above.

## Preview eligibility

A future preview may become `ACTIVE_VISUAL_PREVIEW` only with:

- a real responsive React surface;
- explicit local/demo fixtures;
- client-side search/filter/selection only;
- no executable fiscal HTTP operations;
- XML/representation/correction/regularization/delivery controls disabled or clearly non-executable;
- no local authoritative lifecycle mutation;
- `operations: []`;
- successful repository gates;
- separate deployed runtime review.

## Current decision

The visual baseline is approved and preserved, but `/documentos-fiscales` remains disabled until implementation is reviewed in a dedicated branch/PR.