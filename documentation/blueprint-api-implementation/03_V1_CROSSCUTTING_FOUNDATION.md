# API v1 Cross-cutting Foundation

## Status

`api_implementation = IN_PROGRESS`

This slice implements cross-cutting foundations required by the accepted v1 contract. It does not implement business endpoints and does not claim final `api.architecture_implementation_conformance = PASS`.

Baseline: `main@49023c88e788663bf6baa3283253364c5361dda7`, after human acceptance and merge of the Clean Architecture foundation.

## Clean Architecture placement

### Application-owned abstractions

The new `src/Application` project owns framework-neutral contracts for:

- authenticated actor context;
- correlation context;
- stable permission catalog;
- application-safe problem semantics;
- durable audit writer port;
- idempotency store port;
- outbox writer/integration-event port.

These contracts contain no ASP.NET Core, EF Core, Npgsql, MySQL, Redis, Serilog or Application Insights dependency.

### WebApi-owned delivery adapters

`src/WebApi/CrossCutting` owns HTTP/runtime concerns:

- `X-Correlation-Id` validation/propagation;
- HTTP actor/claim adapter;
- Serilog request/actor enrichment;
- dynamic permission authorization policies;
- RFC 9457 Problem Details mapping for `/api/v1`;
- canonical v1 401/403 responses.

No business rule belongs in these adapters.

### Infrastructure later

Persistence implementations for audit, idempotency and outbox are intentionally not fabricated in this slice. They will implement Application ports in Infrastructure with PostgreSQL/MySQL-compatible persistence and transactional semantics.

## Brownfield compatibility

The legacy `ApiGlobalExceptionHandlerAttribute` remains active for existing routes. It now explicitly bypasses `/api/v1`, allowing the new API to use the accepted RFC 9457 contract without changing legacy response behavior.

No existing controller route is removed or rewritten by this slice.

## Correlation contract

- accepted request header: `X-Correlation-Id`;
- safe inbound IDs are preserved;
- blank, overlong or unsafe IDs are replaced with a server-generated 32-character GUID representation;
- the effective ID is returned in `X-Correlation-Id`;
- trace ID and correlation ID are placed in request context and structured log context;
- future audit/outbox implementations consume the Application correlation context rather than HttpContext.

This prevents CR/LF or arbitrary header content from becoming a log-injection vector.

## Actor and authorization contract

Roles are not authorization shortcuts. The HTTP adapter resolves explicit permissions plus company/location/terminal/device scope claims into `ActorContext`.

Dynamic policies use the prefix `Permission:` and accept only names in the accepted `Permissions` catalog. Unknown permission names fail instead of silently becoming typo-driven authorization policies.

The permission handler proves only the stable application permission. Company/location/terminal/object/domain authorization remains a mandatory second layer in use cases/policies for operations that require scope checks.

## Problem Details contract

For `/api/v1`:

- known `ApplicationProblemException` instances map framework-neutral problem kinds to accepted HTTP statuses;
- responses use `application/problem+json`;
- correlation and trace IDs are included;
- validation errors, conflict type/current version, rule references and retry guidance can flow through the accepted extensions;
- unexpected exceptions are logged server-side and return a sanitized 500 without stack trace, exception class, SQL/provider details or secrets.

The generic unhandled 500 intentionally omits an application `code` rather than inventing a post-contract canonical code without change control.

## Application ports established

### Durable audit

`IAuditWriter` accepts structured accountability events with actor, target, scope, outcome, correlation/causation and safe metadata. It is separate from technical logging.

### Idempotency

`IIdempotencyStore` models atomic reservation, completed replay identity, in-progress detection, request-hash mismatch and safe abandonment before authoritative effects. It does not store HTTP DTOs or EF entities.

### Outbox

`IOutboxWriter` accepts Application-owned integration events plus correlation/causation metadata. Serialization and durable transport remain Infrastructure responsibilities.

## Automated evidence

The Clean Architecture workflow is extended to build the full solution and execute both:

- `ArchitectureTests`;
- `CrossCuttingTests`.

ArchitectureTests also reserve `src/WebApi/Controllers/V1/**` as an inward-facing delivery boundary: future v1 controllers fail CI if they directly reference ApplicationCore, Infrastructure, Shared legacy types, DbContext or provider-specific persistence APIs.

## Explicit non-goals

This slice does not yet:

- implement concrete audit/idempotency/outbox persistence;
- add sales/fiscal controllers;
- change legacy endpoint authorization behavior;
- resolve JWT provider ownership;
- implement object-level authorization rules;
- create OpenAPI output;
- claim API implementation complete.
