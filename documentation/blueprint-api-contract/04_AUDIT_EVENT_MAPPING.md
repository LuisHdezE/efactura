# API Audit Event Mapping

Durable business/security audit is independent from Serilog/Application Insights. The accepted Architecture audit catalog remains authoritative; this document maps public v1 commands/access paths to durable events.

Every event carries actor/system identity, company/scope, occurred/recorded time, correlation ID, target aggregate/resource, outcome and reason/context. When idempotency applies, the event also links the idempotency key/record. Fiscal decisions preserve applicable rule/specification provenance. Secrets/private keys/tokens/full unnecessary PII are forbidden.

## Identity / organization / configuration

| operationId(s) | Durable event |
|---|---|
| `createUser` | `security.user.created` |
| `updateUser` | `security.user.updated` |
| `createRole` | `security.role.created` |
| `updateRole` | `security.role.updated` |
| `assignUserRoles` | `security.user_roles.changed` |
| `updateCurrentCompany` | `organization.company.updated` |
| `createLocation`, `updateLocation` | `organization.location.created|updated` |
| `registerTerminal`, `updateTerminal` | `organization.terminal.registered|updated` |
| `updateFiscalConfiguration` | `fiscal.configuration.updated` |
| `updateIntegration` | `integration.updated` |
| `createPaymentMethod`, `updatePaymentMethod` | `payment_method.created|updated` |

Role/permission/scope changes record target principal, before/after composition and acting administrator. A generic technical log is insufficient.

## Parties / catalog

| operationId(s) | Durable event |
|---|---|
| `createParty` | `party.created` |
| `updateParty` | `party.updated` |
| `addPartyFiscalIdentity`, `updatePartyFiscalIdentity` | `party.fiscal_identity.added|updated` |
| `setPartyRoles` | `party.roles.changed` |
| `createItem`, `updateItem`, `deactivateItem` | `catalog.item.created|updated|deactivated` |
| `createItemCategory`, `updateItemCategory` | `catalog.category.created|updated` |

Fiscal identity events preserve identity type/country/normalized reference needed for accountability, with privacy minimization.

## Sales / fiscal

| operationId(s) | Durable event |
|---|---|
| `createSale` | `sale.created` |
| `updateSaleDraft` | `sale.draft.updated` when configured as material business audit; technical churn may be summarized |
| `validateSale` | `sale.validated` when validation/rule decision must be retained; final confirmation always preserves the decisive rule snapshot |
| `confirmSale` | `sale.confirmed` plus downstream fiscal/financial/inventory events committed by their owning modules |
| `cancelSale` | `sale.cancelled` |
| `createFiscalCorrection` | `fiscal.correction.requested`; later workflow emits generated/signed/submitted/accepted/rejected events |
| `resolveRegularizationCase` | `fiscal.regularization.resolved` |
| `downloadFiscalXml` | `fiscal.document_artifact.accessed` |
| `downloadFiscalRepresentation` | `fiscal.document_representation.accessed` when policy requires access evidence |
| `requestFiscalDocumentDelivery` | `fiscal.delivery.requested` |

Internal workers also emit durable events not caused by a direct public endpoint: fiscal number reserved, fiscal document built, validated, signed, queued, transport submitted, envelope acknowledged, individual CFE accepted/rejected, provider callback replay/rejection and outbox dead-letter/recovery.

## CAE / contingency

| operationId(s) | Durable event |
|---|---|
| `importCaeAuthorization` | `cae.imported` |
| `activateCaeAuthorization` | `cae.activated` |
| `createCaeAllocation` | `cae.allocation.created` |
| `closeCaeAllocation` | `cae.allocation.closed` |
| `enterContingency` | `contingency.entered` |
| `exitContingency` | `contingency.exited` |
| `registerContingencyDocument` | `contingency.document.registered` |
| `reconcileContingencyDocument` | `contingency.document.reconciled` |

CAE number consumption itself is audited by the fiscal allocator even though it is an internal step of another command.

## Inventory / procurement

| operationId(s) | Durable event |
|---|---|
| `createStockAdjustment` | `inventory.adjustment.posted` |
| `createStockTransfer` | `inventory.transfer.created` |
| `approveStockTransfer` | `inventory.transfer.approved` |
| `dispatchStockTransfer` | `inventory.transfer.dispatched` + stock movement evidence |
| `receiveStockTransfer` | `inventory.transfer.received` + stock movement/discrepancy evidence |
| `reconcileStockTransfer` | `inventory.transfer.reconciled` |
| `createPurchaseOrder`, `updatePurchaseOrderDraft` | `procurement.po.created|updated` |
| `approvePurchaseOrder`, `cancelPurchaseOrder` | `procurement.po.approved|cancelled` |
| `createGoodsReceipt` | `procurement.receipt.created` |
| `postGoodsReceipt` | `procurement.receipt.posted` + linked stock/payable evidence |

`simulateReplenishment` does not mutate stock. Advisory simulation may be technical/decision evidence when saved, but it never masquerades as an inventory movement.

## Receivables / payables / treasury

| operationId(s) | Durable event |
|---|---|
| `createReceivableAdjustment` | `receivable.adjusted` |
| `createCollection` | `collection.created` plus allocation details |
| `reverseCollection` | `collection.reversed` via compensating facts |
| `createPayableAdjustment` | `payable.adjusted` |
| `createSupplierPayment` | `supplier_payment.created` plus allocations |
| `reverseSupplierPayment` | `supplier_payment.reversed` |

Financial audit stores original/allocated amounts, currency, source obligations and reason without deleting prior allocations.

## Cash

| operationId(s) | Durable event |
|---|---|
| `openCashShift` | `cash.shift.opened` |
| `createCashMovement` | `cash.movement.posted` |
| `closeCashShift` | `cash.shift.closed` with expected/count/variance context |
| `reconcileCashShift` | `cash.shift.reconciled` with approver/reason |

## Offline / devices

| operationId(s) | Durable event |
|---|---|
| `registerDevice` | `sync.device.registered` |
| `revokeDevice` | `sync.device.revoked` |
| `createSyncBatch` | `sync.batch.processed` plus per-operation `applied|already_applied|rejected|conflict|review_required|dependency_blocked` evidence |

Sync events preserve device ID, clientOperationId, material payload hash, canonical result and permission/revalidation outcome. The full sensitive client payload is not copied blindly into audit.

## Received CFE / reports / export / audit access

| operationId(s) | Durable event |
|---|---|
| `importReceivedFiscalDocument` | `received_fiscal.imported` or duplicate/invalid outcome |
| `importReceivedFiscalDocumentsBatch` | `received_fiscal.batch_imported` with per-file outcomes |
| `downloadReceivedFiscalArtifact` | `received_fiscal.artifact.accessed` |
| `validateFiscalXml` | `fiscal_xml.validation.requested` when validation evidence must be accountable |
| `generateDailyFiscalReport` | `fiscal.daily_report.generated` |
| `submitDailyFiscalReport` | `fiscal.daily_report.submission_requested`; workers add transport/result events |
| `createAuditExport` | `audit.export.requested` |
| `getAuditExport` | `audit.export.accessed` when artifact/status access is sensitive |
| `listAuditEvents`, `getAuditEvent` | `audit.events.queried|audit.event.read` according to security policy |
| `createAccountingExport` | `accounting.export.requested` |
| `getAccountingExport` | `accounting.export.accessed` when download/artifact is accessed |
| `acknowledgeAlert` | `alert.acknowledged` |

## Transaction rule

When a durable business mutation commits, its required audit evidence participates in the same local transaction or a transactionally coupled durable mechanism. Failed authorization can create a separate security event but must never create a false business-success event.

There is no normal API for updating/deleting durable audit events.