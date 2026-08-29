# Legacy Dependency Debt Register

## Purpose

Track Brownfield dependencies that are incompatible with the accepted target Clean Architecture without pretending they disappeared when the new v1 foundation was created.

## LEGACY-ARCH-001: ApplicationCore framework/provider dependencies

**Observed project:** `src/ApplicationCore/ApplicationCore.csproj`

Current dependency debt includes, among others:

- `Microsoft.AspNetCore.Http.Abstractions`;
- `Microsoft.EntityFrameworkCore`;
- `Microsoft.Extensions.Configuration.Abstractions`.

This violates the final target in which domain/application business logic is independent from web/persistence/configuration frameworks.

### Migration rule

- no new `/api/v1` domain/application behavior is added to `ApplicationCore` merely because legacy services already live there;
- migrate behavior incrementally into `EFactura.Domain` / `EFactura.Application` as vertical slices are implemented;
- keep legacy controllers/services working until their consumers and compatibility path are understood;
- remove framework-specific concerns from migrated business rules rather than copying the dependency into the new projects.

### Removal condition

This debt can be closed when all target business behavior required from `ApplicationCore` has moved behind the new inward boundaries and remaining compatibility code can be retired or transformed into an outer adapter without breaking accepted legacy behavior.

## LEGACY-ARCH-002: Shared is not an inward core layer

`src/Shared/Shared.csproj` currently references `ApplicationCore` and contains concrete Serilog, Application Insights, Redis, JWT/Auth0 and configuration dependencies.

Therefore `Shared` must not be treated as a clean Domain/Application dependency bucket.

### Migration rule

New Domain/Application code must not reference `Shared`. Reusable cross-cutting abstractions belong inward only when they are framework-neutral; concrete implementations remain outer.

### Removal condition

Shared may remain as legacy/outer support while consumers are migrated. Any future retained Shared project must have an explicitly defined architectural role rather than acting as a miscellaneous dependency bridge.

## LEGACY-ARCH-003: WebApi provider dependencies

`src/WebApi/WebApi.csproj` currently directly references Npgsql and EF Core tooling/provider packages.

The accepted target prefers persistence/provider composition to reside in Infrastructure, with WebApi acting as delivery/composition rather than persistence implementation.

### Migration rule

Do not add MySQL/PostgreSQL business branching to controllers. Provider selection and concrete persistence registration migrate behind Infrastructure composition/extensions.

### Removal condition

WebApi no longer needs concrete database provider runtime packages except any explicitly justified design-time/composition dependency accepted by architecture review.

## Governance

These are `OBSERVED_LEGACY` exceptions, not accepted target patterns.

Every implementation PR touching an exception area must either:

1. reduce the debt;
2. leave it unchanged while adding new code only in the clean boundary; or
3. document why a temporary exception is unavoidable and define its removal condition.

No PR may silently increase this debt while claiming Clean Architecture conformance.
