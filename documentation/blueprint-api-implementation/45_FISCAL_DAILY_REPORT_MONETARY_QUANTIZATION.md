# 45 — Reporte Diario v13.2 monetary quantization

## Decision

This increment closes the two-decimal monetary quantization gap at the Reporte Diario **wire projection** boundary without changing the lossless reconciliation evidence introduced in PR #65/#66.

Formal DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**

## Authority reviewed 2026-09-11

### DGI — Formato Reporte CFE v13.2

Official source:

`https://www.efactura.dgi.gub.uy/files/formato_reporte_cfe_v13_2-pdf?es=`

The amount-table monetary concepts use `NUM 17` with **15 integer digits and 2 decimal digits**. The functional validations also define, among others:

- C19 from C16 and C22;
- C20 from C17 and C23;
- C24 as the sum of C12 through C21.

### DGI — Instructivo de ingreso al régimen CFE

Official index:

`https://www.efactura.dgi.gub.uy/principal/factura-electronica-informacion-general-instructivos`

The current DGI homologation instruction states that calculation results use **redondeo matemático con 2 decimales**.

The project had already pinned that rule for CFE arithmetic in `UruguayCfe25_2ArithmeticCatalog` and implemented positive monetary midpoint behavior with `MidpointRounding.AwayFromZero`. This increment applies the same fiscal rounding semantics to Reporte Diario wire monetary values instead of introducing a second incompatible convention.

## Precision boundary

Foreign-currency source composition remains unchanged:

1. the authoritative fiscal/BCU exchange rate is frozen with all available decimal digits;
2. CFE monetary partitions are converted to UYU without intermediate rounding;
3. reconciliation snapshots retain those exact converted decimal values;
4. only the final Reporte Diario wire amount concepts are quantized to two decimals.

Therefore source evidence is not rewritten merely to satisfy an XML numeric shape.

## Wire quantizer

`FiscalDailyReportMonetaryQuantizer`:

- uses scale `2` from `FiscalDailyReportV13_2WireContract`;
- uses `MidpointRounding.AwayFromZero` for the accepted non-negative monetary domain, implementing mathematical half-up midpoint behavior;
- rejects negative Release-1 monetary concepts;
- rejects values above `999999999999999.99`, the maximum non-negative value representable by NUM 17 with 15 integer digits and 2 decimals.

## v13.2 algebra after quantization

The wire-readiness projector quantizes C12/C13/C16/C17/C19/C20 only after reconciliation.

It then:

- verifies quantized C19 against quantized C16 and B-C22;
- verifies quantized C20 against quantized C17 and B-C23;
- recomposes C24 from the already quantized C12..C21 concepts rather than independently rounding the source CFE total.

This is important because separately rounding a converted source total can differ by one cent from the sum of separately quantized report concepts. v13.2 defines C24 as the sum of C12..C21, so the wire projection follows that explicit invariant.

If a converted source VAT partition cannot satisfy the v13.2 taxable-amount/rate relationship after the mandated wire quantization, the projector fails closed with a typed VAT-quantization mismatch instead of silently changing fiscal evidence.

## What this unlocks

Rows whose exact UYU reconciliation values carry more than two decimal places can now become deterministic wire rows when the resulting values satisfy the v13.2 algebraic validations.

The previous blanket failure `fiscal.daily_report.wire.quantization_required` is removed from the projector.

This does **not** yet add:

- Reporte Diario XML serialization;
- runtime validation against `ReporteDiarioCFE.xsd`;
- XMLDSig generation or report signing orchestration;
- report persistence/replay or sequence/reliquidation lifecycle;
- live BCU acquisition;
- foreign-currency B-C27 threshold policy;
- DGI transport, ACK lifecycle or Sobre packaging.

## Next bounded gate

The next safe increment is the deterministic **unsigned Reporte Diario v13.2 XML serializer**, mapped only from the already validated semantic wire projection and validated locally against the pinned XSD closure before any signature or transport work.

Formal traditional DGI Testing readiness remains exactly:

**BLOCKED BY MISSING PRODUCT CAPABILITIES**
