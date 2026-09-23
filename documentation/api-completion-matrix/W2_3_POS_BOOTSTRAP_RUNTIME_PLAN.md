# W2.3 POS bootstrap runtime acceptance plan

Status: `RUNTIME_ACCEPTANCE_RETRY_PENDING_OWNER_MERGE`

API ID: `API-POS-001`

Accepted repaired API merge: `47f97a802f577c00cd83642769ac9b94bfad8d97`

Accepted Cloud Run revision: `efactura-api-d22-47f97a8-58-1`

Accepted deployment run: `35798226197` (`Deploy eFactura API Demo #58`)

Post-merge architecture guard: `35798226153` (`Clean Architecture Guard #728`, PASS)

## Purpose

Capture the bounded production runtime evidence required to close W2.3 after implementation merge, post-merge regression, runtime defect repair and deployment success.

## First runtime attempt and repair

The first one-shot runtime acceptance ran as GitHub Actions run `35795755059` against revision `efactura-api-d22-cbfd192-57-1`.

It passed the accepted API baseline check, exact Cloud Run revision check, OpenAPI GET-only surface, unauthenticated `401`, malformed-token `401`, and observed the expected HTTP `403` for an authenticated actor without `sales.read`. It then failed closed because the public Problem Details code was `forbidden` instead of the locked W2.3 contract value `permission_denied`.

The root cause was the HTTP authorization boundary: `[RequirePermission]` is enforced before the application use case, so `V1AuthorizationMiddlewareResultHandler` owned the public `403` code. PR #223 repaired that boundary so permission-policy failures emit `permission_denied` while generic non-permission authorization failures continue to emit `forbidden`.

PR #223 merged as `47f97a802f577c00cd83642769ac9b94bfad8d97`. Post-merge Clean Architecture Guard #728 passed, including build, architecture, cross-cutting, legacy unit and PostgreSQL/MySQL provider-real transaction tests.

Deploy eFactura API Demo #58 then promoted exact revision `efactura-api-d22-47f97a8-58-1` to 100% traffic after successful canary and public smoke tests. The immutable image digest is `sha256:85e2af30f4e11bc742ffb00ae4c9057aaa6210c33ad018288546863426dca6b7`.

## Safety boundary

The runtime harness is strictly read-only.

It MUST NOT create or mutate:

- fiscal locations;
- terminals;
- sales;
- parties;
- payment methods;
- catalog data;
- security users or roles;
- audit/outbox/idempotency data;
- schema or migrations.

No fixture is created solely to force a non-empty bootstrap response.

## Runtime evidence

The one-shot workflow verifies:

1. exact promoted Cloud Run revision remains at 100% traffic;
2. OpenAPI exposes only `GET /api/v1/pos/bootstrap` with operationId `getPosBootstrap`;
3. no JWT returns `401`;
4. malformed JWT returns `401`;
5. authenticated actor without `sales.read` returns `403 permission_denied`;
6. organization-scope escape returns `403 organization_scope_denied`;
7. an actor with `sales.read`, matching organization scope and no `organization.read` can call the endpoint;
8. empty location scope returns `200` with an empty `contexts` array and the exact top-level response fields;
9. `Cache-Control` contains `private` and `no-cache`;
10. unchanged authorized projection produces a stable ETag;
11. changing the actor location-scope projection changes the ETag even when no matching runtime location exists;
12. matching `If-None-Match` returns `304` with an empty body.

JWT signing material is retrieved through the existing WIF/GCP path, masked immediately, never printed and used only to mint short-lived smoke tokens.

## Governance

The retry workflow is pinned to repaired API merge SHA `47f97a802f577c00cd83642769ac9b94bfad8d97` and Cloud Run revision `efactura-api-d22-47f97a8-58-1`.

Backend-sensitive movement after the accepted repaired API SHA fails closed. The workflow runs only on `main` when its own one-shot workflow file changes.

Merge requires explicit owner approval on the final exact PR HEAD. After successful runtime evidence is captured, the workflow must be removed during the W2.3 closure increment.
