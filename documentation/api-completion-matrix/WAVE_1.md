# API Completion Master Matrix — Wave 1

Status: `FULL_OPERATION_LEVEL_RECONCILED / W1_1D_REFERENCE_DATA_COMPLETED`

Parent index: `documentation/API_COMPLETION_MASTER_MATRIX.md`.

Scope: **Identity + Organization + Reference Data**.

Baseline: **30 operation IDs**, **15 implemented**, **15 non-implemented**.

`Deep readiness` is intentionally conservative. Missing HTTP rows remain `NOT_YET_AUDITED` until their bounded readiness audit is complete. `API-REF-001..008` completed the W1.1 readiness audit; W1.1A implemented REF-002/003, W1.1B implemented REF-001/004, W1.1C implemented REF-005/006, and W1.1D closes the deferred ContactType/UOM semantics and implements REF-007/008. Reference Data is now complete for the accepted Wave 1 contract.

| API ID | operationId | Method / path | Permission | Contract | Implementation | Current WebApi evidence | Deep readiness | Wave | Gap / blocker |
|---|---|---|---|---|---|---|---|---:|---|
| `API-IAM-001` | `getCurrentActor` | GET `/api/v1/me` | `AUTHENTICATED` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 1 | Define actor projection over current auth context |
| `API-IAM-002` | `listUsers` | GET `/api/v1/users` | `security.users.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 1 | User read model/use case/controller required |
| `API-IAM-003` | `getUser` | GET `/api/v1/users/{userId}` | `security.users.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 1 | User detail read model/use case/controller required |
| `API-IAM-004` | `createUser` | POST `/api/v1/users` | `security.users.manage` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 1 | Govern identity-linking boundary and idempotent create |
| `API-IAM-005` | `updateUser` | PATCH `/api/v1/users/{userId}` | `security.users.manage` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 1 | Mutable user metadata/status command required |
| `API-IAM-006` | `listRoles` | GET `/api/v1/roles` | `security.roles.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 1 | Role read model required |
| `API-IAM-007` | `getRole` | GET `/api/v1/roles/{roleId}` | `security.roles.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 1 | Role detail read model required |
| `API-IAM-008` | `createRole` | POST `/api/v1/roles` | `security.manage_roles` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 1 | Role composition command required |
| `API-IAM-009` | `updateRole` | PUT `/api/v1/roles/{roleId}` | `security.manage_roles` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 1 | Role replacement command required |
| `API-IAM-010` | `assignUserRoles` | PUT `/api/v1/users/{userId}/roles` | `security.manage_roles` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 1 | Scoped assignment command required |
| `API-IAM-011` | `listPermissions` | GET `/api/v1/permissions` | `security.roles.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 1 | Stable permission catalog projection required |
| `API-ORG-001` | `getCurrentCompany` | GET `/api/v1/company` | `organization.read` | ACCEPTED | IMPLEMENTED | `CompanyController.Get -> GetCurrentCompanyUseCase` | EXISTING_PATH / regression | 1 | Preserve and regression-test |
| `API-ORG-002` | `updateCurrentCompany` | PATCH `/api/v1/company` | `organization.manage` | ACCEPTED | IMPLEMENTED | `CompanyController.Update -> UpsertCompanyFiscalProfileUseCase` | EXISTING_PATH / regression | 1 | Preserve and regression-test |
| `API-ORG-003` | `listLocations` | GET `/api/v1/locations` | `organization.read` | ACCEPTED | IMPLEMENTED | `LocationsController.List -> ListFiscalLocationsUseCase` | EXISTING_PATH / regression | 1 | Preserve and regression-test |
| `API-ORG-004` | `createLocation` | POST `/api/v1/locations` | `organization.manage` | ACCEPTED | IMPLEMENTED | `LocationsController.Create -> CreateFiscalLocationUseCase` | EXISTING_PATH / regression | 1 | Preserve and regression-test |
| `API-ORG-005` | `getLocation` | GET `/api/v1/locations/{locationId}` | `organization.read` | ACCEPTED | IMPLEMENTED | `LocationsController.Get -> GetFiscalLocationUseCase` | EXISTING_PATH / regression | 1 | Preserve and regression-test |
| `API-ORG-006` | `updateLocation` | PATCH `/api/v1/locations/{locationId}` | `organization.manage` | ACCEPTED | IMPLEMENTED | `LocationsController.Update -> UpdateFiscalLocationUseCase` | EXISTING_PATH / regression | 1 | Preserve and regression-test |
| `API-ORG-007` | `listTerminals` | GET `/api/v1/terminals` | `organization.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 1 | Terminal aggregate/read model boundary required |
| `API-ORG-008` | `registerTerminal` | POST `/api/v1/terminals` | `organization.manage` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 1 | Terminal registration command required |
| `API-ORG-009` | `getTerminal` | GET `/api/v1/terminals/{terminalId}` | `organization.read` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 1 | Terminal detail read required |
| `API-ORG-010` | `updateTerminal` | PATCH `/api/v1/terminals/{terminalId}` | `organization.manage` | ACCEPTED | MISSING_HTTP | none | NOT_YET_AUDITED | 1 | Terminal status/location update required |
| `API-REF-001` | `listCountries` | GET `/api/v1/reference-data/countries` | `AUTHENTICATED` | ACCEPTED | IMPLEMENTED | `ReferenceDataController.ListCountries -> ListCountriesUseCase` | EXISTING_PATH / regression | 1 | Preserve authenticated provider-neutral 249-entry ISO 3166-1 alpha-2 snapshot |
| `API-REF-002` | `listUruguayDepartments` | GET `/api/v1/reference-data/uruguay-departments` | `AUTHENTICATED` | ACCEPTED | IMPLEMENTED | `ReferenceDataController.ListUruguayDepartments -> ListUruguayDepartmentsUseCase` | EXISTING_PATH / regression | 1 | Preserve authenticated-only, provider-neutral reference projection |
| `API-REF-003` | `listFiscalIdentityTypes` | GET `/api/v1/reference-data/fiscal-identity-types` | `AUTHENTICATED` | ACCEPTED | IMPLEMENTED | `ReferenceDataController.ListFiscalIdentityTypes -> ListFiscalIdentityTypesUseCase` | EXISTING_PATH / regression | 1 | Preserve versioned DGI identity metadata projection |
| `API-REF-004` | `listCurrencies` | GET `/api/v1/reference-data/currencies` | `AUTHENTICATED` | ACCEPTED | IMPLEMENTED | `ReferenceDataController.ListCurrencies -> ListCurrenciesUseCase` | EXISTING_PATH / regression | 1 | Preserve fail-closed Release-1 supported subset `USD`, `UYI`, `UYU`; ISO recognition alone does not imply product support |
| `API-REF-005` | `listFiscalDocumentTypes` | GET `/api/v1/reference-data/fiscal-document-types` | `fiscal.read` | ACCEPTED | IMPLEMENTED | `ReferenceDataController.ListFiscalDocumentTypes -> ListFiscalDocumentTypesUseCase` | EXISTING_PATH / regression | 1 | Preserve fail-closed Release-1 domestic issue-capability subset `101/102/103/111/112/113`; listing never grants issue authority and all rows require applicability validation |
| `API-REF-006` | `listInvoiceIndicators` | GET `/api/v1/reference-data/invoice-indicators` | `fiscal.read` | ACCEPTED | IMPLEMENTED | `ReferenceDataController.ListInvoiceIndicators -> ListInvoiceIndicatorsUseCase` | EXISTING_PATH / regression | 1 | Preserve exact Release-1 builder-supported indicator subset `1/2/3/10`; do not advertise the broader DGI catalog as application-supported |
| `API-REF-007` | `listContactTypes` | GET `/api/v1/reference-data/contact-types` | `parties.read` | ACCEPTED | IMPLEMENTED | `ReferenceDataController.ListContactTypes -> ListContactTypesUseCase` | EXISTING_PATH / regression | 1 | Preserve Release-1 compatibility defaults `PHONE/MOBILE/EMAIL/FAX` while keeping `PartyContact.TypeCode` configurable rather than converting the domain to an enum |
| `API-REF-008` | `listUnitsOfMeasure` | GET `/api/v1/reference-data/units-of-measure` | `catalog.read` | ACCEPTED | IMPLEMENTED | `ReferenceDataController.ListUnitsOfMeasure -> ListUnitsOfMeasureUseCase -> IUnitOfMeasureReferenceReader` | EXISTING_PATH / regression | 1 | Preserve scoped distinct active commercial-unit projection; DGI CFE 25.2 compatibility is only the existing max-4 constraint, never a fabricated DGI enumeration |
| `API-CAT-009` | `listTaxProfiles` | GET `/api/v1/tax-profiles` | `catalog.read` | ACCEPTED | IMPLEMENTED | `TaxProfilesController.List -> ListTaxProfilesUseCase` | EXISTING_PATH / regression | 1 | Preserve and regression-test |
