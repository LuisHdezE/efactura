# API Implementation 11 — Tax Rate Resolution and CFE Eligibility Preparation

Status: IMPLEMENTED_ON_BRANCH / PENDING_HUMAN_ACCEPTANCE

Branch: `blueprint/tax-rate-cfe-eligibility-v1`

## Purpose

Close the boundary between the accepted tax-treatment decision engine and future Sales/Fiscal execution without allowing either tax percentages or CFE family selection to be guessed by clients.

The implemented sequence is:

`TaxTreatmentDecision -> TaxRateResolution -> CfeEligibilityResult -> future FiscalDocumentSelector`

The last component is deliberately not implemented in this slice.

## Current regulatory evidence reviewed on 2026-08-29

1. T.O. 2023 Título 10 Art. 34 establishes the current general IVA rates: basic 22% and minimum 10%.
   - https://www.impo.com.uy/bases/todgi2023/101-2024/34_T10
2. DGI guidance states that, in principle, goods/services are taxed at 22% except those subject to the 10% minimum rate or exempted.
   - https://www.gub.uy/direccion-general-impositiva/comunicacion/publicaciones/son-bienes-servicios-gravados-tasa-basica-del-22
3. T.O. 2023 Título 10 Art. 5 establishes that exports of goods, and export services determined by the Executive, are not taxed under the territoriality rule.
   - https://www.impo.com.uy/bases/todgi2023/101-2024/5_T10
4. CFE Format 25.2 is the current Release-1 format and was enabled in Production from 2026-06-30.
   - https://www.efactura.dgi.gub.uy/files/formato_cfe_v25-2-pdf?es=
5. CFE 25.2 A-C60/A-C61/A-C62/A-C62.1 requires Uruguayan RUC for ordinary e-Factura and supports typed foreign identities for e-Ticket where identification is required.
6. CFE 25.2 Table E keeps the e-Ticket receiver-identification threshold at more than 5,000 UI from 2022-11-01; identification is also required regardless of amount when retentions/perceptions are documented.
7. The indexed DGI FAQ v27 states that use of the export-invoice combo for export services is optional and that usual CFE may be used according to RUC status. However, the DGI portal announced a newer FAQ on 2026-06-25. The indexed artifact found during review still identifies itself as v27. Therefore this strategy is evidence for candidate preparation only and remains `CURRENTNESS_REQUIRES_REVALIDATION` before executable CFE selection.
   - https://www.efactura.dgi.gub.uy/files/descargar-todas-las-preguntas-frecuentes?es=

## VAT rate resolver

Release 1 supports source-controlled authoritative rate rules:

- `VAT_BASIC` -> 22%
- `VAT_MINIMUM` -> 10%

A domestic operation requires an effective `TaxProfile`. The profile treatment code and stored percentage must agree with the source-controlled current rule. A `VAT_BASIC` profile carrying 10% is not silently corrected; resolution fails closed to review.

`VAT_EXEMPT` does not automatically mean 0%. Exemptions require their own effective legal rule before automation. Until those exemption families are modeled, such a profile yields `REQUIRES_REVIEW`.

For `EXPORT_GOODS`, `EXPORT_SERVICES`, and `OUTSIDE_VAT_SCOPE`, the resolver returns `NO_VAT_DUE`. An `AppliedRatePercent` of `0` is used only as a computational value; it is explicitly not represented as a generic legal "0% VAT rate".

## CFE eligibility preparation

This slice prepares candidate families but does not issue, number, sign, send, or persist a fiscal document.

### Domestic taxpayer operation

- Candidate: e-Factura (111).
- Requires a typed Uruguayan RUC identity.
- A foreign-only identity cannot force ordinary e-Factura.

### Domestic consumer-final operation

- Candidate: e-Ticket (101).
- Receiver identification is required when net amount exceeds 5,000 UI or retentions/perceptions are present.
- DNI type 6 is accepted only with issuing country AR, BR, CL, or PY.
- Types 1/2/3 require UY; types 4/5/7 support the format country rules.

The API will receive or derive the net amount expressed in UI before this eligibility rule. Conversion from UYU/other currency to UI is outside this policy and must use the applicable official quotation source when implemented.

### Export of goods

- Candidate: e-Factura Exportación (121).
- The tax-treatment engine must already have resolved `EXPORT_GOODS`.
- This slice does not yet build XML, allocate CAE, or emit the document.

### Export of services

Candidates are prepared from the available DGI evidence:

- e-Factura Exportación; and
- ordinary e-Factura when there is Uruguayan RUC, otherwise e-Ticket.

The result remains `REQUIRES_REVIEW` until the latest FAQ strategy is revalidated. No candidate is selected automatically in this slice.

## Clean Architecture boundary

- Domain owns `TaxRateResolution`, `CfeEligibilityPolicy`, and pure fiscal/tax concepts.
- Application owns rule-pack providers and orchestration use cases.
- Infrastructure only wires dependencies and continues to implement repository ports.
- WebApi has no new public endpoint in this slice.
- No EF Core, PostgreSQL, MySQL, Dapper, HTTP, CAE, XML-signing, or DGI transport dependency enters Domain/Application decision code.

## Explicit non-goals

Not implemented here:

- Article 36 item-by-item minimum-rate qualification engine;
- Article 38 and other exemption catalogs;
- historical pre-Release-1 VAT rate packs;
- UI quotation conversion for the 5,000 UI threshold;
- final CFE selection policy;
- CAE allocation;
- CFE XML/signing/transport;
- Sales/POS integration.

Sales remains blocked from fiscal confirmation until tax treatment + rate resolution + CFE eligibility can all resolve without `REQUIRES_REVIEW` for the operation being confirmed.
