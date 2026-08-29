# Clean Architecture Implementation Conformance Requirement

## Status

`MANDATORY / HUMAN-CONFIRMED PROJECT REQUIREMENT`

Clean Architecture is a contractual implementation requirement for eFactura, not merely a preferred organization style.

The accepted architecture remains the authority for the exact project/module boundaries. API Implementation and API QA must demonstrate implementation conformance independently from architecture design acceptance.

## Core dependency rule

Dependencies point inward.

```text
WebApi / Delivery
        |
        v
Application
        |
        v
Domain

Infrastructure ----> Application / Domain ports
```

The Domain must not reference WebApi, Infrastructure, EF Core, Dapper, ASP.NET Core, Serilog, Application Insights, Npgsql, MySQL providers, Redis clients, Azure SDKs or transport/persistence DTOs.

Application/use-case code may depend on Domain and inward-owned abstractions/ports, but must not depend on concrete persistence, telemetry, fiscal-provider, HTTP or cloud implementations.

Infrastructure implements ports owned by the inward layers. WebApi is a delivery/composition adapter and may wire implementations but does not own business rules.

## Mandatory implementation evidence

API Implementation cannot be considered complete merely because endpoints work. It must produce automated evidence that at minimum proves:

1. Domain has no references to ASP.NET Core, EF Core, Dapper, Serilog/Application Insights, database providers, Redis, Azure SDKs or WebApi/Infrastructure projects.
2. Application does not reference WebApi or concrete Infrastructure implementations.
3. Infrastructure references inward contracts and contains provider-specific persistence/integration/observability adapters.
4. WebApi contains HTTP/auth/composition concerns and delegates business decisions to Application use cases.
5. DTOs exposed by HTTP are not EF entities or provider models.
6. DGI/provider, PostgreSQL/MySQL, logging and telemetry implementations are replaceable through inward-owned abstractions.
7. Domain/application tests can execute without starting ASP.NET, connecting to a real database or initializing telemetry.
8. architecture tests fail the build when a prohibited dependency is introduced.

## Brownfield migration rule

Existing working behavior is migrated incrementally. Clean Architecture conformance does not authorize a destructive rewrite of legacy endpoints.

Legacy controllers may temporarily act as compatibility adapters over new Application use cases. Existing persistence code may coexist during migration, but new v1 implementation must not deepen the existing dependency violations.

Any legacy dependency exception that must temporarily remain is documented with owner, reason, migration target and removal condition. It is not silently treated as target architecture.

## .NET project boundary target

The implementation must make the dependency graph mechanically enforceable. Project/package structure may evolve from the current Brownfield solution, but the target must provide explicit inward boundaries equivalent to:

- Domain;
- Application;
- Infrastructure;
- WebApi/Presentation;
- tests including architecture tests.

If the existing `ApplicationCore` project is retained during transition, its final target state must not keep EF Core/ASP.NET/provider dependencies merely for backward compatibility. Splitting Domain/Application into dedicated projects is preferred when it makes enforcement clearer and migration safer.

## Cross-cutting capabilities

The following remain outer adapters or application ports and never leak concrete frameworks into Domain:

- PostgreSQL/MySQL persistence;
- Redis/cache;
- Serilog/Application Insights observability;
- durable audit persistence;
- Outbox/Inbox/workers;
- DGI/authorized-provider integration;
- fiscal signing/certificate custody;
- file/artifact storage;
- email/notification adapters;
- Technical Operations Console telemetry backends.

## API QA stop condition

`api.architecture_implementation_conformance` is a hard stop condition.

A functionally correct API must not receive PASS if automated evidence shows a prohibited dependency or if business rules are implemented directly in controllers/infrastructure adapters.

## Human decision provenance

The project owner explicitly reaffirmed before API Implementation that maintaining Clean Architecture is vital because it is a customer/project requirement. This document makes that requirement repository-authoritative for implementation and review.
