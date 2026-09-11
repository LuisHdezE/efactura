# 46 — Reporte Diario v13.2 unsigned XML

## Decision

This increment materializes the accepted Reporte Diario v13.2 semantic wire projection as deterministic **pre-signature XML** for the current Release-1 domestic family boundary:

- 101 e-Ticket;
- 102 Nota de Crédito de e-Ticket;
- 103 Nota de Débito de e-Ticket;
- 111 e-Factura;
- 112 Nota de Crédito de e-Factura;
- 113 Nota de Débito de e-Factura.

It does not create an XML Digital Signature, choose cryptographic algorithms, persist/replay reports, submit to DGI, parse report acknowledgements or package a Sobre.

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Authority rechecked 2026-09-11

The current DGI `Documentos de interés` registry continues to publish for both Testing and Production:

- `Formato Reporte CFE v13 2.pdf` — **Publicado / Publicado**;
- `XSDs_FE_V1.44.2` — **Publicado / Publicado**.

Registry:

`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`

The serializer uses the exact schema closure already byte-pinned in implementation record #42. No new schema bytes are downloaded or substituted in this increment.

## Pinned schema facts used

`ReporteDiarioCFE.xsd` defines:

- root `Reporte` in namespace `http://cfe.dgi.gub.uy`;
- mandatory `Caratula` first;
- fixed `Caratula/@version = 1.0`;
- mandatory final `ds:Signature`;
- current Release-1 summary elements in schema order:
  - `Rsmn_Tck` (101),
  - `Rsmn_Tck_Nota_Credito` (102),
  - `Rsmn_Tck_Nota_Debito` (103),
  - `Rsmn_Fac` (111),
  - `Rsmn_Fac_Nota_Credito` (112),
  - `Rsmn_Fac_Nota_Debito` (113).

The amount table is emitted as `Montos/Mnts_FyT_Item` in the exact pinned XSD element order. The serializer consumes only facts already present in `FiscalDailyReportWireProjection`.

## Deterministic Carátula

`DeterministicUnsignedDailyReportXmlBuilder` emits:

1. `RUCEmisor` from the immutable projection;
2. `FechaResumen` from the projection summary date;
3. no `IDEmisor`, because the current projection does not freeze that optional sender identifier;
4. `SecEnvio` from the immutable report sequence;
5. `TmstFirmaEnv` from an explicitly supplied, already frozen whole-second signing timestamp;
6. `CantComprobantes` from the projection used-CFE total.

The builder does not silently truncate a sub-second timestamp. Such input fails closed.

The timestamp is an input because the XSD requires `TmstFirmaEnv` before the final XMLDSig exists. Persistence and lifecycle orchestration for that signing evidence remain a separate capability.

## Resumen mapping

For every projected type counter, the serializer emits one corresponding summary zone. Amount rows are ordered deterministically by:

1. document date;
2. DGI branch;
3. third-party-payment indicator.

For the accepted `Montos_FyT` boundary it emits the projection's explicit semantic values for:

- B-C12 through B-C21 monetary concepts;
- B-C22/B-C23 only when the projection contains the corresponding VAT rate;
- B-C24;
- B-C25 and B-C25.1.

Release-1 zero values for concepts not yet supported by product arithmetic are not invented by the XML layer. They arrive already frozen in the semantic projection created by the wire-readiness boundary.

`IndPagCta3ros` is emitted only when the source fact is explicitly true, preserving the accepted distinction between absence and value `1`.

Counters and ranges are emitted in pinned schema order. B-C27 is emitted only for the e-Ticket families 101/102/103; e-Factura families cannot acquire that field through the serializer.

## Monetary lexical form

The semantic projection has already completed DGI-backed mathematical two-decimal quantization and v13.2 algebra checks before serialization.

The XML layer renders monetary values deterministically with exactly two decimal places. This changes lexical representation only, not the already accepted numeric value. VAT rates are rendered with up to three decimal places, matching the pinned wire scale.

## Why the unsigned artifact cannot claim full XSD validity

The pinned `ReporteDiarioCFE.xsd` requires `ds:Signature` as the final child of `Reporte` with `minOccurs=1`.

An artifact that intentionally has no signature is therefore not, by definition, a complete schema-valid Reporte. Pretending otherwise would collapse two different gates.

`DgiFeV1_44_2UnsignedDailyReportSchemaValidator` provides a narrower structural guarantee:

1. loads the embedded byte-pinned `ReporteDiarioCFE.xsd`, `DGITypes.xsd` and `xmldsig-core-schema.xsd`;
2. verifies every original resource against the SHA-256 recorded in `daily-report-schema-manifest.json`;
3. keeps the original embedded bytes unchanged;
4. in memory only, requires exactly one known mandatory final signature declaration and changes only its cardinality from `minOccurs="1"` to `minOccurs="0"`;
5. compiles that derivative closure with external schema resolution forbidden;
6. rejects any input that already contains a `ds:Signature`;
7. validates the pre-signature XML against the resulting structural schema.

The validation result explicitly records `SignatureRequirementRelaxedForUnsignedValidation = true` so callers cannot confuse this with final signed-root validation.

## Security boundary

The XML and schema paths:

- prohibit DTD processing for document parsing;
- use no HTTP client and perform no network schema resolution;
- verify the pinned schema hashes before compilation;
- never access a certificate or private key;
- never generate a placeholder/fake XMLDSig node;
- never reuse the historical example's SHA-1 algorithms as current policy.

## Explicit non-scope

This increment does **not** add:

- Reporte Diario XMLDSig generation;
- a current Reporte-specific signature algorithm/profile decision;
- certificate validation/revocation/habilitation policy for reports;
- full signed `ReporteDiarioCFE.xsd` validation;
- durable report-signing timestamp persistence/replay;
- report sequence/reliquidation lifecycle orchestration;
- foreign-currency B-C27 threshold semantics;
- DGI `EFACRECEPCIONREPORTE` transport;
- report ACK / `Reporte Procesado` lifecycle;
- Sobre v05 packaging;
- DGI Testing certification or Production readiness.

## Next bounded gate

The next safe slice is the **Reporte Diario advanced XMLDSig boundary**. It must append the required final signature without borrowing the historical example's SHA-1 algorithms as normative policy, and then validate the resulting complete root against the untouched byte-pinned schema closure.

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**
