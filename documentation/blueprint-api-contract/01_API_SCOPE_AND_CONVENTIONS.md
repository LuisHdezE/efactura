# API v1 Scope and Conventions

## Status

`READY_FOR_REVIEW` for Blueprint `api_contract_design`.

This contract is designed from accepted eFactura Requirements, Interface Scope and Architecture evidence. It is not generated from the current 69 legacy endpoints and it does not authorize implementation until the API Contract gate is explicitly accepted.

Blueprint evaluator: `0.5.1@ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`.

Consumer architecture baseline: `main@c7eebce0826907a619298acc6fcf36aa94846e92`.

## Contract scope

The initial v1 server contract covers authenticated actor/security context; organization/locations/terminals; parties with customer/supplier roles and typed national/foreign fiscal identities; products and services through one commercial-item model; payment media; POS and sale lifecycle; CFE/CAE/CFC; inventory; procurement; CxC/CxP; collections/payments/cash; offline sync; received CFE/XML validation; reporting; audit; alerts/monitoring/configuration/integrations; and accounting-export jobs.

Specialized fiscal families whose field-level rules remain OPEN are not given fake issue endpoints. They are added by a later reviewed contract revision after their specific rule/acceptance evidence is accepted.

## Version boundary

New canonical target: `/api/v1/...`.

Legacy Brownfield routes under `/api/...` remain temporarily available until consumer usage is proven and a reviewed cutover/deprecation plan exists. The legacy namespace is not extended with new fiscal/POS capabilities.

## Transport and representations

- default JSON: `application/json; charset=utf-8`;
- HTTPS required in production;
- ISO-8601 timestamps;
- UTC instants for technical/audit ordering;
- `YYYY-MM-DD` for explicit fiscal/business dates;
- exact decimal numbers for quantities/rates/amounts;
- explicit ISO currency code when an amount can be multi-currency;
- bounded `application/xml` or `multipart/form-data` only for the operations that need fiscal files;
- authorized downloads use concrete XML/PDF/CSV media types as applicable.

## Identifiers

Public identifiers are opaque strings even when a migrated Brownfield aggregate still uses integer/bigint internally. Clients do not infer authorization/sequence from IDs and never generate authoritative fiscal IDs/numbers. Client-generated identities are limited to explicitly client-owned values such as `clientOperationId` and `Idempotency-Key`.

## Pagination and sync cursors

Ordinary collections use `page`, `pageSize`, allow-listed sort and explicit resource filters. Canonical response: `items`, `page`, `pageSize`, `total`. Sync/delta feeds use opaque cursors, never page-number inference.

## Correlation

Client may send `X-Correlation-Id`. If absent/invalid, the server generates one. Every response returns the effective correlation ID and Problem Details includes it.

## Organization context

Protected operations that act inside one company require an effective organization context in addition to permission authorization.

- if the authenticated actor has exactly one allowed company scope, the server may infer it;
- if the actor has more than one allowed company scope, the client sends `X-Organization-Id`;
- `X-Organization-Id` is selection context only and never grants access;
- a requested organization outside the authenticated actor's allowed scopes is rejected;
- server-side Application authorization rechecks the organization scope even when Presentation already validated the header.

This convention avoids embedding organization IDs redundantly into every company-scoped route while preserving explicit multi-company selection.

## Idempotency

Retry-sensitive commands require `Idempotency-Key` according to the operation matrix. Same identity + same material request returns the prior canonical result. Same identity + different material request is `409 Conflict` plus durable audit. Offline additionally uses `deviceId + clientOperationId + material payload hash`.

## Concurrency

Contested mutable resources expose a `version` where relevant. State-changing requests carry an expected version when needed. Stale writes return standardized conflict semantics and never silently overwrite critical fiscal/financial/stock state.

## HTTP success semantics

- `200 OK`: read/synchronous action with representation;
- `201 Created`: synchronously created resource;
- `202 Accepted`: durable asynchronous workflow/job accepted, with status resource;
- `204 No Content`: only where no useful representation exists.

A 202 fiscal workflow result is never equivalent to DGI acceptance.

## Security default

All operations require JWT Bearer authentication plus explicit policy/permission unless the inventory says `PUBLIC`. Public surface is intentionally limited to minimal health/version metadata.

There is no invented API-owned username/password `/login` contract. Token acquisition/refresh/logout remains identity-provider/deployment specific until a separate accepted decision authorizes API-owned credential endpoints. `GET /api/v1/me` resolves the authenticated application context.

## Data minimization

No API response returns JWT/provider secrets, connection strings, certificate private keys/passwords, raw secret configuration, or unrelated PII.

## Server authority

The API is authoritative for permissions/scope, totals/taxes, CFE selection, CAE allocation, fiscal lifecycle, financial balances, stock effects, concurrency and offline reconciliation. UI hints/cached data/client-selected CFE codes never override server rules.

## Specialized fiscal deferral

Architecture supports e-Remito, export documents, e-Resguardo, e-Boleta de Entrada and Venta por Cuenta Ajena. Dedicated public commands remain `DEFERRED_PENDING_RULES` until exact applicability/field/correction/Release-1 rules are accepted. Ordinary fiscal endpoints cannot be used to force these families.

## OpenAPI boundary

Stable `operationId` values are assigned in this phase. The later OpenAPI phase materializes and validates this accepted design. OpenAPI may not silently redesign it.
