# Architecture / Security / Data Acceptance Draft

## Purpose

Define the evidence required before this boundary can be declared `Architecture / Security / Data Ready` under Blueprint 0.5.1.

Current state: **DRAFT / NOT ACCEPTED**.

## Proposed acceptance matrix

| ID | Check | Current design evidence | Status |
|---|---|---|---|
| ARCH-001 | architectural style and Brownfield migration strategy defined | 01, ADR-001/002/012 | READY_FOR_REVIEW |
| ARCH-002 | dependency direction and framework isolation defined | 01, 02 | READY_FOR_REVIEW |
| ARCH-003 | business module ownership/boundaries defined | 02 | READY_FOR_REVIEW |
| ARCH-004 | aggregates/invariants defined for critical domains | 03 | READY_FOR_REVIEW |
| ARCH-005 | value objects/domain policies defined | 04 | READY_FOR_REVIEW |
| ARCH-006 | application ports/transaction boundaries defined | 05 | READY_FOR_REVIEW |
| DATA-001 | authoritative target relational model identified | 06 | READY_FOR_REVIEW |
| DATA-002 | PostgreSQL/MySQL portability strategy defined | 06, ADR-004/015 | READY_FOR_REVIEW |
| DATA-003 | migration/cutover avoids destructive rewrite | 06 | READY_FOR_REVIEW |
| SEC-001 | authentication architecture defined | 07 | READY_FOR_REVIEW |
| SEC-002 | permission/scoped authorization architecture defined | 07 | READY_FOR_REVIEW |
| SEC-003 | durable audit architecture defined | 07 | READY_FOR_REVIEW |
| SEC-004 | threat model applicability acknowledged | 07, ADR-017 | PARTIAL |
| REL-001 | idempotency/inbox/outbox defined | 08 | READY_FOR_REVIEW |
| REL-002 | offline/sync/CFC separation defined | 08 | READY_FOR_REVIEW |
| FISC-001 | fiscal adapter/numbering/lifecycle architecture defined | 09 | READY_FOR_REVIEW |
| FISC-002 | cross-border fiscal-selection architecture preserved | 04, 09 | READY_FOR_REVIEW |
| API-001 | future API version/error/idempotency principles defined | 10 | READY_FOR_REVIEW |
| API-002 | Brownfield route compatibility strategy defined | 10, ADR-012 | READY_FOR_REVIEW |
| CONF-001 | future architecture fitness/conformance plan defined | 01 | READY_FOR_REVIEW |

## Remaining blockers before PASS

1. Human review/acceptance of the target/requirements boundary on which this architecture depends.
2. Human architecture decision on the proposed ADR set.
3. Materialized threat model with trust boundaries/data flows/abuse cases/mitigations for the applicable high-risk surfaces.
4. Final decision/pinning of the MySQL EF Core provider before Data Ready is used to authorize migrations.
5. Confirm whether Release-1 scope needs any OPEN business policy early enough to affect the first implementation slice.
6. Verify that no DGI-sensitive rule still marked OPEN is accidentally treated as architecture-complete implementation guidance.

Direct-DGI/provider and certificate custody may remain adapter/configuration decisions until their implementation slice, but the fiscal gateway/signing ports must be accepted now.

## Required implementation-conformance evidence later

After coding begins, a separate Blueprint 0.5.1 check must prove the implementation conforms to this accepted contract. Planned executable evidence:

- ApplicationCore prohibited-reference test/static rule;
- project-reference/dependency graph test;
- module-boundary tests where practical;
- DI composition tests;
- PostgreSQL/MySQL persistence contract suite;
- authorization policy integration tests;
- concurrency test for fiscal numbering and stock/payment contested updates;
- idempotency replay/conflict tests;
- outbox/inbox crash/retry tests;
- fiscal state-machine tests;
- no-secret/redaction tests where automatable.

This artifact cannot mark `architecture_implementation_conformance` PASS. Design acceptance happens before implementation; conformance happens after.

## Human decision

No architecture gate is automatically passed by this documentation commit. Review must classify each proposed ADR as:

`ACCEPT / CHANGE / DEFER / N/A`

and record the decision before implementation authorization.
