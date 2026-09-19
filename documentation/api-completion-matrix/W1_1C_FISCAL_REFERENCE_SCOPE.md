# W1.1C Fiscal Reference Scope

Status: `IMPLEMENTED / PENDING_EXACT_HEAD_CI_AND_MERGE`

Branch baseline: `main@498469ce315ef77097bdada7db98383170164256`.

Scope:

- `API-REF-005 listFiscalDocumentTypes`
- `API-REF-006 listInvoiceIndicators`

This increment closes the two fiscal-reference prerequisites identified by the historical W1.1 readiness audit without broadening eFactura Release-1 capabilities beyond executable evidence.

## Governing decision

The public reference API distinguishes three different concepts that must not be collapsed:

1. metadata recognized by the current DGI specification;
2. fiscal families or indicator values represented in internal domain preparation;
3. capabilities that the current eFactura Release-1 execution path can safely build and expose.

W1.1C publishes the third category only. It is deliberately fail-closed.

## Authoritative source baseline

The regulatory baseline remains DGI `Formato_CFE` version `25-2`, the accepted Release-1 format already used by the fiscal implementation. The complete DGI specification contains more CFE/CFC families and more invoice-indicator values than this increment exposes.

That broader official catalog is regulatory metadata, not automatic product enablement. A value is not advertised as Release-1 supported merely because DGI defines it.

## API-REF-005 — fiscal document types

Contract:

- operationId: `listFiscalDocumentTypes`
- route: GET `/api/v1/reference-data/fiscal-document-types`
- permission: `fiscal.read`

### Release-1 enabled reference subset

| Code | Name | Family | Correction kind | CFC | Applicability validation |
|---:|---|---|---|---:|---|
| 101 | e-Ticket | ETICKET | ORIGINAL | 201 | required |
| 102 | Nota de Crédito de e-Ticket | ETICKET | CREDIT_NOTE | 202 | required |
| 103 | Nota de Débito de e-Ticket | ETICKET | DEBIT_NOTE | 203 | required |
| 111 | e-Factura | EFACTURA | ORIGINAL | 211 | required |
| 112 | Nota de Crédito de e-Factura | EFACTURA | CREDIT_NOTE | 212 | required |
| 113 | Nota de Débito de e-Factura | EFACTURA | DEBIT_NOTE | 213 | required |

Source metadata:

- `sourceName`: `DGI Formato_CFE / eFactura Release-1 enabled document policy`
- `sourceVersion`: `25-2/release-1-domestic`

### Why the subset stops at these six

The current deterministic unsigned CFE builder has executable mappings for the domestic `101/102/103/111/112/113` families. `CfeFamily.EFacturaExportacion` (`121`) exists in eligibility/domain preparation, but the Release-1 builder explicitly rejects it with `fiscal.cfe_builder.export_not_supported` rather than inventing export-specific evidence.

Therefore `121` and the other DGI-defined families are not exposed by REF-005 in this release.

Every returned row carries `requiresApplicabilityValidation=true`. Listing a document family means only that Release-1 has executable document-family support at the reference level. It does not authorize issuance, bypass eligibility rules, prove issuer/profile enablement, allocate CAE, or override correction/reference constraints.

## API-REF-006 — invoice indicators

Contract:

- operationId: `listInvoiceIndicators`
- route: GET `/api/v1/reference-data/invoice-indicators`
- permission: `fiscal.read`

### Release-1 supported subset

| Code | Name | Tax treatment |
|---:|---|---|
| 1 | Exento de IVA | EXEMPT |
| 2 | Gravado a Tasa Mínima | MINIMUM |
| 3 | Gravado a Tasa Básica | BASIC |
| 10 | Exportación y asimiladas | EXPORT |

Source metadata:

- `sourceName`: `DGI Formato_CFE / eFactura Release-1 supported indicator policy`
- `sourceVersion`: `25-2/release-1`

The subset is tied directly to the deterministic unsigned CFE builder mapping:

- `VatRateKind.Exempt -> 1`
- `VatRateKind.Minimum -> 2`
- `VatRateKind.Basic -> 3`
- `VatRateKind.Export -> 10`

The builder fails closed for any tax kind it cannot map safely. W1.1C therefore does not expose other DGI-defined indicator values as application-supported choices.

The presence of indicator `10` does not imply that e-Factura Exportación `121` is enabled. Indicator capability and document-family issue capability are separate boundaries.

## Architecture

W1.1C extends the existing provider-neutral Reference Data foundation:

`ReferenceDataController -> List*UseCase -> IReferenceDataCatalog -> Release1ReferenceDataCatalog`

The static fiscal catalog is application-owned Release-1 policy evidence and introduces:

- no migration;
- no database read/write path;
- no direct Npgsql or Dapper dependency;
- no direct legacy `ApplicationCore` dependency;
- no WebApp change.

## Authorization

Both W1.1C routes require the accepted specialized permission exactly:

`Permissions.FiscalRead == "fiscal.read"`

The controller still preserves its authenticated bearer-token boundary. Expected behavior is:

- missing/invalid bearer token -> HTTP 401;
- valid bearer token without `fiscal.read` -> HTTP 403;
- valid bearer token with `fiscal.read` -> HTTP 200.

Authorization/application errors remain subject to the API's RFC 9457 Problem Details behavior.

## QA gate

Before merge, the exact PR head must prove:

- exact routes and operationIds;
- exact `fiscal.read` permission on REF-005/006;
- W1.1A/B authenticated-only routes remain free of specialized permissions;
- fiscal document catalog exactly `101/102/103/111/112/113` with CFC mappings `201/202/203/211/212/213`;
- `121` remains excluded while the builder rejects export construction;
- invoice indicators exactly `1/2/3/10` and remain aligned with builder mappings;
- deterministic source/version metadata;
- no legacy/database dependency in the Reference Data boundary;
- ArchitectureTests, CrossCutting/API tests, legacy tests and the normal provider-real PostgreSQL/MySQL Guard jobs pass;
- completion matrix reconciles to `47 / 194` implemented operations, `145` MISSING_HTTP, `2` contract-collision IDs and `147` non-implemented IDs.

## Runtime acceptance after deployment

Post-merge acceptance must validate against the stable Cloud Run URL:

1. Swagger HTTP 200 and OpenAPI HTTP 200.
2. OpenAPI operationIds `listFiscalDocumentTypes` and `listInvoiceIndicators` on the exact routes.
3. Both routes without JWT -> HTTP 401 + `application/problem+json`.
4. Both routes with authenticated JWT lacking `fiscal.read` -> HTTP 403 + Problem Details.
5. Both routes with JWT carrying `fiscal.read` -> HTTP 200.
6. Fiscal document response contains exactly the six governed rows and expected source/version metadata.
7. Invoice indicator response contains exactly codes `1`, `2`, `3`, `10` and expected source/version metadata.
8. W1.1A/B reference endpoints remain regression-green.
9. `/api/v1/parties` remains 401 without JWT and 200 with the governed JWT + Neon connection.

Only after exact-head CI, explicit merge approval, post-merge Guard, deployment and this runtime acceptance may W1.1C be declared CLOSED.
