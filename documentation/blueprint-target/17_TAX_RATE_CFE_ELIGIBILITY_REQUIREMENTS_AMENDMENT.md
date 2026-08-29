# Target Requirements Amendment — VAT Rate Resolution and CFE Eligibility

Status: PROPOSED_FROM_IMPLEMENTATION_EVIDENCE / PENDING_RECONCILIATION_IN_CANONICAL_REQUIREMENTS

This amendment extends the accepted target requirements after current DGI/IMPO revalidation performed on 2026-08-29. It must be reconciled into the canonical Requirements baseline after human acceptance of the implementation slice.

## Functional requirements

### FR-096 — Authoritative VAT rate resolution
The system SHALL resolve the VAT rate only after tax treatment has been resolved and SHALL preserve the legal/rule evidence used for the decision.

### FR-097 — Current general rates
For Release 1 dates within the verified current rule-pack support boundary, the source-controlled Uruguay VAT rate pack SHALL represent the current basic rate as 22% and minimum rate as 10%, with provenance to T.O. 2023 Título 10 Art. 34.

### FR-098 — Domestic profile consistency
For a domestic taxed operation, the system SHALL require an effective TaxProfile and SHALL reject automatic resolution when the profile treatment/rate conflicts with the authoritative rate rule.

### FR-099 — Exemption rule evidence
A domestic profile with a zero amount or an `EXEMPT` label SHALL NOT by itself authorize no VAT. A specific effective exemption rule SHALL be required before automatic exemption is permitted.

### FR-100 — Export/outside-scope distinction
For operations already classified as export of goods, export of services, or outside VAT territorial scope, the system SHALL represent `NO_VAT_DUE` separately from an ordinary zero-percent VAT rate.

### FR-101 — CFE receiver eligibility
The system SHALL determine CFE receiver eligibility from typed fiscal identity and issuing-country rules. It SHALL NOT use nationality or a generic foreign flag as the authority for CFE eligibility.

### FR-102 — e-Ticket identification threshold
For CFE Format 25.2 Release-1 operations, e-Ticket receiver identification SHALL be required when the net amount exceeds 5,000 UI, or regardless of amount when retentions/perceptions are documented. The conversion to UI SHALL use the applicable official quotation policy when implemented.

### FR-103 — CFE candidate preparation before selection
The system SHALL prepare legally possible CFE families before any final selector runs. Candidate preparation SHALL NOT allocate numbering, CAE, build/sign XML, or send a CFE.

## Business rules

### BR-025
`DOMESTIC` does not mean 22% automatically. The item/operation must have a supported, effective TaxProfile or another explicit tax rule.

### BR-026
`VAT_BASIC` and `VAT_MINIMUM` are canonical Release-1 profile treatment codes and must match 22% and 10% respectively within the current verified rate pack.

### BR-027
`VAT_EXEMPT` is not self-authorizing. Exemption requires separately modeled legal evidence.

### BR-028
A computational zero used for export/outside-scope calculations is not semantically equivalent to a generic legal 0% VAT rate.

### BR-029
Ordinary e-Factura requires a valid Uruguayan RUC receiver identity under the current CFE format rules.

### BR-030
For receiver identity type 6 (DNI), issuing country is restricted to Argentina, Brazil, Chile, or Paraguay. Types 1/2/3 are Uruguayan identities; types 4/5/7 follow the corresponding CFE country rules.

### BR-031
Export-service CFE strategy remains fail-closed until the latest DGI FAQ version is revalidated. The indexed v27 evidence may inform candidate preparation but SHALL NOT by itself make final selection executable after the portal announced a newer FAQ on 2026-06-25.

## Acceptance criteria

### AC-049
Given `DOMESTIC` and an effective `VAT_BASIC` TaxProfile at 22%, rate resolution returns VAT due at 22% with profile and Art. 34 provenance.

### AC-050
Given `DOMESTIC` and an effective `VAT_MINIMUM` TaxProfile at 10%, rate resolution returns VAT due at 10% with provenance.

### AC-051
Given `VAT_BASIC` with a stored rate different from 22%, the resolver returns `REQUIRES_REVIEW` and does not silently repair or apply the profile.

### AC-052
Given `VAT_EXEMPT` without a specifically supported exemption rule, the resolver returns `REQUIRES_REVIEW`.

### AC-053
Given a resolved export of goods or services, VAT rate resolution returns `NO_VAT_DUE`; any numeric zero is identified as computational, not as a generic zero VAT rate.

### AC-054
Given a domestic taxpayer invoice receiver with valid Uruguayan RUC, e-Factura is an eligible candidate.

### AC-055
Given a domestic taxpayer invoice receiver without Uruguayan RUC, ordinary e-Factura is ineligible.

### AC-056
Given a consumer-final e-Ticket over 5,000 UI with no compatible receiver identity, eligibility returns `REQUIRES_REVIEW`.

### AC-057
Given an e-Ticket requiring identification and DNI type 6 issued in Argentina, the identity is format-compatible.

### AC-058
Given an e-Ticket requiring identification and DNI type 6 issued in the United States, the identity is not format-compatible.

### AC-059
Given a resolved export of goods and export operation intent, e-Factura Exportación is prepared as a candidate without issuing a document.

### AC-060
Given a resolved export of services, the system prepares the export-combo candidate and the usual-CFE candidate according to RUC status, but remains `REQUIRES_REVIEW` while latest-FAQ currentness is unresolved.

## Sources revalidated 2026-08-29

- T.O. 2023 Título 10 Art. 5: https://www.impo.com.uy/bases/todgi2023/101-2024/5_T10
- T.O. 2023 Título 10 Art. 34: https://www.impo.com.uy/bases/todgi2023/101-2024/34_T10
- T.O. 2023 Título 10 Art. 36: https://www.impo.com.uy/bases/todgi2023/101-2024/36_T10
- T.O. 2023 Título 10 Art. 38: https://www.impo.com.uy/bases/todgi2023/101-2024/38_T10
- DGI guidance on basic rate: https://www.gub.uy/direccion-general-impositiva/comunicacion/publicaciones/son-bienes-servicios-gravados-tasa-basica-del-22
- DGI CFE Format 25.2: https://www.efactura.dgi.gub.uy/files/formato_cfe_v25-2-pdf?es=
- DGI indexed FAQ: https://www.efactura.dgi.gub.uy/files/descargar-todas-las-preguntas-frecuentes?es=
- DGI portal news indicating newer FAQ on 2026-06-25: https://www.efactura.dgi.gub.uy/
