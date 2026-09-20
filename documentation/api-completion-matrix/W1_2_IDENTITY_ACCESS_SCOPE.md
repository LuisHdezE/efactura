# W1.2 — Current Actor + Permission Catalog

Status: `IMPLEMENTED / CI_PENDING`

Baseline branch: `feat/w1-2-current-actor-permissions`

## Scope

This bounded Wave 1 increment implements exactly:

- `API-IAM-001 getCurrentActor` — `GET /api/v1/me` — `AUTHENTICATED`.
- `API-IAM-011 listPermissions` — `GET /api/v1/permissions` — `security.roles.read`.

No other IAM operation is implemented by this increment.

## Contract decisions

### Current actor

`getCurrentActor` is a projection of the existing `IActorContextAccessor.Current` application context. It does not create a second identity model and does not call an external identity provider.

The response contains only:

- actor identifier;
- display name;
- effective permission codes;
- allowed company scopes;
- allowed location scopes;
- allowed terminal scopes.

All set-like values are sorted with ordinal semantics for deterministic output.

`DeviceId` is intentionally not returned because the accepted contract requires identity/display metadata, effective permissions and allowed scopes, and the API data-minimization rule does not justify exposing additional session/device metadata.

### Permission catalog

`listPermissions` projects the canonical `Permissions.All` set from Application. It does not duplicate the permission inventory in WebApi, Infrastructure, configuration, or persistence.

The endpoint returns only canonical permission codes. Friendly labels, categories and descriptions are not invented because no accepted authoritative metadata source exists for them.

The list is sorted ordinally for deterministic output.

## Authorization

- both endpoints require an authenticated bearer token through `[Authorize]`;
- `getCurrentActor` requires no permission beyond authentication;
- `listPermissions` requires exact permission `security.roles.read` through `RequirePermission(Permissions.SecurityRolesRead)`;
- Application use cases retain defense-in-depth authentication/permission checks.

## Architecture boundary

W1.2 is application + WebApi only:

- no database table;
- no migration;
- no repository;
- no provider-specific dependency;
- no legacy `ApplicationCore` dependency;
- no WebApp runtime change.

## Expected matrix reconciliation

After W1.2:

- Wave 1: `17 / 30` implemented;
- global public v1: `51 / 194` implemented;
- remaining `MISSING_HTTP`: `141`;
- remaining contract-collision IDs: `2`;
- remaining non-implemented IDs: `143`.

The next bounded Wave 1 increment is W1.3: `API-IAM-006..009` roles read/write.
