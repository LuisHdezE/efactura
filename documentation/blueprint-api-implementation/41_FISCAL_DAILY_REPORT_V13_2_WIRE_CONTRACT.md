# 41 — Reporte Diario v13.2 authoritative functional wire contract

## Decision

Before any Reporte Diario serializer is accepted, the repository pins the current DGI v13.2 functional wire constraints that can be established from authoritative material and records every unresolved byte-level/schema-level point as fail-closed.

This slice is deliberately **not** a Reporte Diario serializer. It does not manufacture XML element names, namespaces, signature algorithms, rounding behavior or omitted/zero rules from legacy code, examples from providers or memory.

## Authority and current publication status

The DGI `Documentos de interés` registry was rechecked on 2026-09-10.

It publishes for both Testing and Production:

- `Formato Reporte CFE v13 2.pdf` — **Publicado / Publicado**;
- `XSDs_FE_V1.44.2` — **Publicado / Publicado**.

The same registry publishes an official `Ejemplo de Reporte` XML.

Authoritative endpoints:

- registry: `https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`;
- Reporte Diario v13.2 functional format: `https://www.efactura.dgi.gub.uy/files/formato_reporte_cfe_v13_2-pdf?es=`;
- official report example: `https://www.efactura.dgi.gub.uy/files/ejemplo-de-reporte?es=`;
- current FE XSD archive listed by DGI: `https://www.efactura.dgi.gub.uy/files/xsds_fe_1_44_2-zip?es=`.

DGI remains the regulatory authority. Any mirror, generated source or historical implementation may only be corroborative evidence and cannot override the current DGI publication.

## v13.2 identity and delta

The current functional document identifies:

- version: `13.2`;
- publication/update date: `30/06/2023`.

Its v13.2 change log records two changes relative to v13.1:

1. the content section adds information about the fiscal exchange rate used for CFE not issued in national currency;
2. validation of **B-C27** is modified.

The current v13.2 B-C27 validation is only `C27 >= 0`.

The older v13.1 rule additionally coupled positive C27 to C24 and the UI threshold. That superseded validation must not be reintroduced into a v13.2 implementation.

## Report zones and conditionality

The functional report has three semantic zones:

1. **A — Carátula**;
2. **B — Resumen**;
3. **C — Firma Electrónica Avanzada**.

Carátula and advanced signature are mandatory. Resumen is conditional:

- if A-C7 `Cantidad de comprobantes` is zero, the Resumen zone may be omitted;
- if A-C7 is greater than zero, at least one Resumen occurrence is required;
- the Resumen zone is bounded to 1..1000 occurrences;
- for each type, the amount table grouped by document date + DGI branch + `Pagos por cuenta de terceros` is bounded to 1000 non-duplicated grouping rows.

The accepted internal reconciliation already groups emitted/non-rejected domestic CFE by exactly that tuple.

## Carátula semantic fields

The functional field sequence is pinned semantically as:

| Code | Meaning | Functional type / rule |
| --- | --- | --- |
| A-C2 | RUC emisor | NUM 12, DGI verifier rule |
| A-C3 | Fecha del Resumen | FECHA `AAAA-MM-DD`, valid range defined by DGI |
| A-C4 | ID emisor | optional sender-assigned identifier |
| A-C5 | Secuencia de envío | NUM 2; correction/reliquidation increments sequence |
| A-C6 | Fecha y hora de firma del envío | FECHA HORA |
| A-C7 | Cantidad de comprobantes | NUM 10; total used CFE plus emitted CFC per DGI rule |

The internal snapshot currently requires a positive sequence. Exact wire lexical representation and current XSD facet enforcement remain serializer concerns and must be validated against pinned schema bytes before emission.

## Resumen grouping fields

For the currently accepted domestic `101/102/103/111/112/113` boundary, current DGI v13.2 establishes:

| Code | Meaning | Wire constraint |
| --- | --- | --- |
| B-C1 | Tipo de comprobante | NUM 3; must be a DGI CFE/CFC code |
| B-C10 | Fecha de comprobante | `AAAA-MM-DD`; DGI date validation applies |
| B-C11 | Código Casa Principal / Sucursal | NUM 4; registered branch for issuer/date |
| B-C11.1 | Indicador Pagos por cuenta de terceros | value `1` when A-C19 is `1`; conditional |

Absence of A-C19 and exact value `1` are not interchangeable source facts. The accepted typed evidence preserves that distinction before the boolean grouping projection.

## Monetary wire fields

Current v13.2 exposes the following monetary/tax concepts in the amount table:

| Code | Meaning | Type / scale |
| --- | --- | --- |
| B-C12 | Total Monto — No gravado | NUM 17, 15 integer + 2 decimal |
| B-C13 | Total Monto — Exportación y asimiladas | NUM 17, 15 + 2 |
| B-C14 | Total Monto — Impuesto percibido | NUM 17, 15 + 2 |
| B-C15 | Total Monto — IVA en suspenso | NUM 17, 15 + 2 |
| B-C16 | Total Monto — IVA Tasa mínima | NUM 17, 15 + 2 |
| B-C17 | Total Monto — IVA Tasa básica | NUM 17, 15 + 2 |
| B-C18 | Total Monto — IVA Otra Tasa / IVA sobre fictos | NUM 17, 15 + 2 |
| B-C19 | Monto IVA — Tasa mínima | NUM 17, 15 + 2 |
| B-C20 | Monto IVA — Tasa básica | NUM 17, 15 + 2 |
| B-C21 | Monto IVA — Otra Tasa / IVA sobre fictos | NUM 17, 15 + 2 |
| B-C22 | Tasa Mínima IVA | NUM 6, 3 integer + 3 decimal |
| B-C23 | Tasa Básica IVA | NUM 6, 3 + 3 |
| B-C24 | Total Monto Total | NUM 17, 15 + 2; for the domestic boundary equals the DGI-defined sum |
| B-C25 | Total Monto Retenido / Percibido | NUM 17, 15 + 2 |
| B-C25.1 | Total Créditos Fiscales | NUM 17, 15 + 2 |

The internal `FiscalDailyReportSummaryBucket` currently preserves:

- non-taxed amount;
- minimum/basic taxable amounts;
- export amount;
- minimum/basic VAT amounts;
- aggregate VAT;
- net and total amount.

That is **not yet sufficient to serialize the complete v13.2 amount table without policy invention**. In particular, the current immutable evidence does not separately freeze authoritative values for B-C14, B-C15, B-C18, B-C21, B-C22, B-C23, B-C25 and B-C25.1.

Therefore this slice explicitly forbids a serializer from silently emitting zero/default values for those fields. Their omission-vs-zero semantics and sources must be resolved from current authoritative rules before deterministic XML generation is accepted.

The internal `NetAmount` and aggregate `VatAmount` are reconciliation conveniences, not direct substitutes for a wire field unless an explicit mapping proves that equivalence.

## Foreign-currency representation

Reporte Diario monetary information is expressed in UYU. The accepted PR #65/#66 evidence chain already preserves:

- source CFE currency;
- exact frozen fiscal conversion provenance/rate/date;
- UYU reporting currency;
- conversion evidence fingerprint;
- future-date reliquidation marker where applicable;
- converted monetary partitions without intermediate rounding.

v13.2 monetary fields are two-decimal wire fields. The functional format does not, by itself, justify inventing a rounding algorithm for exact converted values that carry more fractional digits.

Wire rounding/quantization therefore remains **UNRESOLVED / FAIL-CLOSED** until authoritative DGI evidence establishes the required rule.

## Counters

Current v13.2 pins these counting semantics:

- **B-C26 Used CFE count**: CFE received by DGI + CFE annulled because DGI rejected them + CFE annulled by the issuer; if none, report zero; equals the sum of used-number ranges;
- **B-C27 High-value ticket count**: NUM 10 and current v13.2 validation is only `C27 >= 0`;
- **B-C28 Annulled CFE count**: DGI-rejected + issuer-annulled; a CFE corrected with a credit note is not thereby annulled;
- **B-C29 Emitted CFE count**: emitted and not rejected by DGI; `C29 = C26 - C28`;
- **B-C30 Emitted CFC count**: contingency-only concept; for a CFE summary it is optional and, if present, must be zero.

For the accepted non-contingency domestic boundary, internal reconciliation already freezes C26/C28/C29-compatible populations. It does **not** yet freeze the source facts needed to compute B-C27, so B-C27 remains an explicit serializer/product gap rather than a fabricated zero.

## Number ranges

Used CFE ranges:

- maximum 50,000 repetitions;
- B-C41 series: ALFA 2, `A..ZZ`;
- B-C42 initial number: NUM 7, `1..9999999`;
- B-C43 final number: NUM 7, `>= C42` and `<= 9999999`.

Annulled CFE ranges:

- maximum 50,000 repetitions;
- B-C44 series: ALFA 2, `A..ZZ`;
- B-C45 initial number: NUM 7, `1..9999999`;
- B-C46 final number: NUM 7, `>= C45` and `<= 9999999`.

Numbering gaps are never inferred as annulments. The accepted internal evidence keeps that invariant.

## Advanced signature contract

Current v13.2 states:

- the public electronic certificate information is required to validate the signature;
- the certificate must be current, non-revoked and enabled for electronic invoicing when the signature is generated;
- the advanced electronic signature covers **the whole report**;
- the signature follows the **XML Digital Signature** standard.

This functional document does not justify hard-coding a specific digest/signature algorithm, canonicalization transform, reference URI, XML element position or certificate substructure for the new Reporte serializer.

Those byte-level details require current authoritative XML/XSD/technical evidence and remain fail-closed.

## Authoritative asset retrieval observation

The DGI registry exposes direct links for both the official `Ejemplo de Reporte` and `XSDs_FE_V1.44.2` archive. During this slice, the available retrieval channel resolved both official URLs but returned no usable body bytes.

Therefore this PR does **not** claim to have vendored or hashed the current report example/XSD bytes.

Historical/generated public implementations corroborate candidate names such as `Reporte`, `Caratula` and the CFE namespace, but they are deliberately excluded from the executable contract because they are not current authoritative byte evidence.

Before serializer implementation, the project must recover the current DGI report XML/XSD closure through a governed channel, preserve provenance, compute SHA-256 per file and pin the exact closure just as the signed-CFE v1.44.2 schema slice does.

## Executable functional pin

`FiscalDailyReportV13_2WireContract` centralizes only authoritative functional constants that are safe to execute before byte-level schema recovery:

- version `13.2`;
- reporting currency `UYU`;
- 1000 Resumen / amount-row cardinality bounds;
- 50,000 numbering-range repetition bound;
- monetary and VAT-rate digit/scale limits;
- branch, series and document-number bounds;
- A-C19 third-party indicator value `1`;
- non-negative B-C27 floor;
- official DGI source endpoints;
- XML Digital Signature as the only currently pinned signature-standard statement.

No constant in that descriptor is permission to serialize unresolved fields.

## Explicit non-scope

This slice does not add:

- Reporte Diario XML serialization;
- pinned report XSD bytes or schema compilation;
- exact root/child XML names or namespace constants without authoritative byte evidence;
- wire-level decimal rounding/quantization;
- synthesis/defaulting for B-C14/B-C15/B-C18/B-C21/B-C22/B-C23/B-C25/B-C25.1/B-C27;
- Reporte Diario XMLDSig generation;
- certificate revocation/habilitation checking;
- report persistence/replay/sequence orchestration;
- BCU quotation acquisition;
- DGI transport;
- Mensaje de Respuesta parsing/lifecycle;
- Sobre v05 packaging;
- Production readiness.

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Next gate

The next serializer gate is **authoritative byte-level asset pinning plus immutable wire-source completion**:

1. recover and hash the current DGI report example/XSD closure;
2. confirm exact XML root/namespace/order/cardinalities and XMLDSig schema position from those bytes;
3. resolve the currently missing domestic wire monetary concepts and B-C27 source evidence;
4. resolve authoritative two-decimal FX quantization/rounding behavior;
5. only then implement deterministic Reporte Diario XML generation and schema validation.
