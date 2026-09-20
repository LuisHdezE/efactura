# W1.4 Users + role assignment readiness

Status: `READINESS_LOCKED / IMPLEMENTATION_NOT_STARTED`

Accepted base: `main@60ca8f6b9907c04c4456ed3b3576da6a0c5ea64f` after formal W1.3 runtime closure and the WebApp-only PR #181 reconciliation.

Scope: `API-IAM-002..005` and `API-IAM-010` only.

No endpoint outside this set is introduced in W1.4.

## Contract lock

| API ID | operationId | Method / path | Permission | Idempotency |
|---|---|---|---|---|
| `API-IAM-002` | `listUsers` | GET `/api/v1/users` | `security.users.read` | NO |
| `API-IAM-003` | `getUser` | GET `/api/v1/users/{userId}` | `security.users.read` | NO |
| `API-IAM-004` | `createUser` | POST `/api/v1/users` | `security.users.manage` | REQUIRED |
| `API-IAM-005` | `updateUser` | PATCH `/api/v1/users/{userId}` | `security.users.manage` | REQUIRED |
| `API-IAM-010` | `assignUserRoles` | PUT `/api/v1/users/{userId}/roles` | `security.manage_roles` | REQUIRED |

The accepted API inventory remains authoritative for method, route and primary permission. W1.4 does not add `/login`, `/refresh`, password-management or token-issuance endpoints.

## Readiness findings

1. There is no application user aggregate, user repository, V1 user record, user-role assignment record or user migration in the current backend.
2. Neon production contains `v1_security_roles` and `v1_security_role_permissions` from W1.3, but no public-schema table whose name contains `user` or application identity state.
3. `Permissions.All` already contains `security.users.read`, `security.users.manage`, `security.roles.read` and `security.manage_roles`; W1.4 must reuse those exact canonical codes.
4. W1.3 already provides the company-scoped `SecurityRole` aggregate and repository plus provider-real PostgreSQL/MySQL persistence, idempotency, audit, outbox, transactions and optimistic-concurrency patterns that W1.4 can extend without creating a parallel security stack.
5. Current runtime authorization is derived from the validated JWT actor context. W1.4 must not silently combine persisted role assignments with JWT claims inside request authorization. That would create an undocumented dual source of authority.
6. Authentication/session lifecycle remains an identity-provider/deployment concern. W1.4 stores an application access link to an external identity, never passwords, refresh tokens, provider secrets or signing material.
7. Users are managed in resolved organization context. Cross-company access through a known user ID is denied rather than trusting the identifier itself.

## Bounded user model

W1.4 introduces a company-scoped application access record `SecurityUser` with:

- opaque application user ID;
- organization ID resolved by the existing V1 organization-context boundary;
- identity-provider key/issuer reference;
- external subject identifier used to link the application record to the authenticated identity provider;
- display name;
- optional email used as administrative/display metadata only;
- active flag;
- concurrency `version`;
- deterministic location-scope assignments;
- deterministic terminal-scope assignments;
- deterministic security-role assignments;
- created/updated UTC timestamps in persistence.

The same external identity may have separate application access records in different organizations. Within one organization, `(identityProvider, externalSubject)` is unique. Email is not an authentication credential and is not used as the uniqueness or authorization primitive.

No password hash, bearer token, refresh token, provider client secret or signing secret is persisted in the W1.4 tables.

## Identity-provider boundary

- `createUser` links an application access record to an existing/external identity reference. It does not provision a provider password or mint a token.
- token acquisition, refresh and logout stay outside this bounded increment;
- the provider key and external subject are administrative identity-link metadata, not secrets;
- current endpoint authorization continues to consume the existing validated JWT actor context;
- persisted role assignments do not retroactively rewrite the permissions of the JWT currently executing a request;
- any future synchronization from persisted assignments into provider/session claims requires its own accepted integration decision and tests.

This preserves the accepted Auth contract instead of introducing an undocumented second token system.

## Scope model

The user record itself is company-scoped. The organization is resolved through the existing `V1OrganizationContextResolver` / `X-Organization-Id` behavior and is never accepted as a body override.

Location and terminal scope assignments are subordinate to that organization:

- duplicate IDs are de-duplicated;
- returned IDs use deterministic ordinal order;
- an assigned location/terminal must belong to the resolved organization;
- cross-company scope IDs fail closed;
- request bodies cannot grant a company outside the current organization context;
- scope mutations are security-sensitive and must be durably audited with before/after composition.

`security.users.manage` is the primary contract permission for create/update. If an update changes location/terminal scope composition, the application additionally requires `security.manage_roles` so an ordinary user-profile administrator cannot use `updateUser` as a privilege-escalation path. Metadata/status-only updates do not require this additional permission.

A caller may never expand its own scopes through W1.4. Self-targeted scope expansion is rejected even when the caller has the generic management permission.

## Role-assignment model

`assignUserRoles` is full replacement semantics for the target user's role IDs inside the resolved organization:

- requires `security.manage_roles`;
- every supplied role must exist in the same organization;
- inactive roles cannot be newly assigned;
- duplicate role IDs are de-duplicated;
- persisted/returned role IDs use deterministic ordinal order;
- assignment is transactional and increments the user's application-managed version;
- stale `expectedVersion` fails with the existing `409 concurrency_conflict / stale_version` shape;
- a caller cannot self-escalate by assigning roles to its own linked identity;
- known IDs never bypass organization-scope checks.

Role assignment stores role references only. Permission composition remains owned by W1.3 `SecurityRole` and `Permissions.All`; W1.4 does not copy permission strings into user-role rows.

## Public DTO lock

`UserDto`:

- `id`
- `identityProvider`
- `externalSubject`
- `displayName`
- `email`
- `active`
- `version`
- `locationScopes[]`
- `terminalScopes[]`
- `roleIds[]`

The user projection never returns provider secrets, JWTs, password material or raw unnecessary claims.

`CreateUserRequest`:

- `identityProvider`
- `externalSubject`
- `displayName`
- `email`
- `locationScopes[]`
- `terminalScopes[]`

New users start active at version `1`. Organization ID is implicit from resolved request context and is never accepted from the body.

`UpdateUserRequest` uses PATCH-style mutable fields and requires optimistic concurrency:

- optional `displayName`
- optional `email`
- optional `active`
- optional `locationScopes[]`
- optional `terminalScopes[]`
- required `expectedVersion`

Identity-provider key, external subject and organization are immutable through `updateUser`.

`AssignUserRolesRequest`:

- `roleIds[]`
- `expectedVersion`

`assignUserRoles` returns the canonical updated `UserDto` and increments version exactly once for a successful non-replay mutation.

## Authorization and organization isolation

- `listUsers` / `getUser`: `security.users.read` plus resolved organization scope;
- `createUser` / metadata-status `updateUser`: `security.users.manage` plus resolved organization scope;
- scope-composition changes: `security.users.manage` plus `security.manage_roles` plus allowed target scope;
- `assignUserRoles`: `security.manage_roles` plus resolved organization scope;
- missing/invalid JWT -> `401`;
- valid JWT missing required permission -> `403`;
- requested organization outside actor company scope -> `403 organization_scope_denied`;
- multi-company actor without `X-Organization-Id` -> existing `400 organization_context_required` behavior.

## Mutation safety

`createUser`, `updateUser` and `assignUserRoles`:

- require `Idempotency-Key` exactly as the accepted matrix specifies;
- use existing material request hashing / `IIdempotencyStore`;
- execute under the existing transaction manager and unit of work;
- persist business state + durable security audit + outbox + idempotency completion atomically;
- deterministic completed replay returns the canonical current resource;
- same key with changed material payload -> `409 idempotency_key_reused`, `conflictType=payload_mismatch`;
- stale expected version -> `409 concurrency_conflict`, `conflictType=stale_version`;
- failed validation/authorization cannot leave a completed success audit/outbox record or orphan idempotency reservation.

Proposed idempotency scopes:

- `identity.user.create:{organizationId}`;
- `identity.user.update:{organizationId}:{userId}`;
- `identity.user.roles:{organizationId}:{userId}`.

## Durable evidence

Accepted semantic audit mappings remain:

- `createUser` -> `security.user.created`;
- `updateUser` -> `security.user.updated`;
- `assignUserRoles` -> `security.user_roles.changed`.

Implementation may retain the existing V1 uppercase event-name convention, but the semantic mapping above must remain traceable and tests must verify target user, acting administrator, organization, version and before/after security composition without copying JWTs or secrets into metadata.

Outbox events should distinguish ordinary user mutation from role-assignment mutation so downstream identity/session integration can react without parsing audit metadata.

## Persistence slice

Additive provider-neutral V1 tables are expected:

- `v1_security_users`;
- `v1_security_user_location_scopes`;
- `v1_security_user_terminal_scopes`;
- `v1_security_user_roles`.

`v1_security_users` owns the user record and application-managed version. Scope/role tables use composite primary keys and foreign keys back to the user. `v1_security_user_roles.RoleId` references the existing `v1_security_roles.Id` table from W1.3.

Required persistence invariants:

- unique external identity link within one organization;
- lookup index by `(OrganizationId, Active)`;
- deterministic provider-neutral behavior in PostgreSQL and MySQL;
- role assignment cannot reference a role from another organization;
- no destructive modification of W1.3 role tables;
- no credential/secret columns.

The schema is expand-first and additive. Production migration remains a separately approved gate after implementation and provider-real validation.

## Error contract candidates

W1.4 should use stable RFC 9457 codes, including:

- `identity.user.not_found` -> 404;
- `identity.user.identity_duplicate` -> 409 with `conflictType=duplicate_identity_link`;
- `identity.user.role_invalid` -> 422 for missing/inactive/cross-organization assignment input that is safe to disclose;
- `identity.user.scope_invalid` -> 422 for invalid location/terminal scope composition;
- `identity.user.self_escalation_forbidden` -> 403;
- existing `idempotency_key_reused` and `concurrency_conflict` shapes for replay/concurrency conflicts.

No error response may disclose provider secrets, token contents or unnecessary cross-company existence information.

## QA / executable evidence required

Before W1.4 can close, executable evidence must prove at least:

1. OpenAPI exposes exactly the five accepted operation IDs, methods, paths and primary permissions.
2. 401/403 behavior for read, user-management and role-assignment operations.
3. organization isolation and multi-company header behavior.
4. create user persists a provider-neutral identity link without credential material.
5. duplicate identity link inside one organization is rejected; the same external identity in a different organization remains valid.
6. list/get projections are deterministic and do not expose secrets.
7. PATCH metadata/status update increments version and preserves immutable provider/subject/org identity.
8. location/terminal scope replacement validates organization ownership and blocks self-escalation.
9. role assignment rejects unknown, inactive and cross-company roles.
10. role replacement is deterministic and increments version.
11. stale expected versions are rejected for update and role assignment.
12. create/update/role-assignment idempotent replay is deterministic; payload mismatch conflicts.
13. durable audit/outbox/idempotency evidence is atomic with successful mutations and absent for rolled-back failures.
14. PostgreSQL provider-real persistence/transaction tests pass.
15. MySQL provider-real persistence/transaction tests pass.
16. regression: W1.3 roles, W1.2 `/me` + `/permissions`, W1.1 reference data and `/parties` remain green.
17. Clean Architecture Guard passes on the exact reconciled head before merge approval.

## Expected completion accounting

W1.4 implements five currently missing HTTP operations. When all five are executable and accepted, expected accounting becomes:

- Wave 1: `26 / 30` implemented (`86.67%`);
- global public v1: `60 / 194` implemented (`30.93%`);
- remaining `MISSING_HTTP`: `132`;
- remaining contract-collision IDs: `2`;
- remaining non-implemented IDs: `134`.

These numbers are projections only until the five endpoints exist and pass the governed merge/deployment/runtime gates.

## Implementation gate

Readiness is complete, but implementation has not started in this document increment.

Before implementation work begins from this branch lineage:

1. re-read live `main` because the WebApp lane is active in parallel;
2. reconcile any new main commits without overwriting WebApp changes;
3. implement the bounded Domain/Application/persistence/WebApi/test slice;
4. validate the additive migration on temporary/provider-real databases;
5. run exact-head CI;
6. obtain explicit merge approval;
7. separately obtain explicit approval before applying the W1.4 schema migration to Neon production;
8. deploy and complete runtime acceptance before declaring W1.4 closed.
