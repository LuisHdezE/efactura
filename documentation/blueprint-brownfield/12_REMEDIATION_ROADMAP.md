# Brownfield Remediation Roadmap

No step in this roadmap is authorized by the existence of this document alone.

| Phase | Objective | Prerequisites | Scope | Evidence/verification | Main regression risk |
|---|---|---|---|---|---|
| R0 | Security containment | validated secret exposure | rotate compromised credentials, remove committed values, exposure/history assessment | secret scan, config tests, credential rotation evidence | breaking runtime integrations |
| R1 | Reproducible baseline | Brownfield acceptance | establish clean restore/build/test baseline and resolve existing failing test deliberately | exact-head build/test evidence | hiding real legacy behavior |
| R2 | Runtime/delivery alignment | R1 | align Docker/runtime and chosen CI path to .NET 8 | container build/smoke, CI result | deployment drift |
| R3 | AuthN/AuthZ hardening | target permission model accepted | enforce policies, session/token model, CORS/transport/rate limits | negative/positive security tests | breaking existing anonymous consumers |
| R4 | Model/persistence reconciliation | data usage/DB constraints inventoried | classify/consolidate parallel models; establish persistence boundaries | migration/repository regression | data loss/schema mismatch |
| R5 | Architecture conformance | accepted architecture target | remove framework leakage from core, narrow Shared, add fitness tests | executable architecture tests | large refactor blast radius |
| R6 | Error/API baseline | current consumer impact known | stable error contract, versioning and authoritative OpenAPI | contract/parity tests | external contract break |
| R7 | Audit/idempotency/outbox foundations | target requirements accepted | durable audit, actor context, replay protection and work dispatch | concurrency/replay/audit QA | duplicated/lost effects |
| R8 | Target business modules | Requirements/Architecture/API gates accepted | implement vertical slices by approved domain priority | domain/API/persistence tests | scope explosion |
| R9 | Fiscal integration | regulatory/provider decisions accepted | CFE/CAE/DGI/provider/signing/contingency/reporting | official test/homologation evidence | fiscal non-compliance |
| R10 | Client-facing readiness | API Gate accepted | prepare data/contracts for web/mobile including offline sync | integration/offline QA | unstable API-client coupling |

## Human checkpoints

Each phase ends with explicit review before merge. R8/R9 must be decomposed into smaller vertical slices; no mega-refactor is permitted.

The expanded product target currently lives separately and must pass Blueprint Requirements, Architecture/Security/Data and API Contract boundaries before production implementation begins.
