# Target Capability Map

## Product definition

`eFactura` will evolve from its current accounting/API skeleton into a modular backend for **commercial management, POS operation and electronic invoicing in Uruguay**, capable of serving future web and mobile clients.

Current boundary: **API/backend only**. No visual/frontend implementation is authorized in this phase.

## Non-negotiable target constraints

1. Preserve .NET/C#/ASP.NET Core and the existing clean-architecture direction.
2. PostgreSQL **or** MySQL is selected per deployment/customer configuration.
3. Business/application code cannot depend on a specific database engine.
4. The API must serve goods-only, services-only and mixed businesses.
5. The API must be offline-aware for future web/mobile clients.
6. Fiscal behavior must be versioned and traceable to DGI official specifications.
7. DGI/provider integration is behind application interfaces/adapters.
8. Technical logging and durable business/security audit are separate capabilities.
9. Financial/fiscal commands are idempotent where retries can duplicate effects.

## Business operating profiles

The system must not assume that every company owns stock.

### `GOODS`
- sellable items can track inventory;
- purchasing, stock, costing and replenishment enabled.

### `SERVICES`
- sellable items do not require stock;
- POS/fiscal/AR/payments remain available;
- examples: barber shop, consultancy, repair service.

### `MIXED`
- one sale may contain stock-tracked products and non-stock services.

Target model principle:

`CommercialItem` identifies what can be sold; inventory participation is a property/policy of the item, not a requirement for every sale line.

## Target modules

| Module | Core responsibilities | Profile |
|---|---|---|
| IdentityAccess | users, credentials, refresh lifecycle, roles, permissions, devices | all |
| Organization | company fiscal profile, branches, terminals, configuration | all |
| Parties | customers, suppliers, tax/document identities, contacts | all |
| Catalog | products, services, categories, units, pricing, tax assignment | all |
| TaxRules | fiscal/tax catalog, VAT/rates, CFE rule versions, currency rules | all |
| Sales | drafts, lines, pricing, discounts, totals, confirmation, cancellation intent | all |
| POS | checkout orchestration and terminal/cash-session context | all |
| Payments | payment records, media, allocations, reversals where allowed | all |
| CashManagement | cash shift open/close, counted cash, reconciliation, variance | all |
| ElectronicInvoicing | CFE/CFC lifecycle, corrections, fiscal archive | all |
| CAE | CAE import/state/ranges/number reservation/alerts | all fiscal issuers |
| DgiIntegration | XML/schema, signing, envelope, transport, acknowledgements | all fiscal issuers |
| Contingency | CFC registry, recovery/transmission/report linkage | all fiscal issuers |
| OfflineSync | client operation envelope, idempotency, sync/conflict policy | web/mobile clients |
| Inventory | stock positions, movements, adjustments, transfers | goods/mixed |
| Costing | PPP/FIFO policy and valuation | goods/mixed, conditional |
| Procurement | purchase proposals/orders/receipts | goods/mixed |
| Replenishment | stock minimum, ROP, EOQ simulation | goods/mixed |
| AccountsReceivable | credit-sale obligations, aging, balances | conditional but core-ready |
| Collections | receipts/collections and allocations | conditional but core-ready |
| AccountsPayable | supplier obligations, aging, balances | conditional but core-ready |
| SupplierPayments | supplier payments and allocations | conditional but core-ready |
| FiscalDocumentsReceived | received CFE/import/manual intake and validation | conditional |
| XmlValidation | batch/single CFE validation and findings | all/contador tool |
| FiscalReporting | daily report and fiscal books/aggregates | all fiscal issuers |
| ManagementReporting | sales, margins, stock, AR/AP, cash-flow projections | all |
| FiscalCalendar | DGI/BPS obligation calendar with source provenance | conditional |
| Audit | durable sensitive business/security events and before/after context | all |
| Observability | logs, metrics, traces, correlation, integration health | all |
| Documents | XML/PDF/report artifacts and retention metadata | all |
| Notifications | CAE/cert/stock/due-date/fiscal transmission alerts | all |
| Integrations | CFE provider, email, WhatsApp, accounting exports, future adapters | conditional |

## Cross-cutting API requirements

Every retry-sensitive command must be designed for safe replay. The contract baseline will support where applicable:

- `Idempotency-Key`;
- `clientOperationId`;
- `deviceId`;
- `occurredAt` and server `receivedAt`;
- correlation/request identifiers;
- optimistic/concurrency metadata where conflicts are possible;
- stable error codes and Problem Details-compatible responses;
- actor/permission context;
- durable audit event linkage.

## Fiscal lifecycle separation

Do not collapse all status into `Sale.Status`.

At minimum the model separates:

### Sale lifecycle
`DRAFT -> VALIDATED -> CONFIRMED -> COMPLETED/CANCELLED`

### Payment lifecycle
`PENDING -> AUTHORIZED/RECORDED -> ALLOCATED -> REVERSED` where applicable.

### Fiscal document lifecycle
`NUMBER_RESERVED -> GENERATED -> VALIDATED -> SIGNED -> QUEUED/SUBMITTED -> ACKNOWLEDGED -> ACCEPTED | REJECTED | REGULARIZATION_REQUIRED`

### CFC contingency lifecycle
`AVAILABLE_PAPER_RANGE -> ISSUED_CFC -> REGISTERED -> PENDING_TRANSMISSION -> REPORTED/TRANSMITTED -> RECONCILED`

### Inventory lifecycle
Stock movement is immutable history; corrections use compensating/adjustment movements rather than deleting history.

## Initial target release priorities

### Release Core A — Foundation
- organization/identity/permissions;
- customer/supplier/catalog;
- database portability;
- audit/idempotency/outbox foundations.

### Release Core B — POS + Fiscal
- sales and payments;
- e-Ticket/e-Factura families selected by approved fiscal matrix;
- CAE;
- signing/transport/response lifecycle;
- corrections/rejections;
- daily report;
- contingency/offline-safe design.

### Release Core C — Commercial Management
- inventory and stock movements;
- procurement/receiving;
- CxC/CxP and allocations;
- cash shift/reconciliation.

### Release Core D — Advanced Tools
- received CFE;
- XML validator;
- EOQ/ROP/costing;
- analytics/fiscal calendar/accounting exports;
- notification adapters.

This ordering is a planning baseline, not authorization to implement before Requirements/Architecture/API Contract gates are satisfied.
