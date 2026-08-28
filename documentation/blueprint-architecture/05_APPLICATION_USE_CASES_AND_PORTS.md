# Application Use Cases, Ports and Transaction Boundaries

## Application pattern

HTTP endpoints call one application use case/handler. Controllers do not coordinate repositories directly.

Recommended conceptual shape:

```text
HTTP Contract
  -> Command/Query
  -> Handler/Application Service
  -> Domain aggregates/policies
  -> Ports
  -> Infrastructure adapters
```

No specific mediator library is mandatory. The contract is architectural, not framework fashion.

## Core application ports

### Request/security context

- `ICurrentActor`
- `IOrganizationContext`
- `IPermissionAuthorizer`
- `IClock`
- `ICorrelationContext`

### Persistence/transaction

- module repository interfaces;
- `IApplicationTransaction` / unit-of-work abstraction where multi-repository atomicity is required;
- `IReadModelQuery` abstractions only where Dapper query ownership benefits from an explicit port.

### Reliability

- `IIdempotencyStore`
- `IInboxStore`
- `IOutboxWriter`
- `IAuditWriter`

### Fiscal

- `IFiscalRuleCatalog`
- `IFiscalDocumentSelector`
- `IFiscalNumberAllocator`
- `IFiscalXmlBuilder`
- `IFiscalValidator`
- `IFiscalSigner`
- `IFiscalTransportGateway`
- `IFiscalResponseInterpreter`
- `ICaeArtifactVerifier`
- `IFiscalArtifactStore`

### External integrations

- `IDocumentStorage`
- `INotificationSender`
- accounting export ports by format;
- provider-specific adapters behind these ports.

## Command/query separation

Queries are side-effect free and may use optimized Dapper read models.

Commands that change financial/fiscal/stock state require:

- explicit permission;
- idempotency classification;
- transaction/concurrency policy;
- durable audit mapping;
- error semantics;
- requirement/use-case traceability.

## Sale confirmation orchestration

Target sequence:

1. authenticate/authorize actor and location/terminal scope;
2. claim/check idempotency key or client operation identity;
3. load Sale and validate version/state;
4. resolve receiver/tax/commercial rules;
5. confirm immutable commercial snapshot;
6. create required receivable or payment intent/effect according to approved payment mode;
7. create inventory movement effects only for stock-tracked lines;
8. create `FiscalizationRequested` durable workflow state when fiscalization applies;
9. append audit/outbox evidence;
10. commit local transaction;
11. execute fiscal generation/signing/transport workflow without holding that transaction;
12. publish/persist canonical command result for retry.

The exact split between steps 5-9 may use one short database transaction in the modular monolith. External provider/DGI calls never run inside it.

## Fiscalization workflow

1. consume/create fiscalization work item idempotently;
2. resolve active rule/specification and CFE eligibility;
3. allocate fiscal number atomically;
4. persist fiscal-document identity/snapshot;
5. generate/validate artifact;
6. sign through configured signer;
7. persist immutable signed artifact/hash;
8. enqueue transport/outbox;
9. transport adapter submits envelope/document;
10. persist synchronous receipt separately;
11. later consume asynchronous result;
12. transition fiscal state and audit;
13. if rejected, create regularization work item instead of delete/reuse.

If certificate/key signing is an external network service, the workflow keeps explicit intermediate states so retries do not allocate another number.

## Payment allocation workflow

Atomic invariant:
- allocation amounts cannot exceed permitted policy;
- the same payment/allocation operation cannot apply twice;
- obligation balance derives from durable allocation facts.

Use optimistic/application version plus transaction locking strategy isolated by provider adapter.

## Inventory workflow

A stock-changing command writes immutable `StockMovement` and updates `InventoryPosition` in the same local transaction. If concurrency conflict occurs, the command returns conflict/retry semantics instead of silently overwriting quantity.

## Cross-module access rule

Application orchestrators may depend on public application contracts from other modules. They may not depend on another module's EF DbSet or concrete repository.

## Background workers

Workers use the same application contracts/security/system-actor context as other entry points and never bypass domain invariants. Every worker item is retry-safe and observable.
