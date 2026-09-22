# 72 — W2.2 Sale Cancellation

Status: `IMPLEMENTED_PENDING_MERGE`

API ID: `API-SAL-008`

Operation ID: `cancelSale`

Route: `POST /api/v1/sales/{saleId}/cancel`

Permission: `sales.cancel`

Contract authority: `documentation/api-completion-matrix/W2_2_SALE_CANCELLATION_CONTRACT.md`

Implementation PR: **#216**.

## 1. Scope implemented

W2.2 adds the bounded pre-confirmation cancellation workflow without introducing a reversal workflow.

The implementation adds:

- `SaleStatus.Cancelled = 4` as a persistable terminal Sale state;
- `Sale.MarkCancelled(expectedVersion)` with irreversible-boundary and terminal-state protection;
- `CancelSaleUseCase` with organization/location/terminal authorization, optimistic concurrency and idempotency;
- `SaleCancellationController` with the exact governed route, operationId and permission;
- required `Idempotency-Key` handling and request hashing;
- durable `SALE_CANCELLED` audit evidence;
- durable `SaleCancelledIntegrationEvent` outbox evidence;
- PostgreSQL/MySQL provider-real persistence coverage;
- architecture tests proving the cancellation slice has no Payment, Receivable, inventory, fiscalization, CAE or DGI dependency.

## 2. Allowed lifecycle

The bounded operation allows only:

- `DRAFT -> CANCELLED`;
- `VALIDATED -> CANCELLED`.

`CONFIRMED` remains the hard irreversible boundary and returns the locked conflict `sales.cancellation.irreversible_boundary_crossed`.

`CANCELLED` is terminal. A new command against an already cancelled Sale returns `sales.already_cancelled`; only an already-completed replay with the same idempotency scope/key/hash may return the original successful outcome.

Conflict precedence for a newly acquired command is intentionally:

1. `CONFIRMED` irreversible boundary;
2. `CANCELLED` terminal state;
3. stale `expectedVersion` for Draft/Validated;
4. transition.

This prevents clients from receiving a misleading stale-version hint for a state that can never be cancelled through this endpoint.

## 3. Atomic boundary

First successful cancellation atomically persists:

1. Sale status/version;
2. `SALE_CANCELLED` audit event;
3. one `SaleCancelledIntegrationEvent` outbox record;
4. idempotency completion with outcome `sale_cancelled`, resource type `Sale`, resource id equal to the Sale id.

The successful transition increments `Sale.Version` exactly once.

Validated cancellation preserves `ValidationFingerprint` and `ValidatedAtUtc`. Confirmation evidence remains absent because only pre-confirmation states can transition.

## 4. Explicit exclusions

This use case has no dependencies on and does not mutate:

- Payment;
- Receivable;
- inventory positions or stock movements;
- FiscalizationRequest;
- FiscalDocument;
- CAE allocation/numbering;
- DGI/provider gateways or transport state.

Cancellation after confirmation therefore remains outside W2.2 and requires a later explicit fiscal/accounting reversal workflow such as governed credit/debit-note handling.

## 5. Persistence impact

No migration is introduced.

`v1_sales.Status` is already stored as an integer by `EfSaleRepository`; the new enum value therefore round-trips through the existing provider-neutral persistence shape. No cancellation-specific snapshot column is introduced. Operator reason/context are durable audit metadata, not Sale columns.

## 6. QA evidence in PR #216

Automated coverage includes:

- Draft cancellation;
- Validated cancellation preserving validation evidence;
- terminal immutability;
- Confirmed irreversible-boundary precedence over stale version;
- already-cancelled precedence over stale version;
- stale version for eligible states;
- permission and organization isolation;
- cross-organization not-found masking;
- same-request idempotent replay without duplicate audit/outbox/version increments;
- payload-mismatch and in-progress idempotency conflicts;
- reason/context validation bounds;
- architecture assertions for exact HTTP surface and forbidden reversal dependencies;
- PostgreSQL and MySQL provider-real cancellation round-trip, replay and rollback/no-residue failure behavior.

## 7. Governance state

This document records a branch implementation candidate only. `API-SAL-008` becomes an accepted `main` implementation only after:

- final exact-head Clean Architecture Guard success;
- explicit owner approval of that exact PR HEAD;
- protected merge;
- post-merge CI reconciliation;
- deployment acceptance;
- any production runtime mutation acceptance that the owner separately authorizes.

No production mutation or schema write is authorized by PR #216 itself.
