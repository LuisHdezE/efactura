# D2.2 — Reproducible API Deployment Workflow

Status: IMPLEMENTATION READY / CI PENDING

Governed branch:

`deployment/d2-2-reproducible-api-deploy`

Accepted base at implementation start:

`main@bfbc0810fc69d28b1fa46e9830e93be7ee307dcd`

## 1. Purpose

D2.2 converts the successful manual D2.1 Cloud Run sequence into a governed, repeatable GitHub Actions deployment path for the WebApi.

The workflow is intentionally separate from the existing WebApp FTP workflow. It does not deploy or modify the React WebApp.

## 2. Deployment trigger

The new workflow is:

`.github/workflows/deploy-api-demo.yml`

It may run:

- automatically after accepted backend/deployment changes are merged to `main`;
- manually through `workflow_dispatch`, but the job itself only executes when the selected ref is `refs/heads/main`.

Path filters intentionally cover the WebApi and its backend dependency projects, Docker build inputs, and the deployment workflow itself.

## 3. Authentication model

D2.2 does not use a Google service-account JSON key.

GitHub Actions authenticates with Google Cloud using OIDC + Workload Identity Federation.

Provider:

`projects/195831190862/locations/global/workloadIdentityPools/github-actions/providers/efactura-main`

Deployment service account:

`efactura-deploy@efactura-demo-0916-9b93.iam.gserviceaccount.com`

Runtime service account remains:

`efactura-run@efactura-demo-0916-9b93.iam.gserviceaccount.com`

The WIF provider is restricted to:

- GitHub repository owner id `88979457`;
- GitHub repository id `893259774`;
- ref `refs/heads/main`.

This prevents a branch or pull-request context from receiving the deployment identity.

## 4. Deployment service-account permissions

The deployment identity is intentionally distinct from the runtime identity.

Its current bounded responsibilities are:

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
- adds `gha-creds-*.json` to both `.gitignore` and `.dockerignore`.

The PostgreSQL connection string is not read by the deployment workflow. Cloud Run continues to receive it through the existing Secret Manager runtime binding.

## 6. Build strategy

The container is built locally on the GitHub-hosted runner before Google Cloud authentication.

This intentionally reduces the lifetime during which cloud credentials exist.

After the local build:

1. GitHub obtains short-lived WIF credentials.
2. Docker authenticates to Artifact Registry through `gcloud`.
3. The image is pushed with a source-SHA tag.
4. Artifact Registry is queried for the immutable digest.
5. Cloud Run is deployed from the digest, not from a mutable tag.

## 7. Canary deployment strategy

Before deployment, the workflow captures:

- the revision currently receiving 100% of service traffic;
- the stable public service URL.

The new revision is then deployed with:

- a deterministic D2.2 revision suffix;
- `Swagger__Enabled=true`;
- the existing runtime service account;
- zero production traffic;
- a unique Cloud Run traffic tag that exposes a canary URL.

This follows the same accepted rollout model proven manually in D2.1.

## 8. Canary acceptance

The tagged zero-traffic revision must pass all of the following before promotion:

- Swagger UI follows its canonical redirect and finishes HTTP 200;
- `/swagger/v1/swagger.json` returns HTTP 200;
- the OpenAPI payload is structurally valid;
- unauthenticated `GET /api/v1/parties` returns HTTP 401;
- the generated OpenAPI payload does not contain the current JWT signing material;
- authenticated `GET /api/v1/parties` returns HTTP 200 against the existing Neon-backed runtime.

The authenticated smoke token is generated locally inside the runner with the repository-configured issuer/audience and the minimum `parties.read` permission.

## 9. Promotion

Only after the canary passes does the workflow assign 100% of service traffic to the new revision.

The stable public service URL is then tested again with the same acceptance set:

- Swagger final HTTP 200 after redirect;
- OpenAPI HTTP 200;
- protected endpoint HTTP 401 without JWT;
- protected endpoint HTTP 200 with valid JWT + Neon.

## 10. Automatic rollback boundary

A pre-promotion failure does not require rollback because the candidate revision still has zero production traffic.

If the post-promotion public smoke fails:

1. the workflow obtains fresh short-lived Google credentials;
2. traffic is returned to the previously captured 100% revision;
3. the workflow fails explicitly.

Rollback changes traffic only. It does not recreate Cloud Run or rebuild infrastructure.

## 11. Evidence

On successful deployment the workflow writes a GitHub Actions job summary containing:

- source commit;
- image tag;
- immutable Artifact Registry digest;
- accepted Cloud Run revision;
- previous rollback revision;
- canary smoke result;
- public post-promotion smoke result;
- final Cloud Run service state;
- final revision image/digest state.

No secret value is written to the summary.

## 12. Workflow governance

Changes to `.github/workflows/deploy-api-demo.yml` are added to the path filters of the existing Clean Architecture Guard.

Therefore this D2.2 PR and future deployment-workflow changes require the normal exact-head backend guard before human merge approval.

The deployment workflow itself runs only after merge to `main`, so opening or updating a PR cannot deploy to Cloud Run.

## 13. D2.2 acceptance model

D2.2 is not closed merely because the workflow YAML exists.

Final acceptance requires:

1. exact-head Clean Architecture Guard success on the implementation PR;
2. explicit human merge approval;
3. the merge-triggered `Deploy eFactura API Demo` workflow to authenticate through WIF;
4. successful image push and immutable digest resolution;
5. successful zero-traffic canary revision;
6. successful canary smoke;
7. promotion to 100%;
8. successful public post-promotion smoke;
9. final deployed revision/image/run evidence reconciled into documentation.

Until that first automated deployment succeeds, D2.2 remains implementation-ready rather than closed.
