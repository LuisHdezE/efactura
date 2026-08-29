# Technical Operations Console: API Traceability and Change Impact

## Change context

The accepted Technical Operations Console amendment adds Requirements `FR-087..FR-098`, `NFR-017..NFR-021`, `BR-016..BR-020` and Interface Scope item `WEB-019` after the original API Contract draft was created.

This document reconciles that accepted additive change into API Contract Design before `api_contract_ready` may become PASS.

## Requirement traceability

| Requirement | API evidence |
|---|---|
| `FR-087` authenticated server-authoritative Console API | all `API-172..API-193`, auth/permission supplement |
| `FR-088` health overview | `getOperationsOverview`, `getOperationsHealth`, `listOperationalDependencies` |
| `FR-089` sanitized technical-event search | `listTechnicalEvents`, `getTechnicalEvent` |
| `FR-090` correlation/trace timeline | `getTraceTimeline` |
| `FR-091` aggregated operational metrics | `getOperationalMetrics`, `getOperationalMetricSeries` |
| `FR-092` Outbox/Inbox/job state | `listOperationalQueues`, `listOperationalWorkItems`, `getOperationalWorkItem`, `listOperationalWorkers` |
| `FR-093` offline synchronization operations state | `getOperationalSyncOverview` |
| `FR-094` explicitly permitted operational commands only | `requestOperationalRetry`, `acknowledgeOperationalAlert`, `createDiagnosticBundle`; forbidden-operation list |
| `FR-095` privileged actions durably audited | `14_TECHNICAL_OPERATIONS_PERMISSION_AUDIT_IDEMPOTENCY.md` |
| `FR-096` safe audit linkage | conditional `relatedAuditReferences` + independent `audit.read` requirement |
| `FR-097` CAE/fiscal integration operational alerts | `listOperationalAlerts`, `getOperationalAlert`, integration/overview contracts |
| `FR-098` sanitized diagnostic bundle | `createDiagnosticBundle`, `getDiagnosticBundle`, `downloadDiagnosticBundle` |

## NFR traceability

| NFR | Contract response |
|---|---|
| `NFR-017` least privilege/bounds/redaction | operations permission family; max ranges/page sizes; server-side sanitization |
| `NFR-018` provider independence | normalized DTOs; no Application Insights query language/credential exposure |
| `NFR-019` performance isolation | bounded synchronous queries; metric projections; async diagnostic bundles |
| `NFR-020` retention awareness | `dataCompleteness`, partial/unavailable semantics, explicit Class-D distinction |
| `NFR-021` correlation continuity | canonical correlation/trace/causation fields and `getTraceTimeline` |

## Business-rule traceability

| Rule | Contract enforcement |
|---|---|
| `BR-016` no raw log-file business API | no route exists; explicit forbidden operations |
| `BR-017` monitoring != audit/fiscal/PII/secret access | dedicated `operations.*` permissions and conditional audit references |
| `BR-018` monitoring not second audit authority | audit remains separate API/store; no audit mutation route |
| `BR-019` retry uses canonical idempotent pathway | `requestOperationalRetry` contract + required idempotency/audit |
| `BR-020` health minimizes infrastructure disclosure | canonical safe dependency DTOs only |

## Interface reconciliation

### `WEB-019 Technical Operations Console`

Authoritative operations:

- `getOperationsOverview`;
- `getOperationsHealth`;
- `listTechnicalEvents`;
- `getTechnicalEvent`;
- `getTraceTimeline`;
- `getOperationalMetrics`;
- `getOperationalMetricSeries`;
- `listOperationalDependencies`;
- `listOperationalIntegrations`;
- `getOperationalIntegration`;
- `listOperationalQueues`;
- `listOperationalWorkItems`;
- `getOperationalWorkItem`;
- `requestOperationalRetry`;
- `listOperationalWorkers`;
- `getOperationalSyncOverview`;
- `listOperationalAlerts`;
- `getOperationalAlert`;
- `acknowledgeOperationalAlert`;
- `createDiagnosticBundle`;
- `getDiagnosticBundle`;
- `downloadDiagnosticBundle`.

This resolves the Interface Scope amendment without authorizing frontend implementation before later client gates.

## Change impact against original v1 draft

Original planned v1 operation count: **171**.

Technical Operations amendment: **22** new operations.

Reconciled planned v1 operation count: **193**.

No existing v1 operation is removed or semantically weakened. No legacy Brownfield route is removed.

Affected API Contract checks:

- `api.scope_defined`: additive operations capability;
- `api.endpoint_inventory`: adds `02C`;
- `api.auth_contract`: JWT boundary unchanged;
- `api.permission_matrix`: adds `operations.*` family;
- `api.audit_event_mapping`: adds privileged operational events;
- `api.idempotency_matrix`: adds three retry-sensitive commands;
- `api.contract_traceability`: adds FR/NFR/BR/WEB-019 mapping.

`api.change_impact_analysis` remains not required under the Blueprint post-initial-API-Gate rule because the initial v1 API has not passed API Gate and has no accepted executable v1 consumer. Nevertheless this document records pre-gate change impact explicitly so the design history is not ambiguous.

## Fiscal/business safety impact

The Console does not create a parallel path for fiscal/business state mutation.

In particular, `requestOperationalRetry` cannot:

- choose/force a CFE type;
- allocate a replacement fiscal number merely due to retry;
- change `REJECTED` to `ACCEPTED`;
- edit CFC/CAE state directly;
- create duplicate payment/stock/receivable effects;
- bypass ordinary use-case authorization.

## Brownfield impact

Existing Serilog rolling files and Application Insights remain preserved as current observability sources/adapters. This contract does not require immediate replacement of those tools. It does require a normalized server-side operations boundary before the future web Console can consume them safely.

## Completion signal

The Technical Operations amendment is fully reconciled into API Contract Design only when:

- `API-172..API-193` remain uniquely identified;
- permissions/audit/idempotency mappings are accepted;
- `WEB-019` is linked to those operations;
- total v1 operation count is reconciled to 193;
- PR #9 remains implementation-free until explicit API Contract acceptance.
