# AS-IS Architecture

## Declared architecture

`README.md` describes a Clean Architecture style in which:

- ApplicationCore is the business/core layer and should be independent;
- Infrastructure implements persistence/external concerns;
- Shared contains reusable shared concerns;
- WebApi is the presentation/composition boundary;
- controllers should delegate business logic to services and services to repositories.

## Observed project dependency graph

```text
ApplicationCore

Shared
  -> ApplicationCore

Infrastructure
  -> ApplicationCore
  -> Shared

WebApi
  -> ApplicationCore
  -> Infrastructure
  -> Shared

UnitTest
  -> ApplicationCore
```

`OBSERVED`: there is no project-reference cycle.

## Implementation dependency drift

Although ApplicationCore has no project references, it directly references framework packages including EF Core and ASP.NET HTTP abstractions. Therefore assembly-level project direction is cleaner than namespace/package-level dependency purity.

`Shared` also contains Auth0/JWT/Serilog/ApplicationInsights/Redis/ASP.NET packages and references ApplicationCore. This does not match the README implication that Shared is a neutral layer Core itself can consume.

## Runtime path

```text
HTTP request
  -> WebApi Controller
  -> ApplicationCore service interface/implementation
  -> ApplicationCore repository interface
  -> Infrastructure repository implementation
  -> Dapper/Npgsql and/or EF Core/PostgreSQL
```

`WebApi/Program.cs` acts as the composition root and wires concrete repository implementations.

## Observed architectural components

| Concern | Observed implementation | Classification |
|---|---|---|
| HTTP entry | ASP.NET controllers | OBSERVED |
| Composition | `Program.cs` DI | OBSERVED |
| Application services | ApplicationCore services by entity | OBSERVED |
| Repository ports | ApplicationCore interfaces | OBSERVED |
| Repository implementations | Infrastructure repositories | OBSERVED |
| Data access | Dapper + Npgsql; EF Core DBContext | OBSERVED |
| Cache | Redis distributed cache abstraction/service | OBSERVED configured |
| Mapping | AutoMapper | OBSERVED configured |
| Error handling | global `ApiGlobalExceptionHandlerAttribute` | OBSERVED |
| Technical logging | Serilog file + Application Insights | OBSERVED |
| Business audit | no durable audit mechanism observed | OBSERVED absence-of-evidence statement |
| External fiscal integration | not implemented | OBSERVED |

## Service behavior sample

`CustomerService` is a direct pass-through to `ICustomerRepository` for create/delete/get/update operations. It injects `ICacheService` but does not use it in the inspected implementation. This confirms pass-through behavior **for the sampled service**, not for every service in the solution.

## Data access observations

Repositories inspected use Dapper with parameterized values and fixed table/column identifiers. This is better than string-concatenating user values, but a complete SQL-security assessment still requires reviewing generic query builders and every dynamic identifier path.

Some repositories create `NpgsqlConnection` objects directly. Provider choice therefore currently leaks into Infrastructure implementations and composition, which is acceptable AS-IS but incompatible with the later dual-provider target until refactored.

## Architecture conformance matrix

| Declared rule | Observed | Conformance |
|---|---|---|
| ApplicationCore independent from Infrastructure project | no project reference to Infrastructure | ALIGNED |
| ApplicationCore framework-agnostic business core | EF Core + ASP.NET abstractions referenced | GAP |
| Infrastructure implements data access | repositories/DBContext live in Infrastructure | ALIGNED |
| WebApi acts as entry/composition boundary | controllers + DI in WebApi | ALIGNED/PARTIAL |
| Controllers use business services | generally yes in exposed CRUD controllers | ALIGNED |
| Services contain business logic | sampled CustomerService is pass-through; broad domain logic not observed | PARTIAL/UNKNOWN |
| Shared is neutral/reusable shared concern | technical/web/security packages concentrated there | PARTIAL/GAP |
| REST-oriented controller conventions | multiple action-named/query/body-delete routes diverge | PARTIAL/GAP |

## Conclusion

The repository has a useful layered skeleton, but its current state is a **partial Clean Architecture implementation** rather than a conformant finished architecture. The target should evolve this structure rather than rewrite the stack.
