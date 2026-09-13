# 59 — Fiscal Sobre ACK Observation

Status: ACCEPTED IMPLEMENTATION

Accepted baseline: `main@63c63f44b91d6fea6fb073af8c3e3d7841aa4c63`
(merge of PR #86, `feat(fiscal): add ACKSobre observation`).

Approved PR #86 head: `1401fb392d2baa348b6ae20946915636875e9342`.
Post-merge Clean Architecture Guard #401 (`34755766824`) completed successfully for Build/Architecture/CrossCutting/Legacy and PostgreSQL/MySQL provider-real transaction tests.

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Purpose

This bounded increment interprets an already durable `ResponseReceived` payload from the accepted Sobre transport boundary as typed, append-only `ACKSobre` evidence.

It does not cross the network again. It does not rewrite the transport submission. It does not mutate a CFE, sale, inventory, finance or fiscal-document business state.

The capability is intentionally an **observation boundary**, not an automatic remediation or acceptance workflow.

## Authoritative DGI evidence

The governing response source is current DGI **Formato de Mensajes de Respuesta v19**:

`https://www.efactura.dgi.gub.uy/files/formato_mensajes_respuesta_v19-pdf?es=`

The current DGI technical registry also continues to publish the reception-service material used by the accepted transport slice:

`https://www.efactura.dgi.gub.uy/files/DocumentoServiciosWebExternos?es=`

DGI's Production communiqué for duplicate Sobre evidence remains:

`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/-25044`

The reviewed material establishes the following immediate `ACKSobre` semantics:

- `AS` = **Sobre Recibido**;
- `BS` = **Sobre Rechazado**;
- `RUCReceptor` identifies who emits the response;
- `RUCEmisor` identifies the contributor whose Sobre is being answered;
- `IDRespuesta` is assigned by the responder;
- `IdEmisor` correlates the response to the issuer-assigned Sobre id;
- `IDReceptor` is assigned by the receiver when the Sobre is received;
- `CantidadCFE` carries the count associated with the Sobre;
- `FecHRecibido` and `Tmst` are response timestamp evidence;
- optional `ParamConsulta` carries `Token` and `FechaHora` for later consultation;
- rejection reasons can repeat up to 30 times and preserve `Motivo`, `Glosa` and optional `Detalle`.

For the DGI reception boundary, the reviewed applicability table supports rejection codes **S01..S08**. The general format also describes codes used by other receivers, including S20, but this DGI-specific observation slice does not silently admit receiver-only codes as DGI evidence.

The reviewed DGI descriptions are preserved as evidence categories only:

- S01: envelope/XML format issue;
- S02: RUC mismatch;
- S03: invalid certificate;
- S04: envelope-format validation failure;
- S05: CFE count mismatch;
- S06: certificate mismatch between Sobre and CFE;
- S07: maximum-size violation;
- S08: Sobre already exists in DGI records.

`S08` is persisted only as a rejection reason. This increment implements **no automatic recovery** from S08, does not infer DGI's exact duplicate key and does not authorize retransmission.

## ACK is not individual CFE acceptance

DGI's documented Sobre acknowledgement concerns receipt/rejection of the **Sobre**. It does not by itself establish individual CFE analysis, commercial acceptance, accounting acceptance or a final business state for the documents inside the envelope.

Therefore this capability does not mutate CFE status from `AS` or `BS`.

Any later CFE-response consultation and document-level interpretation requires separate authoritative evidence and a separate governed slice.

## Source prerequisite

Observation requires the accepted transport evidence to already be durable:

```text
Sobre durable
-> transport submission
-> ResponseReceived
-> exact ResponseXml + ResponseSha256
-> ACKSobre observation
```

`Prepared`, `InFlight` and `Unknown` are not valid sources for this capability.

Before parsing, Application revalidates:

- durable envelope integrity;
- transport-submission integrity;
- exact response SHA-256;
- envelope SHA-256 continuity between Sobre and submission.

The accepted transport row remains the immutable source evidence.

## Structural parser boundary

DGI XML parsing remains Infrastructure-only behind `IFiscalCfeEnvelopeAckParser`.

The parser:

- prohibits DTD processing;
- disables external XML resolution;
- requires root `ACKSobre` in `http://cfe.dgi.gub.uy`;
- requires exactly one direct `Caratula` and one direct `Detalle`;
- requires one direct XMLDSig `Signature` element structurally;
- accepts only `AS` or `BS`;
- validates mandatory numeric ranges and the 1..250 CFE count;
- validates optional `ParamConsulta` as an inseparable `Token + FechaHora` pair;
- validates rejection-reason multiplicity and DGI S01..S08 applicability;
- rejects duplicated mandatory or optional detail fields rather than guessing which value to keep.

Structural signature presence at this accepted boundary is **not cryptographic signature validation**. Pending PR #87 / document 60 proposes that cryptographic integrity proof as a separate append-only capability.

## Correlation policy

The parsed ACK must correlate exactly to the durable Sobre:

- parsed `RUCEmisor` = durable issuer RUC;
- parsed `RUCReceptor` = durable receiver RUT;
- parsed `IdEmisor` = durable `SenderEnvelopeId`;
- parsed `CantidadCFE` = durable `CfeCount`.

Any mismatch fails closed as invalid external evidence. No timestamp, collection-order or heuristic matching is permitted.

## State evidence

The local typed observation uses:

- `Received` for DGI `AS`;
- `Rejected` for DGI `BS`.

The state/reason relationship is strict:

- `AS` must preserve zero rejection reasons;
- `BS` must preserve 1..30 rejection reasons;
- each DGI reason must be S01..S08;
- `Glosa` is required and bounded to the current format's 100-character limit;
- optional `Detalle` is bounded to 500 characters.

This classification is evidence about the immediate Sobre ACK only. It does not mutate transport state or CFE business state.

## Timestamp preservation

`FecHRecibido`, `Tmst` and optional consultation `FechaHora` are preserved as exact non-empty DGI text values.

The reviewed response format does not provide sufficient timezone semantics to justify coercing these wire values to UTC or to `America/Montevideo`. This increment deliberately avoids that inference.

A separate local `ObservedAtUtc` records when this consumer made the durable observation.

## Durable append-only evidence

A single observation is persisted per transport `SubmissionId` in:

`v1_fiscal_cfe_envelope_ack_observations`

The row preserves:

- internal observation id;
- source submission id;
- durable envelope id;
- organization and fiscal identity;
- source response SHA-256;
- DGI `IDRespuesta`;
- DGI `IDReceptor`;
- CFE count;
- typed AS/BS state;
- exact reception/signing timestamp text;
- optional consultation token and availability text;
- typed rejection reasons serialized losslessly for local use;
- local observation timestamp.

Referential integrity uses `Restrict` FKs to both the durable transport submission and durable Sobre.

The repository is read/add-only. It owns no transaction, no `SaveChanges`, no HTTP and no update path.

## Replay and concurrency

`SubmissionId` is unique.

An exact replay returns the existing durable observation after revalidating its persisted evidence and source response hash.

Concurrent observers converge to the single durable row. Provider-specific unique-constraint classification remains isolated in Infrastructure through the already accepted conflict-classifier port.

The observation boundary never triggers a second DGI request.

## Accepted validation coverage

Accepted exact-head and post-merge CI prove that:

- AS parses and persists DGI ids plus consultation parameters;
- BS persists S08 as evidence without recovery behavior;
- receiver-only S20 is rejected at this DGI-specific boundary;
- AS carrying rejection reasons fails closed;
- missing structural XMLDSig fails closed without claiming cryptographic verification;
- DTD/external-entity input fails closed;
- mismatched `IdEmisor` or other correlation evidence creates no observation;
- exact replay returns one durable observation;
- concurrent PostgreSQL/MySQL observers converge to one observation;
- the original transport submission remains `ResponseReceived` and byte/hash evidence remains unchanged;
- the durable Sobre remains unchanged.

Exact-head Guard #400 (`34744587630`) passed before merge. Accepted `main@63c63f44b91d6fea6fb073af8c3e3d7841aa4c63` then passed post-merge Guard #401 (`34755766824`) for both jobs.

## Deliberate non-scope

This accepted increment does not itself implement:

- cryptographic verification of the DGI ACK XMLDSig inside the semantic observation row;
- document-level CFE acceptance/rejection;
- consultation of the second CFE response using `Token` without an authoritative token-input service contract;
- automatic recovery or retransmission for S08;
- automatic reconciliation of ambiguous Sobre `Unknown` delivery;
- automatic `Idemisor` allocation;
- business grouping/batching policy;
- DGI Testing acceptance;
- Production enablement.

Pending PR #87 / document 60 is a separate append-only XMLDSig cryptographic verification capability. It does not rewrite this accepted ACK observation.
