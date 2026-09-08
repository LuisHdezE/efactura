# 26 - Pre-XML Fiscal Content Prerequisites

Status: **PLANNING / NOT IMPLEMENTED**

Date: 2026-09-08

Accepted predecessor baseline: `main@4e69e8f1c3808b89c2d5e4663553a625140ea75d`
(PR #46, Organization Fiscal Issuer Profile Foundation).

Blueprint evaluator for this consumer remains:
`0.5.1@ac8be4e3332b13cab7d27f12e6a62d5d60e9ff4e`.

This document is a bounded planning record. It is not implementation evidence and does not claim
that Party addresses, complete fiscal snapshots, XML generation or XSD validation already exist.

## Why this planning record exists

The originally selected next boundary was:

`FiscalDocument IDENTITY_CREATED -> CFE BUILT -> CFE VALIDATED`

A post-PR46 review against the accepted architecture and current official DGI technical publication
showed that this would skip required immutable inputs.

`documentation/blueprint-architecture/09_FISCAL_INTEGRATION_ARCHITECTURE.md` defines the fiscal XML
builder as consuming immutable snapshots. The accepted `FiscalDocument` currently preserves fiscal
identity, CAE provenance, format version, fingerprints and monetary totals, but it does not yet own
the complete issuer/receiver/line content needed to construct the CFE without rereading mutable
master data.

Therefore XML BUILD is deliberately postponed until the prerequisites below are implemented and
accepted.

## Current official DGI evidence

DGI's current `Documentos de interés` registry was rechecked on 2026-09-08 and publishes for both
Testing and Production:

- `Formato CFE v25.2`;
- `XSDs_FE_V1.44.2`.

Official registry:
`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`

Official Formato CFE 25.2:
`https://www.efactura.dgi.gub.uy/files/formato_cfe_v25-2-pdf?es=`

The current format evidence establishes relevant content obligations for this planning decision:

- receiver identification/content depends on the CFE family and scenario;
- taxpayer e-Factura scenarios require the applicable receiver fiscal identity and receiver
  denomination/domicile content;
- export CFE receiver content includes receiver denomination and address;
- CFE detail contains the goods/services line information and corresponding amounts required by the
  active format;
- totals/taxes must be represented consistently with the authoritative fiscal calculation.

This document does not transcribe the full format or XSD. Exact XML names, namespaces, occurrence
rules, lengths, code tables and schema constraints remain the responsibility of the later BUILD and
VALIDATE slices using the official artifacts.

## Accepted architecture constraints

The later builder must not obtain fiscal truth by dereferencing mutable records at generation time.
In particular:

- Company and FiscalLocation are mutable issuer master data;
- Party is mutable receiver master data;
- Catalog item descriptions and tax profiles are mutable master data;
- the confirmed Sale and accepted fiscal calculation are authoritative source evidence;
- an already-created fiscal document must not silently change because any master record changes
  later.

The correct boundary is therefore to freeze the required fiscal content before XML generation.

## Prerequisite A - Party Address / Contact Contract Completion

### Why it is already canonical

`documentation/blueprint-api-contract/07_REQUEST_RESPONSE_CONTRACTS.md` already states that:

- `PartyCreateRequest` carries typed fiscal identities, **addresses and contacts**;
- `PartyDto` returns normalized identities plus **addresses/contacts**;
- historical sales/CFE retain snapshots and are not rewritten when Party master data changes.

The current v1 Party implementation stores name, residence country, tax-residence country, roles and
typed fiscal identities, but does not yet persist addresses/contacts. This is therefore completion of
an accepted public contract, not invention of a new endpoint family.

### Intended implementation boundary

Use the existing Party create/update surface. Do not add an ad-hoc fiscal-address endpoint unless a
separate contract review proves one is necessary.

The bounded implementation must:

- preserve Party address and contact facts separately from residence country, tax-residence country
  and fiscal identities;
- support the address information needed later for receiver snapshots without assuming that every
  Party or every CFE family requires the same fields;
- keep server-owned IDs/versioning where child identity is needed;
- preserve current organization scoping, permissions, idempotency, audit/outbox and optimistic
  concurrency behavior;
- remain portable across PostgreSQL and MySQL;
- avoid provider/DGI transport concepts, certificates, XML or signing.

### Field-shape rule

The exact Party address/contact DTO and persistence shape is **not decided by this document**.
Before coding it must be reconciled against:

1. the accepted API contract;
2. reusable authoritative legacy address/contact semantics and data structures;
3. current DGI fiscal content needs;
4. general customer/supplier needs that prevent a fiscal-only data model from polluting Parties.

No address code table, mandatory-country policy or provider-specific representation may be invented.

## Prerequisite B - Immutable Fiscal CFE Content Snapshot Foundation

After Party address/contact completion is accepted, the fiscalization workflow must create an
immutable content snapshot associated with the already-created `FiscalDocument` identity.

The snapshot must freeze, as applicable to the selected CFE family:

### Issuer snapshot

- issuer RUC;
- legal/commercial denomination required by the format;
- DGI branch/location code;
- fiscal address/locality/department content required by the active format;
- source master IDs/versions for provenance without making later reads authoritative.

### Receiver snapshot

- Party ID when a Party exists;
- receiver name/denomination;
- selected typed fiscal identity and issuing country when required;
- receiver address/domicile content required by the chosen CFE family;
- evidence of the receiver-identification decision already produced by the fiscal decision path.

### Line snapshot

For each confirmed Sale line, preserve the content required by the active format without rereading
Catalog later, including the appropriate subset of:

- stable line identity/order;
- item/service code and description;
- quantity;
- unit price/commercial amount inputs already accepted at confirmation;
- authoritative calculated line amount;
- VAT liability/rate classification and applied rate where represented/required;
- accepted rule/specification provenance.

The existing authoritative confirmation path already owns `CfeArithmeticResult` and
`CfeArithmeticLineResult` evidence. The snapshot slice should consume that accepted calculation
rather than recalculate tax from mutable profiles or public preview DTOs.

### Header/totals snapshot

The existing FiscalDocument monetary snapshot already carries currency, net, VAT and total. The
content-snapshot slice must reconcile those values with the persisted line/tax buckets and fail
closed if they do not match the accepted confirmation evidence.

## Intended lifecycle sequence

Accepted current boundary:

`FiscalDocument IDENTITY_CREATED`

Planned sequence after this document:

1. Party Address / Contact Contract Completion;
2. Fiscal CFE Content Snapshot created and frozen;
3. `CFE BUILT` from that immutable snapshot;
4. `CFE VALIDATED` against well-formedness, current official XSD and accepted fiscal invariants;
5. `CFE SIGNED` only in a later cryptographic trust-boundary slice;
6. signed-artifact archival and transport only in later slices.

The exact domain status names for the content-snapshot step must be chosen during implementation and
must not falsely imply that XML has already been built or validated.

## BUILD boundary after prerequisites

When the prerequisites are accepted, `IFiscalXmlBuilder` must:

- consume the immutable fiscal content snapshot;
- be selected/versioned for the active accepted CFE format;
- produce deterministic unsigned CFE XML content for the supported Release-1 family;
- not read Company, Location, Party or Catalog masters as fiscal authority;
- not sign, access certificates/private keys, archive signed artifacts or transport anything.

## VALIDATE boundary after BUILD

`IFiscalValidator` must then validate, at minimum within the accepted scope:

- XML well-formedness;
- the current official DGI XSD set accepted for the format version;
- CFE-family structural requirements;
- fiscal arithmetic consistency with the frozen snapshot;
- CAE/fiscal-number invariants already accepted by earlier slices.

Schema validation must use source-controlled/versioned official artifacts or a reproducible accepted
artifact acquisition process. Runtime dependency on a mutable remote DGI URL is not sufficient as
the only validation source.

## Explicit non-scope of the immediate next slice

The immediate Party contract-completion slice must not implement:

- FiscalDocument lifecycle changes;
- fiscal content snapshot persistence;
- XML generation;
- XSD validation;
- XML signature;
- certificate/private-key storage or access;
- signed artifact storage;
- DGI/provider submission;
- DGI response interpretation;
- fiscal correction/cancellation lifecycle.

## Gate expectations for Party contract completion

Before merge, the product slice should demonstrate through automated tests at least:

- create/read/update round-trip of accepted address/contact data;
- existing Party semantics remain intact;
- stale optimistic version produces portable conflict behavior;
- PostgreSQL and MySQL persistence behave equivalently;
- successful writes retain atomic audit/outbox/idempotency evidence;
- failure after flush rolls back Party address/contact changes and companion evidence;
- no XML/signing/transport dependencies enter Domain or Application boundaries;
- existing `API-PTY-001..007` surface remains bounded unless a separately approved contract change
  is justified.

## Decision

**DO NOT start CFE XML BUILD directly from the current PR #46 baseline.**

The immediate selected product slice is **Party Address / Contact Contract Completion**.
After that slice is accepted, implement the **Immutable Fiscal CFE Content Snapshot Foundation**.
Only then proceed to **CFE XML BUILD + VALIDATE**.
