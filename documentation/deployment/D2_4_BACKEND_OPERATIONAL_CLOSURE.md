# D2.4 — Backend Operational Closure

Status: CLOSURE CANDIDATE / GOVERNED REVIEW PENDING

Branch baseline:

`main@9e1b9b6758215aa875e32d5792f85e6655dd7e07`

Purpose: close the backend/API/deployment lane of D2 with one reconciled operational record that is sufficient to operate, redeploy, verify and roll back the public demo API without relying on chat history.

This increment is documentation/governance only. It changes no WebApi, domain, persistence, Cloud Run workflow or WebApp code.

## 1. Accepted runtime baseline

Google Cloud project:

`efactura-demo-0916-9b93`

Project number:

`195831190862`

Region:

`us-east5`

Cloud Run service:

`efactura-api`

Stable public service URL:

`https://efactura-api-yblnutgx3q-ul.a.run.app`

Accepted deployed revision:

`efactura-api-d22-7eed21f-46-1`

Accepted deployment source commit:

`7eed21f969486bd689b5abf2e9c37ba2462b00bd`

Accepted Artifact Registry image tag:

`us-east5-docker.pkg.dev/efactura-demo-0916-9b93/efactura/webapi:sha-7eed21f969486bd689b5abf2e9c37ba2462b00bd`

Accepted immutable registry digest:

`sha256:0fec87aa1700f24c3f54682f2ab5940af0782fcdf1ebae9629ba05b8fb3c77ac`

Prior known-good rollback revision captured before promotion:

`efactura-api-d21-swagger-01`

The repository advanced after the deployment source commit only through documentation-only D2.2 closure PR #148. That merge did not match the API deployment workflow path filters and therefore did not create a replacement governed deployment. The accepted runtime lineage consequently remains the D2.2 revision above unless an out-of-band cloud mutation occurs, which is outside repository governance and must not be treated as accepted state without separate evidence.

## 2. Accepted automated deployment evidence

Workflow:

`.github/workflows/deploy-api-demo.yml`

Accepted successful run:

- workflow: `Deploy eFactura API Demo`;
- run number: `46`;
- run id: `35406809175`;
- event: push to accepted `main`;
- source SHA: `7eed21f969486bd689b5abf2e9c37ba2462b00bd`;
- conclusion: `SUCCESS`.

Accepted sequence:

`accepted main -> linux/amd64 image build -> short-lived OIDC/WIF authentication -> Artifact Registry push -> immutable digest resolution -> zero-traffic tagged Cloud Run canary -> canary smoke -> 100% promotion -> public smoke -> evidence capture`

The workflow deploys the Cloud Run revision from an immutable digest, not from a mutable tag.

## 3. Identity and trust boundary

Deployment service account:

`efactura-deploy@efactura-demo-0916-9b93.iam.gserviceaccount.com`

Runtime service account:

`efactura-run@efactura-demo-0916-9b93.iam.gserviceaccount.com`

Workload Identity Federation provider:

`projects/195831190862/locations/global/workloadIdentityPools/github-actions/providers/efactura-main`

Accepted trust restriction:

- GitHub owner id `88979457`;
- GitHub repository id `893259774`;
- ref `refs/heads/main`.

No long-lived Google service-account JSON key is part of the accepted deployment model.

The runtime identity and deployment identity remain intentionally separate.

## 4. Runtime configuration and secret names

Non-secret runtime configuration used by the accepted demo includes:

- `V1Persistence__Provider=PostgreSql`;
- `V1Persistence__ConnectionStringName=PostgresConnection`;
- `Swagger__Enabled=true` on the demo Cloud Run revision.

Secret-backed configuration names:

- `efactura-jwt-key`;
- `efactura-postgres-connection`.

The repository must not contain the corresponding secret values.

The deployment workflow reads only the JWT signing secret required to generate the authenticated deployment smoke token. It does not read the PostgreSQL connection string. The application receives the PostgreSQL connection through the Cloud Run runtime binding.

## 5. Accepted canary and public smoke evidence

Accepted zero-traffic canary:

`efactura-api-d22-7eed21f-46-1`

Canary tag:

`d22-46-1`

Canary URL used by the accepted run:

`https://d22-46-1---efactura-api-yblnutgx3q-ul.a.run.app`

Canary acceptance:

- `/swagger`: final HTTP `200` after canonical redirect handling;
- `/swagger/v1/swagger.json`: HTTP `200` and structurally valid OpenAPI document;
- unauthenticated `GET /api/v1/parties`: HTTP `401`;
- authenticated `GET /api/v1/parties`: HTTP `200` against the Neon-backed runtime;
- current JWT signing material absent from generated OpenAPI;
- result: `PASS`.

After promotion, the same acceptance set passed against the stable public service URL:

- Swagger: `200`;
- OpenAPI: `200`;
- parties without JWT: `401`;
- parties with valid JWT + Neon: `200`;
- result: `PASS`.

This is the accepted public smoke evidence for the deployed D2.2 revision. A future backend deployment must generate fresh smoke evidence through the same workflow before becoming accepted.

## 6. Repeatable deployment procedure

Normal governed deployment path:

1. implement a bounded backend/deployment increment on a branch;
2. open a PR against `main`;
3. obtain exact-head Clean Architecture Guard success;
4. obtain explicit human merge approval for that exact PR/head;
5. merge to `main`;
6. allow `Deploy eFactura API Demo` to run automatically when the changed paths match its API/deployment filters;
7. require WIF authentication, immutable digest resolution, zero-traffic canary and canary smoke to succeed;
8. promote only after canary acceptance;
9. require public post-promotion smoke to succeed;
10. record run, image, digest, revision and rollback evidence in repository documentation.

Manual `workflow_dispatch` is allowed only on `refs/heads/main`; it is not a substitute for governed acceptance of unmerged feature-branch code.

## 7. Rollback procedure

### Automatic rollback

Before deploying a candidate, the workflow captures the Cloud Run revision currently receiving 100% traffic.

If public post-promotion smoke fails, the workflow:

1. obtains fresh short-lived WIF credentials;
2. restores 100% traffic to the captured previous revision;
3. explicitly fails the deployment.

For accepted run #46 the captured rollback revision was:

`efactura-api-d21-swagger-01`

The rollback branch did not execute because the promoted revision passed public acceptance.

### Manual emergency rollback

If an accepted deployed revision later exhibits a demonstrated operational fault and the automatic deployment workflow is not the active execution context, the bounded traffic rollback command is:

```bash
gcloud run services update-traffic efactura-api \
  --project=efactura-demo-0916-9b93 \
  --region=us-east5 \
  --platform=managed \
  --to-revisions=efactura-api-d21-swagger-01=100
```

This command changes traffic only. It does not rebuild an image, recreate infrastructure, mutate Neon data or remove the faulty revision.

After any manual rollback, verification must repeat at minimum:

```bash
BASE_URL="https://efactura-api-yblnutgx3q-ul.a.run.app"

curl -sS -L -o /dev/null -w 'Swagger HTTP %{http_code}\n' "$BASE_URL/swagger"
curl -sS -o /dev/null -w 'OpenAPI HTTP %{http_code}\n' "$BASE_URL/swagger/v1/swagger.json"
curl -sS -o /dev/null -w 'Parties without JWT HTTP %{http_code}\n' "$BASE_URL/api/v1/parties"
```

Expected unauthenticated results are `200`, `200`, `401`. An authenticated JWT + Neon `200` check must also be executed through an approved secret-safe mechanism before the rollback state is considered fully verified.

A manual rollback is an operational incident and must be documented. It does not silently redefine the accepted repository baseline.

## 8. Persistence and data boundary

The public demo runtime uses Neon PostgreSQL.

Cloud Run -> Neon connectivity was proven by the accepted authenticated parties smoke returning HTTP `200`.

Repository CI additionally keeps provider-real PostgreSQL and MySQL transaction coverage. D2.4 changes no persistence schema or provider behavior.

Redis remains unnecessary for the currently accepted public runtime path unless a future concrete endpoint demonstrates that dependency.

## 9. WebApp integration boundary

D2.3 belongs to the parallel governed WebApp/UI lane.

Backend D2.4 does not:

- invent frontend screens;
- invent API endpoints;
- change mock/UI behavior;
- couple the WebApp deployment to Cloud Run;
- declare all API-completion waves finished.

The public Swagger/OpenAPI document and stable Cloud Run API URL are the handoff surface.

The WebApp may integrate approved slices progressively when their authoritative API operations exist. Backend readiness therefore does not require every later API-completion wave to be implemented first.

## 10. Known non-blocking operational debt

The following remain recorded debt but do not block D2 backend readiness because no accepted failing demo use case currently requires them:

- Serilog/Cloud Run stdout-stderr observability improvement;
- Application Insights;
- Cloud Run-specific cleanup of the internal `UseHttpsRedirection()` warning;
- Windows-only `System.Drawing` modernization where not exercised by the accepted demo path;
- Redis deployment;
- custom demo API domain;
- broader dependency/analyzer modernization that is unrelated to a demonstrated runtime failure.

These items must be handled as bounded future increments, not as speculative platform rewrites.

## 11. Product gaps that are not D2 operational blockers

D2 backend readiness is not equivalent to complete eFactura product capability, DGI Testing acceptance or Production enablement.

Known product/API gaps remain governed separately, including missing endpoint families and unresolved fiscal/business boundaries already recorded in `documentation/BLUEPRINT_CURRENT_STATE.md` and the API implementation records.

After D2 backend closure, API completion is planned through the previously agreed wave model rather than hidden inside deployment work.

## 12. Operational blocker assessment

At this checkpoint there is no repository-evidenced backend deployment blocker remaining for demo integration:

- repeatable API deployment exists;
- WIF/OIDC is proven;
- immutable image deployment is proven;
- canary deployment is proven;
- public Swagger/OpenAPI is proven;
- protected endpoint behavior is proven;
- authenticated Neon-backed behavior is proven;
- promotion is proven;
- deterministic rollback path is implemented and documented;
- secret boundaries are documented;
- WebApp ownership is separated.

The inability of a particular external inspection client to resolve the public `run.app` hostname is not accepted as application failure without corroborating Cloud Run/workflow/runtime evidence. Accepted runtime health remains governed by the deployment workflow evidence and future deployment-generated smokes.

## 13. D2.4 acceptance condition

D2.4 becomes closed only after:

1. this operational record and the D2 living records are reconciled;
2. `documentation/BLUEPRINT_CURRENT_STATE.md` is reconciled to the current governed repository/deployment checkpoint without changing the accepted functional semantics of PR #108;
3. exact-head Clean Architecture Guard succeeds for the D2.4 PR;
4. explicit human merge approval is given for the exact D2.4 PR/head;
5. the documentation is merged to `main`.

No deployment is required for D2.4 itself because this increment changes documentation only.

## 14. Intended closure state

After accepted merge of D2.4:

`D2 BACKEND LANE: READY_FOR_DEMO_INTEGRATION`

D2.1: `CLOSED / DEPLOYED / VERIFIED`

D2.2: `CLOSED / AUTOMATED / VERIFIED`

D2.3: owned by the parallel WebApp/UI lane

D2.4: `CLOSED / OPERATIONALLY RECONCILED`

Full cross-lane D2 closure remains dependent on the separately governed D2.3 integration work, but backend deployment work no longer blocks that integration.