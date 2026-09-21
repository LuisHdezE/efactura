# 71 — W2.1 Sale fiscalization status

Status: `IMPLEMENTED / MERGED / CI_ACCEPTED / DEPLOYED / RUNTIME_ACCEPTED_READ_ONLY / CLOSED`

API ID: `API-SAL-009`

Operation ID: `getSaleFiscalizationStatus`

Public surface:

```text
GET /api/v1/sales/{saleId}/fiscalization
permission: sales.read
idempotency: NO
```

Contract authority: `documentation/api-completion-matrix/W2_1_SALE_FISCALIZATION_STATUS_CONTRACT.md`, owner-locked on `main` by PR #207.

Implementation PR: #208.

Operational closure: `documentation/api-completion-matrix/W2_1_SALE_FISCALIZATION_STATUS_RUNTIME_CLOSURE.md`.

## Purpose

Expose the already durable local fiscalization workflow for a Sale without inventing or reinterpreting DGI/provider state.

The implementation is intentionally read-only and composes existing authority:

- `ISaleRepository`;
- `IFiscalizationRequestRepository`;
- `IFiscalDocumentRepository`;
- existing `IActorContextAccessor` Sales authorization semantics.

No new aggregate, persistence model, migration or production write is introduced.

## Application projection

`GetSaleFiscalizationStatusUseCase` returns one of the locked local workflow states:

- `NOT_REQUESTED` for Draft/Validated Sale with no fiscalization request;
- `PENDING` for Confirmed Sale with a matching Pending request and no fiscal identity;
- `IDENTITY_CREATED` for Confirmed Sale with a matching IdentityCreated request and matching immutable local FiscalDocument identity.

The use case fails closed with:

```text
409
code: fiscalization.inconsistent_state
conflictType: inconsistent_state
```

when the durable facts violate the accepted lifecycle, including confirmed-without-request, non-confirmed-with-request, Pending-with-document, and IdentityCreated with missing or mismatched fiscal identity.

Unknown or cross-organization Sale ids remain masked as:

```text
404
code: sales.not_found
```

## HTTP projection

`SaleFiscalizationController` exposes only GET at the governed route and uses `sales.read`.

The DTO carries the local request fields and optional bounded fiscal identity. Every successful response includes:

```text
statusAuthority = LOCAL_WORKFLOW_ONLY_NOT_DGI_ACCEPTANCE
```

No field claims DGI receipt, acceptance, rejection, submission, acknowledgement or provider state.

## Read-only boundary

W2.1 does not use or introduce:

- `ITransactionManager`;
- `IUnitOfWork` / SaveChanges;
- idempotency reservations;
- audit writes;
- outbox writes;
- DGI/provider gateways;
- schema changes or migrations.

The existing EF repositories used by the read path already issue no-tracking reads for fiscal-document lookup.

## Automated evidence

PR #208 included architecture and CrossCutting coverage for the accepted contract.

Architecture coverage protects:

- exact route and `getSaleFiscalizationStatus` operation name;
- `sales.read` permission;
- required authority marker;
- GET-only public surface;
- use of the three existing read repositories;
- absence of transaction/UoW/idempotency/audit/outbox/DGI gateway dependencies.

CrossCutting coverage proves:

- Draft without request -> `NOT_REQUESTED`;
- Validated without request -> `NOT_REQUESTED`;
- Confirmed + Pending -> `PENDING`;
- Confirmed + matching identity -> `IDENTITY_CREATED`;
- confirmed without request -> fail closed;
- non-confirmed with request -> fail closed;
- Pending with preexisting document -> fail closed;
- IdentityCreated without document -> fail closed;
- missing `sales.read` -> forbidden;
- cross-organization Sale -> masked not-found.

Pre-merge Clean Architecture Guard #695 completed SUCCESS on implementation HEAD `186fc617b29f658e804c3fa75a08dfebb93ffbd4`.

PR #208 was merged as `9989c0f15d76423f76e25e1cfdc1ab1337586849`, and post-merge Clean Architecture Guard #696 completed SUCCESS including PostgreSQL/MySQL transactional persistence integration.

## Deployment and runtime evidence

Deploy API Demo #55 completed SUCCESS and promoted Cloud Run revision:

```text
efactura-api-d22-9989c0f-55-1
```

to 100% traffic after canary and public post-promotion smoke.

The one-shot read-only acceptance harness was merged by PR #210 and executed as run `35643849728`, job `106479398308`, with SUCCESS.

Production observations:

- OpenAPI HTTP 200;
- exact GET-only W2.1 surface PASS;
- no JWT -> 401;
- malformed JWT -> 401;
- missing `sales.read` -> 403;
- organization scope escape -> 403 `organization_scope_denied`;
- authenticated unknown Sale -> 404 `sales.not_found`;
- production writes -> NONE;
- fixture creation -> NONE.

A read-only Neon inspection confirmed production had zero rows in `v1_sales`, so successful 200 business-state projections were not exercised in production. Creating a fixture solely for acceptance was intentionally avoided. Those success-state projections remain covered by the accepted automated QA above.

## Completion accounting

W2.1 closes with:

- Wave 2: `23 / 26` implemented, `3` missing HTTP;
- global public v1: `65 / 194` implemented = `33.51%`;
- global missing HTTP: `127`;
- contract-collision IDs: `2`;
- total non-implemented IDs: `129`.

## Operational impact

Schema impact: none.

Production migration: none.

Production business-data writes during runtime acceptance: none.

The temporary one-shot workflow is removed by the W2.1 closure increment after evidence capture.

## Remaining Wave 2 gaps

W2.1 does not alter the prerequisites already recorded for:

- `API-SAL-008 cancelSale`;
- `API-POS-001 getPosBootstrap`;
- `API-PTY-008 getPartyAccountSummary`.

Those remain separate governed increments. The next frontier is W2.2 `API-SAL-008 cancelSale` contract/lifecycle prerequisite closure.
