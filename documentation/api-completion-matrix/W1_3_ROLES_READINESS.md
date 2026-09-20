# W1.3 Roles read/write readiness

Status: `IMPLEMENTED / CI_GREEN / TEMP_NEON_SCHEMA_VALIDATED / PRODUCTION_SCHEMA_PENDING_APPROVAL`

Accepted backend base: `e669ab6cf12caa8d5c537489ebd35ff8bfaf0241` (`main` after W1.2 closure).

Live-main reconciliation note: `main` later advanced through WebApp-only documentation changes; W1.3 must be reconciled against the live `main` again before merge approval.

Scope: `API-IAM-006..009` only.

## Contract lock

| API ID | operationId | Method / path | Permission | Idempotency |
|---|---|---|---|---|
| `API-IAM-006` | `listRoles` | GET `/api/v1/roles` | `security.roles.read` | NO |
| `API-IAM-007` | `getRole` | GET `/api/v1/roles/{roleId}` | `security.roles.read` | NO |
| `API-IAM-008` | `createRole` | POST `/api/v1/roles` | `security.manage_roles` | REQUIRED |
| `API-IAM-009` | `updateRole` | PUT `/api/v1/roles/{roleId}` | `security.manage_roles` | REQUIRED |

No endpoint outside this set is introduced in W1.3.

## Readiness findings

1. There was no existing IAM role aggregate, repository, EF record, mapping or migration in the V1 persistence model.
2. Existing `PartyRole` / `v1_party_roles` belongs to the Parties bounded context (`CUSTOMER` / `SUPPLIER`) and is not reused for security authorization.
3. `Permissions.All` remains the canonical stable application permission catalog and single source of truth after W1.2.
4. The V1 persistence foundation provides reusable idempotency, durable audit, outbox, transactions and PostgreSQL/MySQL persistence-equivalence infrastructure.
5. The accepted architecture requires role/permission changes to be durable-audited and treats roles as editable permission compositions, not controller authorization shortcuts.
6. Role routes resolve organization through the existing `V1OrganizationContextResolver` / `X-Organization-Id` behavior and validate actor company scope.

## Bounded role model

W1.3 introduces a company-scoped security role definition with:

- opaque role ID;
- organization ID;
- name;
- optional description;
- active flag;
- concurrency `version`;
- deterministic set of canonical permission codes;
- created/updated UTC timestamps in persistence.

Role names are unique within one organization using normalized case-insensitive comparison at the application/persistence boundary. The same normalized name is valid in a different organization. No role delete endpoint is introduced. Deactivation is represented by the replacement semantics of `updateRole`.

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

Clean Architecture Guard run `35487901223` on W1.3 head `006e784c823147bc8f34b79dd88d25da90e4bd9f` passed:

- restore;
- NuGet vulnerability gate;
- Release build;
- ArchitectureTests;
- CrossCuttingTests;
- legacy UnitTest;
- complete provider-real PostgreSQL/MySQL persistence integration suite.

The persistence test verifies create/update atomicity, canonical permission composition, audit/outbox/idempotency evidence and provider equivalence. A later hardening commit extends this proof with create/update replay, company-scoped normalized-name uniqueness and failed-reservation rollback; the final exact-head guard must pass after that change.

## Neon schema gate evidence

Neon project: `efactura-demo` (`sparkling-night-24634185`).

Production/default branch: `production` (`br-restless-thunder-a55cu12n`). Database: `efactura_demo`.

Pre-gate production inspection found:

- no `v1_security_roles` table;
- no `v1_security_role_permissions` table;
- latest recorded EF migration `20260914123000_V1FiscalCfeDocumentResponseCertificateTrust` (`8.0.30`).

Migration `20260920040000_V1SecurityRoles` was then applied only to temporary Neon branch `br-green-term-a5qqyruf` using the PostgreSQL DDL equivalent of the committed EF migration. Validation confirmed:

- exact required columns and nullability;
- `PK_v1_security_roles`;
- composite `PK_v1_security_role_permissions`;
- unique `UX_v1_security_role_org_name` on `(OrganizationId, NormalizedName)`;
- `IX_v1_security_role_org_active`;
- `IX_v1_security_role_permission_code`;
- FK `FK_v1_security_role_permission_role` with `ON DELETE CASCADE`;
- same normalized role name accepted in different organizations;
- permission rows persist with the expected role association;
- `__EFMigrationsHistory` records `20260920040000_V1SecurityRoles / 8.0.30`.

The production branch remains unchanged. Promotion of the prepared migration requires explicit user approval through the Neon migration-completion gate.

## Runtime / QA acceptance

Minimum executable proof before formal W1.3 closure:

1. OpenAPI exposes exactly the four operation IDs with the accepted methods/routes.
2. 401/403 behavior for read and mutation endpoints.
3. company-scope isolation and multi-company organization-header behavior.
4. create role succeeds with canonical permission composition.
5. unknown permission is rejected.
6. duplicate permission input returns one deterministic code in the result.
7. duplicate normalized role name is rejected inside the same organization but allowed in another organization.
8. `listRoles` and `getRole` return deterministic permission ordering.
9. `updateRole` fully replaces mutable metadata/permission composition and increments version.
10. stale `expectedVersion` is rejected.
11. create/update idempotent replay is deterministic and payload mismatch conflicts.
12. durable audit/outbox evidence is written atomically with mutation.
13. PostgreSQL transaction/persistence integration passes.
14. MySQL transaction/persistence integration passes.
15. regression: W1.2 `/me` + `/permissions`, W1.1 reference-data and `/parties` remain green.

## Completion accounting

W1.3 implementation accounting in the governed branch is:

- Wave 1: `21 / 30` implemented (`70.00%`);
- global public v1: `55 / 194` implemented (`28.35%`);
- remaining `MISSING_HTTP`: `137`;
- remaining contract-collision IDs: `2`;
- remaining non-implemented IDs: `139`.

The next bounded Wave 1 increment is W1.4 Users + role assignment (`API-IAM-002..005`, `API-IAM-010`), but it MUST NOT start before formal W1.3 closure.
