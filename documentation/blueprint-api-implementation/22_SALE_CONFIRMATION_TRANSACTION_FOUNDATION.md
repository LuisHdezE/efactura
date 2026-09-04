# 22. Sale confirmation transaction foundation

## Purpose

This slice closes the internal local transaction required by `API-SAL-007 confirmSale` before the HTTP route is exposed.

The confirmation transaction consumes only authoritative server-side evidence and commits the commercial state transition plus all local financial, inventory, fiscal-workflow and reliability effects atomically.

## Canonical local sequence

1. authorize `sales.confirm` and organization/location/terminal scope;
2. reserve idempotency before business mutation;
3. load the authoritative Sale and enforce expected version + `VALIDATED` state;
4. recompute tax treatment, tax rate, CFE eligibility/selection and inventory evidence from server-owned sources;
5. create the deterministic `SaleConfirmationPlan` using the stored validation fingerprint;
6. resolve authoritative PaymentMethod evidence and create the deterministic `SaleSettlementPlan`;
7. stage Payment and/or Receivable effects;
8. consume only tracked inventory through `SaleStockConsumer` using frozen quantity/version evidence;
9. create one immutable pending `FiscalizationRequest`;
10. transition Sale `VALIDATED -> CONFIRMED` and persist confirmation/settlement fingerprints;
11. append audit + `SaleConfirmed` and `FiscalizationRequested` outbox messages;
12. complete idempotency and flush all local effects inside the same `ITransactionManager` boundary.

Any failure rolls back the idempotency reservation and every staged business/reliability effect.

## Confirmed Sale invariant

`SaleStatus.Confirmed` is an irreversible commercial snapshot in Release 1. A confirmed Sale carries:

- the prior validation fingerprint;
- confirmation fingerprint;
- settlement fingerprint;
- confirmation timestamp;
- incremented optimistic version.

`ReplaceDraft` and `MarkValidated` fail closed with `sales.confirmed_immutable` once confirmation succeeds.

## CFE 25.2 integration seam fixed

The production CFE eligibility pack previously identified its format as `CFE-25.2`, while the authoritative arithmetic catalog and confirmation planner use canonical format version `25.2`.

The transaction slice normalizes the eligibility pack to `25.2`. This is not a regulatory rule change. It removes an internal identifier mismatch so the real eligibility/selection output can be consumed by the already accepted CFE 25.2 arithmetic boundary.

## Fiscalization boundary

The transaction creates durable `FiscalizationRequest(PENDING)` state only.

It deliberately does not:

- allocate or reserve CAE;
- create FiscalDocument identity;
- generate CFE XML;
- access certificate/private-key material;
- sign XML;
- call DGI or a transport provider;
- mark fiscalization accepted/rejected.

Those effects remain in the later retryable fiscalization workflow and must never hold the commercial confirmation transaction open.

## Public API gate remains closed

This slice intentionally does **not** add `POST /api/v1/sales/{saleId}/confirm` to `SalesController`.

The Blueprint requires the transaction to be proven first with dual-provider rollback, idempotency and concurrency evidence. Only after that evidence is accepted may `API-SAL-007` be exposed as a small transport-layer slice.

## Acceptance evidence target

The PR must pass:

- Release build and NuGet vulnerability gate;
- Architecture tests proving transaction ownership and the closed CAE/XML/transport boundary;
- CrossCutting tests proving confirmed-state immutability and canonical CFE 25.2 format alignment;
- PostgreSQL 16 and MySQL 8.4 persistence tests proving:
  - immediate-payment confirmation commits every local effect;
  - replay returns the original confirmation without duplicate effects;
  - credit confirmation creates one server-derived Receivable;
  - stale Sale version creates no local confirmation effects;
  - failure after the final flush rolls back Sale, Finance, stock, FiscalizationRequest, audit, outbox and idempotency together.

CI evidence will be recorded on the final exact PR head before owner review.
