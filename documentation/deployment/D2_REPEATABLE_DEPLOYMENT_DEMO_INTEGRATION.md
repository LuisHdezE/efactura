# D2 — Repeatable Deployment & Demo Integration

Status: ACTIVE

Opened from accepted baseline: `main@1bf26f57bc94fc57b5ef3f34815cd4781e832b87`

Current backend checkpoint after D2.2:

`main@7eed21f969486bd689b5abf2e9c37ba2462b00bd`

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

D1.2 infrastructure remains an accepted baseline. D2 does not recreate Cloud Run, Artifact Registry, Secret Manager, the Cloud Run service account, or the Neon database unless a demonstrated requirement makes a change necessary.

## Documentation-as-you-build rule

D2 uses continuous technical documentation as part of the implementation itself.

A material backend/runtime/deployment change is not considered fully recorded until repository documentation captures, as applicable:

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

Historical implementation/evidence documents remain historical records and must not be silently rewritten to make old observations appear current.

## D2 objective

Turn the successful one-time deployment into a repeatable, governed demo-delivery path in which accepted API increments can be built, deployed as new Cloud Run revisions, verified from the public Internet, and progressively consumed by the deployed WebApp when the parallel governed UI lane is ready.

The API must no longer be treated as a capability that is only practically inspectable from a developer workstation.

## Mandatory D2 exit condition — public Swagger/OpenAPI

D2 SHALL NOT be considered complete unless Swagger/OpenAPI is functional in the deployed demo environment.

At D2 closure all of the following must be true:

1. `GET /swagger` or its canonical redirected UI path is reachable on the public Cloud Run service.
2. `GET /swagger/v1/swagger.json` returns a valid OpenAPI document from the deployed revision.
3. Swagger is not enabled in Production accidentally or only because the whole application is running with `Development` environment semantics.
4. Demo Swagger exposure is controlled explicitly by configuration so another Production deployment can keep it disabled without recompilation.
5. The OpenAPI UI preserves Bearer/JWT authorization support.
6. Protected endpoints remain protected by application authorization policy.
7. No secret value, connection string or signing material is exposed through Swagger, configuration endpoints, logs or generated OpenAPI content.
8. The deployed Swagger contract is smoke-tested after deployment and is part of D2 acceptance evidence.

Configuration contract:

`Swagger:Enabled=false` by default, with the demo Cloud Run revision explicitly setting `Swagger__Enabled=true`.

Development may continue to expose Swagger without requiring that demo flag.

D2.1 and D2.2 have both validated the public Swagger/OpenAPI requirement against accepted Cloud Run revisions.

## Parallel-lane dependency rule

This workstream is the backend/API/deployment lane.

D2.3 belongs to the separate governed UI/POS lane. Therefore:

- D2.3 SHALL NOT be implemented in the backend lane;
- backend work SHALL NOT invent or prematurely reshape UI behavior to unblock D2.3;
- the backend lane may reach `READY_FOR_DEMO_INTEGRATION` while D2.3 remains owned by the parallel UI lane;
- Swagger/OpenAPI and the deployed API contract are the handoff surface between lanes;
- D2.3 begins only when the parallel UI lane is sufficiently mature and explicitly ready for real-API integration.

## D2 delivery sequence

### D2.1 — Controlled second API revision + public Swagger

Status: `CLOSED / DEPLOYED / VERIFIED`.

Purpose: prove that the existing infrastructure can receive a new application revision without recreating the platform.

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
- public Swagger `/swagger`: canonical redirect followed to HTTP 200;
- public OpenAPI JSON: HTTP 200;
- protected parties endpoint: HTTP 401 without JWT, HTTP 200 with valid JWT against Neon;
- prior known-good revision `efactura-api-00001-b8c` retained as rollback target at D2.1 closure.

D2.1 intentionally proved the rollout manually before deployment automation was introduced.

### D2.2 — Reproducible API deployment workflow

Status: `CLOSED / AUTOMATED / VERIFIED`.

D2.2 introduced the API-specific GitHub Actions workflow:

`.github/workflows/deploy-api-demo.yml`

It is independent from the WebApp FTP deployment.

Accepted delivery flow:

`feature/deployment branch -> PR -> Clean Architecture Guard -> explicit merge approval -> main -> local container build -> OIDC/WIF authentication -> Artifact Registry push -> immutable digest -> zero-traffic tagged Cloud Run canary -> canary smoke -> 100% promotion -> public smoke -> automatic traffic rollback path on failed post-promotion acceptance`

Security properties:

- no Google service-account JSON key;
- no JWT key or database connection string stored in GitHub;
- WIF trust restricted to this repository and `refs/heads/main`;
- deployment service account separated from the Cloud Run runtime service account;
- immutable image digest used for Cloud Run deployment;
- temporary Google credential files excluded from Git and Docker build context and removed after the job.

#### D2.2 implementation and hotfix lineage

- implementation PR #139 merged as `f0540d915499ce8a62ed464e36375993819cb52b`;
- the first automated run after #139 failed before creating jobs because two Python JWT heredocs were outside the YAML `run: |` indentation;
- failed workflow run #15 / run id `35298178478` therefore performed no cloud mutation;
- corrective PR #142 changed only `.github/workflows/deploy-api-demo.yml` heredoc indentation;
- approved hotfix head: `ebdb495a69ca2ea83494c0ecf37852fcc98ebaa6`;
- Clean Architecture Guard #551 / run `35403102577`: SUCCESS on that exact head;
- PR #142 merge/source commit: `7eed21f969486bd689b5abf2e9c37ba2462b00bd`.

#### First accepted automated deployment

Workflow:

`Deploy eFactura API Demo`

Accepted run:

- run number: `46`;
- run id: `35406809175`;
- source SHA: `7eed21f969486bd689b5abf2e9c37ba2462b00bd`;
- conclusion: `SUCCESS`.

Image tag:

`us-east5-docker.pkg.dev/efactura-demo-0916-9b93/efactura/webapi:sha-7eed21f969486bd689b5abf2e9c37ba2462b00bd`

Immutable Artifact Registry digest:

`sha256:0fec87aa1700f24c3f54682f2ab5940af0782fcdf1ebae9629ba05b8fb3c77ac`

Accepted Cloud Run revision:

`efactura-api-d22-7eed21f-46-1`

Tagged zero-traffic canary URL:

`https://d22-46-1---efactura-api-yblnutgx3q-ul.a.run.app`

Captured rollback revision:

`efactura-api-d21-swagger-01`

Canary acceptance:

- Swagger final HTTP `200`;
- OpenAPI HTTP `200`;
- parties without JWT HTTP `401`;
- parties with valid JWT + Neon HTTP `200`;
- result: `PASS`.

Promotion:

- `100%` traffic assigned to `efactura-api-d22-7eed21f-46-1` only after canary acceptance.

Public post-promotion acceptance:

- Swagger final HTTP `200`;
- OpenAPI HTTP `200`;
- parties without JWT HTTP `401`;
- parties with valid JWT + Neon HTTP `200`;
- result: `PASS`.

Rollback logic was not invoked because the promoted revision passed public acceptance. The previously captured D2.1 revision remains the deterministic rollback target for the accepted D2.2 deployment.

Detailed D2.2 evidence:

`documentation/deployment/D2_2_REPRODUCIBLE_API_DEPLOYMENT.md`

### D2.3 — Demo WebApp to real API integration

Owner: parallel governed WebApp/UI lane.

The backend lane does not implement D2.3.

Requirements remain:

- no invented endpoints;
- no UI behavior that contradicts accepted domain/API contracts;
- environment-driven API base URL;
- preserve mock mode where useful for isolated visual work until a slice is explicitly integrated;
- validate CORS/auth/error behavior against the public API.

The WebApp may progressively consume capabilities when their API operations are deployed; it does not need to wait for completion of all later API-completion waves.

### D2.4 — Backend operational polish and closure evidence

Status: `NEXT BACKEND INCREMENT`.

The backend lane may complete D2.4 while D2.3 remains owned by the parallel UI lane.

Before backend readiness is declared, reconcile and verify at minimum:

- current Cloud Run service and accepted revision;
- image/tag/digest lineage;
- runtime and deployment service accounts;
- WIF provider and trust boundary;
- configuration and secret-binding names without secret values;
- repeatable deployment procedure;
- rollback procedure to a prior known-good revision;
- public API smoke-test evidence;
- public Swagger/OpenAPI smoke-test evidence;
- UI integration dependency/status;
- remaining known operational debt explicitly separated from blockers.

Backend milestone:

`D2 BACKEND LANE: READY_FOR_DEMO_INTEGRATION`

may be reached after D2.4 without waiting for the parallel WebApp lane to finish every UI task.

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
- D2 implementation occurs on governed feature/deployment/documentation branches.
- Clean Architecture Guard must pass for applicable backend merge candidates.
- A PR is not merge authorization.
- Merge requires Luis's explicit approval for the exact PR.
- Deployment success does not waive code-review or contract-governance requirements.
- material implementation and deployment decisions must be captured in repository documentation as they occur.

## D2 completion model

### Backend lane readiness

The backend lane reaches `READY_FOR_DEMO_INTEGRATION` when:

- at least one post-D1.2 API revision has been deployed successfully to the existing Cloud Run service;
- the repeatable API deployment path is automated and documented;
- public Swagger/OpenAPI is deliberately enabled for the demo and validated from the Internet;
- rollback and smoke-test procedures are documented;
- the living integral technical document is current;
- D2.4 operational reconciliation is complete.

D2.1 and D2.2 have satisfied the deployment, Swagger, automated delivery and smoke-test prerequisites. D2.4 is the remaining backend closure increment.

### Full D2 closure

Full D2 closure additionally includes the parallel UI lane completing the approved real-API WebApp integration work defined by D2.3.

D2.3 does not block continued backend development, public Swagger demonstration, or D2.4 backend operational closure.
