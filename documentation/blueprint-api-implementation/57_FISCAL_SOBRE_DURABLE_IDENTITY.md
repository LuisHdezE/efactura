# 57 — Fiscal Sobre Durable Identity

Status: GOVERNED IMPLEMENTATION CANDIDATE

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Purpose

This bounded increment makes the accepted local Sobre v05 package durable and replay-safe without inventing an `Idemisor` allocation algorithm and without crossing the DGI transport boundary.

The caller supplies an explicit `Idemisor`. The application packages the selected already-signed CFE through the accepted document-56 boundary, persists the immutable envelope evidence once, and replays the durable result when the same operation or the same local envelope identity is requested again with the same immutable input.

It does not submit a Sobre, parse a DGI response, retry a network call, allocate the next `Idemisor`, or mutate CFE business state.

## Authoritative DGI evidence

The governing functional source remains DGI **Formato de Sobre v05**:

`https://www.efactura.dgi.gub.uy/files/Formato_Sobre_v05_pdf?es=`

For the Carátula field `ID Emisor` / `Idemisor`, the format defines a `NUM 10` value described as a **number assigned by the issuer to the envío**.

The current DGI **Formato de Mensajes de Respuesta v19** mirrors that meaning in the response Carátula: `ID Emisor` identifies the number assigned by the issuer to the envío being answered.

Current response material is published through DGI's e-Factura documents registry:

`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`

DGI also publishes a Production communiqué stating that a duplicate Sobre is answered with rejection code `S08`:

`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/-25044`

The reviewed authoritative material establishes issuer assignment and response correlation. There is **no authoritative allocation algorithm** in that reviewed material for choosing the next `Idemisor`. This increment therefore does not infer a counter, sequence reset, range reservation, timestamp encoding, database identity rule, or other automatic allocator.

`S08` is preserved only as evidence that duplicate Sobre handling exists at DGI. This slice does not interpret which exact field combination DGI uses to decide that a Sobre is duplicate and does not claim that the local identity defined below is DGI's duplicate-detection key.

## Explicit Idemisor boundary

`PersistFiscalCfeEnvelopeCommand` receives `SenderEnvelopeId` explicitly and validates only the governed wire range `0..9999999999`.

This increment **does not allocate** `Idemisor`; there is no `Idemisor` allocator in this boundary.

If a later authoritative DGI source establishes a required allocation algorithm, it must be introduced as a separate governed capability rather than retrofitted into this persistence boundary by assumption.

## Local durable identity

For replay and correlation inside this consumer, one immutable persisted Sobre is locally identified by:

`OrganizationId + IssuerRuc + ReceiverRut + SenderEnvelopeId`

This is a **local replay/correlation invariant**, not a claim about DGI-wide, cross-environment, cross-receiver, or regulatory uniqueness semantics.

The persistence layer also enforces a separate unique operation identity:

`OrganizationId + OperationId`

The two guards serve different purposes:

- an exact `OperationId` replay returns the already durable result when the immutable command still matches;
- a different operation that reaches the same local Sobre identity also returns the already durable result when the immutable packaging input still matches;
- reusing either identity for different immutable packaging input fails closed.

A replay by local Sobre identity may therefore return the original durable `OperationId`. The second operation id is not persisted as a new envelope because that would create duplicate durable Sobre evidence.

## Immutable persisted evidence

The durable record stores:

- internal envelope id;
- organization id;
- receiver RUT;
- issuer RUC;
- explicit `Idemisor`;
- Sobre creation instant in UTC plus the original offset minutes;
- ordered fiscal-document ids;
- original operation id;
- `CantCFE`;
- common certificate thumbprint and serial;
- complete generated `EnvioCFE` XML;
- SHA-256 of the exact XML bytes represented by the persisted UTF-8 string;
- Sobre schema-set id/version/fingerprint.

The ordered CFE identity list is persisted because order contributes to the generated envelope bytes.

## Portable creation timestamp evidence

Document 56 emits `Fecha` with second precision and the explicit UTC offset.

This increment therefore normalizes the command timestamp to a whole second before packaging and persists both:

- the UTC instant; and
- `CreatedAtOffsetMinutes`.

The repository reconstructs the original offset on replay. This avoids provider-specific `DateTimeOffset` normalization in PostgreSQL/MySQL from silently changing the wire-significant `Fecha` representation.

## Replay integrity

Before returning any stored replay, Application revalidates durable self-consistency, including:

- non-empty internal id and fiscal identity;
- 12-digit issuer/receiver identifiers;
- explicit `Idemisor` wire range;
- whole-second creation timestamp;
- 1..250 unique, non-empty ordered CFE ids;
- persisted `CantCFE` matching the identity list;
- non-empty certificate and schema identity evidence;
- SHA-256 shape for envelope/schema hashes;
- recomputed SHA-256 of persisted `EnvelopeXml` matching `EnvelopeSha256`.

Invalid persisted evidence fails closed as `invalid_persisted_evidence` rather than being replayed.

Replay matching compares both the UTC instant and the original offset, so two timestamps representing the same instant with different offsets are not treated as the same immutable packaging command.

## Persistence and transaction boundary

`PersistFiscalCfeEnvelopeUseCase` composes:

- accepted `PackageFiscalCfeEnvelopeUseCase`;
- `IFiscalCfeEnvelopeRepository`;
- `ITransactionManager`;
- `IUnitOfWork`.

The real EF repository is read/add-only and owns no transaction or `SaveChanges` call. The Application use case owns the atomic transaction and unit-of-work boundary.

The provider model enforces unique indexes for both operation replay and local envelope identity. PostgreSQL and MySQL provider-real tests must prove the same durable behavior.

## Architecture boundary

This candidate has no dependency on:

- HTTP/SOAP transport gateways;
- DGI credentials or transport certificates;
- a private key;
- ACK parsers;
- retry policy;
- an `Idemisor` allocator;
- CFE business-state mutation.

Packaging remains the accepted document-56 responsibility. This increment only makes the resulting validated envelope durable.

## Validation coverage

This increment requires proof that:

- PostgreSQL and MySQL persist exactly one durable envelope for an exact command;
- exact operation replay returns the same durable envelope without rebuilding it;
- a new operation id targeting the same local identity and immutable input replays the existing envelope;
- the original operation id remains the durable one on identity replay;
- a local envelope identity cannot be reused for different ordered CFE input;
- creation offset survives provider round-trip;
- sub-second command precision is normalized to the second precision used by the wire builder;
- persisted envelope XML/hash/count/certificate/schema evidence remains self-consistent before replay;
- repository code remains read/add-only;
- no transport or `Idemisor` allocator enters the slice.

## Deliberate non-scope

This increment does not implement:

- automatic `Idemisor` allocation or reservation;
- claims about DGI's exact duplicate-detection key for `S08`;
- grouping/batching policy deciding which signed CFE enter a Sobre;
- gzip/base64 transport framing;
- HTTP/SOAP submission to DGI;
- transport certificate policy;
- DGI Sobre ACK parsing;
- DGI receiver-id persistence;
- submitted-envelope transport state;
- automatic retry after an ambiguous transport outcome;
- interpretation or recovery from `S08`;
- ER inconsistency-detail retrieval;
- automatic Reporte Diario `R05` recovery;
- external DGI Testing acceptance;
- Production enablement.

Those remain separate governed capabilities. A durable local Sobre is not evidence that DGI received or accepted it.
