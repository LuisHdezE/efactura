# Architecture Decision Record Index

All entries are `PROPOSED` until human architecture acceptance. Implementation must not silently change them.

| ADR | Decision | Status |
|---|---|---|
| ADR-001 | Evolve eFactura as a modular monolith with Clean Architecture dependency direction; no rewrite/microservice split for Release 1 | PROPOSED |
| ADR-002 | Preserve current .NET 8 projects initially; harden `ApplicationCore` into framework/provider-independent inward core | PROPOSED |
| ADR-003 | EF Core for writes/transactions and Dapper for read models/measured hot paths | PROPOSED |
| ADR-004 | PostgreSQL and MySQL are first-class deployment providers with provider-specific migrations and shared contract tests | PROPOSED |
| ADR-005 | Durable outbox/inbox/idempotency records for retry-sensitive external/offline flows | PROPOSED |
| ADR-006 | JWT Bearer authentication plus server-authoritative permission/policy authorization with organization/location scope | PROPOSED |
| ADR-007 | Durable business/security audit is append-oriented and separate from Serilog/Application Insights | PROPOSED |
| ADR-008 | Fiscal rules/selection are versioned domain/application policy; DGI/provider access is behind ports/adapters | PROPOSED |
| ADR-009 | CFE document lifecycle, transport lifecycle and DGI result lifecycle remain separate | PROPOSED |
| ADR-010 | Party model uses typed fiscal identities/residence/tax-residence; no authoritative `IsForeign` shortcut | PROPOSED |
| ADR-011 | Offline sync uses client operation IDs, deterministic replay and formal CFC contingency separation | PROPOSED |
| ADR-012 | Existing Brownfield routes coexist until consumer impact is known; new target contract begins at `/api/v1` | PROPOSED |
| ADR-013 | External network calls are outside local DB transactions; outbox/workflow state bridges commit to transport | PROPOSED |
| ADR-014 | Preserve existing numeric DB keys during migration; add UUID/GUID identities for offline/device/idempotency concerns rather than global re-key | PROPOSED |
| ADR-015 | Application-managed concurrency version is the portable baseline; provider-native locking may be an internal adapter optimization | PROPOSED |
| ADR-016 | Fiscal artifact bytes are stored through `IFiscalArtifactStore`; relational DB stores immutable metadata/hash/state | PROPOSED |
| ADR-017 | Threat modeling is applicable before security architecture can be marked ready | PROPOSED |

## Decisions intentionally OPEN

These require later explicit ADRs/owner choices:

- `ADR-OPEN-01`: direct DGI vs authorized provider strategy/default;
- `ADR-OPEN-02`: production certificate/private-key custody;
- `ADR-OPEN-03`: final EF Core MySQL provider/package/version;
- `ADR-OPEN-04`: negative-stock/backorder policy;
- `ADR-OPEN-05`: credit-limit/overpayment/advance policy;
- `ADR-OPEN-06`: initial PPP/FIFO enablement scope;
- `ADR-OPEN-07`: exact export-of-services documentation strategy/default;
- `ADR-OPEN-08`: retention/RPO/RTO/SLO operational values.

An OPEN product policy does not invalidate module boundaries, but any implementation slice dependent on that policy remains blocked until resolved.
