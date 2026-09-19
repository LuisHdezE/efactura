# API Completion Master Matrix

Status: `GOVERNED BASELINE CANDIDATE`

Checkpoint date: 2026-09-18

Repository baseline inspected: `main@ca0cd93c488453e44f044103ae8de19efb363c22`

This document is the operational completion layer for the public API v1 contract. It does not replace the canonical route, permission, idempotency or purpose definitions in `documentation/blueprint-api-contract/02A_ENDPOINT_INVENTORY_CORE.md`, `02B_ENDPOINT_INVENTORY_OPERATIONS.md` and `02C_ENDPOINT_INVENTORY_TECHNICAL_OPERATIONS.md`.

The matrix records what is contractually present, what is actually exposed by the governed `src/WebApi/Controllers/V1` surface, how the remaining work is partitioned into delivery waves, and which contract defects must be resolved without silently inventing semantics.

## 1. Canonical source hierarchy

For completion accounting, source priority is:

1. `02A_ENDPOINT_INVENTORY_CORE.md` for core/commercial/fiscal operation records.
2. `02B_ENDPOINT_INVENTORY_OPERATIONS.md` for operations/finance/sync/reporting operation records.
3. `02C_ENDPOINT_INVENTORY_TECHNICAL_OPERATIONS.md` for the Technical Operations Console amendment.
4. `src/WebApi/Controllers/V1/*Controller.cs` for governed public implementation evidence.
5. Application, persistence and test evidence for readiness of each implementation slice.

Legacy controllers under `src/WebApi/Controllers` outside `Controllers/V1` are not counted as implemented v1 operations merely because they expose HTTP actions.

## 2. 193 vs 194 reconciliation

The current repository contains:

- `02A`: `79` contract rows;
- `02B`: `93` contract rows;
- `02C`: `22` contract rows;
- raw contract rows: `194`;
- unique API IDs: `194`;
- unique `operationId` values: `194`;
- unique HTTP method + route slots: `193`.

The difference is not a counting typo. There is exactly one current route collision:

| API ID | operationId | Method / route | Permission | State |
|---|---|---|---|---|
| `API-MON-001` | `getIntegrationStatus` | GET `/api/v1/operations/integrations` | `operations.read` | `CONTRACT_COLLISION` |
| `API-180` | `listOperationalIntegrations` | GET `/api/v1/operations/integrations` | `operations.integrations.read` | `CONTRACT_COLLISION` |

Both records are currently present in accepted contract documentation, but ASP.NET cannot expose two independent GET operations with the same route as two distinct public OpenAPI operations without an explicit reconciliation. This baseline therefore preserves both stable IDs and records the collision instead of deleting, renumbering or silently merging either contract.

`API-FIS-010`, added to 02A after the original Technical Operations numbering was established, is a separate real governed operation and is not discarded to make the arithmetic equal 193. It has a public v1 controller, application wrapper, architecture tests and cross-cutting tests.

Until the collision is explicitly governed, two useful measurements coexist:

- raw contract-record denominator: `194`;
- unique HTTP-slot denominator: `193`.

No endpoint implementation decision in Waves 1-6 depends on choosing between `API-MON-001` and `API-180`; the collision belongs to Wave 7 and must be reconciled before that wave is closed.

## 3. Governed implementation baseline

The governed v1 implementation currently exposes `41` contract-mapped HTTP actions across `10` v1 controllers.

Raw-record completion: `41 / 194 = 21.13%`.

Unique-slot completion: `41 / 193 = 21.24%`.

Raw contract records not yet implemented: `153`.

Unique HTTP slots not yet implemented: `152`.

The 41 implemented operations are:

| Controller / area | Implemented API IDs | Count |
|---|---|---:|
| Company | `API-ORG-001`, `API-ORG-002` | 2 |
| Locations | `API-ORG-003` through `API-ORG-006` | 4 |
| Parties | `API-PTY-001` through `API-PTY-007` | 7 |
| Items | `API-CAT-001` through `API-CAT-005` | 5 |
| Item Categories | `API-CAT-006` through `API-CAT-008` | 3 |
| Tax Profiles | `API-CAT-009` | 1 |
| Sales | `API-SAL-001` through `API-SAL-007` | 7 |
| CAE | `API-CAE-001` through `API-CAE-007` | 7 |
| Inventory | `API-INV-001` through `API-INV-004` | 4 |
| Fiscal CFE envelope evidence | `API-FIS-010` | 1 |
| **Total** |  | **41** |

The contract-only `API-SYS-001 GET /api/v1/health` and `API-SYS-002 GET /api/v1/version` are not counted as implemented simply because the deployment has Swagger or other runtime/legacy informational endpoints. Their exact v1 contract routes are not present in the governed v1 controller surface at this checkpoint.

## 4. Delivery-wave partition

Every one of the 194 current contract records belongs to exactly one wave. The partition intentionally tracks contract records, including the known Wave-7 collision, so no accepted API ID disappears from governance.

| Wave | Scope | Contract records | Implemented | Pending raw records |
|---|---|---:|---:|---:|
| 1 | Identity + Organization + Reference Data | 33 | 7 | 26 |
| 2 | Parties + Catalog + Sales Completion | 26 | 22 | 4 |
| 3 | Payments + Cash + AR/AP | 21 | 0 | 21 |
| 4 | Inventory + Transfers + Procurement + Receiving | 23 | 4 | 19 |
| 5 | Fiscal Completion + CAE + CFE Lifecycle | 39 | 8 | 31 |
| 6 | Reporting + Audit + Sync | 24 | 0 | 24 |
| 7 | Technical Operations Console | 28 | 0 | 28 |
| **Total** |  | **194** | **41** | **153** |

Wave allocation by contract family:

- **Wave 1:** `IAM` (11), `ORG` (10), `PMT` (3), `CAT-009` (1), `REF` (8).
- **Wave 2:** `PTY` (8), `CAT-001..008` (8), `POS` (1), `SAL` (9).
- **Wave 3:** `AR` (4), `COL` (3), `AP` (4), `PAY` (3), `CSH` (7).
- **Wave 4:** `INV` (4), `TRF` (7), `RPL` (2), `PRC` (6), `GRC` (4).
- **Wave 5:** `FIS` (10), `FDL` (2), `CAE` (7), `CNT` (7), `RCV` (6), `XML` (1), `DFR` (4), `CFG` (2).
- **Wave 6:** `DEV` (3), `SYN` (4), `REP` (6), `CAL` (1), `AUD` (4), `ALT` (2), `DAS` (1), `AEX` (3).
- **Wave 7:** `SYS` (2), `MON` (1), `INT` (3), plus all 22 Technical Operations Console records from `API-172` through `API-193`.

This partition sums to 194 records exactly and the implemented counts sum to 41 exactly.

## 5. Matrix semantics

Each implementation slice is assessed with these fields:

| Field | Meaning |
|---|---|
| API ID | Stable contract identity from 02A/02B/02C |
| operationId | Canonical OpenAPI operation identity |
| Method / route | Canonical public HTTP slot |
| Contract state | `READY`, `CONTRACT_COLLISION`, or a later governed disposition |
| Implementation state | `IMPLEMENTED`, `PENDING`, or `PARTIAL` |
| WebApi evidence | Governed v1 controller/action or `NONE` |
| Application evidence | Bound use case when verified; absence of a v1 binding is not proof the brownfield contains no related logic |
| Persistence readiness | Provider-real persistence evidence when the operation requires it |
| Authorization | Contract permission and matching enforcement evidence |
| Tests | Architecture, cross-cutting, unit and provider-real evidence as applicable |
| Wave | Delivery wave 1-7 |
| Gap / blocker | Missing capability or known collision |
| Priority | Execution order within the wave |

A route is not considered implemented merely because a legacy controller has similar behavior. A public v1 action must conform to the accepted contract identity, permission, request/response behavior and Clean Architecture boundaries.

## 6. Wave 1 detailed execution matrix

Wave 1 contains 33 contract records. Seven are already implemented; 26 remain pending.

| API ID | operationId | Method / route | State | WebApi / Application evidence | Priority |
|---|---|---|---|---|---|
| `API-IAM-001` | `getCurrentActor` | GET `/api/v1/me` | PENDING | No governed v1 binding | W1-P0 |
| `API-IAM-002` | `listUsers` | GET `/api/v1/users` | PENDING | No governed v1 binding | W1-P1 |
| `API-IAM-003` | `getUser` | GET `/api/v1/users/{userId}` | PENDING | No governed v1 binding | W1-P1 |
| `API-IAM-004` | `createUser` | POST `/api/v1/users` | PENDING | No governed v1 binding | W1-P1 |
| `API-IAM-005` | `updateUser` | PATCH `/api/v1/users/{userId}` | PENDING | No governed v1 binding | W1-P1 |
| `API-IAM-006` | `listRoles` | GET `/api/v1/roles` | PENDING | No governed v1 binding | W1-P1 |
| `API-IAM-007` | `getRole` | GET `/api/v1/roles/{roleId}` | PENDING | No governed v1 binding | W1-P1 |
| `API-IAM-008` | `createRole` | POST `/api/v1/roles` | PENDING | No governed v1 binding | W1-P1 |
| `API-IAM-009` | `updateRole` | PUT `/api/v1/roles/{roleId}` | PENDING | No governed v1 binding | W1-P1 |
| `API-IAM-010` | `assignUserRoles` | PUT `/api/v1/users/{userId}/roles` | PENDING | No governed v1 binding | W1-P1 |
| `API-IAM-011` | `listPermissions` | GET `/api/v1/permissions` | PENDING | No governed v1 binding | W1-P1 |
| `API-ORG-001` | `getCurrentCompany` | GET `/api/v1/company` | IMPLEMENTED | `CompanyController` / `GetCurrentCompanyUseCase` | DONE |
| `API-ORG-002` | `updateCurrentCompany` | PATCH `/api/v1/company` | IMPLEMENTED | `CompanyController` / `UpsertCompanyFiscalProfileUseCase` | DONE |
| `API-ORG-003` | `listLocations` | GET `/api/v1/locations` | IMPLEMENTED | `LocationsController` / `ListFiscalLocationsUseCase` | DONE |
| `API-ORG-004` | `createLocation` | POST `/api/v1/locations` | IMPLEMENTED | `LocationsController` / `CreateFiscalLocationUseCase` | DONE |
| `API-ORG-005` | `getLocation` | GET `/api/v1/locations/{locationId}` | IMPLEMENTED | `LocationsController` / `GetFiscalLocationUseCase` | DONE |
| `API-ORG-006` | `updateLocation` | PATCH `/api/v1/locations/{locationId}` | IMPLEMENTED | `LocationsController` / `UpdateFiscalLocationUseCase` | DONE |
| `API-ORG-007` | `listTerminals` | GET `/api/v1/terminals` | PENDING | No governed v1 binding | W1-P2 |
| `API-ORG-008` | `registerTerminal` | POST `/api/v1/terminals` | PENDING | No governed v1 binding | W1-P2 |
| `API-ORG-009` | `getTerminal` | GET `/api/v1/terminals/{terminalId}` | PENDING | No governed v1 binding | W1-P2 |
| `API-ORG-010` | `updateTerminal` | PATCH `/api/v1/terminals/{terminalId}` | PENDING | No governed v1 binding | W1-P2 |
| `API-PMT-001` | `listPaymentMethods` | GET `/api/v1/payment-methods` | PENDING | No governed v1 binding | W1-P3 |
| `API-PMT-002` | `createPaymentMethod` | POST `/api/v1/payment-methods` | PENDING | No governed v1 binding | W1-P3 |
| `API-PMT-003` | `updatePaymentMethod` | PATCH `/api/v1/payment-methods/{paymentMethodId}` | PENDING | No governed v1 binding | W1-P3 |
| `API-CAT-009` | `listTaxProfiles` | GET `/api/v1/tax-profiles` | IMPLEMENTED | `TaxProfilesController` / `ListTaxProfilesUseCase` | DONE |
| `API-REF-001` | `listCountries` | GET `/api/v1/reference-data/countries` | PENDING | No governed v1 binding | W1-P4 |
| `API-REF-002` | `listUruguayDepartments` | GET `/api/v1/reference-data/uruguay-departments` | PENDING | No governed v1 binding | W1-P4 |
| `API-REF-003` | `listFiscalIdentityTypes` | GET `/api/v1/reference-data/fiscal-identity-types` | PENDING | No governed v1 binding | W1-P4 |
| `API-REF-004` | `listCurrencies` | GET `/api/v1/reference-data/currencies` | PENDING | No governed v1 binding | W1-P4 |
| `API-REF-005` | `listFiscalDocumentTypes` | GET `/api/v1/reference-data/fiscal-document-types` | PENDING | No governed v1 binding | W1-P4 |
| `API-REF-006` | `listInvoiceIndicators` | GET `/api/v1/reference-data/invoice-indicators` | PENDING | No governed v1 binding | W1-P4 |
| `API-REF-007` | `listContactTypes` | GET `/api/v1/reference-data/contact-types` | PENDING | No governed v1 binding | W1-P4 |
| `API-REF-008` | `listUnitsOfMeasure` | GET `/api/v1/reference-data/units-of-measure` | PENDING | No governed v1 binding | W1-P4 |

### Wave 1 execution order

`W1-P0` is the first planned product slice because `getCurrentActor` provides the authenticated actor/effective-permission boundary used by later identity and administrative clients.

After that:

1. `W1-P1` completes users, roles and permissions.
2. `W1-P2` completes terminal registration within the already implemented organization/location foundation.
3. `W1-P3` establishes governed payment-method reference/configuration APIs needed by commercial flows.
4. `W1-P4` exposes contract-defined reference data without treating legacy lookup controllers as automatic v1 compliance.

Each slice still requires its own application/persistence/security/test audit before coding. `PENDING` means the governed public v1 binding is absent at this baseline, not that all related brownfield logic is absent.

## 7. Completion rules

An operation moves to `IMPLEMENTED` only when all applicable evidence is present:

- exact governed v1 route/method;
- contract permission enforcement;
- required idempotency/concurrency behavior;
- inward Application dependency direction;
- persistence/provider-real evidence when durable state is involved;
- canonical Problem Details/error behavior;
- architecture/cross-cutting tests;
- provider-real PostgreSQL/MySQL tests for transactional persistence where applicable;
- public Swagger/OpenAPI visibility when the operation is externally exposed;
- no weakening of historical fiscal fail-closed boundaries.

A future PR that changes the contract-row count, unique IDs, unique operationIds, known route-collision set or the number of governed v1 HTTP actions must update this matrix deliberately. The architecture test added with this baseline enforces that requirement.

## 8. Next governed increment after this baseline

After this matrix baseline is accepted, implementation starts with the first bounded Wave-1 slice, currently `API-IAM-001 getCurrentActor`, unless the pre-slice audit proves that an earlier contract prerequisite must be reconciled first.

The Wave-7 `API-MON-001` / `API-180` collision remains explicit debt and must be resolved before Wave 7 can be declared complete. It does not authorize renumbering accepted API IDs or silently dropping one record.
