# Tax Treatment Decision Matrix — Uruguay VAT / Cross-Border Foundation

Status: `IMPLEMENTATION_FOUNDATION_READY_FOR_REVIEW`

Baseline reviewed: **2026-08-29**.

This document defines the first executable boundary for tax-treatment classification. It does not replace DGI homologation, tax advice or a complete Article 34 rule catalog. The objective is to prevent the application from deriving tax treatment from customer nationality/country shortcuts while preserving source/version evidence for every regulated decision.

## Official evidence reviewed

Primary sources:

1. DGI e-Factura portal: https://www.efactura.dgi.gub.uy/
2. `Formato_CFE_v25-2`: https://www.efactura.dgi.gub.uy/files/formato_cfe_v25-2-pdf?es=
3. T.O. 2023, Título 10, Artículo 5, Territorialidad: https://www.impo.com.uy/bases/todgi2023/101-2024/5_T10
4. Decreto 220/998 actualizado, Artículo 34, Exportación de servicios: https://www.impo.com.uy/bases/decretos/220-1998/34
5. DGI `IVA Servicios Personales`, updated 2026-06-10: https://www.gub.uy/direccion-general-impositiva/comunicacion/publicaciones/iva-servicios-personales
6. DGI guidance `Servicios comprendidos en el IVA servicios personales`: https://www.gub.uy/direccion-general-impositiva/comunicacion/publicaciones/servicios-comprendidos-iva-servicios-personales
7. Indexed DGI FAQ artifact `CFE_Preguntas_Frecuentes_v27`: https://www.efactura.dgi.gub.uy/files/descargar-todas-las-preguntas-frecuentes?es=

Currentness note: the DGI e-Factura portal announced a new `Preguntas Frecuentes` version on **2026-06-25**. The downloadable artifact surfaced by the indexed public endpoint during this review still identifies itself as v27 dated 2025-09-29/printed 2025-10-01. Therefore the v27 statement about export-of-services documentation strategy is retained as regulatory evidence but remains `CURRENTNESS_TO_RECONFIRM` before the CFE selector is made production-ready. The core VAT territoriality and Article 34 classification rules below are grounded independently in current T.O. 2023 / IMPO and current DGI guidance.

## Non-negotiable semantic separation

The system shall treat these as independent facts/decisions:

```text
receiver residence
receiver tax residence
receiver fiscal identities
issuing country of each identity
has Uruguayan RUC
        |
        v
transaction facts
(goods/services, delivery/performance/use, evidence)
        |
        v
TAX TREATMENT CLASSIFICATION
        |
        +----> effective TaxProfile/rate resolution
        |
        +----> fiscal-document/CFE selection
```

There is deliberately no authoritative `IsForeign` business switch.

## Rule foundation

### TTX-001 — Territoriality is not nationality based

T.O. 2023, Título 10, Art. 5 establishes VAT territoriality for deliveries/services performed in Uruguay independently from the place of contract and from the domicile, residence or nationality of the parties. Exports of goods are not taxed, and only the exports of services determined by the Executive are treated as such.

Executable consequence:
- receiver nationality/country never directly returns `EXPORT_*`;
- a local transaction with a foreign receiver can remain `DOMESTIC`;
- cross-border facts with insufficient regulated evidence return `REQUIRES_REVIEW`, not a zero-rate assumption.

Internal rule ID: `UY-IVA-T10-ART5-TERRITORIALITY`.

### TTX-002 — Export of goods requires an authoritative export fact

Art. 5 excludes exports of goods from VAT. The engine may classify `EXPORT_GOODS` only after the upstream business/logistics/customs boundary has produced a trusted `ExportConfirmed` fact. A foreign destination typed by the client is not sufficient on its own.

Internal rule ID: `UY-IVA-T10-ART5-EXPORT-GOODS`.

### TTX-003 — Services performed entirely outside Uruguay are outside the territorial VAT scope

Current DGI guidance states that services performed entirely abroad are not within Uruguayan VAT territoriality. The engine may classify this as `OUTSIDE_VAT_SCOPE` when the server has established the performance scope as entirely outside Uruguay.

This is different from `EXPORT_SERVICES`: an operation can be outside territorial scope without being an Article 34 export-of-services case.

### TTX-004 — Export of services requires Article 34 qualification

Current DGI guidance states that a service to the exterior is treated as export of services only when it satisfies one of the cases/conditions in Art. 34 of Decreto 220/998. Art. 34 currently contains many heterogeneous numbered cases; it is not a single generic foreign-customer rule.

Executable consequence:
- `receiver.taxResidenceCountry != UY` is insufficient;
- `serviceUseCountry != UY` is insufficient by itself;
- a registered Article 34 evaluator must return `Qualified` with rule/version/source evidence before the engine may emit `EXPORT_SERVICES`;
- missing evidence or an unsupported Article 34 scenario returns `REQUIRES_REVIEW`.

### TTX-005 — Article 34 numeral 11 is a distinct rule family

For the common professional/technology slice, current Art. 34 numeral 11 includes, among others:
- advisory/technical/consulting and related services tied to activities, goods or rights economically used outside Uruguay;
- commercial mediation/arbitration, translation, engineering projects, design, architecture, technical assistance, training and audit;
- custom software design/development/implementation;
- software-use licenses;
- assignment of software exploitation/use rights.

The applicable paragraph requires the referenced services to be used exclusively abroad, subject to the specific free-zone extension for software items b), c) and d).

Internal rule family prefix: `UY-IVA-D220-ART34-11`.

This PR does **not** implement a production numeral-11 evaluator. It establishes the typed result/provenance contract an evaluator must satisfy.

## Receiver fiscal identity facts from CFE Format 25.2

The current CFE format distinguishes:

| A-C60 | Receiver document type | Country rule summary |
|---|---|---|
| `1` | NIE | country `UY` |
| `2` | RUC Uruguay | country `UY` |
| `3` | C.I. Uruguay | country `UY` |
| `4` | Otros | ISO country or `99` according to format rules |
| `5` | Pasaporte | all countries; ISO country/`99` path |
| `6` | DNI | Argentina, Brazil, Chile or Paraguay |
| `7` | NIFE | foreign fiscal ID; ISO country/`99` path |

Format 25.2 also states:
- ordinary e-Factura / NC / ND and the ordinary e-Factura Cuenta Ajena family require A-C60=`2` (RUC Uruguay);
- the foreign/other document field A-C62.1 is not valid for CFE 111/112/113/141/142/143;
- e-Ticket families can use the admitted identity types where receiver identification is required by the applicable field/threshold rules.

These facts belong to receiver/CFE eligibility. They do **not** decide VAT treatment.

## Decision matrix implemented by the foundation engine

| Operation facts | Export-service evaluator | Tax classification | Notes |
|---|---|---|---|
| Goods + trusted `DomesticDelivery` | N/A | `DOMESTIC` | Same result whether receiver is domestic/foreign or has a UY RUC. |
| Goods + trusted `ExportConfirmed` | N/A | `EXPORT_GOODS` | Must carry regulatory evidence; CFE export workflow remains separate. |
| Goods + movement/export state unknown | N/A | `REQUIRES_REVIEW` | Foreign receiver/destination cannot auto-promote to export. |
| Services + performed entirely outside Uruguay | not required | `OUTSIDE_VAT_SCOPE` | Territorial-scope decision, not Article 34 export. |
| Services + performed in Uruguay + Article 34 `Qualified` | qualified + provenance | `EXPORT_SERVICES` | Rule evidence must be effective on operation date. |
| Services + performed in Uruguay + Article 34 `NotQualified` | definitive non-match | `DOMESTIC` | `DOMESTIC` is a jurisdiction/treatment class; exact rate/exemption is resolved later from effective tax rules/profiles. |
| Services + performed in Uruguay + insufficient evidence | incomplete | `REQUIRES_REVIEW` | Missing evidence is returned explicitly. |
| Services + unsupported Article 34 scenario | unsupported | `REQUIRES_REVIEW` | No silent fallback to zero VAT. |
| Services + performance scope unknown/mixed | any | `REQUIRES_REVIEW` | Must resolve territorial facts first. |
| Mixed goods/services aggregate | any | `REQUIRES_REVIEW` | Sales must classify at line/sub-operation level. |

## Decision output contract

A resolved or review decision preserves:

- `TaxDecisionStatus`;
- `TaxTreatmentClassification`;
- stable `TreatmentCode`;
- stable reason codes;
- missing fact/evidence keys;
- `RulePackVersion`;
- one-or-many `RegulatoryRuleEvidence` records containing rule ID, source name/reference/version, effective range and optional clause.

This is the evidence that later `sale validation`, `fiscal preview`, audit and support tools can explain.

## Deliberately NOT decided by this engine

The engine does not:

- calculate VAT amounts or rates;
- choose 22%, 10%, exempt or another profile;
- decide e-Ticket vs e-Factura vs e-Factura Exportación;
- trust a client-supplied `export=true` or CFE code;
- decide receiver-document thresholds for e-Ticket;
- implement every Article 34 numeral;
- certify documentary evidence automatically;
- replace e-Resguardo/special-regime rules;
- decide customs/export logistics facts.

Those remain separate bounded policies.

## Export-of-services CFE documentation strategy

The indexed DGI FAQ v27 states that use of the export-invoice combo for exports of services is optional and that ordinary CFE may be used, with e-Factura when the acquirer has a RUC and e-Ticket otherwise. Because the DGI portal announced a newer FAQ on 2026-06-25 but the latest downloadable artifact was not discoverable in this review, this statement is retained for requirements traceability but is **not** converted into production CFE-selector code in this slice.

Status: `CURRENTNESS_TO_RECONFIRM_BEFORE_CFE_SELECTOR`.

## Release-1 rule ingestion strategy

Before Sales confirmation can consume this engine in production, the next rule slice must provide:

1. an effective `ITaxTreatmentRulePackProvider` backed by approved regulatory rule data;
2. one-or-more `IExportServiceEligibilityEvaluator` implementations for explicitly supported Article 34 scenarios;
3. evidence requirements for each implemented evaluator;
4. dual-provider persistence if the rule pack is stored in the application database;
5. regression fixtures with historical effective dates;
6. an explicit `REQUIRES_REVIEW` path for unsupported/incomplete regulated cases.

No Sales/POS implementation may bypass these ports by embedding a VAT/export shortcut in controllers or DTO mapping.
