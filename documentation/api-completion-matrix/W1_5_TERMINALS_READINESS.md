# W1.5 Terminals readiness and implementation evidence

Status: `IMPLEMENTED_PRE_MERGE / PROVIDER_REAL_QA_PASS / PRODUCTION_PROMOTION_NOT_AUTHORIZED / RUNTIME_ACCEPTANCE_PENDING`

Implementation baseline: PR #195 (`feat/w1-5-terminals`), reconciled with `main@59a286185e242dcd59fb1b946a7d19aae07a54b1` before documentation closeout.

Accepted field-level authority: `W1_5_TERMINAL_CONTRACT_PROPOSAL.md`, explicitly approved by Luis on 2026-09-20.

Scope: `API-ORG-007..010` only, plus the accepted active-terminal dependency invariant for existing `API-ORG-006 updateLocation`.

No endpoint outside this set is introduced by W1.5.

## Contract and implementation inventory

| API ID | operationId | Method / path | Permission | Idempotency | Current implementation |
|---|---|---|---|---|---|
| `API-ORG-007` | `listTerminals` | GET `/api/v1/terminals` | `organization.read` | NO | `IMPLEMENTED` |
| `API-ORG-008` | `registerTerminal` | POST `/api/v1/terminals` | `organization.manage` | REQUIRED | `IMPLEMENTED` |
| `API-ORG-009` | `getTerminal` | GET `/api/v1/terminals/{terminalId}` | `organization.read` | NO | `IMPLEMENTED` |
| `API-ORG-010` | `updateTerminal` | PATCH `/api/v1/terminals/{terminalId}` | `organization.manage` | REQUIRED | `IMPLEMENTED` |

The matching public v1 HTTP surfaces now exist in PR #195. Therefore the four rows move from `MISSING_HTTP` to `IMPLEMENTED` in the completion matrix. This is HTTP implementation accounting only and does not imply production acceptance or formal W1.5 closure.

## Implementation evidence

PR #195 contains the bounded W1.5 implementation:

- `Domain.Organizations.Terminal` aggregate with immutable server-owned identity, immutable normalized business code, active/inactive lifecycle and application-managed versioning;
- Application list/get/register/update use cases with `OrganizationAuthorization`, location validation, idempotency, optimistic concurrency, audit and transactional outbox evidence;
- `API-ORG-006` location-deactivation protection when active terminals remain assigned;
- provider-neutral EF persistence, normalized-code uniqueness and additive migration `20260921024500_V1Terminals`;
- `TerminalsController` exposing the exact four governed methods/routes/operation names;
- dedicated architecture tests;
- dedicated provider-real PostgreSQL and MySQL persistence/use-case integration tests.

Clean Architecture Guard #658 passed on implementation HEAD `7983df48a51e9e0f2a5dc11b63552cef6ab43a1b`, including build, architecture, API v1 cross-cutting, legacy unit tests, and PostgreSQL/MySQL transaction integration tests. The implementation blobs were then reconciled unchanged onto live `main` UI-only history. Final exact-head CI after documentation reconciliation remains the authoritative pre-merge gate.

No production migration, Neon production write, Cloud Run deployment or public runtime mutation was performed by this implementation increment.

## Accepted public Terminal contract

Canonical projection:

```text
TerminalDto
- id: string
- organizationId: string
- code: string
- name: string
- locationId: string
- active: boolean
- version: long
```

Registration:

```text
TerminalCreateRequest
- code: string
- name: string
- locationId: string
```

Update:

```text
TerminalUpdateRequest
- name: string
- locationId: string
- active: boolean
- expectedVersion: long
```

Key accepted rules:

- server-generated opaque immutable `id`;
- server-resolved immutable `organizationId`;
- immutable normalized uppercase business `code`, maximum 64 characters;
- organization-level uniqueness on normalized code;
- mutable `name`, `locationId` and `active` only;
- two-state lifecycle, active/inactive;
- registration starts active at version 1;
- PATCH requires `expectedVersion` and increments application-managed version;
- no public delete;
- terminal/device/sync boundaries remain separate.

## Read behavior

`listTerminals` accepts bounded optional filters:

- `active`, defaulting to active-only;
- `locationId`.

The result is deterministic by normalized code then terminal ID. No pagination is introduced by W1.5.

`getTerminal` returns the canonical projection inside the resolved organization or the governed not-found behavior.

## Location invariants

- registration requires an existing active fiscal location in the same organization;
- reassignment requires an existing active destination location and current `expectedVersion`;
- reactivation requires an active requested location;
- inactive terminals may remain readable on a later-inactive location;
- an active terminal cannot be created, assigned or reactivated onto an inactive location;
- an existing fiscal location cannot be deactivated while active terminals remain assigned;
- administrators must first deactivate or reassign those terminals;
- no cascade terminal deactivation or hard delete occurs.

The last rule extends existing `API-ORG-006 updateLocation` compatibility behavior but introduces no new public endpoint or request schema.

## Stable conflict/error semantics

| Situation | HTTP | code | conflictType |
|---|---:|---|---|
| terminal not found | 404 | `organization.terminal_not_found` | n/a |
| location missing/cross-organization | 404 | `organization.location_not_found` | n/a |
| inactive target location for active binding | 409 | `organization.terminal.location_inactive` | `inactive_location` |
| location deactivation blocked by active terminals | 409 | `organization.location.active_terminals_exist` | `active_terminal_dependency` |
| duplicate normalized terminal code | 409 | `organization.terminal.code_duplicate` | `duplicate_terminal_code` |
| stale update | 409 | `concurrency_conflict` | `stale_version` |
| idempotency payload mismatch | 409 | `idempotency_key_reused` | `payload_mismatch` |

Validation remains HTTP 400 through the existing RFC 9457 Problem Details pipeline.

## Preserved architecture

The implementation extends the current Organization slice and preserves:

- `OrganizationAuthorization.EnsureRead/EnsureManage`;
- `V1OrganizationContextResolver`;
- provider-neutral repository interfaces;
- application-managed optimistic concurrency;
- transaction manager and unit of work;
- request-hash idempotency store;
- durable audit writer;
- transactional outbox;
- PostgreSQL/MySQL neutrality;
- current Clean Architecture dependency direction.

`LocationsController` remains the closest structural precedent for organization resolution, permissions, idempotent POST/PATCH, request hashing, replay header, `CreatedAtAction` and canonical projection after mutation. Fiscal-location fields were not copied into Terminal beyond the accepted relationship through `locationId`.

## Terminal vs Device boundary

Terminal remains organization/POS operational master data under `organization.read/manage`.

Device remains offline/sync registration under its separate sync permissions and identity model.

W1.5 does not absorb device IDs, client operation identity, device secrets, bearer/refresh tokens, offline grants, MAC/IP addresses, hardware fingerprints, printer configuration, CAE configuration, arbitrary metadata or heartbeat telemetry.

## Persistence implementation

The additive provider-neutral terminal master persistence contains:

- terminal ID;
- organization ID;
- original and normalized code;
- name;
- location ID;
- active flag;
- version;
- normal V1 persistence timestamps.

The implementation enforces deterministic organization-level normalized-code uniqueness and supports the active-terminal dependency check before location deactivation. Dedicated integration tests exercise the model against PostgreSQL and MySQL.

Production schema promotion remains a separate explicit approval gate. The migration source is present but has not been applied to Neon production by this PR.

## Mutation evidence

Accepted internal idempotency scopes:

```text
organization.terminal.register:{organizationId}
organization.terminal.update:{organizationId}:{terminalId}
```

A successful register/update mutation atomically includes:

- business mutation;
- completed idempotency record;
- durable audit evidence;
- transactional outbox evidence.

Accepted audit semantics remain `organization.terminal.registered` and `organization.terminal.updated`.

## QA coverage

The implementation increment covers, at source/architecture/provider-real layers as applicable:

- exact methods/routes/operationIds for all four operations;
- exact DTO/request contract placement;
- organization permissions and scope boundary wiring;
- server-owned ID/organization/active/version creation behavior;
- code normalization and uniqueness;
- deterministic list filters/order;
- create/update idempotent replay;
- same-key changed-payload conflict;
- stale-version conflict;
- missing/cross-organization/inactive location behavior;
- reassignment/reactivation rules;
- location deactivation dependency protection;
- no public delete/cascade behavior;
- audit/outbox/idempotency durability;
- PostgreSQL and MySQL persistence behavior;
- existing architecture/cross-cutting/unit regression suites.

Public deployed HTTP acceptance still must explicitly prove OpenAPI, 401/403 behavior, runtime Problem Details, idempotency/conflict semantics, organization isolation and required regressions after the accepted deployment.

## Remaining implementation gate

The field-level contract prerequisite is closed and the bounded implementation exists.

Before PR #195 can be merge-approved, its final exact HEAD must pass the complete Clean Architecture Guard after matrix/documentation reconciliation. After merge, a post-merge Guard must also pass.

Production schema promotion, deployment and runtime acceptance remain separate protected steps. No production database write or deployment is authorized by an implementation merge alone.
