# 68 — Fiscal CFE document-response known coverage

Status: governed implementation candidate

## Purpose

This increment adds a read-only assessment boundary over the ACKCFE evidence already persisted by the accepted consultation, XMLDSig-verification and PKI-Uruguay-trust capabilities.

Its question is deliberately narrow:

> Which CFE identities in one durable Sobre are covered by the currently known, verified and PKI-trusted ACKCFE messages, and is that known evidence internally non-contradictory?

It does **not** answer whether DGI has finished producing ACKCFE messages.

## Authoritative DGI evidence

The normative source reviewed for this increment is DGI `Formato_Mensaje_de_Respuesta_v19`, printed `25/11/2025`, currently published by DGI at:

`https://www.efactura.dgi.gub.uy/files/formato_mensajes_respuesta_v19-pdf?es=`

The source establishes the constraints relevant to this boundary:

1. The result of CFE review may be generated in **multiple messages** or in one message, depending on the receiver's internal processes.
2. ACKSobre exposes `Token` as the character string used to invoke the response consultation.
3. ACKSobre exposes `Fecha y hora Consulta` as the point from which the second message, corresponding to the comprobantes response, can be consulted.
4. ACKCFE Carátula counters are message-scoped. In particular, the format describes the number of comprobantes answered **“en este mensaje”**, as well as accepted/rejected counts **“en este mensaje”**.
5. `ACKCFE_det/Estado` keeps the already governed `AE`, `BE` and `CE` semantics from the preceding increment.

No authoritative field was identified in this source that proves any of the following:

- last ACKCFE message;
- protocol-final response;
- token exhaustion;
- maximum reconsultation count;
- retry cadence;
- business timeout.

Therefore none of those concepts is inferred by this implementation.

## Existing reconsultation capability

The accepted consultation boundary already permits an explicit caller to execute another `EFACCONSULTARESTADOENVIO` operation using the same exact durable `IdReceptor + Token`, provided a new idempotency `OperationId` is supplied.

Each result remains a separate append-only `StoredFiscalCfeEnvelopeDocumentResponseConsultation` observation.

This increment does not add a scheduler and authorizes **no automatic polling**. It only adds the read model needed to understand the evidence accumulated through explicit consultations.

## Implemented boundary

`AssessFiscalCfeEnvelopeDocumentResponseCoverageUseCase` starts from one exact durable Sobre identity and requires:

1. the durable `StoredFiscalCfeEnvelope`;
2. its durable `ResponseReceived` submission;
3. its accepted `AS / Received` ACKSobre carrying the durable `IdReceptor + Token` source;
4. the immutable fiscal identities of every CFE in that Sobre;
5. every durable ACKCFE consultation linked to the exact `AckObservationId`;
6. successful durable XMLDSig verification for every consultation included in the assessment;
7. successful durable PKI Uruguay trust evidence for every consultation included in the assessment.

Source continuity is checked across:

- organization;
- envelope id;
- submission id;
- ACKSobre observation id;
- ACKSobre response SHA-256;
- DGI receiver id;
- consultation token SHA-256;
- issuer RUC;
- receiver RUT;
- issuer envelope id;
- total CFE count;
- ACKCFE response SHA-256;
- signature-verification source;
- trust-validation source and certificate SHA-256.

The boundary is read-only. It has no `IUnitOfWork`, no `ITransactionManager`, no repository writes and no local fiscal-document transition.

## Multi-message aggregation

The repository adapter now exposes a read boundary by the already indexed `AckObservationId`. No schema change or migration is required.

For every durable consultation:

- persisted evidence integrity is revalidated;
- its exact source identity must match the same ACKSobre;
- XMLDSig verification must exist and match the exact response hash;
- PKI Uruguay trust must exist and match the exact signature/consultation chain;
- each detail must map to a CFE contained in the durable Sobre;
- `AE / BE / CE` interpretation is repeated deterministically;
- the message-scoped accepted/rejected/observed counters must match the detail semantics.

`ACKCFE_det/@ordinal` remains message-scoped evidence. It is checked for uniqueness inside each message but is not treated as a stable cross-message identifier.

## Duplicate observations

An explicit reconsultation may return response bytes already observed before.

Exact duplicate response XML is identified by the already durable response SHA-256. Such consultations remain preserved as append-only observations, but the coverage calculation counts the exact response bytes only once as a distinct response message.

A duplicate hash carrying contradictory persisted message metadata fails closed.

## Contradiction detection

The aggregate fails closed when the known evidence demonstrates contradictions, including:

- the same DGI `IDRespuesta` associated with different response bytes;
- a consultation that no longer matches its exact ACKSobre/token source;
- signature or trust evidence that belongs to another response;
- unsupported ACKCFE detail state codes;
- message counter/detail mismatches;
- duplicate/foreign CFE identities inside a message;
- the same `TipoCFE + Serie + NroCFE` receiving incompatible trusted `AE / BE / CE` states across distinct known messages.

The implementation never resolves a contradiction by timestamp, collection order or “latest row” selection.

## Coverage result

The local read model can report:

- `NoDocumentCoverage`: no CFE identity in the Sobre is covered by the currently known trusted ACKCFE evidence;
- `PartialDocumentCoverage`: at least one but not all CFE identities are covered;
- `FullDocumentCoverage`: every CFE identity in the durable Sobre is covered by at least one non-contradictory trusted ACKCFE detail.

`FullDocumentCoverage` is intentionally named as a statement about the **known document set only**. It **does not prove protocol finality**.

Even when all known CFE identities are covered, the result remains:

- `ProtocolFinalityProven = false`;
- `TokenExhaustionProven = false`;
- `AutomaticReconsultationAuthorized = false`;
- `DgiIdentityValidated = false`.

PKI Uruguay chain trust is not DGI legal signer identity/habilitation.

## Persistence

No migration is introduced.

The implementation reuses existing indexed columns:

- `v1_fiscal_cfe_document_response_consultations.AckObservationId`;
- `v1_fiscal_cfe_document_response_certificate_trust_validations.ConsultationId`.

The concrete repositories implement narrow read-history ports in addition to their existing append/replay interfaces.

## Explicitly out of scope

This increment does not add or infer:

- automatic token polling;
- polling interval;
- maximum retry count;
- timeout policy;
- token exhaustion;
- DGI protocol finality;
- `EstadoCFE` semantics;
- local FiscalDocument reconciliation;
- DGI-specific signer identity/habilitation;
- S08 recovery;
- Unknown transport recovery;
- `IdEmisor` allocation.

## DGI Testing readiness

Formal DGI Testing remains:

`BLOCKED BY MISSING PRODUCT CAPABILITIES`

This boundary is internal evidence handling only. It does not establish DGI certification, homologation, interoperability approval or production readiness.
