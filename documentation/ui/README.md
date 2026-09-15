# eFactura UI Governance

Status: `ACTIVE / GOVERNANCE FOUNDATION`

This directory governs the visual/frontend lane of eFactura. It is intentionally separate from backend implementation while remaining traceable to authoritative requirements, API contracts and Application use cases.

## Authority and scope

The visual lane MUST NOT invent product capability. A UI action may be designed as executable only when the corresponding backend capability and contract are evidenced. Missing capabilities remain explicitly pending, simulated or unavailable.

Existing `documentation/blueprint-interface/` artifacts remain upstream scope evidence. In particular, `interface-scope-baseline.json` is a `SCOPE_BASELINE` whose entries are `PROPOSED`; those entries are not automatically approved views or frontend-ready specifications.

## Governed chain

Every relevant view must support this trace:

```text
Business Requirement
        ↓
User Story
        ↓
Use Case
        ↓
UI View
        ↓
Visual Reference
        ↓
Frontend Implementation
        ↓
API Contract
        ↓
Application Use Case
```

A view is fully governed only when these three artifacts are linked:

```text
WHAT IT MUST DO
Functional specification

HOW IT MUST LOOK
Approved visual reference

WHAT WAS BUILT
Verifiable implementation
```

## Required lifecycle

1. Reconcile the candidate view against requirements, real use cases and current backend/API capability.
2. Assign a stable UI identifier.
3. Write the functional specification before implementation.
4. Produce one or more visual drafts.
5. Store drafts under `references/drafts/<UI-ID>/`.
6. Obtain explicit human approval from Luis for one exact version.
7. Preserve that exact approved artifact under `references/approved/<UI-ID>/`.
8. Implement or adjust the frontend against the approved baseline.
9. Capture the running implementation.
10. Perform and record a visual/functional comparison.
11. Correct significant drift or document an explicit accepted deviation.

## Approval rule

A stored draft is NOT an approved baseline.

Messages such as `adelante`, `continúa`, `seguimos` or `avanza` do not constitute final visual approval.

Approval must identify the view and version unambiguously, for example:

```text
Apruebo la vista UI-POS-001 v3
```

An approved visual reference must never be silently overwritten. A material change creates a new version and requires a new explicit approval.

## Minimum functional specification

Before a view can be implemented, its specification must cover at least:

- view name and stable identifier;
- objective and problem solved;
- users/roles;
- related business requirements;
- related user stories;
- related use cases;
- data displayed and authority/source;
- available actions;
- visible business rules;
- states;
- validation;
- permissions;
- empty states;
- loading states;
- errors;
- confirmations;
- responsive behavior;
- dependencies on other views;
- required API endpoints/contracts;
- backend-supported functionality;
- pending or simulated functionality.

## Repository structure

```text
documentation/ui/
├── inventory/
├── specifications/
├── references/
│   ├── drafts/
│   └── approved/
├── reviews/
└── README.md
```

Git cannot preserve empty directories, so each governed area contains its own README/template until concrete view artifacts exist.

## Visual review minimum

Post-implementation review must compare the approved reference with the running frontend across at least:

- structure and hierarchy;
- component position;
- spacing;
- typography;
- color;
- iconography;
- dimensions;
- responsive behavior;
- forms;
- tables;
- states;
- interactions.

## Current starting point

At creation of this foundation, the repository already contains an upstream interface scope baseline under `documentation/blueprint-interface/`, including web candidates such as `WEB-001` Login and Session Entry, `WEB-002` Operational Dashboard and `WEB-003` POS Sale. Those records remain upstream proposed scope until each candidate is reconciled into this governed UI lane.
