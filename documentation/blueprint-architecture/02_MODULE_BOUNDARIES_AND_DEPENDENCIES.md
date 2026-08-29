# Module Boundaries and Dependency Rules

## Principle

Modules are business ownership boundaries inside one deployable server initially. A module may expose application contracts/events but must not expose its database as a shared integration surface.

## Canonical modules

| Module | Owns | May depend on |
|---|---|---|
| Organization | company, fiscal establishment/location, terminal/cash-register context | none except SharedKernel |
| IdentityAccess | user, role, permission assignment, organization/location scope | Organization |
| Parties | party, customer/supplier roles, fiscal identities, addresses/contacts | Organization references only |
| Catalog | commercial item, product/service type, categories, units | Taxation references |
| Taxation | tax categories/rates, fiscal rule metadata used outside CFE engine | Organization |
| Sales | sale lifecycle, lines, pricing snapshots, payment terms | Parties, Catalog, Taxation, Organization |
| Fiscal | CFE/CFC lifecycle, CAE, DGI rule/version, numbering, fiscal artifacts | Organization; consumes immutable transaction/party snapshots via contracts |
| Inventory | stock movements, positions, transfers, adjustments | Catalog, Organization |
| Procurement | purchase orders and receipts | Parties, Catalog, Organization |
| Receivables | customer obligations/aging/allocations | Parties, Sales references |
| Payables | supplier obligations/aging/allocations | Parties, Procurement references |
| Treasury | payment/collection instruments and allocation orchestration | Receivables, Payables, CashManagement |
| CashManagement | cash shifts, cash movements, reconciliation | Organization, Treasury references |
| Sync | client operations, devices, batches, replay/conflict state | IdentityAccess, module application contracts |
| Reporting | read models/projections only | reads approved module data, never writes source modules |
| Audit | immutable business/security audit events | consumes event/context from all modules |

## Important separation rules

### Sales is not Fiscal

`Sale` represents the commercial transaction. `FiscalDocument` represents a fiscal artifact and its regulatory lifecycle. A confirmed sale can exist while fiscal transmission is pending; a transport acknowledgement is not sale acceptance and is not final DGI acceptance.

### Payment is not Cash

A payment/collection is a financial fact. A cash movement/shift is a custody/reconciliation fact. Cash is one payment medium, not the identity of all payments.

### Catalog is not Inventory

A service can be sold with no stock. A product may be stock-tracked or intentionally non-stock-tracked. Catalog defines what may be sold; Inventory owns quantities and movements.

### Party is not Customer-only

One legal/person party may hold CUSTOMER and SUPPLIER roles. Nationality/residence, tax residence, document issuing country and possession of a Uruguayan RUC are independent facts.

### Procurement is not Payables

Receipt/purchase approval creates commercial evidence. A payable is a financial obligation linked to that evidence. The modules integrate explicitly.

## Cross-module collaboration

Allowed mechanisms:

1. application orchestrator calling public application contracts;
2. domain/application events within the process;
3. durable outbox events for asynchronous/background effects;
4. stable identifiers/snapshots;
5. reporting read models.

Forbidden mechanisms:

- controller calling another module repository;
- repository joining/updating another module's tables to implement business behavior;
- sharing mutable EF entities as cross-module contracts;
- a module directly changing another module's state through SQL;
- external provider callbacks writing business tables without application use cases.

## Transaction policy

Because the initial deployment is a modular monolith on one relational database, a single application use case may coordinate multiple module repositories in one short local transaction when atomicity is essential. This is an explicit orchestration decision, not permission for module persistence coupling.

Examples where a local atomic boundary may be justified:

- sale confirmation + authoritative commercial state + idempotency record;
- payment allocation + receivable/payable balance effects;
- inventory movement + current-position update;
- fiscal number reservation + fiscal-document identity persistence.

External DGI/provider/email/blob network work is not performed while holding the transaction. It is represented by durable workflow/outbox state.

### Mandatory v1 write invariant

For every new `/api/v1` use case that mutates authoritative business state:

1. Application defines the atomic boundary;
2. Infrastructure executes that boundary through the Application-owned `ITransactionManager` and `IUnitOfWork` contracts;
3. participating repositories stage changes and do not independently commit;
4. the business mutation, required idempotency state, durable audit evidence and Outbox messages that belong to that operation commit together when the use case requires them atomically;
5. any exception or cancellation before commit produces a complete rollback of the local authoritative mutation;
6. external network work runs only after the local commit, normally through Outbox/background processing;
7. PostgreSQL and MySQL must demonstrate equivalent commit/rollback behavior through integration tests.

The target is not merely to "use transactions somewhere". The transaction boundary must surround the complete local business invariant so an operation cannot remain half applied.

### No ad-hoc SQL for business-state mutation

New v1 write-side persistence uses EF Core repositories/change tracking as the default mutation mechanism.

Runtime business-state writes through flat/ad-hoc SQL are prohibited, including direct `INSERT`, `UPDATE`, `DELETE` or `MERGE`, Dapper mutation commands, provider-specific command objects and EF raw-SQL mutation APIs.

Dapper/optimized SQL remain valid for approved read-only query/reporting models. Schema migrations and deployment-time database evolution are separate from runtime business-state mutation.

A future exception requires an explicit ADR and human approval, must participate in the same transaction model, must have equivalent PostgreSQL/MySQL behavior and must include rollback/concurrency tests. No write-side exception is introduced silently as an implementation shortcut.

## Integration event examples

- `SaleConfirmed`
- `FiscalizationRequested`
- `FiscalDocumentIssued`
- `FiscalDocumentAccepted`
- `FiscalDocumentRejected`
- `InventoryMovementPosted`
- `ReceivableCreated`
- `PaymentAllocated`
- `PurchaseReceiptPosted`
- `PayableCreated`
- `ContingencyEntered`
- `ContingencyDocumentRegistered`
- `PermissionChanged`

Events are facts. They do not contain secrets/private keys and do not become an unbounded dumping ground for entire aggregates.

## Reporting boundary

Reporting may use Dapper and optimized SQL/read models across data owned by multiple modules because it is read-only. Reporting queries must not become a backdoor for mutation or business-state transitions.

## Future decomposition

No microservice split is required for Release 1. Module boundaries are designed so that later extraction is possible if operational evidence justifies it, but distributed systems complexity is not introduced speculatively.
