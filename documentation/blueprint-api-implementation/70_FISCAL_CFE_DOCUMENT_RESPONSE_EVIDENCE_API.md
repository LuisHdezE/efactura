# Fiscal CFE document-response evidence API

Status: GOVERNED IMPLEMENTATION CANDIDATE

## Scope

This increment exposes one bounded API v1 surface over the already accepted explicit ACKCFE evidence cycle from implementation record 69.

Canonical operation:

- API ID: `API-FIS-010`;
- operationId: `collectFiscalEnvelopeDocumentResponseEvidence`;
- method/path: `POST /api/v1/fiscal-envelopes/{envelopeId}/document-response-evidence`;
- permission: `fiscal.regularization.manage`;
- idempotency: REQUIRED through `Idempotency-Key`.

The endpoint does not expose the raw ACKCFE consultation boundary. One successful call delegates to the complete accepted chain:

```text
accepted durable Sobre submission
-> accepted ACKSobre IdReceptor + Token evidence
-> explicit EFACCONSULTARESTADOENVIO consultation
-> exact ACKCFE XMLDSig verification
-> PKI Uruguay certificate-trust validation
-> trusted known-document coverage assessment
```

## Resource identity and server-owned fiscal evidence

The client identifies the durable Sobre only by its opaque local `envelopeId` GUID. It does not submit `IssuerRuc`, `ReceiverRut`, DGI `Idemisor`, `IdReceptor` or consultation `Token` as authoritative request fields.

Application resolves the durable Sobre submission by `envelopeId`, verifies organization scope and persisted submission integrity, and then delegates the stored issuer RUC, receiver RUT and sender-envelope identity to the canonical cycle.

A GUID belonging to another organization is surfaced as not found rather than disclosing cross-organization resource existence.

## HTTP idempotency and explicit reconsultation

`Idempotency-Key` is mandatory. The Web API derives a bounded internal operation id from the canonical API id, organization scope, envelope GUID and the caller key using the existing SHA-256 request-hash helper. The raw idempotency key is therefore not copied into the fiscal operation id and the 120-character Application bound remains preserved.

For one organization + envelope + key, the same derived operation id resumes/replays the durable consultation/signature/trust checkpoints. Supplying a different idempotency key is an explicit caller request for another consultation cycle.

This does not claim exactly-once remote invocation when concurrent first executions race before durable operation evidence exists. Persistence uniqueness still reconciles durable duplicate-operation evidence, but no distributed lock is invented around the DGI call.

## Public response projection

The response exposes only bounded operational evidence useful to an authorized fiscal operator:

- durable envelope/submission/ACK/consultation opaque ids;
- consultation timestamp and response SHA-256;
- ACKCFE counters;
- `NO_DOCUMENT_COVERAGE`, `PARTIAL_DOCUMENT_COVERAGE` or `FULL_DOCUMENT_COVERAGE`;
- covered CFE identities with authoritative message-local state (`AE`, `BE`, `CE`) and semantic projection;
- missing CFE identities;
- XML signature and PKI Uruguay trust booleans;
- replay flags;
- the explicit non-capability flags.

The public DTO deliberately excludes:

- raw `ACKCFE` XML;
- `IdReceptor`;
- consultation `Token` and token hash;
- embedded certificate bytes;
- certificate subject, issuer, thumbprint and serial number;
- signature XML/material;
- any last-message or finality indicator not established by authoritative evidence.

## Safety boundary

`FULL_DOCUMENT_COVERAGE` means only that every CFE identity in the durable Sobre is represented by non-contradictory currently known XMLDSig-verified and PKI-trusted ACKCFE evidence. It does not prove that DGI emitted its last response message.

The REST surface preserves the accepted explicit flags:

- `DgiIdentityValidated = false`;
- `ProtocolFinalityProven = false`;
- `TokenExhaustionProven = false`;
- `AutomaticReconsultationAuthorized = false`.

The endpoint contains no timer, polling loop, background worker, retry cadence, maximum retry count or business timeout. It does not mutate `FiscalDocument`, sale, accounting, inventory, Sobre transport state or any other local business lifecycle.

The operation is protected with `fiscal.regularization.manage`, not `fiscal.read`, because it actively contacts DGI and can append new external evidence.

## Persistence and migration impact

No migration is introduced. No persistence model changes. The API-facing Application wrapper reuses the existing `IFiscalCfeEnvelopeSubmissionRepository.GetByEnvelopeIdAsync` read and the already accepted ACKCFE cycle.

## DGI readiness

Formal traditional DGI Testing readiness remains:

`BLOCKED BY MISSING PRODUCT CAPABILITIES`
