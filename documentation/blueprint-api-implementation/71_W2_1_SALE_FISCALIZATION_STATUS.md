# 71 — W2.1 Sale fiscalization status

Status: `IMPLEMENTATION_CANDIDATE_PRE_MERGE`

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

## Automated evidence in PR #208

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

The completion-matrix architecture constants are advanced in the same candidate from 64 to 65 implemented public operations and from 128 to 127 `MISSING_HTTP` operations.

## Completion accounting

Candidate branch accounting:

- Wave 2: `23 / 26` implemented, `3` missing HTTP;
- global public v1: `65 / 194` implemented = `33.51%`;
- global missing HTTP: `127`;
- contract-collision IDs: `2`;
- total non-implemented IDs: `129`.

These numbers describe HTTP implementation presence in PR #208 only. They do not claim governed merge, deployment or runtime acceptance.

## Operational impact

Expected schema impact: none.

Expected production migration: none.

Deployment remains the normal API deployment gate after an owner-approved merge. Runtime acceptance for the new endpoint is read-only; any fixture creation needed to exercise `PENDING` or `IDENTITY_CREATED` in production would be a separate protected mutation and is not authorized by this implementation PR.

## Remaining Wave 2 gaps

W2.1 does not alter the prerequisites already recorded for:

- `API-SAL-008 cancelSale`;
- `API-POS-001 getPosBootstrap`;
- `API-PTY-008 getPartyAccountSummary`.

Those remain separate governed increments.
