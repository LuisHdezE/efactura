# Architecture / Security / Data Ready Acceptance — 2026-08-28

## Decision

`architecture_ready = PASS`

Human decision: after review of the architecture, security, data and threat-model boundary, the product owner instructed the process to advance to API Contract Design. This record accepts the architecture contract, including ADR-001 through ADR-018, while preserving the explicitly OPEN slice/deployment decisions listed below.

Blueprint evaluator: `0.5.1@ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`.

## Gate checks

| Blueprint check | Result | Evidence |
|---|---|---|
| `architecture.domain_model` | PASS | `03_DOMAIN_AGGREGATES_AND_INVARIANTS.md`, `04_VALUE_OBJECTS_AND_DOMAIN_SERVICES.md` |
| `architecture.decision_records` | PASS | `11_ARCHITECTURE_DECISIONS_ADR_INDEX.md`; ADR-001..018 accepted by this record |
| `architecture.security_model` | PASS | `07_SECURITY_AUTHORIZATION_AND_AUDIT.md` |
| `architecture.threat_model` | PASS | `13_THREAT_MODEL_BASELINE.md` |
| `data.architecture` | PASS | `06_DATA_ARCHITECTURE_POSTGRESQL_MYSQL.md` |
| `data.schema_migrations` | PASS | `14_DATA_MIGRATIONS_AND_DATABASE_AUTHORITY.md` |
| `data.authoritative_database` | PASS | `14_DATA_MIGRATIONS_AND_DATABASE_AUTHORITY.md` |
| `audit.event_catalog` | PASS | `15_AUDIT_EVENT_CATALOG_AND_RETENTION_POLICY.md` |
| `audit.retention_policy` | PASS | `15_AUDIT_EVENT_CATALOG_AND_RETENTION_POLICY.md` |
| `api.auth_strategy` | PASS | `07_SECURITY_AUTHORIZATION_AND_AUDIT.md` |
| `api.error_contract` | PASS | architecture rule in `10_API_CONTRACT_PREPARATION.md`: RFC 9457 Problem Details, redacted production errors, canonical 401/403/404/409/422/429 semantics to be specialized per operation in API Contract Design |
| `api.versioning_policy` | PASS | `/api/v1` target plus Brownfield compatibility policy in `10_API_CONTRACT_PREPARATION.md` |

## Accepted ADRs

ADR-001 through ADR-018 are `ACCEPT` for the target architecture. In particular:

- evolutionary modular monolith + Clean Architecture dependency direction;
- preserve current .NET 8 solution and migrate incrementally;
- EF Core writes/transactions + Dapper read models;
- PostgreSQL/MySQL first-class providers with isolated provider behavior;
- outbox/inbox/idempotency;
- JWT Bearer-compatible auth + server-authoritative permission policies;
- durable audit separate from technical logs;
- versioned fiscal rules and DGI/provider ports/adapters;
- separate fiscal document/transport/result lifecycles;
- typed fiscal identity, no authoritative `IsForeign` shortcut;
- deterministic offline sync and formal CFC separation;
- `/api/v1` additive target with legacy coexistence;
- no external network call inside business DB transaction;
- preserve Brownfield numeric keys while using GUID/UUID where new distributed/offline identity needs it;
- portable application-managed concurrency baseline;
- artifact store port with relational identity/hash/state;
- threat model applicable;
- Oracle `MySql.EntityFrameworkCore` EF8 line as current MySQL baseline.

## Decisions still OPEN

These do not invalidate this architecture PASS, but their dependent implementation slices remain blocked until resolved:

- direct DGI vs authorized provider/default;
- production certificate/private-key custody implementation;
- negative-stock/backorder behavior;
- credit-limit/overpayment/advance policy;
- initial PPP/FIFO scope;
- exact export-of-services documentation default;
- deployment-specific numeric retention/RPO/RTO/SLO values.

## Critical invariant

This is architecture **design acceptance only**.

`architecture design acceptance != architecture implementation conformance`

The later `api.architecture_implementation_conformance` check must independently prove that C# implementation follows these accepted boundaries.
