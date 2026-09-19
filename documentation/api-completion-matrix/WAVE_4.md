# API Completion Master Matrix — Wave 4

Status: `FULL_OPERATION_LEVEL_RECONCILED`

Parent index: `documentation/API_COMPLETION_MASTER_MATRIX.md`.

Scope: **Inventory + Transfers + Procurement + Receiving**.

Baseline: **23 operation IDs**, **4 implemented**, **19 non-implemented**.

| API ID | operationId | Method / path | Permission | Contract | Implementation | Current WebApi evidence | Deep readiness | Wave | Gap / blocker |
|---|---|---|---|---|---|---|---|---:|---|
| `API-INV-001` | `listInventoryPositions` | GET `/api/v1/inventory/positions` | `inventory.read` | ACCEPTED | IMPLEMENTED | `InventoryController` | EXISTING_PATH / regression | 4 | Preserve and regression-test |
| `API-INV-002` | `getInventoryPosition` | GET `/api/v1/inventory/positions/{positionId}` | `inventory.read` | ACCEPTED | IMPLEMENTED | `InventoryController` | EXISTING_PATH / regression | 4 | Preserve and regression-test |
| `API-INV-003` | `listStockMovements` | GET `/api/v1/inventory/movements` | `inventory.read` | ACCEPTED | IMPLEMENTED | `InventoryController` | EXISTING_PATH / regression | 4 | Preserve and regression-test |
| `API-INV-004` | `createStockAdjustment` | POST `/api/v1/inventory/adjustments` | `inventory.adjust` | ACCEPTED | IMPLEMENTED | `InventoryController` | EXISTING_PATH / regression | 4 | Preserve and regression-test |
| `API-TRF-001` | `listStockTransfers` | GET `/api/v1/stock-transfers` | `inventory.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 4 | Stock-transfer lifecycle required |
| `API-TRF-002` | `createStockTransfer` | POST `/api/v1/stock-transfers` | `inventory.transfer` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 4 | Stock-transfer lifecycle required |
| `API-TRF-003` | `getStockTransfer` | GET `/api/v1/stock-transfers/{transferId}` | `inventory.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 4 | Stock-transfer read model required |
| `API-TRF-004` | `approveStockTransfer` | POST `/api/v1/stock-transfers/{transferId}/approve` | `inventory.transfer` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 4 | Transfer approval transition required |
| `API-TRF-005` | `dispatchStockTransfer` | POST `/api/v1/stock-transfers/{transferId}/dispatch` | `inventory.transfer` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 4 | Dispatch + source stock effects required |
| `API-TRF-006` | `receiveStockTransfer` | POST `/api/v1/stock-transfers/{transferId}/receive` | `inventory.transfer` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 4 | Receive + destination/discrepancy effects required |
| `API-TRF-007` | `reconcileStockTransfer` | POST `/api/v1/stock-transfers/{transferId}/reconcile` | `inventory.transfer` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 4 | Transfer discrepancy reconciliation required |
| `API-RPL-001` | `simulateReplenishment` | POST `/api/v1/replenishment/simulations` | `inventory.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 4 | Advisory replenishment simulation required |
| `API-RPL-002` | `listReplenishmentRecommendations` | GET `/api/v1/replenishment/recommendations` | `inventory.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 4 | Replenishment recommendation projection required |
| `API-PRC-001` | `listPurchaseOrders` | GET `/api/v1/purchase-orders` | `procurement.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 4 | Purchase-order lifecycle required |
| `API-PRC-002` | `createPurchaseOrder` | POST `/api/v1/purchase-orders` | `procurement.manage` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 4 | Purchase-order lifecycle required |
| `API-PRC-003` | `getPurchaseOrder` | GET `/api/v1/purchase-orders/{purchaseOrderId}` | `procurement.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 4 | Purchase-order read model required |
| `API-PRC-004` | `updatePurchaseOrderDraft` | PATCH `/api/v1/purchase-orders/{purchaseOrderId}` | `procurement.manage` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 4 | Editable PO command required |
| `API-PRC-005` | `approvePurchaseOrder` | POST `/api/v1/purchase-orders/{purchaseOrderId}/approve` | `procurement.approve` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 4 | PO approval transition required |
| `API-PRC-006` | `cancelPurchaseOrder` | POST `/api/v1/purchase-orders/{purchaseOrderId}/cancel` | `procurement.manage` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 4 | Governed PO cancellation required |
| `API-GRC-001` | `listGoodsReceipts` | GET `/api/v1/goods-receipts` | `procurement.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 4 | Goods-receipt lifecycle required |
| `API-GRC-002` | `createGoodsReceipt` | POST `/api/v1/goods-receipts` | `procurement.receive` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 4 | Goods-receipt lifecycle required |
| `API-GRC-003` | `getGoodsReceipt` | GET `/api/v1/goods-receipts/{receiptId}` | `procurement.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 4 | Goods-receipt read/discrepancy projection required |
| `API-GRC-004` | `postGoodsReceipt` | POST `/api/v1/goods-receipts/{receiptId}/post` | `procurement.receive` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 4 | Receipt posting + stock/payable evidence boundary required |
