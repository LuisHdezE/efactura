# API Completion Master Matrix — Wave 2

Status: `FULL_OPERATION_LEVEL_RECONCILED / W2.1_CONTRACT_LOCKED`

Parent index: `documentation/API_COMPLETION_MASTER_MATRIX.md`.

Scope: **Parties + Catalog + Sales Completion**.

Baseline: **26 operation IDs**, **22 implemented**, **4 non-implemented**.

Detailed readiness evidence for the four missing operations is recorded in `W2_READINESS_AUDIT.md`.

W2.1 field-level contract: `W2_1_SALE_FISCALIZATION_STATUS_CONTRACT.md`.

| API ID | operationId | Method / path | Permission | Contract | Implementation | Current WebApi evidence | Deep readiness | Wave | Gap / blocker |
|---|---|---|---|---|---|---|---|---:|---|
| `API-PTY-001` | `listParties` | GET `/api/v1/parties` | `parties.read` | ACCEPTED | IMPLEMENTED | `PartiesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-PTY-002` | `createParty` | POST `/api/v1/parties` | `parties.manage` | ACCEPTED | IMPLEMENTED | `PartiesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-PTY-003` | `getParty` | GET `/api/v1/parties/{partyId}` | `parties.read` | ACCEPTED | IMPLEMENTED | `PartiesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-PTY-004` | `updateParty` | PATCH `/api/v1/parties/{partyId}` | `parties.manage` | ACCEPTED | IMPLEMENTED | `PartiesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-PTY-005` | `addPartyFiscalIdentity` | POST `/api/v1/parties/{partyId}/fiscal-identities` | `parties.fiscal.manage` | ACCEPTED | IMPLEMENTED | `PartiesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-PTY-006` | `updatePartyFiscalIdentity` | PUT `/api/v1/parties/{partyId}/fiscal-identities/{identityId}` | `parties.fiscal.manage` | ACCEPTED | IMPLEMENTED | `PartiesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-PTY-007` | `setPartyRoles` | PUT `/api/v1/parties/{partyId}/roles` | `parties.manage` | ACCEPTED | IMPLEMENTED | `PartiesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-PTY-008` | `getPartyAccountSummary` | GET `/api/v1/parties/{partyId}/account-summary` | `parties.read` | ACCEPTED | MISSING_HTTP | none | PREREQUISITE_REQUIRED | 2 | Authoritative party-scoped AR/AP balance-aging read model + field contract required; do not fake from Party master data or original receivable amounts |
| `API-CAT-001` | `listItems` | GET `/api/v1/items` | `catalog.read` | ACCEPTED | IMPLEMENTED | `ItemsController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-CAT-002` | `createItem` | POST `/api/v1/items` | `catalog.manage` | ACCEPTED | IMPLEMENTED | `ItemsController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-CAT-003` | `getItem` | GET `/api/v1/items/{itemId}` | `catalog.read` | ACCEPTED | IMPLEMENTED | `ItemsController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-CAT-004` | `updateItem` | PATCH `/api/v1/items/{itemId}` | `catalog.manage` | ACCEPTED | IMPLEMENTED | `ItemsController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-CAT-005` | `deactivateItem` | POST `/api/v1/items/{itemId}/deactivate` | `catalog.manage` | ACCEPTED | IMPLEMENTED | `ItemsController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-CAT-006` | `listItemCategories` | GET `/api/v1/item-categories` | `catalog.read` | ACCEPTED | IMPLEMENTED | `ItemCategoriesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-CAT-007` | `createItemCategory` | POST `/api/v1/item-categories` | `catalog.manage` | ACCEPTED | IMPLEMENTED | `ItemCategoriesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-CAT-008` | `updateItemCategory` | PATCH `/api/v1/item-categories/{categoryId}` | `catalog.manage` | ACCEPTED | IMPLEMENTED | `ItemCategoriesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-POS-001` | `getPosBootstrap` | GET `/api/v1/pos/bootstrap` | `sales.read` | ACCEPTED | MISSING_HTTP | none | PREREQUISITE_REQUIRED | 2 | Lock bootstrap sections, dependency set and freshness/cache contract before introducing POS composition |
| `API-SAL-001` | `listSales` | GET `/api/v1/sales` | `sales.read` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-002` | `createSale` | POST `/api/v1/sales` | `sales.create` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-003` | `getSale` | GET `/api/v1/sales/{saleId}` | `sales.read` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-004` | `updateSaleDraft` | PATCH `/api/v1/sales/{saleId}` | `sales.create` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-005` | `validateSale` | POST `/api/v1/sales/{saleId}/validate` | `sales.create` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-006` | `getSaleFiscalPreview` | GET `/api/v1/sales/{saleId}/fiscal-preview` | `sales.read` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-007` | `confirmSale` | POST `/api/v1/sales/{saleId}/confirm` | `sales.confirm` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-008` | `cancelSale` | POST `/api/v1/sales/{saleId}/cancel` | `sales.cancel` | ACCEPTED | MISSING_HTTP | none | PREREQUISITE_REQUIRED | 2 | Lock Cancelled lifecycle, cancellable states, irreversible confirmation boundary, request/version/reason and durable evidence semantics |
| `API-SAL-009` | `getSaleFiscalizationStatus` | GET `/api/v1/sales/{saleId}/fiscalization` | `sales.read` | ACCEPTED | MISSING_HTTP | none | CONTRACT_LOCKED_IMPLEMENTATION_READY | 2 | Field-level local-workflow contract locked in `W2_1_SALE_FISCALIZATION_STATUS_CONTRACT.md`; bounded read-only implementation pending |
