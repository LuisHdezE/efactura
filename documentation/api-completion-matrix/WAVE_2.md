# API Completion Master Matrix — Wave 2

Status: `FULL_OPERATION_LEVEL_RECONCILED / W2.1_CLOSED / W2.2_MERGED_DEPLOYED_RUNTIME_PENDING / W2.3_IMPLEMENTATION_CANDIDATE_PENDING_MERGE`

Parent index: `documentation/API_COMPLETION_MASTER_MATRIX.md`.

Scope: **Parties + Catalog + Sales Completion**.

Accepted baseline after PR #216 merge: **26 operation IDs**, **24 implemented HTTP surfaces**, **2 non-implemented**.

W2.3 PR #221 is the governed implementation candidate for `API-POS-001 getPosBootstrap`; if accepted and merged, Wave 2 becomes **25 / 26 implemented** with only `API-PTY-008 getPartyAccountSummary` remaining.

Detailed readiness evidence for the original four missing operations is recorded in `W2_READINESS_AUDIT.md`.

W2.1 field-level contract: `W2_1_SALE_FISCALIZATION_STATUS_CONTRACT.md`.

W2.1 operational closure: `W2_1_SALE_FISCALIZATION_STATUS_RUNTIME_CLOSURE.md`.

W2.2 owner-locked lifecycle contract: `W2_2_SALE_CANCELLATION_CONTRACT.md`.

W2.3 owner-locked contract: `W2_3_POS_BOOTSTRAP_CONTRACT.md`.

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
| `API-POS-001` | `getPosBootstrap` | GET `/api/v1/pos/bootstrap` | `sales.read` | ACCEPTED | IMPLEMENTATION_CANDIDATE | `PosBootstrapController -> GetPosBootstrapUseCase -> IFiscalLocationRepository + ITerminalRepository` | IMPLEMENTED_PENDING_MERGE | 2 | PR #221 candidate implements actor-scoped active location/terminal contexts, deterministic ordering and `private, no-cache` ETag revalidation. No payment/catalog/party/pricing/fiscal/stock aggregation; no migration or production mutation |
| `API-SAL-001` | `listSales` | GET `/api/v1/sales` | `sales.read` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-002` | `createSale` | POST `/api/v1/sales` | `sales.create` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-003` | `getSale` | GET `/api/v1/sales/{saleId}` | `sales.read` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-004` | `updateSaleDraft` | PATCH `/api/v1/sales/{saleId}` | `sales.create` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-005` | `validateSale` | POST `/api/v1/sales/{saleId}/validate` | `sales.create` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-006` | `getSaleFiscalPreview` | GET `/api/v1/sales/{saleId}/fiscal-preview` | `sales.read` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-007` | `confirmSale` | POST `/api/v1/sales/{saleId}/confirm` | `sales.confirm` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-008` | `cancelSale` | POST `/api/v1/sales/{saleId}/cancel` | `sales.cancel` | ACCEPTED | IMPLEMENTED | `SaleCancellationController` | MERGED_DEPLOYED_RUNTIME_PENDING | 2 | PR #216 merged as `1f627b37d1487a428f6e7582dda37d45623faaa2`; post-merge Guard #712 passed including PostgreSQL/MySQL provider-real tests; Deploy API Demo #56 promoted `efactura-api-d22-1f627b3-56-1` to 100% with canary/public smoke PASS. Production has no Sale rows or commercial items, so mutating runtime acceptance remains a separate owner-approval gate |
| `API-SAL-009` | `getSaleFiscalizationStatus` | GET `/api/v1/sales/{saleId}/fiscalization` | `sales.read` | ACCEPTED | IMPLEMENTED | `SaleFiscalizationController` | EXISTING_PATH / regression | 2 | W2.1 closed after PR #208 merge, Guard #695/#696, Deploy #55 and read-only production runtime acceptance run `35643849728`; see closure evidence |
