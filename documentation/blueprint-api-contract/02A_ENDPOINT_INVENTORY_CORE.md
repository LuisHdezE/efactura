# API v1 Endpoint Inventory — Core and Commercial/Fiscal

Status: `READY_FOR_REVIEW`.

Every row defines a stable project API ID and canonical future OpenAPI `operationId`. `REQUIRED` in Idem means `Idempotency-Key` is mandatory.

| API ID | operationId | Method / path | Permission | Idem | Purpose |
|---|---|---|---|---|---|
| `API-SYS-001` | `getHealth` | GET `/api/v1/health` | `PUBLIC` | NO | Liveness/readiness-safe health summary |
| `API-SYS-002` | `getVersion` | GET `/api/v1/version` | `PUBLIC` | NO | Minimal application/API version metadata |
| `API-IAM-001` | `getCurrentActor` | GET `/api/v1/me` | `AUTHENTICATED` | NO | Authenticated actor, effective permissions and allowed scopes |
| `API-IAM-002` | `listUsers` | GET `/api/v1/users` | `security.users.read` | NO | Search/list users without auth-provider secrets |
| `API-IAM-003` | `getUser` | GET `/api/v1/users/{userId}` | `security.users.read` | NO | Read user status and scope assignments |
| `API-IAM-004` | `createUser` | POST `/api/v1/users` | `security.users.manage` | REQUIRED | Create application user/link identity |
| `API-IAM-005` | `updateUser` | PATCH `/api/v1/users/{userId}` | `security.users.manage` | REQUIRED | Update allowed mutable user metadata/status |
| `API-IAM-006` | `listRoles` | GET `/api/v1/roles` | `security.roles.read` | NO | List role definitions |
| `API-IAM-007` | `getRole` | GET `/api/v1/roles/{roleId}` | `security.roles.read` | NO | Read role and permissions |
| `API-IAM-008` | `createRole` | POST `/api/v1/roles` | `security.manage_roles` | REQUIRED | Create role with permission composition |
| `API-IAM-009` | `updateRole` | PUT `/api/v1/roles/{roleId}` | `security.manage_roles` | REQUIRED | Replace role metadata/permission composition |
| `API-IAM-010` | `assignUserRoles` | PUT `/api/v1/users/{userId}/roles` | `security.manage_roles` | REQUIRED | Replace user role assignments within authorized scope |
| `API-IAM-011` | `listPermissions` | GET `/api/v1/permissions` | `security.roles.read` | NO | List stable application permission catalog |
| `API-ORG-001` | `getCurrentCompany` | GET `/api/v1/company` | `organization.read` | NO | Read current company/issuer profile safe projection |
| `API-ORG-002` | `updateCurrentCompany` | PATCH `/api/v1/company` | `organization.manage` | REQUIRED | Update mutable company/issuer master configuration |
| `API-ORG-003` | `listLocations` | GET `/api/v1/locations` | `organization.read` | NO | List operational/fiscal locations |
| `API-ORG-004` | `createLocation` | POST `/api/v1/locations` | `organization.manage` | REQUIRED | Create location |
| `API-ORG-005` | `getLocation` | GET `/api/v1/locations/{locationId}` | `organization.read` | NO | Read location |
| `API-ORG-006` | `updateLocation` | PATCH `/api/v1/locations/{locationId}` | `organization.manage` | REQUIRED | Update location |
| `API-ORG-007` | `listTerminals` | GET `/api/v1/terminals` | `organization.read` | NO | List POS/server terminal registrations |
| `API-ORG-008` | `registerTerminal` | POST `/api/v1/terminals` | `organization.manage` | REQUIRED | Register operational terminal |
| `API-ORG-009` | `getTerminal` | GET `/api/v1/terminals/{terminalId}` | `organization.read` | NO | Read terminal metadata/status |
| `API-ORG-010` | `updateTerminal` | PATCH `/api/v1/terminals/{terminalId}` | `organization.manage` | REQUIRED | Update terminal status/location metadata |
| `API-PTY-001` | `listParties` | GET `/api/v1/parties` | `parties.read` | NO | Search parties/customers/suppliers with role filters |
| `API-PTY-002` | `createParty` | POST `/api/v1/parties` | `parties.manage` | REQUIRED | Create party with initial roles and identity/contact data |
| `API-PTY-003` | `getParty` | GET `/api/v1/parties/{partyId}` | `parties.read` | NO | Read party with roles/fiscal identities |
| `API-PTY-004` | `updateParty` | PATCH `/api/v1/parties/{partyId}` | `parties.manage` | REQUIRED | Update mutable party master data |
| `API-PTY-005` | `addPartyFiscalIdentity` | POST `/api/v1/parties/{partyId}/fiscal-identities` | `parties.fiscal.manage` | REQUIRED | Add typed national/foreign fiscal identity |
| `API-PTY-006` | `updatePartyFiscalIdentity` | PUT `/api/v1/parties/{partyId}/fiscal-identities/{identityId}` | `parties.fiscal.manage` | REQUIRED | Update/expire identity under versioned validation rules |
| `API-PTY-007` | `setPartyRoles` | PUT `/api/v1/parties/{partyId}/roles` | `parties.manage` | REQUIRED | Set CUSTOMER/SUPPLIER roles |
| `API-PTY-008` | `getPartyAccountSummary` | GET `/api/v1/parties/{partyId}/account-summary` | `parties.read` | NO | Server-authoritative commercial balance/aging summary |
| `API-CAT-001` | `listItems` | GET `/api/v1/items` | `catalog.read` | NO | Search products/services with stock/tax metadata |
| `API-CAT-002` | `createItem` | POST `/api/v1/items` | `catalog.manage` | REQUIRED | Create product/service commercial item |
| `API-CAT-003` | `getItem` | GET `/api/v1/items/{itemId}` | `catalog.read` | NO | Read item |
| `API-CAT-004` | `updateItem` | PATCH `/api/v1/items/{itemId}` | `catalog.manage` | REQUIRED | Update mutable catalog definition |
| `API-CAT-005` | `deactivateItem` | POST `/api/v1/items/{itemId}/deactivate` | `catalog.manage` | REQUIRED | Deactivate item without deleting historical references |
| `API-CAT-006` | `listItemCategories` | GET `/api/v1/item-categories` | `catalog.read` | NO | List item categories |
| `API-CAT-007` | `createItemCategory` | POST `/api/v1/item-categories` | `catalog.manage` | REQUIRED | Create category |
| `API-CAT-008` | `updateItemCategory` | PATCH `/api/v1/item-categories/{categoryId}` | `catalog.manage` | REQUIRED | Update category |
| `API-CAT-009` | `listTaxProfiles` | GET `/api/v1/tax-profiles` | `catalog.read` | NO | Read usable tax profile metadata |
| `API-PMT-001` | `listPaymentMethods` | GET `/api/v1/payment-methods` | `payments.read` | NO | List enabled payment media for POS/treasury |
| `API-PMT-002` | `createPaymentMethod` | POST `/api/v1/payment-methods` | `payments.manage` | REQUIRED | Create enabled payment medium/config metadata |
| `API-PMT-003` | `updatePaymentMethod` | PATCH `/api/v1/payment-methods/{paymentMethodId}` | `payments.manage` | REQUIRED | Update/disable payment method without rewriting history |
| `API-POS-001` | `getPosBootstrap` | GET `/api/v1/pos/bootstrap` | `sales.read` | NO | Compact POS bootstrap/cache dataset with freshness metadata |
| `API-SAL-001` | `listSales` | GET `/api/v1/sales` | `sales.read` | NO | Search sales by date/customer/state/location |
| `API-SAL-002` | `createSale` | POST `/api/v1/sales` | `sales.create` | REQUIRED | Create sale draft |
| `API-SAL-003` | `getSale` | GET `/api/v1/sales/{saleId}` | `sales.read` | NO | Read sale commercial/fiscal/payment summary |
| `API-SAL-004` | `updateSaleDraft` | PATCH `/api/v1/sales/{saleId}` | `sales.create` | REQUIRED | Update DRAFT sale only |
| `API-SAL-005` | `validateSale` | POST `/api/v1/sales/{saleId}/validate` | `sales.create` | REQUIRED | Recompute/validate commercial, tax and fiscal eligibility without confirmation |
| `API-SAL-006` | `getSaleFiscalPreview` | GET `/api/v1/sales/{saleId}/fiscal-preview` | `sales.read` | NO | Explain eligible CFE/tax treatment/rule version before confirmation |
| `API-SAL-007` | `confirmSale` | POST `/api/v1/sales/{saleId}/confirm` | `sales.confirm` | REQUIRED | Confirm snapshot and trigger authorized effects/fiscalization |
| `API-SAL-008` | `cancelSale` | POST `/api/v1/sales/{saleId}/cancel` | `sales.cancel` | REQUIRED | Cancel only before an irreversible boundary when policy permits |
| `API-SAL-009` | `getSaleFiscalizationStatus` | GET `/api/v1/sales/{saleId}/fiscalization` | `sales.read` | NO | Read fiscalization workflow separately from sale state |
| `API-FIS-001` | `listFiscalDocuments` | GET `/api/v1/fiscal-documents` | `fiscal.read` | NO | Search CFE/CFC-linked fiscal documents |
| `API-FIS-002` | `getFiscalDocument` | GET `/api/v1/fiscal-documents/{fiscalDocumentId}` | `fiscal.read` | NO | Read fiscal snapshot/lifecycle safe projection |
| `API-FIS-003` | `downloadFiscalXml` | GET `/api/v1/fiscal-documents/{fiscalDocumentId}/xml` | `fiscal.read` | NO | Download authorized immutable fiscal XML |
| `API-FIS-004` | `downloadFiscalRepresentation` | GET `/api/v1/fiscal-documents/{fiscalDocumentId}/representation` | `fiscal.read` | NO | Download/stream printable representation |
| `API-FIS-005` | `listFiscalDocumentEvents` | GET `/api/v1/fiscal-documents/{fiscalDocumentId}/events` | `fiscal.read` | NO | Read generation/transport/result timeline |
| `API-FIS-006` | `createFiscalCorrection` | POST `/api/v1/fiscal-documents/{fiscalDocumentId}/corrections` | `fiscal.correct` | REQUIRED | Request permitted credit/debit correction via server rules |
| `API-FIS-007` | `listRegularizationCases` | GET `/api/v1/fiscal-regularizations` | `fiscal.regularization.manage` | NO | List rejected/regularization work queue |
| `API-FIS-008` | `getRegularizationCase` | GET `/api/v1/fiscal-regularizations/{caseId}` | `fiscal.regularization.manage` | NO | Read regularization case/evidence |
| `API-FIS-009` | `resolveRegularizationCase` | POST `/api/v1/fiscal-regularizations/{caseId}/resolve` | `fiscal.regularization.manage` | REQUIRED | Apply one permitted regularization disposition |
| `API-FDL-001` | `listFiscalDocumentDeliveries` | GET `/api/v1/fiscal-documents/{fiscalDocumentId}/deliveries` | `fiscal.read` | NO | Read delivery attempts separately from fiscal acceptance |
| `API-FDL-002` | `requestFiscalDocumentDelivery` | POST `/api/v1/fiscal-documents/{fiscalDocumentId}/deliveries` | `fiscal.read` | REQUIRED | Request delivery through an enabled channel |
| `API-CAE-001` | `listCaeAuthorizations` | GET `/api/v1/cae-authorizations` | `fiscal.read` | NO | List CAE ranges/status/usage/alerts |
| `API-CAE-002` | `getCaeAuthorization` | GET `/api/v1/cae-authorizations/{caeId}` | `fiscal.read` | NO | Read CAE metadata/range/allocation state |
| `API-CAE-003` | `importCaeAuthorization` | POST `/api/v1/cae-authorizations/import` | `fiscal.manage_cae` | REQUIRED | Import and verify DGI CAE artifact/metadata |
| `API-CAE-004` | `activateCaeAuthorization` | POST `/api/v1/cae-authorizations/{caeId}/activate` | `fiscal.manage_cae` | REQUIRED | Activate verified CAE for allocation |
| `API-CAE-005` | `listCaeAllocations` | GET `/api/v1/cae-authorizations/{caeId}/allocations` | `fiscal.read` | NO | List branch/cash operational allocations/subranges |
| `API-CAE-006` | `createCaeAllocation` | POST `/api/v1/cae-authorizations/{caeId}/allocations` | `fiscal.manage_cae` | REQUIRED | Allocate subrange without violating company-wide uniqueness |
| `API-CAE-007` | `closeCaeAllocation` | POST `/api/v1/cae-authorizations/{caeId}/allocations/{allocationId}/close` | `fiscal.manage_cae` | REQUIRED | Close allocation without deleting history |
| `API-CNT-001` | `getContingencyStatus` | GET `/api/v1/contingency/status` | `fiscal.read` | NO | Read formal contingency/CFC state |
| `API-CNT-002` | `enterContingency` | POST `/api/v1/contingency/enter` | `fiscal.manage_contingency` | REQUIRED | Enter formal CFC contingency with reason/scope |
| `API-CNT-003` | `exitContingency` | POST `/api/v1/contingency/exit` | `fiscal.manage_contingency` | REQUIRED | Exit contingency and start recovery |
| `API-CNT-004` | `listContingencyDocuments` | GET `/api/v1/contingency/documents` | `fiscal.read` | NO | List CFC documents/recovery states |
| `API-CNT-005` | `registerContingencyDocument` | POST `/api/v1/contingency/documents` | `fiscal.manage_contingency` | REQUIRED | Register issued CFC preserving original identity |
| `API-CNT-006` | `getContingencyDocument` | GET `/api/v1/contingency/documents/{contingencyDocumentId}` | `fiscal.read` | NO | Read CFC recovery/reporting state |
| `API-CNT-007` | `reconcileContingencyDocument` | POST `/api/v1/contingency/documents/{contingencyDocumentId}/reconcile` | `fiscal.manage_contingency` | REQUIRED | Perform permitted recovery/report linkage |

## Contract deferral

Dedicated e-Remito, e-Resguardo, e-Boleta de Entrada, Venta por Cuenta Ajena and specialized export issue commands are not assigned operationIds until their field-level DGI rule slices are accepted. `createFiscalCorrection` and ordinary sale endpoints cannot be used to bypass this.