# Data Migrations and Authoritative Database Policy

## Purpose

Materialize Blueprint checks `data.schema_migrations` and `data.authoritative_database` for the accepted eFactura target architecture.

## Authoritative database

Each deployment selects exactly one authoritative relational provider:

- PostgreSQL, or
- MySQL.

The selected relational database is authoritative for transactional business state, fiscal identities/state, stock movements/positions, receivables/payables, payments, idempotency/inbox/outbox and durable audit metadata.

Redis is a cache/coordination optimization and is never the sole authoritative store of a fiscal, financial, inventory or authorization fact.

Fiscal artifact bytes may live in an approved immutable artifact store behind `IFiscalArtifactStore`; the relational database remains authoritative for document identity, hash, lifecycle, provenance and storage reference.

There is no PostgreSQL/MySQL dual-write mode. Switching provider is a controlled migration, not active-active database ambiguity.

## Migration ownership

EF Core migrations are Infrastructure-owned and provider-specific when SQL/provider behavior differs.

Conceptual layout:

```text
Infrastructure/Persistence/PostgreSql/Migrations
Infrastructure/Persistence/MySql/Migrations
```

Both provider tracks implement the same accepted logical schema/invariants and are validated by the same persistence contract suite.

## Brownfield migration strategy

1. Baseline current production schema/data before each migration boundary.
2. Prefer additive tables/columns/indexes and compatibility mappings first.
3. Never mass drop/recreate the database to reach the target model.
4. Backfill through versioned, reviewable scripts/use cases with reconciliation counts/hashes where appropriate.
5. Keep legacy routes/tables during coexistence when unknown consumers still depend on them.
6. Move one functional slice/aggregate at a time to the target model.
7. Destructive cleanup requires proven consumer cutover, data reconciliation, backup/restore evidence and explicit human approval.

## Migration safety contract

Every production migration set must record:

- migration ID/version;
- target provider;
- forward SQL/EF migration artifact;
- expected locks/operational risk;
- preconditions;
- backup/restore or rollback strategy;
- data backfill/reconciliation steps;
- post-migration verification;
- compatibility impact;
- human approval when destructive/high-risk.

Rollback may use a corrective forward migration when literal down-migration would destroy new business evidence. `Down()` existence alone is not treated as a safe rollback guarantee.

## Provider-equivalence controls

Required before a schema change is accepted for both providers:

- constraints/uniqueness equivalent;
- decimal precision/rounding compatible;
- UTC/local fiscal date semantics compatible;
- concurrency behavior proven;
- indexes/query behavior adequate for critical paths;
- fiscal-number uniqueness/range behavior equivalent;
- migration suite runs on clean and representative upgraded databases.

## Schema versioning

The application records migration history through the provider's EF migration history plus deployment evidence. Provider-specific migration names may differ internally, but both map to the same logical application schema revision.

No application business rule may branch on provider product to compensate for a missing schema invariant.
