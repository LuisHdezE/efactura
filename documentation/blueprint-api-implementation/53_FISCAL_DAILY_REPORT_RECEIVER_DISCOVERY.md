# 53 — Fiscal Daily Report Receiver Discovery

Status: GOVERNED IMPLEMENTATION CANDIDATE

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Purpose

This bounded increment adds authoritative discovery of a DGI `IdReceptor` for a durable Reporte Diario transport target whose local delivery state is `Unknown` and which has no receiver id.

The capability exists to close the gap left deliberately open by document 52: `EFACCONSULTARRESPUESTAREPORTE` can retrieve the original `ACKRepDiario` only after an `IdReceptor` is known. An ambiguous transport failure may leave the local target in `Unknown` without that identifier.

This increment does not retry the report, does not rewrite the transport state and does not treat discovery output as final reconciliation of AR/BR/DR/ER/FR semantics.

## Authoritative DGI evidence

Source reviewed for this increment:

- DGI, **Servicios Web Externos DGI**;
- code `T-5.020.00.001-000005`;
- version `1.9`;
- date `13/05/2024`;
- service `ws_consultas`;
- method `EFACCONSULTARENVIOSREPORTE`.

The published contract states that, given `FechaResumen`, `Secuencia` and `IdEmisor`, the method returns a collection with data for reports matching the supplied conditions. `Secuencia` and `IdEmisor` are optional.

The published request shape contains:

```xml
<dgi:WS_eFactura_Consultas.EFACCONSULTARENVIOSREPORTE>
  <dgi:Consultaenviosreporte>
    <dgi:FechaResumen>2013-01-01</dgi:FechaResumen>
    <dgi:Secuencia>...</dgi:Secuencia>
    <dgi:IdEmisor>...</dgi:IdEmisor>
  </dgi:Consultaenviosreporte>
</dgi:WS_eFactura_Consultas.EFACCONSULTARENVIOSREPORTE>
```

The published response contains `Ackconsultaenviosreporte / ColeccionDatosReporte / DatosReporte`, with each row carrying:

- `IdEmisor`;
- `IdReceptor`;
- `Estado`;
- `FechaHoraRecepcion`.

The DGI example shows `AR` and `BR` rows. This increment preserves returned `Estado` as external evidence but deliberately does **not** infer from that example that these are the only possible values or use the field to implement later-state reconciliation.

## Query boundary

The consumer sends:

- authoritative local `FechaResumen`;
- authoritative local `Secuencia`.

It deliberately omits optional `IdEmisor` because the current durable model does not yet hold authoritative DGI `IdEmisor` evidence that can safely be derived from the issuer RUC.

The endpoint and SOAPAction remain mandatory external configuration under:

`FiscalTransport:DailyReportReceiverDiscovery`

They are not inferred from example URLs, provider code or memory.

The WS-Security transport signature remains isolated to Infrastructure and uses the DGI external-web-service boundary already established for `ws_consultas`: X509 `BinarySecurityToken`, exclusive canonicalization and the legacy RSA-SHA1/SHA1 algorithms required by that transport boundary. This does not weaken the SHA-256 policy used for fiscal XML artifacts.

## Local target eligibility

Discovery is allowed only for one explicit durable local target:

- root Reporte Diario submission; or
- same-`SecEnvio` BR correction revision.

The target must:

1. belong to the requested organization;
2. exist durably;
3. be in `Unknown` state;
4. have no durable `DgiReceiverId`.

Any other local state fails closed before network access.

## Fail-closed association rule

`FechaResumen + Secuencia` can legitimately identify more than one DGI report over the life of a same-sequence correction chain. Therefore the system does not choose a row by reception time, collection order or proximity to the local attempt timestamp.

The association algorithm is:

1. collect every durable `IdReceptor` already associated locally with the same organization, issuer RUC, `FechaResumen` and `Secuencia` from root submissions, BR correction revisions and earlier discovery evidence;
2. query DGI using `FechaResumen + Secuencia`;
3. remove DGI rows whose `IdReceptor` is already accounted for locally;
4. require exactly one unaccounted receiver id;
5. persist that row and the raw `Ackconsultaenviosreporte` evidence append-only.

Outcomes:

- zero unaccounted receiver ids: fail closed as `receiver_not_found`;
- more than one unaccounted receiver id: fail closed as `receiver_ambiguous`;
- duplicate rows for an unaccounted receiver id: fail closed as ambiguous external evidence;
- exactly one unaccounted receiver id: persist discovery evidence.

No time-based heuristic is allowed.

## Durable evidence

Table: `v1_fdr_receiver_discoveries`.

Each record preserves:

- exactly one local target (`RootSubmissionId` or `BrCorrectionRevisionId`);
- organization, issuer RUC, summary date and sequence;
- local correction revision when applicable;
- operation id;
- returned DGI `IdEmisor`;
- returned DGI `IdReceptor`;
- returned DGI `Estado` as uninterpreted evidence;
- returned `FechaHoraRecepcion` as the exact text supplied by DGI, avoiding an invented timezone interpretation;
- raw `Ackconsultaenviosreporte` XML;
- SHA-256 hash of that XML;
- durable discovery timestamp.

Uniqueness protects:

- organization + operation id;
- organization + discovered receiver id;
- one discovery per root submission;
- one discovery per BR correction revision.

The record is append-only. The discovery use case does not update the root submission or correction revision.

## Chaining into original-response consultation

Document 52 remains the authority for `EFACCONSULTARRESPUESTAREPORTE`.

After durable receiver discovery, the response-consultation target reader may resolve the discovered `IdReceptor` back to its local root/revision target. The immediate local ACK remains absent because the transport target is still `Unknown`, so the later original-response consultation is classified as `NoImmediateAck` until a separate reconciliation capability is introduced.

This creates a safe evidence chain:

```text
Unknown local transport target
-> EFACCONSULTARENVIOSREPORTE
-> append-only discovered IdReceptor evidence
-> EFACCONSULTARRESPUESTAREPORTE
-> append-only original ACKRepDiario evidence
-> future governed reconciliation decision
```

The chain still does not auto-retry or auto-rewrite fiscal state.

## Validation coverage

The increment requires:

- Application tests for root and BR-revision targets;
- operation replay without repeated DGI query;
- target-state and already-known-receiver guards;
- zero-match and multi-match fail-closed behavior;
- known-receiver set subtraction;
- exact SOAP operation, `FechaResumen` and `Secuencia` emission;
- deliberate omission of non-authoritative `IdEmisor` input;
- secure XML parsing and required output fields;
- provider-real PostgreSQL/MySQL persistence, target uniqueness and consultation lookup through discovery evidence;
- architecture guards keeping HTTP/X509/SignedXml outside Application.

## Deliberate non-scope

This increment does not implement:

- automatic transition of `Unknown` to `Received` or `Rejected`;
- automatic resend/retry of an ambiguous delivery;
- semantic reconciliation of `DR`, `ER` or `FR`;
- sequence repair for DGI rejection `R05`;
- independent cryptographic validation of DGI ACK signatures;
- Sobre v05 packaging/submission;
- external DGI Testing acceptance;
- Production enablement.

Those remain separately governed capabilities.
