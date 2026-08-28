# Cross-Border Receiver Identity and Tax Treatment

## Status

Target requirements/design evidence. This document closes a gap identified during DGI validation: **receiver nationality/country is not sufficient to determine the fiscal document or VAT treatment**.

Baseline date: **2026-08-28**.

Official sources used:

- DGI `Formato_CFE_v25-2` (Production enabled 2026-06-30).
- DGI `CFE_Preguntas_Frecuentes_v27`, including the rule that the export-invoice family is optional for exports of services and that ordinary CFE may be used depending on whether the acquirer is identified with a RUC.
- DGI guidance `IVA Servicios Personales` and `Servicios comprendidos en el IVA servicios personales`.
- Article 34 of Decreto 220/998 and amendments for export-of-services applicability.

`FacturacionElectronicaBases` remains a functional reference only and is not regulatory authority.

---

## 1. Core conclusion

The target must not model the problem as:

`Customer.IsForeign -> Export CFE`

That rule would be incorrect.

The system must keep separate at least these concepts:

1. country/nationality or residence context;
2. tax residence;
3. fiscal identities held by the receiver;
4. document type and issuing country;
5. existence of a Uruguayan RUC;
6. where goods are delivered/moved;
7. where a service is performed and, when relevant, where it is economically used/enjoyed;
8. whether the operation qualifies as an export under the current tax rule;
9. the CFE family applicable to that operation.

The fiscal document is selected **after** the tax/transaction context has been resolved.

---

## 2. Receiver identity types supported by current DGI format

`Formato_CFE_v25-2` defines receiver/party identity types that include:

| Code | Identity type | Country rule baseline |
|---:|---|---|
| 1 | NIE | Uruguay context according to format rule |
| 2 | RUC (Uruguay) | country `UY` |
| 3 | C.I. (Uruguay) | country `UY` |
| 4 | Otros | foreign country code, or special fallback where defined |
| 5 | Pasaporte | any country |
| 6 | DNI | Argentina, Brazil, Chile or Paraguay |
| 7 | NIFE | foreign fiscal identification, any country |

The country field uses ISO 3166-1 alpha-2 under the current format rules. DNI is not a generic world-wide identity type: its allowed issuing-country set is limited by the CFE specification.

### Design consequence

Do not store one generic `DocumentNumber` plus `IsForeign` flag.

Receiver identity is a first-class, typed, country-aware concept.

---

## 3. Domestic e-Factura and foreign identity

Current CFE format rules for the domestic e-Factura family require the receiver to be identified in the RUC path. The foreign/other receiver-document field is not valid for the ordinary e-Factura/NC/ND family (111/112/113; equivalent restrictions also apply to the account-on-behalf e-Factura family in the format).

### Invariant

A client request cannot force an ordinary domestic e-Factura merely because the customer is a company.

At minimum the selector must ask:

`Does the receiver have the Uruguayan fiscal identity required by this CFE family?`

If not, another applicable CFE/tax workflow must be selected.

---

## 4. Foreign consumer does not imply export

A foreign person may perform a local consumption-final purchase in Uruguay. When receiver identification is required, the e-Ticket path can represent admitted foreign identity types under the active specification.

Example target scenario:

- receiver: Argentine individual;
- identity: DNI, issuing country AR;
- transaction: local retail purchase delivered in Uruguay;
- business nature: consumption final;
- result: do **not** classify as export merely because the person is foreign.

The document selector must use transaction nature and applicable rules, not nationality alone.

---

## 5. Foreign company does not imply export of services

DGI guidance states that services supplied to the exterior are treated as exports of services for VAT purposes only when the operation satisfies the applicable cases of Article 34 of Decreto 220/998 and its amendments.

For several professional/technical/software categories, the guidance requires, among other applicable conditions, that the service be supplied to a person/entity abroad and be used exclusively abroad.

Therefore:

`Foreign customer != automatic export-of-services tax treatment`

The tax-treatment engine must evaluate the applicable rule profile.

A service to a foreign customer that does not meet the legal export conditions must not be silently classified as VAT-free export merely from customer country.

---

## 6. Export of services: CFE family may be optional

DGI FAQ states that use of the e-Factura de Exportación family to document **exports of services** is optional.

The FAQ explicitly allows using ordinary CFE instead:

- ordinary e-Factura when the acquirer is identified with a RUC;
- e-Ticket when the acquirer is not identified with a RUC.

This is materially different from export of goods, where the export CFE family has its own mandatory operational role for electronic issuers under the applicable rules.

### Design consequence

The system must model two separate decisions:

1. `TaxTreatmentResolver`: does this service qualify as an export under the active tax rule?
2. `FiscalDocumentSelector`: for that qualified export-of-services transaction, which permitted CFE family/profile will this issuer use?

The second decision may depend on issuer configuration and receiver RUC identity, but it cannot contradict the current DGI rule.

---

## 7. Export of goods remains a distinct workflow

A sale of goods to a foreign receiver is not modeled merely by setting `Customer.Country != UY`.

The export workflow must include the export context and document family applicable to goods, including e-Factura de Exportación and, when physical movement requires it, e-Remito de Exportación.

Country, customs/logistics context, delivery/export evidence and fiscal-document applicability are explicit inputs.

---

## 8. Target receiver fiscal model

Conceptual model:

```text
Party / Customer
  ├── legalName
  ├── residenceCountry
  ├── taxResidenceCountry
  ├── fiscalProfile
  └── fiscalIdentities[]
       ├── typeCode
       ├── type (RUC | CI | NIE | PASSPORT | DNI | NIFE | OTHER)
       ├── number
       ├── issuingCountry
       ├── validFrom?
       ├── validTo?
       ├── isPrimary
       └── verificationMetadata?
```

A foreign party may also hold a Uruguayan RUC. `residenceCountry` and `hasUruguayanRuc` are therefore independent facts.

Historical fiscal documents preserve an immutable receiver identity snapshot even if master data changes later.

---

## 9. Cross-border transaction context

Fiscal selection receives a normalized context instead of raw UI flags.

```text
CrossBorderTransactionContext
  issuerProfile
  receiverFiscalProfile
  transactionKind
  goodsServicesComposition
  domesticOrCrossBorder
  deliveryCountry
  servicePerformanceContext?
  serviceEconomicUseCountry?
  exportQualificationCandidate
  currency
  correctionReference?
  accountOnBehalfContext?
  contingencyState
  fiscalSpecificationVersion
```

The exact set is refined during Domain/Data Architecture, but the separation of concerns is mandatory.

---

## 10. Decision pipeline

The target fiscal decision chain is:

```text
ReceiverIdentityResolver
        ↓
TransactionJurisdictionResolver
        ↓
TaxTreatmentResolver
        ↓
FiscalDocumentSelector
        ↓
FiscalValidationProfile
        ↓
CFE lifecycle
```

### ReceiverIdentityResolver

Determines valid identities, issuing country, RUC presence and receiver category under the active specification.

### TransactionJurisdictionResolver

Determines domestic/cross-border facts without deciding tax treatment solely from country.

### TaxTreatmentResolver

Determines the tax classification and explains the applied rule/version/source.

Examples include:

- domestic taxable;
- domestic exempt/non-taxed where legally applicable;
- export of goods;
- export of services;
- special regulatory treatment.

### FiscalDocumentSelector

Selects eligible/required CFE family based on the resolved tax/receiver/transaction context and issuer capabilities.

A frontend cannot bypass this engine by sending an arbitrary `cfeType=121` or `cfeType=111`.

---

## 11. Required application use cases

### UC-XBR-001 — Register/maintain foreign receiver fiscal identity

1. Create/update customer/party master data.
2. Capture residence/tax-residence independently.
3. Capture typed fiscal identity and issuing country.
4. Validate type/country compatibility under the active identity rule set.
5. If RUC Uruguay is present, preserve it as a separate fiscal identity rather than changing the receiver's foreign residence.
6. Audit changes.
7. Never rewrite historical invoice receiver snapshots.

### UC-XBR-002 — Local sale to foreign consumption-final customer

1. Identify foreign receiver using an admitted identity type when required.
2. Determine that goods/service transaction is local and not an export solely from receiver country.
3. Apply domestic tax treatment.
4. Select the consumption-final CFE path under the current rules.
5. Fiscalize using the normalized foreign receiver identity.

### UC-XBR-003 — Domestic transaction with foreign company holding Uruguayan RUC

1. Resolve party as foreign-resident but with a valid Uruguayan RUC identity.
2. Determine transaction tax nature separately.
3. If the operation falls into the ordinary taxpayer e-Factura path, use the RUC identity required by that CFE family.
4. Preserve residence and RUC as distinct facts in the snapshot/audit.

### UC-XBR-004 — Service to foreign customer that qualifies as export

1. Capture service/customer/jurisdiction facts required by the active rule profile.
2. Evaluate Article-34-derived export-of-services criteria applicable to the service category.
3. Persist the rule result and evidence/provenance needed by policy.
4. Apply export tax treatment only when conditions are satisfied.
5. Determine permitted CFE strategy: export CFE family or ordinary CFE path where DGI allows it.
6. If ordinary CFE path is used, select e-Factura when receiver has RUC, otherwise e-Ticket, subject to the active specification.
7. Audit rule/version and selection result.

### UC-XBR-005 — Service to foreign customer that does NOT qualify as export

1. Resolve receiver identity and foreign context.
2. Evaluate export-of-services rule.
3. Receive negative/non-qualifying decision.
4. Apply the applicable non-export tax treatment.
5. Select CFE under that treatment.
6. Preserve explanatory rule evidence so support/accounting can understand why no export treatment was applied.

### UC-XBR-006 — Export of goods to foreign receiver

1. Establish export-of-goods context and destination.
2. Resolve foreign receiver identity required by the export specification.
3. Apply export tax profile.
4. Select/generate e-Factura de Exportación under the active rules.
5. Link e-Remito de Exportación when the physical movement workflow requires it.
6. Preserve customs/logistics/fiscal references required by enabled scope.

---

## 12. API consequences

The future API must not expose only:

```json
{ "isForeign": true }
```

as fiscal decision input.

Expected contract concepts include typed structures similar to:

```text
ReceiverFiscalIdentity
ReceiverFiscalProfile
CrossBorderTransactionContext
TaxTreatmentDecision
FiscalDocumentSelectionDecision
```

Responses from preview/validation endpoints should expose explainable rule identifiers/version and required missing data, without exposing sensitive internal configuration.

Potential API capabilities to design later:

- validate receiver fiscal identity;
- resolve/preview tax treatment;
- preview eligible CFE family;
- validate sale before confirmation;
- expose active identity/document catalogs for clients;
- return structured reasons when a requested CFE family is not eligible.

No endpoint names/routes are frozen by this requirements document.

---

## 13. Acceptance examples

1. Argentine DNI + AR is accepted as a valid typed foreign identity where that identity type is allowed; DNI + US is rejected by the identity rule profile.
2. Foreign tourist local retail sale does not become export automatically.
3. Foreign company without RUC cannot force ordinary e-Factura 111 through client input.
4. Foreign-resident company with valid Uruguayan RUC retains both facts and may use the RUC path where the operation/document rule permits it.
5. Foreign customer alone is insufficient to classify a service as export of services.
6. A qualifying export of services can follow the approved export-CFE strategy or the DGI-permitted ordinary-CFE strategy; selection is explicit and auditable.
7. An unqualified service to foreign customer receives applicable domestic/non-export tax treatment rather than zero VAT by default.
8. Historical CFE keeps the exact receiver identity/type/country snapshot used at issuance.

---

## 14. OPEN items before implementation-ready fiscal slice

The following remain intentionally open for deeper rule-matrix work:

- exact current receiver-identification thresholds/conditional fields for every enabled CFE type;
- detailed Article 34 rule catalog by service category supported in Release 1;
- evidence fields/supporting-document policy for proving foreign use/enjoyment where applicable;
- whether Release 1 exposes both allowed documentation strategies for export of services or adopts one configurable default;
- detailed export-goods/customs field set;
- foreign identity verification services, if any;
- tax treatment for additional special regimes not yet in Release-1 scope.

These OPEN items block only the corresponding fiscal-sensitive implementation slice; they do not invalidate the domain separation established here.
