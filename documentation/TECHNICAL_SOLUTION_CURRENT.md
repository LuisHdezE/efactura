# eFactura — Current Technical Solution

Status: LIVING TECHNICAL RECORD

Last reconciled backend increment: D2.2 — Reproducible API Deployment Workflow

Current accepted backend source baseline:

`main@7eed21f969486bd689b5abf2e9c37ba2462b00bd`

This document is the current integral technical description of the eFactura solution. It is maintained during implementation rather than reconstructed only at the end of a phase.

It complements, but does not replace, the detailed architecture, API-contract, implementation, UI-governance and historical brownfield records under `documentation/`.

## 1. Documentation governance

Every governed backend increment that materially changes runtime behavior, deployment, configuration, public API exposure, infrastructure assumptions, security boundaries or integration behavior SHALL update the appropriate technical documentation.

At minimum, each relevant increment must preserve enough evidence to answer:

- what changed;
- why it changed;
- which component owns the behavior;
- which configuration keys are involved;
- which secrets or external dependencies are required, without recording secret values;
- how the behavior is built, deployed and verified;
- which Cloud Run revision/image/commit carries it when deployed;
- which smoke tests or CI evidence validate it;
- what remains intentionally out of scope or owned by another lane.

This file is the preferred source for a current end-to-end technical explanation. Phase-specific evidence remains in dedicated records such as:

- `documentation/deployment/D2_REPEATABLE_DEPLOYMENT_DEMO_INTEGRATION.md`;
- `documentation/deployment/D2_2_REPRODUCIBLE_API_DEPLOYMENT.md`.

Historical records remain historical; they are not silently rewritten to make old observations appear current.

## 2. Solution shape

The accepted solution is a brownfield modernization centered on a .NET 10 Web API governed by Clean Architecture and Ports & Adapters constraints.

Primary components:

- `src/WebApi`: ASP.NET Core HTTP entry point and composition root;
- Application layer/use cases under the governed application projects;
- Infrastructure persistence and external integrations;
- PostgreSQL/MySQL provider support for governed persistence paths;
- React WebApp deployed independently for demo/UI work;
- GitHub Actions for governed CI and deployments;
- dedicated self-hosted runner `efactura-ci-01` for the Clean Architecture Guard and provider-real persistence tests.

The WebApp and WebApi have independent deployment paths. The WebApp is not hosted in the Cloud Run API service.

## 3. Accepted backend deployment topology

Current demo topology:

`Internet -> Google Cloud Run / efactura-api -> ASP.NET Core WebApi -> Neon PostgreSQL`

Google Cloud project:

`efactura-demo-0916-9b93`

Google Cloud project number:

`195831190862`

Cloud Run region:

`us-east5`

Artifact Registry repository:

`us-east5-docker.pkg.dev/efactura-demo-0916-9b93/efactura`

Cloud Run service:

`efactura-api`

Stable public base URL:

`https://efactura-api-yblnutgx3q-ul.a.run.app`

Current accepted deployed revision:

`efactura-api-d22-7eed21f-46-1`

Current accepted source commit:

`7eed21f969486bd689b5abf2e9c37ba2462b00bd`

Current accepted image tag:

`us-east5-docker.pkg.dev/efactura-demo-0916-9b93/efactura/webapi:sha-7eed21f969486bd689b5abf2e9c37ba2462b00bd`

Current accepted immutable Artifact Registry digest:

`sha256:0fec87aa1700f24c3f54682f2ab5940af0782fcdf1ebae9629ba05b8fb3c77ac`

Current rollback revision captured by the accepted D2.2 deployment:

`efactura-api-d21-swagger-01`

The Cloud Run service allows unauthenticated network access at the platform boundary, while protected application endpoints enforce JWT/authz inside the WebApi.

## 4. Container and runtime

The WebApi is built as a Linux container using .NET 10 SDK/runtime and listens on container port `8080`.

Accepted runtime validation demonstrates:

- Linux image build success;
- Kestrel runtime startup success;
- JWT validation success;
- permission authorization success;
- Cloud Run to Neon PostgreSQL connectivity;
- authenticated `GET /api/v1/parties` returning HTTP 200;
- unauthenticated protected access returning HTTP 401;
- repeatable deployment of a new revision without recreating infrastructure.

D2 treats the platform as an accepted baseline. Normal backend increments deploy new revisions of the existing Cloud Run service rather than recreate the platform.

## 5. Persistence

The demo API uses Neon PostgreSQL.

Runtime persistence configuration is supplied through:

- `V1Persistence__Provider=PostgreSql`;
- `V1Persistence__ConnectionStringName=PostgresConnection`;
- `ConnectionStrings__PostgresConnection` supplied by Google Secret Manager binding.

The repository and CI retain provider-real PostgreSQL and MySQL coverage for governed persistence behavior.

The exact-head Guard that accepted the D2.2 hotfix included successful PostgreSQL/MySQL transaction integration tests.

Redis is not part of the accepted required demo path unless a concrete endpoint demonstrates that dependency.

## 6. Secrets and configuration

D1.2 — Secrets & Configuration Hardening remains closed and accepted through PR #129.

Version-controlled WebApi configuration does not contain populated values for:

- `Jwt:Key`;
- `ConnectionStrings:PostgresConnection`;
- `ConnectionStrings:BlobStorage`.

Local development sensitive values use .NET User Secrets.

Cloud Run sensitive values use Google Secret Manager.

Current demo backend secret names include:

- `efactura-jwt-key`;
- `efactura-postgres-connection`.

Runtime service account:

`efactura-run@efactura-demo-0916-9b93.iam.gserviceaccount.com`

Deployment service account:

`efactura-deploy@efactura-demo-0916-9b93.iam.gserviceaccount.com`

The runtime identity owns runtime secret access. The deployment identity is separate and has only the bounded permissions required to publish the image, update the existing service, act as the runtime service account during revision deployment and read the JWT signing secret required for authenticated deployment smoke testing.

Secret values must never be copied into repository documentation, Git history, workflow YAML, image layers, generated OpenAPI content or chat transcripts.

## 7. GitHub OIDC / Google Workload Identity Federation

API deployment does not use a long-lived Google service-account JSON key.

Accepted WIF provider:

`projects/195831190862/locations/global/workloadIdentityPools/github-actions/providers/efactura-main`

Trust is restricted to:

- GitHub owner id `88979457`;
- repository id `893259774`;
- `refs/heads/main`.

The accepted automated D2.2 deployment authenticated successfully through GitHub OIDC and this WIF provider using short-lived credentials.

The workflow is intentionally incapable of obtaining the deployment identity from an arbitrary feature branch or pull-request ref.

## 8. Authentication and authorization

The WebApi uses JWT Bearer authentication.

Runtime authorization includes permission-aware application policies and API v1 authorization handling. Cloud Run public reachability is therefore not equivalent to anonymous application access.

Swagger/OpenAPI Bearer metadata is documentation/client tooling only. It does not replace runtime authentication or authorization enforcement.

The D2.2 smoke token is generated ephemerally inside the deployment runner using the configured issuer/audience and minimum `parties.read` permission. The JWT signing key is fetched only for that bounded smoke operation, masked immediately and never persisted as a workflow output.

## 9. Swagger/OpenAPI

D2.1 changed Swagger exposure to an explicit configuration contract:

`Swagger:Enabled=false` by default.

Development continues to expose Swagger automatically. The demo Cloud Run revision explicitly sets:

`Swagger__Enabled=true`

The current D2.2 accepted revision validates:

- `/swagger`: canonical redirect handling ends at HTTP `200`;
- `/swagger/v1/swagger.json`: HTTP `200`;
- protected `GET /api/v1/parties`: HTTP `401` without JWT;
- the same protected endpoint: HTTP `200` with a valid JWT against Neon;
- generated OpenAPI does not expose the current JWT signing material.

These checks passed first against the zero-traffic canary and then again against the stable public URL after 100% promotion.

`UseDeveloperExceptionPage()` remains Development-only. Swagger exposure is enabled for the demo without changing Production environment semantics or weakening application authentication/authorization.

## 10. API deployment workflow

The accepted API deployment workflow is:

`.github/workflows/deploy-api-demo.yml`

It is separate from the WebApp FTP workflow `.github/workflows/deploy-demo.yml`.

Accepted automated delivery model:

`accepted main -> local Linux image build -> short-lived WIF auth -> Artifact Registry push -> immutable digest resolution -> tagged zero-traffic Cloud Run canary -> Swagger/OpenAPI/401/JWT+Neon smoke -> promote 100% -> repeat public smoke -> rollback traffic on failed post-promotion acceptance`

The first fully accepted automated deployment is:

- workflow: `Deploy eFactura API Demo`;
- run number: `46`;
- run id: `35406809175`;
- source commit: `7eed21f969486bd689b5abf2e9c37ba2462b00bd`;
- conclusion: `SUCCESS`.

The workflow builds locally before cloud authentication, minimizing credential lifetime, then deploys Cloud Run using the immutable registry digest rather than a mutable tag.

## 11. Canary, promotion and rollback

Accepted D2.2 candidate revision:

`efactura-api-d22-7eed21f-46-1`

Canary traffic tag:

`d22-46-1`

Canary URL:

`https://d22-46-1---efactura-api-yblnutgx3q-ul.a.run.app`

The candidate was first deployed with zero production traffic.

Canary smoke result:

`PASS`

Only after canary acceptance did the workflow promote:

`efactura-api-d22-7eed21f-46-1=100`

Public post-promotion smoke result:

`PASS`

Rollback revision captured before promotion:

`efactura-api-d21-swagger-01`

Because public acceptance passed, the rollback steps were correctly skipped. The workflow contains the failure branch that obtains fresh short-lived credentials, restores traffic to the captured prior revision and fails the deployment if post-promotion smoke fails.

D2.4 will preserve this rollback procedure as part of the operational closure record.

## 12. Logging and observability

The WebApi currently clears default logging providers and configures Serilog primarily to file output under the container filesystem.

This remains accepted technical debt because it has not blocked deployment or public API behavior. Cloud Run stdout/stderr integration may be improved as a bounded operational increment if evidence justifies it.

Application Insights remains optional for the demo unless a concrete requirement makes it necessary.

## 13. HTTPS behind Cloud Run

The application currently calls `UseHttpsRedirection()`.

Early container/deployment validation observed the known warning that an internal HTTPS port could not be determined. Cloud Run terminates public HTTPS successfully and the accepted D2.1/D2.2 public requests are not blocked.

This remains operational debt to evaluate based on evidence rather than a reason to recreate the deployment.

## 14. Frontend/UI parallel lane boundary

The React visual/demo lane is developed in parallel and remains outside this backend-focused lane.

D2.3 — WebApp mock to real API integration is owned by that parallel lane.

No backend increment may invent temporary UI screens, fake frontend contracts or unsupported endpoints merely to unblock D2.3.

The public Swagger/OpenAPI contract and deployed API are the handoff surface between lanes.

The WebApp may consume a capability when its API operation is actually deployed; it does not need to wait for completion of every later API-completion wave.

## 15. Current phase: D2

### D2.1 — Controlled second API revision + public Swagger

Status: `CLOSED / DEPLOYED / VERIFIED`.

Accepted outcome:

- Swagger exposure is configuration-driven and disabled by default outside Development;
- existing Cloud Run infrastructure was reused;
- revision `efactura-api-d21-swagger-01` was promoted after canary validation;
- Artifact Registry tag `d21-swagger` resolved to OCI index digest `sha256:3113a175da3929a8ca3c0e3fcc18447ac9e6ef62a5bccdb08d6b491feec5234d`;
- Cloud Run resolved the `linux/amd64` application manifest `sha256:b45a2e5d7edc15f40ea4a09b34812245853bc1273e99c00d1ad7c378a8a0f4b4`;
- public Swagger/OpenAPI and protected JWT + Neon behavior were verified.

That D2.1 revision is now the retained rollback target for the accepted D2.2 deployment.

### D2.2 — Repeatable API deployment

Status: `CLOSED / AUTOMATED / VERIFIED`.

Implementation lineage:

- implementation PR #139;
- first automated run #15 / `35298178478` exposed a YAML heredoc indentation defect before any job or cloud mutation;
- hotfix PR #142 corrected only the two heredoc indentation blocks;
- approved hotfix head `ebdb495a69ca2ea83494c0ecf37852fcc98ebaa6`;
- Clean Architecture Guard #551 / run `35403102577`: SUCCESS;
- accepted merge/source commit `7eed21f969486bd689b5abf2e9c37ba2462b00bd`.

Accepted automated deployment:

- deployment run #46 / `35406809175`: SUCCESS;
- source-SHA tag `webapi:sha-7eed21f969486bd689b5abf2e9c37ba2462b00bd`;
- immutable registry digest `sha256:0fec87aa1700f24c3f54682f2ab5940af0782fcdf1ebae9629ba05b8fb3c77ac`;
- accepted revision `efactura-api-d22-7eed21f-46-1`;
- zero-traffic canary acceptance PASS;
- 100% promotion PASS;
- public post-promotion acceptance PASS;
- rollback target `efactura-api-d21-swagger-01` retained;
- WIF/OIDC authentication confirmed without a long-lived Google key.

Detailed evidence:

`documentation/deployment/D2_2_REPRODUCIBLE_API_DEPLOYMENT.md`

### D2.3 — WebApp integration

Owner: parallel UI/WebApp lane.

The backend lane does not implement D2.3.

### D2.4 — Backend operational closure

Status: `NEXT BACKEND INCREMENT`.

Objectives:

- reconcile current Cloud Run revision/image lineage;
- document runtime and deployment identities;
- document WIF trust and deployment procedure;
- document current configuration names and secret bindings without secret values;
- validate and document rollback procedure;
- reconcile public API and Swagger/OpenAPI smoke evidence;
- inventory remaining technical debt and separate non-blockers from blockers;
- mark the backend lane `READY_FOR_DEMO_INTEGRATION` when the operational record is complete.

## 16. Evidence ledger

### Accepted D1.2 / D2-opening evidence

- PR #129 merged;
- merge commit `1bf26f57bc94fc57b5ef3f34815cd4781e832b87`;
- Clean Architecture Guard #525: SUCCESS on the approved PR head;
- Cloud Run service `efactura-api`: publicly reachable;
- first accepted revision `efactura-api-00001-b8c`;
- image tag `us-east5-docker.pkg.dev/efactura-demo-0916-9b93/efactura/webapi:d12`;
- image digest `sha256:7b4b3e5f5093c0b2b5d37740cc499654dd115f748dd24dc05b04d1f91d9e3365`;
- authenticated public parties call HTTP 200;
- unauthenticated protected call HTTP 401.

### Accepted D2.1 evidence

- PR #133 merged from final head `724936e84d5dfc67afb929b1430d348e34583777`;
- Clean Architecture Guard #534 / workflow run `35180211947`: SUCCESS;
- merge commit `8cbc896d5be7a8ff9d3ebaf0e84bd3ca56580360`;
- Artifact Registry tag `d21-swagger`;
- immutable OCI index digest `sha256:3113a175da3929a8ca3c0e3fcc18447ac9e6ef62a5bccdb08d6b491feec5234d`;
- Cloud Run runtime manifest `sha256:b45a2e5d7edc15f40ea4a09b34812245853bc1273e99c00d1ad7c378a8a0f4b4`;
- accepted revision `efactura-api-d21-swagger-01`;
- public Swagger final HTTP 200;
- public OpenAPI HTTP 200;
- parties HTTP 401 without JWT and HTTP 200 with valid JWT against Neon.

### Accepted D2.2 evidence

- PR #142 accepted head `ebdb495a69ca2ea83494c0ecf37852fcc98ebaa6`;
- Clean Architecture Guard #551 / run `35403102577`: SUCCESS;
- merge/source commit `7eed21f969486bd689b5abf2e9c37ba2462b00bd`;
- deployment workflow run #46 / `35406809175`: SUCCESS;
- image tag `us-east5-docker.pkg.dev/efactura-demo-0916-9b93/efactura/webapi:sha-7eed21f969486bd689b5abf2e9c37ba2462b00bd`;
- immutable registry digest `sha256:0fec87aa1700f24c3f54682f2ab5940af0782fcdf1ebae9629ba05b8fb3c77ac`;
- accepted revision `efactura-api-d22-7eed21f-46-1`;
- prior rollback revision `efactura-api-d21-swagger-01`;
- canary Swagger/OpenAPI/401/JWT+Neon smoke PASS;
- final traffic `100%` to accepted revision;
- public post-promotion Swagger/OpenAPI/401/JWT+Neon smoke PASS.

Next backend increment:

`D2.4 — Backend operational closure`

## 17. Known non-blocking technical debt

Unless a real failing use case proves otherwise, the following are tracked but are not automatic D2 blockers:

- Redis deployment;
- System.Drawing Linux/Windows-only analyzer warnings unrelated to a demonstrated endpoint failure;
- Npgsql package resolution warning where `8.0.5` is requested and `8.0.8` is resolved;
- nullable annotation warnings in generated/existing code;
- obsolete cryptography API warnings around `AesCryptoServiceProvider`;
- Application Insights instrumentation-key deprecation / optional telemetry configuration;
- Serilog-to-stdout/stderr improvement for Cloud Run;
- Cloud Run-specific handling of `UseHttpsRedirection()`;
- custom API demo domain.

The accepted D2.2 Docker build completed with warnings but no build errors. These items belong to technical-debt/operational review unless a failing runtime use case elevates them.

## 18. Update rule

This document must be updated whenever an accepted change makes any material statement above stale.

A final technical report is therefore not a one-time archaeological exercise. It is a rendered snapshot of a continuously maintained technical record plus its supporting evidence.
