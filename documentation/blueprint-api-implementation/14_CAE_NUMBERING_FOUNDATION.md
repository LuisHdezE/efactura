# API Implementation 14 — CAE and Atomic Fiscal Number Allocation

Status: MERGED / VERIFIED_IN_MAIN

Merged PR: #31 `feat(cae): add atomic fiscal number allocation v1`

## Purpose

Establish the Release-1 CAE authorization/allocation boundary and a server-authoritative fiscal-number reservation mechanism without crossing into final CFE issuance, XML/signing or DGI transport.

## Accepted surface

The slice implements the contracted API-CAE-001..007 family for CAE metadata management and allocation lifecycle. It also introduces the internal Application port `IFiscalNumberAllocator`.

There is deliberately no public `/next-number` endpoint. Fiscal numbering is reserved internally by the operation that requires a number so clients cannot prefetch or invent authoritative fiscal identity.

## Core model and invariants

- `CaeAuthorization` represents accepted authorization metadata.
- operational `CaeAllocation` tracks the bounded usable range/lifecycle.
- fiscal-number reservation is immutable business evidence.
- allocation/reservation selection is server-authoritative and concurrency-aware.
- the database independently protects uniqueness of `(organization, CFE type, series, number)`.
- CAE commands and fiscal-number reservation/exhaustion evidence participate in durable audit, outbox and idempotency foundations.
- write repositories do not own transaction boundaries or `SaveChanges` and do not introduce ad-hoc mutation SQL.
- validity dates and identifiers are persisted using provider-portable mappings accepted by both PostgreSQL and MySQL.

## Concurrency and failure behavior

The slice combines application orchestration, provider-neutral optimistic/concurrency handling and database uniqueness so two competing reservations cannot successfully own the same fiscal identity.

Injected post-flush failures are required to roll back the fiscal reservation plus related audit/outbox/idempotency evidence as one transaction. Exhaustion remains explicit business state rather than silent number reuse.

## Verification at acceptance

The accepted branch was validated with Release build, architecture guards and dual-provider PostgreSQL/MySQL integration, including atomic numbering, unique fiscal identity, rollback/retry and exhaustion behavior.

Later consolidated validation on current main includes these cases inside the 93/93 persistence suite and 170/170 total automated baseline.

## Explicit non-goals

Not implemented by this slice:

- CFE XML generation;
- XML signing;
- certificate/private-key custody;
- DGI/provider transport;
- final CFE issuance;
- `confirmSale`;
- payments/receivables;
- correction notes;
- contingency;
- daily fiscal reporting.

Direct DGI versus provider and production certificate/key custody remain separate decisions. The Release-1 CAE verifier establishes bounded metadata consistency; it does not claim cryptographic DGI authenticity.
