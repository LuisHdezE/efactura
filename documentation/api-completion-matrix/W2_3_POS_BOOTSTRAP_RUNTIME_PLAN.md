# W2.3 POS bootstrap runtime acceptance plan

Status: `RUNTIME_ACCEPTANCE_CANDIDATE_PENDING_OWNER_MERGE`

API ID: `API-POS-001`

Accepted implementation merge: `cbfd1926b609b083eed3faa9d79abf3e46cca788`

Accepted Cloud Run revision: `efactura-api-d22-cbfd192-57-1`

## Purpose

Capture the bounded production runtime evidence required to close W2.3 after implementation merge, post-merge regression and deployment success.

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

The workflow is pinned to API merge SHA `cbfd1926b609b083eed3faa9d79abf3e46cca788` and Cloud Run revision `efactura-api-d22-cbfd192-57-1`.

Backend-sensitive movement after the accepted API SHA fails closed. The workflow runs only on `main` when its own one-shot workflow file changes.

Merge requires explicit owner approval on the final exact PR HEAD. After successful runtime evidence is captured, the workflow must be removed during the W2.3 closure increment.
