# Fiscal Daily Report response consultation foundation

Status: ACCEPTED

Accepted baseline: `main@85fe095449f7b4e9bfb97b45e01ae283b8071913` (merge of PR #79).

Post-merge validation: Clean Architecture Guard #312, run `34727894835`, SUCCESS for Build/Architecture and PostgreSQL/MySQL transaction jobs.

## Scope

This increment adds the first authoritative Reporte Diario consultation capability for a **known durable DGI `IdReceptor`**. It queries DGI for the original response associated with that report receiver id and stores the returned `ACKRepDiario` as new append-only evidence.

The capability deliberately does not reinterpret or rewrite the existing transport lifecycle. Root submissions and same-`SecEnvio` BR correction revisions remain immutable historical evidence.

## Authoritative DGI contract

The implementation is based on the current official DGI technical document:

- `Servicios Web Externos DGI`;
- code `T-5.020.00.001-000005`;
- version `1.9`;
- date `13/05/2024`;
- official publication: `https://www.efactura.dgi.gub.uy/files/web-services-externos-consultas-pdf?es=`.

For `WS_Consultas / EFACCONSULTARRESPUESTAREPORTE`, DGI states that, given the `Id Receptor` of a report, the service returns the **original response of the consulted report**.

The published request shape is:

```xml
<dgi:WS_eFactura_Consultas.EFACCONSULTARRESPUESTAREPORTE>
  <dgi:Consultarrespuestareporte>
    <dgi:IdReceptor>...</dgi:IdReceptor>
  </dgi:Consultarrespuestareporte>
</dgi:WS_eFactura_Consultas.EFACCONSULTARRESPUESTAREPORTE>
```

The published response wrapper is `WS_eFactura_Consultas.EFACCONSULTARRESPUESTAREPORTEResponse` in namespace `http://dgi.gub.uy` and carries the original `ACKRepDiario` payload.

The same official document publishes the TEST WSDL at:

`https://efactura.dgi.gub.uy:6460/ePrueba/ws_consultasPrueba?wsdl`

That published TEST location is regulatory/development evidence only. Runtime `Endpoint` and `SoapAction` remain mandatory external configuration and are not inferred or hard-coded by the application.

## WS-Security boundary

The consultation adapter lives only in Infrastructure and reuses the organization-scoped externally configured DGI transport certificate source.

The request follows the existing DGI SOAP/WS-Security boundary already isolated for fiscal transport:

- SOAP 1.1;
- X509v3 `BinarySecurityToken`;
- exclusive canonicalization;
- legacy RSA-SHA1/SHA1 only inside the SOAP transport signature.

The legacy SHA1 profile must never leak into Reporte Diario fiscal XMLDSig policy. Reporte Diario fiscal signing remains independently SHA-256 based.

No PFX, password or private key is stored by this capability.

## Local target resolution

Consultation begins from a DGI `IdReceptor` stored as durable local evidence.

The target reader can resolve either:

1. the root Reporte Diario submission; or
2. a same-`SecEnvio` BR correction revision.

On the original PR #79 baseline, the receiver id could be present directly on root/revision transport evidence. Accepted PR #80 added a separately governed append-only discovery record that can also resolve a receiver id back to exactly one root/revision target without rewriting that target.

The target must resolve to exactly one durable local artifact. Missing or ambiguous receiver ids fail closed before crossing the network boundary.

This method is not itself a discovery operation.

## Original ACK boundary

This slice treats `EFACCONSULTARRESPUESTAREPORTE` as a retrieval of the **original** `ACKRepDiario` for the known receiver id.

The returned payload must therefore:

- be safe, well-formed XML;
- have root `ACKRepDiario`;
- use namespace `http://cfe.dgi.gub.uy`;
- contain `IDReceptor` equal to the requested durable receiver id;
- carry original immediate state `AR` or `BR`.

Later processing/reconciliation states `DR`, `ER` and `FR` are not accepted as the original ACK in this bounded capability. They require a separate evidence model and lifecycle slice.

The raw returned ACK XML is preserved. Independent cryptographic validation of the DGI signature inside that ACK remains a later gate.

## Durable consultation evidence

Each successful consultation creates an append-only record containing:

- exactly one local target: root submission or BR correction revision;
- `OrganizationId`;
- issuer RUC;
- `FechaResumen`;
- `SecEnvio`;
- local BR revision number when applicable;
- caller `OperationId`;
- DGI `IdReceptor`;
- returned `AR` / `BR` state;
- raw `ACKRepDiario` XML;
- SHA-256 hash of the raw ACK XML;
- consultation timestamp;
- consistency classification against the immediate ACK already stored locally.

`OperationId` is unique per organization. Replaying the same operation id returns the durable consultation evidence and does not query DGI again. Reusing an operation id for a different receiver id fails closed.

## Consistency classification

The consultation evidence records one of three observations:

- `NoImmediateAck`: local target has no immediate ACK state available for comparison;
- `MatchesImmediateAck`: consulted original ACK state agrees with durable immediate local evidence;
- `ConflictsWithImmediateAck`: consulted original ACK state differs from durable immediate local evidence.

A conflict is preserved as evidence. This capability does **not** overwrite the original submission, correction revision, immediate ACK, transport state or sequence authorization.

Any state-changing interpretation belongs to a later governed reconciliation capability.

## Why this does not solve every `Unknown`

A local transport state of `Unknown` means DGI may have received bytes but the application did not durably obtain a trustworthy immediate response.

`EFACCONSULTARRESPUESTAREPORTE` requires `IdReceptor`. Therefore it can retrieve the original response only when that receiver id is known durably from trustworthy evidence.

An `Unknown` attempt that has no `IdReceptor` cannot be discovered by this method alone. Accepted PR #80 now supplies authoritative discovery through `EFACCONSULTARENVIOSREPORTE`, documented in `53_FISCAL_DAILY_REPORT_RECEIVER_DISCOVERY.md`. The discovered receiver remains append-only evidence and is resolved back to the original local root/revision target without rewriting that target.

Automatic retry from `Unknown` remains forbidden.

## Validation boundary

Accepted validation covers:

- root consultation and operation replay without a second DGI call;
- BR correction-revision consultation;
- missing and ambiguous receiver fail-closed behavior;
- consulted/local ACK conflict preservation without state mutation;
- receiver mismatch rejection;
- published SOAP operation and `IdReceptor` request structure;
- safe parsing of original AR/BR ACK payloads;
- rejection of DR/ER/FR as original ACK in this slice;
- DTD/malformed XML rejection;
- PostgreSQL and MySQL round-trip of append-only consultation evidence;
- PostgreSQL and MySQL operation-id uniqueness.

PR #79 exact head `2a6d8cc3d8b1e8836fa74e8f00350029311cb18a` passed Clean Architecture Guard #311 before merge. Accepted `main@85fe095449f7b4e9bfb97b45e01ae283b8071913` then passed post-merge Guard #312.

Accepted PR #80 subsequently extended target resolution through durable receiver-discovery evidence at `main@7e930d8ffe1978da24ea8365b9661f74162d51b5`; post-merge Guard #320 passed.

## Deliberate non-scope

This accepted increment does not itself implement:

- receiver discovery logic; that is the accepted separate PR #80 / document 53 capability;
- automatic state mutation or retry recovery for `Unknown`;
- state-changing `DR` / `ER` / `FR` reconciliation semantics;
- automatic `R05` sequence recovery;
- independent cryptographic validation of DGI ACK signatures;
- Sobre v05 packaging/submission;
- Production enablement.

PR #81 / document 54 separately proposes append-only observation of DGI `DR`, `ER` and `FR` without local state mutation.

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**
