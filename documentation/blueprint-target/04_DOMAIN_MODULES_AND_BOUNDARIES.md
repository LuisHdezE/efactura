# Domain Modules and Clean-Architecture Boundaries

## Architecture intent

The target preserves the existing .NET solution style instead of replacing the technology stack. The current project names can remain while internal boundaries become explicit and enforceable.

Recommended evolutionary layout:

```text
src/ApplicationCore/
  Domain/
    Organization/
    Parties/
    Catalog/
    Sales/
    Payments/
    Fiscal/
    Inventory/
    Procurement/
    Receivables/
    Payables/
    CashManagement/
    Audit/
  Application/
    <same modules>/UseCases
    Common/Ports

src/Infrastructure/
  Persistence/
    Common/
    PostgreSql/
    MySql/
  Integrations/
    Dgi/
    CfeProviders/
    Email/
    AccountingExports/
  Security/
  Observability/

src/WebApi/
  Controllers/V1/
  Contracts/
  Middleware/
  Composition/

src/Shared/
  truly generic cross-cutting primitives only
```

This does **not** require immediately splitting ApplicationCore into new assemblies. A future assembly split requires an ADR and human approval. The first objective is dependency direction and logical module boundaries.

## Dependency rules

1. `ApplicationCore.Domain` depends on no EF Core, ASP.NET Core, Npgsql, MySql provider, Redis or external CFE SDK.
2. `ApplicationCore.Application` may depend on Domain and application-owned ports/interfaces.
3. `Infrastructure` implements application ports and may depend on database/provider/integration libraries.
4. `WebApi` is the composition and transport boundary; controllers do not contain business rules.
5. `Shared` cannot become a backdoor for Infrastructure/Web dependencies into the core.
6. DGI/provider response models are translated at the infrastructure boundary into application/domain concepts.
7. Provider-specific SQL is isolated and cannot leak into use cases.

## Module responsibilities

### Organization
Owns company fiscal identity, branches, terminals, operational/fiscal configuration references. It does not own authentication credentials.

### IdentityAccess
Owns users, credentials/session lifecycle, roles, permissions and device/session context. Business modules consume actor/permission abstractions.

### Parties
Owns customers, suppliers, fiscal identity data and contacts. It provides party snapshots/references to fiscal documents so historical documents do not mutate when a master record changes.

### Catalog
Owns sellable items, categories, units, price/tax assignment and `TrackInventory`. A service is not forced into an inventory entity.

### Sales
Owns sale intent/draft/confirmation, sale lines and commercial totals. It does not directly call Npgsql/EF or DGI.

### Payments
Owns payment records, media and allocations. A payment is not just a mutable amount field on a receivable.

### ElectronicInvoicing/Fiscal
Owns fiscal-document identity/lifecycle, references/corrections, fiscal snapshots and state transitions. It delegates XML/signature/transport to ports.

### CAE
Owns imported CAE metadata, validity, authorized ranges and atomic number reservation. Fiscal numbering is company-wide per CFE type according to the DGI baseline.

### DgiIntegration
Infrastructure-facing concerns for official schemas, signing, envelope/transport, acknowledgements and provider adapters. It must support both a direct-DGI gateway and authorized-provider gateways when required by deployment.

### Contingency
Owns CFC registry and reconciliation. It is not equivalent to a generic HTTP retry queue.

### OfflineSync
Owns client operation deduplication/synchronization protocol. It must not grant mobile/web clients authority to invent CAE/CFE numbering.

### Inventory
Owns stock positions and immutable movements for stock-tracked catalog items. Sale, receipt, transfer and adjustment are movement sources.

### Procurement
Owns purchase proposals/orders/receipts. EOQ/ROP simulation can suggest quantities but cannot directly mutate stock.

### AccountsReceivable / Collections
Own obligations created by credit sales/other receivables and payment allocation history.

### AccountsPayable / SupplierPayments
Own supplier obligations and payment allocation history.

### CashManagement
Owns POS/cash sessions, expected vs counted amounts and reconciliation/variance.

### Audit
Owns durable business/security audit records. Technical logs remain an Observability responsibility.

## Transaction and consistency principles

### Local transaction
Within one database transaction, persist the business state change, audit metadata needed for durability, idempotency record and outbox event/message where applicable.

### External calls
Do not hold database transactions open while waiting for DGI/provider/email services. Persist an outbox/work item and advance the external lifecycle asynchronously or through a controlled synchronous orchestration where the current fiscal rule requires an immediate transmission attempt.

### Fiscal number reservation
CAE number reservation must be atomic and unique under concurrent POS terminals. The design must work in PostgreSQL and MySQL without relying on a PostgreSQL-only primitive in application code.

### Historical snapshots
CFE, sale, purchase and payment evidence must preserve the names/tax IDs/rates/prices/rules used at the moment of the transaction instead of dereferencing mutable master data at read time.

## Architecture fitness tests to introduce later

- ApplicationCore cannot reference Infrastructure/WebApi projects.
- Domain namespaces cannot reference EF Core, ASP.NET, Npgsql/MySql, Redis or provider SDK namespaces.
- WebApi controllers cannot reference persistence implementations directly.
- provider-specific namespaces cannot be referenced by Domain/Application.
- all fiscal gateway implementations satisfy the same application port contract.
- both PostgreSQL and MySQL integration suites execute the same persistence contract tests.

These tests are future implementation evidence, not executed in this documentation boundary.
