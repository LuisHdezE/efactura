# Interface Scope Amendment: Technical Operations Console

## Status

`READY_FOR_REVIEW / SCOPE_BASELINE AMENDMENT`

This document extends the accepted Interface Scope Baseline with one proposed Web interface. It is additive and does not alter previously accepted Web/Android scope items.

## WEB-019 — Technical Operations Console

- **Platform:** web
- **Module/capability:** Operations / Observability
- **Source classification:** PROPOSED
- **Implementation status:** PROPOSED
- **Requirements:** FR-087..FR-098, NFR-017..NFR-021, BR-016..BR-020
- **Primary roles:** Technical Operator, Technical Administrator, Support Analyst
- **Purpose:** provide a secure operational view of logs, traces, metrics, health, dependencies, background workers, queues and integration state for diagnosis/support without direct infrastructure access.

### Authoritative data

The UI consumes canonical server observability contracts. It never reads local server log files, Application Insights credentials, databases, Redis, Outbox/Inbox tables or provider APIs directly.

Required data groups:

1. runtime/API health summary;
2. dependency health and active database provider;
3. technical-event search results;
4. correlation/trace timeline;
5. request-rate/error-rate/latency metrics;
6. fiscal-integration latency/failure/retry state;
7. Outbox/Inbox/background-job state;
8. offline sync operational metrics/conflicts;
9. CAE/integration operational alerts;
10. safe related-audit references where authorized;
11. bounded diagnostic export job/status where enabled.

### Proposed sections/views

- **Overview:** service state, dependency state, current alert summary and key operational metrics.
- **Events:** filterable technical event explorer with severity/time/module/operation/status filters.
- **Trace Explorer:** timeline for a `CorrelationId` / trace identity including asynchronous causation.
- **API Performance:** operation-level request volume, latency distribution and error-rate views.
- **Fiscal Integration:** DGI/provider health, latency, retries, current degradation and transport backlog.
- **Queues & Workers:** Outbox, Inbox, workers, retries, stuck/dead-letter state.
- **Offline Sync:** batch throughput, device freshness, conflicts and review-required counts.
- **Alerts:** active/acknowledged operational alerts including CAE expiry/exhaustion and dependency degradation.
- **Diagnostic Export:** authorized generation/download of bounded sanitized incident evidence.

These are logical views. Final routing, visual layout and responsive composition belong to later Interface Inventory/Design System/Client Architecture phases.

### Actions

Read-only by default:

- inspect system/dependency status;
- search events;
- open correlation timeline;
- filter metrics;
- inspect queue/worker state;
- inspect integration/sync state;
- inspect safe related-audit reference.

Privileged proposed actions:

- acknowledge operational alert;
- request retry of an eligible failed/dead-letter asynchronous item;
- request sanitized diagnostic export.

Forbidden UI actions:

- edit/delete technical log records;
- edit/delete durable audit;
- manipulate fiscal/business rows directly;
- reveal secret configuration values;
- force CFE/CAE/business state from an observability screen;
- bypass the canonical idempotent retry workflow.

### States

- default
- loading
- empty
- filtered_empty
- degraded
- partial_data
- retention_expired
- unauthorized/403
- rate_limited/429
- dependency_error
- export_pending
- export_ready
- retry_pending
- retry_failed

The UI must distinguish `NO EVENTS IN RANGE`, `TELEMETRY EXPIRED/UNAVAILABLE` and `DEPENDENCY DEGRADED`; these are not equivalent states.

### Security/accessibility

- server-side permissions are authoritative;
- sensitive fields are recursively redacted before reaching browser code;
- correlation IDs are identifiers, not credentials;
- status/severity must never depend on color alone;
- event tables and trace timelines require keyboard/screen-reader semantics;
- technical detail may be expandable but the core incident state must remain understandable without reading raw JSON.

### Responsive expectation

Desktop-first because of dense operational tables/timelines. Tablet receives a usable inspection/alert mode. Mobile web may provide summary, search by correlation ID and alert inspection, while dense trace analysis can remain desktop-oriented.

## Unresolved API needs introduced

API Contract amendment must define, at minimum:

- operations overview;
- dependency/health details;
- sanitized technical-event search;
- correlation/trace timeline;
- operation metrics;
- integration metrics/state;
- queue/worker state;
- sync monitoring;
- alerts/acknowledgement;
- eligible retry command/status;
- diagnostic export create/status/download.

No route or `operationId` is invented in this scope amendment. Those become authoritative only in API Contract Design.

## Brownfield note

Current Serilog rolling files and Application Insights are observed inputs to target observability, not browser-facing contracts. The Console must use adapters so cloud/on-premises deployments can change telemetry backends without changing the web contract.
