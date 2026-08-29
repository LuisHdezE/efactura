# Taxation Foundation

## Status

Implementation slice for the versioned Taxation foundation that must precede Sales/POS tax calculation.

This slice does not claim complete Uruguayan tax-law automation. It establishes the authority, provenance, effective-date and resolution model required to add rules without scattering tax constants through Sales, WebApi or clients.

## Official baseline revalidated on 2026-08-29

Primary sources:

- DGI guidance: in principle goods/services are subject to the basic 22% IVA rate unless exempt or subject to the minimum 10% rate.
  - https://www.gub.uy/direccion-general-impositiva/comunicacion/publicaciones/son-bienes-servicios-gravados-tasa-basica-del-22
- Título 10 TO 2023, article 34: basic rate 22%, minimum rate 10%.
  - https://www.impo.com.uy/bases/todgi-2023/10-2024/10
- Decreto 220/998, article 99: basic 22%, minimum 10%.
  - https://www.impo.com.uy/bases/decretos/220-1998
- DGI CFE Format 25.2, field B-C4: current billing indicators include 1 Exempt, 2 Minimum Rate, 3 Basic Rate, 4 Other Rate/IVA on fictitious basis, 10 Export/assimilated, 11 perceived tax and 12 IVA suspended.
  - https://www.efactura.dgi.gub.uy/files/formato_cfe_v25-2-pdf?es=
- DGI current electronic-payment benefit guidance demonstrates time-varying contextual tax rules: for specified services the benefit moves from a 9-point IVA reduction to 5 points from 2026-10-01.
  - https://www.gub.uy/direccion-general-impositiva/comunicacion/publicaciones/reduccion-9-puntos-iva-determinados-servicios-siempre-sean-abonados
- Updated Decreto 220/998 article 34 remains the authority boundary for export-of-services qualification.
  - https://www.impo.com.uy/bases/decretos/220-1998/34

## Implemented domain concepts

`TaxProfile` is a versioned, effective-dated tax assignment definition. It carries:

- stable ID/code/name;
- optional organization scope, with null representing a system reference profile;
- treatment kind;
- percentage rate only where a percentage rate is semantically applicable;
- DGI CFE billing indicator;
- effective-from/effective-to;
- rule version;
- source authority/reference/URI;
- CFE specification version used for the indicator mapping;
- verification timestamp;
- active/version metadata.

Structural validation prevents invalid combinations, for example Basic VAT with CFE indicator 2 or Export treatment represented as an ordinary percentage rate.

## Initial system profiles

The migration materializes only the two safely general base-rate profiles whose legal rate/effective date was revalidated:

- `UY-IVA-BASIC-22`: 22%, CFE indicator 3, effective 2007-07-01;
- `UY-IVA-MINIMUM-10`: 10%, CFE indicator 2, effective 2007-07-01.

The migration deliberately does **not** create a generic assignable `EXEMPT`, `EXPORT`, `IVA_SUSPENDED` or `OTHER_RATE` profile. Those treatments depend on a specific legal/applicability rule and must enter through a later governed rule/configuration slice with provenance.

## Tax treatment resolution

`TaxTreatmentResolver` separates:

1. base catalog tax profile;
2. transaction date;
3. jurisdiction (`DOMESTIC_URUGUAY`, `EXPORT_GOODS`, `EXPORT_SERVICES`);
4. export-service qualification;
5. the rule reference proving that qualification.

Domestic operations can resolve from the effective base profile.

Export goods remain `RequiresRuleQualification` until the specialized export-goods rule slice is accepted.

An export-service candidate remains unresolved while Article 34 qualification is unknown. Even when a caller says it qualifies, the resolver refuses a final Export/assimilated result unless a concrete rule reference is supplied. This prevents `foreignCustomer == true` or a client boolean from becoming fiscal authority.

## Public API

This slice implements only the already-approved operation:

`API-CAT-009 listTaxProfiles`

`GET /api/v1/tax-profiles`

Permission: `catalog.read`.

The response exposes usable profile metadata plus rule/provenance/effective-date data. There is no public POST/PATCH/PUT/DELETE tax-profile endpoint in this slice.

## Persistence and portability

Table: `v1_tax_profiles`.

The same EF Core migration and repository are exercised against PostgreSQL and MySQL. No mutation SQL/Dapper path is introduced.

## Deliberate next boundaries

Still required before Sales tax calculation can be considered complete:

- controlled/auditable rule-profile provisioning and supersession instead of generic CRUD;
- exact exemption profiles by supported goods/services;
- exact Article 34 export-service rule catalog and supporting-fact/evidence requirements;
- export-goods tax/logistics rule slice;
- contextual tax-benefit/discount rule model, including payment-method/date-dependent regimes;
- item-to-tax-profile assignment after the selected profile is validated as effective and usable;
- Sale validation/fiscal preview consuming `TaxTreatmentResolver` and later the CFE-family selector.

Sales/POS must never hard-code 22, 10, export, exemption or benefit percentages independently of Taxation.
