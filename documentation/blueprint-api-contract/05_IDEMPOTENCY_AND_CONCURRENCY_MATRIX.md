# API Idempotency and Concurrency Matrix

## Idempotency header

Operations marked `REQUIRED` require `Idempotency-Key`.

Scope is canonical operation + company + actor/device context as applicable. The server stores the key, material request hash, processing state and canonical result/reference.

Replay rules:

- same key + same material request -> return canonical prior result without duplicate effects;
- same key + materially different request -> `409 idempotency_key_reused` and durable anomaly audit;
- crash after local commit but before response -> retry recovers committed result;
- concurrent same-key attempts cannot both execute the side effect.

## Required operations by risk class

### Security / organization / master data

`createUser`, `updateUser`, `createRole`, `updateRole`, `assignUserRoles`, `updateCurrentCompany`, `createLocation`, `updateLocation`, `registerTerminal`, `updateTerminal`, `createParty`, `updateParty`, `addPartyFiscalIdentity`, `updatePartyFiscalIdentity`, `setPartyRoles`, `createItem`, `updateItem`, `deactivateItem`, `createItemCategory`, `updateItemCategory`, `createPaymentMethod`, `updatePaymentMethod`, `updateFiscalConfiguration`, `updateIntegration`.

Reason: safe retry of administrative writes and protection against double create/change when clients retry after timeouts.

### Sales / fiscal

`createSale`, `updateSaleDraft`, `validateSale`, `confirmSale`, `cancelSale`, `createFiscalCorrection`, `resolveRegularizationCase`, `requestFiscalDocumentDelivery`.

`confirmSale` is high-risk: one key cannot create duplicate sale confirmation, payment/receivable, stock movement or fiscalization effects.

Fiscal number reservation additionally enforces unique company + CFE type + series + number independently of idempotency.

### CAE / contingency

`importCaeAuthorization`, `activateCaeAuthorization`, `createCaeAllocation`, `closeCaeAllocation`, `enterContingency`, `exitContingency`, `registerContingencyDocument`, `reconcileContingencyDocument`.

CAE import also deduplicates/validates authorization artifact identity and range overlap. CFC registration protects formal contingency identity independently from HTTP retry keys.

### Inventory / procurement

`createStockAdjustment`, `createStockTransfer`, `approveStockTransfer`, `dispatchStockTransfer`, `receiveStockTransfer`, `reconcileStockTransfer`, `createPurchaseOrder`, `updatePurchaseOrderDraft`, `approvePurchaseOrder`, `cancelPurchaseOrder`, `createGoodsReceipt`, `postGoodsReceipt`.

Stock-changing transitions combine idempotency with aggregate version/locking and immutable movement uniqueness.

`simulateReplenishment` has OPTIONAL idempotency because it is advisory and non-mutating.

### Financial / cash

`createReceivableAdjustment`, `createCollection`, `reverseCollection`, `createPayableAdjustment`, `createSupplierPayment`, `reverseSupplierPayment`, `openCashShift`, `createCashMovement`, `closeCashShift`, `reconcileCashShift`.

Payments/collections additionally guard obligation/payment versions and allocation uniqueness. Reversal is compensating, never delete/re-run history.

### Offline / device

`registerDevice`, `revokeDevice`, `createSyncBatch`.

For `createSyncBatch`, HTTP idempotency is secondary to per-operation identity:

`deviceId + clientOperationId + materialPayloadHash`.

Each operation returns exactly one canonical result class: `APPLIED`, `ALREADY_APPLIED`, `REJECTED`, `CONFLICT`, `REVIEW_REQUIRED`, `DEPENDENCY_BLOCKED`.

Same clientOperationId + different material payload -> `409 client_operation_conflict` plus audit.

### Received fiscal / reports / exports

`importReceivedFiscalDocument`, `importReceivedFiscalDocumentsBatch`, `generateDailyFiscalReport`, `submitDailyFiscalReport`, `createAuditExport`, `createAccountingExport`, `acknowledgeAlert`.

Received documents also use canonical fiscal identity/hash duplicate detection. Daily fiscal report generation uses report date/company/specification identity so repeated jobs cannot create contradictory canonical reports.

`validateFiscalXml` has OPTIONAL idempotency because it is a bounded validation operation without canonical import side effect.

## Optimistic concurrency

Contested mutable resources expose a portable application-managed `version` where appropriate.

Requests include `expectedVersion` for transitions/updates that must reject stale state. Later OpenAPI may additionally formalize ETag/If-Match where useful, but the business conflict semantics remain the same.

Stale mutation -> HTTP 409:

- `code = concurrency_conflict`;
- `conflictType = stale_version`;
- `currentVersion` only when safe to expose;
- no silent last-write-wins.

## Contested aggregate examples

| Resource | Required guard |
|---|---|
| fiscal number/CAE | transactional allocation + uniqueness/range check + version/lock |
| sale confirm | sale version + idempotency + unique downstream business identities |
| stock position | version/locking + immutable movement identity |
| transfer | aggregate version/state transition guard |
| receivable/payable | obligation version + allocation uniqueness |
| collection/payment | idempotency + reversal/state guard |
| cash shift | terminal/shift uniqueness + version/state guard |
| CAE allocation | version + non-overlap/global numbering invariant |
| sync operation | device/clientOperationId/payload hash |

## External transport

DGI/provider/email/accounting transport calls are not protected merely by HTTP idempotency. Outbox/inbox and provider-message identities handle crash/retry at integration boundaries.

A local transaction commits business state + outbox before external transport. Provider callbacks are deduplicated through inbox identity/hash.

## Error codes

- missing required header -> `400 idempotency_key_missing`;
- same key, different material payload -> `409 idempotency_key_reused`;
- stale aggregate -> `409 concurrency_conflict`;
- duplicated canonical business identity -> `409 duplicate_resource` or domain-specific code;
- same offline clientOperationId, changed payload -> `409 client_operation_conflict`.