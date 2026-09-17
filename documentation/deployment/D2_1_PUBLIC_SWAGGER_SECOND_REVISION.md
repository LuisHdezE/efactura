# D2.1 — Public Swagger and Controlled Second API Revision

Status: IMPLEMENTATION READY / CI IN PROGRESS

Pull request: `#133 — feat(d2): make demo Swagger exposure configuration-driven`

Governed branch:

`deployment/d2-repeatable-demo-integration`

The exact PR head and base are intentionally treated as dynamic while parallel governed work may advance `main`. Only the final reconciled PR head and the Clean Architecture Guard run attached to that exact head count as merge evidence. Final accepted commit/base/run identifiers are appended after the increment is frozen and accepted.

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

After merge and before/while deploying the second revision, the existing Cloud Run demo service will explicitly receive:

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

A second Cloud Run revision is expected. A second Cloud Run service is not.

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

## 9. Required post-deployment acceptance

After merge and second-revision deployment, capture all of the following:

1. new image tag;
2. immutable image digest;
3. new Cloud Run revision name;
4. traffic assignment;
5. `GET /swagger` or canonical Swagger UI redirect returns successfully;
6. `GET /swagger/v1/swagger.json` returns HTTP 200 and valid OpenAPI JSON;
7. generated OpenAPI document contains no secrets;
8. unauthenticated `GET /api/v1/parties` remains HTTP 401;
9. authenticated `GET /api/v1/parties` remains HTTP 200 against Neon;
10. the accepted prior revision remains available as rollback evidence.

## 10. Rollback expectation

If the second revision fails acceptance, traffic should be returned to the prior known-good revision rather than recreating infrastructure.

Prior known-good revision at D2.1 opening:

`efactura-api-00001-b8c`

The exact rollback procedure will be captured with real second-revision evidence during D2.4/backend operational closure.

## 11. Parallel UI boundary

D2.1 is backend-only.

It does not integrate the React WebApp with the API and does not alter WebApp views.

D2.3 remains:

`BLOCKED_BY_PARALLEL_UI_LANE`

Public Swagger/OpenAPI is the intended demonstrable API surface and future contract handoff to that lane.

## 12. Evidence ledger

Verified during implementation:

- PR #133 exists and is governed independently from the parallel UI lane;
- runtime diff remains a 7-line addition in `Program.cs`;
- configuration diff remains a 3-line addition in `appsettings.json`;
- branch reconciliation has preserved exactly the five intended D2.1 files while inheriting newer accepted `main` changes;
- multiple preliminary Guard runs may be superseded when documentation or base reconciliation moves the branch head;
- only the Guard attached to the final frozen PR head may be cited as merge evidence.

To append after final head freeze / acceptance:

- final PR head SHA;
- final base SHA;
- final Clean Architecture Guard number/run id/conclusion;
- merge commit SHA;
- second Cloud Run image tag/digest;
- second Cloud Run revision;
- public Swagger/API smoke-test evidence.
