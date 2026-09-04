# 20 — Finance Sale Settlement Persistence Foundation

## Purpose

Introduce the durable PaymentMethod, Payment and Receivable primitives required before the transactional implementation of `API-SAL-007 confirmSale`.

This slice follows the accepted Sale settlement plan from document 19. It creates server-owned financial persistence but deliberately does not mark a Sale confirmed and does not expose a new public API.

## Ownership

### PaymentMethod

The Payments module owns target payment-method evidence. A method has organization scope, display name, enabled state and application-managed version.

Release 1 deliberately does not infer cash/card/bank custody semantics from Brownfield names. CashManagement consequences remain a separate policy/integration slice.

### Payment

A sale payment is an immutable financial fact linked to:

- organization;
- source Sale;
- deterministic sequence inside the settlement plan;
- PaymentMethod ID and the exact PaymentMethod version used;
- amount/currency;
- optional external reference;
- confirmation fingerprint;
- settlement fingerprint;
- recorded timestamp.

The database uniquely protects `(OrganizationId, SaleId, SettlementFingerprint, Sequence)` so the same settlement plan cannot accidentally persist the same payment slot twice.

### Receivable

A credit sale may create one base Receivable obligation. The durable record stores:

- organization/customer/source Sale;
- original amount and currency;
- due date;
- confirmation and settlement fingerprints;
- version;
- creation time.

The database uniquely protects `(OrganizationId, SaleId)` for the base obligation.

No mutable/open balance is stored in this foundation. The accepted contract requires balance to be derived from durable adjustments and allocations. Those facts will be introduced by the Receivables/Collections slices rather than allowing a client or mutable column to become balance authority.

## Persistence architecture

Application-owned ports:

- `IPaymentMethodRepository`;
- `IPaymentRepository`;
- `IReceivableRepository`.

Infrastructure implementations:

- `EfPaymentMethodRepository`;
- `EfPaymentRepository`;
- `EfReceivableRepository`.

`V1PersistenceModelCustomizer` extends the normal EF model after `V1PersistenceDbContext.OnModelCreating` runs. This avoids expanding the already large DbContext configuration method while still applying the same Finance model to PostgreSQL and MySQL through `V1PersistenceDatabaseConfigurator`.

Migration:

- `20260904002000_V1SaleSettlementFinance`.

Tables:

- `v1_payment_methods`;
- `v1_payments`;
- `v1_receivables`.

## Transaction evidence

The persistence integration suite executes the same Finance contract against PostgreSQL 16 and MySQL 8.4. It covers:

- PaymentMethod round-trip and versioned enabled evidence;
- Payment round-trip with settlement/payment-method snapshot evidence;
- Receivable round-trip with original obligation evidence;
- rollback of Payment + Receivable after a post-flush failure;
- unique enforcement of one base Receivable per Sale.

## Explicit non-goals

This slice does **not**:

- expose PaymentMethod/Payment/Receivable endpoints;
- expose `POST /api/v1/sales/{saleId}/confirm`;
- transition Sale to `CONFIRMED`;
- create payment allocations or collections;
- create receivable adjustments or aging;
- store a client-authoritative/open balance;
- implement credit limits, approval policies, advances or overpayments;
- infer cash/card/bank custody semantics;
- mutate CashManagement;
- consume stock;
- create `FiscalizationRequested`;
- reserve CAE or create a FiscalDocument;
- generate/sign/transport XML.

## Next slice

After this persistence foundation is accepted, implement the remaining local sale-confirmation effects: tracked-stock consumption plus durable `FiscalizationRequested` state. Then orchestrate Sale + Finance + Inventory + fiscalization-request + audit/idempotency/outbox in one local transaction before exposing `API-SAL-007`.

## Governance

This document records an internal implementation boundary. It does not mark `API-SAL-007`, OpenAPI, Postman or API QA complete. Merge remains owner-gated.
