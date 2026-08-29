# Taxation / TaxRules Foundation

## Status

Implementation slice under review. This slice establishes the first executable TaxRules boundary required before Sales/POS can calculate taxes or select fiscal treatment.

## Contract alignment

Public API implemented in this slice:

- `API-CAT-009 listTaxProfiles` -> `GET /api/v1/tax-profiles` with `catalog.read`.

No additional public Taxation CRUD route is invented. The accepted fiscal-configuration API remains a separate future slice.

## Domain model

`TaxProfile` is framework-free and preserves:

- organization scope;
- stable profile code and human name;
- `treatmentCode` as versionable data rather than a closed enum that pretends the legal taxonomy is finished;
- exact decimal `ratePercent`;
- `effectiveFrom` / optional `effectiveTo`;
- source name;
- source reference;
- source/specification version;
- active state;
- optimistic-concurrency version.

The Domain validates rate range and effective-date consistency but does not hard-code current Uruguay VAT values.

## Regulatory provenance principle

This repository does **not** seed production rates merely because a rate is commonly known. A usable production profile must be provisioned from reviewed regulatory/configuration evidence and retain its source/version/effective dates.

This prevents a future regulatory change from rewriting historical meaning or requiring tax percentages to be searched through controllers.

## Catalog integration

`CommercialItem.TaxProfileId` becomes usable through an application-owned validation port.

For create/update assignment, Taxation must confirm:

1. profile exists under the effective organization;
2. profile is active;
3. profile is effective on the assignment business date.

A cross-organization, missing or non-effective profile is rejected before authoritative Catalog state is committed.

The database also enforces a restricted FK from `v1_commercial_items.TaxProfileId` to `v1_tax_profiles.Id`.

## Persistence

New provider-neutral table:

- `v1_tax_profiles`.

Migration:

- `20260829031000_V1TaxProfiles`.

The migration uses EF Core migration operations and must execute on both PostgreSQL and MySQL through the existing v1 provider selector.

No Dapper or raw SQL mutation path is introduced.

## What this slice deliberately does not claim

This is **not yet** the full tax decision engine.

Not implemented here:

- Article 34 export-of-services eligibility matrix;
- export-goods/customs classification;
- receiver identity -> tax-treatment resolver;
- CFE-family selection;
- organization fiscal-policy mutation API;
- production tax-profile seed catalog;
- historical sale/fiscal tax snapshots, because Sales/Fiscal aggregates do not exist yet.

Those rules require their exact accepted regulatory evidence and must remain versioned data/policies rather than controller conditionals.

## Proof obligations

CI must demonstrate on PostgreSQL and MySQL:

- migration succeeds;
- effective-date query returns only applicable profiles;
- invalid profile rate/effective range is rejected by Domain;
- active/effective profile can be assigned to a CommercialItem;
- non-effective profile is rejected;
- existing transactional/idempotency/audit/outbox tests remain green;
- Clean Architecture guards remain green;
- the Tax Profiles controller exposes GET only, matching `API-CAT-009`.

## Next boundary after acceptance

Before Sales/POS, Taxation still needs the versioned tax-treatment decision model that separates:

`receiver fiscal identity -> transaction jurisdiction -> tax treatment -> CFE-family eligibility`.

That next slice must use the accepted DGI evidence and keep nationality, tax residence, issuing country, Uruguayan RUC possession and transaction destination/use as separate facts.
