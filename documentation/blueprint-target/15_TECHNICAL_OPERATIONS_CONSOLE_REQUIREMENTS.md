# Requirements Amendment: Technical Operations Console

## Status

`READY_FOR_REVIEW / ADDITIVE REQUIREMENTS AMENDMENT`

This amendment extends the accepted eFactura target with a web-based **Technical Operations Console**. It does not reopen or invalidate previously accepted fiscal/business requirements. It adds an operational observability capability required to diagnose and supervise the running system.

The console is **not** a replacement for durable business/security audit. Technical observability and durable audit remain separate authorities with explicit linkage through correlation identifiers when useful.

## Product intent

Provide authorized technical/support personnel with one web tool to understand system health and reconstruct operational incidents without direct filesystem/database access and without exposing secrets or raw unrestricted production payloads.

The console must answer questions such as:

- Is the API healthy now?
- Which endpoints are failing or slow?
- What happened to correlation ID X?
- Is DGI/the configured fiscal provider degraded?
- Are PostgreSQL/MySQL, Redis, artifact storage or background workers degraded?
- Are Outbox/Inbox messages stuck, retrying or dead-lettered?
- Are offline synchronization batches accumulating conflicts?
- Are CAE expiry/exhaustion conditions creating operational risk?
- Which technical errors occurred in a selected time range/module/operation?
- Which durable audit event, if any, is related to this technical incident?

## Actors

### Technical Operator

May read sanitized operational telemetry, health, traces, workers, integration and queue state within assigned company/deployment scope.

### Technical Administrator

Has Technical Operator capabilities plus explicitly authorized operational actions such as acknowledging alerts or requesting retry of an eligible failed asynchronous work item. It does not automatically gain fiscal/business administration permissions.

### Support Analyst

May receive a reduced read-only support view centered on correlation IDs, sanitized errors and service status. Access to sensitive telemetry fields remains restricted.

## Functional requirements

- **FR-087** System shall provide an authenticated web Technical Operations Console backed by server-authoritative observability APIs rather than direct browser access to log files, telemetry credentials, databases or infrastructure consoles.
- **FR-088** Console shall expose a health overview for API runtime, active database provider, Redis/cache when enabled, artifact storage, background workers and configured external integrations without exposing secrets.
- **FR-089** Console shall support sanitized technical-event search by time range, severity, stable technical event code, module, `operationId`, HTTP status, correlation/trace ID and safe operational scope fields.
- **FR-090** Console shall reconstruct an operation timeline using correlation/trace/causation identifiers across HTTP request, application handling, Outbox/Inbox, workers and external integration attempts where evidence exists.
- **FR-091** Console shall expose aggregated operational metrics including request rate, error rate, latency distributions, DGI/provider latency/failure/retry state, queue backlog, worker failures, sync conflict counts and dependency health.
- **FR-092** Console shall expose Outbox/Inbox/background-job operational state including pending, retrying, failed/stuck and dead-letter/review conditions using sanitized metadata.
- **FR-093** Console shall expose offline synchronization operational state including batch throughput, conflicts, review-required operations and device/sync freshness without returning unrestricted queued business payloads.
- **FR-094** Console shall allow privileged operational actions only through explicit server commands and permissions. Initial permitted actions may include alert acknowledgement and retry request for an eligible failed/dead-letter work item; direct mutation/deletion of raw logs, audit events or fiscal/business state is forbidden.
- **FR-095** Every privileged operational action shall itself be durably audited with actor, target operational item, reason, correlation context and outcome.
- **FR-096** Console shall provide a safe link/reference from technical evidence to related durable audit events when correlation/entity context permits, while preserving separate storage, permission and retention semantics.
- **FR-097** Console shall surface CAE expiry/exhaustion and fiscal integration degradation as operational alerts based on authoritative system state, not by parsing free-form log text alone.
- **FR-098** Console shall support export of a bounded sanitized diagnostic bundle for support/incident analysis when explicitly authorized; secrets, unrestricted PII and private fiscal artifacts are excluded by default.

## Non-functional requirements

- **NFR-017 Observability UI security:** telemetry queries and operational actions use least privilege, bounded time ranges/pagination, rate limits where appropriate and recursive redaction before data reaches the browser.
- **NFR-018 Provider independence:** the Console depends on canonical observability query contracts. Application Insights may be one adapter, but the web/API contract must not require Azure-specific query objects or credentials.
- **NFR-019 Performance isolation:** monitoring queries must not materially degrade transactional POS/fiscal paths. Expensive searches/exports use bounded queries, aggregation or asynchronous jobs.
- **NFR-020 Retention awareness:** the Console must clearly distinguish unavailable/expired technical telemetry from missing durable audit evidence; technical-log Class-D retention does not control Classes A-C.
- **NFR-021 Correlation continuity:** support views must use the same canonical correlation/trace identifiers defined by the API logging contract.

## Business/security rules

- **BR-016** Raw technical log files are not a normal business API and are not directly downloaded/browsed by ordinary application users.
- **BR-017** Technical monitoring permission does not imply permission to read business audit, fiscal documents, customer PII, secrets or administrative configuration values.
- **BR-018** A monitoring UI may summarize/reference durable audit but may not become a second mutable audit authority.
- **BR-019** Operational retry commands re-enter the same idempotent application/worker pathway and cannot bypass domain/fiscal authorization or create duplicate effects.
- **BR-020** Health/dependency data reveals the minimum information required for operations and must not disclose connection strings, tokens, private endpoints/credentials or certificate secrets.

## Acceptance scenarios

1. Operator searches a `CorrelationId` and sees request completion, application conflict, Outbox enqueue, worker retry and sanitized provider failure in chronological order.
2. A nested object containing `accessToken`, `connectionString` and certificate password fields is emitted by a failing component; none of those values appear in the Console.
3. DGI/provider is unavailable while local sale confirmation succeeds; Console shows fiscal transport degradation/backlog without reporting the commercial sale as failed.
4. A dead-lettered Outbox item is retried by an authorized Technical Administrator; retry uses the original idempotency/correlation context and produces durable `operations.retry.requested/completed` audit evidence.
5. Unauthorized user receives 403 for technical monitoring APIs even if they know a correlation ID.
6. Expired technical telemetry cannot be mistaken for missing legal audit; UI labels the retention boundary explicitly.
7. Console can show PostgreSQL or MySQL as the active database provider without changing its public monitoring contract.

## Interface/API impact

This requirement introduces one new proposed Web Interface Scope item and new `/api/v1/operations/**` contract families. Exact operation IDs/routes remain the API Contract Design amendment responsibility.

## Implementation boundary

Current work remains API-first. This amendment defines the future web tool and the API/data contracts it will require; it does not authorize frontend implementation before the Blueprint API Gate and later client gates are satisfied.
