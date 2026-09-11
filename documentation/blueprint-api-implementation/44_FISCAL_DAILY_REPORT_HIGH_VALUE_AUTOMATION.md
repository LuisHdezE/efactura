# 44 — Fiscal Daily Report high-value automation

## Scope

This increment closes the manual-count gap for Reporte Diario v13.2 field B-C27 within the currently accepted Release-1 boundary.

It does **not** generate XML, sign a Reporte Diario, persist/replay report artifacts, submit to DGI, package a Sobre, or define a new monetary quantization rule.

## Authoritative rule evidence reviewed 2026-09-11

### DGI — Reporte Diario v13.2

Source:

`https://www.efactura.dgi.gub.uy/files/formato_reporte_cfe_v13_2-pdf?es=`

B-C27 represents the quantity of CFE/CFC whose amounts are greater than the UI cap, excluding VAT, emitted in the reporting period. In the accepted domestic boundary this applies to e-Ticket families 101/102/103. The current v13.2 validation is `C27 >= 0`.

### DGI — Preguntas Frecuentes CFE

Current download endpoint:

`https://www.efactura.dgi.gub.uy/files/descargar-todas-las-preguntas-frecuentes?es=`

The DGI rule states that the e-Ticket identification threshold was reduced to **5.000 UI** for documents issued from 2022-11-01 and that the UI quotation to use is the quotation corresponding to **31/12 of the previous year**.

### DGI — Definiciones funcionales

Source reviewed through the current DGI documentation index:

`https://www.efactura.dgi.gub.uy/files/descripcion-e-factura-archivo-pdf-263-kb?es=`

The functional definition also states that e-Tickets and their correction notes whose net amount is greater than **5.000 UI** must be sent individually, considering billable concepts and the UI quotation corresponding to 31/12 of the previous year.

### BCU — Cotizaciones

Source:

`https://www.bcu.gub.uy/Estadisticas-e-Indicadores/Paginas/Cotizaciones.aspx`

BCU publishes current and historical quotations, including **UNIDAD INDEXADA**. The domain does not perform a live BCU request in this increment. Instead, the exact quotation used must be frozen as immutable evidence together with an HTTPS source URI and a source-artifact fingerprint.

## Shared threshold policy

`UruguayCfeHighValueThresholdPolicy` is now the single current threshold definition:

- `CurrentThresholdUi = 5_000m`
- `CurrentThresholdEffectiveFrom = 2022-11-01`

`UruguayCfe25_2EligibilityRulePackProvider` consumes the same domain constant. The architecture guard rejects reintroduction of the previous standalone `5000m` literal at that boundary.

This avoids divergent eligibility and reporting thresholds.

## Annual UI quotation evidence

`FiscalDailyReportAnnualUiQuoteEvidence` freezes:

- applicable report year;
- quote date;
- UYU per UI;
- 5.000 UI threshold;
- exact UYU threshold (`5.000 × UYU/UI`);
- HTTPS source URI;
- source-artifact fingerprint;
- deterministic evidence fingerprint.

The evidence is accepted only when:

`QuoteDate == 31/12/(ReportYear - 1)`

A quotation from 30/12, 01/01, the report date itself, or any other date fails closed.

Historical pre-2022-11-01 threshold handling remains outside Release 1.

## Automatic B-C27 composition

`FiscalDailyReportHighValueCounterComposer` receives:

1. an immutable `FiscalDailyReportSnapshot`;
2. one valid annual UI quotation evidence record.

For each emitted/not-rejected e-Ticket family present in the snapshot it evaluates every CFE individually.

For the currently supported UYU path:

`highValue = document.NetAmount > annualUiQuote.ThresholdUyu`

The comparison is **strictly greater**.

Therefore, with a hypothetical quotation of 6 UYU/UI:

- threshold = `5.000 × 6 = 30.000 UYU`;
- net amount `30.000 UYU` does **not** increment B-C27;
- net amount `30.006 UYU` does increment B-C27.

The resulting `FiscalDailyReportHighValueCounterEvidence` source fingerprint binds:

- reconciliation fingerprint;
- annual UI quote evidence fingerprint;
- CFE type;
- ordered emitted document evidence fingerprints.

The same snapshot plus the same annual quotation therefore produces the same B-C27 evidence.

## Currency boundary

Automatic B-C27 calculation in this increment is intentionally limited to CFE whose **original currency is UYU**.

A foreign-currency or UYI source CFE fails with:

`fiscal.daily_report.wire.high_value_foreign_currency_policy_required`

Reason: the reviewed DGI material clearly pins the 5.000 UI threshold and the prior-31/12 UI quotation, but this increment does not have sufficiently explicit authoritative evidence for which foreign-currency conversion semantics must be used specifically for the B-C27 threshold comparison.

The existing Reporte Diario monetary conversion evidence cannot silently be reused as if that automatically proved the threshold-conversion rule.

## Quantization remains fail-closed

This increment does not invent a Reporte Diario rounding rule.

The accepted wire-readiness projection continues to reject UYU amounts that cannot be represented exactly with the pinned two-decimal wire restriction without numerical modification:

`fiscal.daily_report.wire.quantization_required`

This is deliberate. `NUM ... 2` constrains representation; it does not by itself establish a rounding algorithm.

## Serializer gate after this increment

This slice removes the manual B-C27 input for the original-UYU path. It does **not** make every Reporte Diario serializable.

A later serializer may safely operate only on a projection that has already passed all current fail-closed guards, including:

- signed-CFE-bound VAT-rate evidence;
- automatic or otherwise authoritative B-C27 evidence;
- exact monetary representability at the wire scale;
- all existing snapshot integrity and cardinality rules.

Foreign-currency B-C27 policy and any authoritative quantization rule remain separate unresolved capabilities.

## DGI Testing readiness

Formal DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**
