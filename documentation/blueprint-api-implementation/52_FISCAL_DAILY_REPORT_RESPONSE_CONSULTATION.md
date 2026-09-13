# Fiscal Daily Report response consultation foundation

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

Consultation begins from a DGI `IdReceptor` already stored as durable local evidence.

The target reader can resolve either:

1. the root Reporte Diario submission; or
2. a same-`SecEnvio` BR correction revision.

The target must resolve to exactly one durable local artifact. Missing or ambiguous receiver ids fail closed before crossing the network boundary.

This is intentionally not a discovery service.

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

`EFACCONSULTARRESPUESTAREPORTE` requires `IdReceptor`. Therefore it can reconcile only cases where that receiver id is already known durably from trustworthy evidence.

An `Unknown` attempt that has no `IdReceptor` cannot be discovered by this method alone. Discovery through other authoritative DGI consultation capabilities, including the separately documented `EFACCONSULTARENVIOSREPORTE`, is outside this slice and must be evidenced before implementation.

Automatic retry from `Unknown` remains forbidden.

## Validation boundary

The candidate validation covers:

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

The exact final PR head must pass the complete Clean Architecture Guard before review or merge.

## Deliberate non-scope

This increment does not implement:

- `EFACCONSULTARENVIOSREPORTE` receiver-id discovery;
- automatic state mutation or retry recovery for `Unknown`;
- `DR` / `ER` / `FR` reconciliation lifecycle;
- automatic `R05` sequence recovery;
- independent cryptographic validation of DGI ACK signatures;
- Sobre v05 packaging/submission;
- Production enablement.

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**
