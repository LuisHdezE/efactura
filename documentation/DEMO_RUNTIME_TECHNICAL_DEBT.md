# Demo Runtime Technical Debt

Status: TRACKED FOLLOW-UP

Recorded: 2026-09-16

Context: eFactura Cloud Run demo-readiness work. These items are intentionally recorded so they are not lost while the immediate deployment lane continues. They are not claims that the affected capabilities are currently broken in every flow; they identify bounded runtime/portability debt that must be revisited deliberately.

## TD-DEMO-001 — Redis dependency should be optional for demo runtime

### Current state

`src/WebApi/Program.cs` currently registers `AddStackExchangeRedisCache(...)` and binds `ICacheService` to `RedisDistributedCacheService` unconditionally.

The repository already contains an `InMemoryCache` implementation of `ICacheService`. The validated V1 production-like path `GET /api/v1/parties` completed successfully without a live Redis server, which indicates Redis is not a startup or execution requirement for that demonstrated V1 flow.

### Debt / risk

Keeping Redis as an unconditional runtime dependency creates unnecessary deployment complexity and may introduce avoidable infrastructure cost or failure modes for the demo even when the exercised V1 endpoints do not need distributed caching.

### Follow-up direction

Evaluate and implement explicit cache-mode composition so the demo can run with an in-memory cache when distributed caching is not required, while preserving Redis as an intentional option for flows that genuinely need it.

### Exit criteria

- Demo runtime can start without a Redis service or Redis connection string when configured for in-memory caching.
- Existing flows that genuinely require distributed caching remain explicitly supported and tested.
- Cache selection is external configuration, not hard-coded environment guessing.

---

## TD-DEMO-002 — `System.Drawing` is a Linux / Cloud Run portability risk

### Current state

Legacy barcode/QR utility code under `src/ApplicationCore/Utilities/Barcode/Barcode.cs` uses `System.Drawing` / `System.Drawing.Common`. The repository already records this as Windows-only modernization debt.

The validated production-like `GET /api/v1/parties` path does not exercise that utility, so the current successful Linux-oriented Cloud Run rehearsal does not prove barcode-related flows are portable.

### Debt / risk

`System.Drawing.Common` is not a general-purpose cross-platform graphics dependency for modern .NET server workloads. Any endpoint or workflow that reaches this legacy barcode path may fail or behave differently in a Linux container even though unrelated endpoints work correctly.

### Follow-up direction

Before exposing barcode-dependent functionality in the Cloud Run demo, identify every reachable caller of the legacy utility and either:

- replace the implementation with a supported cross-platform graphics/barcode path; or
- explicitly exclude and document those flows from the Linux-hosted demo until replacement is complete.

### Exit criteria

- All Cloud Run-exposed barcode/QR flows have Linux-container tests.
- No exposed demo endpoint depends on unsupported Windows-only drawing behavior.
- Any replacement remains isolated behind the appropriate application/infrastructure boundary rather than leaking graphics-library details into Domain logic.

## Scope note

These debts should not block unrelated V1 endpoints that have already been demonstrated end-to-end on the production-like runtime. They remain tracked so the deployment lane can proceed without silently forgetting infrastructure and portability work that must be closed before claiming broader Cloud Run compatibility.
