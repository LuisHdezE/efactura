# eFactura — Current Technical Solution

Status: LIVING TECHNICAL RECORD

Last reconciled backend increment:

`D2.4 — Backend Operational Closure (closure candidate)`

Current governed repository baseline entering D2.4:

`main@9e1b9b6758215aa875e32d5792f85e6655dd7e07`

Current accepted deployed backend source:

`7eed21f969486bd689b5abf2e9c37ba2462b00bd`

This document is the current integral technical description of the eFactura solution. It is maintained during implementation rather than reconstructed only at phase end.

It complements, but does not replace, architecture, API-contract, fiscal implementation, UI-governance, Blueprint and deployment evidence under `documentation/`.

## 1. Documentation governance

Every governed backend increment that materially changes runtime behavior, deployment, configuration, public API exposure, infrastructure assumptions, security boundaries or integration behavior SHALL update appropriate repository documentation.

Current primary records:

- `documentation/TECHNICAL_SOLUTION_CURRENT.md`;
- `documentation/BLUEPRINT_CURRENT_STATE.md`;
- `documentation/deployment/D2_REPEATABLE_DEPLOYMENT_DEMO_INTEGRATION.md`;
- `documentation/deployment/D2_2_REPRODUCIBLE_API_DEPLOYMENT.md`;
- `documentation/deployment/D2_4_BACKEND_OPERATIONAL_CLOSURE.md`.

Historical implementation/evidence records remain historical and are not rewritten to make past observations appear current.

## 2. Solution shape

The accepted solution is a brownfield modernization centered on a .NET 10 Web API governed by Clean Architecture and Ports & Adapters constraints.

Primary components:

- `src/WebApi`: ASP.NET Core HTTP entry point/composition root;
- Application/use-case layers under governed application projects;
- Infrastructure persistence and external integrations;
- PostgreSQL/MySQL provider support in repository validation;
- Neon PostgreSQL for the public demo runtime;
- React WebApp deployed independently for UI/demo work;
- GitHub Actions for CI and deployment;
- dedicated self-hosted runner `efactura-ci-01` for Clean Architecture Guard and provider-real persistence tests.

The WebApp and WebApi have independent deployment paths. The WebApp is not hosted in the Cloud Run API service.

## 3. Accepted public backend topology

Topology:

`Internet -> Google Cloud Run / efactura-api -> ASP.NET Core WebApi -> Neon PostgreSQL`

Google Cloud project:

`efactura-demo-0916-9b93`

Project number:

`195831190862`

Region:

`us-east5`

Artifact Registry repository:

`us-east5-docker.pkg.dev/efactura-demo-0916-9b93/efactura`

Cloud Run service:

`efactura-api`

Stable public API URL:

`https://efactura-api-yblnutgx3q-ul.a.run.app`

Current accepted deployed revision:

`efactura-api-d22-7eed21f-46-1`

Current accepted source commit for that revision:

`7eed21f969486bd689b5abf2e9c37ba2462b00bd`

Current accepted image tag:

`us-east5-docker.pkg.dev/efactura-demo-0916-9b93/efactura/webapi:sha-7eed21f969486bd689b5abf2e9c37ba2462b00bd`

Current accepted immutable Artifact Registry digest:

`sha256:0fec87aa1700f24c3f54682f2ab5940af0782fcdf1ebae9629ba05b8fb3c77ac`

Current retained rollback revision:

`efactura-api-d21-swagger-01`

The repository later advanced to `9e1b9b6758215aa875e32d5792f85e6655dd7e07` through documentation-only PR #148. That merge did not match API deployment path filters and therefore did not replace the accepted deployed revision.

## 4. Container/runtime contract

The WebApi is built as a Linux/.NET 10 container and listens on port `8080`.

Accepted runtime evidence includes:

- Linux image build success;
- Kestrel startup success;
- Cloud Run public reachability;
- JWT authentication success;
- permission-aware authorization success;
- Cloud Run -> Neon PostgreSQL connectivity;
- protected endpoint HTTP `401` without JWT;
- authenticated protected endpoint HTTP `200` with valid JWT;
- repeatable revision deployment without recreating platform resources.

Normal backend increments deploy new revisions of the existing service. They do not recreate Cloud Run, Artifact Registry, Secret Manager or Neon without demonstrated need.

## 5. Persistence

Public demo persistence provider:

`PostgreSql`

Current configuration contract:

- `V1Persistence__Provider=PostgreSql`;
- `V1Persistence__ConnectionStringName=PostgresConnection`;
- `ConnectionStrings__PostgresConnection` supplied by Cloud Run Secret Manager binding.

The repository retains provider-real PostgreSQL and MySQL transaction coverage in governed CI.

Redis is not part of the accepted public demo path unless a future concrete endpoint demonstrates that dependency.

## 6. Secrets and configuration

D1.2 — Secrets & Configuration Hardening remains closed and accepted.

Version-controlled WebApi configuration does not contain populated sensitive values for:

- `Jwt:Key`;
- `ConnectionStrings:PostgresConnection`;
- `ConnectionStrings:BlobStorage`.

Local development uses .NET User Secrets for sensitive local values.

Cloud Run uses Google Secret Manager.

Accepted secret names used by the demo backend include:

- `efactura-jwt-key`;
- `efactura-postgres-connection`.

Secret values must not be copied into repository documentation, Git history, workflow YAML, image layers, generated OpenAPI or chat transcripts.

## 7. Runtime and deployment identities

Runtime service account:

`efactura-run@efactura-demo-0916-9b93.iam.gserviceaccount.com`

Deployment service account:

`efactura-deploy@efactura-demo-0916-9b93.iam.gserviceaccount.com`

The identities remain intentionally separate.

The runtime identity owns runtime access required by the application.

The deployment identity is bounded to publishing the API image, updating the existing Cloud Run service, acting as the runtime service account during deployment and reading the JWT signing secret needed for the authenticated smoke check.

## 8. GitHub OIDC / Workload Identity Federation

API deployment does not use a long-lived Google service-account JSON key.

Accepted provider:

`projects/195831190862/locations/global/workloadIdentityPools/github-actions/providers/efactura-main`

Trust is restricted to:

- GitHub owner id `88979457`;
- repository id `893259774`;
- ref `refs/heads/main`.

The accepted D2.2 deployment authenticated successfully using short-lived GitHub OIDC/WIF credentials.

Feature branches and pull-request refs are not accepted deployment identities.

## 9. Authentication and authorization

The WebApi uses JWT Bearer authentication.

Runtime authorization includes permission-aware application policies and API v1 authorization handling.

Cloud Run permits public network reachability at the platform boundary, but protected application endpoints remain protected inside the WebApi.

Swagger/OpenAPI Bearer metadata is client/documentation support only. It does not replace runtime authn/authz.

The D2.2 workflow generates an ephemeral smoke JWT with minimum `parties.read` permission after reading the signing secret through Secret Manager. The secret and generated token are masked and are not persisted as workflow outputs.

## 10. Swagger/OpenAPI

Configuration contract:

`Swagger:Enabled=false` by default.

Development may expose Swagger automatically.

The demo Cloud Run revision explicitly sets:

`Swagger__Enabled=true`

Accepted D2.2 public/canary behavior:

- `/swagger`: final HTTP `200` after redirect handling;
- `/swagger/v1/swagger.json`: HTTP `200` and valid OpenAPI structure;
- protected parties endpoint: HTTP `401` without JWT;
- protected parties endpoint: HTTP `200` with valid JWT + Neon;
- current JWT signing material absent from generated OpenAPI.

The checks passed first against the zero-traffic canary and then against the stable public URL after promotion.

`UseDeveloperExceptionPage()` remains Development-only. Demo Swagger exposure therefore does not require Development environment semantics.

## 11. API deployment workflow

Accepted API workflow:

`.github/workflows/deploy-api-demo.yml`

It is separate from the WebApp FTP workflow:

`.github/workflows/deploy-demo.yml`

Accepted automated delivery model:

`accepted main -> linux/amd64 local build -> short-lived WIF auth -> Artifact Registry push -> immutable digest -> zero-traffic tagged Cloud Run canary -> Swagger/OpenAPI/401/JWT+Neon smoke -> promote 100% -> repeat public smoke -> rollback traffic if post-promotion acceptance fails`

First fully accepted automated deployment:

- workflow: `Deploy eFactura API Demo`;
- run #46;
- run id `35406809175`;
- source commit `7eed21f969486bd689b5abf2e9c37ba2462b00bd`;
- conclusion `SUCCESS`.

The workflow builds before cloud authentication to reduce credential lifetime and deploys Cloud Run from the immutable registry digest rather than from the mutable tag.

## 12. Canary, promotion and rollback

Accepted D2.2 candidate/accepted revision:

`efactura-api-d22-7eed21f-46-1`

Canary tag:

`d22-46-1`

Accepted canary URL:

`https://d22-46-1---efactura-api-yblnutgx3q-ul.a.run.app`

The candidate was created with zero production traffic.

Canary smoke:

`PASS`

Only then was traffic promoted:

`efactura-api-d22-7eed21f-46-1=100`

Public post-promotion smoke:

`PASS`

Captured rollback revision:

`efactura-api-d21-swagger-01`

If public post-promotion smoke fails, the workflow refreshes short-lived Google credentials, restores 100% traffic to the captured prior revision and fails the deployment.

The accepted run did not execute rollback because public acceptance passed.

The manual emergency rollback procedure and verification commands are documented in:

`documentation/deployment/D2_4_BACKEND_OPERATIONAL_CLOSURE.md`

## 13. Logging and observability

The WebApi currently clears default logging providers and configures Serilog primarily to file output under the container filesystem.

This remains non-blocking technical debt because it has not prevented accepted deployment or public API behavior.

Potential later bounded improvement:

- Cloud Run stdout/stderr integration.

Application Insights remains optional unless a demonstrated requirement makes it necessary.

## 14. HTTPS behind Cloud Run

The application currently calls `UseHttpsRedirection()`.

Early container validation observed the warning that an internal HTTPS port could not be determined. Cloud Run terminates public HTTPS successfully and accepted public requests were not blocked.

This remains evidence-driven operational debt, not a reason to recreate the platform.

## 15. Frontend/UI parallel lane boundary

The React visual/demo lane is developed separately.

D2.3 — WebApp mock-to-real-API integration is owned by that lane.

The backend lane must not invent temporary screens, fake contracts or unsupported endpoints to accelerate frontend integration.

The handoff surface is:

- stable public API URL;
- public Swagger/OpenAPI contract;
- accepted authentication/error behavior.

The WebApp may integrate capabilities progressively as their authoritative API operations exist.

Backend deployment readiness does not imply that all endpoint-completion waves are already finished.

## 16. Accepted fiscal/product functional baseline

The deployment work in D1.2/D2 does not rewrite the accepted fiscal/product semantics established by the governed Blueprint implementation lineage.

The latest accepted product-capability boundary remains PR #108:

`feat(fiscal): compose explicit ACKCFE evidence cycle`

That boundary preserves, among other explicit safety claims:

- `DgiIdentityValidated = false`;
- `ProtocolFinalityProven = false`;
- `TokenExhaustionProven = false`;
- `AutomaticReconsultationAuthorized = false`.

D2.4 changes none of those semantics.

Formal DGI Testing acceptance and Production enablement remain separate from demo deployment readiness.

## 17. Current D2 status

### D2.1 — Controlled second API revision + public Swagger

Status:

`CLOSED / DEPLOYED / VERIFIED`

Accepted revision:

`efactura-api-d21-swagger-01`

This revision is now the deterministic rollback target retained for the accepted D2.2 deployment.

### D2.2 — Reproducible API deployment

Status:

`CLOSED / AUTOMATED / VERIFIED`

Accepted evidence:

- implementation PR #139;
- YAML indentation hotfix PR #142;
- exact-head Guard #551 / `35403102577`: SUCCESS;
- accepted deployment run #46 / `35406809175`: SUCCESS;
- immutable digest `sha256:0fec87aa1700f24c3f54682f2ab5940af0782fcdf1ebae9629ba05b8fb3c77ac`;
- accepted revision `efactura-api-d22-7eed21f-46-1`;
- canary acceptance PASS;
- promotion PASS;
- public acceptance PASS;
- WIF/OIDC confirmed;
- prior revision retained for rollback.

D2.2 documentation closure PR #148 merged as:

`9e1b9b6758215aa875e32d5792f85e6655dd7e07`

### D2.3 — WebApp integration

Owner:

`parallel UI/WebApp lane`

Backend D2.4 does not implement this increment.

Full cross-lane D2 closure remains dependent on the separately governed WebApp integration work.

### D2.4 — Backend operational closure

Status:

`CLOSURE CANDIDATE / GOVERNED REVIEW PENDING`

D2.4 reconciles:

- accepted runtime/image lineage;
- identity/WIF boundary;
- secret/configuration names;
- repeatable deployment procedure;
- rollback procedure;
- smoke evidence;
- WebApp ownership;
- known operational debt;
- distinction between demo deployment readiness and remaining product/API work;
- current Blueprint human checkpoint.

Detailed record:

`documentation/deployment/D2_4_BACKEND_OPERATIONAL_CLOSURE.md`

Intended accepted state after exact-head Guard, explicit approval and merge:

`D2 BACKEND LANE: READY_FOR_DEMO_INTEGRATION`

## 18. Operational debt classification

Known non-blocking debt includes:

- Serilog stdout/stderr integration for Cloud Run;
- Application Insights;
- `UseHttpsRedirection()` container-warning cleanup;
- unrelated Windows-only `System.Drawing` modernization;
- Redis deployment unless a concrete future endpoint requires it;
- custom demo API domain;
- broader advisory dependency/analyzer modernization.

These items are future bounded increments, not current demo-deployment blockers.

## 19. Product/API gaps are separate from D2 operational readiness

The accepted product is not endpoint-complete and is not represented as DGI-certified or production-enabled merely because the demo API is deployable.

Remaining product/API work continues under governed implementation records and the agreed API completion wave plan after D2 backend closure.

D2 operational closure therefore means:

- the backend can be deployed repeatably;
- its accepted public contract can be inspected;
- protected behavior and Neon connectivity have accepted smoke evidence;
- a deterministic rollback path exists;
- the WebApp has a stable integration surface.

It does not mean every business capability or endpoint already exists.

## 20. Evidence ledger

### D1.2

- PR #129 merged;
- initial public Cloud Run deployment accepted;
- runtime secret hardening accepted;
- JWT/Neon protected API path proven.

### D2.1

- PR #133;
- Guard #534 / `35180211947`: SUCCESS;
- merge `8cbc896d5be7a8ff9d3ebaf0e84bd3ca56580360`;
- image tag `d21-swagger`;
- OCI index digest `sha256:3113a175da3929a8ca3c0e3fcc18447ac9e6ef62a5bccdb08d6b491feec5234d`;
- runtime manifest `sha256:b45a2e5d7edc15f40ea4a09b34812245853bc1273e99c00d1ad7c378a8a0f4b4`;
- revision `efactura-api-d21-swagger-01`;
- Swagger/OpenAPI/401/JWT+Neon accepted.

### D2.2

- PR #142 approved hotfix head `ebdb495a69ca2ea83494c0ecf37852fcc98ebaa6`;
- Guard #551 / `35403102577`: SUCCESS;
- source/merge `7eed21f969486bd689b5abf2e9c37ba2462b00bd`;
- deployment run #46 / `35406809175`: SUCCESS;
- image tag `webapi:sha-7eed21f969486bd689b5abf2e9c37ba2462b00bd`;
- digest `sha256:0fec87aa1700f24c3f54682f2ab5940af0782fcdf1ebae9629ba05b8fb3c77ac`;
- revision `efactura-api-d22-7eed21f-46-1`;
- canary `200 / 200 / 401 / 200`;
- public `200 / 200 / 401 / 200`;
- final traffic 100% accepted revision;
- rollback revision `efactura-api-d21-swagger-01`.

### D2.2 documentation closure

- PR #148 exact head `e16cd350b5a7e10cc472c23940bb094fb0359141`;
- manually dispatched exact-head Clean Architecture Guard #554 / `35410181743`: SUCCESS;
- merge commit `9e1b9b6758215aa875e32d5792f85e6655dd7e07`;
- documentation only, therefore no new API deployment.

## 21. D2.4 acceptance rule

D2.4 is not accepted merely because these documents exist.

Final acceptance requires:

1. D2.4 documents and Blueprint checkpoint reconciled on one exact branch head;
2. Clean Architecture Guard success on that exact head;
3. PR mergeability against the exact current `main` base;
4. Luis's explicit approval for that exact PR;
5. merge to `main`.

After that merge, and only then, the backend lane status becomes:

`D2 BACKEND LANE: READY_FOR_DEMO_INTEGRATION`

Full D2 across backend + WebApp remains dependent on separately governed D2.3 work.