# API Completion Master Matrix — Wave 2

Status: `FULL_OPERATION_LEVEL_RECONCILED / W2.1_CLOSED / W2.2_MERGED_DEPLOYED_RUNTIME_PENDING / W2.3_RUNTIME_ACCEPTED / W2.4_RUNTIME_ACCEPTED_READ_ONLY / HTTP_COMPLETE`

Parent index: `documentation/API_COMPLETION_MASTER_MATRIX.md`.

Scope: **Parties + Catalog + Sales Completion**.

Accepted baseline after W2.4 read-only production runtime acceptance: **26 operation IDs**, **26 implemented HTTP surfaces**, **0 non-implemented**.

`API-PTY-008 getPartyAccountSummary` is implemented, deployed and runtime-accepted read-only. Wave 2 now has no missing public HTTP surface; W2.2 mutation-based production runtime acceptance remains a separate owner-gated operational concern.

Detailed readiness evidence for the original four missing operations is recorded in `W2_READINESS_AUDIT.md`.

W2.1 field-level contract: `W2_1_SALE_FISCALIZATION_STATUS_CONTRACT.md`.

W2.1 operational closure: `W2_1_SALE_FISCALIZATION_STATUS_RUNTIME_CLOSURE.md`.

W2.2 owner-locked lifecycle contract: `W2_2_SALE_CANCELLATION_CONTRACT.md`.

W2.3 owner-locked contract: `W2_3_POS_BOOTSTRAP_CONTRACT.md`.

W2.3 runtime evidence: `W2_3_POS_BOOTSTRAP_RUNTIME_PLAN.md`.

W2.4 field-level HTTP contract: `W2_4_PARTY_ACCOUNT_SUMMARY_HTTP_CONTRACT.md`.

W2.4 operational closure: `W2_4_PARTY_ACCOUNT_SUMMARY_RUNTIME_CLOSURE.md`.

| API ID | operationId | Method / path | Permission | Contract | Implementation | Current WebApi evidence | Deep readiness | Wave | Gap / blocker |
|---|---|---|---|---|---|---|---|---:|---|
| `API-PTY-001` | `listParties` | GET `/api/v1/parties` | `parties.read` | ACCEPTED | IMPLEMENTED | `PartiesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-PTY-002` | `createParty` | POST `/api/v1/parties` | `parties.manage` | ACCEPTED | IMPLEMENTED | `PartiesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-PTY-003` | `getParty` | GET `/api/v1/parties/{partyId}` | `parties.read` | ACCEPTED | IMPLEMENTED | `PartiesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-PTY-004` | `updateParty` | PATCH `/api/v1/parties/{partyId}` | `parties.manage` | ACCEPTED | IMPLEMENTED | `PartiesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-PTY-005` | `addPartyFiscalIdentity` | POST `/api/v1/parties/{partyId}/fiscal-identities` | `parties.fiscal.manage` | ACCEPTED | IMPLEMENTED | `PartiesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-PTY-006` | `updatePartyFiscalIdentity` | PUT `/api/v1/parties/{partyId}/fiscal-identities/{identityId}` | `parties.fiscal.manage` | ACCEPTED | IMPLEMENTED | `PartiesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-PTY-007` | `setPartyRoles` | PUT `/api/v1/parties/{partyId}/roles` | `parties.manage` | ACCEPTED | IMPLEMENTED | `PartiesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-PTY-008` | `getPartyAccountSummary` | GET `/api/v1/parties/{partyId}/account-summary` | `parties.read` | ACCEPTED | IMPLEMENTED | `PartyAccountSummaryController -> IPartyAccountSummaryReadModel` | RUNTIME_ACCEPTED / regression | 2 | PR #238 merged at `7f8281c9f98e2c0a9171fbc9d56ee5bd872eb60b`; Deploy API Demo #62 promoted `efactura-api-d22-7f8281c-62-1`; bounded read-only production acceptance passed route/OpenAPI/auth/scope/not-found behavior with zero production Party rows, while successful 200 composition remains covered by exact-head controller and provider-real PostgreSQL/MySQL QA |
| `API-CAT-001` | `listItems` | GET `/api/v1/items` | `catalog.read` | ACCEPTED | IMPLEMENTED | `ItemsController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-CAT-002` | `createItem` | POST `/api/v1/items` | `catalog.manage` | ACCEPTED | IMPLEMENTED | `ItemsController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-CAT-003` | `getItem` | GET `/api/v1/items/{itemId}` | `catalog.read` | ACCEPTED | IMPLEMENTED | `ItemsController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-CAT-004` | `updateItem` | PATCH `/api/v1/items/{itemId}` | `catalog.manage` | ACCEPTED | IMPLEMENTED | `ItemsController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-CAT-005` | `deactivateItem` | POST `/api/v1/items/{itemId}/deactivate` | `catalog.manage` | ACCEPTED | IMPLEMENTED | `ItemsController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-CAT-006` | `listItemCategories` | GET `/api/v1/item-categories` | `catalog.read` | ACCEPTED | IMPLEMENTED | `ItemCategoriesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-CAT-007` | `createItemCategory` | POST `/api/v1/item-categories` | `catalog.manage` | ACCEPTED | IMPLEMENTED | `ItemCategoriesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-CAT-008` | `updateItemCategory` | PATCH `/api/v1/item-categories/{categoryId}` | `catalog.manage` | ACCEPTED | IMPLEMENTED | `ItemCategoriesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-POS-001` | `getPosBootstrap` | GET `/api/v1/pos/bootstrap` | `sales.read` | ACCEPTED | IMPLEMENTED | `PosBootstrapController -> GetPosBootstrapUseCase -> IFiscalLocationRepository + ITerminalRepository` | RUNTIME_ACCEPTED / regression | 2 | PR #221 implemented the actor-scoped active location/terminal projection. PR #223 repaired the permission-policy Problem Details boundary found by the first runtime attempt. Deploy API Demo #58 promoted `efactura-api-d22-47f97a8-58-1`; production runtime run `35802751788` passed GET-only OpenAPI, 401/403 boundaries, `permission_denied`, organization isolation, empty scoped projection, cache/ETag and 304 checks with no production writes or fixtures |
| `API-SAL-001` | `listSales` | GET `/api/v1/sales` | `sales.read` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-002` | `createSale` | POST `/api/v1/sales` | `sales.create` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-003` | `getSale` | GET `/api/v1/sales/{saleId}` | `sales.read` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-004` | `updateSaleDraft` | PATCH `/api/v1/sales/{saleId}` | `sales.create` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-005` | `validateSale` | POST `/api/v1/sales/{saleId}/validate` | `sales.create` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-006` | `getSaleFiscalPreview` | GET `/api/v1/sales/{saleId}/fiscal-preview` | `sales.read` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-007` | `confirmSale` | POST `/api/v1/sales/{saleId}/confirm` | `sales.confirm` | ACCEPTED | IMPLEMENTED | `SalesController` | EXISTING_PATH / regression | 2 | Preserve and regression-test |
| `API-SAL-008` | `cancelSale` | POST `/api/v1/sales/{saleId}/cancel` | `sales.cancel` | ACCEPTED | IMPLEMENTED | `SaleCancellationController` | MERGED_DEPLOYED_RUNTIME_PENDING | 2 | PR #216 merged as `1f627b37d1487a428f6e7582dda37d45623faaa2`; post-merge Guard #712 passed including PostgreSQL/MySQL provider-real tests; Deploy API Demo #56 promoted `efactura-api-d22-1f627b3-56-1` to 100% with canary/public smoke PASS. Production has no Sale rows or commercial items, so mutating runtime acceptance remains a separate owner-approval gate |
| `API-SAL-009` | `getSaleFiscalizationStatus` | GET `/api/v1/sales/{saleId}/fiscalization` | `sales.read` | ACCEPTED | IMPLEMENTED | `SaleFiscalizationController` | EXISTING_PATH / regression | 2 | W2.1 closed after PR #208 merge, Guard #695/#696, Deploy #55 and read-only production runtime acceptance run `35643849728`; see closure evidence |
