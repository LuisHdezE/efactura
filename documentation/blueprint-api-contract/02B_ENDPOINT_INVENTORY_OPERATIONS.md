# API v1 Endpoint Inventory — Operations, Finance, Sync and Reporting

Status: `READY_FOR_REVIEW`.

Continuation of `02A_ENDPOINT_INVENTORY_CORE.md`.

| API ID | operationId | Method / path | Permission | Idem | Purpose |
|---|---|---|---|---|---|
| `API-INV-001` | `listInventoryPositions` | GET `/api/v1/inventory/positions` | `inventory.read` | NO | Search stock positions by item/location |
| `API-INV-002` | `getInventoryPosition` | GET `/api/v1/inventory/positions/{positionId}` | `inventory.read` | NO | Read stock position/version |
| `API-INV-003` | `listStockMovements` | GET `/api/v1/inventory/movements` | `inventory.read` | NO | Read immutable movement history |
| `API-INV-004` | `createStockAdjustment` | POST `/api/v1/inventory/adjustments` | `inventory.adjust` | REQUIRED | Create reasoned auditable stock adjustment |
| `API-TRF-001` | `listStockTransfers` | GET `/api/v1/stock-transfers` | `inventory.read` | NO | List transfers |
| `API-TRF-002` | `createStockTransfer` | POST `/api/v1/stock-transfers` | `inventory.transfer` | REQUIRED | Create transfer request |
| `API-TRF-003` | `getStockTransfer` | GET `/api/v1/stock-transfers/{transferId}` | `inventory.read` | NO | Read transfer/lines/state |
| `API-TRF-004` | `approveStockTransfer` | POST `/api/v1/stock-transfers/{transferId}/approve` | `inventory.transfer` | REQUIRED | Approve transfer |
| `API-TRF-005` | `dispatchStockTransfer` | POST `/api/v1/stock-transfers/{transferId}/dispatch` | `inventory.transfer` | REQUIRED | Dispatch transfer and post source movement effects |
| `API-TRF-006` | `receiveStockTransfer` | POST `/api/v1/stock-transfers/{transferId}/receive` | `inventory.transfer` | REQUIRED | Receive transfer and record destination/discrepancies |
| `API-TRF-007` | `reconcileStockTransfer` | POST `/api/v1/stock-transfers/{transferId}/reconcile` | `inventory.transfer` | REQUIRED | Resolve authorized transfer discrepancy |
| `API-RPL-001` | `simulateReplenishment` | POST `/api/v1/replenishment/simulations` | `inventory.read` | OPTIONAL | Run EOQ/ROP advisory simulation without stock mutation |
| `API-RPL-002` | `listReplenishmentRecommendations` | GET `/api/v1/replenishment/recommendations` | `inventory.read` | NO | List server-computed replenishment recommendations |
| `API-PRC-001` | `listPurchaseOrders` | GET `/api/v1/purchase-orders` | `procurement.read` | NO | List/filter purchase orders |
| `API-PRC-002` | `createPurchaseOrder` | POST `/api/v1/purchase-orders` | `procurement.manage` | REQUIRED | Create PO draft |
| `API-PRC-003` | `getPurchaseOrder` | GET `/api/v1/purchase-orders/{purchaseOrderId}` | `procurement.read` | NO | Read PO |
| `API-PRC-004` | `updatePurchaseOrderDraft` | PATCH `/api/v1/purchase-orders/{purchaseOrderId}` | `procurement.manage` | REQUIRED | Update PO while editable |
| `API-PRC-005` | `approvePurchaseOrder` | POST `/api/v1/purchase-orders/{purchaseOrderId}/approve` | `procurement.approve` | REQUIRED | Approve PO |
| `API-PRC-006` | `cancelPurchaseOrder` | POST `/api/v1/purchase-orders/{purchaseOrderId}/cancel` | `procurement.manage` | REQUIRED | Cancel PO when permitted |
| `API-GRC-001` | `listGoodsReceipts` | GET `/api/v1/goods-receipts` | `procurement.read` | NO | List goods receipts |
| `API-GRC-002` | `createGoodsReceipt` | POST `/api/v1/goods-receipts` | `procurement.receive` | REQUIRED | Create receipt draft linked to PO/source |
| `API-GRC-003` | `getGoodsReceipt` | GET `/api/v1/goods-receipts/{receiptId}` | `procurement.read` | NO | Read receipt/discrepancy state |
| `API-GRC-004` | `postGoodsReceipt` | POST `/api/v1/goods-receipts/{receiptId}/post` | `procurement.receive` | REQUIRED | Post receipt, stock and linked payable evidence under policy |
| `API-AR-001` | `listReceivables` | GET `/api/v1/receivables` | `receivables.read` | NO | Search receivables/open balances |
| `API-AR-002` | `getReceivable` | GET `/api/v1/receivables/{receivableId}` | `receivables.read` | NO | Read obligation/allocations/balance |
| `API-AR-003` | `getReceivablesAging` | GET `/api/v1/receivables/aging` | `receivables.read` | NO | Authoritative aging summary |
| `API-AR-004` | `createReceivableAdjustment` | POST `/api/v1/receivables/{receivableId}/adjustments` | `receivables.adjust` | REQUIRED | Append authorized receivable adjustment |
| `API-COL-001` | `createCollection` | POST `/api/v1/collections` | `receivables.collect` | REQUIRED | Create customer collection with one-or-many receivable allocations |
| `API-COL-002` | `getCollection` | GET `/api/v1/collections/{collectionId}` | `receivables.read` | NO | Read collection and allocations |
| `API-COL-003` | `reverseCollection` | POST `/api/v1/collections/{collectionId}/reverse` | `receivables.collect` | REQUIRED | Reverse via compensating financial facts under policy |
| `API-AP-001` | `listPayables` | GET `/api/v1/payables` | `payables.read` | NO | Search payables/open balances |
| `API-AP-002` | `getPayable` | GET `/api/v1/payables/{payableId}` | `payables.read` | NO | Read payable/allocations/balance |
| `API-AP-003` | `getPayablesAging` | GET `/api/v1/payables/aging` | `payables.read` | NO | Authoritative payable aging |
| `API-AP-004` | `createPayableAdjustment` | POST `/api/v1/payables/{payableId}/adjustments` | `payables.adjust` | REQUIRED | Append authorized payable adjustment |
| `API-PAY-001` | `createSupplierPayment` | POST `/api/v1/supplier-payments` | `payables.pay` | REQUIRED | Create supplier payment with one-or-many payable allocations |
| `API-PAY-002` | `getSupplierPayment` | GET `/api/v1/supplier-payments/{paymentId}` | `payables.read` | NO | Read supplier payment/allocations |
| `API-PAY-003` | `reverseSupplierPayment` | POST `/api/v1/supplier-payments/{paymentId}/reverse` | `payables.pay` | REQUIRED | Reverse supplier payment via compensating facts |
| `API-CSH-001` | `getCurrentCashShift` | GET `/api/v1/cash-shifts/current` | `cash.read` | NO | Read current actor/terminal shift if any |
| `API-CSH-002` | `openCashShift` | POST `/api/v1/cash-shifts` | `cash.open` | REQUIRED | Open terminal cash shift |
| `API-CSH-003` | `getCashShift` | GET `/api/v1/cash-shifts/{cashShiftId}` | `cash.read` | NO | Read shift expected/count/reconciliation state |
| `API-CSH-004` | `listCashMovements` | GET `/api/v1/cash-shifts/{cashShiftId}/movements` | `cash.read` | NO | Read cash movement ledger |
| `API-CSH-005` | `createCashMovement` | POST `/api/v1/cash-shifts/{cashShiftId}/movements` | `cash.move` | REQUIRED | Create authorized manual cash in/out movement |
| `API-CSH-006` | `closeCashShift` | POST `/api/v1/cash-shifts/{cashShiftId}/close` | `cash.close` | REQUIRED | Submit counted values and close/reconciliation calculation |
| `API-CSH-007` | `reconcileCashShift` | POST `/api/v1/cash-shifts/{cashShiftId}/reconcile` | `cash.reconcile` | REQUIRED | Approve/resolve variance with reason |
| `API-DEV-001` | `registerDevice` | POST `/api/v1/devices` | `sync.device.manage` | REQUIRED | Register sync/offline device in authorized scope |
| `API-DEV-002` | `getDevice` | GET `/api/v1/devices/{deviceId}` | `sync.use` | NO | Read device/sync/grant status |
| `API-DEV-003` | `revokeDevice` | POST `/api/v1/devices/{deviceId}/revoke` | `sync.device.manage` | REQUIRED | Revoke device/offline capability |
| `API-SYN-001` | `createSyncBatch` | POST `/api/v1/sync/batches` | `sync.use` | REQUIRED | Submit ordered offline operations with deterministic per-operation result |
| `API-SYN-002` | `getSyncBatch` | GET `/api/v1/sync/batches/{batchId}` | `sync.use` | NO | Read batch progress/results |
| `API-SYN-003` | `getSyncOperation` | GET `/api/v1/sync/operations/{clientOperationId}` | `sync.use` | NO | Read canonical replay result |
| `API-SYN-004` | `getSyncChanges` | GET `/api/v1/sync/changes` | `sync.use` | NO | Get scoped delta feed from cursor |
| `API-RCV-001` | `listReceivedFiscalDocuments` | GET `/api/v1/received-fiscal-documents` | `received_fiscal.read` | NO | Search received CFE and validation/linkage status |
| `API-RCV-002` | `importReceivedFiscalDocument` | POST `/api/v1/received-fiscal-documents/import` | `received_fiscal.import` | REQUIRED | Import one received fiscal artifact with duplicate detection |
| `API-RCV-003` | `importReceivedFiscalDocumentsBatch` | POST `/api/v1/received-fiscal-documents/import-batch` | `received_fiscal.import` | REQUIRED | Import bounded batch of received fiscal artifacts |
| `API-RCV-004` | `getReceivedFiscalDocument` | GET `/api/v1/received-fiscal-documents/{receivedDocumentId}` | `received_fiscal.read` | NO | Read received document metadata/validation/linkage |
| `API-RCV-005` | `downloadReceivedFiscalArtifact` | GET `/api/v1/received-fiscal-documents/{receivedDocumentId}/artifact` | `received_fiscal.read` | NO | Download original received artifact if authorized |
| `API-RCV-006` | `listReceivedFiscalValidationFindings` | GET `/api/v1/received-fiscal-documents/{receivedDocumentId}/findings` | `received_fiscal.read` | NO | Read structured validation findings |
| `API-XML-001` | `validateFiscalXml` | POST `/api/v1/fiscal-validation/xml` | `received_fiscal.validate` | OPTIONAL | Validate supplied CFE XML without importing as canonical document |
| `API-REP-001` | `getSalesReport` | GET `/api/v1/reports/sales` | `reports.read` | NO | Sales report data |
| `API-REP-002` | `getTaxReport` | GET `/api/v1/reports/tax` | `reports.read` | NO | Tax report data separated from statutory submission |
| `API-REP-003` | `getInventoryReport` | GET `/api/v1/reports/inventory` | `reports.read` | NO | Inventory valuation/movement/position report data |
| `API-REP-004` | `getReceivablesReport` | GET `/api/v1/reports/receivables-aging` | `reports.read` | NO | Receivables aging report |
| `API-REP-005` | `getPayablesReport` | GET `/api/v1/reports/payables-aging` | `reports.read` | NO | Payables aging report |
| `API-REP-006` | `getCashFlowReport` | GET `/api/v1/reports/cash-flow` | `reports.read` | NO | Projected cash-flow data |
| `API-DFR-001` | `listDailyFiscalReports` | GET `/api/v1/fiscal-reports/daily` | `fiscal.report.read` | NO | List statutory daily reports/state |
| `API-DFR-002` | `getDailyFiscalReport` | GET `/api/v1/fiscal-reports/daily/{reportId}` | `fiscal.report.read` | NO | Read statutory report generation/submission/ack state |
| `API-DFR-003` | `generateDailyFiscalReport` | POST `/api/v1/fiscal-reports/daily/generate` | `fiscal.report.manage` | REQUIRED | Generate/rebuild allowed daily report from authoritative fiscal consumption |
| `API-DFR-004` | `submitDailyFiscalReport` | POST `/api/v1/fiscal-reports/daily/{reportId}/submit` | `fiscal.report.manage` | REQUIRED | Queue/submit statutory daily report |
| `API-CAL-001` | `getFiscalCalendar` | GET `/api/v1/fiscal-calendar` | `reports.read` | NO | Read sourced fiscal obligations/calendar |
| `API-AUD-001` | `listAuditEvents` | GET `/api/v1/audit-events` | `audit.read` | NO | Search durable audit by authorized scope |
| `API-AUD-002` | `getAuditEvent` | GET `/api/v1/audit-events/{auditEventId}` | `audit.read` | NO | Read one durable audit event |
| `API-AUD-003` | `createAuditExport` | POST `/api/v1/audit-exports` | `audit.export` | REQUIRED | Create bounded auditable export job |
| `API-AUD-004` | `getAuditExport` | GET `/api/v1/audit-exports/{exportId}` | `audit.export` | NO | Get export status/download authorization metadata |
| `API-ALT-001` | `listAlerts` | GET `/api/v1/alerts` | `alerts.read` | NO | List scoped actionable CAE/stock/fiscal/integration alerts |
| `API-ALT-002` | `acknowledgeAlert` | POST `/api/v1/alerts/{alertId}/acknowledge` | `alerts.manage` | REQUIRED | Acknowledge operational alert |
| `API-MON-001` | `getIntegrationStatus` | GET `/api/v1/operations/integrations` | `operations.read` | NO | Safe integration/certificate/CAE/outbox status without secrets |
| `API-CFG-001` | `getFiscalConfiguration` | GET `/api/v1/configuration/fiscal` | `fiscal.configuration.read` | NO | Read safe effective fiscal configuration/profile |
| `API-CFG-002` | `updateFiscalConfiguration` | PATCH `/api/v1/configuration/fiscal` | `fiscal.configuration.manage` | REQUIRED | Update approved non-secret fiscal policy/configuration |
| `API-INT-001` | `listIntegrations` | GET `/api/v1/integrations` | `integrations.read` | NO | List integration adapters and safe operational metadata |
| `API-INT-002` | `getIntegration` | GET `/api/v1/integrations/{integrationId}` | `integrations.read` | NO | Read one integration safe metadata/config state |
| `API-INT-003` | `updateIntegration` | PATCH `/api/v1/integrations/{integrationId}` | `integrations.manage` | REQUIRED | Update non-secret integration settings/reference to secret material |
| `API-DAS-001` | `getDashboardSummary` | GET `/api/v1/dashboard` | `dashboard.read` | NO | Permission-aware operational summary |
| `API-AEX-001` | `listAccountingExportFormats` | GET `/api/v1/accounting-exports/formats` | `accounting.export` | NO | List enabled accounting adapter formats |
| `API-AEX-002` | `createAccountingExport` | POST `/api/v1/accounting-exports` | `accounting.export` | REQUIRED | Create bounded accounting export job |
| `API-AEX-003` | `getAccountingExport` | GET `/api/v1/accounting-exports/{exportId}` | `accounting.export` | NO | Get accounting export status/download metadata |
| `API-REF-001` | `listCountries` | GET `/api/v1/reference-data/countries` | `AUTHENTICATED` | NO | Country reference data |
| `API-REF-002` | `listUruguayDepartments` | GET `/api/v1/reference-data/uruguay-departments` | `AUTHENTICATED` | NO | Uruguay department reference data |
| `API-REF-003` | `listFiscalIdentityTypes` | GET `/api/v1/reference-data/fiscal-identity-types` | `AUTHENTICATED` | NO | Fiscal identity types and issuing-country metadata |
| `API-REF-004` | `listCurrencies` | GET `/api/v1/reference-data/currencies` | `AUTHENTICATED` | NO | Supported currency metadata |
| `API-REF-005` | `listFiscalDocumentTypes` | GET `/api/v1/reference-data/fiscal-document-types` | `fiscal.read` | NO | Enabled/versioned fiscal document metadata, not issue authority |
| `API-REF-006` | `listInvoiceIndicators` | GET `/api/v1/reference-data/invoice-indicators` | `fiscal.read` | NO | Fiscal indicator metadata |
| `API-REF-007` | `listContactTypes` | GET `/api/v1/reference-data/contact-types` | `parties.read` | NO | Party contact-type metadata |
| `API-REF-008` | `listUnitsOfMeasure` | GET `/api/v1/reference-data/units-of-measure` | `catalog.read` | NO | Commercial/fiscal units of measure |

Together with 02A this forms the complete initial v1 public operation inventory. Internal workers/provider callbacks are not public client endpoints; they use application/inbox ports defined by Architecture.