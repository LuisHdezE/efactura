# Technical Logging and Observability Contract

## Purpose

Technical/operational logging is a REQUIRED cross-cutting capability and is distinct from durable business/security audit.

- **Technical logs** diagnose runtime, transport, integration, performance and failure behavior.
- **Durable audit** proves accountable business/security events and remains append-oriented authoritative evidence.

A technical log entry never substitutes for required durable audit evidence, and durable audit must not become a verbose application log sink.

## Brownfield preservation

The AS-IS already uses Serilog, an asynchronous rolling file sink and Application Insights. The target preserves these capabilities and hardens them rather than replacing them for style.

Current Brownfield uncertainty that must be closed during implementation: the existing log template references `CorrelationId` and `Username`, but consistent request enrichment was not proven.

## Canonical correlation model

Every inbound request receives a server-trusted correlation context.

- accept a syntactically valid `X-Correlation-Id` from trusted/normal clients when policy allows, otherwise generate one;
- always generate/preserve the ASP.NET request/trace identifier;
- use W3C `traceparent`/Activity tracing where available;
- return the effective `X-Correlation-Id` in the response;
- include the same correlation identifier in RFC 9457 Problem Details;
- propagate correlation through application handlers, durable audit, outbox/inbox messages, background jobs and outbound integrations;
- preserve parent/causation identifiers for asynchronous processing when the original request has already completed.

Correlation IDs are diagnostic identifiers, not credentials and not authorization evidence.

## Required structured log context

Technical logs use structured properties, not string concatenation. Include when applicable:

- UTC timestamp;
- severity level;
- stable technical event code;
- message template;
- application/service name and version;
- environment;
- request/trace/correlation identifiers;
- OpenAPI `operationId` when an HTTP operation is resolved;
- HTTP method/path template/status code/duration;
- actor/user identifier only when authenticated and safe to record;
- company/location/terminal/device identifiers when relevant and authorized for diagnostic context;
- aggregate/resource identifier only when useful and privacy-safe;
- idempotency/client-operation reference or safe hash when relevant;
- outbox/inbox/job/message identifier for background work;
- integration/provider name and canonical operation/result category;
- retry attempt/backoff state;
- exception type and sanitized stack/error context for unexpected failures.

Do not rely on free-form usernames or mutable display names as the only actor linkage.

## Stable technical event categories

Representative event codes:

- `http.request.started`
- `http.request.completed`
- `http.request.rejected`
- `auth.authentication.failed`
- `auth.authorization.denied`
- `validation.request.failed`
- `application.command.conflict`
- `application.unhandled_exception`
- `database.transient_failure`
- `database.concurrency_conflict`
- `cache.failure`
- `outbox.dispatch.started`
- `outbox.dispatch.succeeded`
- `outbox.dispatch.failed`
- `inbox.message.duplicate`
- `integration.fiscal.requested`
- `integration.fiscal.completed`
- `integration.fiscal.failed`
- `sync.batch.started`
- `sync.batch.completed`
- `sync.operation.conflict`
- `worker.job.failed`
- `health.dependency.degraded`

These are technical categories. Business facts such as `sale.confirmed` or `fiscal.document.accepted` belong to the durable audit catalog when accountability is required.

## Severity policy

- `Trace`: very fine diagnostic detail; normally disabled outside controlled troubleshooting.
- `Debug`: development/test diagnostics; not a production default.
- `Information`: expected lifecycle and successful operational events.
- `Warning`: recoverable anomaly, retry, degradation, conflict or suspicious condition requiring attention but not necessarily failed business outcome.
- `Error`: an operation failed unexpectedly or a required technical dependency/action failed.
- `Critical`: process/service availability, data-integrity or security-critical failure requiring immediate intervention.

Expected domain validation/business rejection is not automatically logged as `Error`. Use appropriate structured Information/Warning diagnostics while the API returns the canonical Problem Details response.

## Request/response logging policy

By default, do **not** log complete request or response bodies.

Never log merely for convenience:

- Authorization headers;
- JWT/access/refresh tokens;
- passwords, reset secrets or API keys;
- private keys/certificate passwords;
- complete connection strings;
- cookies/session secrets;
- raw card/payment credentials;
- full fiscal XML or signed CFE payloads;
- unrestricted customer/supplier PII;
- uploaded files;
- arbitrary offline sync payloads.

When payload-level diagnosis is genuinely required, use explicitly approved field allow-lists, masking/tokenization or cryptographic hashes and temporary controlled diagnostic configuration.

## Recursive sanitization

Redaction must occur before a value reaches Serilog sinks, Application Insights or any other telemetry exporter.

Sanitization is recursive for objects, dictionaries, arrays, exception data and headers. Secret-field matching cannot depend only on one DTO shape.

At minimum protect names/patterns equivalent to:

`password`, `secret`, `token`, `authorization`, `apiKey`, `connectionString`, `privateKey`, `certificatePassword`, `clientSecret`, `accessToken`, `refreshToken`.

Provider/DB exceptions are sanitized before logging if they may contain connection/configuration data.

## HTTP logging

A request completion event should capture, when applicable:

- method;
- route template rather than uncontrolled raw URL where practical;
- operationId;
- response status;
- elapsed milliseconds;
- correlation/trace identifiers;
- authenticated actor and scope identifiers in minimized form;
- canonical failure category.

Query strings and route values containing PII or tokens are not blindly serialized.

## Background jobs, outbox and integrations

Asynchronous work must remain reconstructable after the initiating HTTP request is gone.

Log:

- work/message identifier;
- correlation + causation identifiers;
- attempt number;
- canonical integration operation;
- start/completion/failure;
- elapsed time;
- retry/backoff/dead-letter state;
- sanitized external status/category.

Do not log raw DGI/provider payloads as the primary diagnostic representation. Persist legally/business-required artifacts in the approved fiscal artifact store and reference their safe IDs/hashes from logs.

## Offline/synchronization observability

Technical logs may record batch/device/operation identifiers, processing duration and canonical outcome categories, but not copy the whole queued command payload.

Idempotency conflicts and replay anomalies are both:

1. technically logged for diagnosis; and
2. durably audited when security/business accountability requires it.

## Sinks and environment policy

Baseline:

- Serilog remains the structured logging facade;
- Application Insights remains an allowed telemetry sink;
- rolling file output may remain for development/on-premises/support scenarios where configured and protected;
- production hosting must not rely on ephemeral local files as the sole log store;
- sink selection, retention and sampling are deployment configuration, not Domain/Application logic.

ApplicationCore must not depend directly on Serilog or Application Insights. Logging abstractions/adapters stay outside the Domain model.

## Sampling

Technical telemetry may be sampled when volume requires it, provided incident-diagnostic value is preserved. Sampling policy must not remove durable audit events because audit is a separate persistence capability and is never implemented as sampled telemetry.

Security-critical and unexpected error telemetry should receive a retention/sampling policy appropriate to incident response.

## Retention

Technical logs use Architecture retention **Class D** and may have shorter retention than fiscal/business/security audit Classes A-C.

Exact retention is deployment configuration based on support, privacy, cost and incident-response requirements. Log expiration never deletes or weakens required durable audit evidence.

## Access

Raw technical logs are an operational/administrative observability resource, not a normal business API resource.

The public `/api/v1` contract does not expose arbitrary raw log-file browsing. Business users receive sanitized health/status/support identifiers where required. Access to production telemetry is controlled through the approved observability platform and least-privilege operational roles.

## Metrics and traces

Logging complements, but does not replace, metrics and distributed traces.

Required operational signals should include later implementation evidence for at least:

- HTTP latency/error/rate by operationId;
- DGI/provider latency, success/failure and retry state;
- outbox backlog/stuck/dead-letter counts;
- inbox duplicate/rejection counts;
- sync batch throughput/conflicts/review-required counts;
- CAE exhaustion/expiry operational alerts;
- database/cache dependency health;
- background worker health.

No metric label may contain unbounded/high-cardinality sensitive values such as full customer names, raw document payloads or tokens.

## Implementation and QA obligations

API Implementation must prove:

1. request correlation is created/propagated consistently;
2. `operationId` can be attached to request diagnostics;
3. Serilog/Application Insights receive structured sanitized events;
4. Domain/Application remain independent from concrete logging sinks;
5. background jobs/outbox/inbox preserve correlation/causation;
6. no secret-bearing committed configuration is emitted through startup/config logs;
7. production exception responses and logs do not leak DB/provider secrets.

API QA must include automated evidence for:

- correlation response/header + Problem Details continuity;
- log enrichment on successful and failed requests;
- authorization-denial diagnostics without leaking credentials;
- recursive redaction of representative nested secrets;
- absence of Authorization/token/private-key/connection-string values;
- outbox/integration retry correlation continuity;
- distinction between technical log emission and required durable audit event creation.

Integration QA later verifies correlation continuity from web/Android client support context through API/runtime and ensures client telemetry does not become a second durable audit authority.

## Final invariant

`diagnosable != auditable`

The system must be both. Technical logs explain runtime behavior; durable audit proves significant business/security actions.