# 43 — Reporte Diario v13.2 wire-source readiness

> Supersession note (2026-09-11): the quantization gap described in this historical slice was closed by implementation record `45_FISCAL_DAILY_REPORT_MONETARY_QUANTIZATION.md`. Lossless FX/reconciliation precision remains unchanged; mathematical two-decimal rounding is applied only at the wire projection boundary.

## Decision

This slice introduces the deterministic semantic boundary that must be satisfied **before** a Reporte Diario XML serializer may exist.

It does not generate XML. It converts the accepted reconciliation snapshot plus explicitly frozen wire-source evidence into a tamper-evident semantic projection. Any value that would require an unpinned DGI policy remains fail-closed.

Formal DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Authority recheck

On 2026-09-11 the DGI `Documentos de interés` registry still publishes, for both Testing and Production:

- `Formato Reporte CFE v13 2.pdf` — **Publicado**;
- `XSDs_FE_V1.44.2` — **Publicado**;
- `Formato CFE v25.2.pdf` — **Publicado**;
- `Formato Mensajes Respuesta v19.pdf` — **Publicado**.

The registry also continues to expose the official `Ejemplo de Reporte` XML.

Official registry:
`https://www.efactura.dgi.gub.uy/principal/ampliacion_de_contenido/documentos-de-interes?es=`

The exact Reporte schema/example bytes and their provenance were pinned in the previous bounded slice.

## Why XML generation is still deferred

The accepted reconciliation snapshot was intentionally built before the complete wire contract. It already freezes:

- CFE type, fiscal date, branch and A-C19 grouping semantics;
- UYU report partitions;
- C26/C28/C29-compatible populations;
- used and annulled number ranges;
- DGI reception outcome provenance;
- foreign-currency conversion provenance.

It did not yet expose all source facts required to construct the v13.2 amount table safely. The serializer therefore remains prohibited from deciding missing values on its own.

This slice closes the parts that can be proven from already accepted immutable evidence and keeps the unresolved parts explicit.

## VAT-rate evidence for B-C22 / B-C23

`FiscalDailyReportVatRateEvidence` captures minimum/basic VAT rates from the immutable fiscal line evidence that was frozen before the CFE was built.

The rate is not hard-coded to a statutory percentage by the report layer. It is derived from the exact signed-CFE content snapshot and is bound to:

- fiscal document id;
- signed artifact id;
- signing evidence id;
- CFE type / series / number;
- fiscal-content fingerprint;
- signed-content hash;
- signed-CFE identity fingerprint.

The evidence accepts at most one applied rate per minimum/basic bucket and enforces the v13.2 NUM 6 shape without rounding.

## B-C27 high-value e-Ticket evidence

`FiscalDailyReportHighValueCounterEvidence` makes B-C27 an explicit source fact instead of a default zero.

For the accepted domestic boundary:

- 101/102/103 require explicit B-C27 evidence when their type is present in the report consumption set;
- the value must be non-negative;
- it cannot exceed B-C29 emitted/not-rejected population;
- 111/112/113 do not acquire a positive B-C27 value.

This slice does **not** yet calculate the count from sale/UI quotation history. The production capture path for that source fact is a separate prerequisite. The report projector consumes only evidence that has already been frozen.

## Release-1 zero concepts are profile facts, not serializer defaults

The current accepted fiscal arithmetic profile supports:

- non-taxed amount;
- export amount;
- minimum VAT taxable amount + VAT amount;
- basic VAT taxable amount + VAT amount.

It does not support creating CFE arithmetic for:

- perceived-tax amount B-C14;
- VAT in suspense B-C15;
- other VAT rate / fictos B-C18 and B-C21;
- retained/perceived amount B-C25;
- fiscal credit amount B-C25.1.

Therefore the semantic projection `release1-minimum-basic-export-only` freezes those unsupported concepts as zero **before XML serialization**. This is not permission for an XML serializer to decide zero-vs-omission. Wire omission/presence remains governed by the functional contract plus the pinned XSD.

If the product later gains any of those tax treatments, this Release-1 projector must reject or be replaced/extended together with new immutable source evidence. The zeros must never silently survive a product-capability expansion.

## Foreign currency and two-decimal wire representation

PR #65/#66 preserve exact fiscal exchange-rate provenance and exact UYU multiplication without intermediate rounding.

Reporte Diario monetary wire fields allow two decimal places. No authoritative DGI rule has been pinned that authorizes this project to choose a particular rounding/quantization policy for exact converted UYU values carrying additional non-zero decimal digits.

Therefore `FiscalDailyReportWireReadinessProjector` behaves as follows:

- values exactly representable with at most two meaningful decimal digits are accepted, even if their `decimal` representation carries trailing zero scale;
- values requiring a numeric change to fit two decimals fail with `fiscal.daily_report.wire.quantization_required`;
- no `decimal.Round`, midpoint mode or truncation policy is exposed as fiscal behavior.

This preserves lossless evidence and prevents a serializer from smuggling in a financial rounding decision.

## Deterministic semantic projection

`FiscalDailyReportWireReadinessProjector` produces:

- amount rows keyed by CFE type + fiscal date + DGI branch + third-party-payment indicator;
- B-C12..B-C25.1 semantic values for the Release-1 profile;
- B-C22/B-C23 rates from signed-CFE-bound evidence;
- B-C26/B-C27/B-C28/B-C29 counters;
- used and annulled number ranges;
- source reconciliation fingerprint;
- a wire-evidence fingerprint binding VAT-rate and B-C27 provenance;
- a final deterministic projection fingerprint.

The projection is intentionally independent of XML element construction, namespace emission, XMLDSig and transport.

## Fail-closed conditions introduced here

Representative guards include:

- `fiscal.daily_report.wire.vat_rate_evidence_required`;
- `fiscal.daily_report.wire.vat_rate_evidence_unexpected`;
- `fiscal.daily_report.wire.minimum_vat_rate_required`;
- `fiscal.daily_report.wire.basic_vat_rate_required`;
- `fiscal.daily_report.wire.high_value_evidence_required`;
- `fiscal.daily_report.wire.high_value_count_exceeds_emitted`;
- `fiscal.daily_report.wire.quantization_required`.

The absence of evidence is never converted into a plausible fiscal value.

## Explicit non-scope

This slice does not add:

- Reporte Diario XML generation;
- runtime Reporte XSD validation;
- XMLDSig generation or crypto-policy selection;
- report signing timestamp orchestration;
- report persistence/replay;
- report sequence lifecycle / reliquidation orchestration;
- automatic B-C27 calculation from UI quotation history;
- a DGI rounding/quantization rule;
- `EFACRECEPCIONREPORTE` transport;
- response/ACK lifecycle;
- Sobre v05 packaging;
- Testing certification or Production readiness.

## Next gates

Before deterministic XML generation can be accepted:

1. implement durable production capture/composition for B-C27 from authoritative 5,000-UI source facts;
2. pin an authoritative Reporte Diario monetary quantization rule for values not exactly representable with two decimals, or retain a permanently fail-closed restricted subset;
3. map the semantic projection to the byte-pinned `ReporteDiarioCFE.xsd` with exact order/conditional omission rules;
4. validate unsigned/structural report XML locally against the pinned closure;
5. handle advanced XMLDSig in a separate gated slice.

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**
