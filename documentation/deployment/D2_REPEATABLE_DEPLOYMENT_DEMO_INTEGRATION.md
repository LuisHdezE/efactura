# D2 — Repeatable Deployment & Demo Integration

Status: ACTIVE / BACKEND D2.4 CLOSURE CANDIDATE

Opened from accepted baseline:

`main@1bf26f57bc94fc57b5ef3f34815cd4781e832b87`

Current governed repository checkpoint entering D2.4:

`main@9e1b9b6758215aa875e32d5792f85e6655dd7e07`

Current accepted deployed backend source:

`7eed21f969486bd689b5abf2e9c37ba2462b00bd`

## D1.2 closure checkpoint

D1.2 — Secrets & Configuration Hardening is CLOSED.

Accepted merge: PR #129, merge commit `1bf26f57bc94fc57b5ef3f34815cd4781e832b87`.

Accepted D1.2 outcomes include:

- version-controlled WebApi configuration does not contain populated JWT/PostgreSQL/BlobStorage secrets;
- local sensitive values use .NET User Secrets;
- Cloud Run sensitive values use Google Secret Manager bindings;
- the historically exposed JWT signing material was rotated before public deployment;
- Linux/.NET 10 container startup on port `8080` was proven;
- the public Cloud Run service `efactura-api` was created in `us-east5`;
- Cloud Run -> Neon PostgreSQL connectivity was proven;
- protected API behavior was proven with HTTP `401` unauthenticated and HTTP `200` authenticated;
- Redis was not required for the accepted demo path.

D1.2 infrastructure remains an accepted platform baseline. D2 does not recreate working Cloud Run, Artifact Registry, Secret Manager, service-account or Neon resources without a demonstrated requirement.

## Documentation-as-you-build rule

D2 treats technical documentation as part of implementation acceptance.

Material runtime/deployment work must record, as applicable:

- reason and design decision;
- affected code/configuration/workflow;
- public/runtime behavior;
- security and secret-handling boundaries;
- build/test/CI evidence;
- Cloud Run revision and image lineage;
- smoke-test evidence;
- rollback implications;
- known limitations and debt;
- parallel-lane dependencies.

The living integral technical reference is:

`documentation/TECHNICAL_SOLUTION_CURRENT.md`

Detailed D2 operational records are:

- `documentation/deployment/D2_2_REPRODUCIBLE_API_DEPLOYMENT.md`;
- `documentation/deployment/D2_4_BACKEND_OPERATIONAL_CLOSURE.md`.

Historical implementation/evidence documents remain historical and are not rewritten to make past observations look current.

## D2 objective

Turn the successful one-time deployment into a repeatable, governed demo-delivery path in which accepted API increments can be built, deployed as Cloud Run revisions, verified from the public Internet and progressively consumed by the separately governed WebApp.

The backend must be independently demonstrable through the public API and Swagger/OpenAPI without requiring a developer workstation.

## Mandatory D2 backend exit condition — public Swagger/OpenAPI

Backend D2 readiness requires:

1. public `/swagger` or canonical redirected UI path;
2. public `/swagger/v1/swagger.json` returning valid OpenAPI;
3. explicit configuration-driven demo exposure rather than Development-mode leakage;
4. Bearer/JWT documentation support;
5. application authorization remaining enforced independently of Swagger metadata;
6. no secret/signing material exposed through generated OpenAPI;
7. deployment smoke coverage for Swagger/OpenAPI and protected endpoint behavior.

Configuration contract:

`Swagger:Enabled=false` by default.

Demo Cloud Run explicitly sets:

`Swagger__Enabled=true`

D2.1 and D2.2 both validated this requirement against accepted Cloud Run revisions.

## Parallel-lane dependency rule

This workstream is the backend/API/deployment lane.

D2.3 belongs to the separate governed WebApp/UI lane. Therefore:

- D2.3 is not implemented in the backend lane;
- backend work does not invent or reshape UI behavior merely to unblock integration;
- Swagger/OpenAPI plus the stable public API URL are the lane handoff surface;
- the WebApp may integrate approved slices progressively when their authoritative API operations exist;
- backend operational readiness does not imply that all future API-completion waves are already implemented.

## D2 delivery sequence

### D2.1 — Controlled second API revision + public Swagger

Status: `CLOSED / DEPLOYED / VERIFIED`.

Accepted evidence:

- PR #133 final head `724936e84d5dfc67afb929b1430d348e34583777`;
- Clean Architecture Guard #534 / run `35180211947`: SUCCESS;
- merge commit `8cbc896d5be7a8ff9d3ebaf0e84bd3ca56580360`;
- Artifact Registry tag `d21-swagger`;
- OCI index digest `sha256:3113a175da3929a8ca3c0e3fcc18447ac9e6ef62a5bccdb08d6b491feec5234d`;
- resolved Cloud Run `linux/amd64` manifest `sha256:b45a2e5d7edc15f40ea4a09b34812245853bc1273e99c00d1ad7c378a8a0f4b4`;
- Cloud Run revision `efactura-api-d21-swagger-01`;
- traffic promoted to 100% after canary validation;
- stable public URL `https://efactura-api-yblnutgx3q-ul.a.run.app`;
- Swagger/OpenAPI HTTP `200`;
- parties endpoint HTTP `401` without JWT and HTTP `200` with valid JWT + Neon.

D2.1 intentionally proved the rollout manually before automation.

### D2.2 — Reproducible API deployment workflow

Status: `CLOSED / AUTOMATED / VERIFIED`.

Workflow:

`.github/workflows/deploy-api-demo.yml`

Accepted delivery flow:

`accepted main -> linux/amd64 build -> OIDC/WIF -> Artifact Registry -> immutable digest -> zero-traffic tagged canary -> canary smoke -> 100% promotion -> public smoke -> rollback branch on failed post-promotion acceptance`

Security properties:

- no permanent Google service-account JSON key;
- no database connection string stored in GitHub;
- WIF trust restricted to repository identity and `refs/heads/main`;
- deployment/runtime identities are separate;
- Cloud Run deploys by immutable digest;
- smoke JWT material is read only through the bounded secret-safe workflow path.

Implementation/hotfix lineage:

- implementation PR #139 merged as `f0540d915499ce8a62ed464e36375993819cb52b`;
- initial automated run #15 / `35298178478` failed before creating jobs because JWT heredocs were outside the YAML `run: |` indentation;
- no cloud mutation occurred in that failed run;
- PR #142 corrected only the workflow heredoc indentation;
- approved hotfix head `ebdb495a69ca2ea83494c0ecf37852fcc98ebaa6`;
- Clean Architecture Guard #551 / `35403102577`: SUCCESS;
- PR #142 merge/source commit `7eed21f969486bd689b5abf2e9c37ba2462b00bd`.

First accepted automated deployment:

- workflow `Deploy eFactura API Demo`;
- run #46 / id `35406809175`;
- conclusion `SUCCESS`;
- image tag `us-east5-docker.pkg.dev/efactura-demo-0916-9b93/efactura/webapi:sha-7eed21f969486bd689b5abf2e9c37ba2462b00bd`;
- immutable registry digest `sha256:0fec87aa1700f24c3f54682f2ab5940af0782fcdf1ebae9629ba05b8fb3c77ac`;
- accepted revision `efactura-api-d22-7eed21f-46-1`;
- captured rollback revision `efactura-api-d21-swagger-01`;
- zero-traffic canary smoke `PASS` with `200 / 200 / 401 / 200`;
- 100% promotion to the accepted revision;
- public post-promotion smoke `PASS` with `200 / 200 / 401 / 200`.

Rollback steps were correctly skipped because the public post-promotion smoke passed.

PR #148 then reconciled D2.2 documentation only. Its merge commit is:

`9e1b9b6758215aa875e32d5792f85e6655dd7e07`

That documentation-only merge did not match the API deployment workflow path filters and therefore did not create a replacement deployment.

Detailed D2.2 evidence:

`documentation/deployment/D2_2_REPRODUCIBLE_API_DEPLOYMENT.md`

### D2.3 — Demo WebApp to real API integration

Owner: parallel governed WebApp/UI lane.

Backend D2.4 does not implement D2.3.

Requirements remain:

- no invented endpoints;
- no UI behavior contradicting accepted contracts;
- environment-driven API base URL;
- mock mode may remain where useful for isolated visual work until a slice is explicitly integrated;
- CORS/auth/error behavior must be validated against the public API when integration occurs.

Full cross-lane D2 closure still depends on the separately governed D2.3 integration work.

### D2.4 — Backend operational closure

Status: `CLOSURE CANDIDATE / GOVERNED REVIEW PENDING`.

D2.4 is documentation/governance only. It does not require a new Cloud Run revision because no runtime code or deployment workflow changes are introduced.

D2.4 reconciles:

- current accepted Cloud Run service/revision;
- image/tag/digest lineage;
- runtime and deployment service accounts;
- WIF provider/trust boundary;
- configuration and secret-binding names without secret values;
- governed repeatable deployment procedure;
- automatic and manual rollback procedures;
- accepted canary/public smoke evidence;
- WebApp integration ownership;
- known non-blocking operational debt;
- distinction between deployment readiness and remaining product/API gaps.

Detailed runbook:

`documentation/deployment/D2_4_BACKEND_OPERATIONAL_CLOSURE.md`

D2.4 acceptance requires:

1. operational records reconciled;
2. `documentation/BLUEPRINT_CURRENT_STATE.md` reconciled to the current governed checkpoint without changing the accepted PR #108 functional semantics;
3. exact-head Clean Architecture Guard success;
4. Luis's explicit merge approval for the exact D2.4 PR/head;
5. merge to `main`.

Intended accepted status after merge:

`D2 BACKEND LANE: READY_FOR_DEMO_INTEGRATION`

## D2 non-goals unless evidence makes them necessary

The following do not block D2 backend readiness by themselves:

- Redis deployment;
- Application Insights;
- unrelated `System.Drawing` modernization;
- redesign of fiscal/domain behavior;
- recreation of working Google Cloud resources;
- speculative infrastructure changes;
- completion of parallel UI views;
- implementation of all later API-completion waves.

## Governance

- `main` remains the accepted stable line.
- implementation occurs on governed branches.
- Clean Architecture Guard must pass on applicable exact heads.
- a PR is not merge authorization.
- merge requires Luis's explicit approval for the exact PR/head.
- deployment success does not waive code-review or contract governance.
- material runtime/deployment decisions must remain documented.
- out-of-band cloud changes are not silently treated as accepted repository state.

## D2 completion model

### Backend lane readiness

After accepted D2.4 merge, the backend lane may be classified:

`READY_FOR_DEMO_INTEGRATION`

because:

- multiple accepted API revisions have been deployed to the existing Cloud Run service;
- repeatable API deployment is automated;
- OIDC/WIF and immutable image deployment are proven;
- public Swagger/OpenAPI is deliberate and validated;
- zero-traffic canary and promotion are proven;
- protected/authenticated Neon-backed smoke behavior is proven;
- rollback logic and manual traffic rollback are documented;
- secret/configuration boundaries are documented;
- operational debt is separated from blockers;
- the living technical record and Blueprint checkpoint are reconciled in D2.4.

### Full D2 closure

Full cross-lane D2 closure additionally depends on D2.3 in the parallel WebApp/UI lane.

That dependency does not block backend API development, Swagger demonstration, endpoint-completion planning or the backend `READY_FOR_DEMO_INTEGRATION` milestone once D2.4 is accepted.