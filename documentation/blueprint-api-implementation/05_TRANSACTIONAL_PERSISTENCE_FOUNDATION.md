# API v1 Transactional Persistence Foundation

## Status

Implementation slice for the accepted transactional write policy. This slice does not claim final API persistence conformance; it establishes and proves the cross-cutting relational transaction substrate before business verticals are migrated.

## Separate v1 context

`V1PersistenceDbContext` is intentionally separate from the Brownfield `DBContext`.

The legacy context contains PostgreSQL-specific sequence/default/type mappings and remains attached to legacy repositories during migration. The v1 context begins provider-neutral and owns only newly implemented v1 persistence models.

## Supported providers

The implementation supports:

- PostgreSQL through the existing Npgsql EF Core provider;
- MySQL through `MySql.EntityFrameworkCore` 8.0.8.

Version 8.0.8 was selected for this Brownfield slice because it supports net8.0, accepts EF Core 8.0.8 or later, and matches the repository's existing `MySql.Data` 9.1.0 dependency without forcing an unrelated framework-wide EF upgrade in the same PR.

Provider selection is explicit through `V1DatabaseProvider`. The default parser remains PostgreSQL when no new provider setting is present, preserving the current deployment behavior. Selecting MySQL requires a MySQL connection string at composition time.

## Application-owned ports

Application remains framework-neutral and owns:

- `IUnitOfWork`;
- `ITransactionManager`;
- `IAuditWriter`;
- `IIdempotencyStore`;
- `IOutboxWriter`;
- `IInboxStore`.

Infrastructure implements these contracts with EF Core. Domain contains no persistence knowledge.

## Atomicity

`EfTransactionManager` opens one short relational transaction for an authoritative local workflow. Repositories/writers stage changes only. `EfUnitOfWork` flushes the shared scoped v1 DbContext. The transaction commits only after the Application callback finishes successfully.

If the callback throws or is cancelled, the transaction rolls back and the change tracker is cleared before the failure propagates.

Intermediate relational flushes do not weaken atomicity. A use case may flush once to establish database-generated/constraint-visible state and continue working inside the same open transaction. If a later step fails, the earlier flush is rolled back with the transaction.

## Cross-cutting records

The first provider-neutral tables are:

- `v1_audit_events`;
- `v1_idempotency_records`;
- `v1_outbox_messages`;
- `v1_inbox_messages`.

They deliberately avoid PostgreSQL-only sequences, PostgreSQL-only column types and provider-specific runtime write commands.

Idempotency keys and inbox message IDs are persisted as SHA-256 hashes for stable indexed identity and to avoid depending on provider collation behavior for raw client identifiers.

## No flat mutation SQL

The v1 write path uses EF Core tracked state. Dapper remains available only for approved read/reporting paths. The existing ArchitectureTests continue to reject raw mutation APIs or provider command objects under `Infrastructure/Persistence/V1/Write` and reject repository-owned commits/transactions.

## Integration evidence

`PersistenceIntegrationTests` execute the same transaction scenarios against real PostgreSQL and MySQL service instances in GitHub Actions.

Required rollback scenario:

1. reserve idempotency;
2. reserve inbox identity;
3. stage audit;
4. stage outbox;
5. flush relational changes while the transaction is still open;
6. inject an exception before commit;
7. verify all four tables remain empty after rollback.

Required success scenario:

1. reserve/stage the same cross-cutting state;
2. flush;
3. complete idempotency and inbox state;
4. flush again;
5. commit;
6. verify all records are present together;
7. verify replay returns the completed idempotent outcome;
8. verify a different request hash is rejected as a payload mismatch.

The injected failure occurs after a real database flush, specifically proving that transaction rollback protects against partially persisted multi-step work.

## Next boundary

Before a Sales/Fiscal write vertical is declared implemented, its aggregate/business rows must join the same transaction tests so CI demonstrates rollback across business state plus audit, idempotency and Outbox, not only across cross-cutting tables.

Provider-specific production migrations are intentionally deferred until the first business schema slice is ready; tests use provider database creation only as ephemeral CI evidence. Production startup must not use `EnsureCreated`.
