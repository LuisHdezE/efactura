# W1.5 Terminals readiness

Status: `READINESS_AUDITED / CONTRACT_ACCEPTED / IMPLEMENTATION_READY_PENDING_MERGE`

Readiness baseline: `main@04fd00362f7535e15be8b9de8f10649eeac84192`.

Accepted field-level authority: `W1_5_TERMINAL_CONTRACT_PROPOSAL.md`, explicitly approved by Luis on 2026-09-20.

Scope: `API-ORG-007..010` only.

No endpoint outside this set is introduced by W1.5.

## Contract inventory

| API ID | operationId | Method / path | Permission | Idempotency | Current implementation |
|---|---|---|---|---|---|
| `API-ORG-007` | `listTerminals` | GET `/api/v1/terminals` | `organization.read` | NO | `MISSING_HTTP` |
| `API-ORG-008` | `registerTerminal` | POST `/api/v1/terminals` | `organization.manage` | REQUIRED | `MISSING_HTTP` |
| `API-ORG-009` | `getTerminal` | GET `/api/v1/terminals/{terminalId}` | `organization.read` | NO | `MISSING_HTTP` |
| `API-ORG-010` | `updateTerminal` | PATCH `/api/v1/terminals/{terminalId}` | `organization.manage` | REQUIRED | `MISSING_HTTP` |

The four operations remain unimplemented. Contract acceptance changes readiness only and does not change Wave 1 or global implementation counts.

## Readiness conclusion

The previously identified field-level contract prerequisite is now closed by the owner-approved Terminal contract.

W1.5 is therefore **implementation-ready once this contract-lock PR is merged**, subject to the normal bounded implementation PR, exact-head CI, migration approval, deployment and runtime-acceptance gates.

No production migration or production database write is authorized by this readiness state alone.

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

## Existing architecture to reuse

The implementation must extend the current Organization slice and preserve:

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

`LocationsController` remains the closest HTTP structural precedent for organization resolution, permissions, idempotent POST/PATCH, request hashing, replay header, `CreatedAtAction` and canonical projection after mutation. Fiscal-location fields must not be copied into Terminal beyond the accepted relationship through `locationId`.

## Terminal vs Device boundary

Terminal remains organization/POS operational master data under `organization.read/manage`.

Device remains offline/sync registration under its separate sync permissions and identity model.

W1.5 must not absorb device IDs, client operation identity, device secrets, bearer/refresh tokens, offline grants, MAC/IP addresses, hardware fingerprints, printer configuration, CAE configuration, arbitrary metadata or heartbeat telemetry.

## Persistence readiness

The accepted contract authorizes an additive provider-neutral terminal master persistence design containing at least:

- terminal ID;
- organization ID;
- normalized code;
- name;
- location ID;
- active flag;
- version;
- normal V1 persistence timestamps where appropriate.

The implementation must enforce deterministic organization-level normalized-code uniqueness and support an efficient active-terminal dependency check before location deactivation.

Exact table/index/FK names are implementation details to derive in the bounded implementation increment.

Production schema promotion remains a separate explicit approval gate.

## Mutation evidence

Accepted internal idempotency scopes:

```text
organization.terminal.register:{organizationId}
organization.terminal.update:{organizationId}:{terminalId}
```

A successful register/update mutation must atomically include:

- business mutation;
- completed idempotency record;
- durable audit evidence;
- transactional outbox evidence.

Accepted audit semantics remain `organization.terminal.registered` and `organization.terminal.updated`.

## Test readiness

The implementation increment must add at minimum:

- exact OpenAPI methods/routes/operationIds for all four operations;
- 401 without JWT;
- 403 without required permission;
- organization-scope isolation;
- exact DTO/request schema;
- server-owned ID/organization/active/version creation behavior;
- code normalization and uniqueness;
- deterministic list filters/order;
- create/update idempotent replay;
- same-key changed-payload conflict;
- stale-version conflict;
- missing/cross-organization/inactive location behavior;
- reassignment/reactivation rules;
- location deactivation dependency protection;
- no delete/cascade behavior;
- audit/outbox/idempotency atomicity;
- PostgreSQL and MySQL persistence behavior;
- regression coverage for company/locations, W1.4 users/roles, reference data, parties and existing sales/fiscal terminal references.

## Implementation gate

The field-level contract prerequisite is closed by explicit owner approval.

After this contract/readiness PR is merged and its exact-head/post-merge CI is green, a separate bounded implementation increment may begin for `API-ORG-007..010` plus the accepted `API-ORG-006` active-terminal dependency invariant.

Implementation counts remain unchanged until the matching public HTTP surfaces actually exist and pass the governed acceptance process.
