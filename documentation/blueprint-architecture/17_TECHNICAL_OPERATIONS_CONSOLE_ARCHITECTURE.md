# Architecture Amendment: Technical Operations Console

## Status

`READY_FOR_REVIEW / ADDITIVE ARCHITECTURE AMENDMENT`

This amendment adds a technical observability capability to the accepted Architecture/Security/Data boundary. It does not move business/fiscal rules out of ApplicationCore and does not convert technical logs into business audit.

## Architectural role

The Technical Operations Console is an operational/support surface over canonical observability contracts.

```text
Future Web Console
        |
        | HTTPS + JWT + operations.* permissions
        v
/api/v1/operations/**
        |
        v
Operations Application Boundary
        |
        +--> IObservabilityQueryPort
        +--> IHealthSnapshotPort
        +--> ITraceTimelinePort
        +--> IOperationalMetricsPort
        +--> IWorkerMonitorPort
        +--> IQueueMonitorPort
        +--> IIntegrationMonitorPort
        +--> ISyncMonitorPort
        +--> IOperationalAlertStore
        +--> IOperationalActionPort
        +--> IDiagnosticBundlePort
        |
        v
Infrastructure adapters
        +--> Application Insights / configured telemetry backend
        +--> structured Serilog-compatible log store/search adapter
        +--> ASP.NET Health Checks
        +--> PostgreSQL/MySQL health adapter
        +--> Redis health adapter
        +--> Outbox/Inbox/job persistence adapters
        +--> DGI/provider integration telemetry adapter
        +--> artifact/storage health adapter
```

The browser never receives telemetry credentials and never queries Application Insights, filesystem log files, databases, Redis or DGI/provider APIs directly.

## Boundary placement

This capability is technical/application infrastructure, not a new fiscal/business Domain aggregate.

- canonical query/result DTOs may live under an Operations application namespace;
- adapter interfaces are owned by the inward application boundary;
- provider-specific query languages/SDK DTOs stay in Infrastructure;
- WebApi enforces authentication, permission, scope, rate/bounds and Problem Details;
- durable audit remains owned by Audit and is only referenced through authorized links/queries.

ApplicationCore/Domain must not depend directly on Serilog, Application Insights, Azure SDKs or raw log-file formats.

## Canonical models

### `OperationalHealthSnapshot`

Contains safe status only:

- overall state;
- API/runtime version;
- active database provider name (`PostgreSQL` or `MySQL`), never connection data;
- dependency component name;
- `HEALTHY/DEGRADED/UNHEALTHY/UNKNOWN`;
- checkedAt;
- bounded sanitized diagnostic code/message;
- optional latency.

### `TechnicalEventSummary`

Normalized event projection independent of telemetry vendor:

- eventId/reference;
- timestampUtc;
- severity;
- stable technical event code;
- message summary;
- module/component;
- operationId;
- HTTP method/route template/status when applicable;
- correlationId/traceId/parentId;
- actor/company/location/terminal/device references only when policy allows;
- retry/job/outbox/inbox/integration references when applicable;
- sanitized exception category;
- source backend metadata limited to support-safe fields.

### `TraceTimeline`

Ordered causal projection across request/application/worker/integration boundaries. It may combine normalized telemetry from multiple sources but does not fabricate missing spans.

### `OperationalMetricSeries`

Bounded aggregated measurements, never unbounded high-cardinality business labels.

### `OperationalWorkItemStatus`

Represents Outbox/Inbox/job state without exposing raw business payload:

- workItemId;
- type/category;
- state;
- attempts;
- nextAttemptAt;
- created/updated timestamps;
- correlation/causation references;
- last sanitized error category;
- retryEligible;
- reason when not eligible.

### `OperationalAlert`

Technical/support alert generated from authoritative health/metric/state rules. Examples:

- fiscal provider degraded;
- Outbox backlog threshold;
- worker stopped/stuck;
- database/cache degradation;
- sync conflict surge;
- CAE approaching expiry/exhaustion.

Alerts are not fiscal/business state themselves.

## Query isolation and performance

Monitoring must not compete destructively with transactional POS/fiscal operations.

Rules:

1. every event/log query requires bounded time range and pagination/cursor;
2. default ranges are short; broad historical searches may use asynchronous query/export jobs;
3. expensive metrics are read from telemetry aggregation/projections where possible, not calculated by scanning transactional tables on every page load;
4. DB operational queries use read-only bounded projections;
5. telemetry backend outages must not fail core transaction processing;
6. Console may report `PARTIAL_DATA` when one observability source is unavailable;
7. production runtime must not rely on local rolling files as the sole searchable operational source.

## Provider independence

`Application Insights` is an observed/current allowed sink, not the public Console contract.

Infrastructure may later implement:

- `ApplicationInsightsObservabilityAdapter`;
- another centralized log/trace backend adapter;
- controlled local/on-prem support adapter where appropriate.

Changing telemetry backend requires adapter/configuration work, not frontend/API redesign.

## Security model

Proposed permissions:

- `operations.monitor`
- `operations.traces.read`
- `operations.metrics.read`
- `operations.queues.read`
- `operations.integrations.read`
- `operations.alerts.read`
- `operations.alerts.acknowledge`
- `operations.retry`
- `operations.diagnostics.export`

`operations.monitor` does not imply `audit.read`, `fiscal.read`, `security.manage_roles` or access to secrets.

Object/scope checks remain applicable where telemetry references company/location/device resources.

## Redaction boundary

Sanitization occurs before normalized observability DTOs cross the API boundary. The browser must not be trusted to hide secrets after receipt.

The adapter/application layer removes or masks:

- authorization headers/tokens;
- passwords/secrets/API keys;
- connection strings;
- private keys/certificate passwords;
- unrestricted request/response bodies;
- raw signed fiscal XML;
- unnecessary PII;
- raw offline command payloads.

## Operational actions

The Console is read-only by default.

Privileged actions are commands, not direct persistence edits.

### Alert acknowledgement

Records operator acknowledgement/comment/time. It does not delete the underlying technical event.

### Retry eligible asynchronous work

`operations.retry` submits a retry request to the canonical worker/application pathway.

Invariants:

- work item must be in a retry-eligible state;
- original idempotency/business identity is preserved;
- retry cannot allocate a new fiscal identity or duplicate financial/stock effects merely because an operator clicked Retry;
- request reason is required;
- action is durably audited;
- success means retry was accepted/processed according to worker semantics, not that DGI necessarily accepted the CFE.

No generic “execute SQL”, “edit queue row” or “force status” operation exists.

## Durable audit linkage

Technical events and audit may share:

- correlationId;
- actor;
- aggregate/resource reference;
- operationId;
- idempotency/client operation identity.

The Console may expose a related-audit reference when the caller also has the required audit permission. It must not merge or duplicate audit records into the technical log store.

## Diagnostic bundle

Authorized asynchronous export may include:

- selected sanitized technical events;
- correlation timeline;
- dependency health snapshots;
- operational metric summary;
- safe queue/job metadata;
- application/version/environment metadata.

Excluded by default:

- secrets;
- raw connection configuration;
- full customer datasets;
- unrestricted fiscal XML;
- private keys/certificates;
- arbitrary database dumps.

Export creation/access is itself durably audited and uses short-lived protected artifact delivery.

## Availability semantics

Observability is important but must fail independently.

If the telemetry backend is unavailable:

- transactional API remains available when its business dependencies are healthy;
- monitoring API returns canonical degraded/partial-data semantics;
- a telemetry outage is itself surfaced through health mechanisms where possible;
- business audit persistence must not be silently replaced by technical telemetry.

## Client architecture implication

Later Web Client Architecture will treat this as a dedicated administrative slice. The browser receives normalized models and support-safe correlation IDs only. Dense event/trace exploration is desktop-first; responsive summaries remain available on tablet/mobile web.

## Implementation-conformance evidence later

API Implementation/QA must prove:

- operations endpoints never expose raw telemetry credentials;
- nested secret redaction occurs server-side;
- bounded query enforcement;
- 401/403 permission tests;
- telemetry provider outage does not break ordinary business endpoints;
- trace correlation can follow HTTP -> Outbox -> worker -> integration;
- retry preserves original idempotency/fiscal identity semantics;
- retry/diagnostic export actions create durable audit events;
- PostgreSQL and MySQL deployments expose equivalent canonical monitoring DTOs.
