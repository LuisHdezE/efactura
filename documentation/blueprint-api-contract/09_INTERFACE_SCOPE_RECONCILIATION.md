# Interface Scope Reconciliation to API v1

## Purpose

Resolve every accepted `SCOPE_BASELINE` item into authoritative v1 API dependencies without authorizing client implementation. The later post-API `EXECUTABLE_INVENTORY` still reconciles each interface as COMMITTED/DEFERRED/DROPPED after API Gate.

Important authentication resolution: `WEB-001` and `APP-001` asked for login/session capability. The business API does **not** invent credential issuance. Token acquisition is external/deployment identity-provider lifecycle; `getCurrentActor` resolves authenticated application context and Device APIs resolve mobile device/offline context.

## Web

| Interface | Authoritative operationIds |
|---|---|
| `WEB-001` Login and Session Entry | `getCurrentActor` (API side); token acquisition external/deployment-specific |
| `WEB-002` Operational Dashboard | `getDashboardSummary`, `listAlerts`, `getIntegrationStatus` as permitted |
| `WEB-003` POS Sale | `getPosBootstrap`, `listPaymentMethods`, `listItems`, `listParties`, `createSale`, `getSale`, `updateSaleDraft`, `validateSale`, `getSaleFiscalPreview`, `confirmSale`, `cancelSale`, `getSaleFiscalizationStatus`, `downloadFiscalRepresentation` |
| `WEB-004` Customers and Parties | `listParties`, `createParty`, `getParty`, `updateParty`, `addPartyFiscalIdentity`, `updatePartyFiscalIdentity`, `setPartyRoles`, `getPartyAccountSummary`, `listCountries`, `listFiscalIdentityTypes` |
| `WEB-005` Suppliers | `listParties`, `createParty`, `getParty`, `updateParty`, `setPartyRoles`, `getPartyAccountSummary` |
| `WEB-006` Products and Services Catalog | `listItems`, `createItem`, `getItem`, `updateItem`, `deactivateItem`, `listItemCategories`, `createItemCategory`, `updateItemCategory`, `listTaxProfiles`, `listUnitsOfMeasure` |
| `WEB-007` Inventory and Movements | `listInventoryPositions`, `getInventoryPosition`, `listStockMovements`, `createStockAdjustment`, `simulateReplenishment`, `listReplenishmentRecommendations` |
| `WEB-008` Stock Transfers | `listStockTransfers`, `createStockTransfer`, `getStockTransfer`, `approveStockTransfer`, `dispatchStockTransfer`, `receiveStockTransfer`, `reconcileStockTransfer` |
| `WEB-009` Purchase Orders and Receipts | `listPurchaseOrders`, `createPurchaseOrder`, `getPurchaseOrder`, `updatePurchaseOrderDraft`, `approvePurchaseOrder`, `cancelPurchaseOrder`, `listGoodsReceipts`, `createGoodsReceipt`, `getGoodsReceipt`, `postGoodsReceipt` |
| `WEB-010` Accounts Receivable and Collections | `listReceivables`, `getReceivable`, `getReceivablesAging`, `createReceivableAdjustment`, `createCollection`, `getCollection`, `reverseCollection` |
| `WEB-011` Accounts Payable and Supplier Payments | `listPayables`, `getPayable`, `getPayablesAging`, `createPayableAdjustment`, `createSupplierPayment`, `getSupplierPayment`, `reverseSupplierPayment` |
| `WEB-012` Cash Shift and Reconciliation | `getCurrentCashShift`, `openCashShift`, `getCashShift`, `listCashMovements`, `createCashMovement`, `closeCashShift`, `reconcileCashShift` |
| `WEB-013` Fiscal Documents | `listFiscalDocuments`, `getFiscalDocument`, `downloadFiscalXml`, `downloadFiscalRepresentation`, `listFiscalDocumentEvents`, `createFiscalCorrection`, `listRegularizationCases`, `getRegularizationCase`, `resolveRegularizationCase`, `listFiscalDocumentDeliveries`, `requestFiscalDocumentDelivery` |
| `WEB-014` CAE Administration | `listCaeAuthorizations`, `getCaeAuthorization`, `importCaeAuthorization`, `activateCaeAuthorization`, `listCaeAllocations`, `createCaeAllocation`, `closeCaeAllocation` |
| `WEB-015` Contingency and Synchronization Supervision | `getContingencyStatus`, `enterContingency`, `exitContingency`, `listContingencyDocuments`, `registerContingencyDocument`, `getContingencyDocument`, `reconcileContingencyDocument`, `createSyncBatch`, `getSyncBatch`, `getSyncOperation` |
| `WEB-016` Received CFE and XML Validation | `listReceivedFiscalDocuments`, `importReceivedFiscalDocument`, `importReceivedFiscalDocumentsBatch`, `getReceivedFiscalDocument`, `downloadReceivedFiscalArtifact`, `listReceivedFiscalValidationFindings`, `validateFiscalXml` |
| `WEB-017` Reports and Fiscal Calendar | `getSalesReport`, `getTaxReport`, `getInventoryReport`, `getReceivablesReport`, `getPayablesReport`, `getCashFlowReport`, `listDailyFiscalReports`, `getDailyFiscalReport`, `generateDailyFiscalReport`, `submitDailyFiscalReport`, `getFiscalCalendar`, `listAccountingExportFormats`, `createAccountingExport`, `getAccountingExport` |
| `WEB-018` Audit, Security and Configuration | IAM/ORG operations, `listAuditEvents`, `getAuditEvent`, `createAuditExport`, `getAuditExport`, `getIntegrationStatus`, fiscal configuration and Integration operations |
| `WEB-019` Technical Operations Console | `getOperationsOverview`, `getOperationsHealth`, `listTechnicalEvents`, `getTechnicalEvent`, `getTraceTimeline`, `getOperationalMetrics`, `getOperationalMetricSeries`, `listOperationalDependencies`, `listOperationalIntegrations`, `getOperationalIntegration`, `listOperationalQueues`, `listOperationalWorkItems`, `getOperationalWorkItem`, `requestOperationalRetry`, `listOperationalWorkers`, `getOperationalSyncOverview`, `listOperationalAlerts`, `getOperationalAlert`, `acknowledgeOperationalAlert`, `createDiagnosticBundle`, `getDiagnosticBundle`, `downloadDiagnosticBundle` |

## Android

| Interface | Authoritative operationIds |
|---|---|
| `APP-001` Mobile Login and Device Session | `getCurrentActor`, `registerDevice`, `getDevice`, `revokeDevice`; token acquisition external/deployment-specific |
| `APP-002` Mobile POS Sale | `getPosBootstrap`, `listPaymentMethods`, `listItems`, `listParties`, `createSale`, `getSale`, `updateSaleDraft`, `validateSale`, `getSaleFiscalPreview`, `confirmSale`, `getSaleFiscalizationStatus` |
| `APP-003` Offline Queue and Synchronization | `createSyncBatch`, `getSyncBatch`, `getSyncOperation`, `getSyncChanges` |
| `APP-004` Customer Lookup | `listParties`, `getParty`, `listCountries`, `listFiscalIdentityTypes` |
| `APP-005` Product and Service Lookup | `getPosBootstrap`, `listItems`, `getItem`, `listTaxProfiles`, `listUnitsOfMeasure` |
| `APP-006` Mobile Inventory Operations | `listInventoryPositions`, `getInventoryPosition`, `listStockMovements`, `listStockTransfers`, `getStockTransfer`, `dispatchStockTransfer`, `receiveStockTransfer` subject to mobile/offline policy |
| `APP-007` Mobile Cash Shift | `getCurrentCashShift`, `openCashShift`, `getCashShift`, `listCashMovements`, `closeCashShift`; offline allowance remains Client Architecture policy |
| `APP-008` Fiscal Document Status and Representation | `getSaleFiscalizationStatus`, `getFiscalDocument`, `downloadFiscalRepresentation`, `listFiscalDocumentEvents` |

## Explicit deferrals

- e-Remito/e-Resguardo/e-Boleta Entrada/Venta por Cuenta Ajena public commands remain deferred pending accepted field-level rules;
- certificate/private-key provisioning endpoint is not invented while production custody remains OPEN;
- direct-DGI/provider-specific routes are not public client contracts;
- exact push-vs-poll notification strategy is not contracted; query/poll status is authoritative baseline;
- offline mutation availability is limited to later Client Architecture-approved command types. `createSyncBatch` is an allow-listed command transport, not arbitrary execution.

## Resolved cross-cutting needs

- pagination/filter/sort -> API conventions;
- Problem Details -> RFC 9457 contract;
- correlation -> `X-Correlation-Id`;
- idempotency/concurrency -> dedicated matrix;
- file/XML -> bounded validation/import contracts;
- artifacts -> permissioned fiscal/received-document endpoints;
- permission/scope context -> `getCurrentActor` + server policies;
- dashboard/alerts -> `getDashboardSummary` + alerts;
- offline sync -> batch/operation/cursor contract;
- technical observability -> `/api/v1/operations/**` normalized monitoring APIs;
- log/event search -> bounded sanitized `listTechnicalEvents`/`getTechnicalEvent`;
- incident reconstruction -> `getTraceTimeline`;
- health/metrics/queues/workers/integrations -> dedicated operations read contracts;
- privileged operational actions -> explicit `operations.*` permissioned/idempotent commands only.

No unresolved interface need is permission for a future client to invent server behavior.

## Amendment provenance

`WEB-019` comes from the human-accepted Technical Operations Console amendment merged in PR #10. Its detailed requirement/API/change-impact traceability is recorded in `15_TECHNICAL_OPERATIONS_TRACEABILITY_AND_CHANGE_IMPACT.md`.
