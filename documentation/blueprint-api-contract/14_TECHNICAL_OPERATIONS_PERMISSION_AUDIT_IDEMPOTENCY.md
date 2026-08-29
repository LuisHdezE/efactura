# Technical Operations: Permission, Audit and Idempotency Mapping

## Permission model

The Technical Operations Console introduces a dedicated permission family. These permissions do not imply fiscal, accounting, customer-data, security-administration or durable-audit access.

| Permission | Intent |
|---|---|
| `operations.monitor` | Read overview, health, sanitized events, dependency/worker/sync summaries. |
| `operations.traces.read` | Read sanitized cross-component trace timelines. |
| `operations.metrics.read` | Read curated metrics and time series. |
| `operations.queues.read` | Read safe Outbox/Inbox/job/work-item metadata. |
| `operations.integrations.read` | Read safe integration operational state. |
| `operations.alerts.read` | Read operational alerts. |
| `operations.alerts.acknowledge` | Acknowledge an alert with comment/context. |
| `operations.retry` | Request retry of an eligible asynchronous work item through the canonical pathway. |
| `operations.diagnostics.export` | Create/read/download bounded sanitized diagnostic bundles. |

## Role examples

Roles are compositions, not enforcement primitives.

### Technical Operator

Typical permissions:

- `operations.monitor`;
- `operations.traces.read`;
- `operations.metrics.read`;
- `operations.queues.read`;
- `operations.integrations.read`;
- `operations.alerts.read`.

No mutation permission by default.

### Technical Administrator

May add:

- `operations.alerts.acknowledge`;
- `operations.retry`;
- `operations.diagnostics.export`.

Does not automatically receive `fiscal.*`, `audit.read`, `security.manage_roles` or customer/business administration permissions.

### Support Analyst

Reduced least-privilege profile, typically:

- `operations.monitor`;
- optionally `operations.traces.read` within approved scope.

Queue, integration and metric access may be withheld.

## Scope enforcement

Where telemetry carries company/location/terminal/device references, server policy filters or rejects access according to the actor's authorized operational scope.

Knowing a correlation ID, work-item ID, alert ID or trace ID is never sufficient authorization.

If a technical event links to a durable audit record, `audit.read` is independently required before the API includes usable audit references or audit content.

## Operation-to-permission matrix

| operationId | Required permission |
|---|---|
| `getOperationsOverview` | `operations.monitor` |
| `getOperationsHealth` | `operations.monitor` |
| `listTechnicalEvents` | `operations.monitor` |
| `getTechnicalEvent` | `operations.monitor` |
| `getTraceTimeline` | `operations.traces.read` |
| `getOperationalMetrics` | `operations.metrics.read` |
| `getOperationalMetricSeries` | `operations.metrics.read` |
| `listOperationalDependencies` | `operations.monitor` |
| `listOperationalIntegrations` | `operations.integrations.read` |
| `getOperationalIntegration` | `operations.integrations.read` |
| `listOperationalQueues` | `operations.queues.read` |
| `listOperationalWorkItems` | `operations.queues.read` |
| `getOperationalWorkItem` | `operations.queues.read` |
| `requestOperationalRetry` | `operations.retry` |
| `listOperationalWorkers` | `operations.monitor` |
| `getOperationalSyncOverview` | `operations.monitor` |
| `listOperationalAlerts` | `operations.alerts.read` |
| `getOperationalAlert` | `operations.alerts.read` |
| `acknowledgeOperationalAlert` | `operations.alerts.acknowledge` |
| `createDiagnosticBundle` | `operations.diagnostics.export` |
| `getDiagnosticBundle` | `operations.diagnostics.export` |
| `downloadDiagnosticBundle` | `operations.diagnostics.export` |

## Durable audit mapping

Read-only monitoring normally remains technical observability, not a business-audit event for every page view. Sensitive access may be security-audited by deployment policy, but the following commands are REQUIRED durable audit events:

| operationId / worker outcome | Durable audit event |
|---|---|
| `acknowledgeOperationalAlert` | `operations.alert.acknowledged` |
| `requestOperationalRetry` | `operations.retry.requested` |
| retry worker accepted/started | `operations.retry.started` |
| retry worker canonical completion | `operations.retry.completed` |
| retry worker canonical failure | `operations.retry.failed` |
| `createDiagnosticBundle` | `operations.diagnostic_export.requested` |
| bundle generation completion/failure | `operations.diagnostic_export.generated|failed` |
| `downloadDiagnosticBundle` | `operations.diagnostic_export.accessed` |

Required audit context includes:

- actor;
- operational target ID/type;
- reason/comment where applicable;
- company/deployment scope;
- correlation ID;
- idempotency key/reference for commands;
- original work-item correlation/causation identity for retry;
- outcome;
- timestamps.

No secret-bearing telemetry payload is copied blindly into audit.

## Idempotency matrix

| operationId | Requirement | Semantic identity |
|---|---|---|
| `requestOperationalRetry` | REQUIRED `Idempotency-Key` | actor scope + target workItemId + normalized retry request |
| `acknowledgeOperationalAlert` | REQUIRED `Idempotency-Key` | actor scope + alertId + acknowledgement request |
| `createDiagnosticBundle` | REQUIRED `Idempotency-Key` | actor scope + normalized bounded bundle request |
| all Technical Operations GETs | N/A | safe/read-only |

Rules:

1. same key + same normalized request returns the previously committed canonical result;
2. same key + materially different request returns `409 idempotency_key_reused`;
3. retry idempotency does not replace the original business operation idempotency. It references and preserves it;
4. multiple technical retry requests must never create a new CFE identity, duplicate payment, duplicate stock effect or bypass business state-machine constraints;
5. operational commands participate in the same central idempotency infrastructure as other v1 retry-sensitive commands.

## Concurrency/state conflicts

Representative conflicts:

- alert already acknowledged/resolved under a policy that forbids the requested transition;
- work item moved from retry-eligible to processing/resolved before retry command commits;
- diagnostic bundle already expired before artifact access;
- requested telemetry time window is no longer available because Class-D retention expired.

State conflicts use canonical Problem Details and never mutate underlying data to make the command succeed.

## Redaction and authorization tests required later

API QA must prove at minimum:

- no `operations.*` permission -> 403;
- correlation/work-item/alert IDs cannot cross company/deployment scope;
- `operations.monitor` alone cannot use retry, acknowledge or diagnostic export;
- `operations.monitor` alone cannot retrieve durable audit content;
- nested tokens/connection strings/private-key material do not appear in event/trace/diagnostic responses;
- retry maintains original idempotency/fiscal identity;
- privileged commands create the expected durable audit events.
