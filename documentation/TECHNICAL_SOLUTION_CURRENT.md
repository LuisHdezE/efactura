# eFactura — Current Technical Solution

Status: LIVING TECHNICAL RECORD

Last reconciled phase: D2 — Repeatable Deployment & Demo Integration

Accepted baseline at D2 opening: `main@1bf26f57bc94fc57b5ef3f34815cd4781e832b87`

This document is the current integral technical description of the eFactura solution. It is intentionally maintained during implementation rather than reconstructed only at the end of a phase.

It complements, but does not replace, the detailed architecture, API-contract, implementation, UI-governance and historical brownfield records already stored under `documentation/`.

## 1. Documentation governance

Every governed backend increment that materially changes runtime behavior, deployment, configuration, public API exposure, infrastructure assumptions, security boundaries or integration behavior SHALL update the appropriate technical documentation in the same branch/PR.

At minimum, each relevant increment must preserve enough evidence to answer:

- what changed;
- why it changed;
- which component owns the behavior;
- which configuration keys are involved;
- which secrets or external dependencies are required, without recording secret values;
- how the behavior is built, deployed and verified;
- which Cloud Run revision/image/commit carries it when deployed;
- which smoke tests or CI evidence validate it;
- what remains intentionally out of scope or blocked by another lane.

This file is the preferred source for a current end-to-end technical explanation. Phase-specific evidence remains in dedicated records such as `documentation/deployment/D2_REPEATABLE_DEPLOYMENT_DEMO_INTEGRATION.md`.

When a detailed technical report is requested, this living record can be used as the current spine and enriched with the supporting implementation, contract and deployment records. It must be updated rather than silently rewritten to erase historical decisions.

## 2. Solution shape

The accepted solution is a brownfield modernization centered on a .NET 10 Web API governed by Clean Architecture and Ports & Adapters constraints.

Primary runtime components:

- `src/WebApi`: ASP.NET Core HTTP entry point and composition root;
- Application layer/use cases under the governed application projects;
- Infrastructure persistence and external integrations;
- PostgreSQL/MySQL provider support for governed persistence paths;
- React WebApp deployed separately for demo/UI work;
- GitHub Actions plus the dedicated self-hosted CI runner `efactura-ci-01` for the governed backend guard.

The WebApp and WebApi have independent deployment paths. The WebApp is not hosted in the Cloud Run API service.

## 3. Accepted backend deployment topology

Current demo backend topology at D2 opening:

`Internet -> Google Cloud Run / efactura-api -> ASP.NET Core WebApi -> Neon PostgreSQL`

Google Cloud project:

`efactura-demo-0916-9b93`

Cloud Run region:

`us-east5`

Cloud Run service:

`efactura-api`

Public base URL:

`https://efactura-api-195831190862.us-east5.run.app`

Accepted first deployed revision:

`efactura-api-00001-b8c`

The Cloud Run service allows unauthenticated network access at the platform boundary, while protected application endpoints continue to enforce JWT/authz inside the WebApi.

## 4. Container and runtime

The WebApi is built as a Linux container using .NET 10 SDK/runtime and listens on container port `8080`.

Accepted D1.2 validation demonstrated:

- image build success;
- Linux runtime startup success;
- Kestrel listening on port 8080;
- JWT validation success;
- permission authorization success;
- Cloud Run to Neon PostgreSQL connectivity;
- authenticated `GET /api/v1/parties` returning HTTP 200;
- unauthenticated protected access returning HTTP 401.

D2 treats this infrastructure as an accepted baseline. Normal application increments must deploy new revisions of the existing service rather than recreate the platform.

## 5. Persistence

The demo API currently uses Neon PostgreSQL.

Runtime persistence configuration is supplied through:

- `V1Persistence__Provider=PostgreSql`;
- `V1Persistence__ConnectionStringName=PostgresConnection`;
- `ConnectionStrings__PostgresConnection` supplied by Google Secret Manager binding.

The repository and CI retain provider-real PostgreSQL and MySQL coverage for the governed persistence suite.

Redis is not part of the accepted required demo path unless a concrete endpoint demonstrates that dependency.

## 6. Secrets and configuration

D1.2 — Secrets & Configuration Hardening is closed and accepted through PR #129.

Version-controlled WebApi configuration does not contain populated values for:

- `Jwt:Key`;
- `ConnectionStrings:PostgresConnection`;
- `ConnectionStrings:BlobStorage`.

Local development sensitive values use .NET User Secrets.

Cloud Run sensitive values use Google Secret Manager.

Current secret names used by the demo backend:

- `efactura-jwt-key`;
- `efactura-postgres-connection`.

Current Cloud Run service account:

`efactura-run@efactura-demo-0916-9b93.iam.gserviceaccount.com`

It has secret-accessor permission scoped to the required demo secrets rather than unrestricted secret access.

Secret values must never be copied into this document, Git history, workflow YAML, image layers, generated OpenAPI content or chat transcripts.

## 7. Authentication and authorization

The WebApi uses JWT Bearer authentication.

Runtime authorization includes permission-aware application policies and API v1 authorization handling. Cloud Run public reachability is therefore not equivalent to anonymous application access.

Swagger/OpenAPI Bearer metadata is documentation/client tooling only. It does not replace runtime authentication or authorization enforcement.

## 8. Swagger/OpenAPI

At D2 opening, Swagger services and Bearer metadata already exist in `src/WebApi/Program.cs`, but Swagger middleware is enabled only when `app.Environment.IsDevelopment()`.

Because Cloud Run runs with `ASPNETCORE_ENVIRONMENT=Production`, Swagger is not publicly available in the accepted first revision.

D2 introduces the following intended configuration contract:

`Swagger:Enabled=false` by default.

Development remains allowed to expose Swagger automatically. The demo Cloud Run environment will deliberately set:

`Swagger__Enabled=true`

D2 cannot close until the deployed demo exposes and validates:

- `/swagger` (or canonical redirect);
- `/swagger/v1/swagger.json`;
- Bearer/JWT interaction without weakening protected endpoints.

Swagger exposure is a demo/documentation capability and must not leak secrets or change endpoint authorization semantics.

## 9. Logging and observability

The current WebApi clears default logging providers and configures Serilog primarily to file output under the container filesystem.

This is accepted technical debt from D1.2 because it did not block deployment or API behavior. D2 may improve Cloud Run stdout/stderr integration if done as a bounded operational increment, but logging cleanup is not allowed to become a speculative blocker for public API repeatability.

Application Insights remains optional for the demo unless a concrete requirement makes it necessary.

## 10. HTTPS behind Cloud Run

The application currently calls `UseHttpsRedirection()`.

The first container/deployment validation observed the known warning that an internal HTTPS port could not be determined. Cloud Run terminated public HTTPS successfully and HTTP application requests were not blocked.

This remains operational debt to evaluate based on evidence rather than a reason to recreate the deployment.

## 11. Deployment model

D1.2 established the first controlled manual deployment.

D2 target flow is:

`feature/deployment branch -> PR -> Clean Architecture Guard -> explicit merge approval -> main -> image build -> Artifact Registry -> new Cloud Run revision -> public smoke tests`

D2.1 intentionally performs a second controlled deployment before automation is introduced.

D2.2 then codifies the repeatable API deployment path.

The existing `.github/workflows/deploy-demo.yml` is a WebApp/FTP workflow and is not the API Cloud Run deployment pipeline.

## 12. Frontend/UI parallel lane boundary

The React visual/demo lane is being developed in parallel and is intentionally outside this backend-focused working lane.

D2.3 — WebApp mock to real API integration depends on sufficient maturity and approval of that parallel UI lane.

Therefore D2.3 is currently classified as:

`BLOCKED_BY_PARALLEL_UI_LANE`

This status is a coordination dependency, not a backend defect.

The backend lane may reach:

`READY_FOR_DEMO_INTEGRATION`

when Swagger, repeatable API deployment, public smoke tests, rollback procedure and backend operational documentation are complete.

No backend increment may invent temporary UI screens, fake frontend contracts or unsupported endpoints merely to unblock D2.3.

## 13. Current phase: D2

### D2.1 — Controlled second API revision + public Swagger

Objectives:

- make Swagger exposure explicitly configuration-driven;
- keep it disabled by default outside Development;
- deploy a second revision to the existing Cloud Run service;
- reuse existing Artifact Registry, service account, secrets and Neon connection;
- enable Swagger only in the demo runtime;
- validate public Swagger JSON/UI plus JWT-protected API behavior;
- record image and revision lineage.

### D2.2 — Repeatable API deployment

Objectives:

- remove unnecessary manual deployment steps;
- preserve secret isolation;
- deploy accepted `main` as a new revision of the existing service;
- make post-deploy smoke tests part of the governed delivery evidence;
- document rollback.

### D2.3 — WebApp integration

Owner lane: parallel UI/WebApp work.

Current state: `BLOCKED_BY_PARALLEL_UI_LANE`.

It will not be implemented from this backend-focused lane.

### D2.4 — Backend operational closure

Objectives:

- reconcile accepted Cloud Run revision/image lineage;
- document current configuration names and secret bindings;
- validate rollback and smoke tests;
- reconcile this living technical record and the current project checkpoint;
- mark the backend lane `READY_FOR_DEMO_INTEGRATION` when appropriate.

## 14. Evidence ledger

Accepted D1.2 / D2-opening evidence:

- PR #129 merged;
- merge commit: `1bf26f57bc94fc57b5ef3f34815cd4781e832b87`;
- Clean Architecture Guard #525: SUCCESS on the approved PR head;
- Cloud Run service `efactura-api`: deployed and publicly reachable;
- accepted first revision: `efactura-api-00001-b8c`;
- accepted image tag: `us-east5-docker.pkg.dev/efactura-demo-0916-9b93/efactura/webapi:d12`;
- accepted image digest: `sha256:7b4b3e5f5093c0b2b5d37740cc499654dd115f748dd24dc05b04d1f91d9e3365`;
- public authenticated `GET /api/v1/parties`: HTTP 200;
- unauthenticated protected call: HTTP 401.

New D2 evidence must be appended/reconciled as increments are completed.

## 15. Known non-blocking debt

Unless a real failing use case proves otherwise, the following are tracked but are not automatic D2 blockers:

- Redis deployment;
- System.Drawing Linux warnings unrelated to a demonstrated endpoint failure;
- Application Insights;
- Serilog-to-stdout/stderr improvement;
- Cloud Run-specific handling of `UseHttpsRedirection()`;
- custom API demo domain.

## 16. Update rule

This document must be updated whenever an accepted change makes any material statement above stale.

A final technical report is therefore not a one-time archaeological exercise. It is a rendered snapshot of a continuously maintained technical record plus its supporting evidence.