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

## First reconciliation candidates

| Upstream ID | Candidate | Governed UI ID | Current UI status | Evidence |
| --- | --- | --- | --- | --- |
| `WEB-001` | Login and Session Entry | not assigned | `CANDIDATE` | pending reconciliation |
| `WEB-002` | Operational Dashboard | not assigned | `CANDIDATE` | pending reconciliation |
| `WEB-003` | POS Sale | `UI-POS-001` | `VISUAL_DRAFT` | `UI-POS-001_RECONCILIATION.md`, `../specifications/UI-POS-001_POS.md`, `../references/drafts/UI-POS-001/` |

`UI-POS-001` is not visually approved. Its `VISUAL_DRAFT` status means that a governed functional specification exists and repository-stored draft references now exist, but no visual baseline has been approved.

The v1 references are exploratory and currently marked `NOT_APPROVAL_READY` because their visual review found assumptions that exceed the current authoritative backend surface. Those findings are recorded in `../references/drafts/UI-POS-001/README.md`.

No row implies visual approval or frontend implementation. Reconciliation must use current repository evidence, not historical unresolved-API notes alone.
