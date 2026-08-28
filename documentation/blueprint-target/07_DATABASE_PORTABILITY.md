# PostgreSQL / MySQL Portability Requirements

## Requirement

Each eFactura deployment can use **PostgreSQL or MySQL**, selected in deployment configuration. The business/API contract must behave the same on both supported providers.

This is not a runtime per-request switch and one logical installation does not simultaneously write the same business aggregate to two providers.

## Target configuration

Conceptually:

```text
Database:Provider = PostgreSql | MySql
Database:ConnectionString = <secret source>
```

Secrets are never stored in committed application settings.

## Layering

Application/Domain consume persistence ports/repositories/unit-of-work abstractions. Provider libraries live only in Infrastructure/Composition.

```text
ApplicationCore -> persistence abstractions
Infrastructure.Persistence.PostgreSql -> EF/Npgsql/provider-specific implementation
Infrastructure.Persistence.MySql      -> EF/MySQL/provider-specific implementation
WebApi Composition                     -> select configured provider
```

Dapper may remain for specialized read paths, but provider-specific SQL/dialect is isolated behind Infrastructure implementations.

## Migration strategy

Maintain provider-aware migration assets. A single migration source may be used only when generated SQL is proven portable. Otherwise keep explicit provider tracks, for example:

```text
Infrastructure/Persistence/Migrations/PostgreSql
Infrastructure/Persistence/Migrations/MySql
```

Every schema change must be validated on both providers before API/release acceptance.

## Portable design rules

1. **Money:** .NET `decimal`; database exact decimal/numeric with explicit precision/scale. Never float/double for fiscal amounts.
2. **Identity:** avoid application reliance on PostgreSQL-only `nextval(...)`; use provider-neutral identity strategy or application-generated IDs where justified.
3. **Concurrency:** do not depend on PostgreSQL `xmin` in application/domain contracts. Use portable version/concurrency token strategy.
4. **Dates:** persist instants consistently (UTC where appropriate) while retaining business/fiscal local date/time fields needed for Uruguay rules. Do not infer fiscal date from database-server local timezone.
5. **Text/collation:** document case/accent-sensitive uniqueness behavior and normalize identifiers such as RUT/series/codes explicitly.
6. **Boolean/enums:** use provider mappings with identical domain semantics.
7. **JSON:** do not make core domain behavior depend on PostgreSQL `jsonb` operators or MySQL JSON-specific querying unless isolated behind provider-specific read models.
8. **XML/artifacts:** store large XML/PDF through an artifact storage abstraction or portable text/blob strategy; retain hash/metadata in relational DB.
9. **Indexes:** provider-specific physical indexes are allowed in migrations when they preserve the same logical constraints/performance target.
10. **Transactions:** application use cases depend on semantic transaction boundaries, not provider-specific syntax.

## Fiscal-number concurrency contract

A shared persistence contract test must prove under contention that:

- two concurrent terminals cannot reserve the same company+CFE-type+series+number;
- an idempotent retry returns its prior reservation/result rather than reserving another number;
- expired/exhausted CAE cannot issue a new normal CFE;
- transaction rollback does not leave an externally visible duplicate reservation state;
- PostgreSQL and MySQL satisfy equivalent behavior.

The exact implementation can differ per provider, but the application contract cannot.

## Outbox / idempotency schema

Both providers must support equivalent logical constraints for:

- idempotency record unique key;
- request/payload hash;
- canonical result/reference;
- outbox message identity/type/aggregate/time;
- processing lease/attempt/error metadata;
- audit/correlation linkage.

## Test matrix required later

CI/test acceptance must execute the same persistence contract against:

- PostgreSQL supported version;
- MySQL supported version.

At minimum:

- migrations up from empty database;
- CRUD/repositories;
- decimal/tax precision;
- date/time serialization;
- unique constraints;
- concurrency/CAE reservation;
- idempotency;
- outbox processing;
- audit writes;
- sales + payments + stock transaction;
- receivable/payable allocations;
- rollback/error paths.

A provider is not considered supported merely because its NuGet package is referenced.
