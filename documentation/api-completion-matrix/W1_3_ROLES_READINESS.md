# W1.3 Roles read/write readiness

Status: `CLOSED / MERGED / NEON_PRODUCTION_SCHEMA_APPLIED / RUNTIME_ACCEPTED`

Accepted backend base: `e669ab6cf12caa8d5c537489ebd35ff8bfaf0241` (`main` after W1.2 closure).

Final W1.3 merge: PR #177, merge commit `29011a9a490ae836efd06f8b7a991a9a54cd64d4`, with accepted W1.3 head `47be8165841c58c3e600365d6225a35b0fef2f37` reconciled against the then-live WebApp-inclusive base `dcf54ef640bc206ee8c360610b8c4e93f25aab18`.

Scope: `API-IAM-006..009` only.

## Contract lock

| API ID | operationId | Method / path | Permission | Idempotency |
|---|---|---|---|---|
| `API-IAM-006` | `listRoles` | GET `/api/v1/roles` | `security.roles.read` | NO |
| `API-IAM-007` | `getRole` | GET `/api/v1/roles/{roleId}` | `security.roles.read` | NO |
| `API-IAM-008` | `createRole` | POST `/api/v1/roles` | `security.manage_roles` | REQUIRED |
| `API-IAM-009` | `updateRole` | PUT `/api/v1/roles/{roleId}` | `security.manage_roles` | REQUIRED |

No endpoint outside this set was introduced in W1.3.

## Readiness findings

1. There was no prior IAM security-role aggregate, repository, EF record, mapping or migration in the V1 persistence model.
2. Existing `PartyRole` / `v1_party_roles` belongs to the Parties bounded context (`CUSTOMER` / `SUPPLIER`) and was not reused for security authorization.
3. `Permissions.All` remains the canonical stable application permission catalog and single source of truth after W1.2.
4. The V1 persistence foundation supplied reusable idempotency, durable audit, outbox, transactions and PostgreSQL/MySQL persistence-equivalence infrastructure.
5. Role/permission changes are durable-audited and roles remain editable permission compositions, not controller authorization shortcuts.
6. Role routes resolve organization through the existing `V1OrganizationContextResolver` / `X-Organization-Id` behavior and validate actor company scope.

## Bounded role model

W1.3 introduced a company-scoped security role definition with:

- opaque role ID;
- organization ID;
- name;
- optional description;
- active flag;
- concurrency `version`;
- deterministic set of canonical permission codes;
- created/updated UTC timestamps in persistence.

Role names are unique within one organization using normalized case-insensitive comparison at the application/persistence boundary. The same normalized name is valid in a different organization. No role delete endpoint was introduced. Deactivation is represented by the replacement semantics of `updateRole`.

## Permission composition

- Incoming permission codes MUST exist in `Permissions.All`.
- Unknown permission codes fail closed with validation Problem Details.
- Duplicate codes are de-duplicated.
- Persisted and returned permission codes are deterministic ordinal order.
- W1.3 does NOT create an independently mutable `permissions` table or second catalog.
- The role-permission persistence record stores only validated canonical permission codes.

This preserves the W1.2 source-of-truth decision instead of creating drift between code and database catalogs.

## Public DTO lock

`RoleDto`:

- `id`
- `name`
- `description`
- `active`
- `version`
- `permissions[]`

`CreateRoleRequest`:

- `name`
- `description`
- `permissions[]`

New roles start active at version `1`.

`UpdateRoleRequest` uses full replacement semantics required by `PUT`:

- `name`
- `description`
- `active`
- `permissions[]`
- `expectedVersion`

Organization ID is never accepted from the request body.

## Authorization and scope

- read operations require `security.roles.read` plus resolved company scope;
- mutation operations require `security.manage_roles` plus resolved company scope;
- missing/invalid JWT -> `401`;
- valid JWT missing required permission -> `403`;
- requested organization outside actor scope -> `403`;
- multi-company actor without `X-Organization-Id` -> existing `400 organization_context_required` behavior.

Role names are never authorization primitives. Runtime endpoint policies continue to authorize against explicit permission codes only.

## Mutation safety

`createRole` and `updateRole`:

- require `Idempotency-Key`;
- use the existing request-hash/idempotency store;
- execute under the existing transaction manager/unit of work;
- append durable security audit evidence;
- emit a role-change integration/outbox event;
- return deterministic replay behavior;
- reject stale update versions with the existing RFC 9457 concurrency-conflict shape;
- reject duplicate normalized role names within the organization.

Audit metadata may contain role ID/name/version/active state and permission-code composition, but no JWT or secret material.

## Persistence slice

Additive V1 tables:

- `v1_security_roles`
- `v1_security_role_permissions`

`v1_security_roles` owns `(Id, OrganizationId, Name, NormalizedName, Description, Active, Version, CreatedAtUtc, UpdatedAtUtc)`.

Required indexes:

- unique `(OrganizationId, NormalizedName)`;
- lookup `(OrganizationId, Active)`.

`v1_security_role_permissions` owns `(RoleId, PermissionCode)` with composite primary key and cascade delete only from the role aggregate record. W1.3 itself exposes no delete operation.

The schema is additive and provider-neutral.

## Executable CI evidence

Pre-merge exact-head Clean Architecture Guard #618 passed on W1.3 head `47be8165841c58c3e600365d6225a35b0fef2f37`.

Post-merge Clean Architecture Guard #619 passed on merge commit `29011a9a490ae836efd06f8b7a991a9a54cd64d4`.

Both gates preserved:

- Release build PASS;
- architecture tests PASS;
- API cross-cutting tests PASS;
- legacy unit tests PASS;
- provider-real PostgreSQL transaction/persistence integration PASS;
- provider-real MySQL transaction/persistence integration PASS.

## Neon production schema evidence

Neon project: `efactura-demo` (`sparkling-night-24634185`).

Production/default branch: `production` (`br-restless-thunder-a55cu12n`). Database: `efactura_demo`.

Migration `20260920040000_V1SecurityRoles` was first validated on temporary Neon branches. The final governed migration package used migration ID `8ff99177-cc62-4862-9bb1-c33573262f5a`.

Following explicit user approval, that prepared migration was promoted to production.

Post-promotion production verification confirmed:

- `v1_security_roles` exists with 9 expected columns;
- `v1_security_role_permissions` exists with 2 expected columns;
- `PK_v1_security_roles`;
- composite `PK_v1_security_role_permissions`;
- unique `UX_v1_security_role_org_name` on `(OrganizationId, NormalizedName)`;
- `IX_v1_security_role_org_active`;
- `IX_v1_security_role_permission_code`;
- FK `FK_v1_security_role_permission_role` from `RoleId` to `v1_security_roles.Id` with `ON DELETE CASCADE`;
- runtime role `efactura_app` has `SELECT/INSERT/UPDATE/DELETE` on both new tables;
- `__EFMigrationsHistory` records `20260920040000_V1SecurityRoles / 8.0.30`.

The schema promotion was expand-first and additive. No existing production table was altered by the W1.3 migration.

## Runtime / QA closure evidence

Deploy API Demo #52 completed successfully and promoted Cloud Run revision `efactura-api-d22-29011a9-52-1` to the public service.

Runtime acceptance on that exact revision proved:

1. Swagger final HTTP 200 and OpenAPI HTTP 200 after the expected `/swagger` redirect.
2. OpenAPI exposes exactly `listRoles`, `getRole`, `createRole`, `updateRole` on the accepted methods/routes.
3. missing JWT -> 401 and missing role-read permission -> 403.
4. multi-company organization-context behavior is enforced.
5. create role succeeds with canonical de-duplicated permission composition.
6. create replay returns deterministic prior result and replay header.
7. same idempotency key + changed payload -> 409 payload mismatch.
8. unknown permission -> 422.
9. duplicate normalized name -> 409.
10. `getRole` returns the created role.
11. `updateRole` replaces mutable state and increments version 1 -> 2.
12. update replay is deterministic.
13. stale expected version -> 409 concurrency conflict with current version.
14. final deactivation increments version 2 -> 3 and leaves the QA role inactive.
15. `/me`, `/permissions` (68/68), eight W1.1 reference-data endpoints and `/parties` regressions remain green.

Runtime QA role:

- ID `8fdc4eff1b1349829146b0601956414d`;
- final name `QA-W13-20260920160620-1577-UPDATED`;
- final state inactive;
- final version `3`;
- final permissions `audit.read`, `catalog.read`.

Independent Neon post-runtime verification confirmed exactly:

- 1 security role;
- 2 final role-permission rows;
- 3 role audit events (`CREATED`, `UPDATED`, `UPDATED`);
- 3 `RoleChangedIntegrationEvent` outbox rows;
- 3 completed role idempotency rows;
- 0 non-completed role idempotency rows;
- 0 invalid-role rows;
- 0 duplicate-role rows.

This proves successful state/evidence atomicity and rollback of the negative test paths.

## Completion accounting

W1.3 closed accounting is:

- Wave 1: `21 / 30` implemented (`70.00%`);
- global public v1: `55 / 194` implemented (`28.35%`);
- remaining `MISSING_HTTP`: `137`;
- remaining contract-collision IDs: `2`;
- remaining non-implemented IDs: `139`.

W1.3 is formally closed. The next bounded Wave 1 increment is W1.4 Users + role assignment (`API-IAM-002..005`, `API-IAM-010`).
