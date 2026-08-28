# API Contract Preparation and Brownfield Compatibility

## Status

This is architecture input to the next Blueprint phase `API Contract Ready`; it is **not OpenAPI yet** and does not authorize endpoint implementation.

## Versioning

New target API uses explicit version namespace:

`/api/v1/...`

Current Brownfield endpoints under `api/[controller]` are preserved during migration until consumer usage/impact is reconciled. New v1 behavior is additive by default.

Legacy controllers may later delegate to new application use cases as compatibility adapters. Removal/route breaking requires API impact classification and human approval.

## Contract principles

- stable unique `operationId` for every OpenAPI operation;
- server-authoritative state transitions;
- permission requirement per operation;
- RFC 9457 Problem Details;
- request/correlation ID support;
- idempotency metadata on retry-sensitive commands;
- pagination/filter/sort conventions;
- explicit 401/403/404/409/422/429 semantics where applicable;
- no raw exception/provider/database messages;
- requirement/use-case/audit mapping;
- no generic `ResultObject` as the future universal domain/API error contract.

## Command-style operations

Important state changes should be explicit operations, not unrestricted CRUD updates.

Examples for API-design phase:

- `POST /api/v1/sales`
- `POST /api/v1/sales/{id}/validate`
- `POST /api/v1/sales/{id}/confirm`
- `POST /api/v1/fiscal-documents/{id}/corrections`
- `POST /api/v1/cae-authorizations/import`
- `POST /api/v1/inventory/adjustments`
- `POST /api/v1/stock-transfers/{id}/dispatch`
- `POST /api/v1/stock-transfers/{id}/receive`
- `POST /api/v1/receivables/{id}/collections`
- `POST /api/v1/payables/{id}/payments`
- `POST /api/v1/cash-shifts/{id}/close`
- sync operations described in the offline architecture.

Exact routes/operationIds remain the API Contract phase's responsibility.

## Query/read families

Planned resource areas:

- organization/locations/terminals;
- parties/customers/suppliers;
- catalog/items/services/categories;
- sales/POS;
- fiscal documents/CAE/contingency/reports;
- inventory/transfers/stock;
- procurement/receipts;
- receivables/payables/payments;
- cash shifts/reconciliation;
- received CFE/XML validation;
- audit/reporting/monitoring;
- sync/device status.

## Idempotency classification

At minimum require idempotency for:

- sale confirmation;
- fiscal issuance/correction requests;
- payment/collection creation/allocation/reversal;
- inventory adjustment/transfer posting;
- purchase receipt posting;
- cash closing/reconciliation commands;
- offline sync commands;
- retryable external callback handling internally.

## Concurrency

Where the client edits contested mutable data, the future contract may expose version/ETag-style concurrency token. A stale write returns `409 Conflict` or the final selected standardized concurrency response, never silent last-write-wins for critical state.

## Fiscal selection API rule

Clients submit transaction facts and may request a desired action, but cannot bypass `FiscalDocumentSelector`. If an incompatible CFE type is supplied, API rejects it with explainable validation/problem details.

## Offline contract rule

Offline sync payloads carry client operation identity and dependencies, not server fiscal numbers invented by the client.

## OpenAPI security

Swagger's current global Bearer decoration is not proof of endpoint authorization. The future OpenAPI contract must reflect actual security requirements and permission metadata consistently with runtime policy tests.
