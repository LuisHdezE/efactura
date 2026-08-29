# API v1 Endpoint Inventory: Technical Operations Console

## Scope

This inventory reconciles the accepted Technical Operations Console amendment into the public v1 contract. These operations expose normalized, sanitized operational data only. They never expose raw log files, telemetry credentials, connection strings, private keys, unrestricted fiscal XML, raw offline payloads or arbitrary infrastructure consoles.

Base path: `/api/v1/operations`.

The Console is read-only by default. Only alert acknowledgement, retry request and diagnostic-bundle creation are mutating operational commands.

## Endpoint inventory

| API ID | Method | Route | operationId | Permission | Idempotency | Purpose |
|---|---|---|---|---|---|---|
| `API-172` | GET | `/api/v1/operations/overview` | `getOperationsOverview` | `operations.monitor` | N/A | Compact system state, active DB provider, dependency summary, error/backlog/alert headline data. |
| `API-173` | GET | `/api/v1/operations/health` | `getOperationsHealth` | `operations.monitor` | N/A | Canonical dependency health snapshot with safe status/latency only. |
| `API-174` | GET | `/api/v1/operations/events` | `listTechnicalEvents` | `operations.monitor` | N/A | Bounded sanitized technical-event search. |
| `API-175` | GET | `/api/v1/operations/events/{eventId}` | `getTechnicalEvent` | `operations.monitor` | N/A | Sanitized normalized technical-event detail. |
| `API-176` | GET | `/api/v1/operations/traces/{correlationId}` | `getTraceTimeline` | `operations.traces.read` | N/A | Ordered request/application/outbox/worker/integration timeline using observed evidence only. |
| `API-177` | GET | `/api/v1/operations/metrics` | `getOperationalMetrics` | `operations.metrics.read` | N/A | Bounded aggregated operational metric set. |
| `API-178` | GET | `/api/v1/operations/metrics/{metricKey}` | `getOperationalMetricSeries` | `operations.metrics.read` | N/A | Time series for one allow-listed metric key. |
| `API-179` | GET | `/api/v1/operations/dependencies` | `listOperationalDependencies` | `operations.monitor` | N/A | Safe runtime dependency status, including DB/cache/storage/telemetry where configured. |
| `API-180` | GET | `/api/v1/operations/integrations` | `listOperationalIntegrations` | `operations.integrations.read` | N/A | Sanitized external-integration health/latency/retry summaries. |
| `API-181` | GET | `/api/v1/operations/integrations/{integrationKey}` | `getOperationalIntegration` | `operations.integrations.read` | N/A | Detailed safe operational state for one configured integration such as fiscal transport. |
| `API-182` | GET | `/api/v1/operations/queues` | `listOperationalQueues` | `operations.queues.read` | N/A | Outbox/Inbox/job queue counts and state summaries. |
| `API-183` | GET | `/api/v1/operations/work-items` | `listOperationalWorkItems` | `operations.queues.read` | N/A | Bounded work-item search by type/state/time/correlation without raw business payload. |
| `API-184` | GET | `/api/v1/operations/work-items/{workItemId}` | `getOperationalWorkItem` | `operations.queues.read` | N/A | Safe work-item detail, retry eligibility and sanitized last-error category. |
| `API-185` | POST | `/api/v1/operations/work-items/{workItemId}/retry` | `requestOperationalRetry` | `operations.retry` | REQUIRED | Submit a retry request through the canonical worker pathway while preserving original business/idempotency identity. |
| `API-186` | GET | `/api/v1/operations/workers` | `listOperationalWorkers` | `operations.monitor` | N/A | Worker heartbeat/state/failure/retry summary. |
| `API-187` | GET | `/api/v1/operations/sync` | `getOperationalSyncOverview` | `operations.monitor` | N/A | Offline-sync throughput/conflict/review/freshness summary without queued payload exposure. |
| `API-188` | GET | `/api/v1/operations/alerts` | `listOperationalAlerts` | `operations.alerts.read` | N/A | Bounded operational-alert query. |
| `API-189` | GET | `/api/v1/operations/alerts/{alertId}` | `getOperationalAlert` | `operations.alerts.read` | N/A | One operational alert with source/rule/status and safe context. |
| `API-190` | POST | `/api/v1/operations/alerts/{alertId}/acknowledge` | `acknowledgeOperationalAlert` | `operations.alerts.acknowledge` | REQUIRED | Record acknowledgement/comment without deleting alert/source evidence. |
| `API-191` | POST | `/api/v1/operations/diagnostics/bundles` | `createDiagnosticBundle` | `operations.diagnostics.export` | REQUIRED | Create bounded asynchronous sanitized support bundle. |
| `API-192` | GET | `/api/v1/operations/diagnostics/bundles/{bundleId}` | `getDiagnosticBundle` | `operations.diagnostics.export` | N/A | Read bundle generation/status/expiry metadata. |
| `API-193` | GET | `/api/v1/operations/diagnostics/bundles/{bundleId}/artifact` | `downloadDiagnosticBundle` | `operations.diagnostics.export` | N/A | Authorized download of generated sanitized bundle artifact. |

## Query bounds

`listTechnicalEvents`, work-item searches and alert searches require bounded server-enforced windows/cursors. Defaults are intentionally short. The API rejects unbounded scans with canonical Problem Details.

Baseline limits for implementation review:

- synchronous technical-event search default: last 15 minutes;
- synchronous technical-event maximum time range: 24 hours per request;
- maximum page size: 200 events/work-items unless a stricter deployment limit is configured;
- metric queries require an allow-listed `metricKey`, bounded range and aggregation interval;
- broad historical/incident extraction uses `createDiagnosticBundle` rather than an unrestricted browser query.

These limits may be tightened by deployment policy without changing business semantics. Relaxing them beyond safe performance/security boundaries requires explicit change review.

## Related audit references

`TechnicalEventSummary` and trace items may include safe `relatedAuditReferences` only when evidence can be linked and the caller also possesses `audit.read`. Lack of `audit.read` omits those references rather than leaking audit existence/details.

There is no operation that merges, edits or deletes durable audit from the Technical Operations Console.

## Partial-data semantics

Read operations may return a canonical `dataCompleteness` state such as `COMPLETE`, `PARTIAL` or `UNAVAILABLE` per source. Telemetry-source degradation does not imply business API failure. Missing Class-D telemetry must not be represented as absence of required Classes A-C durable audit.

## Explicitly forbidden public operations

No v1 operation exists for:

- raw `.log` file download/browse;
- arbitrary Kusto/Application Insights query execution;
- arbitrary SQL/Redis command execution;
- editing/deleting Outbox/Inbox/job rows;
- forcing fiscal/business status;
- deleting technical or durable audit evidence;
- exposing provider credentials, connection strings or certificate material.
