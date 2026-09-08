# Fiscal Document Identity Foundation

## Purpose

Advance the accepted sale-confirmation workflow from durable `FiscalizationRequest = PENDING`
to one immutable, server-owned fiscal-document identity without crossing into XML generation,
digital signature or DGI/provider transport.

This is an internal Application workflow slice. It does not expose a new public HTTP endpoint.

## Accepted predecessor

The accepted `confirmSale` transaction already freezes server-owned evidence into one durable
`FiscalizationRequest`:

- source Sale id and organization/location/terminal context;
- selected Release-1 CFE family;
- receiver-identification requirement;
- CFE format version;
- confirmation and settlement fingerprints;
- authoritative currency, net, VAT and total amounts.

The identity workflow consumes that work item. It does not accept fiscal type, series, number,
CAE or amounts from a client and it does not reconstruct those values from a public DTO.

## Workflow

`PrepareFiscalDocumentIdentityUseCase` performs one short local transaction:

1. load the durable fiscalization work item;
2. replay the existing document when the work item is already `IDENTITY_CREATED`;
3. require a still-consistent immutable `Sale = CONFIRMED` snapshot;
4. reserve one fiscal number through the already accepted `IFiscalNumberAllocator`;
5. snapshot the CAE and fiscal identity into `FiscalDocument`;
6. transition the work item `PENDING -> IDENTITY_CREATED`;
7. append audit and outbox evidence;
8. flush once and commit.

External calls never run inside this transaction.

## Retry identity

The durable `FiscalizationRequestId` is the workflow retry identity.

The CAE allocator receives the deterministic internal operation id:

`fiscalization:{FiscalizationRequestId:N}`

No second HTTP-style `Idempotency-Key` is invented for this internal worker step.

The database keeps independent uniqueness guards for:

- `organization + fiscalizationRequestId`;
- `organization + cfeType + series + number`;
- `fiscalNumberReservationId`;
- the existing `organization + fiscal operation id` reservation guard.

A failure after `SaveChanges` but before transaction commit rolls back the number reservation,
FiscalDocument, work-item transition, audit and outbox together. A successful retry reads the
same FiscalDocument and does not consume another number.

## FiscalDocument identity snapshot

The new domain aggregate records only the immutable identity foundation needed by later artifact
generation:

- FiscalDocument id;
- source FiscalizationRequest and Sale ids;
- fiscal-number reservation id;
- CAE authorization/allocation ids;
- CFE type;
- series and number;
- CAE authorization number;
- CAE range from/to;
- CAE valid-from/valid-to;
- fiscal date;
- organization/location/terminal context;
- receiver-identification requirement;
- format version;
- confirmation and settlement fingerprints;
- currency, net, VAT and total amounts;
- identity-created timestamp and status.

It deliberately does not claim to be the complete signed CFE snapshot yet. Issuer/receiver
artifact fields required by the selected CFE format are a later generation slice and must be
frozen before signing.

## Regulatory evidence checked for this slice

Official DGI evidence was rechecked on 2026-09-08:

1. DGI e-Factura portal / current news:
   `https://www.efactura.dgi.gub.uy/principal/FacturaElectronica/emision-de-comprobantes-fiscales-electronicos`
   - published CFE Format 25.2 on 2026-05-04;
   - announced Format 25.2 enabled in Production from 2026-06-30.

2. Official CFE Format 25.2:
   `https://www.efactura.dgi.gub.uy/files/formato_cfe_v25-2-pdf?es=`
   - CFE identity includes type and comprobante number (series + number);
   - representation includes CAE authorization/range/expiry information.

3. Official DGI CFE Frequently Asked Questions:
   `https://www.efactura.dgi.gub.uy/files/descargar-todas-las-preguntas-frecuentes?es=`
   - electronic numbering is unique by CFE type for the company;
   - expired unused CAE numbering cannot be silently reused.

This slice preserves the already accepted bounded Release-1 CFE families (`101`, `111`, `121`);
it does not expand the product scope merely because DGI supports additional document families.

## Architecture boundary

`FiscalDocument` remains Domain-owned and has no dependency on:

- EF Core or Dapper;
- ASP.NET;
- PostgreSQL/MySQL provider APIs;
- XML libraries;
- signing/certificate implementations;
- artifact storage;
- HTTP/provider SDKs;
- DGI transport.

`PrepareFiscalDocumentIdentityUseCase` composes repositories, transaction/unit-of-work,
`IFiscalNumberAllocator`, audit and outbox through Application ports.

Repositories stage persistence only. They do not commit their own transaction.

## Explicit non-scope

This slice does not:

- generate CFE XML;
- validate CFE XML/XSD;
- access or store certificate/private-key material;
- sign a CFE;
- create a signed immutable artifact/hash;
- enqueue or execute DGI/provider transport;
- persist synchronous DGI receipt;
- process asynchronous acceptance/rejection;
- expose `API-FIS-*` routes;
- expose `getSaleFiscalizationStatus`;
- implement `cancelSale`, credit/debit notes or regularization;
- implement contingency/CFC workflow.

## Validation target

The candidate must prove on PostgreSQL 16 and MySQL 8.4 that:

- the first execution commits exactly one number reservation and one FiscalDocument;
- replay returns the same identity with no duplicate number;
- failure after the final flush rolls back every staged effect;
- missing active CAE leaves the work item pending with no partial identity;
- domain guards reject invalid CAE range/validity evidence;
- architecture guards keep XML/signing/transport outside this slice.
