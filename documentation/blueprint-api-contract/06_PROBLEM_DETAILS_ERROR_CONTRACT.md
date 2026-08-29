# RFC 9457 Problem Details Contract

## Purpose

API v1 replaces the Brownfield universal `ResultObject` / mostly-400 behavior with one stable error model.

Media type: `application/problem+json`.

RFC 9457 base fields:

- `type`
- `title`
- `status`
- `detail`
- `instance`

Allowed eFactura extensions:

- `code`: stable machine-readable application code;
- `traceId`: server trace/request identifier;
- `correlationId`: cross-boundary correlation ID;
- `errors`: optional field/path validation collection;
- `conflictType`: optional conflict class;
- `currentVersion`: optional safe concurrency version;
- `ruleReferences`: optional safe business/fiscal rule ID/version/source references;
- `retryAfterSeconds`: optional retry guidance.

Production responses never expose exception class names, SQL, stack traces, connection/provider details, credentials or secret configuration.

## HTTP status policy

| Status | Contract meaning |
|---:|---|
| 400 | malformed protocol/request, invalid JSON/query shape, missing required idempotency header |
| 401 | authentication absent/invalid/expired |
| 403 | authenticated but permission/scope denied |
| 404 | resource absent or intentionally hidden by the approved object-authorization policy |
| 409 | concurrency, state, duplicate or idempotency conflict |
| 413 | upload/request exceeds accepted bound |
| 415 | unsupported media type |
| 422 | syntactically valid request rejected by domain/business/fiscal validation |
| 429 | throttled |
| 503 | required external dependency temporarily unavailable when the requested action cannot safely be accepted locally |

## Canonical codes

Cross-cutting:

- `validation_failed`
- `not_found`
- `forbidden`
- `authentication_required`
- `invalid_state_transition`
- `concurrency_conflict`
- `idempotency_key_missing`
- `idempotency_key_reused`
- `duplicate_resource`
- `dependency_unavailable`
- `rate_limited`
- `request_too_large`
- `unsupported_media_type`

Fiscal:

- `fiscal_rule_violation`
- `fiscal_document_type_not_eligible`
- `receiver_identity_invalid`
- `tax_treatment_not_eligible`
- `cae_unavailable`
- `cae_expired`
- `cae_exhausted`
- `cae_allocation_conflict`
- `fiscal_number_conflict`
- `fiscal_document_immutable`
- `fiscal_correction_not_allowed`
- `fiscal_regularization_required`
- `contingency_not_active`
- `contingency_document_invalid`
- `invalid_fiscal_xml`
- `invalid_fiscal_signature`
- `duplicate_fiscal_document`

Financial/inventory:

- `allocation_exceeds_policy`
- `payment_already_reversed`
- `cash_shift_state_conflict`
- `stock_conflict`
- `negative_stock_not_allowed` only if the accepted stock policy selects that rule;
- `transfer_state_conflict`

Offline/sync:

- `client_operation_conflict`
- `sync_dependency_blocked`
- `offline_permission_expired`
- `review_required`

## Validation shape

Example:

```json
{
  "type": "https://api.example/problems/validation",
  "title": "Validation failed",
  "status": 422,
  "code": "validation_failed",
  "detail": "One or more business fields are invalid.",
  "instance": "/api/v1/parties",
  "correlationId": "…",
  "errors": [
    {
      "path": "fiscalIdentities[0].issuingCountry",
      "code": "identity_country_not_allowed",
      "message": "The selected fiscal identity type is not valid for this issuing country."
    }
  ],
  "ruleReferences": [
    {"ruleId": "DGI-CFE-IDENTITY-…", "version": "25.2"}
  ]
}
```

Messages are safe for operators/users; rule references support explainability/audit without leaking implementation secrets.

## Conflict shape

When safe:

```json
{
  "status": 409,
  "code": "concurrency_conflict",
  "conflictType": "stale_version",
  "currentVersion": 18,
  "correlationId": "…"
}
```

The server does not automatically include the full current resource if that would bypass authorization/data minimization.

## Fiscal dependency failure

A DGI/provider outage does **not** automatically mean the public command returns 503.

If the accepted local workflow can durably commit the business operation and queue transport, return the appropriate 200/201/202 local result with transport `PENDING`. 503 is used only when the requested action cannot safely be accepted locally.

Thus `dependency unavailable`, `transport pending` and `fiscal rejected` are three different states.

## Field validation vs business rejection

- malformed JSON/query/header -> 400;
- syntactically valid DTO with invalid field/business/fiscal combination -> 422;
- valid command for stale/illegal current aggregate state -> normally 409;
- authorization decision -> 401/403/approved hidden-404 policy.

## Correlation

Every Problem Details document carries the effective `correlationId`; technical trace ID is included when safe. The same correlation flows into logs, audit and outbox/integration evidence.