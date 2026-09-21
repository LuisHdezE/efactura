# W1.5 Terminals readiness and implementation evidence

Status: `IMPLEMENTED / MERGED / CI_ACCEPTED / DEPLOYED / PRODUCTION_MIGRATED / RUNTIME_ACCEPTED / CLOSED`

Accepted field-level authority: `W1_5_TERMINAL_CONTRACT_PROPOSAL.md`, explicitly approved by Luis on 2026-09-20.

Operational closure evidence: `W1_5_TERMINALS_RUNTIME_CLOSURE.md`.

Scope: `API-ORG-007..010` only, plus the accepted active-terminal dependency invariant for existing `API-ORG-006 updateLocation`.

No endpoint outside this set is introduced by W1.5.

## Contract and implementation inventory

| API ID | operationId | Method / path | Permission | Idempotency | Final state |
|---|---|---|---|---|---|
| `API-ORG-007` | `listTerminals` | GET `/api/v1/terminals` | `organization.read` | NO | `CLOSED` |
| `API-ORG-008` | `registerTerminal` | POST `/api/v1/terminals` | `organization.manage` | REQUIRED | `CLOSED` |
| `API-ORG-009` | `getTerminal` | GET `/api/v1/terminals/{terminalId}` | `organization.read` | NO | `CLOSED` |
| `API-ORG-010` | `updateTerminal` | PATCH `/api/v1/terminals/{terminalId}` | `organization.manage` | REQUIRED | `CLOSED` |

The four governed public v1 HTTP surfaces are implemented and production-accepted. Wave 1 therefore remains `30 / 30` implemented with no non-implemented operation in this wave.

## Governed implementation evidence

Implementation PR #195 delivered the bounded W1.5 slice:

- `Domain.Organizations.Terminal` aggregate with immutable server-owned identity, immutable normalized business code, active/inactive lifecycle and application-managed versioning;
- Application list/get/register/update use cases with `OrganizationAuthorization`, location validation, idempotency, optimistic concurrency, audit and transactional outbox evidence;
- `API-ORG-006` location-deactivation protection when active terminals remain assigned;
- provider-neutral EF persistence, normalized-code uniqueness and additive migration `20260921024500_V1Terminals`;
- `TerminalsController` exposing the exact four governed methods/routes/operation names;
- dedicated architecture tests;
- dedicated provider-real PostgreSQL and MySQL persistence/use-case integration tests.

Approved implementation HEAD: `f954c7e21268bf9fc1a6e3f3fbcdb0cd87850c71`.

Implementation merge commit: `11139c7c4adf4037c2c2c279dedd302a6b4c7958`.

Post-merge Clean Architecture Guard #675, run `35560423073`, passed on `bc138d6076a6d9f2f8f948171082511e9385b39f`.

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

Accepted rules:

- server-generated opaque immutable `id`;
- server-resolved immutable `organizationId`;
- immutable normalized uppercase business `code`, maximum 64 characters;
- organization-level uniqueness on normalized code;
- mutable `name`, `locationId` and `active` only;
- active/inactive lifecycle;
- registration starts active at version 1;
- PATCH requires `expectedVersion` and increments application-managed version;
- no public DELETE;
- Terminal and Device remain separate concepts.

## Read behavior

`listTerminals` supports:

- `active`, nullable and defaulting to active-only;
- `locationId`, optional.

Ordering is deterministic by normalized code then terminal ID.

`getTerminal` returns the canonical projection inside the resolved organization or the governed not-found behavior.

## Location invariants

- registration requires an existing active fiscal location in the same organization;
- reassignment requires an existing active destination location and current `expectedVersion`;
- reactivation requires an active requested location;
- inactive terminals may remain readable on a later-inactive location;
- an active terminal cannot be created, assigned or reactivated onto an inactive location;
- a fiscal location cannot be deactivated while active terminals remain assigned;
- administrators must first deactivate or reassign those terminals;
- no cascade terminal deactivation or hard delete occurs.

The last rule extends existing `API-ORG-006 updateLocation` behavior but introduces no new endpoint or request schema.

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

## Persistence and mutation evidence

Accepted idempotency scopes:

```text
organization.terminal.register:{organizationId}
organization.terminal.update:{organizationId}:{terminalId}
```

A successful register/update mutation atomically persists:

- the business mutation;
- completed idempotency evidence;
- durable audit evidence;
- transactional outbox evidence.

Production migration `20260921024500_V1Terminals` was explicitly approved and promoted to Neon production. Independent verification confirmed the migration row, exact governed schema shape, expected FK/index set, role permissions and ProductVersion `8.0.30`.

## Accepted deployment and runtime

Accepted Cloud Run revision: `efactura-api-d22-11139c7-54-1`.

Deployment workflow run: `35560340904`.

Final production runtime acceptance run: `35612527016`, SUCCESS.

The runtime acceptance proved:

- Swagger/OpenAPI availability;
- exact 4/4 Terminal operationIds and no DELETE;
- 401 and 403 boundaries;
- organization-scope isolation;
- Company Fiscal Profile and Location prerequisites through public HTTP;
- registration, code normalization and deterministic ordering;
- deterministic idempotent replay;
- payload-mismatch conflicts;
- duplicate code conflicts;
- missing/cross-organization Location masking;
- cross-organization Terminal masking;
- inactive-location restrictions;
- reassignment and reactivation rules;
- stale-version concurrency;
- inactive Terminal rename while its Location is inactive;
- location-deactivation dependency protection;
- final active/inactive list behavior;
- Wave 1 regression reads.

Final accepted QA Terminal state:

- T1 `6ea871509b4f4fc68d22eb6544ad44a7`, code `W15.35612527016.B`, inactive, Location A2, version 4;
- T2 `f60bb7666b9342abbd022f4d0072d2a5`, code `W15.35612527016.A`, inactive, Location A1, version 2.

Independent Neon verification confirmed exactly 6 successful Terminal mutations, 6 Terminal audit events, 6 Terminal outbox messages, 6 completed Terminal idempotency records, zero non-completed Terminal idempotency residue, and zero Terminal evidence in the isolated organization B.

## Architecture preserved

W1.5 preserves:

- `OrganizationAuthorization.EnsureRead/EnsureManage`;
- `V1OrganizationContextResolver`;
- provider-neutral repository interfaces;
- application-managed optimistic concurrency;
- transaction manager and unit of work;
- request-hash idempotency store;
- durable audit writer;
- transactional outbox;
- PostgreSQL/MySQL neutrality;
- Clean Architecture dependency direction.

Terminal remains organization/POS operational master data under `organization.read/manage`. Device remains offline/sync registration under its separate sync permissions and identity model.

## Final closure

W1.5 is formally closed as:

`IMPLEMENTED / MERGED / CI_ACCEPTED / DEPLOYED / PRODUCTION_MIGRATED / RUNTIME_ACCEPTED / CLOSED`

Wave 1 is formally closed at `30 / 30` implemented operations.

The next API-completion work begins from Wave 2. The global public v1 accounting remains `64 / 194` implemented (`32.99%`), `128` missing HTTP, `2` contract collisions and `130` non-implemented operations.
