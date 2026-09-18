# D2.1 — Public Swagger and Controlled Second API Revision

Status: CLOSED / DEPLOYED / VERIFIED

Pull request: `#133 — feat(d2): make demo Swagger exposure configuration-driven`

Governed branch:

`deployment/d2-repeatable-demo-integration`

Final accepted governance evidence:

- PR head: `724936e84d5dfc67afb929b1430d348e34583777`;
- PR base at merge: `ede0475dda758097dbdea74426ecd955492fff0d`;
- Clean Architecture Guard: run #534, workflow run id `35180211947`, conclusion `success`;
- merge commit: `8cbc896d5be7a8ff9d3ebaf0e84bd3ca56580360`.

The parallel UI lane advanced `main` after this deployment. The D2.1 runtime evidence remains bound to the immutable image and Cloud Run revision recorded below.

## 1. Purpose

D2.1 is the first controlled post-D1.2 backend increment.

It has two linked goals:

1. expose Swagger/OpenAPI deliberately in the public demo runtime without running the whole application as `Development`;
2. use that bounded change to prove that the already-created Google Cloud infrastructure can receive a second WebApi revision without recreating Cloud Run, Artifact Registry, Secret Manager, service accounts or Neon.

## 2. Problem before D2.1

Swagger services and Bearer metadata were already registered, but the middleware was hosted inside:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(...);
}
```

The accepted Cloud Run runtime uses:

`ASPNETCORE_ENVIRONMENT=Production`

Therefore the first accepted public revision could serve API endpoints but could not expose the Swagger UI or generated OpenAPI document.

Changing the entire runtime environment to `Development` was rejected because it would couple a demo-documentation requirement to broader Development behavior, including the developer exception page.

## 3. Accepted design

Swagger exposure becomes configuration-driven while preserving secure defaults.

Version-controlled default:

```json
"Swagger": {
  "Enabled": false
}
```

Runtime decision:

```csharp
var swaggerEnabled = app.Environment.IsDevelopment()
    || app.Configuration.GetValue<bool>("Swagger:Enabled");
```

Developer exception behavior remains separate:

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
```

Swagger middleware is activated only when the explicit runtime decision is true:

```csharp
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        builder.Configuration["App:Name"]));
}
```

## 4. Demo runtime configuration

The deployed second revision explicitly receives:

`Swagger__Enabled=true`

This setting is non-secret configuration.

No JWT signing key, database connection string or other secret is stored in source control as part of D2.1.

## 5. Files changed by PR #133

Runtime/configuration:

- `src/WebApi/Program.cs`
- `src/WebApi/appsettings.json`

Governance/documentation:

- `documentation/TECHNICAL_SOLUTION_CURRENT.md`
- `documentation/deployment/D2_REPEATABLE_DEPLOYMENT_DEMO_INTEGRATION.md`
- this D2.1 implementation record

Parallel UI changes are not part of this increment. If `main` advances because of the UI lane, the D2.1 branch is reconciled onto the new base while preserving this bounded five-file backend/documentation diff.

## 6. Security properties preserved

D2.1 does not change:

- JWT validation parameters;
- permission-based application authorization;
- protected endpoint policies;
- Cloud Run platform IAM/public invocation setting;
- Neon credentials;
- Secret Manager secret values;
- service-account secret scope.

Swagger Bearer metadata remains client/documentation metadata only. Protected endpoints continue to rely on runtime authentication and authorization middleware.

Production-like deployments that do not set `Swagger__Enabled=true` keep Swagger disabled by default.

## 7. Infrastructure reuse rule

D2.1 SHALL reuse the existing accepted infrastructure:

- Google Cloud project `efactura-demo-0916-9b93`;
- Artifact Registry repository `efactura` in `us-east5`;
- Cloud Run service `efactura-api`;
- Cloud Run service account `efactura-run@efactura-demo-0916-9b93.iam.gserviceaccount.com`;
- Google Secret Manager bindings;
- Neon PostgreSQL project/database.

The second Cloud Run revision is `efactura-api-d21-swagger-01`. No second Cloud Run service was created.

## 8. Required CI acceptance before merge

The exact final PR head must pass the governed Clean Architecture Guard, including the applicable:

- restore/build path;
- NuGet vulnerability gate;
- architecture tests;
- API v1 cross-cutting tests;
- legacy unit tests;
- provider-real PostgreSQL/MySQL transaction suite.

PR creation is not merge authorization.

If the branch is reconciled after any CI run because `main` advanced, that earlier run becomes historical/preliminary evidence only. A new Guard on the new exact head is required.

## 9. Post-deployment acceptance evidence

D2.1 post-deployment acceptance is complete.

Image publication:

- Artifact Registry tag: `us-east5-docker.pkg.dev/efactura-demo-0916-9b93/efactura/webapi:d21-swagger`;
- OCI image-index digest: `sha256:3113a175da3929a8ca3c0e3fcc18447ac9e6ef62a5bccdb08d6b491feec5234d`;
- resolved `linux/amd64` application manifest digest: `sha256:b45a2e5d7edc15f40ea4a09b34812245853bc1273e99c00d1ad7c378a8a0f4b4`;
- OCI attestation manifest observed in the index: `sha256:b2d5734df67392dbea3da3546c0a6e8df18418f13ee864d52c25852df1535f77`.

The distinction between the index digest and the platform manifest digest is intentional: Artifact Registry reports the immutable OCI index as `3113a175...`, while Cloud Run resolves and executes the `linux/amd64` manifest `b45a2e5d...`.

Cloud Run:

- service: `efactura-api`;
- region: `us-east5`;
- revision: `efactura-api-d21-swagger-01`;
- tagged canary URL: `https://d21-swagger---efactura-api-yblnutgx3q-ul.a.run.app`;
- current public service URL: `https://efactura-api-yblnutgx3q-ul.a.run.app`;
- final traffic: `100%` to `efactura-api-d21-swagger-01`.

Canary smoke tests before promotion:

- Swagger UI: HTTP 200;
- OpenAPI JSON: HTTP 200;
- `GET /api/v1/parties` without JWT: HTTP 401;
- `GET /api/v1/parties` with locally generated valid JWT and Neon-backed runtime: HTTP 200.

Public service smoke tests after promotion:

- `GET /swagger`: HTTP 301 to `swagger/index.html`, followed to HTTP 200 at `/swagger/index.html`;
- `GET /swagger/v1/swagger.json`: HTTP 200;
- `GET /api/v1/parties` without JWT: HTTP 401;
- `GET /api/v1/parties` with locally generated valid JWT and Neon-backed runtime: HTTP 200.

No secret value, token, connection string or signing key is recorded in this evidence.

## 10. Rollback expectation

If the second revision fails acceptance, traffic should be returned to the prior known-good revision rather than recreating infrastructure.

Prior known-good revision at D2.1 opening:

`efactura-api-00001-b8c`

The prior known-good revision remains available for rollback. The operational rollback target is `efactura-api-00001-b8c`; D2.2/D2.4 will codify the repeatable rollback command/workflow rather than recreate infrastructure.

## 11. Parallel UI boundary

D2.1 is backend-only.

It does not integrate the React WebApp with the API and does not alter WebApp views.

D2.3 remains:

`BLOCKED_BY_PARALLEL_UI_LANE`

Public Swagger/OpenAPI is the intended demonstrable API surface and future contract handoff to that lane.

## 12. Evidence ledger

Accepted D2.1 evidence:

- PR #133 merged;
- final PR head: `724936e84d5dfc67afb929b1430d348e34583777`;
- final PR base: `ede0475dda758097dbdea74426ecd955492fff0d`;
- Clean Architecture Guard #534 / run id `35180211947`: SUCCESS;
- merge commit: `8cbc896d5be7a8ff9d3ebaf0e84bd3ca56580360`;
- Artifact Registry tag: `d21-swagger`;
- immutable OCI index digest: `sha256:3113a175da3929a8ca3c0e3fcc18447ac9e6ef62a5bccdb08d6b491feec5234d`;
- Cloud Run-resolved `linux/amd64` manifest: `sha256:b45a2e5d7edc15f40ea4a09b34812245853bc1273e99c00d1ad7c378a8a0f4b4`;
- Cloud Run revision: `efactura-api-d21-swagger-01`;
- final traffic: 100% to the D2.1 revision;
- canary acceptance: Swagger 200, OpenAPI 200, protected endpoint 401 without JWT and 200 with JWT + Neon;
- public-main acceptance: Swagger 301 -> 200, OpenAPI 200, protected endpoint 401 without JWT and 200 with JWT + Neon;
- prior rollback revision retained: `efactura-api-00001-b8c`.

D2.1 is therefore closed. D2.2 is the next backend increment.
