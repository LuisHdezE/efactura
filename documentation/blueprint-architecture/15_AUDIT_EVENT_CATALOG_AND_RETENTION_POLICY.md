# Durable Audit Event Catalog and Retention Policy

## Purpose

Materialize Blueprint checks `audit.event_catalog` and `audit.retention_policy`. Durable audit is separate from Serilog/Application Insights technical logs.

## Canonical event categories

| Category | Representative events | Minimum context |
|---|---|---|
| Authentication/Security | login success/failure where app observes it, token/session anomaly, device registration/revocation | actor/device/company, time, source, correlation |
| Authorization | role created/changed, permission/scope assignment/revocation, privileged denial | actor, target principal, before/after, reason |
| Organization/Fiscal config | issuer/location/terminal fiscal config changed | actor, company/location, before/after |
| CAE | CAE imported/verified/activated/allocated/exhausted/expired/annulled | actor/system, CAE/type/range/allocation, rule/source |
| Fiscal document | requested, number reserved, built, validated, signed, queued, submitted, acknowledged, accepted, rejected, regularization/correction created | document identity, actor/system, correlation/idempotency, rule/spec version |
| Contingency | entered/exited, CFC registered/used/annulled/reconciled | actor, location/terminal, CFC identity/reason |
| Sales | sale confirmed/cancelled where allowed, material correction | actor, sale, commercial snapshot reference |
| Receivables/Payables | obligation created/adjusted, due/status material change | actor/system, obligation, amounts/reason |
| Treasury | payment/collection created, allocation/reversal/adjustment | actor, payment, obligations, amounts, idempotency |
| Cash | shift opened/closed, count submitted, variance/override/reconciliation | operator/terminal, expected/counted/difference, reason |
| Inventory | adjustment, transfer dispatch/receipt/discrepancy, compensating movement | actor, item/location, before/after or movement quantities, reason |
| Procurement | PO approval/cancellation, goods receipt/posting/discrepancy | actor, supplier/order/receipt |
| Integrations | provider/DGI callback accepted/rejected/replayed, connector config changed, outbox dead-letter/recovery | integration/message IDs, result, correlation |
| Offline/Sync | device sync started/completed, operation applied/already-applied/rejected/conflict/review | actor/device/clientOperationId, payload hash, canonical result |
| Audit administration | audit export/read of sensitive scope, retention/purge job | actor, query/export scope, reason |

## Event record policy

Every durable audit event has a stable event ID and, when applicable:

- occurred and recorded timestamps;
- actor type/user/system identity;
- company/location/terminal/device;
- category + canonical event type;
- aggregate/entity references;
- request/correlation/idempotency/client-operation IDs;
- reason/authorization context;
- outcome;
- before/after or immutable transaction-context reference;
- rule/specification provenance for regulated decisions.

No passwords, tokens, connection strings, private keys/certificate passwords or unnecessary full PII/XML payloads are stored in audit.

## Retention classes

### Class A — Fiscal/legal evidence

Fiscal document lifecycle, CAE/CFC, fiscal configuration and relevant regulatory decision evidence are retained for at least the applicable statutory/documentation period. Exact duration is a versioned compliance configuration backed by an authoritative legal source and may not be shortened below the applicable requirement.

### Class B — Financial/inventory/business accountability

Payment/allocation, receivable/payable adjustments, cash reconciliation and sensitive stock corrections are retained according to the approved accounting/business retention policy and never purged while required to explain retained financial/fiscal records.

### Class C — Security/authorization audit

Role/permission/scope, privileged administration, device/sync anomalies and sensitive audit-access events use a security retention policy long enough to support incident investigation and compliance obligations. Exact operational duration is deployment policy, never an ad hoc table cleanup.

### Class D — Technical logs

Serilog/Application Insights operational logs may have a shorter independently configured retention. They are not the legal/business audit store and their expiration does not remove Class A-C evidence.

## Purge/archival controls

- retention/purge executes through an authorized audited job;
- legal hold or unresolved incident can suspend purge;
- immutable/hash integrity is preserved when moving evidence to archive;
- purge scope/count/result is itself audited;
- no ordinary API exposes audit update/delete;
- backup/restore policy covers retained audit evidence.

Exact numeric retention/RPO/RTO values may be finalized per production deployment and legal review, but these classes and minimum-governance rules are architecture-authoritative now.
