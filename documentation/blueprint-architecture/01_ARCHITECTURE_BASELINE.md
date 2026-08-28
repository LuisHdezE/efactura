# eFactura Target Architecture Baseline

## Status

`DRAFT / ARCHITECTURE-Security-Data boundary`

This artifact is stacked on `blueprint/target-functional-reconstruction` and does not authorize implementation. It translates approved/draft target requirements into an architectural contract candidate for Blueprint `Architecture / Security / Data Ready`.

Baseline inputs:

- Blueprint Master: `0.5.1@ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`
- eFactura Brownfield baseline: `a6c9bf96572b8a0a88efde2c68b0749a71020a18`
- Target/requirements branch parent: `146f49f6ef341471ae059356a511c1129c814cf5`
- Functional/reference source: `LuisHdezE/FacturacionElectronicaBases` as non-authoritative know-how
- Regulatory authority: current official DGI material captured in `documentation/blueprint-target/`

## Architectural style

The target is an **evolutionary modular monolith using Clean Architecture dependency direction**, implemented within the existing .NET 8 / ASP.NET Core solution rather than by rewriting it.

Governing Brownfield rule:

`ALIGN, DO NOT REWRITE`

Technology preservation:

- .NET 8 and C# remain;
- ASP.NET Core remains the HTTP host;
- EF Core remains the principal write persistence technology;
- Dapper remains available for optimized/read-model queries;
- Redis, Serilog and Application Insights remain valid infrastructure capabilities when applicable;
- PostgreSQL remains supported and MySQL becomes a first-class configurable alternative;
- Swagger/OpenAPI remains the API-description path, hardened later into the authoritative contract;
- no frontend technology is selected by this server architecture boundary.

## Physical solution strategy

The existing projects are preserved during the first target implementation:

```text
WebApi
  -> ApplicationCore
  -> Infrastructure
  -> Shared

Infrastructure
  -> ApplicationCore
  -> Shared (technical cross-cutting only)

Shared
  -> ApplicationCore only where a technical adapter must consume application contracts

ApplicationCore
  -> BCL + explicitly approved pure/domain-safe libraries only
```

### ApplicationCore target responsibility

`ApplicationCore` becomes the inward core and contains:

- domain model;
- aggregates/entities/value objects;
- domain services and policies;
- application use cases/commands/queries;
- ports/interfaces;
- domain/application events;
- authorization intent and business invariants.

It must not depend on:

- ASP.NET Core HTTP abstractions;
- EF Core mapping/runtime;
- Dapper/Npgsql/MySQL provider packages;
- Serilog/Application Insights;
- concrete Redis/client libraries;
- filesystem/blob implementations;
- DGI/provider SDKs.

`NodaTime` may remain if used as a pure temporal-domain dependency. AutoMapper, when retained, is application mapping only and never required by domain entities.

### Infrastructure target responsibility

Concrete adapters:

- EF Core persistence;
- PostgreSQL/MySQL provider selection and migrations;
- Dapper read models;
- Redis;
- document/blob storage;
- fiscal signing/transport adapters;
- email/integration adapters;
- outbox/inbox workers;
- provider-specific implementations.

### WebApi target responsibility

- API v1 endpoints/controllers;
- authentication middleware;
- policy authorization enforcement;
- Problem Details/error mapping;
- idempotency/correlation middleware integration;
- OpenAPI contract exposure;
- composition root/DI;
- no business rules in controllers.

### Shared target responsibility

`Shared` is not a second domain layer. It may contain reusable technical middleware/observability primitives and compatibility helpers. Business entities, fiscal decisions and repository contracts belong to `ApplicationCore`.

## Logical module layout

The preferred in-project structure is:

```text
ApplicationCore/
  SharedKernel/
  Modules/
    Organization/
      Domain/
      Application/
    IdentityAccess/
    Parties/
    Catalog/
    Taxation/
    Sales/
    Fiscal/
    Inventory/
    Procurement/
    Receivables/
    Payables/
    Treasury/
    CashManagement/
    Sync/
    Reporting/
    Audit/

Infrastructure/
  Persistence/
    Common/
    PostgreSql/
    MySql/
  Modules/<Module>/
  Integrations/
  BackgroundJobs/

WebApi/
  Controllers/V1/<Module>/
  Contracts/V1/
  Security/
  Middleware/
```

This is a target ownership layout, not permission for a mass file move. Brownfield code is migrated slice-by-slice behind compatibility boundaries.

## Core architectural invariants

1. Domain decisions are not made in controllers or persistence adapters.
2. Modules own their write model/tables; direct cross-module repository access is forbidden.
3. Cross-module effects use explicit application orchestration, contracts/events and transaction policy.
4. External network calls never occur while holding a long-running database transaction.
5. Fiscal/financial/stock side effects are idempotent and auditable.
6. Technical logs do not substitute for durable business/security audit.
7. Current DGI rule/version used for a fiscal decision is traceable.
8. API clients cannot choose states/CFE types/permissions that bypass server policy.
9. PostgreSQL/MySQL differences are isolated in Infrastructure.
10. Existing API routes are not removed until consumer impact is proven and approved.

## Architecture fitness evidence required during implementation

Blueprint 0.5.1 requires implementation conformance after architecture acceptance. The implementation must therefore add executable tests/checks for at least:

- forbidden ApplicationCore framework/provider references;
- project dependency direction;
- module ownership constraints where practical;
- DI bindings for required ports;
- persistence contract tests on PostgreSQL and MySQL;
- idempotency/concurrency invariants;
- authorization enforcement on protected endpoints;
- no duplicate fiscal numbering under concurrency.

`architecture design acceptance != architecture implementation conformance`.
