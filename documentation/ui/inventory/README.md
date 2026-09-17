# Governed UI Inventory

Status: `ACTIVE / RECONCILIATION_REQUIRED`

This area converts upstream interface scope candidates into governed UI views.

## Upstream source

The current upstream scope lives in:

- `documentation/blueprint-interface/01_INTERFACE_SCOPE_BASELINE.md`
- `documentation/blueprint-interface/interface-scope-baseline.json`

Those artifacts are scope evidence, not executable frontend inventory. Their `WEB-*` and Android items must be reconciled against the current API/application implementation before a stable `UI-*` view is committed.

## Reconciliation states

Each candidate should be classified as one of:

- `CANDIDATE`: upstream scope exists but has not been reconciled against current backend capability;
- `SPECIFICATION_READY`: authoritative requirements/use cases/contracts are sufficient to write the governed view specification;
- `SPECIFIED`: functional specification exists but no visual baseline is approved;
- `VISUAL_DRAFT`: one or more reference drafts exist;
- `VISUAL_APPROVED`: Luis explicitly approved one exact visual version;
- `IMPLEMENTED`: frontend implementation exists;
- `REVIEWED`: implementation was compared against the approved visual baseline and runtime-reviewed;
- `ACCEPTED`: visual/functional review is accepted and all governance prerequisites are satisfied.

These are UI-governance states and do not replace Blueprint project maturity fields.

## Identifier rule

The upstream `WEB-*` identifier is retained as traceability evidence. A governed view receives a stable `UI-*` identifier only when its boundary is clear enough to specify without fabricating backend behavior.

Example mapping format:

```text
WEB-003 -> UI-POS-001
```

The mapping must be recorded in the view specification and inventory entry. The upstream identifier is never silently discarded.

## First reconciliation candidates

| Upstream ID | Candidate | Governed UI ID | Current UI status | Evidence |
| --- | --- | --- | --- | --- |
| `WEB-001` | Login and Session Entry | not assigned | `CANDIDATE` | pending reconciliation |
| `WEB-002` | Operational Dashboard | not assigned | `CANDIDATE` | pending reconciliation |
| `WEB-003` | POS Sale | `UI-POS-001` | `REVIEWED` | `UI-POS-001_RECONCILIATION.md`, `../specifications/UI-POS-001_POS.md`, `../references/approved/UI-POS-001/v3-theme-pair/`, `../reviews/UI-POS-001/D1_5_RUNTIME_VISUAL_ACCEPTANCE.md` |
| `WEB-004` | Customers and Parties | `UI-CUSTOMER-001` | `VISUAL_APPROVED` | `UI-CUSTOMER-001_RECONCILIATION.md`, `../specifications/UI-CUSTOMER-001_CUSTOMERS.md`, `../references/approved/UI-CUSTOMER-001/v2-theme-pair/README.md` |

## UI-POS-001 closure note

`UI-POS-001` has an approved `v3-theme-pair`, a deployed React implementation, and explicit cross-browser runtime acceptance by Luis on 2026-09-17 after successful rendering in Firefox and Chrome Incognito.

The visual/runtime implementation lane is therefore closed as `REVIEWED / VISUAL_RUNTIME_ACCEPTED`.

The inventory intentionally does not promote the row to final `ACCEPTED` yet because `UI-POS-001_POS.md` records an independent governance prerequisite: a governed `US-*` user-story artifact for this flow is still missing. That traceability gap must be resolved in its proper governance lane and must not be fabricated by frontend work.

## UI-CUSTOMER-001 visual approval note

`UI-CUSTOMER-001 v2-theme-pair` was explicitly approved by Luis on 2026-09-16. Its approved repository artifact is preserved under `documentation/ui/references/approved/UI-CUSTOMER-001/v2-theme-pair/`. The older `v1-desktop` remains historical draft evidence only.

Visual approval does not imply frontend implementation or acceptance. The governed customer specification and reconciliation preserve the current contract boundary and keep unsupported account-summary/reference-data capabilities pending rather than fabricating them.

Reconciliation and future views must continue to use current repository evidence rather than historical unresolved-API notes alone.
