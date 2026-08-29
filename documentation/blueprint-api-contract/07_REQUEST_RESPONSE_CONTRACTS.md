# API v1 Request and Response Contract Registry

## Purpose

Define payload intent before OpenAPI. Names below are public DTO contract concepts, not permission to expose EF entities, Dapper rows, domain aggregates or provider SDK models.

## Shared contracts

### `Page<T>`

- `items: T[]`
- `page: integer`
- `pageSize: integer`
- `total: integer`

### `MoneyDto`

- `amount: decimal`
- `currency: ISO code`

### `ActorContextDto`

- actor ID/display metadata;
- effective `permissions[]`;
- company/location/terminal scopes;
- optional safe device/offline capability metadata.

Never contains bearer tokens/provider secrets/raw auth configuration.

### `OperationAcceptedDto`

For durable async work: resource/operation ID, status, status URL, accepted time, correlation ID. `ACCEPTED` is not DGI acceptance.

## Party contracts

### `PartyCreateRequest`

Carries party kind/person-or-organization metadata, name, roles, residence country, tax-residence country, typed fiscal identities, addresses and contacts.

Fiscal identity fields include type, number, issuing country and validity fields where applicable. There is no authoritative `isForeign` input.

### `PartyDto`

Opaque ID, version, active state, roles, normalized identity projections, addresses/contacts and safe links/summaries. Historical sales/CFE retain snapshots and are not rewritten when Party master data changes.

## Commercial item contracts

### `CommercialItemRequest`

- code;
- name/description;
- `kind: PRODUCT|SERVICE`;
- unit;
- `trackInventory`;
- tax profile ID;
- allowed price/commercial fields;
- category.

Service-only businesses work with `trackInventory=false`; mixed sales are valid.

### `CommercialItemDto`

Includes safe catalog/tax/inventory hints. Current stock remains authoritative through Inventory/POS projections.

## POS bootstrap

`PosBootstrapDto` is a bounded, permission/location-aware cache payload with effective location/terminal, enabled payment methods, compact item/customer lookup data, relevant fiscal reference metadata, current cash-shift summary and cache/sync revision. It is not a full database dump.

## Sales

### `SaleCreateRequest`

- location/terminal when not inferred;
- optional customer Party ID;
- commercial terms;
- currency;
- lines with item ID, quantity and allowed commercial override inputs;
- structured discounts/surcharges;
- optional client intent metadata.

Server resolves authoritative item/tax data and computes amounts.

### `SaleDraftUpdateRequest`

Editable DRAFT-only fields plus `expectedVersion`.

### `SaleValidationDto`

Returns normalized line calculations, totals/taxes, warnings/errors, receiver identity decision, cross-border/tax treatment, eligible/required fiscal family, missing facts and safe rule references. Validation is side-effect free apart from optional accountable validation evidence.

### `SaleConfirmRequest`

- `expectedVersion`;
- payment intents and/or credit terms;
- optional desired fiscal documentation strategy only where multiple DGI-permitted strategies are enabled by accepted configuration;
- required operator reason/context.

Requires `Idempotency-Key`.

### `SaleDto`

Separates commercial state, payment/receivable summary, stock effect summary and fiscalization workflow. It never collapses `sale confirmed` into `CFE accepted`.

## Fiscal decision

`FiscalDecisionDto` includes transaction jurisdiction, tax treatment, receiver identity used, selected/eligible fiscal family, applicability errors/warnings, fiscal rule/specification version and `ruleReferences[]`.

Client cannot post a CFE code to bypass this decision.

## Fiscal documents

### `FiscalDocumentDto`

Includes ID, family/code, series/number, issuer/receiver snapshot summaries, business/fiscal date, totals, generation state, transport state, final fiscal result state, source references, correction/reference links, rule/spec version, artifact descriptors and timestamps.

### `FiscalCorrectionRequest`

Correction intent plus affected lines/amount/reason/reference evidence and expected version. Server selects/validates the actual correction CFE family permitted by current rules.

### `FiscalRegularizationResolutionRequest`

Allowed server-provided disposition ID, reason/supporting metadata and expected version. Arbitrary state strings are rejected.

### Delivery contracts

`requestFiscalDocumentDelivery` accepts an enabled delivery channel/recipient reference allowed by policy. Delivery state is separate from fiscal acceptance.

## CAE

### `CaeImportRequest`

Bounded CAE artifact plus optional non-authoritative operator metadata. Server verifies/normalizes authorization metadata, signature/range/type/expiry as technically applicable.

### `CaeAllocationRequest`

Location/terminal scope, requested allocation/subrange where policy permits, reason and expected CAE version. Server validates range ownership, overlap and company-wide numbering uniqueness.

## Contingency

### `EnterContingencyRequest`

Location/terminal scope, reason, occurredAt and supporting context.

### `ContingencyDocumentRegistrationRequest`

Formal CFC type/series/number identity, issue date/time, receiver snapshot, lines/totals and optional source sale/client-operation link. It never asks the client to generate a normal CAE/CFE number.

### `ContingencyReconcileRequest`

Server-allowed reconciliation disposition, recovery/report linkage and reason/evidence.

## Inventory and transfers

### `StockAdjustmentRequest`

Item ID, location ID, quantity delta/accepted adjustment mode, reason code/explanation and expected inventory version.

### `StockTransferCreateRequest`

Source, destination, lines and reason/reference.

Transition commands (`approve`, `dispatch`, `receive`, `reconcile`) carry expected version and transition-specific evidence. Receive may contain actual received quantities/discrepancies.

## Replenishment

`ReplenishmentSimulationRequest` contains item/location/date horizon and allowed model inputs/overrides. Response records input provenance, EOQ/ROP/recommendation values and warnings. Simulation never mutates stock.

## Procurement

### `PurchaseOrderCreateRequest` / `PurchaseOrderUpdateRequest`

Supplier, currency, lines, prices/terms and expectedVersion on mutable updates.

### `GoodsReceiptCreateRequest`

PO/source references and actual received quantities/discrepancies. `postGoodsReceipt` is the idempotent boundary that may create stock/payable evidence.

## Receivables / payables

Canonical obligation DTO contains ID, party, source, original amount/currency, due date, adjustments, allocations, derived open balance, state and version. Balance is never client-computed authority.

`ObligationAdjustmentRequest`: adjustment type/amount, reason, expectedVersion.

## Collections and supplier payments

### `CollectionCreateRequest`

Customer/party, payment date, method, amount/currency, external reference and `allocations[] {receivableId, amount}`. Advance/overpayment intent is accepted only after the relevant business policy is approved.

### `SupplierPaymentCreateRequest`

Equivalent for supplier/payable allocations.

Reversal requests include reason/expectedVersion and create compensating facts rather than deleting history.

## Cash

### `CashShiftOpenRequest`

Terminal/location context when not inferred and opening counted amount(s).

### `CashMovementRequest`

Type, amount/currency, reason and source reference where applicable.

### `CashShiftCloseRequest`

ExpectedVersion, counted amounts by supported medium/currency and notes/reason.

### `CashShiftReconcileRequest`

Allowed disposition, variance reason/approval evidence and expectedVersion.

## Offline synchronization

### `SyncBatchRequest`

- `deviceId`;
- client `batchId`;
- optional prior cursor;
- ordered `operations[]`.

Each operation carries `clientOperationId`, `clientSequence`, an allow-listed `commandType`, `occurredAt`, dependency IDs, material payload and offline-grant context required by client architecture.

The sync API is not an arbitrary route executor. `commandType` comes from a server-approved sync command catalog.

### `SyncOperationResultDto`

Status is one of `APPLIED`, `ALREADY_APPLIED`, `REJECTED`, `CONFLICT`, `REVIEW_REQUIRED`, `DEPENDENCY_BLOCKED`; includes canonical server result/reference and Problem Details fragment where applicable.

### `SyncChangesDto`

Scoped changed items, next opaque cursor, server revision/time and freshness metadata.

## Received fiscal documents / XML

Single validation may accept bounded `application/xml`; imports preserve original bytes/hash/source plus detected identity, schema/spec version, signature status, findings and duplicate/linkage result.

`ImportReceivedFiscalDocumentResultDto` explicitly distinguishes `IMPORTED`, `DUPLICATE`, `INVALID`, `REVIEW_REQUIRED`.

Batch import returns per-file results. One bad file does not silently erase valid imported results unless an explicit all-or-nothing mode is separately accepted.

## Reports / fiscal reports

Report endpoints expose structured data using allow-listed filters. Charts are a client presentation concern.

Statutory daily fiscal report DTOs keep generation, signing, submission, envelope/transport and final acknowledgement state distinct.

Large exports use durable job resources where necessary.

## Audit

Audit query filters are bounded to date range, category/type, actor, scope, aggregate/reference and correlation/idempotency/client-operation identifiers. Audit export is job-based and its request/access is audited.

## Accounting exports

`createAccountingExport` accepts one enabled adapter `formatId`, bounded date/scope filters and output options. It cannot load arbitrary server assemblies/templates. Result is a job with status/artifact metadata.

## Configuration and integrations

Responses expose adapter type/status, safe configuration metadata and at most secret-reference/presence/expiry metadata. Secret values/private keys are never returned.

Ordinary update payloads reference separately provisioned secret/key IDs rather than transporting private-key/password material. A future secure provisioning contract requires its own review.

## Reference data

Reference-data endpoints return effective/version metadata where fiscal meaning can change. Fiscal identity types, document types and invoice indicators are not timeless client enums.

## DTO isolation

Public contracts are separate from EF entities, Dapper rows, provider SDK models, raw DGI transport envelopes and domain aggregate internal state. Mapping is explicit at Presentation/Application boundaries.