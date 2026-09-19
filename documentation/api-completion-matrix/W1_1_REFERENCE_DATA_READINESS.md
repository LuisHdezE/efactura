# W1.1 Reference Data Readiness Audit

Status: `AUDITED / PREREQUISITES_CLASSIFIED`

Baseline: `main@020972f93a8f7e5857d6df286a6cf05d1e35819a`

Parent wave: `documentation/api-completion-matrix/WAVE_1.md`

Scope: `API-REF-001..API-REF-008` only.

This audit does not implement HTTP endpoints. All eight operations remain `MISSING_HTTP` until an exact runtime increment provides executable WebApi evidence and passes the normal governed merge gate.

## Governing rules

1. `ALIGN, DO NOT REWRITE`: brownfield data is compatibility evidence, not automatically the canonical v1 source.
2. A v1 reference endpoint must not depend directly on legacy `ApplicationCore` services or Npgsql/Dapper repositories.
3. Reference metadata must have an explicit, complete and semantically compatible source before it is exposed as authoritative v1 data.
4. Fiscal/reference metadata must preserve source/version/effective semantics where the accepted target design requires them.
5. `AUTHENTICATED` means a valid bearer token is still required even when no specialized business permission is required.
6. No endpoint may manufacture regulated values, silently broaden an enabled fiscal set or reinterpret free-form commercial data as a governed fiscal code table.

## Contracted operations

| API ID | operationId | Route | Permission | Readiness | Evidence / blocker | Required implementation boundary |
|---|---|---|---|---|---|---|
| `API-REF-001` | `listCountries` | GET `/api/v1/reference-data/countries` | `AUTHENTICATED` | `PREREQUISITE_REQUIRED` | Legacy `Countries` exists but the retained seed contains only US, CA, MX, DE, BR and UY. That is not a complete canonical country catalog. | Establish an approved complete country-code/name source and expose it through a provider-neutral Application reference-data port. |
| `API-REF-002` | `listUruguayDepartments` | GET `/api/v1/reference-data/uruguay-departments` | `AUTHENTICATED` | `READY_FOR_FOUNDATION` | Brownfield evidence contains the 19 Uruguay department names, but the old repository is Npgsql/Dapper-specific and keys departments through legacy `CountryId = 6`. | Freeze the governed 19-department projection behind the new provider-neutral reference-data boundary; do not query the legacy repository from v1. |
| `API-REF-003` | `listFiscalIdentityTypes` | GET `/api/v1/reference-data/fiscal-identity-types` | `AUTHENTICATED` | `READY_FOR_FOUNDATION` | Accepted fiscal evidence already distinguishes receiver identity type codes 1..7 and their issuing-country rules. Legacy `DocumentType` contains only `Id` + `Name`, so it is not the canonical source. | Project the accepted versioned fiscal identity metadata from a dedicated provider-neutral reference-data source. |
| `API-REF-004` | `listCurrencies` | GET `/api/v1/reference-data/currencies` | `AUTHENTICATED` | `PREREQUISITE_REQUIRED` | Architecture validates ISO-4217-style currency codes and DGI schema references ISO 4217, but no governed application catalog defines which currencies are supported or their display metadata. | Accept an explicit supported-currency catalog/source before exposing the endpoint. Do not derive support from currencies that merely appear in transactions. |
| `API-REF-005` | `listFiscalDocumentTypes` | GET `/api/v1/reference-data/fiscal-document-types` | `fiscal.read` | `PREREQUISITE_REQUIRED` | A versioned DGI-backed CFE/CFC catalog exists in target evidence, but Release-1 issuer/product enablement remains explicitly open. Legacy `VoucherType` is only `Id` + `Name` and is not authoritative issue metadata. | Close enabled-family policy/configuration and project only enabled/versioned metadata with provenance; the endpoint must not imply issue authority. |
| `API-REF-006` | `listInvoiceIndicators` | GET `/api/v1/reference-data/invoice-indicators` | `fiscal.read` | `PREREQUISITE_REQUIRED` | DGI schema documents fiscal indicator semantics while the accepted Release-1 XML builder currently maps only Exempt=1, Minimum=2, Basic=3 and Export=10. Legacy `InvoiceIndicator` is only `Id` + `Name`. | Decide and document whether the endpoint exposes the complete DGI metadata set or the application-supported subset, with version/source evidence. |
| `API-REF-007` | `listContactTypes` | GET `/api/v1/reference-data/contact-types` | `parties.read` | `PREREQUISITE_REQUIRED` | Party work explicitly preserved configurable `TypeCode` and deferred the reference catalog until legacy `ContactType` semantics are reconciled. Brownfield seed currently contains Phone, Mobile, Email and Fax. | Complete contact-type semantic reconciliation, then expose the accepted codes through the v1 reference boundary without copying legacy CRUD. |
| `API-REF-008` | `listUnitsOfMeasure` | GET `/api/v1/reference-data/units-of-measure` | `catalog.read` | `PREREQUISITE_REQUIRED` | `CommercialItem.Unit` is free commercial master data up to 40 characters; fiscal `UniMed` is a separate DGI-facing value constrained to max 4 characters. Existing design explicitly says the commercial field is not a DGI code table. No canonical UOM catalog exists. | Decide whether this operation lists configured commercial units, canonical platform units, fiscal-compatible units, or a governed projection combining them. Do not manufacture a DGI enumeration where the schema provides only a length constraint. |

## Brownfield compatibility evidence

The retained compatibility matrix intentionally maps legacy CRUD families into read-only/reference semantics rather than cloning those CRUD APIs into v1:

- `Country` -> `listCountries`;
- `Department` -> `listUruguayDepartments`;
- `DocumentType` -> `listFiscalIdentityTypes` after semantic reconciliation;
- `InvoiceIndicator` -> controlled/versioned fiscal metadata;
- `ContactType` -> reference/config metadata;
- `VoucherType` -> versioned fiscal/reference catalog.

That evidence is useful for migration but does not override the provider-neutral v1 architecture.

The brownfield country repository opens `NpgsqlConnection` directly and reads the legacy `Countries` table. That path must not become a dependency of the modern v1 Application layer. The retained SQL seed contains only six countries, so using it directly would turn an incomplete migration seed into an externally authoritative API contract.

The same SQL evidence does contain all 19 Uruguay departments, which is why `API-REF-002` is classified `READY_FOR_FOUNDATION` rather than source-blocked. Its implementation still needs a canonical v1 projection independent of the legacy numeric country key.

## Fiscal source evidence

### Fiscal identity types

Accepted DGI-format evidence already records receiver identity codes and country rules:

- `1` NIE -> UY;
- `2` RUC Uruguay -> UY;
- `3` C.I. Uruguay -> UY;
- `4` Otros -> ISO country / accepted `99` path;
- `5` Pasaporte -> all countries / ISO or `99` path;
- `6` DNI -> Argentina, Brazil, Chile or Paraguay;
- `7` NIFE -> foreign fiscal identity / ISO or `99` path.

This is materially richer than legacy `DocumentType(Id, Name)` and is the correct foundation direction for `API-REF-003`.

### Fiscal document types

The accepted target catalog is version-aware and includes CFE/CFC families and applicability evidence. It explicitly states that listing a family is not permission to enable it for every installation and that Release-1 special-family enablement remains open. `API-REF-005` therefore cannot be declared runtime-ready until the enabled subset is governed.

### Invoice indicators

The retained DGI schema documents `IndicadorFactType`; the current Release-1 unsigned builder intentionally supports only the fiscal buckets already backed by frozen tax evidence: codes `1`, `2`, `3` and `10`. `API-REF-006` must not silently present unsupported indicator values as application-ready choices.

## Currency and unit-of-measure boundary

`CurrencyCode` is an ISO-4217-style domain primitive, but syntactic validity is not equivalent to application support. No accepted repository/catalog currently answers the endpoint question "which currencies are supported?". `API-REF-004` remains prerequisite-blocked until that source exists.

For units, the accepted design draws an explicit line:

`CommercialItem.Unit` -> commercial master fact, up to 40 characters.

`FiscalContentLineSnapshot.UnitOfMeasure` / DGI `UniMed` -> frozen fiscal evidence, maximum four characters for the current format.

The application must not reinterpret all commercial unit strings as a canonical DGI list, and DGI's length constraint is not itself an enumeration. `API-REF-008` remains prerequisite-blocked until its catalog semantics are approved.

## Authorization boundary

`API-REF-001..004` use contract permission `AUTHENTICATED`. The API currently configures JWT authentication, but the modern permission attribute is what adds authorization enforcement on specialized-permission endpoints. The implementation slice must therefore explicitly enforce authenticated access for these four operations rather than assuming Swagger bearer metadata or middleware registration protects an undecorated controller action.

`API-REF-005..008` retain their contracted specialized permissions:

- `fiscal.read` for REF-005/006;
- `parties.read` for REF-007;
- `catalog.read` for REF-008.

## W1.1 implementation sequencing

The audit closes the readiness question but does not force all eight endpoints into one runtime PR.

### W1.1A - Reference Data Foundation

Establish the clean Application/reference-data boundary, authenticated-only enforcement pattern and public response DTO conventions. The first executable candidates are:

- `API-REF-002 listUruguayDepartments`;
- `API-REF-003 listFiscalIdentityTypes`.

They have enough source evidence to design a governed projection without depending on legacy persistence.

### W1.1B - Country and Currency Catalog Sources

Close complete country metadata and supported-currency source policy before implementing REF-001/004.

### W1.1C - Fiscal Reference Scope

Close issuer/product enablement for fiscal document types and supported-vs-complete indicator semantics before implementing REF-005/006.

### W1.1D - Contact Type and UOM Semantics

Complete the previously deferred contact-type reconciliation and define canonical unit-of-measure catalog semantics before implementing REF-007/008.

## QA gate for runtime slices

Every W1.1 runtime increment must, as applicable, prove:

- exact route and operationId contract;
- JWT/authenticated enforcement for `AUTHENTICATED` endpoints;
- exact specialized permission enforcement for REF-005..008;
- RFC 9457 behavior for authorization/application failures;
- no direct v1 dependency on legacy `ApplicationCore` service/repository or Npgsql/Dapper types;
- deterministic metadata/source/version behavior;
- ArchitectureTests and CrossCutting tests;
- provider-real PostgreSQL/MySQL tests whenever persistence is introduced;
- exact-head Clean Architecture Guard success;
- Master Matrix status updated only for operations actually executable through WebApi.

## Readiness conclusion

`W1.1 READINESS AUDIT: CLOSED`

- `READY_FOR_FOUNDATION`: REF-002, REF-003.
- `PREREQUISITE_REQUIRED`: REF-001, REF-004, REF-005, REF-006, REF-007, REF-008.
- `IMPLEMENTED`: none of REF-001..008 at this checkpoint.

The global implementation baseline therefore remains **41 / 194** and Wave 1 remains **7 / 30** until runtime code is merged and verified.
