# D2.2 — Reproducible API Deployment Workflow

Status: CLOSED / AUTOMATED / VERIFIED

Accepted implementation lineage:

- implementation PR: `#139`;
- implementation merge: `f0540d915499ce8a62ed464e36375993819cb52b`;
- YAML hotfix PR: `#142`;
- hotfix exact-head Guard: `#551` / run `35403102577` / SUCCESS;
- hotfix approved head: `ebdb495a69ca2ea83494c0ecf37852fcc98ebaa6`;
- accepted merge/source commit: `7eed21f969486bd689b5abf2e9c37ba2462b00bd`;
- first accepted automated deployment: workflow run `#46`, run id `35406809175`.

## 1. Purpose

D2.2 converts the successful manual D2.1 Cloud Run sequence into a governed, repeatable GitHub Actions deployment path for the WebApi.

The workflow is intentionally separate from the existing WebApp FTP workflow. It does not deploy or modify the React WebApp.

The first merge-triggered automated deployment has now completed successfully end to end, so D2.2 is no longer implementation-ready only; it is an accepted operational deployment capability.

## 2. Deployment trigger

The API workflow is:

`.github/workflows/deploy-api-demo.yml`

It may run:

- automatically after accepted backend/deployment changes are merged to `main`;
- manually through `workflow_dispatch`, but the job itself only executes when the selected ref is `refs/heads/main`.

Path filters intentionally cover the WebApi and its backend dependency projects, Docker build inputs, and the deployment workflow itself. Pure WebApp/UI changes do not trigger the API deployment workflow.

## 3. Authentication model

D2.2 does not use a Google service-account JSON key.

GitHub Actions authenticates with Google Cloud using OIDC + Workload Identity Federation.

Provider:

`projects/195831190862/locations/global/workloadIdentityPools/github-actions/providers/efactura-main`

Deployment service account:

`efactura-deploy@efactura-demo-0916-9b93.iam.gserviceaccount.com`

Runtime service account:

`efactura-run@efactura-demo-0916-9b93.iam.gserviceaccount.com`

The WIF provider is restricted to:

- GitHub repository owner id `88979457`;
- GitHub repository id `893259774`;
- ref `refs/heads/main`.

This prevents a branch or pull-request context from receiving the deployment identity.

Run `35406809175` authenticated successfully through this WIF path using `google-github-actions/auth@v3`; no permanent Google credential was required.

## 4. Deployment service-account permissions

The deployment identity is intentionally distinct from the runtime identity.

Its bounded responsibilities are:

- publish WebApi images to the existing Artifact Registry repository;
- update the existing Cloud Run service;
- act as the existing Cloud Run runtime service account during revision deployment;
- read only the JWT signing secret required to perform the authenticated smoke test.

The runtime service account continues to own runtime secret access for the application itself.

## 5. Secret handling

No JWT key, PostgreSQL connection string, Google credential JSON, or other secret is committed to GitHub.

The workflow:

- obtains short-lived Google credentials via OIDC;
- reads `efactura-jwt-key` from Secret Manager only inside the smoke-test shell;
- masks the value immediately;
- never persists the JWT key or generated test token as a workflow output;
- excludes `gha-creds-*.json` from Git and Docker build context;
- removes exported temporary credentials during post-job cleanup.

The PostgreSQL connection string is not read by the deployment workflow. Cloud Run continues to receive it through the existing Secret Manager runtime binding.

The accepted run also checked that the generated OpenAPI document did not contain the current JWT signing material.

## 6. Build and immutable image strategy

The container is built locally on the GitHub-hosted runner before Google Cloud authentication.

This intentionally reduces the lifetime during which cloud credentials exist.

Accepted run `35406809175` performed:

1. checkout of accepted `main@7eed21f969486bd689b5abf2e9c37ba2462b00bd`;
2. Linux `amd64` Docker build;
3. short-lived WIF authentication;
4. Docker authentication to Artifact Registry through `gcloud`;
5. push with an immutable source-SHA tag;
6. Artifact Registry digest resolution;
7. Cloud Run deployment from the digest rather than from the mutable tag.

Accepted image tag:

`us-east5-docker.pkg.dev/efactura-demo-0916-9b93/efactura/webapi:sha-7eed21f969486bd689b5abf2e9c37ba2462b00bd`

Accepted immutable Artifact Registry digest:

`sha256:0fec87aa1700f24c3f54682f2ab5940af0782fcdf1ebae9629ba05b8fb3c77ac`

Cloud Run was deployed from:

`us-east5-docker.pkg.dev/efactura-demo-0916-9b93/efactura/webapi@sha256:0fec87aa1700f24c3f54682f2ab5940af0782fcdf1ebae9629ba05b8fb3c77ac`

## 7. Canary deployment strategy

Before deployment, the workflow captures:

- the revision currently receiving 100% of service traffic;
- the stable public service URL.

For the accepted D2.2 run the previously accepted production revision was:

`efactura-api-d21-swagger-01`

The new revision was deployed with:

- deterministic revision name `efactura-api-d22-7eed21f-46-1`;
- `Swagger__Enabled=true`;
- runtime service account `efactura-run@efactura-demo-0916-9b93.iam.gserviceaccount.com`;
- zero production traffic;
- traffic tag `d22-46-1`.

Tagged canary URL:

`https://d22-46-1---efactura-api-yblnutgx3q-ul.a.run.app`

Cloud Run confirmed the candidate was serving `0 percent of traffic` before acceptance.

## 8. Canary acceptance

The tagged zero-traffic revision passed the complete acceptance battery:

- `/swagger` final HTTP `200` after canonical redirect handling;
- `/swagger/v1/swagger.json` HTTP `200`;
- OpenAPI payload structurally valid;
- unauthenticated `GET /api/v1/parties` HTTP `401`;
- authenticated `GET /api/v1/parties` with a short-lived locally generated JWT HTTP `200`;
- the authenticated path reached the existing Neon-backed runtime successfully;
- current JWT signing material was not present in the generated OpenAPI output.

Accepted canary result:

`PASS`

The JWT smoke token is generated only inside the runner using the repository-configured issuer/audience and minimum `parties.read` permission.

## 9. Promotion

Only after the canary passed did the workflow assign production traffic to the candidate:

`efactura-api-d22-7eed21f-46-1=100`

Stable public service URL:

`https://efactura-api-yblnutgx3q-ul.a.run.app`

Cloud Run traffic immediately after promotion:

- `100%` → `efactura-api-d22-7eed21f-46-1`;
- `0%` → `efactura-api-d21-swagger-01`.

## 10. Public post-promotion acceptance

The same acceptance battery was repeated against the stable service URL after promotion:

- public Swagger final HTTP `200`;
- public OpenAPI HTTP `200`;
- public protected parties endpoint without JWT HTTP `401`;
- public protected parties endpoint with valid JWT + Neon HTTP `200`.

Accepted public smoke result:

`PASS`

## 11. Automatic rollback boundary

A pre-promotion failure requires no traffic rollback because the candidate remains at zero production traffic.

If the post-promotion public smoke fails, the workflow is designed to:

1. obtain fresh short-lived Google credentials;
2. restore 100% traffic to the revision captured before promotion;
3. fail the deployment explicitly.

For accepted run `35406809175`, the rollback target captured before deployment was:

`efactura-api-d21-swagger-01`

The rollback steps were correctly skipped because post-promotion public acceptance succeeded. Therefore D2.2 validates the rollback decision path and retained rollback target, while no destructive rollback was necessary during the successful acceptance run.

A future post-promotion failure must exercise the rollback branch automatically and its traffic restoration must be verified from that failing run.

## 12. Evidence emitted by the workflow

The accepted run wrote a GitHub Actions job summary containing:

- source commit `7eed21f969486bd689b5abf2e9c37ba2462b00bd`;
- source-SHA image tag;
- immutable Artifact Registry digest `sha256:0fec87aa1700f24c3f54682f2ab5940af0782fcdf1ebae9629ba05b8fb3c77ac`;
- accepted Cloud Run revision `efactura-api-d22-7eed21f-46-1`;
- previous rollback revision `efactura-api-d21-swagger-01`;
- canary smoke `PASS`;
- public post-promotion smoke `PASS`;
- final Cloud Run service state;
- final revision image/digest state.

No secret value is written to the summary.

## 13. Workflow governance

Changes to `.github/workflows/deploy-api-demo.yml` participate in the normal backend governance path.

The accepted hotfix was validated by Clean Architecture Guard #551 on exact head:

`ebdb495a69ca2ea83494c0ecf37852fcc98ebaa6`

Guard run id:

`35403102577`

Result:

`SUCCESS`

PR #142 was merged only after explicit human authorization. The resulting merge/source commit is:

`7eed21f969486bd689b5abf2e9c37ba2462b00bd`

The deployment workflow itself runs only from accepted `main`, so opening or updating a PR cannot deploy to Cloud Run.

## 14. Failure history and corrected root cause

The first deployment after implementation PR #139 was workflow run #15 / run id `35298178478` and failed before creating any jobs.

No Docker build, WIF authentication, Artifact Registry push, Cloud Run revision, traffic change or smoke test occurred in that failed run.

Root cause was YAML structure: the two embedded Python heredocs used to generate JWT smoke tokens were outside the `run: |` block indentation.

PR #142 corrected only that indentation. No deployment behavior, IAM contract, WebApi behavior, persistence, domain logic or WebApp behavior changed.

The successful run #46 proves the corrected workflow is syntactically executable and operational end to end.

## 15. D2.2 acceptance result

All D2.2 closure conditions are satisfied:

1. exact-head Clean Architecture Guard success before merge;
2. explicit human merge approval;
3. merge-triggered workflow execution on accepted `main`;
4. successful GitHub OIDC → Google WIF authentication;
5. successful Linux image build and Artifact Registry push;
6. immutable digest resolution;
7. successful zero-traffic canary deployment;
8. successful canary Swagger/OpenAPI/401/JWT+Neon smoke;
9. successful promotion to 100% traffic;
10. successful public post-promotion smoke;
11. retained deterministic rollback target and automatic rollback branch;
12. final deployment evidence captured without secret leakage.

Final state:

`D2.2: CLOSED / AUTOMATED / VERIFIED`

Next backend increment:

`D2.4 — Backend operational polish and closure evidence`

D2.3 remains owned by the parallel WebApp/UI lane.
