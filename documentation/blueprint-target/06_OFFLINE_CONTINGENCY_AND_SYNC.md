# Offline, Contingency and Synchronization Architecture Requirements

## Goal

Future web/mobile clients must remain useful when connectivity to the API is interrupted without causing duplicate sales/payments, corrupting stock, inventing fiscal numbering or violating DGI contingency rules.

Offline capability is therefore a **server contract concern now**, even though frontend/mobile implementation is deferred.

## Failure modes that must remain distinct

### MODE-A — Normal online
Client can reach API and the fiscal integration path is healthy.

### MODE-B — API reachable, fiscal transport impaired
Client/API business transaction reaches the server, but DGI/provider transport is unavailable or delayed.

The server can persist authoritative state, idempotency, audit and controlled transport/outbox work. Whether the operation may be delivered/printed immediately depends on the active fiscal rule/document type. A transport outage is not automatically a CFC event.

### MODE-C — Client cannot reach authoritative API / issuance system unavailable
The client can only use its local cache/queue. It cannot safely reserve company-wide CAE/CFE numbering under the baseline architecture.

If a fiscal sale must be completed during this condition, the official CFC contingency process is used according to current DGI rules. Local queued data supports later synchronization and linkage; it does not become a self-authorized CFE.

## Client offline data model

Future clients may cache only the data required for their authorized workflows, for example:

- identity/session metadata suitable for offline policy, never server secrets;
- branch/terminal profile;
- sellable catalog snapshot;
- permitted prices/tax presentation metadata;
- selected customer snapshots where policy allows;
- current open local cart/drafts;
- last synchronized stock information as **stale-capable** data;
- pending local operations;
- contingency linkage fields when a CFC was physically issued.

Sensitive cached data must be encrypted/protected according to platform capability and logout/device-revocation policy.

## Canonical client operation envelope

Retry-capable commands must be representable as:

```json
{
  "clientOperationId": "uuid-or-ulid-generated-on-client",
  "deviceId": "registered-device-id",
  "clientSequence": 1042,
  "occurredAt": "2026-08-28T18:30:00-03:00",
  "operationType": "SALE_CONFIRM",
  "payloadVersion": 1,
  "payload": {},
  "relatedClientOperationIds": [],
  "contingency": {
    "usedCfc": false,
    "cfcType": null,
    "series": null,
    "number": null
  }
}
```

The server records its own `receivedAt` and correlation identifiers.

## Deduplication

A unique server constraint must protect at least:

`organization + deviceId + clientOperationId`

When the same operation is submitted again:

- if payload hash/version is identical, return the canonical existing result;
- if the same operation ID arrives with different material payload, reject as an idempotency conflict and audit it;
- never create another payment/sale/CFE merely because the HTTP request was repeated.

## Batch synchronization

Target contract will support conceptually:

- submit batch of local operations;
- return per-operation status/result/conflict;
- server sync cursor/checkpoint for downloading changed reference data;
- resumable batches;
- ordered application when dependency relationships exist.

Provisional result states:

- `APPLIED`
- `ALREADY_APPLIED`
- `REJECTED_VALIDATION`
- `REJECTED_PERMISSION`
- `CONFLICT`
- `REQUIRES_REVIEW`
- `WAITING_DEPENDENCY`

## Conflict strategy

### Master/reference data
Use version/concurrency token. Client cannot silently overwrite a newer customer/product/configuration record.

### Stock
Offline stock is never authoritative. The server revalidates stock-tracked operations when synchronized. If business policy allowed a contingency sale despite uncertainty, reconciliation produces explicit variance/backorder/negative-stock handling instead of silently losing the sale.

### Prices/tax rules
The server validates the rule/version effective at the actual operation time and the fiscal context. A stale client price/rule mismatch is explicit evidence, not silently rewritten.

### Payments
Every offline payment/collection has an operation ID and external-reference policy. Duplicate money effects are prohibited.

### Fiscal numbering
Web/mobile client does not choose a normal CFE number offline. CAE reservation remains authoritative server behavior.

## CFC linkage

When a CFC was used during MODE-C, the synchronized sale operation carries the CFC identity and contingency episode reference.

The server must:

1. validate that the CFC identity is not already linked to another operation;
2. create/reconcile the contingency registry record;
3. preserve the original occurrence/issuance time and operator/terminal;
4. transmit/report CFC information after recovery as required;
5. never consume an unrelated normal CAE number merely to “convert” the CFC into another sale;
6. include it in the applicable DGI daily-report process;
7. audit the full chain.

## Offline authentication/authorization

Offline authorization is bounded:

- permissions are cached with expiry/version and device/session policy;
- high-risk actions can be marked `ONLINE_REQUIRED`;
- privilege revocation cannot be guaranteed to reach an isolated client immediately, so offline permission scope must be narrower than online scope;
- server revalidates permission/context when synchronized and can require supervisor review for high-risk queued actions.

Recommended default `ONLINE_REQUIRED` examples:

- user/role administration;
- CAE import/activation;
- fiscal configuration/certificate changes;
- high-risk stock adjustments;
- direct correction/anulation workflows unless a specific offline policy is approved.

## API consequences

The later API Contract Design must include:

- idempotency matrix for all command endpoints;
- stable operation IDs/status resources;
- batch sync endpoint(s);
- delta/reference-data synchronization strategy;
- conflict/error codes;
- device registration/revocation model if mobile/offline scope is enabled;
- explicit CFC/contingency resources;
- correlation IDs in responses/errors;
- no endpoint that lets a generic client reserve arbitrary CAE numbers.

## Required QA later

- replay same sale 10 times -> one canonical sale/fiscal effect;
- reconnect after response loss -> retrieve same result;
- two devices reuse operation ID -> scope rules enforced;
- same operation ID with altered payload -> conflict;
- stale stock/customer/price -> deterministic conflict policy;
- offline CFC sale -> one CFC linkage, no duplicate normal CFE;
- batch interrupted mid-way -> safe resume;
- PostgreSQL and MySQL produce identical sync/idempotency semantics;
- revoked/high-risk offline permission -> server rejects/review path;
- clock skew -> server preserves occurredAt but does not trust client time for unrestricted fiscal decisions.
