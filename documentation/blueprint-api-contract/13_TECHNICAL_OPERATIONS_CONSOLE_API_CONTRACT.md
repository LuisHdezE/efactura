# Technical Operations Console API Contract

## Purpose

Define canonical request/response semantics for the Technical Operations Console without coupling clients to Serilog, Application Insights, Azure query syntax, PostgreSQL/MySQL internals, Redis commands or raw log-file formats.

All operations require JWT Bearer authentication plus explicit `operations.*` permission and applicable company/deployment scope.

## Canonical response models

### `OperationalHealthSnapshot`

```json
{
  "overallState": "DEGRADED",
  "checkedAt": "2026-08-29T00:00:00Z",
  "applicationVersion": "1.0.0",
  "activeDatabaseProvider": "PostgreSQL",
  "components": [
    {
      "key": "database",
      "displayName": "Primary database",
      "state": "HEALTHY",
      "latencyMs": 18,
      "diagnosticCode": null,
      "message": null
    }
  ],
  "dataCompleteness": "COMPLETE"
}
```

Never includes hostnames/connection strings, credentials, private endpoint URLs or certificate material unless an explicitly approved support-safe alias is used.

### `TechnicalEventSummary`

```json
{
  "eventId": "evt_...",
  "timestampUtc": "2026-08-29T00:00:00Z",
  "severity": "Warning",
  "eventCode": "integration.fiscal.failed",
  "message": "Fiscal transport attempt failed and will be retried.",
  "module": "Fiscal",
  "component": "FiscalTransportWorker",
  "operationId": "confirmSale",
  "httpStatus": null,
  "correlationId": "...",
  "traceId": "...",
  "parentId": "...",
  "companyId": "...",
  "locationId": null,
  "terminalId": null,
  "deviceId": null,
  "workItemId": "...",
  "attempt": 2,
  "exceptionCategory": "TransportTimeout",
  "relatedAuditReferences": [],
  "source": "centralized-telemetry"
}
```

`message` is already sanitized. No raw provider exception/payload is returned merely because it exists in a sink.

### `TraceTimeline`

```json
{
  "correlationId": "...",
  "traceId": "...",
  "from": "2026-08-29T00:00:00Z",
  "to": "2026-08-29T00:01:00Z",
  "dataCompleteness": "PARTIAL",
  "missingSources": ["telemetry-backend"],
  "items": [
    {
      "timestampUtc": "...",
      "kind": "HTTP",
      "eventCode": "http.request.completed",
      "summary": "POST /api/v1/sales/{saleId}/confirm -> 200",
      "component": "WebApi",
      "operationId": "confirmSale",
      "workItemId": null
    }
  ]
}
```

Timeline is evidence projection, not inference. Missing spans are represented as missing, never fabricated.

### `OperationalWorkItemStatus`

```json
{
  "workItemId": "...",
  "category": "OUTBOX",
  "type": "FiscalDocumentTransportRequested",
  "state": "DEAD_LETTER",
  "attempts": 6,
  "nextAttemptAt": null,
  "createdAt": "...",
  "updatedAt": "...",
  "correlationId": "...",
  "causationId": "...",
  "lastErrorCategory": "ProviderUnavailable",
  "retryEligible": true,
  "retryIneligibleReason": null
}
```

No raw serialized message/business payload is returned.

### `OperationalAlert`

Contains:

- `alertId`;
- stable `alertCode`;
- severity;
- current state (`OPEN`, `ACKNOWLEDGED`, `RESOLVED`);
- source component/domain reference when safe;
- first/last observed timestamps;
- concise sanitized message;
- rule/threshold metadata where safe;
- acknowledgement metadata if present.

Alert state does not replace underlying authoritative fiscal/business state.

## Event search

`GET /api/v1/operations/events`

Supported bounded filters:

- `from`, `to`;
- `severity`;
- `eventCode`;
- `module`;
- `component`;
- `operationId`;
- `httpStatus`;
- `correlationId`;
- `traceId`;
- safe `companyId/locationId/terminalId/deviceId` scope when permitted;
- cursor/page size.

At least one bounded time window is resolved by the server. The client cannot request `all history` synchronously.

Sort is newest-first by default; chronological trace reconstruction uses the trace endpoint.

## Metrics

`GET /api/v1/operations/metrics` returns a curated overview. `GET /metrics/{metricKey}` supports only allow-listed metrics such as:

- `http.request.rate`;
- `http.error.rate`;
- `http.latency`;
- `fiscal.integration.latency`;
- `fiscal.integration.failure_rate`;
- `outbox.backlog`;
- `outbox.dead_letter`;
- `worker.failure_rate`;
- `sync.conflict_rate`;
- `sync.review_required`;
- `dependency.health`.

Arbitrary telemetry-language expressions are forbidden.

## Integration status

Integration responses expose canonical state:

`HEALTHY | DEGRADED | UNAVAILABLE | UNKNOWN`

plus safe fields such as last success time, latency summary, retry/backlog state and sanitized error category. They do not expose endpoint credentials, request signatures or provider secrets.

A fiscal integration being `UNAVAILABLE` does not automatically mean an already committed sale failed.

## Retry command

`POST /api/v1/operations/work-items/{workItemId}/retry`

Headers:

- `Authorization: Bearer ...`;
- `Idempotency-Key: ...` REQUIRED;
- `X-Correlation-Id` supported.

Request:

```json
{
  "reason": "Provider incident resolved; retry requested after validation."
}
```

Rules:

1. `reason` is mandatory and bounded;
2. work item must exist and be retry eligible;
3. caller requires `operations.retry` and resource scope;
4. retry re-enters canonical worker/application flow;
5. original business/idempotency/fiscal identity remains authoritative;
6. a second identical retry command with the same idempotency key returns the original canonical response;
7. same key + different request returns `409 idempotency_key_reused`;
8. retry-ineligible state returns `409 operational_retry_not_eligible`;
9. accepted request returns `202 Accepted` with a retry request/reference, not a false claim that DGI/business processing succeeded;
10. durable audit records request and eventual outcome.

The endpoint cannot force `ACCEPTED`, allocate a new CFE number, modify a queue row or bypass normal authorization/state machines.

## Alert acknowledgement

`POST /api/v1/operations/alerts/{alertId}/acknowledge`

Request:

```json
{
  "comment": "Incident acknowledged by on-call support."
}
```

Requires `operations.alerts.acknowledge` and `Idempotency-Key`.

Acknowledgement does not resolve the source condition automatically and does not delete technical evidence.

## Diagnostic bundles

### Create

`POST /api/v1/operations/diagnostics/bundles`

Request includes bounded incident scope, for example:

```json
{
  "from": "2026-08-28T23:00:00Z",
  "to": "2026-08-29T00:00:00Z",
  "correlationIds": ["..."],
  "includeHealth": true,
  "includeMetrics": true,
  "includeWorkItemMetadata": true,
  "reason": "Support case INC-1234"
}
```

Server allow-lists included evidence and performs recursive redaction. Client cannot request unrestricted database dump, secrets, complete customer data or raw signed fiscal XML.

Returns `202 Accepted` + `bundleId` + status link.

### Status

`GET /api/v1/operations/diagnostics/bundles/{bundleId}`

States:

`QUEUED | GENERATING | READY | FAILED | EXPIRED`

### Download

`GET /api/v1/operations/diagnostics/bundles/{bundleId}/artifact`

Requires current authorization on every access. Artifacts are protected, short-lived according to operational policy, and download is durably auditable. The API does not return permanent public URLs.

## Related audit references

Technical-event/trace projections can reveal only safe audit references when caller also has `audit.read`. To view audit content, normal Audit API authorization still applies.

Operations monitoring does not grant audit content access.

## Error contract

All JSON errors use the accepted RFC 9457 Problem Details contract.

Representative stable codes:

- `operations_time_range_required`;
- `operations_time_range_too_large`;
- `operations_metric_not_allowed`;
- `operations_source_unavailable`;
- `operational_work_item_not_found`;
- `operational_retry_not_eligible`;
- `operational_alert_not_found`;
- `diagnostic_bundle_not_found`;
- `diagnostic_bundle_not_ready`;
- `diagnostic_bundle_expired`;
- standard `forbidden`, `rate_limited`, `idempotency_key_reused`.

Telemetry-source outage may produce a successful `PARTIAL` response when useful data remains. Use `503` only when the requested monitoring result cannot be safely served at all, never to signal unrelated transaction failure.

## Security and privacy invariant

`server-side sanitized projection -> browser`

Never:

`raw telemetry -> browser -> hide sensitive fields in React`
