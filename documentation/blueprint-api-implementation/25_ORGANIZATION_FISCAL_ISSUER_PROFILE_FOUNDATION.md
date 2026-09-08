# Organization Fiscal Issuer Profile Foundation

Status: CANDIDATE IMPLEMENTATION SLICE

Date: 2026-09-08

Blueprint evaluator: `0.5.1@ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`

Predecessor accepted baseline: `main@f8c4b006a71e8d20b424b484e9e01badf3d03dab`

## Why this slice exists

The previously selected next fiscal boundary was `FiscalDocument IDENTITY_CREATED -> CFE BUILT -> CFE VALIDATED`.

Fresh review of the official DGI CFE 25.2 format showed that a valid CFE cannot be constructed from the currently persisted `FiscalDocument` identity alone. The issuer area requires authoritative issuer/location data including issuer RUC, issuer denomination, branch/main-house code, fiscal address, city and department. Those values were already required by the accepted target architecture and `UC-ORG-001`, but had not yet been implemented as durable product state.

Building XML before these masters exist would force the application to invent data or bind fiscal artifacts to ad-hoc deployment configuration. This slice closes that prerequisite instead.

## Official regulatory evidence

Current official source set checked on 2026-09-08:

- DGI e-Factura, Documentos de interés: `https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es`
- Formato CFE v25.2: `https://www.efactura.dgi.gub.uy/files/formato_cfe_v25-2-pdf?es=`
- DGI lists `XSDs_FE_V1.44.2` as published for Testing and Production.

The DGI portal reports Formato CFE 25.2 as the current published format and records its Production enablement from 2026-06-30.

For the issuer area, the format defines mandatory issuer identity/denomination and fiscal establishment/address data. This implementation intentionally validates only the structural forms needed to preserve those values:

- issuer RUC: exactly 12 decimal digits;
- DGI branch/main-house code: exactly four decimal digits, stored as text to preserve leading zeroes.

This slice does **not** claim to implement the RUC check-digit algorithm. That validation must be introduced only with separately accepted authoritative evidence.

## Implemented domain

### `CompanyFiscalProfile`

One current issuer master per organization: `OrganizationId`, `Ruc`, `LegalName`, optional `CommercialName`, and optimistic `Version`.

The aggregate is mutable master data. Future `FiscalDocument` content must snapshot it; issued historical CFE must never reread the current mutable profile as fiscal truth.

### `FiscalLocation`

Fiscal/operational branch master: server-owned `Id`, `OrganizationId`, operational `Name`, `DgiBranchCode`, `FiscalAddress`, `City`, `Department`, `Active`, and optimistic `Version`.

`LocationId` is deliberately the same identifier family already referenced by Sale, CAE allocation, actor scopes and fiscalization. No second branch identity is introduced.

The database prevents two locations in one organization from owning the same DGI branch code.

## Application behavior

Read use cases require `organization.read`. Mutation use cases require `organization.manage`, company scope, `Idempotency-Key` and request hash.

The accepted local write pattern is reused: reserve idempotency inside one local transaction, persist reservation without committing, load/validate authoritative state, stage business mutation plus audit/outbox, complete idempotency, final `SaveChanges`, then commit or roll back everything.

Company `PATCH` acts as initial configuration when no profile exists and as optimistic update afterward. Once the company exists, `expectedVersion` is mandatory.

A fiscal location cannot be created before its company fiscal profile exists.

## Public API

This slice implements only the already-canonical endpoints:

- `API-ORG-001` `GET /api/v1/company`;
- `API-ORG-002` `PATCH /api/v1/company`;
- `API-ORG-003` `GET /api/v1/locations`;
- `API-ORG-004` `POST /api/v1/locations`;
- `API-ORG-005` `GET /api/v1/locations/{locationId}`;
- `API-ORG-006` `PATCH /api/v1/locations/{locationId}`.

No new operation IDs or permissions are invented.

## Persistence

New tables: `v1_company_fiscal_profiles` and `v1_fiscal_locations`.

Portable guards: company profile uses `OrganizationId` as authoritative key; `UX_v1_location_org_branch` enforces one DGI branch code per organization; both master records use optimistic `Version`; PostgreSQL and MySQL branch-code unique violations map to one application conflict contract.

## Deliberate non-scope

This slice does not implement Party addresses/contacts, receiver fiscal snapshot, Sale line unit-of-measure snapshot, per-line confirmed fiscal/tax snapshot persistence, CFE XML builder, XSD validation, XML signature, certificates/private-key custody, signed artifact persistence, CAE changes, DGI/provider transport or public `API-FIS-*`.

## Required next prerequisite after acceptance

Even with issuer masters available, BUILD + VALIDATE must still not reread mutable Party/Catalog/Tax masters after confirmation.

The next bounded prerequisite should capture the immutable CFE content snapshot: receiver identity/name/address evidence required by the selected CFE family; sale line code/name/unit/quantity/price; confirmed per-line tax bucket/rate/amount and rule provenance; and issuer + fiscal-location snapshot from the masters introduced here.

Only after that durable snapshot exists can the DGI 25.2 XML builder be accepted without reconstructing fiscal truth from mutable state.

## Validation target

Candidate CI must prove Domain is framework/provider/transport free; canonical API-ORG routes and permissions only; repository owns no transaction or `SaveChanges`; portable uniqueness guard exists for DGI branch code; Company/Location round-trip on PostgreSQL and MySQL; stale version becomes portable `concurrency_conflict`; duplicate DGI branch code becomes portable `duplicate_branch_code`; and no XML/signature/certificate/transport dependency enters this slice.
