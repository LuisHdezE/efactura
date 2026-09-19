# API Completion Master Matrix — Wave 7

Status: `FULL_OPERATION_LEVEL_RECONCILED / CONTRACT_COLLISION_EXPOSED`

Parent index: `documentation/API_COMPLETION_MASTER_MATRIX.md`.

Scope: **Technical Operations Console**.

Baseline: **30 operation IDs**, **0 implemented**, **30 non-implemented**. Of those, **2 IDs are blocked by one accepted-contract route collision**.

Known collision: `API-MON-001` and `API-180` both declare `GET /api/v1/operations/integrations` with different operationIds and permissions. Neither is silently selected, removed, renamed or renumbered by this matrix.

| API ID | operationId | Method / path | Permission | Contract | Implementation | Current WebApi evidence | Deep readiness | Wave | Gap / blocker |
|---|---|---|---|---|---|---|---|---:|---|
| `API-SYS-001` | `getHealth` | GET `/api/v1/health` | `PUBLIC` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Public safe health surface required |
| `API-SYS-002` | `getVersion` | GET `/api/v1/version` | `PUBLIC` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Minimal version metadata surface required |
| `API-ALT-001` | `listAlerts` | GET `/api/v1/alerts` | `alerts.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Scoped actionable-alert surface required |
| `API-ALT-002` | `acknowledgeAlert` | POST `/api/v1/alerts/{alertId}/acknowledge` | `alerts.manage` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Alert acknowledgement command required |
| `API-MON-001` | `getIntegrationStatus` | GET `/api/v1/operations/integrations` | `operations.read` | ACCEPTED / COLLIDES_WITH_API-180 | CONTRACT_COLLISION | none | BLOCKED_BY_CONTRACT | 7 | Same method/path as API-180; contract reconciliation required before implementation |
| `API-INT-001` | `listIntegrations` | GET `/api/v1/integrations` | `integrations.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Safe integration-adapter metadata surface required |
| `API-INT-002` | `getIntegration` | GET `/api/v1/integrations/{integrationId}` | `integrations.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Safe integration detail surface required |
| `API-INT-003` | `updateIntegration` | PATCH `/api/v1/integrations/{integrationId}` | `integrations.manage` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Non-secret integration settings update required |
| `API-172` | `getOperationsOverview` | GET `/api/v1/operations/overview` | `operations.monitor` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Sanitized operations overview required |
| `API-173` | `getOperationsHealth` | GET `/api/v1/operations/health` | `operations.monitor` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Dependency health snapshot required |
| `API-174` | `listTechnicalEvents` | GET `/api/v1/operations/events` | `operations.monitor` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Bounded technical-event query required |
| `API-175` | `getTechnicalEvent` | GET `/api/v1/operations/events/{eventId}` | `operations.monitor` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Sanitized technical-event detail required |
| `API-176` | `getTraceTimeline` | GET `/api/v1/operations/traces/{correlationId}` | `operations.traces.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Evidence-backed trace timeline required |
| `API-177` | `getOperationalMetrics` | GET `/api/v1/operations/metrics` | `operations.metrics.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Bounded aggregated metrics required |
| `API-178` | `getOperationalMetricSeries` | GET `/api/v1/operations/metrics/{metricKey}` | `operations.metrics.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Allow-listed metric series required |
| `API-179` | `listOperationalDependencies` | GET `/api/v1/operations/dependencies` | `operations.monitor` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Safe dependency-state surface required |
| `API-180` | `listOperationalIntegrations` | GET `/api/v1/operations/integrations` | `operations.integrations.read` | ACCEPTED / COLLIDES_WITH_API-MON-001 | CONTRACT_COLLISION | none | BLOCKED_BY_CONTRACT | 7 | Same method/path as API-MON-001; contract reconciliation required before implementation |
| `API-181` | `getOperationalIntegration` | GET `/api/v1/operations/integrations/{integrationKey}` | `operations.integrations.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Safe operational integration detail required |
| `API-182` | `listOperationalQueues` | GET `/api/v1/operations/queues` | `operations.queues.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Queue count/state summaries required |
| `API-183` | `listOperationalWorkItems` | GET `/api/v1/operations/work-items` | `operations.queues.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Bounded work-item query required |
| `API-184` | `getOperationalWorkItem` | GET `/api/v1/operations/work-items/{workItemId}` | `operations.queues.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Safe work-item detail required |
| `API-185` | `requestOperationalRetry` | POST `/api/v1/operations/work-items/{workItemId}/retry` | `operations.retry` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Canonical retry-request pathway required |
| `API-186` | `listOperationalWorkers` | GET `/api/v1/operations/workers` | `operations.monitor` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Worker heartbeat/state summary required |
| `API-187` | `getOperationalSyncOverview` | GET `/api/v1/operations/sync` | `operations.monitor` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Offline-sync operational overview required |
| `API-188` | `listOperationalAlerts` | GET `/api/v1/operations/alerts` | `operations.alerts.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Bounded operational-alert query required |
| `API-189` | `getOperationalAlert` | GET `/api/v1/operations/alerts/{alertId}` | `operations.alerts.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Operational alert detail required |
| `API-190` | `acknowledgeOperationalAlert` | POST `/api/v1/operations/alerts/{alertId}/acknowledge` | `operations.alerts.acknowledge` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Evidence-preserving acknowledgement required |
| `API-191` | `createDiagnosticBundle` | POST `/api/v1/operations/diagnostics/bundles` | `operations.diagnostics.export` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Bounded sanitized diagnostic-bundle job required |
| `API-192` | `getDiagnosticBundle` | GET `/api/v1/operations/diagnostics/bundles/{bundleId}` | `operations.diagnostics.export` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Diagnostic bundle status/expiry projection required |
| `API-193` | `downloadDiagnosticBundle` | GET `/api/v1/operations/diagnostics/bundles/{bundleId}/artifact` | `operations.diagnostics.export` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 7 | Authorized sanitized bundle artifact download required |
