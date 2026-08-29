# Release-1 Uruguay Tax Rule Pack + Article 34 Numeral 11

## Status

Implementation slice prepared for human review on 2026-08-29. This artifact records what is executable and, equally importantly, what is not yet authorized.

## Official sources used

1. IMPO, T.O. 2023 Título 10, Artículo 5 - Territorialidad:
   https://www.impo.com.uy/bases/todgi2023/101-2024/5_T10
2. IMPO, Decreto 220/998 actualizado, Artículo 34 - Exportación de servicios:
   https://www.impo.com.uy/bases/decretos/220-1998/34
3. DGI, IVA Servicios Personales, updated 2026-06-10:
   https://www.gub.uy/direccion-general-impositiva/comunicacion/publicaciones/iva-servicios-personales
4. DGI, Servicios comprendidos en el IVA servicios personales:
   https://www.gub.uy/direccion-general-impositiva/comunicacion/publicaciones/servicios-comprendidos-iva-servicios-personales

The DGI guidance explicitly states that services supplied to the exterior are exports of services only when the conditions of the applicable Article 34 numeral are met. Otherwise the operation remains subject to the corresponding non-export VAT treatment.

## Release-1 rule pack

`UruguayRelease1TaxTreatmentRulePackProvider` is the first source-controlled production rule pack.

Stable identifier:

`UY-IVA-R1-2026.08.29`

It currently contains the core Article 5 territoriality rule and the export-of-goods rule evidence. It does not contain tax rates and does not select a CFE family.

### Support boundary

Release 1 intentionally supports this consolidated rule evidence only for operation dates on or after `2024-05-16`, the publication date of the current T.O. 2023 approved by Decreto 101/024.

This is a **software verification/support boundary**, not a claim that the underlying legal rules first became effective on that date. Historical/backdated operations before the boundary require an explicitly verified historical pack and fail closed instead of silently applying the current pack retroactively.

## Article 34 numeral 11 scope

Release 1 supports these categories:

- 11(a): advisory/technical services related to activities developed, assets located or rights economically used outside Uruguay;
- 11(b): specific/custom software design, development or implementation produced by prior user order;
- 11(c): software use licence;
- 11(d): full assignment of software use/exploitation rights.

For the exterior-recipient path, the evaluator requires explicit regulated facts rather than deriving them from master-data shortcuts:

- `RecipientIsPersonAbroad`;
- `ExclusiveUseAbroad`;
- for 11(a), `ForeignEconomicRelation`.

For 11(b), 11(c) and 11(d), the current Article 34 text also permits the separately modeled free-zone path when services are supplied from non-free national territory to persons/entities installed in free zones. That path requires:

- `RecipientInstalledInFreeZone`;
- `ProviderFromNonFreeNationalTerritory`.

## Deliberate non-inference rules

The evaluator does not infer any of the following:

- foreign status from `ResidenceCountry != UY`;
- person-abroad legal status from nationality;
- exclusive use abroad from a destination country alone;
- export treatment from possession or absence of a Uruguayan RUC;
- VAT rate from treatment classification;
- CFE family from treatment classification.

Unknown evidence returns `InsufficientEvidence`; unsupported Article 34 families return `UnsupportedScenario`. The outer `TaxTreatmentDecisionEngine` consequently returns `REQUIRES_REVIEW` rather than granting export treatment.

## Architecture

The regulatory policy implementation remains inside Application and depends only inward on Domain concepts. No EF Core, SQL provider, ASP.NET, DGI SDK, CFE selector or WebApi dependency exists in the evaluator.

Dependency direction:

`WebApi -> Application -> Domain`

`Infrastructure -> Application/Domain`

The rule pack/evaluator are registered through the existing v1 composition root but their business logic remains framework-free.

## What is intentionally still unsupported

- Article 34 numerals other than 11;
- detailed evidence acquisition/verification workflow;
- tax-rate resolver beyond the existing TaxProfile boundary;
- CFE selector;
- e-Ticket/e-Factura/export-family strategy;
- production UI/API for modifying regulatory rules;
- historical rule packs before the verified support boundary;
- final currentness reconciliation of the export-services CFE-strategy FAQ before the CFE selector is implemented.

Sales/POS may consume this engine for preview/validation only after this slice is human accepted. Irreversible fiscal confirmation remains blocked until tax-rate and CFE-selection boundaries are accepted.
