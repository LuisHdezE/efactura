# 27 - Party Address / Contact Contract Completion

Status: **CANDIDATE / NOT ACCEPTED UNTIL MERGE**

Date: 2026-09-08

Predecessor baseline: `main@e28f538525571d9261895f14407a19352ce5961e`
(merge of PR #47, post-PR46 pre-XML sequencing reconciliation).

Blueprint evaluator remains:
`0.5.1@ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`.

## Purpose

Complete the Party address/contact data that the accepted API v1 contract already promises, without
inventing new endpoint families and without advancing the fiscal-document lifecycle.

This slice is a master-data prerequisite for a later immutable CFE content snapshot. It does **not**
implement a fiscal snapshot, XML generation, XSD validation, signing, certificate/private-key
custody, signed-artifact archival or DGI/provider transport.

## Accepted contract basis

`documentation/blueprint-api-contract/07_REQUEST_RESPONSE_CONTRACTS.md` already defines:

- `PartyCreateRequest` as carrying typed fiscal identities, addresses and contacts;
- `PartyDto` as returning addresses/contacts;
- historical Sale/CFE data as snapshot-based and not rewritten when Party master data changes.

`documentation/blueprint-api-contract/02A_ENDPOINT_INVENTORY_CORE.md` already defines the relevant
surface:

- `API-PTY-001` GET `/api/v1/parties`;
- `API-PTY-002` POST `/api/v1/parties`;
- `API-PTY-003` GET `/api/v1/parties/{partyId}`;
- `API-PTY-004` PATCH `/api/v1/parties/{partyId}`.

No dedicated `/addresses` or `/contacts` route is introduced by this slice.

## Brownfield evidence reused

The legacy model already carries useful, non-authoritative compatibility evidence:

- `Customer.Address` max length 255;
- `Customer.City` max length 80;
- `Customer.ZipCode` max length 5;
- `Customer.DepartmentId`;
- `ContactDetail.ContactTypeId` + `ContactValue` max length 100;
- `ContactType.ContactTypeName` max length 80.

The accepted compatibility matrix already classifies legacy `ContactDetail` as Party contact data to
be reconciled through Party use cases rather than copied as a second v1 CRUD family.

This slice therefore preserves useful semantics while keeping legacy routes/models separate until a
future explicit compatibility-adapter boundary.

## Current official DGI relevance

The current official DGI technical publication was rechecked before this candidate. DGI currently
publishes `Formato CFE v25.2` and `XSDs_FE_V1.44.2` for Testing and Production.

Official registry:
`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`

Official Formato CFE 25.2:
`https://www.efactura.dgi.gub.uy/files/formato_cfe_v25-2-pdf?es=`

The format confirms that receiver domicile/address content is relevant for applicable taxpayer and
export CFE scenarios, including direction, city and applicable department/province/state/country
content; postal code is separately represented where applicable.

This Party master model deliberately does **not** copy every DGI XML length/occurrence rule into the
mutable master-data aggregate. The later immutable CFE snapshot/build boundary must select the
applicable address and perform format-specific projection/validation against the accepted official
format/XSD. This prevents mutable Party data from becoming an implicit XML schema model.

## Domain model

### `PartyAddress`

- server-owned `Id`;
- `Kind`: `FISCAL | DELIVERY | OTHER`;
- `AddressLine`;
- `City`;
- optional `Region`;
- `CountryCode` as ISO alpha-2 master-data fact;
- optional `PostalCode`;
- `Primary` flag.

Rules:

- required line/city/country;
- country normalized to uppercase ISO alpha-2 structure;
- duplicate normalized addresses rejected;
- at most one primary address per address kind.

### `PartyContact`

- server-owned `Id`;
- configurable `TypeCode`;
- `Value`;
- `Primary` flag.

`TypeCode` is intentionally not collapsed into a hard-coded EMAIL/PHONE enum in this slice. The
legacy system had configurable `ContactType` metadata, and the accepted compatibility matrix says
that reference/config semantics must be reconciled before copying that catalog into v1.

Rules:

- required type/value;
- duplicate normalized type/value pairs rejected;
- at most one primary contact per type.

## API behavior

### Create

`POST /api/v1/parties` accepts optional `addresses[]` and `contacts[]` alongside existing Party
master, roles and fiscal identities.

Server owns child IDs. Existing idempotency, authorization, audit, outbox and local-transaction
semantics remain unchanged.

### Update

`PATCH /api/v1/parties/{partyId}` accepts optional address/contact collections.

Semantics are explicit:

- property omitted / `null`: preserve the current collection;
- empty array: replace the current collection with empty;
- supplied non-empty array: replace the collection with the submitted normalized master data.

A combined scalar + address + contact PATCH increments the Party aggregate version exactly once.
The operation continues to use the existing `party.update` idempotency namespace and mutation
workflow.

### Read

List/get/create/update responses expose addresses/contacts through `PartyDto`. Repository queries load
the complete Party aggregate in one query shape rather than performing controller-side or N+1
master-data lookups.

## Persistence

New v1 child tables:

- `v1_party_addresses`;
- `v1_party_contacts`.

Both are dependent master-data rows keyed by server-owned UUID and cascade only with deletion of the
Party aggregate. Replacement during PATCH explicitly removes obsolete child rows and adds the new
server-owned rows inside the existing Party transaction.

Migration candidate:
`20260908190000_V1PartyAddressContact`.

The repository still owns neither transaction boundaries nor `SaveChanges`.

## Validation plan

Candidate validation covers:

- Domain normalization and collection invariants;
- one-version-increment combined update;
- architecture guard preventing child route proliferation and fiscal transport leakage;
- persistence round-trip and collection replacement on PostgreSQL 16 and MySQL 8.4;
- full repository Clean Architecture Guard after the exact candidate head exists.

No PASS claim is made by this document until exact-head CI completes successfully and the PR is
accepted through the normal human gate.

## Explicit non-scope

Still excluded:

- legacy ContactDetail/Customer migration adapter;
- reference endpoint/catalog for ContactType;
- immutable fiscal receiver snapshot;
- CFE-specific selection of fiscal vs delivery address;
- CFE XML names/namespaces/occurrence rules;
- XSD validation;
- XML signature;
- certificate/private-key handling;
- DGI/provider transport or response lifecycle.

The next fiscal product slice after this prerequisite is accepted will be the **Immutable Fiscal CFE
Content Snapshot Foundation**, not direct transport or signing.
