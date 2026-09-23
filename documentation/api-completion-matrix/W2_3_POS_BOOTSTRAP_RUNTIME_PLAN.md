# W2.3 POS bootstrap runtime closure

Status: `CLOSED`

API ID: `API-POS-001`

Accepted repaired API merge: `47f97a802f577c00cd83642769ac9b94bfad8d97`

Accepted Cloud Run revision: `efactura-api-d22-47f97a8-58-1`

Accepted deployment run: `35798226197` (`Deploy eFactura API Demo #58`)

Accepted runtime-harness merge: `b79afd1d3504b5cb5f98bb326239f23ec5ebe92b` (PR #224)

Successful production runtime acceptance: run `35802751788`, job `106996575773`

Post-runtime merge regression: `35802751792` (`Clean Architecture Guard #730`, PASS)

Guard #730 jobs:

- Build and architecture tests: `106996576169`, PASS;
- PostgreSQL and MySQL transaction tests: `106997360926`, PASS.

## Purpose

Record the bounded production runtime evidence that closes W2.3 after implementation merge, runtime defect discovery and repair, deployment, successful read-only acceptance and post-merge regression.

## First runtime attempt and repair

The first one-shot runtime acceptance ran as GitHub Actions run `35795755059` against revision `efactura-api-d22-cbfd192-57-1`.

It passed the accepted API baseline check, exact Cloud Run revision check, OpenAPI GET-only surface, unauthenticated `401`, malformed-token `401`, and observed the expected HTTP `403` for an authenticated actor without `sales.read`. It then failed closed because the public Problem Details code was `forbidden` instead of the locked W2.3 contract value `permission_denied`.

The root cause was the HTTP authorization boundary: `[RequirePermission]` is enforced before the application use case, so `V1AuthorizationMiddlewareResultHandler` owned the public `403` code. PR #223 repaired that boundary so permission-policy failures emit `permission_denied` while generic non-permission authorization failures continue to emit `forbidden`.

PR #223 merged as `47f97a802f577c00cd83642769ac9b94bfad8d97`. Post-merge Clean Architecture Guard #728 passed, including build, architecture, cross-cutting, legacy unit and PostgreSQL/MySQL provider-real transaction tests.

Deploy eFactura API Demo #58 then promoted exact revision `efactura-api-d22-47f97a8-58-1` to 100% traffic after successful canary and public smoke tests. The immutable image digest is `sha256:85e2af30f4e11bc742ffb00ae4c9057aaa6210c33ad018288546863426dca6b7`.

## Successful runtime acceptance

PR #224 repinned the one-shot harness to the repaired API SHA and exact promoted revision. It merged as `b79afd1d3504b5cb5f98bb326239f23ec5ebe92b` and triggered the governed runtime run `35802751788`.

Run `35802751788`, job `106996575773`, completed successfully against `efactura-api-d22-47f97a8-58-1` and verified:

1. accepted repaired API baseline unchanged;
2. exact promoted Cloud Run revision at 100% traffic;
3. OpenAPI HTTP `200`;
4. exact GET-only `/api/v1/pos/bootstrap` surface with operationId `getPosBootstrap`;
5. no JWT -> `401`;
6. malformed JWT -> `401`;
7. authenticated actor without `sales.read` -> `403 permission_denied`;
8. organization-scope escape -> `403 organization_scope_denied`;
9. `sales.read` with matching organization scope and without `organization.read` -> `200`;
10. empty location scope -> `200` with empty `contexts` and exact top-level response fields;
11. `Cache-Control` contains `private` and `no-cache`;
12. unchanged authorized projection produces a stable ETag;
13. changing the actor location-scope projection changes the ETag even when no matching runtime location exists;
14. matching `If-None-Match` -> `304` with empty body and unchanged ETag.

Production writes: **NONE**.

Fixture creation: **NONE**.

JWT signing material was retrieved through the existing WIF/GCP path, masked immediately, never printed and used only to mint short-lived smoke tokens.

## Post-merge regression

The PR #224 merge also triggered Clean Architecture Guard #730 (`35802751792`) on exact merge commit `b79afd1d3504b5cb5f98bb326239f23ec5ebe92b`.

The Guard completed `SUCCESS`:

- solution restore/build and NuGet gates: PASS;
- Clean Architecture tests: PASS;
- API v1 CrossCutting tests: PASS;
- legacy unit tests: PASS;
- PostgreSQL/MySQL provider-real transaction tests: PASS.

## Safety boundary

The runtime harness was strictly read-only and created or mutated none of the following:

- fiscal locations;
- terminals;
- sales;
- parties;
- payment methods;
- catalog data;
- security users or roles;
- audit/outbox/idempotency data;
- schema or migrations.

No fixture was created solely to force a non-empty bootstrap response.

## Closure

This closure increment removes the temporary `.github/workflows/w23-readonly-runtime-acceptance.yml` one-shot, reconciles the Wave 2 matrix/ledger and advances the accepted completion baseline to `67 / 194` globally and `25 / 26` for Wave 2.

After this closure increment merges, W2.3 is operationally closed. The only remaining Wave 2 public-v1 HTTP gap is W2.4 `API-PTY-008 getPartyAccountSummary`, which remains prerequisite-gated on an authoritative party-scoped AR/AP balance-aging read model and field contract.
