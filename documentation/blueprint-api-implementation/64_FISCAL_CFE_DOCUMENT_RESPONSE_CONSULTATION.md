# 64 — Fiscal CFE document-response consultation by ACKSobre token

Status: **GOVERNED IMPLEMENTATION CANDIDATE**

Date: 2026-09-13

## Purpose

Introduce one bounded, read-only consultation after an accepted DGI `ACKSobre` when that acknowledgement carries the governed `IdReceptor + Token` pair.

This slice closes a previously fail-closed evidence gap. The authoritative token-input operation is not part of the separately reviewed `ws_consultas` v1.9 catalog. It is documented by DGI in the reception service `ws_efactura` as:

`WS_eFactura.EFACCONSULTARESTADOENVIO`

with an input `ConsultaCFE` containing:

- `IdReceptor`;
- `Token`.

The operation returns `ACKCFE` in `DataOut/xmlData`.

## Authoritative evidence used

### DGI Servicios Web Externos Factura Electrónica

Official DGI technical document:

- code `T-5.020.00.001-004`;
- version `1.1`;
- date `23/05/2013`;
- current DGI eFactura portal copy reviewed on 2026-09-13.

The published `ws_efactura / EFACCONSULTARESTADOENVIO` contract shows direct CDATA framing:

```xml
<Datain xmlns="http://dgi.gub.uy">
  <xmlData><![CDATA[
    <ConsultaCFE xmlns="http://dgi.gub.uy">
      <IdReceptor>...</IdReceptor>
      <Token>...</Token>
    </ConsultaCFE>
  ]]></xmlData>
</Datain>
```

and returns:

```xml
<DataOut xmlns="http://dgi.gub.uy">
  <xmlData><![CDATA[
    <ACKCFE>...</ACKCFE>
  ]]></xmlData>
</DataOut>
```

The same official document contains a concrete SOAP example whose `ACKCFE` caratula includes `RUCReceptor`, `RUCEmisor`, `IDRespuesta`, `IDEmisor`, `IDReceptor`, `CantenSobre`, `CantResponden`, accepted/rejected/observed counters and per-document `ACKCFE_det` entries with `TipoCFE`, `Serie`, `NroCFE` and `Estado`.

### DGI Formato del Mensaje de Respuesta

The current DGI portal publishes `Formato Mensajes Respuesta v19` and continues to define **Consulta de Comprobantes** as the issuer query by `ID Receptor + Token`, returning the verification result for each CFE/CFC.

The response-format lineage explicitly states that the per-CFE result may be generated in **multiple messages or one response**. Therefore this implementation must never infer that one `ACKCFE` completes the whole Sobre lifecycle.

## Accepted implementation boundary

The slice adds:

1. `ConsultFiscalCfeEnvelopeDocumentResponseUseCase` in Application;
2. `IFiscalCfeEnvelopeDocumentResponseConsultationGateway` port;
3. `DgiWsSecurityFiscalCfeEnvelopeDocumentResponseConsultationGateway` Infrastructure adapter;
4. direct `ConsultaCFE` CDATA request framing for `IdReceptor + Token`;
5. WS-Security X.509 body signing following the already isolated DGI reception transport pattern;
6. safe XML parsing with DTD processing prohibited and no external resolver;
7. exact `ACKCFE` XML preservation plus SHA-256;
8. structural extraction of bounded caratula counters and per-CFE detail identity/state evidence;
9. correlation to the exact durable Sobre, durable accepted ACKSobre and the immutable CFE identities contained in that Sobre;
10. append-only PostgreSQL/MySQL persistence with operation replay protection;
11. explicit support for a partial `ACKCFE`, without requiring `CantResponden == CantenSobre`;
12. provider-real persistence coverage and cross-cutting transport/parser/use-case tests.

## Source prerequisites

The consultation is permitted only when all of the following already exist:

```text
durable Sobre
-> durable ResponseReceived submission
-> durable structural ACKSobre observation
-> ACKSobre state AS / Received
-> non-empty IdReceptor + Token pair
```

A rejected `BS` acknowledgement or an ACK without the token pair is not eligible for this consultation.

The source evidence is never mutated.

## Correlation rules

The returned `ACKCFE` must correlate to the source through:

- `RUCEmisor == durable Sobre IssuerRuc`;
- `RUCReceptor == durable Sobre ReceiverRut`;
- `IDEmisor == durable SenderEnvelopeId`;
- `IDReceptor == durable ACKSobre DgiReceiverId`;
- `CantenSobre == durable Sobre CfeCount`.

Every returned `ACKCFE_det` must map uniquely to a durable CFE identity already contained in that Sobre by exact:

`TipoCFE + Serie + NroCFE`.

Unexpected or duplicate response detail fails closed.

`CantResponden` is required to match the number of response-detail rows in that single returned `ACKCFE`. It is deliberately **not** required to equal the total CFE count in the Sobre.

## Append-only persistence

One consultation operation persists:

- source ACK observation id;
- source submission id;
- source envelope id;
- organization id;
- operation id;
- source ACK response SHA-256;
- DGI `IdReceptor`;
- SHA-256 of the source consultation token rather than duplicating the token in the new row;
- DGI response id;
- correlated RUC and envelope ids/counts;
- response counters;
- serialized bounded per-CFE details;
- exact returned `ACKCFE` XML;
- response SHA-256;
- whole-second consultation timestamp.

`OrganizationId + OperationId` is unique. Multiple separately identified consultation operations may reference the same accepted ACKSobre because DGI permits per-CFE results to evolve across multiple response messages.

All foreign keys to the accepted ACKSobre/submission/Sobre chain use `Restrict` semantics.

## Explicit non-capabilities

This slice does **not**:

- mutate `FiscalDocument`, sale, accounting, inventory or fiscalization lifecycle state;
- interpret raw `ACKCFE_det/Estado` as a local lifecycle transition;
- claim that one query yields the final state for all CFE in the Sobre;
- poll DGI automatically;
- schedule retries;
- change the existing `Unknown` transport policy;
- implement `S08` recovery;
- allocate `Idemisor`;
- validate the embedded `ACKCFE` business-document XMLDSig or PKI chain;
- prove DGI legal signer identity;
- reinterpret the separately accepted `EFACCONSULTARESTADOCFE` boundary;
- expose a new public REST endpoint;
- change formal DGI Testing readiness.

Formal traditional DGI Testing readiness therefore remains:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Architecture

Dependency direction remains:

```text
Domain
  ^
Application
  ^
Infrastructure
```

Application owns the consultation contract, source-correlation policy and append-only persistence port. Infrastructure owns SOAP/WS-Security/XML details and EF persistence. No DGI XML or HTTP concern enters Domain.

## Follow-up boundaries

Any later increment must remain independent and separately governed, including:

- cryptographic XMLDSig verification of returned `ACKCFE`;
- PKI Uruguay trust validation for that signature;
- DGI-specific signer identity/habilitation;
- semantic interpretation of `ACKCFE_det/Estado`;
- a product policy for polling/reconsultation or completeness assessment;
- local fiscal-document reconciliation after authoritative state semantics are proven.
