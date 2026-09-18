# D2 — Repeatable Deployment & Demo Integration

Status: ACTIVE

Opened from accepted baseline: `main@1bf26f57bc94fc57b5ef3f34815cd4781e832b87`

Branch: `deployment/d2-repeatable-demo-integration`

## D1.2 closure checkpoint

D1.2 — Secrets & Configuration Hardening is CLOSED.

Accepted merge: PR #129, merge commit `1bf26f57bc94fc57b5ef3f34815cd4781e832b87`.

Validated outcomes:

- sensitive JWT/PostgreSQL/BlobStorage values are not populated in version-controlled WebApi configuration;
- local sensitive values are supplied through .NET User Secrets;
- Cloud Run sensitive values are supplied through Google Secret Manager bindings;
- JWT signing material exposed historically was rotated before the public deployment;
- Release build and the governed test suites passed;
- Clean Architecture Guard #525 completed successfully on the exact PR head before merge;
- the WebApi container runs on Linux/.NET 10 and listens on port 8080;
- the public Cloud Run service `efactura-api` is deployed in `us-east5`;
- the deployed API reaches Neon PostgreSQL successfully;
- authenticated `GET /api/v1/parties` returned HTTP 200 from the public Cloud Run URL;
- no Redis deployment was required for that accepted public runtime path.

Public API base URL at D1.2 closure:

`https://efactura-api-195831190862.us-east5.run.app`

D1.2 infrastructure is an accepted baseline. D2 must not recreate Cloud Run, Artifact Registry, Secret Manager, the Cloud Run service account, or the Neon database unless a demonstrated requirement makes a change necessary.

## Documentation-as-you-build rule

D2 uses continuous technical documentation as part of the implementation itself.

A material backend/runtime/deployment change is not considered fully recorded until the repository documentation captures, as applicable:

- why the change was needed;
- the accepted design decision and alternatives rejected when relevant;
- affected source/configuration/workflow files;
- public/runtime behavior before and after the change;
- security implications and secret-handling boundaries;
- build/test/CI evidence;
- Cloud Run revision and image lineage when deployed;
- smoke-test evidence and rollback implications;
- known limitations, debt and deferred work;
- dependencies on parallel project lanes.

The living integral technical reference is:

`documentation/TECHNICAL_SOLUTION_CURRENT.md`

It SHALL be updated throughout the work, not reconstructed only at phase completion. At any checkpoint it should contain enough verified context to produce or update a detailed technical report of the whole solution without relying on chat history.

Historical implementation/evidence documents remain historical records and must not be silently rewritten to make old observations appear current.

## D2 objective

Turn the successful one-time deployment into a repeatable, governed demo-delivery path in which accepted API increments can be built, deployed as new Cloud Run revisions, verified from the public Internet, and progressively consumed by the deployed WebApp when the parallel governed UI lane is ready.

The API must no longer be treated as a capability that is only practically inspectable from a developer workstation.

## Mandatory D2 exit condition — public Swagger/OpenAPI

D2 SHALL NOT be considered complete unless Swagger/OpenAPI is functional in the deployed demo environment.

At D2 closure all of the following must be true:

1. `GET /swagger` (or its canonical redirected UI path) is reachable on the public Cloud Run service.
2. `GET /swagger/v1/swagger.json` returns a valid OpenAPI document from the deployed revision.
3. Swagger is not enabled in Production accidentally or only because the whole application is running with `Development` environment semantics.
4. Demo Swagger exposure is controlled explicitly by configuration so another Production deployment can keep it disabled without recompilation.
5. The OpenAPI UI preserves Bearer/JWT authorization support.
6. Protected endpoints remain protected by the application authorization policy; Swagger metadata alone must never be treated as authorization enforcement.
7. No secret value, connection string or signing material is exposed through Swagger, configuration endpoints, logs or generated OpenAPI content.
8. The deployed Swagger contract is smoke-tested after deployment and is part of the D2 acceptance evidence.

Configuration contract:

`Swagger:Enabled=false` by default, with the demo Cloud Run revision explicitly setting `Swagger__Enabled=true`.

Development may continue to expose Swagger without requiring that demo flag.

## Parallel-lane dependency rule

This chat/workstream is the backend/API/deployment lane.

D2.3 depends on the separate governed UI/POS lane where the WebApp views are still being designed, approved and implemented. Therefore:

- D2.3 SHALL NOT be implemented in the backend lane;
- backend work SHALL NOT invent or prematurely reshape UI behavior to unblock D2.3;
- the backend lane may reach `READY_FOR_DEMO_INTEGRATION` while D2.3 remains `BLOCKED_BY_PARALLEL_UI_LANE`;
- Swagger/OpenAPI and the deployed API contract are the handoff surface between lanes;
- D2.3 begins only when the parallel UI lane is sufficiently mature and explicitly ready for real-API integration.

## D2 delivery sequence

### D2.1 — Controlled second API revision + public Swagger

Purpose: prove that the existing infrastructure can receive a new application revision without recreating the platform.

Scope:

- make Swagger exposure configuration-driven;
- keep Production behavior secure by default;
- build and validate the change through the normal governed CI path;
- deploy a new image/revision to the existing `efactura-api` Cloud Run service;
- reuse the existing service account, Secret Manager bindings, Neon database and runtime configuration;
- explicitly enable Swagger only for the demo runtime;
- smoke-test `/swagger`, `/swagger/v1/swagger.json`, unauthenticated protected-endpoint behavior, and an authenticated API path;
- record the deployed revision and image digest.

D2.1 proves repeatability. It was intentionally manual and controlled before deployment automation is introduced.

D2.1 status: `CLOSED / DEPLOYED / VERIFIED`.

Accepted evidence:

- PR #133 final head `724936e84d5dfc67afb929b1430d348e34583777`;
- Clean Architecture Guard #534 / workflow run `35180211947`: SUCCESS;
- merge commit `8cbc896d5be7a8ff9d3ebaf0e84bd3ca56580360`;
- Artifact Registry tag `d21-swagger`;
- immutable OCI index digest `sha256:3113a175da3929a8ca3c0e3fcc18447ac9e6ef62a5bccdb08d6b491feec5234d`;
- resolved Cloud Run `linux/amd64` manifest `sha256:b45a2e5d7edc15f40ea4a09b34812245853bc1273e99c00d1ad7c378a8a0f4b4`;
- Cloud Run revision `efactura-api-d21-swagger-01`;
- traffic promoted to 100% after canary validation;
- public service URL `https://efactura-api-yblnutgx3q-ul.a.run.app`;
- public Swagger `/swagger`: canonical HTTP 301 redirect followed to HTTP 200;
- public OpenAPI JSON: HTTP 200;
- protected parties endpoint: HTTP 401 without JWT, HTTP 200 with valid JWT against Neon;
- prior known-good revision `efactura-api-00001-b8c` retained as rollback target.

The next backend increment is D2.2 — Reproducible API deployment workflow.

### D2.2 — Reproducible API deployment workflow

Status: `IMPLEMENTATION READY / CI PENDING`.

Governed branch:

`deployment/d2-2-reproducible-api-deploy`

D2.2 introduces an API-specific GitHub Actions workflow independent from the WebApp FTP deployment.

Target flow:

`feature branch -> PR -> Clean Architecture Guard -> explicit merge approval -> main -> local container build -> OIDC/WIF authentication -> Artifact Registry push -> zero-traffic tagged Cloud Run canary -> canary smoke -> 100% promotion -> public smoke -> automatic traffic rollback on failed post-promotion acceptance`

Security properties:

- no Google service-account JSON key;
- no JWT key or database connection string stored in GitHub;
- WIF trust restricted to this repository and `refs/heads/main`;
- deployment service account separated from the Cloud Run runtime service account;
- immutable image digest used for Cloud Run deployment;
- temporary Google credential files excluded from Git and Docker build context.

Detailed implementation and acceptance evidence:

`documentation/deployment/D2_2_REPRODUCIBLE_API_DEPLOYMENT.md`

D2.2 does not become CLOSED until the first merge-triggered automated deployment succeeds and its exact run/revision/image evidence is reconciled.

### D2.3 — Demo WebApp to real API integration

Status at D2 opening: `BLOCKED_BY_PARALLEL_UI_LANE`.

This work belongs to the parallel governed WebApp/UI lane, not this backend chat.

When that lane is ready, it may progressively replace approved mock-backed WebApp slices with real API-backed slices only where the authoritative API contract already exists.

Requirements:

- no invented endpoints;
- no UI behavior that contradicts the accepted domain/API contracts;
- environment-driven API base URL;
- preserve mock mode where useful for isolated visual work until a slice is explicitly integrated;
- validate CORS/auth/error behavior against the public API.

### D2.4 — Backend operational polish and closure evidence

The backend lane may complete this work while D2.3 remains blocked by the parallel UI lane.

Before backend readiness is declared, reconcile the deployment documentation and capture at minimum:

- current Cloud Run service and accepted revision;
- image/tag/digest lineage;
- configuration and secret-binding names without secret values;
- repeatable deploy command/workflow;
- rollback procedure to a prior known-good revision;
- public API smoke-test evidence;
- public Swagger/OpenAPI smoke-test evidence;
- UI integration dependency/status;
- remaining known operational debt explicitly separated from blockers.

Backend milestone:

`D2 BACKEND LANE: READY_FOR_DEMO_INTEGRATION`

may be reached before D2.3 executes.

## D2 non-goals unless evidence makes them necessary

The following do not automatically block D2 backend readiness:

- Redis deployment;
- Application Insights;
- System.Drawing cleanup unrelated to an observed demo failure;
- redesign of fiscal/domain behavior;
- recreation of already-working Google Cloud resources;
- speculative infrastructure changes without a failing use case;
- completion of still-in-progress UI views in the backend lane.

## Governance

- `main` remains the accepted stable line.
- D2 implementation occurs on governed feature/deployment branches.
- Clean Architecture Guard must pass for merge candidates.
- A PR is not merge authorization.
- Merge requires Luis's explicit approval for the exact PR.
- Deployment success does not waive code-review or contract-governance requirements.
- material implementation and deployment decisions must be captured in repository documentation as they occur.

## D2 completion model

### Backend lane readiness

The backend lane reaches `READY_FOR_DEMO_INTEGRATION` when:

- at least one post-D1.2 API revision has been deployed successfully to the existing Cloud Run service;
- the repeatable API deployment path is documented and preferably automated after the controlled second deployment;
- public Swagger/OpenAPI is deliberately enabled for the demo and validated from the Internet;
- rollback and smoke-test procedures are documented;
- the living integral technical document is current;
- the accepted backend state is reconciled into project checkpoint documentation.

### Full D2 closure

Full D2 closure additionally requires the parallel UI lane to complete at least one approved real-API WebApp integration slice where the authoritative contract exists.

Until then, D2.3 remains `BLOCKED_BY_PARALLEL_UI_LANE` without blocking continued backend development or demonstration through public Swagger/OpenAPI.
