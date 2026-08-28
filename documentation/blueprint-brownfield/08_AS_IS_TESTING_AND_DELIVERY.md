# AS-IS Testing and Delivery

## Verification results

The prior inspection executed the solution commands against the AS-IS baseline:

- `dotnet restore`: **SUCCESS**
- `dotnet build`: **SUCCESS WITH WARNINGS**
- `dotnet test`: **FAILED**

Test result:

- passed: **20**
- failed: **1**
- skipped: **0**
- total: **21**

Failing test:

`ApplicationCore.Tests.Services.DepartmentServiceTests.GetById_Returns_Department_Successfully`

Observed failure: `Assert.NotNull()` received null at the reported assertion line. The Brownfield inspection does not repair this test.

## Test inventory

Observed test files include:

- `ContactTypeServiceTests.cs`
- `CountryServiceTests.cs`
- `DepartmentServiceTests.cs`
- `UnitTest1.cs` placeholder

Framework/tooling: xUnit, Moq, Microsoft.NET.Test.Sdk, coverlet collector.

## Coverage assessment

`PARTIALLY_CONFIRMED`: test evidence covers only a small subset of the service surface. The weakness is not inferred from “21 tests vs 24 entities”; it is evidenced by the narrow subject set and absence of observed suites for major system boundaries.

No dedicated suites were observed for:

- architecture fitness;
- HTTP/API integration;
- authorization/security;
- persistence integration;
- PostgreSQL/MySQL parity;
- OpenAPI contract;
- end-to-end business lifecycles;
- fiscal behavior;
- smoke deployment.

Some of these capabilities do not exist yet, so their tests are future target work rather than regression omissions.

## Docker

`Dockerfile` uses `.NET 6` SDK/runtime images while application projects target `net8.0`.

Status: `GAP / runtime-container drift`.

## CI/CD

### Azure Pipelines

`azure-pipelines.yml` performs NuGet restore, VSBuild and VSTest on `main`. It is a real basic pipeline, but does not demonstrate current .NET 8/Linux container parity, security scanning, dual-database testing or deployment evidence.

### GitLab CI

`.gitlab-ci.yml` defines build/test/deploy stages whose scripts only `echo` placeholder messages.

Status: placeholder, not effective CI/CD evidence.

### GitHub Actions

No canonical GitHub Actions workflow was identified in the inspected baseline.

## Delivery risks

- Docker/runtime mismatch;
- one existing test failing;
- no observed architecture/API/security integration gates;
- CI definitions reflect mixed historical toolchains;
- README/setup instructions reference outdated database/runtime assumptions.
