# 69 — Fiscal CFE document-response explicit evidence cycle

Status: **GOVERNED IMPLEMENTATION CANDIDATE**

Date: 2026-09-15

## Purpose

Compose the already accepted ACKCFE boundaries into one explicit, caller-triggered evidence cycle for one exact durable Sobre:

```text
accepted ACKSobre IdReceptor + Token
        ↓
explicit EFACCONSULTARESTADOENVIO consultation
        ↓
ACKCFE XMLDSig verification
        ↓
PKI Uruguay certificate-trust validation
        ↓
read-only known-document coverage assessment
```

The cycle exists so a caller does not create a durable ACKCFE observation and accidentally stop before the cryptographic and trust evidence required by the coverage boundary.

It is an orchestration boundary. It does not invent any new DGI response semantics.

## Authoritative DGI basis

The normative source retained for this increment is DGI `Formato_Mensaje_de_Respuesta_v19`, printed `25/11/2025`, together with the already accepted `ws_efactura / EFACCONSULTARESTADOENVIO` technical contract.

The governed facts used here remain:

1. an accepted ACKSobre may carry `IdReceptor + Token` for response consultation;
2. ACKSobre carries `Fecha y hora Consulta`, described by DGI as the point from which the second message / comprobantes response can be consulted;
3. the CFE review result may be produced in one message or in multiple messages;
4. ACKCFE counters describe the content of the individual message, not protocol finality.

No authoritative evidence currently establishes:

- a last-message marker;
- token exhaustion;
- a maximum reconsultation count;
- a polling cadence;
- a business timeout;
- permission to infer local fiscal finality from `FullDocumentCoverage`.

Those concepts remain absent from this implementation.

## Implemented boundary

`CollectFiscalCfeEnvelopeDocumentResponseEvidenceUseCase` accepts:

- organization id;
- issuer RUC;
- receiver RUT;
- sender envelope id;
- one explicit `OperationId`.

For that single caller request it executes, in order:

1. `ConsultFiscalCfeEnvelopeDocumentResponseUseCase`;
2. `VerifyFiscalCfeEnvelopeDocumentResponseSignatureUseCase`;
3. `ValidateFiscalCfeEnvelopeDocumentResponseCertificateTrustUseCase`;
4. `AssessFiscalCfeEnvelopeDocumentResponseCoverageUseCase`.

The exact source lineage is checked again across the four results. A cycle fails closed if consultation, signature, trust or coverage no longer point to the same ACKSobre / submission / Sobre chain, or if any child result overclaims DGI identity, token exhaustion or protocol finality.

## OperationId and resumability

The same caller-supplied `OperationId` is deliberately reused as:

- the ACKCFE consultation operation id;
- the consultation operation reference used by signature verification;
- the consultation operation reference and trust-validation operation id.

This gives the cycle checkpoint semantics rather than pretending that a remote DGI call and multiple durable evidence writes can be one atomic database transaction.

Examples:

- first execution with operation `X`: performs the explicit consultation and then validates the resulting evidence;
- replay with operation `X`: reuses the durable consultation, signature verification and trust validation whenever they already exist;
- if consultation and signature were persisted but PKI validation failed, retrying operation `X` resumes from those durable checkpoints instead of deliberately requesting another ACKCFE message;
- a new operation `Y` represents another explicit caller-requested consultation of the same accepted ACKSobre token.

The cycle itself contains no loop and schedules nothing.

### Concurrent duplicate callers

This increment does not add a distributed lock around the existing remote consultation boundary. The existing persistence uniqueness rules reconcile durable duplicate operation evidence, but this document does not claim exactly-once remote invocation when two callers intentionally race the same previously unseen `OperationId` before either has persisted the consultation.

That limitation must not be reinterpreted as authorization for automatic polling.

## Coverage semantics

After trust validation, the cycle invokes the already accepted known-coverage read model.

The coverage result may be:

- `NoDocumentCoverage`;
- `PartialDocumentCoverage`;
- `FullDocumentCoverage`.

`FullDocumentCoverage` means only that every CFE identity in the durable Sobre is represented by non-contradictory currently known, XMLDSig-verified and PKI-trusted ACKCFE evidence.

It still does **not** prove that DGI has emitted its last ACKCFE message.

The composed result therefore remains explicit:

- `DgiIdentityValidated = false`;
- `ProtocolFinalityProven = false`;
- `TokenExhaustionProven = false`;
- `AutomaticReconsultationAuthorized = false`.

## DI completion

The persistence composition now registers the concrete ACKCFE consultation and trust repositories once per scope and exposes the same scoped adapters through both their write/replay and history-reader interfaces.

This makes the PR #106 coverage boundary resolvable by the production service container without introducing another persistence adapter or schema migration.

The service container also registers:

- `AssessFiscalCfeEnvelopeDocumentResponseCoverageUseCase`;
- `CollectFiscalCfeEnvelopeDocumentResponseEvidenceUseCase`.

## Deliberately not exposed as REST yet

This increment does **not** add a public REST endpoint.

The internal cycle is completed first so a later API surface cannot expose a raw consultation that leaves durable ACKCFE evidence without the signature and PKI trust checkpoints required by coverage assessment.

Any future REST contract must be separately governed for:

- permissions;
- idempotency header semantics;
- safe DTO projection without exposing the raw consultation token;
- whether raw ACKCFE XML is ever appropriate to return;
- explicit distinction between first consultation and later manual reconsultation.

## Explicit non-capabilities

This increment does not:

- poll DGI automatically;
- run a background worker or scheduler;
- call `Task.Delay` to form a polling loop;
- define a retry interval;
- define a maximum retry count;
- infer token exhaustion;
- infer protocol finality;
- mutate `FiscalDocument`, sale, accounting or inventory state;
- establish DGI signer identity/habilitation;
- change `EstadoCFE` semantics;
- implement S08 recovery;
- change Unknown transport recovery;
- allocate `IdEmisor`;
- establish DGI certification, homologation or production readiness.

## DGI Testing readiness

Formal DGI Testing remains:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

This increment improves internal evidence orchestration only. It does not remove the remaining product-capability blockers.
