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
- `REVIEWED`: implementation was compared against the approved visual baseline;
- `ACCEPTED`: visual/functional review is accepted.

These are UI-governance states and do not replace Blueprint project maturity fields.

## Identifier rule

The upstream `WEB-*` identifier is retained as traceability evidence. A governed view receives a stable `UI-*` identifier only when its boundary is clear enough to specify without fabricating backend behavior.

Example mapping format:

```text
WEB-003 -> UI-POS-001
```

The mapping must be recorded in the view specification and inventory entry. The upstream identifier is never silently discarded.

## Reconciled starting candidates

| Upstream ID | Candidate | Governed UI ID | Current UI status | Evidence |
| --- | --- | --- | --- | --- |
| `WEB-001` | Login and Session Entry | not assigned | `CANDIDATE` | token acquisition remains external/deployment-specific |
| `WEB-002` | Operational Dashboard | not assigned | `CANDIDATE` | contracted dashboard/alerts dependencies not currently evidenced as implemented public routes |
| `WEB-003` | POS Sale | `UI-POS-001` | `SPECIFIED` | `UI-POS-001_RECONCILIATION.md`, `../specifications/UI-POS-001_POS.md` |
| `WEB-004` | Customers and Parties | `UI-CUSTOMER-001` | `VISUAL_DRAFT` | `UI-CUSTOMER-001_RECONCILIATION.md`, `../specifications/UI-CUSTOMER-001_CUSTOMERS.md`, `../references/drafts/UI-CUSTOMER-001/v1-desktop.jpg` |

`UI-CUSTOMER-001` has a repository-stored visual draft but no approved visual baseline. `VISUAL_DRAFT` records only that one or more governed reference candidates exist.

No row implies visual approval or frontend implementation. Reconciliation must use current repository evidence, not historical unresolved-API notes alone.
