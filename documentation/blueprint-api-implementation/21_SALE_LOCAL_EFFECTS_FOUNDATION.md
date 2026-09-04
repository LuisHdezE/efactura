# 21 — Sale Local Effects Foundation

## Purpose

Introduce the remaining durable local effects required before the transactional implementation of `API-SAL-007 confirmSale`:

- authoritative stock consumption for inventory-tracked sale items;
- durable `FiscalizationRequested` workflow state.

This slice deliberately does **not** mark a Sale confirmed and does not expose a new public endpoint. Its role is to give the future outer sale-confirmation transaction durable, provider-neutral primitives that can be committed atomically with the Finance foundation accepted in document 20.

## Canonical transaction boundary

The accepted application architecture defines sale confirmation as a short local transaction that:

1. authenticates/authorizes and claims idempotency;
2. reloads and revalidates the Sale;
3. freezes the commercial/fiscal confirmation snapshot;
4. creates the required payment and/or receivable effects;
5. creates stock effects only for tracked items;
6. creates durable `FiscalizationRequested` state;
7. appends audit/outbox evidence;
8. commits;
9. performs CAE allocation, FiscalDocument creation, XML generation/signing and transport later in the fiscalization workflow.

Accordingly, this foundation does not allocate CAE numbers and does not create a FiscalDocument.

## Stock consumption authority

`StockMovementKind.SaleConsumption` is introduced as a distinct immutable business fact rather than encoding a sale as a generic adjustment reason.

Every sale-consumption movement preserves:

- source Sale ID;
- organization/item/location/position identity;
- quantity before, negative quantity delta and quantity after;
- resulting inventory-position version;
- confirmation SHA-256 fingerprint;
- settlement SHA-256 fingerprint;
- occurrence timestamp.

`InventoryPosition.ConsumeForSale` rejects:

- stale expected versions;
- empty Sale identity;
- zero/negative requested quantity;
- insufficient stock;
- invalid confirmation/settlement fingerprints.

The movement is constructed before the position mutates so a rejected movement cannot leave an in-memory quantity/version change behind.

`SaleStockConsumer` is an application-level staging component. It consumes the exact inventory evidence already frozen by `SaleConfirmationPlan`, reloads each tracked position and requires both quantity and application-managed version to remain unchanged before staging the decrement and immutable movement.

It intentionally does not own:

- transaction begin/commit;
- UnitOfWork flush;
- idempotency;
- audit;
- outbox;
- Sale state transition.

Those responsibilities belong to the future `confirmSale` orchestration boundary.

## Database protection for stock effects

`v1_stock_movements` gains nullable sale-source evidence fields so historical/manual Adjustment rows remain valid.

The database uniquely protects:

`(OrganizationId, SourceSaleId, PositionId)`

for sale-linked rows. This prevents the same Sale from creating a second consumption movement against the same authoritative position even if a future application defect bypasses the outer idempotency layer.

That unique index protects the duplicate movement fact only. It does **not** by itself make an inventory-position update plus movement insert an atomic business operation. The future `confirmSale` use case must execute position mutation, movement append and all companion local effects inside the application-owned `ITransactionManager` boundary. Provider-neutral rollback evidence below validates that explicit transaction contract rather than relying on provider-specific `SaveChanges` behavior.

## Durable FiscalizationRequested work item

`FiscalizationRequest` represents pending fiscal work created only after the commercial/financial/stock prerequisites have been resolved locally.

It preserves:

- organization and source Sale;
- location and terminal context;
- selected CFE family;
- receiver-identification requirement;
- CFE format version;
- confirmation and settlement fingerprints;
- currency;
- authoritative net, VAT and total amounts;
- pending workflow status/version;
- requested timestamp.

Database table:

`v1_fiscalization_requests`

A unique `(OrganizationId, SaleId)` constraint guarantees one base fiscalization work item per Sale.

The work item deliberately contains no CAE authorization/range, series, fiscal number, XML, signature, artifact or transport receipt. Those facts belong to the later Fiscalization workflow and must not be fabricated during Sale confirmation.

## Provider-neutral persistence evidence

The persistence integration contract exercises PostgreSQL 16 and MySQL 8.4 with the same model/migration. It covers:

- sale stock consumption round-trip with quantity/version and source evidence;
- database rejection of a duplicate sale-consumption movement for the same Sale + position, isolated from unrelated position persistence;
- fiscalization-request round-trip and one-work-item-per-Sale uniqueness;
- explicit-transaction rollback of stock position + movement + fiscalization request after a post-flush failure.

The rollback scenario is especially important: these local effects are useful only if a future outer transaction can treat them atomically with Payment/Receivable, Sale state, audit/outbox and idempotency.

## Explicit non-goals

This slice does **not**:

- expose `POST /api/v1/sales/{saleId}/confirm`;
- add `SaleStatus.Confirmed`;
- persist the final immutable Sale confirmation snapshot;
- create Payment/Receivable rows itself;
- own the outer transaction/idempotency/audit/outbox sequence;
- mutate CashManagement;
- allocate or reserve a CAE number;
- create a FiscalDocument;
- generate, validate, sign or store CFE XML;
- call DGI or a fiscal provider;
- process synchronous/asynchronous fiscal responses;
- mark OpenAPI, Postman or API QA gates complete.

## Next slice

After this foundation is accepted, the next slice can implement the complete local `confirmSale` transaction:

`validated Sale + authoritative ConfirmationPlan + SettlementPlan`

→ Payment/Receivable effects

→ tracked-stock consumption

→ durable FiscalizationRequest

→ Sale `CONFIRMED` immutable snapshot/state

→ audit + outbox + idempotency canonical result

→ one atomic local commit.

Only after that transaction is proven with dual-provider rollback/idempotency/concurrency evidence should `API-SAL-007` be exposed publicly.

## Traceability

Primary sources:

- `documentation/blueprint-architecture/03_DOMAIN_AGGREGATES_AND_INVARIANTS.md`;
- `documentation/blueprint-architecture/05_APPLICATION_USE_CASES_AND_PORTS.md`;
- `documentation/blueprint-api-implementation/18_SALE_CONFIRMATION_PLANNING_FOUNDATION.md`;
- `documentation/blueprint-api-implementation/19_SALE_SETTLEMENT_PLANNING_FOUNDATION.md`;
- `documentation/blueprint-api-implementation/20_FINANCE_SALE_SETTLEMENT_PERSISTENCE.md`.

## Governance

This document records an internal implementation boundary only. It does not mark `API-SAL-007` implemented and does not authorize merge without explicit owner approval.
