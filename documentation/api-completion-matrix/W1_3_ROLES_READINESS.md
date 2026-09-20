# W1.3 Roles read/write readiness

Status: `IMPLEMENTED / NEON_PRODUCTION_SCHEMA_APPLIED / MERGE_GATE_PENDING`

Accepted backend base: `e669ab6cf12caa8d5c537489ebd35ff8bfaf0241` (`main` after W1.2 closure).

Live-main reconciliation: W1.3 is reconciled with `main@dcf54ef640bc206ee8c360610b8c4e93f25aab18`, which includes the approved WebApp `UI-CATALOG-001` implementation. The reconciliation preserves both lanes and leaves the W1.3 branch `0` commits behind that main checkpoint.

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

1. There was no prior IAM security-role aggregate, repository, EF record, mapping or migration in the V1 persistence model.
2. Existing `PartyRole` / `v1_party_roles` belongs to the Parties bounded context (`CUSTOMER` / `SUPPLIER`) and is not reused for security authorization.
3. `Permissions.All` remains the canonical stable application permission catalog and single source of truth after W1.2.
4. The V1 persistence foundation provides reusable idempotency, durable audit, outbox, transactions and PostgreSQL/MySQL persistence-equivalence infrastructure.
5. Role/permission changes are durable-audited and roles remain editable permission compositions, not controller authorization shortcuts.
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

Exact-head Clean Architecture Guard #614 (`35515593390`) passed on W1.3 head `b74d2c7edf9c9829a4e439f06b41ee861cfb432f` before the later WebApp-only `main` advancement:

- restore PASS;
- NuGet vulnerability gate PASS;
- Release build PASS;
- ArchitectureTests PASS (`249/249`);
- CrossCuttingTests PASS;
- legacy UnitTest PASS;
- complete provider-real PostgreSQL/MySQL transactional persistence integration PASS.

After `main` advanced through PR #178, W1.3 was reconciled with `main@dcf54ef640bc206ee8c360610b8c4e93f25aab18`. A new exact-head guard is required on the reconciled branch before merge approval.

## Neon production schema evidence

Neon project: `efactura-demo` (`sparkling-night-24634185`).

Production/default branch: `production` (`br-restless-thunder-a55cu12n`). Database: `efactura_demo`.

Pre-promotion production inspection confirmed:

- no `v1_security_roles` table;
- no `v1_security_role_permissions` table;
- latest recorded EF migration `20260914123000_V1FiscalCfeDocumentResponseCertificateTrust` (`8.0.30`).

Migration `20260920040000_V1SecurityRoles` was first validated on temporary Neon branches. The final governed migration package used migration ID `8ff99177-cc62-4862-9bb1-c33573262f5a` and temporary branch `br-plain-silence-a530cfw2`.

Following explicit user approval, that prepared migration was promoted to production. Neon reported successful application to parent branch `br-restless-thunder-a55cu12n` and deleted the temporary branch.

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

The schema promotion is expand-first and additive. No existing production table was altered by the W1.3 migration.

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
