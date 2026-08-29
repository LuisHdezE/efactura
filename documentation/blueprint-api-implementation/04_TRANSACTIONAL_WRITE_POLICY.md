# Transactional Write Policy

## Status

Mandatory implementation invariant for every new `/api/v1` business-state mutation.

This policy strengthens the accepted Architecture transaction policy and the project Clean Architecture requirement. It is not optional implementation guidance.

## Core rule

An authoritative business operation that changes persistent state must either complete as one coherent local transaction or leave no partial business-state mutation behind.

For a multi-step operation:

```text
BEGIN TRANSACTION
  validate current authoritative state
  mutate aggregate(s)
  persist related financial/inventory/fiscal state when part of the same atomic use case
  reserve/complete idempotency state
  append durable audit evidence
  append Outbox message(s)
  SaveChanges
COMMIT
```

If any required local step fails before commit:

```text
ROLLBACK
```

No half-completed authoritative operation is accepted.

## Application-owned boundary

Application owns the transaction abstraction through:

- `ITransactionManager`;
- `IUnitOfWork`.

Infrastructure implements them with the configured relational provider.

Domain knows nothing about transactions, EF Core, PostgreSQL, MySQL, connections or SQL.

Application use cases orchestrate the business operation and decide what belongs to one atomic unit. Controllers do not open transactions. Repositories do not independently commit.

## Write persistence rule

For new v1 write-side persistence, EF Core change tracking/repositories are the default mutation mechanism.

Runtime business-state mutation through flat/ad-hoc SQL is prohibited in the v1 write path, including direct `INSERT`, `UPDATE`, `DELETE`, `MERGE`, provider commands, Dapper mutation commands and EF raw-SQL mutation APIs.

Examples that are not allowed in v1 write repositories:

```text
ExecuteSqlRaw(... UPDATE ...)
ExecuteSqlInterpolated(... DELETE ...)
connection.ExecuteAsync("INSERT ...")
new NpgsqlCommand("UPDATE ...")
new MySqlCommand("DELETE ...")
```

Dapper and optimized SQL remain allowed for approved read-only reporting/query models. They must never become a backdoor for business-state mutation.

Database migrations/schema-management scripts are a separate deployment concern and are not runtime business mutation paths.

Any future exception to the no-raw-write rule requires an explicit ADR, human approval, provider-equivalent implementation, transaction participation and rollback tests. No exception may be introduced silently for convenience or performance speculation.

## Repository behavior

New v1 write repositories:

1. load and persist aggregates/entities through the configured EF Core DbContext;
2. stage changes only;
3. do not call `SaveChanges` on their own;
4. do not begin/commit/rollback transactions;
5. do not make external network calls;
6. do not modify another module's tables directly.

The Application use case coordinates repositories and calls `IUnitOfWork.SaveChangesAsync` inside the transaction managed by `ITransactionManager`.

## Atomic companions

When required by the use case, these records participate in the same local transaction as the business mutation:

- idempotency reservation/result state;
- durable audit event(s);
- Outbox event(s);
- fiscal-number reservation and fiscal-document identity where the accepted workflow defines them as one atomic local boundary;
- inventory movement and authoritative inventory position update;
- payment allocation and obligation balance effects.

This prevents scenarios such as a committed sale without its Outbox event, a payment allocation without its audit evidence, or a fiscal number reservation without its associated fiscal-document identity.

## External side effects

External calls are not held inside the relational transaction.

Wrong:

```text
BEGIN TRANSACTION
  update sale
  call DGI over network
  wait/retry
  COMMIT
```

Required pattern:

```text
BEGIN TRANSACTION
  persist authoritative local state
  persist audit/idempotency
  persist Outbox
COMMIT

Outbox worker
  call DGI/provider/email/blob/etc.
  persist resulting state in a new short transaction
```

This avoids long locks and prevents Internet/provider latency from controlling database transaction lifetime.

## Rollback semantics

Infrastructure transaction implementations must:

- begin one local provider transaction for the Application operation;
- use the same scoped write DbContext/connection for all participating v1 repositories;
- commit only after the Application callback and required `SaveChangesAsync` complete successfully;
- roll back on any exception;
- roll back on cancellation;
- rethrow/propagate the failure for canonical Problem Details handling;
- never convert a failed partial write into a successful API result.

## Concurrency

Transactions do not replace concurrency control.

The implementation will additionally use the appropriate optimistic or controlled pessimistic strategy for operations such as:

- CAE/fiscal number reservation;
- stock reservation/position mutation;
- payment allocation;
- cash close/reconciliation;
- idempotency claim;
- retry/work-item state transitions.

A concurrency conflict produces no partial authoritative mutation.

## PostgreSQL and MySQL equivalence

The same transaction contract is required on both supported database providers.

A write workflow is not considered implemented until integration tests demonstrate equivalent atomic behavior against PostgreSQL and MySQL, including rollback after an injected failure between local write steps.

Representative required test:

```text
1. stage Sale confirmation
2. stage Inventory movement
3. stage Audit event
4. inject failure before Outbox/commit
5. operation fails
6. assert Sale was not confirmed
7. assert Inventory was not changed
8. assert no partial Audit/Outbox/idempotency completion remains
```

The complementary success test proves all participating state commits together.

## CI guard

ArchitectureTests guard the future `src/Infrastructure/Persistence/V1/Write/**` path so it cannot silently introduce Dapper/raw-SQL mutation or repository-owned commit/transaction calls.

Runtime integration tests in the next persistence slice must prove actual commit/rollback semantics. Static architecture checks alone are not sufficient evidence.
