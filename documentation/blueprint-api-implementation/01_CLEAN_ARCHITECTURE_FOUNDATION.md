# API Implementation Foundation: Clean Architecture

## Status

`api_implementation = IN_PROGRESS`

Implementation starts from accepted API Contract merge `main@bbdb45b4edd95b883ffeefd824054ce53ac49e43`.

Clean Architecture is a mandatory project/customer requirement and is treated as an implementation constraint, not a naming convention.

## Migration strategy

The Brownfield solution currently contains `ApplicationCore`, `Infrastructure`, `Shared` and `WebApi`. `ApplicationCore` still carries framework/provider dependencies that were explicitly documented during Brownfield inspection.

To avoid a destructive rewrite while preventing new v1 code from inheriting those violations, implementation introduces two clean inward projects:

```text
EFactura.Domain
       ^
       |
EFactura.Application
       ^
       |
Infrastructure ---- adapters/providers
       ^
       |
WebApi ----------- delivery/composition
```

`ApplicationCore` remains temporarily as a **legacy compatibility boundary** only. New `/api/v1` domain/application code must be added to the new `Domain` and `Application` projects.

## Project responsibilities

### `src/Domain`

May contain:

- entities/aggregates;
- Value Objects;
- domain events;
- domain policies/services that are pure business logic;
- domain exceptions/result primitives where appropriate.

Must not reference persistence, HTTP, logging, cloud, database, cache or external-provider frameworks.

### `src/Application`

May contain:

- use cases/commands/queries;
- inward-owned ports/interfaces;
- application policies/orchestration;
- application DTOs/results independent from HTTP and persistence;
- authorization/idempotency/audit/outbox abstractions where required by use cases.

May depend on Domain. Must not depend on concrete Infrastructure or WebApi implementations.

### `src/Infrastructure`

Owns concrete adapters for:

- PostgreSQL/MySQL;
- EF Core/Dapper;
- Redis;
- Outbox/Inbox/workers;
- DGI/provider transport;
- signing/artifact storage;
- Serilog/Application Insights telemetry adapters;
- external integrations.

It may temporarily retain legacy dependencies while migration is active, but new implementations must point inward to Domain/Application contracts.

### `src/WebApi`

Owns:

- HTTP routing/controllers/endpoints;
- JWT authentication/policy wiring;
- Problem Details;
- request correlation;
- composition root/DI;
- transport DTO mapping.

Business decisions do not live in controllers.

## Mechanical enforcement

A new `test/ArchitectureTests` project inspects project/package/source boundaries and fails if:

- Domain gains any package or project dependency;
- Application references a project other than Domain;
- Application imports known persistence/web/telemetry/provider dependencies;
- Infrastructure references WebApi;
- WebApi stops referencing the inward Application boundary;
- new Domain/Application source imports known outer frameworks/providers.

These tests are intentionally simple and repository-owned so they do not depend on another architecture-testing package merely to protect the dependency graph.

## Brownfield rule

This foundation does **not** claim full architecture implementation conformance yet. Existing `ApplicationCore` violations remain migration debt and are tracked separately.

The rule for all new work is stricter:

`do not add new v1 business behavior to the legacy dependency direction`.

Legacy routes may remain functional and may later delegate into new Application use cases as compatibility adapters.

## Next implementation slices

After this foundation is accepted, implementation proceeds vertically while preserving the same dependency graph. Initial cross-cutting work should establish correlation/Problem Details/auth context/idempotency/audit ports before high-impact fiscal and financial commands are enabled.

The later Blueprint check `api.architecture_implementation_conformance` cannot PASS until legacy dependency debt required by the target has been removed or explicitly reconciled and the automated architecture suite passes.
