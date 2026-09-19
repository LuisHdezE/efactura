# W1.1B Country and Currency Catalogs

Status: `IMPLEMENTED / PENDING_EXACT_HEAD_CI_AND_MERGE`

Baseline: `main@8e11feaf5fe9d8284297325e1d5acc0db825bed0`

Parent wave: `documentation/api-completion-matrix/WAVE_1.md`

Scope: `API-REF-001` and `API-REF-004` only.

## Purpose

Close the source prerequisites identified by the W1.1 readiness audit and expose two authenticated, provider-neutral reference endpoints without depending on legacy reference repositories or database persistence.

The implemented operations are:

- `API-REF-001 listCountries` -> `GET /api/v1/reference-data/countries`;
- `API-REF-004 listCurrencies` -> `GET /api/v1/reference-data/currencies`.

Both operations retain the accepted `AUTHENTICATED` boundary. They require a valid bearer token but do not require a specialized business permission.

## Country catalog decision

The country endpoint uses a governed static snapshot aligned to the current ISO 3166-1 alpha-2 code set.

Release-1 projection:

- exactly 249 country/territory entries;
- `Alpha2Code` as the public stable code;
- display `Name`;
- deterministic ordering by alpha-2 code;
- source metadata `ISO 3166-1 current country codes`;
- snapshot version `snapshot-2026-09-19`.

ISO 3166-1 is the normative external source. The snapshot is an application-owned immutable projection for this release, not a claim that the application is itself the ISO Maintenance Agency.

Legacy numeric `Countries.Id` values are not exposed as v1 identity. The retained six-row legacy seed remains migration evidence only and is not used to build the public catalog.

## Currency catalog decision

ISO 4217 defines currency identifiers, but ISO recognition is deliberately separated from eFactura product support.

Release-1 exposes only the explicitly governed supported subset:

| Code | Display name | Accepted Release-1 evidence |
|---|---|---|
| `USD` | US Dollar | Existing sales/fiscal design explicitly discusses USD transaction amounts and authoritative conversion requirements. |
| `UYI` | Uruguay Peso en Unidades Indexadas (UI) | Existing sales boundary explicitly recognizes UYI as the ISO 4217 code for transactions already expressed in Unidad Indexada. |
| `UYU` | Peso Uruguayo | Native Uruguay fiscal/reporting currency and explicit existing sales/fiscal evidence. |

The endpoint is fail-closed. A currency appearing in the ISO 4217 current list, a historical transaction, or a future external integration does not make it supported by Release-1 automatically.

The provider metadata is:

- source name `ISO 4217 / eFactura Release-1 supported currency policy`;
- source version `release-1/amendment-180`.

SIX Financial Information is the official ISO 4217 Maintenance Agency. Amendment 180 is the relevant current-list checkpoint effective in 2026, including the Bulgarian transition from BGN to EUR. BGN is intentionally not exposed by the eFactura Release-1 supported subset, and neither is EUR unless a separate governed product-support decision adds it.

This source/version metadata therefore records both the external identifier baseline and the narrower application policy. It must not be interpreted as a complete mirror of ISO 4217 List One.

## Architecture boundary

W1.1B extends the provider-neutral `IReferenceDataCatalog` introduced by W1.1A:

`ReferenceDataController -> ListCountriesUseCase/ListCurrenciesUseCase -> IReferenceDataCatalog -> Release1ReferenceDataCatalog`

The new catalogs have no direct dependency on:

- `ApplicationCore` legacy services;
- Npgsql;
- Dapper;
- EF Core `DbContext`;
- provider-specific persistence.

No schema migration or persistence integration test is required by the implementation itself because both catalogs are immutable release metadata.

## Authentication and failure behavior

The controller remains class-level `[Authorize]` and the Application use cases repeat the authenticated-actor invariant through `IActorContextAccessor`.

Expected runtime behavior:

- no/invalid bearer token -> `401` through the existing RFC 9457 authentication/problem-details pipeline;
- valid bearer token -> deterministic `200` reference collection;
- no specialized permission is required for REF-001 or REF-004.

## QA contract

Architecture guards verify:

- both exact routes and operationIds;
- authenticated-only controller boundary;
- complete 249-entry unique alpha-2 country snapshot;
- presence of key interoperability codes including `UY`, `AR`, `BR`, `CU`, `US`;
- exact Release-1 currency set `USD`, `UYI`, `UYU`;
- explicit non-inclusion of unsupported `EUR` and historical `BGN` from the Release-1 support subset;
- provider-neutral Application/Infrastructure dependency direction;
- version/source metadata;
- preservation of W1.1A routes and guards;
- completion-matrix arithmetic moves only from 43/194 to 45/194.

Merge remains blocked until the exact PR head passes the normal Clean Architecture Guard. Deployment/runtime acceptance remains a post-merge gate.

## Runtime acceptance after merge

The demo acceptance must prove at minimum:

1. Swagger/OpenAPI remain HTTP 200;
2. `/countries` without JWT -> 401 + Problem Details;
3. `/currencies` without JWT -> 401 + Problem Details;
4. `/countries` with JWT -> 200, source/version metadata, exactly 249 unique alpha-2 entries, `UY=Uruguay`;
5. `/currencies` with JWT -> 200, exact ordered codes `USD`, `UYI`, `UYU` and expected source/version metadata;
6. W1.1A endpoints remain green;
7. `/api/v1/parties` remains 401 without JWT and 200 with JWT + Neon.

## Completion effect

If exact-head CI, merge, deployment and runtime acceptance pass:

- Wave 1: `11 / 30` implemented;
- global public-v1: `45 / 194` implemented;
- REF-001 and REF-004 move to `IMPLEMENTED / EXISTING_PATH / regression`;
- REF-005..008 remain prerequisite-blocked for W1.1C/W1.1D.
