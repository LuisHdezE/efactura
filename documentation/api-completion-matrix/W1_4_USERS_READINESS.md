# W1.4 Users + role assignment readiness

Status: `IMPLEMENTED / MERGED / CI_ACCEPTED / DEPLOYED / PRODUCTION_MIGRATED / RUNTIME_ACCEPTED / CLOSED`

Initial readiness base: `main@60ca8f6b9907c04c4456ed3b3576da6a0c5ea64f` after formal W1.3 runtime closure and WebApp-only PR #181 reconciliation.

Pre-merge reconciliation baseline: `main@74eaf99c417904191c59d8a7e454bed557f35158` after WebApp PR #183. The W1.4 readiness branch was merged forward onto that live base without overwriting WebApp changes.

Implementation merge: PR #185 -> `5a939ba251a898ea1dd6a181050fffa20e91324d`.

Closure reconciliation baseline: `main@eac26a36d53fb54a71a0fabb2dfcbbab9db69b89` after WebApp-only documentation PR #186. PR #186 changes only `documentation/ui/inventory/**`; it does not change the W1.4 API, Application, Domain or Infrastructure implementation.

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

The readiness audit established the implementation constraints that remain authoritative after closure:

1. The application user model must remain provider-neutral and company-scoped.
2. W1.4 reuses the existing W1.3 security-role model and canonical `Permissions.All` codes rather than creating a parallel security stack.
3. Runtime authorization remains derived from the validated JWT actor context. Persisted role assignments are application state and do not silently rewrite the permissions of the JWT executing a request.
4. Authentication/session lifecycle remains an identity-provider/deployment concern. W1.4 stores an external identity link, never passwords, refresh tokens, provider secrets or signing material.
5. Users are managed in resolved organization context. Cross-company access through a known user ID is denied rather than trusting the identifier itself.

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
- scope mutations are security-sensitive and are durably audited with before/after composition.

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

Accepted idempotency scopes:

- `identity.user.create:{organizationId}`;
- `identity.user.update:{organizationId}:{userId}`;
- `identity.user.roles:{organizationId}:{userId}`.

## Durable evidence

Accepted semantic audit mappings remain:

- `createUser` -> `security.user.created`;
- `updateUser` -> `security.user.updated`;
- `assignUserRoles` -> `security.user_roles.changed`.

The implementation retains the existing V1 uppercase event-name convention:

- `SECURITY_USER_CREATED`;
- `SECURITY_USER_UPDATED`;
- `SECURITY_USER_ROLES_CHANGED`.

Outbox events distinguish ordinary user mutation from role-assignment mutation through `SecurityUserChangedIntegrationEvent` and `SecurityUserRolesChangedIntegrationEvent`.

## Persistence slice

The additive provider-neutral V1 tables are:

- `v1_security_users`;
- `v1_security_user_location_scopes`;
- `v1_security_user_terminal_scopes`;
- `v1_security_user_roles`.

`v1_security_users` owns the user record and application-managed version. Scope/role tables use composite primary keys and foreign keys back to the user. `v1_security_user_roles.RoleId` references the existing `v1_security_roles.Id` table from W1.3.

Persistence invariants:

- unique external identity link within one organization;
- lookup index by `(OrganizationId, Active)`;
- deterministic provider-neutral behavior in PostgreSQL and MySQL;
- role assignment cannot reference a role from another organization;
- no destructive modification of W1.3 role tables;
- no credential/secret columns.

The schema is expand-first and additive.

## Stable error contract

W1.4 uses stable RFC 9457 codes including:

- `identity.user.not_found` -> 404;
- `identity.user.identity_duplicate` -> 409 with `conflictType=duplicate_identity_link`;
- `identity.user.role_invalid` -> 422 for missing/inactive/cross-organization assignment input that is safe to disclose;
- `identity.user.scope_invalid` -> 422 for invalid location/terminal scope composition;
- `identity.user.self_escalation_forbidden` -> 403;
- existing `idempotency_key_reused` and `concurrency_conflict` shapes for replay/concurrency conflicts.

No error response may disclose provider secrets, token contents or unnecessary cross-company existence information.

## Implementation and CI evidence

W1.4 implementation landed through PR #185 and merge commit `5a939ba251a898ea1dd6a181050fffa20e91324d`.

The exact merged API head passed the governed Clean Architecture Guard before/around merge, and Deploy API Demo run `35534908414` completed successfully from that same source commit.

Cloud Run deployment evidence:

- accepted revision: `efactura-api-d22-5a939ba-53-1`;
- immutable registry digest: `sha256:81591ce316fcd2628473d76414ebff506e6b7512169917af7f7913fb8f5f0059`;
- public service promotion: 100% traffic to the accepted revision;
- canary smoke: PASS;
- public post-promotion smoke: PASS.

## Neon production migration evidence

Production database: `efactura_demo` on branch `br-restless-thunder-a55cu12n`.

Migration `20260920183000_V1SecurityUsers` was applied only after explicit approval. Post-migration verification proved:

- `__EFMigrationsHistory` contains `20260920183000_V1SecurityUsers` with `ProductVersion=8.0.30`;
- all four W1.4 tables exist;
- all five expected indexes exist;
- user -> location scopes uses `ON DELETE CASCADE`;
- user -> terminal scopes uses `ON DELETE CASCADE`;
- user -> role assignments uses `ON DELETE CASCADE`;
- role -> user-role assignments uses `ON DELETE RESTRICT`;
- `efactura_app` has `SELECT`, `INSERT`, `UPDATE`, `DELETE` on all four W1.4 tables;
- production schema compared equal to the previously validated temporary migration schema.

No destructive modification of W1.3 role tables occurred.

## Runtime / QA closure evidence

Runtime acceptance stamp: `20260920220501`.

The public Cloud Run runtime passed all W1.4 acceptance checks:

1. Swagger HTTP `200` and OpenAPI HTTP `200`.
2. OpenAPI exposed exactly `listUsers`, `getUser`, `createUser`, `updateUser` and `assignUserRoles` with `updateUser` as `PATCH`, never `PUT`.
3. `GET /api/v1/users` returned `401` without JWT, `403` without `security.users.read`, and `200` with the accepted permission and organization scope.
4. `createUser` returned `201`, persisted a provider-neutral external identity link and started at version `1`.
5. deterministic create replay returned `201`, `Idempotent-Replayed=true` and did not increment version.
6. same idempotency key with changed payload returned `409 idempotency_key_reused / payload_mismatch`.
7. duplicate external identity returned `409 identity.user.identity_duplicate / duplicate_identity_link`.
8. get/list projections returned `200` and exposed the created user deterministically.
9. PATCH metadata/status update returned `200`, incrementing version `1 -> 2`; replay did not increment again.
10. stale update returned `409 concurrency_conflict / stale_version`.
11. invalid scope composition returned `422 identity.user.scope_invalid`.
12. inactive role assignment returned `422 identity.user.role_invalid`.
13. valid role full replacement returned `200`, incrementing version `2 -> 3`; replay did not increment again.
14. changed role-assignment payload under the same key returned `409 idempotency_key_reused`.
15. self scope escalation returned `403 identity.user.self_escalation_forbidden`.
16. organization escape attempt returned `403 organization_scope_denied`.
17. role full replacement to empty returned `200`, incrementing version `3 -> 4`.
18. W1.3 roles, W1.2 `/me` + `/permissions`, W1.1 reference-data endpoints and `/parties` all remained HTTP `200` under the accepted runtime actor.

QA runtime resource evidence:

- user ID `ae87c4338e604e7cb8febf0b79bf513b` finished active at version `4`;
- final user has zero location scopes, zero terminal scopes and zero role assignments;
- QA role ID `77fc171cfe2d43109a0aa4c6aa0e9ee9` finished inactive at version `2`;
- the pre-existing W1.3 inactive role `8fdc4eff1b1349829146b0601956414d` remained inactive at version `3`.

## Independent Neon post-runtime verification

Read-only production queries after HTTP acceptance independently confirmed atomic durable evidence.

For the four successful W1.4 user mutations there are exactly four matching triplets keyed by `CorrelationId`:

- completed idempotency record;
- successful security audit event;
- durable outbox message.

The four accepted W1.4 triplets correspond to:

- create user, version `1`;
- update user, version `2`;
- assign role set, version `3`;
- replace role set with empty, version `4`.

Aggregate verification for the runtime window returned:

- QA external identity rows: `1`;
- forbidden self-target user rows: `0`;
- W1.4 user idempotency records: `4`;
- W1.4 user audit events: `4`;
- W1.4 user outbox messages: `4`;
- final user-role rows: `0`;
- final location-scope rows: `0`;
- final terminal-scope rows: `0`.

Rejected duplicate, stale-version, invalid-scope, inactive-role, idempotency-mismatch, self-escalation and organization-isolation paths left no successful audit/outbox/idempotency residue. The W1.3 QA role create/deactivate mutations formed their own matching durable triplets and are not counted as W1.4 user mutations.

## Completion accounting

W1.4 implements five previously missing HTTP operations. The accepted accounting is now:

- Wave 1: `26 / 30` implemented (`86.67%`);
- global public v1: `60 / 194` implemented (`30.93%`);
- remaining `MISSING_HTTP`: `132`;
- remaining contract-collision IDs: `2`;
- remaining non-implemented IDs: `134`.

These are current counts after W1.4 closure, not projections.

## Closure and next gate

All governed W1.4 gates are satisfied: implementation, exact contract, merge, CI, deployment, explicitly approved production schema promotion, production schema audit, HTTP runtime acceptance, durable Neon verification and documentation reconciliation.

W1.4 is formally closed by the merge of this reconciliation increment.

The next bounded Wave 1 increment is `W1.5` Terminals:

- `API-ORG-007 listTerminals`;
- `API-ORG-008 registerTerminal`;
- `API-ORG-009 getTerminal`;
- `API-ORG-010 updateTerminal`.

W1.5 must begin with a fresh readiness audit against live `main`; it must not assume that existing organization/location persistence automatically defines terminal lifecycle, registration, status or concurrency semantics.
