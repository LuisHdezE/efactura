# API Completion Master Matrix

Status: `BASELINE_RECONCILED / WAVE_1_OPERATION_LEVEL_AUDITED`

Baseline commit: `main@7c046f18a32cf373577d7338c2e26a9aae142144`.

This document is the operational source of truth for completing the public v1 API after D2 backend operational closure. It does not replace the accepted API contract files. It reconciles the accepted historical contract with the current inventory and current WebApi implementation.

## 1. Authoritative inputs

Current contract/inventory sources:

- `documentation/blueprint-api-contract/02A_ENDPOINT_INVENTORY_CORE.md`
- `documentation/blueprint-api-contract/02B_ENDPOINT_INVENTORY_OPERATIONS.md`
- `documentation/blueprint-api-contract/02C_ENDPOINT_INVENTORY_TECHNICAL_OPERATIONS.md`
- `documentation/blueprint-api-contract/11_API_CONTRACT_ACCEPTANCE_DRAFT.md`
- current `src/WebApi/Controllers/V1/**` implementation

No endpoint is considered implemented merely because Domain/Application capability exists. Public API implementation requires an actual v1 HTTP surface matching the governed method/path and permission boundary.

## 2. 193 vs 194 reconciliation

The historical API Contract Ready acceptance records:

- original commercial/fiscal/administrative design: `171` operations;
- Technical Operations Console amendment: `22` operations (`API-172..API-193`);
- historical accepted total: `193` operations.

The current endpoint inventories now contain:

- `02A_ENDPOINT_INVENTORY_CORE.md`: `79` operations;
- `02B_ENDPOINT_INVENTORY_OPERATIONS.md`: `93` operations;
- `02C_ENDPOINT_INVENTORY_TECHNICAL_OPERATIONS.md`: `22` operations;
- current inventory total: **194 operations**.

The extra operation is not a duplicate or counting defect. It is the later governed fiscal operation:

- `API-FIS-010`
- `collectFiscalEnvelopeDocumentResponseEvidence`
- `POST /api/v1/fiscal-envelopes/{envelopeId}/document-response-evidence`

That operation has current contract inventory, WebApi controller, Application composition and automated tests. Therefore the completion program uses **194** as the current public-v1 inventory denominator while preserving **193** as the historical API Contract Ready count that existed before `API-FIS-010` was added.

No existing API ID is renumbered to hide this chronology.

## 3. Current implementation baseline

The current v1 WebApi exposes ten controller surfaces:

| Surface | Contract operations implemented | Count |
|---|---:|---:|
| Company | `API-ORG-001..002` | 2 |
| Locations | `API-ORG-003..006` | 4 |
| Parties | `API-PTY-001..007` | 7 |
| Items | `API-CAT-001..005` | 5 |
| Item Categories | `API-CAT-006..008` | 3 |
| Tax Profiles | `API-CAT-009` | 1 |
| Sales | `API-SAL-001..007` | 7 |
| Inventory | `API-INV-001..004` | 4 |
| CAE | `API-CAE-001..007` | 7 |
| Fiscal ACKCFE evidence | `API-FIS-010` | 1 |
| **Total** |  | **41** |

Current raw public-v1 operation coverage is therefore:

`41 / 194 = 21.13%`

This is an operation-count measure only. It is not a product-readiness score and does not reduce the significance of deeper Domain/Application/fiscal capabilities that are not yet exposed as public endpoints.

## 4. Status vocabulary

- `IMPLEMENTED`: governed method/path has a current v1 WebApi action and matching Application dependency.
- `MISSING_HTTP`: contract exists but no matching public v1 endpoint exists.
- `PARTIAL`: an HTTP surface exists but one or more governed contract obligations are missing or materially mismatched.
- `DEFERRED_PENDING_RULES`: explicitly deferred by the accepted contract.
- `REVIEW_REQUIRED`: evidence is insufficient for a safe classification.

## 5. Wave assignment

The agreed completion sequence is:

1. Wave 1: Identity + Organization + Reference Data
2. Wave 2: Parties + Catalog + Sales Completion
3. Wave 3: Payments + Cash + AR/AP
4. Wave 4: Inventory + Transfers + Procurement + Receiving
5. Wave 5: Fiscal Completion + CAE + CFE Lifecycle
6. Wave 6: Reporting + Audit + Sync
7. Wave 7: Technical Operations Console

Cross-cutting system operations such as health/version are tracked with Wave 7 unless a prior wave requires them as a hard dependency. Payment methods remain in Wave 3. `API-CAT-009` is assigned to Wave 1 because it is reference/master tax metadata already consumed by catalog and sales.

## 6. Wave 1 operation-level matrix

Wave 1 contains **30** governed operations: 11 IAM + 10 Organization + 8 Reference Data + 1 Tax Profile reference operation.

Current Wave 1 coverage: **7 implemented / 30 total = 23.33%**.

| API ID | operationId | Method / path | Contract status | Implementation status | Current WebApi evidence | Wave | Gap / next action |
|---|---|---|---|---|---|---:|---|
| `API-IAM-001` | `getCurrentActor` | GET `/api/v1/me` | ACCEPTED | MISSING_HTTP | none | 1 | Define actor projection over current auth context |
| `API-IAM-002` | `listUsers` | GET `/api/v1/users` | ACCEPTED | MISSING_HTTP | none | 1 | User read model/use case/controller required |
| `API-IAM-003` | `getUser` | GET `/api/v1/users/{userId}` | ACCEPTED | MISSING_HTTP | none | 1 | User detail read model/use case/controller required |
| `API-IAM-004` | `createUser` | POST `/api/v1/users` | ACCEPTED | MISSING_HTTP | none | 1 | Govern identity-linking boundary and idempotent create |
| `API-IAM-005` | `updateUser` | PATCH `/api/v1/users/{userId}` | ACCEPTED | MISSING_HTTP | none | 1 | Mutable user metadata/status command required |
| `API-IAM-006` | `listRoles` | GET `/api/v1/roles` | ACCEPTED | MISSING_HTTP | none | 1 | Role read model required |
| `API-IAM-007` | `getRole` | GET `/api/v1/roles/{roleId}` | ACCEPTED | MISSING_HTTP | none | 1 | Role detail read model required |
| `API-IAM-008` | `createRole` | POST `/api/v1/roles` | ACCEPTED | MISSING_HTTP | none | 1 | Role composition command required |
| `API-IAM-009` | `updateRole` | PUT `/api/v1/roles/{roleId}` | ACCEPTED | MISSING_HTTP | none | 1 | Role replacement command required |
| `API-IAM-010` | `assignUserRoles` | PUT `/api/v1/users/{userId}/roles` | ACCEPTED | MISSING_HTTP | none | 1 | Scoped assignment command required |
| `API-IAM-011` | `listPermissions` | GET `/api/v1/permissions` | ACCEPTED | MISSING_HTTP | none | 1 | Stable permission catalog projection required |
| `API-ORG-001` | `getCurrentCompany` | GET `/api/v1/company` | ACCEPTED | IMPLEMENTED | `CompanyController.Get` -> `GetCurrentCompanyUseCase` | 1 | Preserve |
| `API-ORG-002` | `updateCurrentCompany` | PATCH `/api/v1/company` | ACCEPTED | IMPLEMENTED | `CompanyController.Update` -> `UpsertCompanyFiscalProfileUseCase` | 1 | Preserve |
| `API-ORG-003` | `listLocations` | GET `/api/v1/locations` | ACCEPTED | IMPLEMENTED | `LocationsController.List` -> `ListFiscalLocationsUseCase` | 1 | Preserve |
| `API-ORG-004` | `createLocation` | POST `/api/v1/locations` | ACCEPTED | IMPLEMENTED | `LocationsController.Create` -> `CreateFiscalLocationUseCase` | 1 | Preserve |
| `API-ORG-005` | `getLocation` | GET `/api/v1/locations/{locationId}` | ACCEPTED | IMPLEMENTED | `LocationsController.Get` -> `GetFiscalLocationUseCase` | 1 | Preserve |
| `API-ORG-006` | `updateLocation` | PATCH `/api/v1/locations/{locationId}` | ACCEPTED | IMPLEMENTED | `LocationsController.Update` -> `UpdateFiscalLocationUseCase` | 1 | Preserve |
| `API-ORG-007` | `listTerminals` | GET `/api/v1/terminals` | ACCEPTED | MISSING_HTTP | none | 1 | Terminal aggregate/read model boundary required |
| `API-ORG-008` | `registerTerminal` | POST `/api/v1/terminals` | ACCEPTED | MISSING_HTTP | none | 1 | Terminal registration command required |
| `API-ORG-009` | `getTerminal` | GET `/api/v1/terminals/{terminalId}` | ACCEPTED | MISSING_HTTP | none | 1 | Terminal detail read required |
| `API-ORG-010` | `updateTerminal` | PATCH `/api/v1/terminals/{terminalId}` | ACCEPTED | MISSING_HTTP | none | 1 | Terminal status/location update required |
| `API-REF-001` | `listCountries` | GET `/api/v1/reference-data/countries` | ACCEPTED | MISSING_HTTP | none | 1 | Reference projection required |
| `API-REF-002` | `listUruguayDepartments` | GET `/api/v1/reference-data/uruguay-departments` | ACCEPTED | MISSING_HTTP | none | 1 | Reference projection required |
| `API-REF-003` | `listFiscalIdentityTypes` | GET `/api/v1/reference-data/fiscal-identity-types` | ACCEPTED | MISSING_HTTP | none | 1 | Governed identity-type metadata projection required |
| `API-REF-004` | `listCurrencies` | GET `/api/v1/reference-data/currencies` | ACCEPTED | MISSING_HTTP | none | 1 | Currency metadata projection required |
| `API-REF-005` | `listFiscalDocumentTypes` | GET `/api/v1/reference-data/fiscal-document-types` | ACCEPTED | MISSING_HTTP | none | 1 | Read-only enabled/versioned fiscal metadata projection required |
| `API-REF-006` | `listInvoiceIndicators` | GET `/api/v1/reference-data/invoice-indicators` | ACCEPTED | MISSING_HTTP | none | 1 | Fiscal indicator metadata projection required |
| `API-REF-007` | `listContactTypes` | GET `/api/v1/reference-data/contact-types` | ACCEPTED | MISSING_HTTP | none | 1 | Party contact metadata projection required |
| `API-REF-008` | `listUnitsOfMeasure` | GET `/api/v1/reference-data/units-of-measure` | ACCEPTED | MISSING_HTTP | none | 1 | Commercial/fiscal UOM projection required |
| `API-CAT-009` | `listTaxProfiles` | GET `/api/v1/tax-profiles` | ACCEPTED | IMPLEMENTED | `TaxProfilesController.List` -> `ListTaxProfilesUseCase` | 1 | Preserve |

## 7. Wave 1 implementation ordering

Wave 1 should not be implemented as one oversized PR. The recommended bounded sequence is:

- `W1.1` Reference Data read-only foundation: `API-REF-001..008`.
- `W1.2` Current actor + permission catalog: `API-IAM-001`, `API-IAM-011`.
- `W1.3` Roles read/write: `API-IAM-006..009`.
- `W1.4` Users + role assignment: `API-IAM-002..005`, `API-IAM-010`.
- `W1.5` Terminals: `API-ORG-007..010`.
- `W1.6` Wave reconciliation: exact contract/implementation/test coverage and documentation closeout.

This ordering favors low-mutation reference reads first, then the security model, then organization terminals. Existing company/location/tax-profile endpoints are preserved and regression-tested rather than rewritten.

## 8. QA rule for completion waves

Every implementation increment must include, as applicable:

- Unit tests for new Application behavior;
- architecture tests protecting dependency direction and public-contract placement;
- CrossCutting/API tests for route, permission, Problem Details and idempotency obligations;
- provider-real PostgreSQL/MySQL integration tests for persistence/concurrency behavior where persistence changes;
- exact-head Clean Architecture Guard success before merge approval;
- no test weakening to obtain green CI.

## 9. Remaining master-matrix work before Wave 1 coding

Before `W1.1` implementation begins, the remaining 164 operations must be expanded row-by-row into this same matrix and assigned to Waves 2-7 (or a documented cross-cutting classification). This prevents endpoint-by-endpoint drift and preserves the 194-operation denominator throughout the completion program.
