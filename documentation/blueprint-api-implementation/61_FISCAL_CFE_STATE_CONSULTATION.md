# 61 - Fiscal CFE state consultation

Status: GOVERNED CANDIDATE

Date: 2026-09-13

## Purpose

Add one bounded read-only DGI consultation capability for an already durable local `FiscalDocument` identity, using the authoritative `ws_consultas / EFACCONSULTARESTADOCFE` contract.

This increment does not mutate the fiscal document, Sobre transport evidence, ACKSobre evidence, sale state, accounting state or inventory state. Returned evidence is stored append-only.

## Authoritative DGI evidence

The governing source for this increment is the DGI document:

- `Servicios Web Externos DGI`;
- code `T-5.020.00.001-000005`;
- version `1.9`;
- date `13/05/2024`.

The reviewed contract exposes `ws_consultas / EFACCONSULTARESTADOCFE` and defines the request identity as:

- `TipoCFE`;
- `Serie`;
- `Nro`.

Its response is `Ackconsultaestadocfe` and exposes:

- `EstadoCFE`;
- `IdEmisor`;
- `IdReceptor`;
- optional `ParamConsulta` containing `Token` and `Fechahora`.

The contract describes the returned consultation parameters as those corresponding to the first Sobre received by DGI for the queried CFE.

The current contract also establishes certificate-based WS-Security. As with the already accepted DGI consultation adapters, endpoint and SOAPAction are external configuration and are not inferred from examples.

## Important separation from token-based Consulta de Comprobantes

The separately published `Formato Mensajes Respuesta` describes a `Consulta de Comprobantes` request based on `ID Receptor + Token`, whose response can report the verification result of the CFE/CFC contained in the Sobre.

The governed `Servicios Web Externos DGI` v1.9 catalog reviewed for this increment does not expose an operation whose input contract is that `ID Receptor + Token` pair.

Therefore this increment deliberately does **not**:

- invent a token-input web-service operation;
- treat `EFACCONSULTARESTADOCFE` as the missing second-response operation;
- invoke the returned token;
- claim that the second/document-level response lifecycle has been implemented.

The existing token-based gap remains fail-closed.

## Implemented boundary

The candidate flow is:

```text
existing durable FiscalDocument
-> immutable TipoCFE + Serie + Nro identity
-> ws_consultas / EFACCONSULTARESTADOCFE
-> Ackconsultaestadocfe
-> raw EstadoCFE + IdEmisor + IdReceptor
-> optional Token + Fechahora evidence
-> exact raw response XML
-> SHA-256 evidence
-> append-only durable consultation row
```

Application receives only the local `FiscalDocumentId`. The use case obtains `TipoCFE`, `Serie` and `Nro` from the already durable fiscal identity instead of allowing the caller to supply a second competing fiscal identity.

Operation replay is scoped by `OrganizationId + OperationId`. Reusing an operation id for a different `FiscalDocumentId` fails closed.

## EstadoCFE handling

The reviewed authoritative material proves the existence of the `EstadoCFE` field but this increment does not establish a complete authoritative state-code taxonomy suitable for local lifecycle transitions.

Consequently:

- `EstadoCFE` is preserved as bounded raw external evidence;
- no local accepted/rejected/observed enum is inferred from examples;
- no `FiscalDocumentStatus` mutation occurs;
- no sale or accounting state changes occur;
- no automatic retry or recovery decision is derived from the returned state.

This separation prevents an example value or undocumented provider convention from becoming a product rule.

## Transport and XML safety

`DgiWsSecurityFiscalCfeStateConsultationGateway`:

- accepts only externally configured absolute HTTPS endpoint;
- requires an externally configured SOAPAction;
- uses the already governed organization-scoped transport certificate source;
- signs the SOAP body with the same bounded WS-Security pattern already accepted for DGI consultation adapters;
- prohibits DTD processing and external XML resolution;
- requires the expected `EFACCONSULTARESTADOCFE` response wrapper;
- requires `EstadoCFE`, `IdEmisor` and `IdReceptor`;
- treats `Token` and `Fechahora` as an all-or-nothing pair;
- preserves the raw SOAP response as durable evidence.

No endpoint, SOAPAction or state meaning is hardcoded from an example.

## Persistence

New table:

`v1_fiscal_cfe_state_consultations`

Durable evidence includes:

- consultation id;
- `FiscalDocumentId` with `Restrict` FK;
- organization id;
- snapshotted CFE type, series and number;
- operation id;
- raw `EstadoCFE`;
- DGI `IdEmisor`;
- DGI `IdReceptor`;
- optional token;
- optional consultation availability text;
- exact response XML;
- response SHA-256;
- whole-second consultation timestamp.

Database invariants:

- unique `OrganizationId + OperationId`;
- indexed `FiscalDocumentId`;
- `Restrict` FK to `v1_fiscal_documents`.

The source `FiscalDocument` remains immutable.

## Verification strategy

Cross-cutting tests cover:

- authoritative request element names and `TipoCFE + Serie + Nro` identity;
- WS-Security certificate/signature presence;
- absence of token input in the request body;
- response parsing with `EstadoCFE`, ids and consultation parameters;
- fail-closed incomplete `Token/Fechahora` evidence;
- DTD rejection;
- application operation replay and mismatched replay rejection.

Provider-real PostgreSQL/MySQL tests cover:

- append-only round-trip tied to an actual durable `FiscalDocument`;
- source fiscal identity remains unchanged;
- provider-enforced unique operation identity rejects competing evidence.

## Explicit non-scope

This increment does not implement:

- `IDReceptor + Token` second/document-level CFE response consultation;
- authoritative interpretation of all `EstadoCFE` values;
- CFE lifecycle mutation from consultation evidence;
- ACKSobre certificate trust-chain or trust-anchor validation;
- OCSP/CRL or DGI certificate habilitation;
- S08 recovery;
- ambiguous Sobre `Unknown` reconciliation;
- automatic `Idemisor` allocation;
- CFE business grouping/batching policy;
- DGI Testing acceptance;
- Production enablement.

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**
