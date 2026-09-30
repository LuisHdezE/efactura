# Fiscal CFE DGI state-evidence API

Status: GOVERNED IMPLEMENTATION CANDIDATE

Baseline after PR #110 retarget: `main@8201b915dd08a6a6167773bc20db98029fbcb898`.

The implementation delta is evaluated against this accepted `main` baseline; the former stacked PR #110 branch is no longer the active base.

## Scope

This increment exposes one bounded API v1 surface over the already accepted read-only `ws_consultas / EFACCONSULTARESTADOCFE` capability documented in implementation record 61.

Canonical operation:

- API ID: `API-FIS-011`;
- operationId: `collectFiscalDocumentDgiStateEvidence`;
- method/path: `POST /api/v1/fiscal-documents/{fiscalDocumentId}/dgi-state-evidence`;
- permission: `fiscal.regularization.manage`;
- idempotency: REQUIRED through `Idempotency-Key`.

The endpoint performs one explicit caller-triggered DGI consultation and stores the returned evidence append-only through the existing `ConsultFiscalCfeStateUseCase`.

## Resource identity

The client supplies only the opaque local `fiscalDocumentId` GUID. Organization scope is resolved from the authenticated API v1 context. `TipoCFE`, `Serie` and `Nro` are read from the already durable fiscal document by Application and are not accepted as competing caller input.

The existing Application use case performs organization-scoped document lookup before contacting DGI. A document outside the current organization therefore remains unavailable through this surface.

## HTTP idempotency

`Idempotency-Key` is mandatory. The Web API derives a bounded internal operation id from the canonical API id, organization scope, fiscal-document GUID and caller key using the existing SHA-256 request-hash helper. The raw caller key is not copied into fiscal evidence.

For one organization + fiscal document + key, the same derived operation id replays the existing durable consultation evidence. A different idempotency key is an explicit caller request for another consultation.

The existing Application boundary does not claim exactly-once remote invocation for concurrent first executions that race before durable operation evidence exists. Persistence uniqueness reconciles durable duplicate-operation evidence, but no distributed lock or undocumented DGI retry guarantee is invented.

## Public response projection

The response exposes only bounded evidence required by an authorized fiscal operator:

- consultation GUID;
- fiscal-document GUID;
- CFE type, series and number;
- `externalStateCode`, preserving the bounded raw DGI `EstadoCFE` value without interpretation;
- consultation timestamp;
- exact response SHA-256;
- replay state;
- explicit product flags stating that state meaning is unresolved and local lifecycle mutation is not authorized.

The public DTO deliberately excludes:

- consultation `Token`;
- consultation `FechaHora` text;
- raw SOAP/XML response;
- DGI `IdEmisor`;
- DGI `IdReceptor`;
- endpoint/SOAP transport metadata.

## EstadoCFE safety boundary

The authoritative material accepted for implementation record 61 proves that `EstadoCFE` exists, but it does not establish a complete state-code taxonomy suitable for local lifecycle transitions.

Consequently this API:

- exposes the value only as `externalStateCode`;
- does not translate it to accepted/rejected/observed local semantics;
- reports `StateMeaningResolved = false`;
- reports `LocalLifecycleMutationAuthorized = false`;
- does not mutate `FiscalDocument`, Sale, accounting, inventory or Sobre state;
- does not trigger a second consultation from the returned Token;
- does not infer protocol finality, retry cadence or recovery policy.

The ACKCFE `AE/BE/CE` detail taxonomy accepted elsewhere must not be reused as the taxonomy for this separate `EstadoCFE` field.

## Permission boundary

The operation uses `fiscal.regularization.manage`, not `fiscal.read`, because the request actively contacts DGI and can append new external evidence. A read permission alone must not authorize an outbound fiscal consultation.

## Persistence and migration impact

No migration is introduced. No persistence model changes. `ConsultFiscalCfeStateUseCase` and its already accepted repository/gateway registrations are reused unchanged.

## DGI readiness

This API does not prove DGI signer identity/habilitation, token exhaustion, protocol finality, automatic polling/reconsultation or Production enablement.

Formal traditional DGI Testing readiness remains:

`BLOCKED BY MISSING PRODUCT CAPABILITIES`
